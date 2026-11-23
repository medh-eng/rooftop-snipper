/*
 * RED SIGNAL · FIND THE ONE — browser prototype.
 * Rules live in case-engine.js (shared with Unity). This file renders the
 * square, simulates the crowd's behaviour and handles touch input.
 */
(() => {
  'use strict';
  const E = globalThis.CaseEngine;
  const W = 1600, H = 900;
  const $ = id => document.getElementById(id);
  const clamp = (v, lo, hi) => Math.max(lo, Math.min(hi, v));
  const lerp = (a, b, t) => a + (b - a) * t;
  const ease = t => 1 - Math.pow(1 - clamp(t, 0, 1), 3);
  const fmt = s => { s = Math.max(0, Math.ceil(s)); return Math.floor(s / 60) + ':' + String(s % 60).padStart(2, '0'); };

  // ------------------------------------------------------------ Text ----
  const LABEL = {
    outfit: ['Mustard bomber jacket', 'Green shirt, shoulder bag', 'Long pale coat', 'Dark hooded jacket'],
    headwear: ['No hat', 'Red cap', 'Blue cap', 'Yellow cap', 'White beanie', 'Black beanie'],
    carry: ['Nothing', 'Backpack', 'Briefcase'],
    pace: ['Strolls slowly', 'Normal pace', 'In a hurry'],
    habit: ['No habit', 'Keeps checking phone', 'Glances over shoulder', 'Stops and loiters'],
    calm: ['Reacts to noise', "Doesn't flinch at noise"],
    cell: ['Not in the cell', 'Answers the cell ping']
  };
  const ICON = { outfit: '🧥', headwear: '🧢', carry: '🎒', pace: '👟', habit: '👀', calm: '🔇', cell: '📡' };
  const HAT_COLOURS = [null, '#d8382b', '#2f6fd6', '#f2c230', '#f1efe8', '#1d1f22'];
  const clueText = c => LABEL[c.attr][c.value];

  // --------------------------------------------------------- Storage ----
  const SAVE_KEY = 'red-signal-find-the-one-v1';
  const save = (() => {
    let s = { best: {}, daily: {}, sound: true, seen: {} };
    try { s = Object.assign(s, JSON.parse(localStorage.getItem(SAVE_KEY)) || {}); } catch (e) { /* private mode */ }
    return s;
  })();
  const persist = () => { try { localStorage.setItem(SAVE_KEY, JSON.stringify(save)); } catch (e) { /* ignore */ } };

  // ----------------------------------------------------------- Audio ----
  const audio = (() => {
    let ctx = null, master = null;
    const ensure = () => {
      if (!save.sound) return null;
      if (!ctx) {
        const AC = window.AudioContext || window.webkitAudioContext;
        if (!AC) return null;
        ctx = new AC(); master = ctx.createGain(); master.gain.value = .5; master.connect(ctx.destination);
      }
      if (ctx.state === 'suspended') ctx.resume();
      return ctx;
    };
    const tone = (freq, dur, type = 'sine', vol = .3, slide = 0, delay = 0) => {
      const c = ensure(); if (!c) return;
      const t = c.currentTime + delay, o = c.createOscillator(), g = c.createGain();
      o.type = type; o.frequency.setValueAtTime(freq, t);
      if (slide) o.frequency.exponentialRampToValueAtTime(Math.max(20, freq * slide), t + dur);
      g.gain.setValueAtTime(0, t); g.gain.linearRampToValueAtTime(vol, t + .01); g.gain.exponentialRampToValueAtTime(.0001, t + dur);
      o.connect(g); g.connect(master); o.start(t); o.stop(t + dur + .05);
    };
    const noise = (dur, vol = .3, freq = 1200, delay = 0) => {
      const c = ensure(); if (!c) return;
      const t = c.currentTime + delay, len = Math.floor(c.sampleRate * dur), buf = c.createBuffer(1, len, c.sampleRate), d = buf.getChannelData(0);
      for (let i = 0; i < len; i++) d[i] = (Math.random() * 2 - 1) * Math.pow(1 - i / len, 2);
      const src = c.createBufferSource(), f = c.createBiquadFilter(), g = c.createGain();
      f.type = 'lowpass'; f.frequency.value = freq; g.gain.value = vol;
      src.buffer = buf; src.connect(f); f.connect(g); g.connect(master); src.start(t);
    };
    return {
      unlock: ensure,
      clue: () => { tone(880, .12, 'triangle', .2); tone(1320, .18, 'triangle', .15, 0, .08); },
      select: () => tone(620, .06, 'square', .06),
      beat: () => { tone(58, .14, 'sine', .5); tone(52, .16, 'sine', .4, 0, .16); },
      lock: () => tone(1500, .08, 'sine', .12),
      shot: () => { noise(.35, .5, 2600); tone(220, .5, 'sawtooth', .18, .25); },
      good: () => [523, 659, 784, 1046].forEach((f, i) => tone(f, .35, 'triangle', .18, 0, i * .09)),
      bad: () => { tone(160, .45, 'sawtooth', .2, .7); tone(120, .5, 'square', .1, .8, .05); },
      horn: () => { tone(110, 1.6, 'sawtooth', .22, 1.02); tone(165, 1.6, 'sawtooth', .12, 1.02); },
      bell: () => [0, .7].forEach(d => { tone(392, 1.8, 'sine', .3, 1, d); tone(784, 1.2, 'sine', .12, 1, d); tone(1176, .8, 'sine', .06, 1, d); }),
      surge: () => { tone(60, .9, 'square', .14, 1.5); noise(.7, .25, 5000, .05); },
      ping: () => { tone(1800, .25, 'sine', .15, .5); tone(1800, .25, 'sine', .12, .5, .3); },
      glint: () => tone(3200, .25, 'sine', .14),
      tick: () => tone(1000, .03, 'square', .05),
      block: () => tone(300, .15, 'square', .1, .6)
    };
  })();

  // ---------------------------------------------------------- Assets ----
  const images = {};
  // The single-file build (tools/build-game-offline.cjs) embeds artwork as data URLs.
  const asset = p => (globalThis.EMBEDDED_ASSETS && globalThis.EMBEDDED_ASSETS[p]) || p;
  const loadImage = src => new Promise((ok, fail) => { const i = new Image(); i.onload = () => ok(i); i.onerror = fail; i.src = src; });
  // Head, back and hand anchors per sprite, measured from the art (fractions).
  // cx: head centre (of width); hw: head width (of height); back: rear of torso (of width).
  const ANCHOR = {
    'walker-0': { cx: .54, hw: .137, back: .15 }, 'walker-1': { cx: .50, hw: .14, back: .17 },
    'walker-2': { cx: .54, hw: .137, back: .18 }, 'walker-3': { cx: .562, hw: .15, back: .19 },
    'passing-0': { cx: .609, hw: .14, back: .10 }, 'passing-1': { cx: .56, hw: .14, back: .10 },
    'passing-2': { cx: .588, hw: .14, back: .06 }, 'passing-3': { cx: .62, hw: .15, back: .07 }
  };
  // The hooded sprite shipped with a red armband from the old "marked target"
  // design. Desaturate that band so nobody is visibly marked.
  function cleanArmband(img, box) {
    const c = document.createElement('canvas'); c.width = img.width; c.height = img.height;
    const g = c.getContext('2d'); g.drawImage(img, 0, 0);
    g.save(); g.beginPath(); g.rect(box[0] * img.width, box[1] * img.height, (box[2] - box[0]) * img.width, (box[3] - box[1]) * img.height); g.clip();
    g.filter = 'grayscale(1) brightness(.55)'; g.globalCompositeOperation = 'source-atop'; g.drawImage(img, 0, 0); g.restore();
    return c;
  }
  async function loadAssets() {
    const list = ['meridian-roof', 'vesper-roof', 'solara-roof'].map(n => ['scene:' + n, '../assets/scenes/' + n + '.webp'])
      .concat([0, 1, 2, 3].map(i => ['walker-' + i, '../assets/ui/walker-' + i + '.webp']))
      .concat([0, 1, 2, 3].map(i => ['passing-' + i, '../assets/ui/passing-' + i + '.webp']))
      .concat([['projector', '../assets/ui/projector-view.webp']]);
    await Promise.all(list.map(([k, src]) => loadImage(asset(src)).then(i => { images[k] = i; })));
    images['walker-3'] = cleanArmband(images['walker-3'], [.12, .24, .5, .37]);
    images['passing-3'] = cleanArmband(images['passing-3'], [.32, .22, .7, .37]);
  }

  // ----------------------------------------------------------- Canvas ---
  const canvas = $('view'), ctx = canvas.getContext('2d', { alpha: false });
  let vw = 0, vh = 0, dpr = 1, baseScale = 1;
  function resize() {
    const r = canvas.getBoundingClientRect();
    vw = r.width; vh = r.height; dpr = Math.min(2, window.devicePixelRatio || 1);
    canvas.width = Math.round(vw * dpr); canvas.height = Math.round(vh * dpr);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    baseScale = Math.max(vw / W, vh / H);
    if (game) constrain();
  }
  window.addEventListener('resize', resize);

  // ------------------------------------------------------------ Game ----
  let game = null;          // active case state
  let menuState = { from: 'title' };

  function newGame(cfg, seed, meta) {
    const kase = E.generate(seed, cfg);
    const district = E.DISTRICTS[cfg.district];
    const people = kase.people.map(p => Object.assign({}, p, {
      wx: p.x, wy: p.y * H, facing: p.dir, step: Math.random() * 6, moving: true,
      size: 58 + (p.y - .6) * 260, mark: false, wrong: false, react: 0, ear: 0, phone: 0
    }));
    game = {
      cfg, seed, kase, district, people, meta,
      courier: people[kase.target],
      t: 0, timeLeft: cfg.time, revealed: 1, intelBought: 0, nextReveal: cfg.reveal,
      shots: 2, wrongTags: 0, provocations: 0, noiseUsed: false, pingUsed: false, noiseAt: -99, pingAt: -99,
      heat: 0, lens: false, alert: false,
      cam: { x: W / 2, y: H * .66, zoom: vw > vh ? 1.3 : 1 }, selected: null,
      hold: null, shot: null, glint: null, nextGlint: cfg.hunter ? 13 + Math.random() * 6 : Infinity,
      fx: [], birds: [], flash: 0, shake: 0, paused: false, over: false, timeScale: 1,
      lastBeat: 0, lastTick: 99, coachStep: 0, revealTimes: [0]
    };
    constrain();
    return game;
  }

  const scale = () => baseScale * game.cam.zoom;
  function constrain() {
    const s = scale(), hw = Math.min(W / 2, vw / (2 * s)), hh = Math.min(H / 2, vh / (2 * s));
    game.cam.x = clamp(game.cam.x, hw, W - hw); game.cam.y = clamp(game.cam.y, hh, H - hh);
  }
  const toScreen = (x, y) => ({ x: (x - game.cam.x) * scale() + vw / 2, y: (y - game.cam.y) * scale() + vh / 2 });
  const toWorld = (x, y) => ({ x: game.cam.x + (x - vw / 2) / scale(), y: game.cam.y + (y - vh / 2) / scale() });

  function spriteFor(p) {
    const passing = p.moving && Math.sin(p.step) > .48;
    return (passing ? 'passing-' : 'walker-') + p.outfit;
  }
  function project(p) {
    const key = spriteFor(p), im = images[key], s = toScreen(p.wx, p.wy);
    const h = p.size * scale(), w = h * im.width / im.height;
    return { p, key, im, x: s.x, y: s.y, h, w };
  }

  // ------------------------------------------------------- Simulation ---
  const PACE = [22, 36, 60];
  const HABIT_LEN = [0, 1.9, 1.0, 2.4];

  function stepPeople(dt) {
    const g = game, t = g.t;
    const noiseActive = t - g.noiseAt > .15 && t - g.noiseAt < 2.3;
    const pingActive = t - g.pingAt > .25 && t - g.pingAt < 2.0;
    for (const p of g.people) {
      let speed = PACE[p.pace] * p.jitter;
      if (p === g.courier && g.alert) speed = Math.max(speed, 62) * 1.15;
      const phase = (t + p.offset) % p.period;
      const habitOn = p.habit && phase < HABIT_LEN[p.habit];
      let facing = p.dir;
      p.phone = 0; p.ear = 0;
      if (habitOn) {
        if (p.habit === 1) { p.phone = 1; speed *= .55; }
        if (p.habit === 2) facing = -p.dir;
        if (p.habit === 3) speed = 0;
      }
      if (noiseActive && !p.calm) {
        speed = 0;
        facing = g.district.noiseX > p.wx ? 1 : -1;
        p.react = 1;
      } else p.react = 0;
      if (pingActive && p.cell) { speed = 0; p.ear = 1; }
      if (p.wrong) speed *= .3;
      p.wx += speed * p.dir * dt;
      if (p.wx > W + 70) { p.dir = -1; p.wx = W + 70; }
      if (p.wx < -70) { p.dir = 1; p.wx = -70; }
      p.moving = speed > 1;
      if (p.moving) p.step += dt * speed * .12;
      p.facing = facing;
    }
  }

  function update(dtReal) {
    const g = game;
    if (!g || g.paused || g.over) return;
    if (g.shot) { updateShot(dtReal); return; }
    const dt = dtReal * g.timeScale;
    g.t += dt;
    g.timeLeft -= dt;

    // Intel drops.
    if (g.revealed < g.cfg.clues.length && g.t >= g.nextReveal) revealClue(false);

    // Lens heat: your glint gives you away.
    if (g.lens) g.heat += dt / g.cfg.heat; else g.heat -= dt / (g.cfg.heat * .5);
    g.heat = clamp(g.heat, 0, 1);
    if (g.heat >= 1) {
      g.heat = .55;
      spook('Your lens glinted. The courier is on the move');
    }

    // Counter-sniper.
    if (g.cfg.hunter) {
      if (!g.glint && g.t >= g.nextGlint) {
        const pts = g.district.glints, pt = pts[Math.floor(Math.random() * pts.length)];
        g.glint = { x: pt[0], y: pt[1], left: 4.5 };
        audio.glint(); toast('Glint on a rooftop! Tap it', 'bad');
      }
      if (g.glint && (g.glint.left -= dt) <= 0) {
        g.glint = null; g.nextGlint = g.t + 16 + Math.random() * 8;
        g.timeLeft -= 10; g.flash = 1; g.shake = 1; audio.bad();
        toast('Counter-sniper fired! −10 s', 'bad');
      }
    }

    stepPeople(dt);

    // Follow the selected person while the lens is up.
    if (g.lens && g.selected) {
      g.cam.x = lerp(g.cam.x, g.selected.wx, 1 - Math.pow(.02, dtReal));
      g.cam.y = lerp(g.cam.y, g.selected.wy - g.selected.size * .5, 1 - Math.pow(.02, dtReal));
      constrain();
    }

    // Holding the tag button: the ring tightens, your heart pounds.
    if (g.hold) {
      g.hold.t += dtReal;
      if (g.t - g.lastBeat > .62) { g.lastBeat = g.t; audio.beat(); }
      if (!g.hold.locked && g.hold.t >= STEADY) { g.hold.locked = true; audio.lock(); }
      if (g.hold.t > STEADY + 2.6) { g.hold = null; setFire(false); toast('Out of breath. Steady again', 'bad'); }
    }

    g.fx = g.fx.filter(f => (f.t += dtReal) < f.life);
    g.birds = g.birds.filter(b => { b.t += dt; b.x += b.vx * dt; b.y += b.vy * dt; b.vy -= 30 * dt; return b.t < 4; });
    g.flash = Math.max(0, g.flash - dtReal * 2); g.shake = Math.max(0, g.shake - dtReal * 3);

    if (g.timeLeft <= 10 && Math.ceil(g.timeLeft) !== g.lastTick) { g.lastTick = Math.ceil(g.timeLeft); audio.tick(); }
    if (g.timeLeft <= 0) { g.timeLeft = 0; endCase(false, 'The courier reached the drop point.'); }
    coach();
    hud();
  }

  function revealClue(bought) {
    const g = game;
    g.revealed++; g.revealTimes.push(g.t);
    g.nextReveal = g.t + g.cfg.reveal;
    if (bought) { g.intelBought++; g.timeLeft -= 8; }
    audio.clue();
    const c = g.kase.clues[g.revealed - 1];
    toast('New intel: ' + clueText(c), 'good');
    renderDossier(g.revealed - 1);
  }

  function spook(msg) {
    const g = game;
    g.alert = true;
    if (g.timeLeft > 15) g.timeLeft = 15;
    audio.bad(); toast(msg + ' · 15 s!', 'bad');
  }

  // ---------------------------------------------------------- Tagging ---
  const STEADY = .95;
  function startHold() {
    const g = game;
    if (!g || g.over || g.paused || g.shot) return;
    if (!g.selected) { toast('Tap a person first to select them'); return; }
    g.hold = { t: 0, locked: false }; setFire(true); audio.unlock();
  }
  function releaseHold() {
    const g = game;
    if (!g || !g.hold) return;
    const h = g.hold; g.hold = null; setFire(false);
    if (!h.locked) { g.heat = Math.min(1, g.heat + .06); toast('Too early. Hold until the ring locks'); return; }
    const target = g.selected;
    const blocker = lineBlocked(target);
    if (blocker) { audio.block(); toast('Line blocked by a passer-by. Wait for a clear shot', 'bad'); blocker.flashBlock = 1; return; }
    fire(target);
  }
  function lineBlocked(target) {
    const t = project(target), cx = t.x, cy = t.y - t.h * .55;
    for (const q of game.people) {
      if (q === target || q.wy <= target.wy) continue;
      const s = project(q);
      if (Math.abs(s.x - cx) < s.w * .28 && cy > s.y - s.h * .95 && cy < s.y) return q;
    }
    return null;
  }
  function fire(target) {
    const g = game;
    g.revealedAtTag = g.revealed;   // lines visible at the moment of the tag
    g.shot = { target, t: 0, from: Object.assign({}, g.cam), zoomTo: Math.min(3, Math.max(g.cam.zoom * 1.5, 2)) };
    g.lens = false; g.timeScale = .12;
    audio.shot();
  }
  function updateShot(dtReal) {
    const g = game, s = g.shot;
    s.t += dtReal;
    const k = ease(s.t / .9);
    g.cam.zoom = lerp(s.from.zoom, s.zoomTo, k);
    g.cam.x = lerp(s.from.x, s.target.wx, k); g.cam.y = lerp(s.from.y, s.target.wy - s.target.size * .55, k);
    constrain();
    g.t += dtReal * g.timeScale;
    stepPeople(dtReal * g.timeScale);
    if (s.t >= 1.15 && !s.resolved) {
      s.resolved = true;
      const hit = s.target === g.courier;
      g.fx.push({ kind: hit ? 'hit' : 'wrong', p: s.target, t: 0, life: 1.4 });
      if (hit) { audio.good(); s.win = true; }
      else {
        audio.bad(); s.target.wrong = true; g.shots--; g.wrongTags++; g.selected = null;
        if (g.shots <= 0) s.lose = true;
      }
    }
    if (s.t >= 2.1) {
      g.shot = null; g.timeScale = 1; g.cam.zoom = s.from.zoom; constrain();
      if (s.win) endCase(true);
      else if (s.lose) endCase(false, 'Two wrong tags. The courier slipped away.');
      else spook('Wrong person. The courier saw it');
    }
    hud();
  }

  // ------------------------------------------------------ Provocations ---
  function useNoise() {
    const g = game;
    if (!g || !g.cfg.noise || g.noiseUsed || g.shot || g.over) return;
    g.noiseUsed = true; g.provocations++; g.noiseAt = g.t;
    const d = g.district;
    g.fx.push({ kind: 'noise', x: d.noiseX, y: d.noiseY, t: 0, life: 1.8 });
    for (let i = 0; i < 14; i++) g.birds.push({ x: d.noiseX + (Math.random() - .5) * 200, y: d.noiseY + 120 + Math.random() * 80, vx: (Math.random() - .5) * 260, vy: -60 - Math.random() * 80, t: 0, f: Math.random() * 6 });
    ({ meridian: audio.horn, vesper: audio.surge, solara: audio.bell })[d.id]();
    toast(d.noise + '! Who didn\'t look?');
    hud();
  }
  function usePing() {
    const g = game;
    if (!g || !g.cfg.ping || g.pingUsed || g.shot || g.over) return;
    g.pingUsed = true; g.provocations++; g.pingAt = g.t;
    g.fx.push({ kind: 'ping', t: 0, life: 1.6 });
    audio.ping(); toast('Ping sent on the cell frequency. Watch for hands to ears');
    hud();
  }

  // ------------------------------------------------------------ Input ---
  let pointer = null;
  canvas.addEventListener('pointerdown', e => {
    if (!game || game.paused || game.over || game.shot) return;
    audio.unlock();
    const r = canvas.getBoundingClientRect();
    pointer = { id: e.pointerId, x: e.clientX - r.left, y: e.clientY - r.top, ox: e.clientX - r.left, oy: e.clientY - r.top, moved: false };
    canvas.setPointerCapture(e.pointerId);
  });
  canvas.addEventListener('pointermove', e => {
    if (!pointer || pointer.id !== e.pointerId) return;
    const r = canvas.getBoundingClientRect(), x = e.clientX - r.left, y = e.clientY - r.top;
    if (!pointer.moved && Math.hypot(x - pointer.ox, y - pointer.oy) > 7) pointer.moved = true;
    if (pointer.moved && !(game.lens && game.selected)) {
      game.cam.x -= (x - pointer.x) / scale(); game.cam.y -= (y - pointer.y) / scale(); constrain();
    }
    pointer.x = x; pointer.y = y;
  });
  const endPointer = e => {
    if (!pointer || pointer.id !== e.pointerId) return;
    const p = pointer; pointer = null;
    if (!p.moved) tap(p.x, p.y);
  };
  canvas.addEventListener('pointerup', endPointer);
  canvas.addEventListener('pointercancel', () => { pointer = null; });
  canvas.addEventListener('wheel', e => {
    if (!game) return; e.preventDefault();
    game.cam.zoom = clamp(game.cam.zoom * (e.deltaY < 0 ? 1.1 : .9), 1, 2.6); constrain();
  }, { passive: false });

  function tap(x, y) {
    const g = game;
    if (g.glint) {
      const s = toScreen(g.glint.x, g.glint.y);
      if (Math.hypot(s.x - x, s.y - y) < 46) {
        g.glint = null; g.nextGlint = g.t + 16 + Math.random() * 8; audio.lock();
        g.fx.push({ kind: 'glintHit', x: s.x, y: s.y, t: 0, life: .6 });
        toast('Counter-sniper blinded', 'good'); return;
      }
    }
    const list = game.people.slice().sort((a, b) => b.wy - a.wy).map(project);
    let best = null;
    for (const s of list) {
      if (x > s.x - s.w * .36 && x < s.x + s.w * .36 && y > s.y - s.h && y < s.y + 4) { best = s.p; break; }
    }
    if (!best) {   // forgiving touch: nearest torso within 34 px
      let d = 34;
      for (const s of list) { const dd = Math.hypot(s.x - x, s.y - s.h * .5 - y); if (dd < d) { d = dd; best = s.p; } }
    }
    g.selected = best && !best.wrong ? best : null;
    if (g.selected) audio.select();
    hud();
  }

  window.addEventListener('keydown', e => {
    if (!game || game.over) return;
    const k = e.key.toLowerCase();
    if (k === ' ' && !e.repeat) { e.preventDefault(); startHold(); }
    if (k === 'z') toggleLens();
    if (k === 'm') toggleMark();
    if (k === 'n') useNoise();
    if (k === 'p') usePing();
    if (k === 'escape') pause(true);
  });
  window.addEventListener('keyup', e => { if (e.key === ' ' && game) releaseHold(); });

  function toggleLens() {
    const g = game; if (!g || g.shot) return;
    const anchor = g.selected ? { x: g.selected.wx, y: g.selected.wy - g.selected.size * .5 } : toWorld(vw / 2, vh / 2);
    g.lens = !g.lens;
    g.cam.zoom = g.lens ? 2.1 : (vw > vh ? 1.3 : 1);
    g.cam.x = anchor.x; g.cam.y = anchor.y; constrain(); hud();
  }
  function toggleMark() {
    const g = game; if (!g || !g.selected) { toast('Select a person to mark them as a suspect'); return; }
    g.selected.mark = !g.selected.mark; hud();
  }

  const fireBtn = $('fire-btn');
  fireBtn.addEventListener('pointerdown', e => { e.preventDefault(); fireBtn.setPointerCapture(e.pointerId); startHold(); });
  fireBtn.addEventListener('pointerup', e => { e.preventDefault(); releaseHold(); });
  fireBtn.addEventListener('pointercancel', () => { if (game) { game.hold = null; setFire(false); } });
  fireBtn.addEventListener('contextmenu', e => e.preventDefault());
  const setFire = on => fireBtn.classList.toggle('holding', on);
  $('lens-btn').onclick = toggleLens;
  $('mark-btn').onclick = toggleMark;
  $('noise-btn').onclick = useNoise;
  $('ping-btn').onclick = usePing;
  $('pause-btn').onclick = () => pause(true);
  $('resume-btn').onclick = () => pause(false);
  $('restart-btn').onclick = () => { $('pause-screen').hidden = true; startCase(game.meta); };
  $('quit-btn').onclick = () => { $('pause-screen').hidden = true; game = null; show(menuState.from === 'daily' ? 'title' : 'cases'); };
  document.addEventListener('visibilitychange', () => { if (document.hidden && game && !game.over) pause(true); });

  function pause(on) {
    if (!game || game.over) return;
    game.paused = on; game.hold = null; setFire(false);
    $('pause-screen').hidden = !on;
  }

  // -------------------------------------------------------------- HUD ---
  let toastTimer = 0;
  function toast(text, kind) {
    const el = $('toast'); el.textContent = text; el.className = 'toast show ' + (kind || '');
    clearTimeout(toastTimer); toastTimer = setTimeout(() => el.classList.remove('show'), 2400);
  }

  function renderDossier(fresh) {
    const g = game, el = $('dossier');
    el.innerHTML = '';
    g.kase.clues.forEach((c, i) => {
      const d = document.createElement('div');
      if (i < g.revealed) {
        const need = E.NEEDS[c.attr];
        d.className = 'clue' + (i === fresh ? ' new' : '') + (need ? ' needs' : '');
        d.innerHTML = `<span class="n">${i + 1}</span><span class="ic">${ICON[c.attr]}</span><span>${clueText(c)}${need ? `<small>use ${need === 'noise' ? g.district.noise.toLowerCase() : 'ping'} to check</small>` : ''}</span>`;
      } else {
        d.className = 'clue locked';
        d.dataset.lock = i;
        d.innerHTML = `<span class="n">${i + 1}</span><span class="ic">🔒</span><span class="cd">–</span><em>${i === g.revealed ? 'tap: now −8s' : ''}</em>`;
        if (i === g.revealed) d.onclick = () => { if (!game.shot && !game.over) revealClue(true); };
      }
      el.appendChild(d);
    });
  }

  let lastHud = '';
  function hud() {
    const g = game; if (!g) return;
    const timer = $('hud-timer');
    timer.textContent = fmt(g.timeLeft);
    timer.classList.toggle('urgent', g.timeLeft <= 15);
    const heat = document.querySelector('.heat');
    $('heat-fill').style.width = (g.heat * 100).toFixed(1) + '%';
    heat.classList.toggle('on', g.lens || g.heat > .05); heat.classList.toggle('hot', g.heat > .75);
    document.querySelectorAll('.clue.locked .cd').forEach(n => {
      const i = +n.parentNode.dataset.lock;
      n.textContent = 'in ' + fmt(g.nextReveal - g.t + (i - g.revealed) * g.cfg.reveal);
    });
    const key = [g.shots, g.lens, !!g.selected, g.selected && g.selected.mark, g.noiseUsed, g.pingUsed].join();
    if (key === lastHud) return;
    lastHud = key;
    $('hud-shots').innerHTML = '<i></i><i></i>';
    $('hud-shots').querySelectorAll('i').forEach((n, i) => n.classList.toggle('spent', i >= g.shots));
    $('lens-btn').classList.toggle('on', g.lens);
    $('mark-btn').disabled = !g.selected;
    $('mark-btn').classList.toggle('on', !!(g.selected && g.selected.mark));
    const nb = $('noise-btn'), pb = $('ping-btn');
    nb.hidden = !g.cfg.noise; pb.hidden = !g.cfg.ping;
    nb.classList.toggle('used', g.noiseUsed); nb.disabled = g.noiseUsed;
    pb.classList.toggle('used', g.pingUsed); pb.disabled = g.pingUsed;
    $('noise-label').textContent = g.district.noise.split(' ')[1].replace(/^./, c => c.toUpperCase());
    fireBtn.disabled = !g.selected;
  }

  // Tutorial nudges for the first case.
  function coach() {
    const g = game, el = $('coach');
    if (!g.meta || g.meta.index !== 0 || save.seen.coach) { el.hidden = true; return; }
    const steps = [
      [() => g.t > .5, 'Read the dossier at the top. Look for: ' + clueText(g.kase.clues[0]).toLowerCase() + '. Drag to look around.'],
      [() => g.selected, 'Tap a person who matches to select them. Tap 🔍 Lens to zoom in.'],
      [() => g.revealed > 1, 'New intel arrived. Only one person matches every line.'],
      [() => g.hold, 'Hold TAG until the ring locks, then release.']
    ];
    while (g.coachStep < steps.length && g.coachStep > 0 && steps[g.coachStep][0]()) g.coachStep++;
    if (g.coachStep === 0 && steps[0][0]()) g.coachStep = 1;
    const cur = steps[g.coachStep - 1];
    const show = cur && g.coachStep <= steps.length && !(g.coachStep === 4 && g.hold);
    el.hidden = !show;
    if (show) el.textContent = cur[1];
    if (g.coachStep === 4 && g.hold) { save.seen.coach = true; persist(); }
  }

  // ------------------------------------------------------------ Render ---
  function draw() {
    const g = game;
    if (!g) { ctx.fillStyle = '#0d1214'; ctx.fillRect(0, 0, vw, vh); return; }
    ctx.save();
    if (g.shake) ctx.translate((Math.random() - .5) * 14 * g.shake, (Math.random() - .5) * 14 * g.shake);
    const o = toScreen(0, 0), s = scale();
    ctx.drawImage(images['scene:' + g.district.id + '-roof'], o.x, o.y, W * s, H * s);

    const list = g.people.slice().sort((a, b) => a.wy - b.wy).map(project);
    for (const e of list) if (e.x > -e.w && e.x < vw + e.w) drawPerson(e);
    for (const e of list) drawPersonUI(e);

    drawBirds();
    drawFx();
    if (g.glint) drawGlint();
    if (g.lens && !g.shot) drawLensVignette();
    if (!g.lens && !g.shot) drawProjector();
    if (g.shot) drawShot();
    if (g.alert && !g.over) { ctx.fillStyle = `rgba(246,77,55,${.06 + .05 * Math.sin(g.t * 8)})`; ctx.fillRect(0, 0, vw, vh); }
    if (g.flash) { ctx.fillStyle = `rgba(255,60,40,${g.flash * .45})`; ctx.fillRect(0, 0, vw, vh); }
    if (g.over && g.reveal) drawReveal();
    ctx.restore();
  }

  function drawPerson(e) {
    const { p, im, key, h } = e, a = ANCHOR[key];
    const vwid = h * im.width / im.height;
    const swing = Math.sin(p.step), bob = p.moving ? Math.abs(swing) * h * .014 : 0;
    // contact shadow
    ctx.fillStyle = game.district.id === 'vesper' ? 'rgba(5,11,23,.5)' : 'rgba(31,25,20,.34)';
    ctx.beginPath(); ctx.ellipse(e.x, e.y + 1, vwid * .42, h * .055, 0, 0, Math.PI * 2); ctx.fill();
    ctx.save();
    ctx.translate(e.x, e.y - bob);
    ctx.scale(p.facing, 1);
    const left = -vwid / 2, top = -h;
    const hx = left + a.cx * vwid, hw = a.hw * h;
    // backpack behind the body
    if (p.carry === 1) {
      const bx = left + a.back * vwid;
      roundRect(bx - h * .055, top + h * .2, h * .11, h * .21, h * .03, '#3b4a3a', '#1e261e');
    }
    ctx.drawImage(im, left, top, vwid, h);
    // briefcase in the rear hand
    if (p.carry === 2) {
      const bx = left + (a.back + .12) * vwid, by = top + h * .5 + Math.sin(p.step) * h * .01;
      ctx.strokeStyle = '#1b130d'; ctx.lineWidth = Math.max(1, h * .008);
      ctx.beginPath(); ctx.moveTo(bx - h * .02, by); ctx.lineTo(bx - h * .02, by - h * .02); ctx.lineTo(bx + h * .02, by - h * .02); ctx.lineTo(bx + h * .02, by); ctx.stroke();
      roundRect(bx - h * .055, by, h * .11, h * .075, h * .01, '#5a3b22', '#2a1a0e');
    }
    // headwear
    if (p.headwear) {
      const col = HAT_COLOURS[p.headwear];
      const capTop = top + h * .004, cy = top + h * .045;
      ctx.fillStyle = col; ctx.strokeStyle = 'rgba(0,0,0,.45)'; ctx.lineWidth = Math.max(.6, h * .004);
      if (p.headwear <= 3) {         // cap: dome and forward peak
        ctx.beginPath(); ctx.ellipse(hx, cy, hw * .55, h * .042, 0, Math.PI, 0); ctx.closePath(); ctx.fill(); ctx.stroke();
        ctx.beginPath(); ctx.moveTo(hx + hw * .3, cy); ctx.lineTo(hx + hw * .95, cy + h * .006); ctx.lineTo(hx + hw * .9, cy + h * .016); ctx.lineTo(hx + hw * .25, cy + h * .012); ctx.closePath(); ctx.fill(); ctx.stroke();
      } else {                       // beanie: taller dome with a fold
        ctx.beginPath(); ctx.ellipse(hx - hw * .03, cy + h * .004, hw * .56, h * .05, 0, Math.PI, 0); ctx.closePath(); ctx.fill(); ctx.stroke();
        ctx.fillStyle = 'rgba(0,0,0,.18)'; ctx.fillRect(hx - hw * .59, cy - h * .004, hw * 1.12, h * .012);
      }
      void capTop;
    }
    // headphones: bright band and ear cup
    if (p.headphones) {
      ctx.strokeStyle = '#e9f7f2'; ctx.lineWidth = Math.max(1.2, h * .011);
      ctx.beginPath(); ctx.ellipse(hx - hw * .05, top + h * .055, hw * .5, h * .05, 0, Math.PI * 1.05, Math.PI * 1.95); ctx.stroke();
      ctx.fillStyle = '#e9f7f2'; ctx.beginPath(); ctx.ellipse(hx - hw * .12, top + h * .075, hw * .17, h * .022, 0, 0, Math.PI * 2); ctx.fill();
      ctx.fillStyle = '#20b39a'; ctx.beginPath(); ctx.ellipse(hx - hw * .12, top + h * .075, hw * .09, h * .012, 0, 0, Math.PI * 2); ctx.fill();
    }
    // phone glow in front of the face
    if (p.phone) {
      const px = hx + hw * .62, py = top + h * .115;
      ctx.fillStyle = 'rgba(160,220,255,.35)'; ctx.beginPath(); ctx.arc(px, py, h * .045, 0, Math.PI * 2); ctx.fill();
      ctx.fillStyle = '#d8f2ff'; ctx.fillRect(px - h * .01, py - h * .018, h * .02, h * .034);
    }
    // cell ping: hand to the earpiece, red blink
    if (p.ear) {
      const ex = hx - hw * .05, ey = top + h * .07;
      ctx.fillStyle = Math.sin(game.t * 30) > 0 ? '#ff3b2a' : '#7a1810';
      ctx.beginPath(); ctx.arc(ex, ey, Math.max(1.6, h * .016), 0, Math.PI * 2); ctx.fill();
      ctx.fillStyle = 'rgba(255,60,40,.25)'; ctx.beginPath(); ctx.arc(ex, ey, h * .05, 0, Math.PI * 2); ctx.fill();
    }
    ctx.restore();
  }

  function drawPersonUI(e) {
    const g = game, p = e.p, headY = e.y - e.h;
    if (p.mark) {
      ctx.fillStyle = '#ffd277'; ctx.strokeStyle = '#2a1e05'; ctx.lineWidth = 1.5;
      ctx.beginPath(); ctx.arc(e.x, headY - 13, 9, 0, Math.PI * 2); ctx.fill(); ctx.stroke();
      ctx.fillStyle = '#2a1e05'; ctx.font = '800 13px "Barlow Condensed", sans-serif'; ctx.textAlign = 'center'; ctx.textBaseline = 'middle'; ctx.fillText('?', e.x, headY - 12.5);
    }
    if (p.wrong) {
      ctx.strokeStyle = 'rgba(200,210,210,.8)'; ctx.lineWidth = 2;
      ctx.beginPath(); ctx.moveTo(e.x - 6, headY - 19); ctx.lineTo(e.x + 6, headY - 7); ctx.moveTo(e.x + 6, headY - 19); ctx.lineTo(e.x - 6, headY - 7); ctx.stroke();
    }
    // Readable at any zoom: who flinched at the noise, who touched an earpiece.
    if (p.react) {
      const k = clamp((g.t - g.noiseAt - .15) * 4, 0, 1);
      ctx.globalAlpha = k; ctx.fillStyle = '#fff6dd'; ctx.strokeStyle = 'rgba(0,0,0,.6)'; ctx.lineWidth = 3;
      ctx.font = '800 16px "Barlow Condensed", sans-serif'; ctx.textAlign = 'center'; ctx.textBaseline = 'alphabetic';
      ctx.strokeText('!', e.x, headY - 5); ctx.fillText('!', e.x, headY - 5); ctx.globalAlpha = 1;
    }
    if (p.ear) {
      ctx.strokeStyle = Math.sin(g.t * 25) > -.3 ? '#ff4a35' : 'rgba(255,74,53,.4)'; ctx.lineWidth = 2;
      for (let i = 1; i <= 3; i++) { ctx.beginPath(); ctx.arc(e.x, headY - 2, 4 + i * 4, -Math.PI * .8, -Math.PI * .2); ctx.stroke(); }
    }
    if (p.flashBlock) {
      p.flashBlock = Math.max(0, p.flashBlock - .03);
      ctx.strokeStyle = `rgba(130,241,219,${p.flashBlock})`; ctx.lineWidth = 2; ctx.strokeRect(e.x - e.w * .3, e.y - e.h, e.w * .6, e.h);
    }
    if (g.selected === p && !g.over) {
      const x0 = e.x - e.w * .42, x1 = e.x + e.w * .42, y0 = e.y - e.h - 4, y1 = e.y + 4, c = Math.max(6, e.h * .12);
      ctx.strokeStyle = g.hold && g.hold.locked ? '#ffd277' : '#f4efe2'; ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.moveTo(x0, y0 + c); ctx.lineTo(x0, y0); ctx.lineTo(x0 + c, y0);
      ctx.moveTo(x1 - c, y0); ctx.lineTo(x1, y0); ctx.lineTo(x1, y0 + c);
      ctx.moveTo(x0, y1 - c); ctx.lineTo(x0, y1); ctx.lineTo(x0 + c, y1);
      ctx.moveTo(x1 - c, y1); ctx.lineTo(x1, y1); ctx.lineTo(x1, y1 - c);
      ctx.stroke();
      if (g.hold) drawRing(e);
    }
  }

  function drawRing(e) {
    const g = game, k = ease(g.hold.t / STEADY);
    const over = Math.max(0, g.hold.t - STEADY - 1.2);
    const sway = (1 - k) * 14 + over * 9;
    const cx = e.x + Math.sin(g.t * 7.3) * sway, cy = e.y - e.h * .55 + Math.cos(g.t * 5.1) * sway;
    const r = lerp(Math.max(56, e.h * 1.05), Math.max(10, e.h * .2), k);
    ctx.strokeStyle = g.hold.locked ? '#ffd277' : 'rgba(255,255,255,.85)';
    ctx.lineWidth = g.hold.locked ? 3 : 2;
    ctx.beginPath(); ctx.arc(cx, cy, r, 0, Math.PI * 2); ctx.stroke();
    for (let i = 0; i < 4; i++) {
      const a = i * Math.PI / 2;
      ctx.beginPath(); ctx.moveTo(cx + Math.cos(a) * (r + 3), cy + Math.sin(a) * (r + 3)); ctx.lineTo(cx + Math.cos(a) * (r + 11), cy + Math.sin(a) * (r + 11)); ctx.stroke();
    }
    if (g.hold.locked) { ctx.fillStyle = '#ffd277'; ctx.font = '800 13px "Barlow Condensed"'; ctx.textAlign = 'center'; ctx.fillText('RELEASE', cx, cy + r + 24); }
  }

  function drawFx() {
    const g = game;
    for (const f of g.fx) {
      const k = f.t / f.life;
      if (f.kind === 'noise') {
        const s = toScreen(f.x, f.y);
        for (let i = 0; i < 3; i++) {
          const kk = (k * 1.4 + i * .18) % 1;
          ctx.strokeStyle = `rgba(255,232,180,${(1 - kk) * .7})`; ctx.lineWidth = 3;
          ctx.beginPath(); ctx.arc(s.x, s.y, 20 + kk * 260 * scale(), 0, Math.PI * 2); ctx.stroke();
        }
      } else if (f.kind === 'ping') {
        for (let i = 0; i < 3; i++) {
          const kk = (k + i * .25) % 1;
          ctx.strokeStyle = `rgba(255,70,50,${(1 - kk) * .5})`; ctx.lineWidth = 2;
          ctx.beginPath(); ctx.arc(vw * .5, -40, 60 + kk * Math.max(vw, vh), 0, Math.PI * 2); ctx.stroke();
        }
      } else if (f.kind === 'hit' || f.kind === 'wrong') {
        const e = project(f.p), cx = e.x, cy = e.y - e.h * .55;
        ctx.strokeStyle = f.kind === 'hit' ? `rgba(255,210,119,${1 - k})` : `rgba(180,200,200,${1 - k})`; ctx.lineWidth = 3;
        ctx.beginPath(); ctx.arc(cx, cy, 8 + k * 70, 0, Math.PI * 2); ctx.stroke();
        if (k < .85) {
          ctx.save(); ctx.translate(cx, cy - e.h * .75); ctx.rotate(-.08);
          ctx.font = '800 26px "Barlow Condensed", sans-serif'; ctx.textAlign = 'center';
          ctx.fillStyle = f.kind === 'hit' ? '#ffd277' : '#d6dede';
          ctx.strokeStyle = 'rgba(0,0,0,.7)'; ctx.lineWidth = 4;
          const label = f.kind === 'hit' ? 'COURIER TAGGED' : 'CIVILIAN';
          ctx.strokeText(label, 0, 0); ctx.fillText(label, 0, 0); ctx.restore();
        }
      } else if (f.kind === 'glintHit') {
        ctx.strokeStyle = `rgba(130,241,219,${1 - k})`; ctx.lineWidth = 3;
        ctx.beginPath(); ctx.arc(f.x, f.y, 10 + k * 40, 0, Math.PI * 2); ctx.stroke();
      }
    }
  }

  function drawBirds() {
    ctx.strokeStyle = 'rgba(25,25,25,.8)'; ctx.lineWidth = 1.6;
    for (const b of game.birds) {
      const s = toScreen(b.x, b.y), w = 7 * scale(), f = Math.sin(b.t * 18 + b.f) * 4 * scale();
      ctx.beginPath(); ctx.moveTo(s.x - w, s.y - f); ctx.quadraticCurveTo(s.x - w / 2, s.y - 3, s.x, s.y); ctx.quadraticCurveTo(s.x + w / 2, s.y - 3, s.x + w, s.y - f); ctx.stroke();
    }
  }

  function drawGlint() {
    const g = game, s = toScreen(g.glint.x, g.glint.y), pulse = .6 + .4 * Math.sin(g.t * 20);
    const on = s.x > 20 && s.x < vw - 20 && s.y > 150 && s.y < vh - 150;
    if (on) {
      const grd = ctx.createRadialGradient(s.x, s.y, 0, s.x, s.y, 30);
      grd.addColorStop(0, `rgba(255,255,255,${pulse})`); grd.addColorStop(.3, `rgba(255,240,200,${pulse * .6})`); grd.addColorStop(1, 'rgba(255,240,200,0)');
      ctx.fillStyle = grd; ctx.beginPath(); ctx.arc(s.x, s.y, 30, 0, Math.PI * 2); ctx.fill();
      ctx.strokeStyle = `rgba(255,255,255,${pulse})`; ctx.lineWidth = 1.5;
      ctx.beginPath(); ctx.moveTo(s.x - 22, s.y); ctx.lineTo(s.x + 22, s.y); ctx.moveTo(s.x, s.y - 22); ctx.lineTo(s.x, s.y + 22); ctx.stroke();
      ctx.strokeStyle = 'rgba(255,80,60,.9)'; ctx.lineWidth = 2;
      ctx.beginPath(); ctx.arc(s.x, s.y, 26, -Math.PI / 2, -Math.PI / 2 + Math.PI * 2 * (g.glint.left / 4.5)); ctx.stroke();
    } else {   // off-screen pointer
      const a = Math.atan2(s.y - vh / 2, s.x - vw / 2);
      const ex = clamp(vw / 2 + Math.cos(a) * vw, 26, vw - 26), ey = clamp(vh / 2 + Math.sin(a) * vh, 120, vh - 140);
      ctx.save(); ctx.translate(ex, ey); ctx.rotate(a);
      ctx.fillStyle = `rgba(255,80,60,${pulse})`; ctx.beginPath(); ctx.moveTo(16, 0); ctx.lineTo(-8, -11); ctx.lineTo(-8, 11); ctx.closePath(); ctx.fill();
      ctx.restore();
    }
  }

  function drawLensVignette() {
    const r = Math.min(vw, vh) * .47, cx = vw / 2, cy = vh * .5;
    ctx.save();
    ctx.fillStyle = 'rgba(6,10,12,.5)';
    ctx.beginPath(); ctx.rect(0, 0, vw, vh); ctx.arc(cx, cy, r, 0, Math.PI * 2, true); ctx.fill('evenodd');
    ctx.strokeStyle = 'rgba(240,233,204,.35)'; ctx.lineWidth = 1;
    ctx.beginPath(); ctx.arc(cx, cy, r, 0, Math.PI * 2); ctx.stroke();
    ctx.beginPath(); ctx.moveTo(cx - r, cy); ctx.lineTo(cx - 14, cy); ctx.moveTo(cx + 14, cy); ctx.lineTo(cx + r, cy);
    ctx.moveTo(cx, cy - r); ctx.lineTo(cx, cy - 14); ctx.moveTo(cx, cy + 14); ctx.lineTo(cx, cy + r); ctx.stroke();
    ctx.restore();
  }

  function drawProjector() {
    const im = images.projector, w = Math.min(vw * .34, vh * .42, 300), h = w * im.height / im.width;
    const kick = game.hold ? Math.sin(game.t * 40) * game.hold.t * .6 : 0;
    ctx.globalAlpha = .95;
    ctx.drawImage(im, vw - w * .82 + kick, vh - h * .62 - 112, w, h);
    ctx.globalAlpha = 1;
  }

  function drawShot() {
    const g = game, s = g.shot, e = project(s.target);
    const k = clamp((s.t - .1) / .9, 0, 1);
    const sx = vw * .92, sy = vh * 1.05, tx = e.x, ty = e.y - e.h * .55;
    const hx = lerp(sx, tx, k), hy = lerp(sy, ty, k);
    if (s.t < 1.2) {
      const grd = ctx.createLinearGradient(sx, sy, hx, hy);
      grd.addColorStop(0, 'rgba(255,210,119,0)'); grd.addColorStop(1, 'rgba(255,230,170,.95)');
      ctx.strokeStyle = grd; ctx.lineWidth = 3;
      ctx.beginPath(); ctx.moveTo(lerp(sx, tx, Math.max(0, k - .35)), lerp(sy, ty, Math.max(0, k - .35))); ctx.lineTo(hx, hy); ctx.stroke();
      ctx.fillStyle = '#fff4d2'; ctx.beginPath(); ctx.arc(hx, hy, 4, 0, Math.PI * 2); ctx.fill();
    }
    // letterbox for the slow-motion moment
    const bar = Math.min(1, s.t * 4) * vh * .08;
    ctx.fillStyle = '#000'; ctx.fillRect(0, 0, vw, bar); ctx.fillRect(0, vh - bar, vw, bar);
  }

  function drawReveal() {
    const g = game, e = project(g.courier), pulse = .5 + .5 * Math.sin(performance.now() / 160);
    ctx.fillStyle = 'rgba(5,8,9,.45)'; ctx.fillRect(0, 0, vw, vh);
    drawPerson(e);
    ctx.strokeStyle = `rgba(246,77,55,${.6 + .4 * pulse})`; ctx.lineWidth = 3;
    ctx.beginPath(); ctx.arc(e.x, e.y - e.h * .5, e.h * .75, 0, Math.PI * 2); ctx.stroke();
  }

  function roundRect(x, y, w, h, r, fill, stroke) {
    ctx.beginPath(); ctx.moveTo(x + r, y); ctx.arcTo(x + w, y, x + w, y + h, r); ctx.arcTo(x + w, y + h, x, y + h, r);
    ctx.arcTo(x, y + h, x, y, r); ctx.arcTo(x, y, x + w, y, r); ctx.closePath();
    ctx.fillStyle = fill; ctx.fill(); if (stroke) { ctx.strokeStyle = stroke; ctx.lineWidth = 1; ctx.stroke(); }
  }

  // ------------------------------------------------------- Case flow ---
  function startCase(meta) {
    $('result-screen').hidden = true;
    const cfg = meta.daily ? meta.cfg : E.CASES[meta.index];
    const seed = meta.daily ? meta.seed : (Math.random() * 4294967295) >>> 0;
    newGame(cfg, seed, meta);
    hideScreens(); $('hud').hidden = false;
    $('hud-district').textContent = game.district.name.toUpperCase();
    $('hud-case').textContent = meta.daily ? cfg.name : (meta.index + 1) + ' · ' + cfg.name;
    lastHud = '';
    renderDossier(0);
    hud();
    audio.clue();
    toast('Intel: ' + clueText(game.kase.clues[0]), 'good');
  }

  function endCase(win, why) {
    const g = game;
    if (g.over) return;
    g.over = true; g.hold = null; g.lens = false; g.glint = null; setFire(false);
    const run = {
      cleared: win, total: g.cfg.clues.length, revealed: win ? g.revealedAtTag || g.revealed : g.revealed,
      secondsLeft: g.timeLeft, provocations: g.provocations, wrongTags: g.wrongTags, intelBought: g.intelBought, elapsed: g.t
    };
    const result = E.score(run);
    if (!win) { g.reveal = true; g.cam.x = g.courier.wx; g.cam.y = g.courier.wy - g.courier.size * .5; g.cam.zoom = 1.6; constrain(); }
    let share = '';
    if (g.meta.daily) {
      share = E.shareText(g.meta.number, run, result);
      if (!save.daily[g.meta.key]) {
        save.daily[g.meta.key] = { score: result.score, stars: result.stars, share, win };
        persist();
      }
    } else if (win) {
      const prev = save.best[g.meta.index];
      if (!prev || result.score > prev.score) save.best[g.meta.index] = { score: result.score, stars: Math.max(result.stars, prev ? prev.stars : 0) };
      else if (result.stars > prev.stars) prev.stars = result.stars;
      persist();
    }
    setTimeout(() => showResult(win, why, run, result, share), win ? 300 : 1600);
  }

  function showResult(win, why, run, result, share) {
    const g = game;
    $('hud').hidden = true;
    $('result-eyebrow').textContent = win ? 'CASE CLOSED' : 'CASE LOST';
    $('result-title').textContent = win ? (run.wrongTags ? 'Tagged on the second shot' : 'Courier tagged') : 'The courier escaped';
    $('result-stars').innerHTML = [0, 1, 2].map(i => i < result.stars ? '<b>★</b>' : '★').join('');
    const c = g.courier;
    const desc = g.kase.clues.map(clueText).join(' · ');
    $('result-line').textContent = win
      ? `Found with ${run.revealed} of ${run.total} intel lines in ${fmt(run.elapsed)}.`
      : `${why} The courier was: ${desc}.`;
    void c;
    const rows = win ? [
      ['Case bounty', result.base], [`Early tag bonus (${run.total - run.revealed} lines unread)`, result.early], ['Time left', result.time],
      result.provoke ? ['Provocations', -result.provoke] : null, result.intel ? ['Intel bought', -result.intel] : null,
      result.penalty ? ['Wrong tag', -result.penalty] : null
    ].filter(Boolean) : [];
    $('result-table').innerHTML = rows.map(r => `<tr><td>${r[0]}</td><td>${r[1] > 0 ? '+' : ''}${r[1]}</td></tr>`).join('') +
      (win ? `<tr class="total"><td>Score</td><td>${result.score}</td></tr>` : '');
    const pre = $('result-share'); pre.hidden = !share; pre.textContent = share;
    const actions = $('result-actions'); actions.innerHTML = '';
    const add = (label, cls, fn) => { const b = document.createElement('button'); b.className = 'btn ' + cls; b.textContent = label; b.onclick = fn; actions.appendChild(b); };
    if (share) add('Share result', 'primary', () => doShare(share));
    if (!g.meta.daily && win && g.meta.index < E.CASES.length - 1) add('Next case', 'primary', () => openBrief(g.meta.index + 1));
    if (!g.meta.daily) add(win ? 'Replay (new crowd)' : 'Try again', win ? '' : 'primary', () => startCase(g.meta));
    else add('Practise again', '', () => startCase(g.meta));
    add('Menu', 'ghost', () => { game = null; show(g.meta.daily ? 'title' : 'cases'); });
    $('result-screen').hidden = false;
  }

  async function doShare(text) {
    try {
      if (navigator.share) { await navigator.share({ text }); return; }
      await navigator.clipboard.writeText(text); toast('Copied to clipboard', 'good');
    } catch (e) { /* cancelled */ }
  }

  // ------------------------------------------------------------ Menus ---
  const screens = ['title-screen', 'cases-screen', 'brief-screen', 'result-screen', 'howto-screen', 'pause-screen'];
  function hideScreens() { screens.forEach(s => { $(s).hidden = true; }); }
  function show(name) {
    hideScreens(); $('hud').hidden = true;
    if (name === 'title') renderTitle();
    if (name === 'cases') renderCases();
    $(name + '-screen').hidden = false;
  }

  function today() { const d = new Date(); return [d.getFullYear(), d.getMonth() + 1, d.getDate()]; }
  function renderTitle() {
    const [y, m, d] = today(), key = E.dayKey(y, m, d), n = E.dailyNumber(y, m, d), done = save.daily[key];
    $('daily-btn').innerHTML = `Daily Case #${n}<small>${done ? (done.win ? 'Solved · ' + done.score + ' pts · tap to share' : 'Courier escaped · see result') : 'Same case for everyone today'}</small>`;
    // streak of consecutive solved days ending today or yesterday
    let streak = 0;
    for (let i = 0; i < 400; i++) {
      const dt = new Date(); dt.setDate(dt.getDate() - i);
      const k = E.dayKey(dt.getFullYear(), dt.getMonth() + 1, dt.getDate());
      if (save.daily[k] && save.daily[k].win) streak++; else if (i > 0) break;
    }
    $('streak').textContent = streak > 1 ? `🔥 ${streak}-day streak` : '';
    $('sound-btn').textContent = save.sound ? '🔊' : '🔇';
  }
  function renderCases() {
    const el = $('case-list'); el.innerHTML = '';
    let stars = 0;
    E.DISTRICTS.forEach((d, di) => {
      const sec = document.createElement('div'); sec.className = 'district';
      sec.innerHTML = `<h3>${d.name.toUpperCase()}</h3><div class="cards"></div>`;
      E.CASES.forEach((c, i) => {
        if (c.district !== di) return;
        const best = save.best[i], open = i === 0 || save.best[i - 1];
        if (best) stars += best.stars;
        const b = document.createElement('button'); b.className = 'case-card'; b.disabled = !open;
        b.style.backgroundImage = `url(${asset('../assets/scenes/' + d.id + '-roof.webp')})`;
        const newIdea = c.noise && !E.CASES.slice(0, i).some(x => x.noise) ? 'NEW: noise' : c.ping && !E.CASES.slice(0, i).some(x => x.ping) ? 'NEW: ping' : c.hunter && !E.CASES.slice(0, i).some(x => x.hunter) ? 'NEW: hunter' : '';
        b.innerHTML = `${newIdea && open ? `<span class="new-tag">${newIdea}</span>` : ''}<span class="no">CASE ${String(i + 1).padStart(2, '0')}</span><b>${c.name}</b><span class="st">${best ? '★'.repeat(best.stars) + '☆'.repeat(3 - best.stars) : open ? '' : '🔒'}</span>`;
        b.onclick = () => openBrief(i);
        sec.querySelector('.cards').appendChild(b);
      });
      el.appendChild(sec);
    });
    $('star-total').textContent = `★ ${stars} / ${E.CASES.length * 3}`;
  }

  function openBrief(index, daily) {
    const meta = daily || { index };
    const cfg = daily ? daily.cfg : E.CASES[index];
    const d = E.DISTRICTS[cfg.district];
    menuState = { from: daily ? 'daily' : 'cases', meta };
    $('brief-art').style.backgroundImage = `url(${asset('../assets/scenes/' + d.id + '.webp')})`;
    $('brief-eyebrow').textContent = (daily ? 'DAILY' : 'CASE ' + String(index + 1).padStart(2, '0')) + ' · ' + d.name.toUpperCase();
    $('brief-name').textContent = cfg.name;
    $('brief-text').textContent = cfg.brief;
    const facts = [
      ['👥', `${cfg.crowd} people on the square`],
      ['📄', `${cfg.clues.length} lines of intel, one every ${cfg.reveal} s`],
      ['⏱', `${fmt(cfg.time)} before the courier reaches the drop`],
      ['🎯', 'Two shots. A wrong tag spooks the courier']
    ];
    if (cfg.noise) facts.push(['📣', `${d.noise}: everyone looks, except the trained courier`]);
    if (cfg.ping) facts.push(['📡', 'Cell ping: Red Signal members touch their earpiece']);
    if (cfg.hunter) facts.push(['✨', 'Counter-sniper: tap rooftop glints']);
    $('brief-facts').innerHTML = facts.map(f => `<li><span>${f[0]}</span>${f[1]}</li>`).join('');
    $('begin-btn').onclick = () => { audio.unlock(); startCase(meta); };
    show('brief');
  }

  function openDaily() {
    const [y, m, d] = today(), key = E.dayKey(y, m, d), number = E.dailyNumber(y, m, d);
    const done = save.daily[key];
    if (done) {
      hideScreens();
      $('result-eyebrow').textContent = 'DAILY CASE #' + number;
      $('result-title').textContent = done.win ? 'Solved' : 'Courier escaped';
      $('result-stars').innerHTML = [0, 1, 2].map(i => i < done.stars ? '<b>★</b>' : '★').join('');
      $('result-line').textContent = 'A new case arrives at midnight.';
      $('result-table').innerHTML = '';
      const pre = $('result-share'); pre.hidden = false; pre.textContent = done.share;
      const actions = $('result-actions'); actions.innerHTML = '';
      const add = (label, cls, fn) => { const b = document.createElement('button'); b.className = 'btn ' + cls; b.textContent = label; b.onclick = fn; actions.appendChild(b); };
      add('Share result', 'primary', () => doShare(done.share));
      add('Practise again', '', () => startCase({ daily: true, cfg: E.dailyConfig(y, m, d), seed: key, key, number }));
      add('Menu', 'ghost', () => show('title'));
      $('result-screen').hidden = false;
      return;
    }
    openBrief(0, { daily: true, cfg: E.dailyConfig(y, m, d), seed: key, key, number });
  }

  $('daily-btn').onclick = openDaily;
  $('campaign-btn').onclick = () => show('cases');
  $('howto-btn').onclick = () => show('howto');
  $('sound-btn').onclick = () => { save.sound = !save.sound; persist(); renderTitle(); };
  document.querySelectorAll('[data-go]').forEach(b => {
    b.onclick = () => { const to = b.dataset.go; show(to === 'back' ? (menuState.from === 'daily' ? 'title' : 'cases') : to); };
  });

  // ------------------------------------------------------------- Loop ---
  let prev = performance.now();
  function frame(now) {
    const dt = Math.min(.1, (now - prev) / 1000); prev = now;
    if (game) update(dt);
    draw();
    requestAnimationFrame(frame);
  }

  // Test hook for automated play-throughs (no effect on normal play).
  globalThis.__redSignal = {
    get game() { return game; }, startCase, openDaily, show, tap, toScreen, project, startHold, releaseHold,
    useNoise, usePing, toggleLens, revealClue: () => revealClue(true), endCase
  };

  resize();
  loadAssets().then(() => { show('title'); requestAnimationFrame(frame); })
    .catch(() => { document.body.insertAdjacentHTML('beforeend', '<p style="position:fixed;inset:auto 0 40% 0;text-align:center">Could not load artwork. Serve the folder over http or open from a full checkout.</p>'); });
})();
