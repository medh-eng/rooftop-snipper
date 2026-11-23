/*
 * RED SIGNAL · FIND THE ONE — shared case engine.
 *
 * Pure, deterministic rules shared by the browser prototype and the Unity
 * mobile game (ported 1:1 to C# in unity/HarborWatch/Assets/HarborWatch/Core).
 * Same seed => same crowd, same courier, same dossier on every platform, which
 * is what makes the Daily Case fair and shareable.
 *
 * Nothing here touches the DOM, canvas, time or Math.random.
 */
(function (root) {
  'use strict';

  // ---------------------------------------------------------------- PRNG ---
  // mulberry32. 32-bit integer maths only, so C# (uint, unchecked) matches.
  function rng(seed) {
    let a = seed >>> 0;
    const next = () => {
      a = (a + 0x6D2B79F5) >>> 0;
      let t = a;
      t = Math.imul(t ^ (t >>> 15), t | 1);
      t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
    return {
      next,
      int: n => Math.floor(next() * n),
      range: (lo, hi) => lo + next() * (hi - lo)
    };
  }

  // Weighted pick among values not in `exclude`. Weights are integers.
  function pick(r, weights, exclude) {
    let total = 0;
    for (let v = 0; v < weights.length; v++) if (!exclude || exclude.indexOf(v) < 0) total += weights[v];
    let roll = r.next() * total;
    for (let v = 0; v < weights.length; v++) {
      if (exclude && exclude.indexOf(v) >= 0) continue;
      roll -= weights[v];
      if (roll < 0) return v;
    }
    for (let v = weights.length - 1; v >= 0; v--) if (!exclude || exclude.indexOf(v) < 0) return v;
    return 0;
  }

  // ---------------------------------------------------------- Attributes ---
  // Order matters: generation consumes the PRNG in this order on every platform.
  const ATTRS = ['outfit', 'headwear', 'carry', 'pace', 'habit', 'calm', 'cell'];

  // Integer weights for an ordinary passer-by.
  const WEIGHTS = {
    outfit: [25, 25, 25, 25],          // 0 mustard bomber, 1 green shirt, 2 long pale coat, 3 dark hooded jacket
    headwear: [45, 11, 11, 11, 11, 11], // 0 none, 1 red cap, 2 blue cap, 3 yellow cap, 4 white beanie, 5 black beanie
    carry: [50, 30, 20],               // 0 nothing, 1 backpack, 2 briefcase / satchel
    pace: [20, 60, 20],                // 0 strolling, 1 normal, 2 in a hurry
    habit: [40, 25, 20, 15],           // 0 none, 1 checks phone, 2 glances back, 3 loiters
    calm: [88, 12],                    // 1 = does not react to noise (civilians: headphones)
    cell: [1, 0]                       // civilians are never cell members unless forced
  };

  // Values a courier may NOT have when that attribute is used as a clue,
  // because "normal" is not a useful thing to tell the player.
  const CLUE_EXCLUDE = {
    outfit: [],
    headwear: [],
    carry: [0],
    pace: [1],
    habit: [0],
    calm: [0],
    cell: [0]
  };

  // Provocation needed before a clue can be checked in the field.
  const NEEDS = { calm: 'noise', cell: 'ping' };

  // ------------------------------------------------------------ Districts ---
  const DISTRICTS = [
    { id: 'meridian', name: 'Port Meridian', noise: 'Ferry horn', noiseX: 1080, noiseY: 70,
      yMin: .60, yMax: .89,
      glints: [[60, 260], [260, 330], [1060, 210], [1480, 250], [560, 200]] },
    { id: 'vesper', name: 'Vesper City', noise: 'Neon surge', noiseX: 1500, noiseY: 240,
      yMin: .60, yMax: .90,
      glints: [[240, 330], [1300, 300], [860, 210], [1520, 260], [120, 300]] },
    { id: 'solara', name: 'Solara', noise: 'Church bell', noiseX: 1430, noiseY: 40,
      yMin: .60, yMax: .90,
      glints: [[340, 200], [860, 210], [1200, 270], [1520, 330], [60, 460]] }
  ];

  // ------------------------------------------------------------- Campaign ---
  // A curriculum: every case introduces at most one new idea.
  //   crowd     people on the square (courier included)
  //   clues     dossier order; first is revealed at once
  //   reveal    seconds between automatic clue drops
  //   time      seconds before the courier reaches the drop
  //   near      civilians who match every clue except the last
  //   noise / ping   provocations available
  //   hunter    counter-sniper glints appear
  //   heat      seconds of continuous lens use before your glint is spotted
  const CASES = [
    { name: 'First Light', district: 0, crowd: 10, clues: ['outfit', 'carry'], reveal: 9, time: 70, near: 1, noise: 0, ping: 0, hunter: 0, heat: 30,
      brief: 'A courier is carrying a Red Signal drive across the ferry concourse. Read the dossier. Find the one person who matches every line.' },
    { name: 'Ferry Rush', district: 0, crowd: 13, clues: ['carry', 'outfit', 'habit'], reveal: 11, time: 75, near: 1, noise: 0, ping: 0, hunter: 0, heat: 24,
      brief: 'Habits give people away. Watch how they move, not only what they wear.' },
    { name: 'Horn Blast', district: 0, crowd: 15, clues: ['outfit', 'habit', 'calm'], reveal: 12, time: 80, near: 2, noise: 1, ping: 0, hunter: 0, heat: 22,
      brief: 'Our courier is trained not to flinch. Sound the ferry horn: everyone turns to look, except the one we want. Watch for headphones.' },
    { name: 'Last Sailing', district: 0, crowd: 17, clues: ['headwear', 'carry', 'pace', 'outfit'], reveal: 12, time: 85, near: 2, noise: 1, ping: 0, hunter: 0, heat: 20,
      brief: 'Four lines of intel. Tag early for a bigger bonus, or wait for certainty.' },
    { name: 'Neon Static', district: 1, crowd: 16, clues: ['outfit', 'cell', 'habit'], reveal: 13, time: 85, near: 2, noise: 0, ping: 1, hunter: 0, heat: 20,
      brief: 'We cracked the cell radio. Send a ping and every Red Signal member touches an earpiece. Only one of them is the courier.' },
    { name: 'Night Market', district: 1, crowd: 19, clues: ['carry', 'headwear', 'cell', 'outfit'], reveal: 13, time: 90, near: 2, noise: 1, ping: 1, hunter: 0, heat: 18,
      brief: 'The market is packed. Use both provocations wisely: each one costs points.' },
    { name: 'Rooftop Glint', district: 1, crowd: 18, clues: ['habit', 'outfit', 'calm', 'carry'], reveal: 13, time: 90, near: 2, noise: 1, ping: 0, hunter: 1, heat: 16,
      brief: 'Red Signal posted a counter-sniper. If you see a glint on a rooftop, tap it before they line you up.' },
    { name: 'Blackout', district: 1, crowd: 20, clues: ['pace', 'outfit', 'carry', 'cell', 'habit'], reveal: 12, time: 95, near: 3, noise: 1, ping: 1, hunter: 1, heat: 16,
      brief: 'Five lines, a crowded square and a counter-sniper. Trust the dossier.' },
    { name: 'Bell Tower', district: 2, crowd: 20, clues: ['headwear', 'habit', 'calm', 'outfit'], reveal: 13, time: 90, near: 2, noise: 1, ping: 1, hunter: 0, heat: 16,
      brief: 'Ring the church bell. Pigeons scatter, people look up. The courier keeps walking.' },
    { name: 'Market Day', district: 2, crowd: 22, clues: ['outfit', 'carry', 'habit', 'cell', 'headwear'], reveal: 12, time: 95, near: 3, noise: 1, ping: 1, hunter: 1, heat: 14,
      brief: 'The busiest market of the year. Several cell members are working the square.' },
    { name: 'Procession', district: 2, crowd: 24, clues: ['carry', 'pace', 'outfit', 'calm', 'habit'], reveal: 12, time: 95, near: 3, noise: 1, ping: 1, hunter: 1, heat: 14,
      brief: 'A festival procession floods the square. Keep your lens cool and your shot clean.' },
    { name: 'Last Signal', district: 2, crowd: 26, clues: ['outfit', 'headwear', 'cell', 'carry', 'calm', 'habit'], reveal: 11, time: 100, near: 3, noise: 1, ping: 1, hunter: 1, heat: 12,
      brief: 'The Red Signal leader is moving the master drive. Six lines of intel. One shot that matters.' }
  ];

  // ----------------------------------------------------------- Generator ---
  // How many civilians match the first j clues (j = 0..k-1). m[0] is everyone
  // but the courier; m[k-1] is the near-miss count; geometric in between.
  function prefixCounts(civilians, k, near) {
    const m = [civilians];
    if (k === 1) return m;
    const first = Math.max(near, Math.round(civilians * 0.45));
    // A two-line dossier: the first line must still leave a real crowd.
    if (k === 2) { m.push(first); return m; }
    for (let j = 1; j < k; j++) {
      if (j === k - 1) { m.push(Math.min(near, m[j - 1])); break; }
      const t = (j - 1) / Math.max(1, k - 2);
      const v = Math.round(first * Math.pow(near / first, t));
      m.push(Math.max(near, Math.min(m[j - 1], v)));
    }
    return m;
  }

  /**
   * Build a case.
   * config: { crowd, clues:[attr], near, district }
   * Returns { seed, district, people:[...], target, clues:[{attr,value}], counts }
   * Invariant: exactly one person (people[target]) matches every clue.
   */
  function generate(seed, config) {
    const r = rng(seed);
    const k = config.clues.length;
    const civilians = config.crowd - 1;
    const order = config.clues.slice();

    // 1. Courier.
    const courier = {};
    for (const attr of ATTRS) {
      const inClues = order.indexOf(attr) >= 0;
      if (attr === 'calm' || attr === 'cell') courier[attr] = inClues ? 1 : 0;
      else courier[attr] = pick(r, WEIGHTS[attr], inClues ? CLUE_EXCLUDE[attr] : null);
    }

    // 2. Civilians by depth: depth d means "matches clues [0, d) and not clue d".
    const m = prefixCounts(civilians, k, config.near);
    const depths = [];
    for (let d = 0; d < k; d++) {
      const count = d === k - 1 ? m[k - 1] : m[d] - m[d + 1];
      for (let i = 0; i < count; i++) depths.push(d);
    }
    const people = [];
    for (const depth of depths) {
      const p = {};
      for (const attr of ATTRS) {
        const ci = order.indexOf(attr);
        if (ci >= 0 && ci < depth) p[attr] = courier[attr];
        else if (ci === depth) p[attr] = pick(r, WEIGHTS[attr], [courier[attr]]);
        else p[attr] = pick(r, WEIGHTS[attr], null);
      }
      // A civilian who does not react to noise is visibly wearing headphones.
      p.headphones = p.calm === 1 ? 1 : 0;
      p.depth = depth;
      people.push(p);
    }
    courier.headphones = 0;          // trained, not distracted: no headphones
    courier.depth = k;
    people.push(courier);

    // 3. Movement and timing, the same on every platform.
    const d = DISTRICTS[config.district || 0];
    for (const p of people) {
      p.y = d.yMin + r.next() * (d.yMax - d.yMin);          // depth on the square (fraction of 900)
      p.x = 90 + r.next() * 1420;                             // start position (of 1600)
      p.dir = r.next() < 0.5 ? -1 : 1;
      p.jitter = 0.85 + r.next() * 0.3;                       // speed multiplier
      p.period = 5.5 + r.next() * 3;                          // habit cycle (s)
      p.offset = r.next() * p.period;                         // habit phase (s)
    }

    // 4. Shuffle so the courier is not always last. Fisher-Yates.
    for (let i = people.length - 1; i > 0; i--) {
      const j = r.int(i + 1);
      const t = people[i]; people[i] = people[j]; people[j] = t;
    }
    let target = -1;
    people.forEach((p, i) => { p.id = i; if (p.depth === k) target = i; });

    return {
      seed: seed >>> 0,
      district: config.district || 0,
      people,
      target,
      clues: order.map(attr => ({ attr, value: courier[attr] })),
      counts: m
    };
  }

  function matches(person, clue) { return person[clue.attr] === clue.value; }

  // Indices of people consistent with the first `revealed` clues.
  function candidates(kase, revealed) {
    const out = [];
    for (const p of kase.people) {
      let ok = true;
      for (let i = 0; i < revealed && ok; i++) ok = matches(p, kase.clues[i]);
      if (ok) out.push(p.id);
    }
    return out;
  }

  // ------------------------------------------------------------- Scoring ---
  // revealed: clues visible when the courier was tagged; total: clue count.
  function score(run) {
    if (!run.cleared) return { score: 0, stars: 0, base: 0, early: 0, time: 0, provoke: 0, intel: 0, penalty: 0 };
    const base = 500;
    const early = Math.round(base * 0.5 * Math.max(0, run.total - run.revealed));
    const time = Math.max(0, Math.floor(run.secondsLeft)) * 4;
    const provoke = 60 * (run.provocations || 0);
    const intel = 40 * (run.intelBought || 0);
    let total = base + early + time - provoke - intel;
    let penalty = 0;
    if (run.wrongTags > 0) { penalty = total - Math.round(total * 0.4); total -= penalty; }
    total = Math.max(50, total);
    const stars = 1 + (run.wrongTags === 0 ? 1 : 0) + (run.wrongTags === 0 && run.revealed < run.total ? 1 : 0);
    return { score: total, stars, base, early, time, provoke, intel, penalty };
  }

  // ---------------------------------------------------------- Daily case ---
  const LAUNCH_UTC = Date.UTC(2026, 10, 23);  // 23 Nov 2026 = Daily #1

  function dayKey(y, m, d) { return y * 10000 + m * 100 + d; }
  function dailyNumber(y, m, d) { return Math.floor((Date.UTC(y, m - 1, d) - LAUNCH_UTC) / 86400000) + 1; }

  // Monday gentle -> Sunday hardest. Uses UTC weekday of the calendar date.
  function dailyConfig(y, m, d) {
    const weekday = new Date(Date.UTC(y, m - 1, d)).getUTCDay(); // 0 Sun
    const level = weekday === 0 ? 6 : weekday;                    // Mon 1 .. Sun 6
    const n = dailyNumber(y, m, d);
    const pools = [
      ['outfit', 'carry', 'habit'],
      ['carry', 'headwear', 'habit'],
      ['outfit', 'habit', 'calm'],
      ['headwear', 'carry', 'pace', 'outfit'],
      ['outfit', 'carry', 'cell', 'habit'],
      ['carry', 'outfit', 'habit', 'calm', 'headwear'],
      ['outfit', 'headwear', 'cell', 'carry', 'habit']
    ];
    const clues = pools[level];
    return {
      name: 'Daily Case #' + n,
      district: ((n % 3) + 3) % 3,
      crowd: 12 + level * 2,
      clues,
      reveal: 12,
      time: 70 + clues.length * 6,
      near: level < 3 ? 1 : 2,
      noise: clues.indexOf('calm') >= 0 || level >= 4 ? 1 : 0,
      ping: clues.indexOf('cell') >= 0 ? 1 : 0,
      hunter: level >= 5 ? 1 : 0,
      heat: 20 - level,
      brief: 'One case a day, the same for every operator. Find the courier and share your result.'
    };
  }

  function shareText(n, run, result) {
    const cells = [];
    for (let i = 0; i < run.total; i++) cells.push(i < run.revealed ? '🟥' : '⬛');
    const mm = Math.floor(run.elapsed / 60), ss = String(Math.floor(run.elapsed % 60)).padStart(2, '0');
    const head = run.cleared ? (run.wrongTags ? '🎯 Found (2nd shot)' : '🎯 Found') : '💨 Courier escaped';
    return [
      'RED SIGNAL · Daily #' + n,
      head + ' · ' + mm + ':' + ss,
      'Intel ' + cells.join('') + ' ' + run.revealed + '/' + run.total,
      (run.provocations ? '📣×' + run.provocations + ' ' : '') + '⭐'.repeat(result.stars) + ' ' + result.score + ' pts'
    ].join('\n');
  }

  const api = {
    rng, pick, ATTRS, WEIGHTS, CLUE_EXCLUDE, NEEDS, DISTRICTS, CASES,
    prefixCounts, generate, candidates, matches, score,
    dayKey, dailyNumber, dailyConfig, shareText
  };
  if (typeof module === 'object' && module.exports) module.exports = api;
  else root.CaseEngine = api;
})(typeof globalThis !== 'undefined' ? globalThis : this);
