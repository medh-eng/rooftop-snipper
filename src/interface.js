(() => {
  'use strict';
  const paths = {
    grid: '<rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/>',
    case: '<path d="M8 7V5a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M3 11c5 3 13 3 18 0M10 13h4"/><rect x="2" y="7" width="20" height="13" rx="2"/>',
    medal: '<circle cx="12" cy="8" r="5"/><path d="m8 12-2 9 6-3 6 3-2-9"/>',
    coin: '<path d="m12 2 8 5v10l-8 5-8-5V7zM15.5 8.5 12 6.5 7.8 9v6l4.2 2.5 3.5-2"/>',
    sound: '<path d="m11 4-6 5H2v6h3l6 5zM15 8a6 6 0 0 1 0 8m3-11a10 10 0 0 1 0 14"/>',
    mute: '<path d="m11 4-6 5H2v6h3l6 5zM16 9l6 6m0-6-6 6"/>',
    pin: '<path d="M19 10c0 5-7 11-7 11S5 15 5 10a7 7 0 1 1 14 0Z"/><circle cx="12" cy="10" r="2.2"/>',
    cross: '<circle cx="12" cy="12" r="7.5"/><path d="M12 1v6m0 10v6M1 12h6m10 0h6"/>',
    lens: '<circle cx="10" cy="10" r="6.5"/><path d="m15 15 6 6M7 10h6m-3-3v6"/>',
    arrow: '<path d="M3 12h17m-6-6 6 6-6 6"/>',
    chevron: '<path d="m9 5 7 7-7 7"/>',
    down: '<path d="m6 9 6 6 6-6"/>',
    play: '<path fill="currentColor" stroke="none" d="M6 3.5 21 12 6 20.5z"/>',
    info: '<circle cx="12" cy="12" r="9"/><path d="M12 11v6m0-10v.2"/>',
    lock: '<rect x="5" y="10" width="14" height="11" rx="2"/><path d="M8 10V7a4 4 0 0 1 8 0v3m-4 4v3"/>',
    sun: '<path d="M2 17h20M4 21h16M7 17a5 5 0 0 1 10 0M12 3v4M3 8l3 3m12 0 3-3"/>',
    drone: '<path d="m6 6 12 12M6 18 18 6"/><ellipse cx="6" cy="5.5" rx="4" ry="2.5"/><ellipse cx="18" cy="5.5" rx="4" ry="2.5"/><ellipse cx="6" cy="18.5" rx="4" ry="2.5"/><ellipse cx="18" cy="18.5" rx="4" ry="2.5"/><rect x="9" y="9" width="6" height="6" rx="1"/>',
    stack: '<path d="m12 3 9 5-9 5-9-5zM3 12l9 5 9-5M3 16l9 5 9-5"/>',
    time: '<circle cx="12" cy="12" r="9"/><path d="M12 6v6l4 2"/>',
    close: '<path d="m5 5 14 14M19 5 5 19"/>',
    pause: '<path stroke-width="3.5" d="M8 5v14m8-14v14"/>',
    reload: '<path d="M3 10a9 9 0 1 1 1 7M3 4v6h6"/>',
    star: '<path d="m12 2 3 6.5 7 1-5 5 1 7-6-3.5L6 21l1-6.5-5-5 7-1z"/>',
    check: '<path d="m4 12 5 5L20 6"/>',
    shield: '<path d="m12 2 8 3v7c0 6-8 10-8 10S4 18 4 12V5z"/><path d="m8 12 2.5 2.5L16 9"/>',
    cell: '<rect x="5" y="5" width="14" height="16" rx="2"/><path d="M9 5V2h6v3M8 15h8m-8-4h8"/>',
    bolt: '<path d="M13 2 4 14h6l-1 8 11-14h-7z"/>',
    target: '<path d="m12 3 9 9-9 9-9-9z"/><circle cx="12" cy="12" r="1.7"/>',
    map: '<path d="m3 5 6-2 6 2 6-2v16l-6 2-6-2-6 2zm6-2v16m6-14v16"/>',
    focus: '<path d="M8 3H3v5m13-5h5v5M8 21H3v-5m13 5h5v-5"/><circle cx="12" cy="12" r="3"/>',
    signal: '<path d="M4 19v-3m5 3v-7m5 7V8m5 11V4"/>',
    flag: '<path d="M5 21V3m0 1c5-4 9 4 14 0v9c-6 4-9-4-14 0"/>',
    book: '<path d="M3 4h7a3 3 0 0 1 2 2 3 3 0 0 1 2-2h7v15h-7a3 3 0 0 0-2 2 3 3 0 0 0-2-2H3zm9 2v15"/>',
    heart: '<path d="M3 12h4l3-7 4 14 3-7h4"/>'
  };
  const icon = key =>
    `<svg aria-hidden="true" viewBox="0 0 24 24" class="icon" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">${paths[key] || ''}</svg>`;
  const format = n => n.toLocaleString('en-US');
  const padded = n => String(n).padStart(2, '0');
  const asset = path => globalThis.EMBEDDED_ASSETS?.[path] || path;
  const scene = id => asset(`assets/scenes/${id}.webp`);
  const photo = id => asset(`assets/ui/${id}.webp`);
  let context;
  const melodies = {
    click: [
      [280, .05]
    ],
    fire: [
      [130, .08],
      [76, .09]
    ],
    hit: [
      [440, .06],
      [660, .09]
    ],
    block: [
      [135, .16]
    ],
    miss: [
      [105, .07]
    ],
    reload: [
      [220, .07],
      [330, .08]
    ],
    focus: [
      [510, .06]
    ],
    win: [
      [392, .1],
      [494, .1],
      [587, .12],
      [784, .22]
    ],
    upgrade: [
      [330, .1],
      [494, .12],
      [660, .14]
    ],
    drone: [
      [210, .11],
      [420, .15]
    ]
  };

  function tone(name, enabled) {
    if (!enabled) return;
    try {
      const Audio = window.AudioContext || window.webkitAudioContext;
      if (!Audio) return;
      context ||= new Audio();
      if (context.state === 'suspended') context.resume().catch(() => {});
      let t = context.currentTime;
      for (const [pitch, length] of melodies[name] || melodies.click) {
        const o = context.createOscillator(),
          g = context.createGain();
        o.type = ['fire', 'miss', 'block'].includes(name) ? 'triangle' : 'sine';
        o.frequency.setValueAtTime(pitch, t);
        o.frequency.exponentialRampToValueAtTime(Math.max(20, pitch * .9), t + length);
        g.gain.setValueAtTime(.0001, t);
        g.gain.exponentialRampToValueAtTime(.057, t + .012);
        g.gain.exponentialRampToValueAtTime(.0001, t + length);
        o.connect(g).connect(context.destination);
        o.start(t);
        o.stop(t + length + .015);
        t += length * .95;
      }
    } catch {
      /* Audio is optional in silent or restricted browsers. */ }
  }
  globalThis.SignalUI = {
    icon,
    format,
    padded,
    asset,
    scene,
    photo,
    tone
  };
})();
