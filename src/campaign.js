(() => {
  'use strict';
  const districts = [{
      id: 'meridian',
      name: 'Port Meridian',
      subtitle: 'Harbor district',
      light: 'Dawn',
      time: '06:40',
      cover: 'harbor-cover',
      lanes: [.66, .72, .79, .87]
    },
    {
      id: 'vesper',
      name: 'Vesper City',
      subtitle: 'Neon district',
      light: 'Blue hour',
      time: '20:15',
      cover: 'vesper',
      lanes: [.65, .72, .80, .89]
    },
    {
      id: 'solara',
      name: 'Solara',
      subtitle: 'Old quarter',
      light: 'Golden hour',
      time: '17:10',
      cover: 'solara',
      lanes: [.65, .73, .81, .89]
    }
  ];
  const missions = [{
      name: 'Harbor Watch',
      district: 0,
      ground: 3,
      air: 0,
      crowd: 6,
      active: 2,
      seconds: 90,
      base: 240,
      pace: .80,
      brief: 'Three members of the Red Signal terrorist cell are crossing the ferry concourse. Follow their red diamonds through the morning crowd.'
    },
    {
      name: 'Ferry Line',
      district: 0,
      ground: 4,
      air: 0,
      crowd: 8,
      active: 2,
      seconds: 85,
      base: 280,
      pace: 1,
      brief: 'The next ferry just docked. Four marked couriers weave through passengers and pause between crossings.'
    },
    {
      name: 'Last Departure',
      district: 0,
      ground: 5,
      air: 0,
      crowd: 10,
      active: 3,
      seconds: 85,
      base: 320,
      pace: 1.1,
      brief: 'The final sailing brings a second wave of signals. Keep the concourse clear to close out Port Meridian.'
    },
    {
      name: 'Neon Relay',
      district: 1,
      ground: 4,
      air: 0,
      crowd: 8,
      active: 2,
      seconds: 85,
      base: 340,
      pace: 1.06,
      brief: 'Red diamonds reflect off the rain-soaked plaza. Follow the relay team through Vesper’s evening commuters.'
    },
    {
      name: 'After Hours',
      district: 1,
      ground: 6,
      air: 0,
      crowd: 11,
      active: 3,
      seconds: 85,
      base: 400,
      pace: 1.18,
      brief: 'Lanterns come on and the night market fills up. Six marked runners cross the arcade in waves.'
    },
    {
      name: 'Ghost Frequency',
      district: 1,
      ground: 0,
      air: 5,
      crowd: 0,
      active: 2,
      seconds: 90,
      base: 460,
      pace: 1,
      brief: 'Take the drone above the neon roofs. Five rogue transmitters are airborne. Break their links with a pulse.'
    },
    {
      name: 'Market Day',
      district: 2,
      ground: 6,
      air: 0,
      crowd: 12,
      active: 3,
      seconds: 85,
      base: 480,
      pace: 1.24,
      brief: 'The old quarter is hosting its busiest market of the year. Track six marked saboteurs between the colonnades.'
    },
    {
      name: 'Sun Chaser',
      district: 2,
      ground: 0,
      air: 6,
      crowd: 0,
      active: 3,
      seconds: 85,
      base: 520,
      pace: 1.13,
      brief: 'Fly over Solara’s terracotta rooftops. Rogue drones split up above the domes as the sun drops.'
    },
    {
      name: 'Last Signal',
      district: 2,
      ground: 4,
      air: 4,
      crowd: 10,
      active: 2,
      seconds: 115,
      base: 650,
      pace: 1.2,
      brief: 'Clear the marked ground team in the square, then take the drone uplink and finish the operation above the city.'
    }
  ].map((mission, id) => ({
    ...mission,
    id
  }));
  const kits = [{
      id: 'gun',
      name: 'Pulse rifle',
      type: 'Projector',
      icon: 'cross',
      image: 'gun',
      labels: ['Kestrel R1', 'Kestrel R2', 'Kestrel R3'],
      stat: 'Pulse recovery',
      values: ['0.55 s', '0.40 s', '0.27 s'],
      costs: [300, 720],
      detail: 'A quicker follow-up pulse keeps a chain alive when marks cross.'
    },
    {
      id: 'lens',
      name: 'Optical lens',
      type: 'Optics',
      icon: 'lens',
      image: 'lens',
      labels: ['Glass I', 'Glass II', 'Glass III'],
      stat: 'Magnification',
      values: ['1.5×', '1.9×', '2.3×'],
      costs: [240, 580],
      detail: 'Bring distant marks closer in both rooftop and aerial views.'
    },
    {
      id: 'cartridge',
      name: 'Cartridge',
      type: 'Power cell',
      icon: 'cell',
      image: 'cartridge',
      labels: ['Cell 5', 'Cell 7', 'Cell 9'],
      stat: 'Shots per charge',
      values: ['5', '7', '9'],
      costs: [210, 560],
      detail: 'Carry more pulses in each charge before reloading.'
    },
    {
      id: 'round',
      name: 'Pulse rounds',
      type: 'Ammunition',
      icon: 'bolt',
      image: 'round',
      labels: ['Echo I', 'Echo II', 'Echo III'],
      stat: 'Signal catch radius',
      values: ['Standard', '+18%', '+36%'],
      costs: [260, 640],
      detail: 'An expanded signal field helps catch the edge of a marked target.'
    }
  ];
  const SAVE = 'rooftop-red-signal-v1';
  const starter = 350;
  const cache = 350;
  const clamp = (n, lo, hi) => Math.max(lo, Math.min(hi, n));
  const integer = (n, limit = 1e9) => Number.isFinite(n) ? clamp(Math.floor(n), 0, limit) : 0;
  const fresh = () => ({
    version: 1,
    credits: starter,
    sound: true,
    upgrades: {
      gun: 0,
      lens: 0,
      cartridge: 0,
      round: 0
    },
    best: {},
    earned: 0,
    clears: 0,
    hits: 0,
    shots: 0,
    bestChain: 0
  });

  function sanitize(raw) {
    const p = fresh();
    if (!raw || raw.version !== 1) return p;
    p.credits = raw.credits == null ? starter : integer(raw.credits);
    p.sound = raw.sound !== false;
    for (const k of kits) p.upgrades[k.id] = integer(raw.upgrades?.[k.id], 2);
    for (const key of ['earned', 'clears', 'hits', 'shots', 'bestChain']) p[key] = integer(raw[key]);
    for (const m of missions) {
      const r = raw.best?.[m.id];
      if (!r || !integer(r.stars, 3)) break;
      p.best[m.id] = {
        stars: integer(r.stars, 3),
        precision: integer(r.precision, 100),
        seconds: integer(r.seconds, 999),
        chain: integer(r.chain)
      };
    }
    return p;
  }

  function readSave() {
    try {
      return sanitize(JSON.parse(localStorage.getItem(SAVE)));
    } catch {
      return fresh();
    }
  }

  function writeSave(p) {
    try {
      localStorage.setItem(SAVE, JSON.stringify(p));
      return true;
    } catch {
      return false;
    }
  }
  const completed = p => Object.keys(p.best).length;
  const available = (p, id) => Number.isInteger(id) && id >= 0 && id < missions.length && (id === 0 ||
    Boolean(p.best[id - 1]));
  const mode = m => m.ground && m.air ? 'Rooftop + drone' : m.air ? 'Drone uplink' : 'Rooftop';
  const stats = p => ({
    reload: 1.1,
    recovery: [.55, .4, .27][p.upgrades.gun],
    zoom: [1.5, 1.9, 2.3][p.upgrades.lens],
    capacity: [5, 7, 9][p.upgrades.cartridge],
    radius: [1, 1.18, 1.36][p.upgrades.round]
  });

  function buy(p, id) {
    const k = kits.find(item => item.id === id);
    if (!k) return false;
    const tier = p.upgrades[id];
    if (tier >= 2 || p.credits < k.costs[tier]) return false;
    p.credits -= k.costs[tier];
    p.upgrades[id] = tier + 1;
    return true;
  }

  function settle(p, m, run) {
    if (!available(p, m.id) || run.settled || run.hits !== m.ground + m.air) return null;
    run.settled = true;
    const first = !p.best[m.id];
    const precision = Math.round(run.hits / Math.max(run.shots, run.hits) * 100);
    const clean = m.ground ? run.blocks === 0 : run.shots === run.hits;
    const stars = 1 + Number(precision >= 80) + Number(clean);
    const districtCache = first && m.id % 3 === 2 ? cache : 0;
    const rewards = [
      ['Contract bounty', first ? m.base : Math.round(m.base / 2)],
      ['Precision bonus', Math.round(precision * .8)],
      [m.ground ? 'Crowd protected' : 'Clean intercept', clean ? 100 : 0],
      ['Signal chain', run.bestChain * 12],
      ['Gold relay', run.relay ? 80 : 0],
      ['First clear', first ? 120 : 0],
      ['District cache', districtCache]
    ];
    const total = rewards.reduce((sum, row) => sum + row[1], 0);
    const old = p.best[m.id];
    const seconds = Math.ceil(run.elapsed);
    p.best[m.id] = {
      stars: Math.max(old?.stars || 0, stars),
      precision: Math.max(old?.precision || 0, precision),
      seconds: old ? Math.min(old.seconds, seconds) : seconds,
      chain: Math.max(old?.chain || 0, run.bestChain)
    };
    p.credits += total;
    p.earned += total;
    p.clears++;
    p.hits += run.hits;
    p.shots += run.shots;
    p.bestChain = Math.max(p.bestChain, run.bestChain);
    return {
      total,
      rewards,
      precision,
      stars,
      first,
      districtCache,
      seconds
    };
  }
  const api = {
    districts,
    missions,
    kits,
    cache,
    clamp,
    fresh,
    sanitize,
    readSave,
    writeSave,
    completed,
    available,
    mode,
    stats,
    buy,
    settle
  };
  globalThis.RedSignal = api;
  if (typeof module !== 'undefined' && module.exports) module.exports = api;
})();
