// Writes game/vectors.json: reference cases produced by the JS engine.
// The Unity C# tests load the same file and must reproduce it exactly, which
// keeps the Daily Case identical on browser and Android.
//   node tools/case-vectors.cjs
const fs = require('node:fs');
const path = require('node:path');
const E = require('../game/case-engine.js');

const round = v => Math.round(v * 1e6) / 1e6;

function build() {
  const out = { rng: [], cases: [], daily: [] };
  for (const seed of [0, 1, 42, 12345, 20261123, 4294967295]) {
    const r = E.rng(seed);
    out.rng.push({ seed, values: [r.next(), r.next(), r.next(), r.next()].map(round) });
  }
  const pack = (seed, cfg, label) => {
    const k = E.generate(seed, cfg);
    return {
      label, seed, target: k.target, counts: k.counts,
      clues: k.clues.map(c => c.attr + '=' + c.value),
      people: k.people.map(p => [p.outfit, p.headwear, p.carry, p.pace, p.habit, p.calm, p.cell, p.headphones,
        round(p.x), round(p.y), p.dir, round(p.jitter), round(p.period), round(p.offset)])
    };
  };
  E.CASES.forEach((c, i) => out.cases.push(pack(1000 + i, c, 'case ' + (i + 1) + ' ' + c.name)));
  for (const [y, m, d] of [[2026, 11, 23], [2026, 11, 29], [2027, 2, 14]]) {
    const cfg = E.dailyConfig(y, m, d);
    out.daily.push(Object.assign({ date: [y, m, d], number: E.dailyNumber(y, m, d), crowd: cfg.crowd, district: cfg.district },
      pack(E.dayKey(y, m, d), cfg, cfg.name)));
  }
  return JSON.stringify(out, null, 1) + '\n';
}

if (require.main === module) {
  fs.writeFileSync(path.join(__dirname, '..', 'game', 'vectors.json'), build());
  console.log('wrote game/vectors.json');
}
module.exports = { build };
