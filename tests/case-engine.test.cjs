const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const E = require('../game/case-engine.js');

test('PRNG is deterministic and matches published mulberry32 values', () => {
  const a = E.rng(1), b = E.rng(1);
  for (let i = 0; i < 50; i++) assert.equal(a.next(), b.next());
  const r = E.rng(42);
  const first = r.next();
  assert.ok(first >= 0 && first < 1);
  // Pinned so a JS or C# change that breaks parity fails loudly.
  assert.equal(Math.round(E.rng(12345).next() * 1e9), 979728268);
});

test('every campaign case has exactly one courier for many seeds', () => {
  E.CASES.forEach((c, index) => {
    for (let seed = 1; seed <= 400; seed++) {
      const k = E.generate(seed * 7919 + index, c);
      assert.equal(k.people.length, c.crowd, c.name);
      const all = E.candidates(k, c.clues.length);
      assert.deepEqual(all, [k.target], `${c.name} seed ${seed}`);
      assert.equal(k.people[k.target].headphones, 0);
    }
  });
});

test('clues narrow the field gradually instead of giving it away', () => {
  E.CASES.forEach((c, index) => {
    const k = E.generate(1000 + index, c);
    const sizes = c.clues.map((_, i) => E.candidates(k, i + 1).length);
    for (let i = 1; i < sizes.length; i++) assert.ok(sizes[i] <= sizes[i - 1], c.name);
    // After all but the last clue at least courier + near-miss decoys remain.
    if (c.clues.length > 1) assert.ok(E.candidates(k, c.clues.length - 1).length >= 1 + c.near, c.name);
    // The first clue alone never identifies the courier.
    assert.ok(sizes[0] >= 3, `${c.name}: first clue leaves ${sizes[0]}`);
  });
});

test('clue values are informative: never "normal pace", "no bag" or "no habit"', () => {
  for (let seed = 1; seed < 300; seed++) {
    const k = E.generate(seed, E.CASES[11]);
    for (const clue of k.clues) assert.ok(E.CLUE_EXCLUDE[clue.attr].indexOf(clue.value) < 0);
  }
});

test('headphones explain every civilian who ignores noise; cell members only exist for ping cases', () => {
  for (let seed = 1; seed < 200; seed++) {
    const noPing = E.generate(seed, E.CASES[2]);
    assert.ok(noPing.people.every(p => p.cell === 0));
    const ping = E.generate(seed, E.CASES[4]);
    const cell = ping.people.filter(p => p.cell === 1);
    assert.ok(cell.length >= 1 && cell.length <= 1 + E.CASES[4].near + 1);
    for (const p of ping.people) if (p.id !== ping.target) assert.equal(p.headphones, p.calm);
  }
});

test('same seed, same case', () => {
  const a = JSON.stringify(E.generate(77, E.CASES[7]));
  const b = JSON.stringify(E.generate(77, E.CASES[7]));
  assert.equal(a, b);
  assert.notEqual(a, JSON.stringify(E.generate(78, E.CASES[7])));
});

test('scoring rewards early, clean tags and charges for help', () => {
  const s = run => E.score(Object.assign({ cleared: true, total: 4, revealed: 4, secondsLeft: 30, provocations: 0, wrongTags: 0, intelBought: 0 }, run));
  assert.equal(s({}).score, 500 + 120);
  assert.equal(s({ revealed: 2 }).score, 500 + 500 + 120);
  assert.equal(s({ revealed: 2 }).stars, 3);
  assert.equal(s({}).stars, 2);
  assert.equal(s({ wrongTags: 1 }).stars, 1);
  assert.equal(s({ wrongTags: 1 }).score, Math.round(620 * 0.4));
  assert.equal(s({ provocations: 2, intelBought: 1 }).score, 620 - 120 - 40);
  assert.equal(E.score({ cleared: false }).score, 0);
  assert.equal(s({ secondsLeft: 0, provocations: 30 }).score, 50);
});

test('daily case: numbering, weekday difficulty and solvable for a year', () => {
  assert.equal(E.dailyNumber(2026, 11, 23), 1);
  assert.equal(E.dailyNumber(2026, 12, 1), 9);
  assert.equal(E.dayKey(2026, 11, 23), 20261123);
  assert.equal(E.dailyConfig(2026, 11, 23).crowd, 14);     // Monday
  assert.equal(E.dailyConfig(2026, 11, 29).crowd, 24);     // Sunday
  const start = Date.UTC(2026, 10, 23);
  for (let i = 0; i < 366; i++) {
    const d = new Date(start + i * 86400000);
    const y = d.getUTCFullYear(), m = d.getUTCMonth() + 1, day = d.getUTCDate();
    const cfg = E.dailyConfig(y, m, day);
    const k = E.generate(E.dayKey(y, m, day), cfg);
    assert.deepEqual(E.candidates(k, cfg.clues.length), [k.target]);
    if (cfg.clues.indexOf('cell') >= 0) assert.equal(cfg.ping, 1);
    if (cfg.clues.indexOf('calm') >= 0) assert.equal(cfg.noise, 1);
  }
});

test('campaign cases unlock provocations before their clues need them', () => {
  for (const c of E.CASES) {
    if (c.clues.indexOf('calm') >= 0) assert.equal(c.noise, 1, c.name);
    if (c.clues.indexOf('cell') >= 0) assert.equal(c.ping, 1, c.name);
    assert.ok(c.time >= c.reveal * (c.clues.length - 1) + 20, c.name + ' leaves time after the last clue');
  }
});

test('share text has no spoilers and fits a chat message', () => {
  const run = { cleared: true, total: 4, revealed: 2, elapsed: 41.3, provocations: 1, wrongTags: 0 };
  const text = E.shareText(9, run, E.score(Object.assign({ secondsLeft: 40 }, run)));
  assert.match(text, /Daily #9/);
  assert.match(text, /0:41/);
  assert.match(text, /🟥🟥⬛⬛ 2\/4/);
  assert.ok(text.length < 160);
});

test('shared vectors file is current (run: node tools/case-vectors.cjs)', () => {
  const file = path.join(__dirname, '..', 'game', 'vectors.json');
  const expected = require('../tools/case-vectors.cjs').build();
  assert.equal(fs.readFileSync(file, 'utf8'), expected);
});
