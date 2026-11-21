(() => {
  'use strict';
  const {
    clamp,
    districts
  } = globalThis.RedSignal;
  const {
    scene,
    photo
  } = globalThis.SignalUI;
  const W = 1600,
    H = 900;
  const images = new Map();

  function image(url) {
    if (!images.has(url)) {
      const pending = new Promise((resolve, reject) => {
        const im = new Image();
        im.onload = () => resolve(im);
        im.onerror = () => reject(new Error('Scene image unavailable'));
        im.src = url;
      });
      images.set(url, pending);
      pending.catch(() => images.delete(url));
    }
    return images.get(url);
  }
  const rand = (lo, hi) => lo + Math.random() * (hi - lo);
  const hash = (n, span) => ((n * 0.61803399) % 1) * span;

  function resolveShot(items, x, y, radius, touch = false) {
    // Markers are explicit commands. For overlapping bodies, the frontmost person owns the contact.
    for (let i = items.length - 1; i >= 0; i--) {
      const p = items[i];
      if (!p.actor.target) continue;
      const size = Math.max(touch ? 23 : 13, p.markerSize + 3) * radius;
      if (Math.abs(x - p.x) + Math.abs(y - p.markY) <= size * 1.32) return p.actor;
    }
    for (let i = items.length - 1; i >= 0; i--) {
      const p = items[i],
        scale = p.actor.target ? radius : 1;
      if (Math.abs(x - p.x) <= p.width * .43 * scale && Math.abs(y - (p.y - p.height * .48)) <= p.height *
        .50 * scale) return p.actor;
    }
    return null;
  }
  class Field {
    constructor(canvas, mission, stats, callbacks) {
      this.canvas = canvas;
      this.ctx = canvas.getContext('2d', {
        alpha: false
      });
      this.m = mission;
      this.cfg = stats;
      this.cb = callbacks;
      this.d = districts[mission.district];
      this.run = {
        hits: 0,
        shots: 0,
        blocks: 0,
        bestChain: 0,
        elapsed: 0
      };
      this.air = !mission.ground;
      this.phaseHits = 0;
      this.chain = 0;
      this.serial = 0;
      this.actors = [];
      this.effects = [];
      this.ammo = stats.capacity;
      this.reloading = 0;
      this.cooldown = 0;
      this.paused = true;
      this.destroyed = false;
      this.done = false;
      this.cam = {
        x: W / 2,
        y: H * .53,
        zoom: this.air ? 1.3 : 1
      };
      this.scoped = false;
      this.aim = {
        x: 0,
        y: 0
      };
      this.kick = 0;
      this.relay = null;
      this.relayOffered = false;
      this.keys = new Set();
      this.lastHud = '';
      this.lastFind = -1;
      this.time = 0;
      this.weather = Array.from({
        length: 30
      }, (_, i) => [hash(i + 1, 1), hash(i * 7 + 3, 1)]);
      this.reduced = typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)')
        .matches;
      this.events = new AbortController();
      this.frame = this.frame.bind(this);
    }
    async start() {
      const urls = [this.m.ground ? scene(this.d.id + '-roof') : null, this.m.air ? scene(this.d.id +
          '-air') : null, ...[0, 1, 2, 3].map(i => this.m.ground ? photo('walker-' + i) : null), this
        .m.air ? photo('aircraft') : null
      ];
      const [ground, sky, ...cast] = await Promise.all(urls.map(url => url ? image(url) : null));
      if (this.destroyed) return;
      this.groundImage = ground;
      this.skyImage = sky;
      this.sprites = cast.slice(0, 4);
      this.aircraft = cast[4];
      if (this.m.ground) {
        const extra = await Promise.all([photo('projector-view'), ...[0, 1, 2, 3].map(i => photo(
          'passing-' + i))].map(image));
        if (this.destroyed) return;
        this.projector = extra[0];
        this.passing = extra.slice(1);
      }
      this.resize();
      this.seedActors();
      this.bindControls();
      this.observer = new ResizeObserver(() => this.resize());
      this.observer.observe(this.canvas);
      this.ready = true;
      this.resume();
    }
    resize() {
      const box = this.canvas.getBoundingClientRect();
      if (!box.width || !box.height) return;
      const ratio = Math.min(2, window.devicePixelRatio || 1);
      this.w = box.width;
      this.h = box.height;
      this.canvas.width = Math.round(this.w * ratio);
      this.canvas.height = Math.round(this.h * ratio);
      this.ctx.setTransform(ratio, 0, 0, ratio, 0, 0);
      this.baseScale = Math.max(this.w / W, this.h / H);
      this.constrain();
      this.aim = {
        x: this.w / 2,
        y: this.h * .55
      };
      if (this.ready) this.draw();
    }
    get scale() {
      return this.baseScale * this.cam.zoom;
    }
    get goal() {
      return this.air ? this.m.air : this.m.ground;
    }
    constrain() {
      const hw = Math.min(W / 2, this.w / (2 * this.scale)),
        hh = Math.min(H / 2, this.h / (2 * this.scale));
      this.cam.x = clamp(this.cam.x, hw, W - hw);
      this.cam.y = clamp(this.cam.y, hh, H - hh);
    }
    toScreen(x, y) {
      return {
        x: (x - this.cam.x) * this.scale + this.w / 2,
        y: (y - this.cam.y) * this.scale + this.h / 2
      };
    }
    seedActors() {
      this.actors = [];
      this.phaseHits = 0;
      this.spawned = 0;
      this.lastFind = -1;
      if (!this.air) {
        for (let i = 0; i < this.m.crowd; i++) {
          const a = this.makeActor(false, i);
          a.x = 65 + (i + rand(.05, .65)) / this.m.crowd * (W - 130);
          this.actors.push(a);
        }
      }
      for (let i = 0; i < Math.min(this.m.active, this.goal); i++) this.spawnTarget(true);
    }
    makeActor(target, index) {
      const lane = this.d.lanes[(index + (target ? 1 : 0)) % this.d.lanes.length] + rand(-.014, .014);
      const y = this.air ? H * (.29 + (.41 * ((index * .59 + .13) % 1))) : H * lane;
      return {
        id: ++this.serial,
        target,
        x: rand(130, W - 130),
        y,
        baseY: y,
        direction: (index % 2 ? -1 : 1),
        speed: rand(28, 47) * this.m.pace * (this.air ? 1.3 : 1),
        phase: rand(0, 6.28),
        step: 0,
        sprite: target ? 3 : index % 3,
        size: this.air ? rand(68, 92) : 58 + (lane - .60) * 260,
        variant: index
      };
    }
    spawnTarget(initial) {
      const index = this.spawned++,
        a = this.makeActor(true, index);
      const spread = Math.min(W * .75, this.w / this.scale * .72);
      if (initial) a.x = clamp(this.cam.x + ((index + .5) / Math.min(this.m.active, this.goal) - .5) *
        spread, 85, W - 85);
      else {
        const factors = [-.38, .38, -.21, .21];
        a.x = clamp(this.cam.x + spread * factors[index % 4], 100, W - 100);
      }
      this.actors.push(a);
    }
    project(a) {
      const point = this.toScreen(a.x, a.y),
        size = a.size * this.scale;
      const sprite = this.air ? this.aircraft : this.sprites[a.sprite];
      const height = this.air ? size * sprite.height / sprite.width : size;
      const width = this.air ? size : height * sprite.width / sprite.height;
      const markerSize = clamp(height * .14, 8.5, 15);
      return {
        actor: a,
        x: point.x,
        y: point.y,
        height,
        width,
        markerSize,
        markY: point.y - height - markerSize - 7
      };
    }
    visible() {
      return this.actors.slice().sort((a, b) => a.y - b.y).map(a => this.project(a));
    }
    fire(point = this.aim, touch = false) {
      if (this.paused || this.done || this.cooldown > 0 || this.reloading > 0) return;
      if (!this.ammo) {
        this.reload();
        return;
      }
      const relay = this.relay && this.toScreen(this.relay.x, this.relay.y);
      if (relay && Math.hypot(point.x - relay.x, point.y - relay.y) <= (touch ? 30 : 22)) {
        this.run.relay = true;
        this.relay = null;
        this.ammo = this.cfg.capacity;
        this.aim = {
          ...point
        };
        this.cooldown = this.cfg.recovery;
        this.cb.sound('upgrade');
        this.cb.message('Gold relay +80 CR · cartridge refilled');
        this.hud();
        return;
      }
      const contact = resolveShot(this.visible(), point.x, point.y, this.cfg.radius, touch);
      this.aim = {
        ...point
      };
      const pos = {
        x: this.cam.x + (point.x - this.w / 2) / this.scale,
        y: this.cam.y + (point.y - this.h / 2) / this.scale
      };
      if (contact && !contact.target) {
        this.run.blocks++;
        this.chain = 0;
        this.cooldown = .25;
        this.ripple(pos, 'block');
        this.cb.sound('block');
        this.cb.message('Civilian protected · pulse blocked');
        this.hud();
        return;
      }
      this.run.shots++;
      this.ammo--;
      this.cooldown = this.cfg.recovery;
      this.kick = 1;
      this.ripple(pos, contact ? 'hit' : 'miss');
      if (contact) {
        this.actors = this.actors.filter(a => a !== contact);
        this.phaseHits++;
        this.run.hits++;
        this.chain++;
        this.run.bestChain = Math.max(this.run.bestChain, this.chain);
        this.cb.sound('hit');
        this.cb.message(this.chain >= 2 ? `${this.chain}× chain · signal cleared` : 'Signal cleared');
        if (this.phaseHits === this.goal) {
          if (this.m.air && !this.air) {
            this.waitingAir = true;
            this.pause();
            this.hud();
            this.cb.phase();
            return;
          }
          this.finish(true);
          return;
        }
        if (this.spawned < this.goal) this.spawnTarget(false);
        if (this.chain >= 2 && this.m.id > 0 && !this.relayOffered) {
          this.relayOffered = true;
          this.relay = {
            x: clamp(this.cam.x + .16 * this.w / this.scale, 80, W - 80),
            y: clamp(this.cam.y - .14 * this.h / this.scale, 100, H - 100),
            ttl: 8
          };
          this.cb.message('Gold relay online · tap for +80 CR and a refill');
        }
      } else {
        this.chain = 0;
        this.cb.sound('miss');
        this.cb.message('No signal · chain reset');
      }
      if (!this.ammo) this.reload();
      this.hud();
    }
    ripple(pos, kind) {
      this.effects.push({
        ...pos,
        kind,
        t: 0
      });
    }
    reload() {
      if (this.paused || this.reloading > 0 || this.ammo === this.cfg.capacity) return;
      this.reloading = this.cfg.reload;
      this.cb.sound('reload');
      this.cb.message('Charging cartridge…');
      this.hud();
    }
    lens() {
      if (this.paused) return;
      const wx = this.cam.x + (this.aim.x - this.w / 2) / this.scale,
        wy = this.cam.y + (this.aim.y - this.h / 2) / this.scale;
      this.scoped = !this.scoped;
      this.cam.zoom = (this.air ? 1.3 : 1) * (this.scoped ? this.cfg.zoom : 1);
      this.cam.x = wx - (this.aim.x - this.w / 2) / this.scale;
      this.cam.y = wy - (this.aim.y - this.h / 2) / this.scale;
      this.constrain();
      this.cb.sound('focus');
      this.hud();
      this.draw();
    }
    find() {
      if (this.paused) return;
      const live = this.actors.filter(a => a.target).sort((a, b) => a.id - b.id);
      const a = live.find(item => item.id > this.lastFind) || live[0];
      if (!a) return;
      this.lastFind = a.id;
      this.cam.x = a.x;
      this.cam.y = a.y - a.size * .65;
      this.constrain();
      const p = this.project(a);
      this.aim = {
        x: p.x,
        y: p.markY
      };
      this.cb.sound('focus');
      this.draw();
    }
    continueAir() {
      if (!this.waitingAir || this.destroyed) return;
      this.waitingAir = false;
      this.air = true;
      this.scoped = false;
      this.cam = {
        x: W / 2,
        y: H * .48,
        zoom: 1.3
      };
      this.constrain();
      this.seedActors();
      this.ammo = this.cfg.capacity;
      this.reloading = 0;
      this.cooldown = 0;
      this.effects = [];
      this.relay = null;
      this.cb.sound('drone');
      this.resume();
    }
    enterMove(dx, dy, dt) {
      if (this.air) {
        this.cam.x += dx * dt * 480 / this.scale;
        this.cam.y += dy * dt * 480 / this.scale;
        this.constrain();
      } else {
        this.aim.x = clamp(this.aim.x + dx * dt * 360, 8, this.w - 8);
        this.aim.y = clamp(this.aim.y + dy * dt * 360, 8, this.h - 8);
        if (this.aim.x < 20 || this.aim.x > this.w - 20) this.cam.x += dx * dt * 450 / this.scale;
        if (this.aim.y < 20 || this.aim.y > this.h - 20) this.cam.y += dy * dt * 450 / this.scale;
        this.constrain();
      }
    }
    bindControls() {
      const on = (el, event, fn) => el.addEventListener(event, fn, {
        signal: this.events.signal
      });
      const location = e => {
        const r = this.canvas.getBoundingClientRect();
        return {
          x: e.clientX - r.left,
          y: e.clientY - r.top
        };
      };
      on(this.canvas, 'pointerdown', e => {
        if (this.paused || e.button !== 0 || this.pointer) return;
        const p = location(e);
        this.pointer = {
          id: e.pointerId,
          origin: p,
          last: p,
          moved: false
        };
        this.aim = p;
        this.canvas.focus({
          preventScroll: true
        });
        this.canvas.setPointerCapture(e.pointerId);
      });
      on(this.canvas, 'pointermove', e => {
        if (this.paused) return;
        const p = location(e),
          hold = this.pointer;
        if (hold && hold.id === e.pointerId) {
          if (!hold.moved && Math.hypot(p.x - hold.origin.x, p.y - hold.origin.y) > 6) hold.moved =
            true;
          if (hold.moved) {
            this.cam.x -= (p.x - hold.last.x) / this.scale;
            this.cam.y -= (p.y - hold.last.y) / this.scale;
            this.constrain();
          }
          hold.last = p;
          this.aim = p;
        } else if (!hold && e.pointerType === 'mouse') this.aim = p;
      });
      on(this.canvas, 'pointerup', e => {
        const hold = this.pointer;
        if (!hold || hold.id !== e.pointerId) return;
        this.pointer = null;
        if (!hold.moved) this.fire(location(e), e.pointerType !== 'mouse');
      });
      on(this.canvas, 'pointercancel', () => {
        this.pointer = null;
      });
      on(this.canvas, 'lostpointercapture', () => {
        this.pointer = null;
      });
      on(this.canvas, 'contextmenu', e => e.preventDefault());
      on(window, 'keydown', e => {
        if (this.paused || e.altKey || e.ctrlKey || e.metaKey) return;
        const k = e.key.toLowerCase(),
          movement = ['arrowleft', 'a', 'arrowright', 'd', 'arrowup', 'w', 'arrowdown', 's'];
        if (k === 'escape' || k === 'p') {
          e.preventDefault();
          if (!e.repeat) this.cb.pause();
          return;
        }
        if (movement.includes(k)) {
          e.preventDefault();
          this.keys.add(k);
          return;
        }
        if (k === ' ' && e.target.closest?.('button')) return;
        if (![' ', 'f', 'z', 'r'].includes(k)) return;
        e.preventDefault();
        if (e.repeat) return;
        if (k === ' ') this.fire();
        if (k === 'f') this.find();
        if (k === 'z') this.lens();
        if (k === 'r') this.reload();
      });
      on(window, 'keyup', e => this.keys.delete(e.key.toLowerCase()));
      on(document, 'visibilitychange', () => {
        if (document.hidden && !this.paused && !this.done) this.cb.pause();
      });
      on(window, 'blur', () => {
        this.keys.clear();
        if (!this.paused && !this.done) this.cb.pause();
      });
    }
    pause() {
      this.paused = true;
      this.keys.clear();
      this.pointer = null;
      cancelAnimationFrame(this.raf);
    }
    resume() {
      if (!this.ready || this.destroyed || this.done || this.waitingAir) return;
      cancelAnimationFrame(this.raf);
      this.paused = false;
      this.prev = performance.now();
      this.hud(true);
      this.draw();
      this.raf = requestAnimationFrame(this.frame);
    }
    finish(success) {
      if (this.done) return;
      this.done = true;
      this.pause();
      this.draw();
      this.hud(true);
      this.cb.finish(success, this.run);
    }
    destroy() {
      this.destroyed = true;
      this.pause();
      this.events.abort();
      this.observer?.disconnect();
    }
    frame(now) {
      if (this.paused || this.destroyed) return;
      const realDt = Math.max(0, (now - this.prev) / 1000),
        dt = Math.min(.10, realDt);
      this.prev = now;
      this.time += dt;
      this.run.elapsed += realDt;
      if (this.run.elapsed >= this.m.seconds) {
        this.finish(false);
        return;
      }
      this.cooldown = Math.max(0, this.cooldown - realDt);
      this.kick = Math.max(0, this.kick - realDt * 5);
      if (this.relay && (this.relay.ttl -= realDt) <= 0) this.relay = null;
      if (this.reloading > 0) {
        this.reloading = Math.max(0, this.reloading - realDt);
        if (!this.reloading) {
          this.ammo = this.cfg.capacity;
          this.cb.message('Cartridge ready');
        }
      }
      if (this.keys.size) {
        const has = (a, b) => Number(this.keys.has(a) || this.keys.has(b));
        this.enterMove(has('d', 'arrowright') - has('a', 'arrowleft'), has('s', 'arrowdown') - has('w',
          'arrowup'), dt);
      }
      for (const a of this.actors) {
        const walking = this.air || this.m.id !== 1 || Math.sin(this.time * .9 + a.phase) > -.83;
        if (walking) {
          a.x += a.speed * a.direction * dt;
          a.step += dt * a.speed * .115;
        }
        if (a.x > W - 62 || a.x < 62) {
          a.x = clamp(a.x, 62, W - 62);
          a.direction *= -1;
        }
        if (this.air) a.y = a.baseY + Math.sin(this.time * .68 + a.phase) * 48 + Math.sin(this.time *
          .29 + a.phase) * 18;
        else a.y = a.baseY + (this.d.id === 'meridian' ? (0.5 - a.x / W) * 60 : 0);
      }
      this.effects = this.effects.filter(e => (e.t += dt) < .38);
      this.draw();
      this.hud();
      this.raf = requestAnimationFrame(this.frame);
    }
    hud(force = false) {
      const data = {
        air: this.air,
        hits: this.run.hits,
        goal: this.m.ground + this.m.air,
        seconds: Math.max(0, Math.ceil(this.m.seconds - this.run.elapsed)),
        ammo: this.ammo,
        capacity: this.cfg.capacity,
        chain: this.chain,
        reloading: this.reloading > 0,
        zoom: this.scoped,
        mag: this.cfg.zoom
      };
      const key = JSON.stringify(data);
      if (force || key !== this.lastHud) {
        this.lastHud = key;
        this.cb.hud(data);
      }
    }
    drawWalker(p) {
      const c = this.ctx,
        a = p.actor,
        h = p.height,
        w = p.width;
      const passing = !this.reduced && Math.sin(a.step + a.phase) > .48;
      const im = (passing ? this.passing : this.sprites)[a.sprite],
        visibleWidth = h * im.width / im.height;
      const swing = Math.sin(a.step + a.phase),
        bob = this.reduced ? 0 : Math.abs(swing) * h * .015;
      c.save();
      c.fillStyle = this.d.id === 'vesper' ? 'rgba(5,11,23,.5)' : 'rgba(31,25,20,.34)';
      c.beginPath();
      c.ellipse(p.x - w * .12, p.y + 1, w * .54, h * .066, -.09, 0, Math.PI * 2);
      c.fill();
      c.translate(p.x, p.y - bob);
      c.scale(a.direction, 1);
      const split = Math.floor(im.height * .58),
        torso = h * split / im.height,
        leg = h - torso;
      c.drawImage(im, 0, 0, im.width, split, -visibleWidth / 2, -h, visibleWidth, torso + .4);
      const stride = this.reduced ? 1 : .86 + Math.abs(swing) * .16;
      c.drawImage(im, 0, split, im.width, im.height - split, -visibleWidth * stride / 2, -leg,
        visibleWidth * stride, leg + bob);
      c.restore();
    }
    drawDrone(p) {
      const c = this.ctx;
      c.save();
      c.translate(p.x, p.y - p.height / 2);
      c.rotate(this.reduced ? 0 : Math.sin(this.time * 2 + p.actor.phase) * .07);
      c.shadowColor = 'rgba(5,10,12,.9)';
      c.shadowBlur = 4;
      c.shadowOffsetY = 3;
      c.drawImage(this.aircraft, -p.width / 2, -p.height / 2, p.width, p.height);
      c.shadowBlur = 0;
      c.shadowOffsetY = 0;
      c.fillStyle = Math.sin(this.time * 8 + p.actor.phase) > .1 ? '#ff543d' : '#9e271c';
      c.beginPath();
      c.arc(0, 0, Math.max(2, p.width * .033), 0, Math.PI * 2);
      c.fill();
      c.restore();
    }
    drawMarker(p) {
      const c = this.ctx,
        s = p.markerSize;
      c.save();
      c.translate(p.x, p.markY);
      c.shadowColor = '#25120f';
      c.shadowBlur = 4;
      c.beginPath();
      c.moveTo(0, -s);
      c.lineTo(s, 0);
      c.lineTo(0, s);
      c.lineTo(-s, 0);
      c.closePath();
      c.fillStyle = '#f64d37';
      c.fill();
      c.shadowBlur = 0;
      c.strokeStyle = '#fff3db';
      c.lineWidth = 1.3;
      c.stroke();
      c.fillStyle = '#fff8e8';
      c.fillRect(-1.3, -s * .42, 2.6, s * .57);
      c.fillRect(-1.3, s * .24, 2.6, 2.2);
      c.restore();
    }
    drawRelay() {
      const c = this.ctx,
        p = this.toScreen(this.relay.x, this.relay.y);
      c.save();
      c.translate(p.x, p.y);
      c.strokeStyle = '#ffdb7b';
      c.fillStyle = '#523b18';
      c.lineWidth = 2;
      c.beginPath();
      for (let i = 0; i < 6; i++) {
        const a = i * Math.PI / 3;
        const x = Math.sin(a) * 13,
          y = Math.cos(a) * 13;
        if (i) c.lineTo(x, y);
        else c.moveTo(x, y);
      }
      c.closePath();
      c.fill();
      c.stroke();
      c.fillStyle = '#ffdf79';
      c.beginPath();
      c.moveTo(2, -8);
      c.lineTo(-5, 1);
      c.lineTo(0, 1);
      c.lineTo(-2, 8);
      c.lineTo(6, -2);
      c.lineTo(1, -2);
      c.fill();
      c.beginPath();
      c.arc(0, 0, 18, -Math.PI / 2, -Math.PI / 2 + Math.PI * 2 * this.relay.ttl / 8);
      c.stroke();
      c.restore();
    }
    drawProjector() {
      const c = this.ctx,
        im = this.projector;
      const w = Math.min(this.w * .31, this.h * .50, 370),
        h = w * im.height / im.width;
      const aimX = (this.aim.x / this.w - .5) * 10,
        aimY = (this.aim.y / this.h - .5) * 5;
      const kick = this.reduced ? 0 : this.kick;
      const dip = this.reloading ? Math.sin(Math.PI * (this.reloading / this.cfg.reload)) * h * .30 : 0;
      c.drawImage(im, this.w - w * .85 + aimX + kick * 5, this.h - h * .80 + aimY + kick * 8 + dip, w,
        h);
    }
    drawSight() {
      const c = this.ctx,
        x = this.aim.x,
        y = this.aim.y,
        air = this.air;
      c.save();
      c.translate(x, y);
      c.strokeStyle = air ? '#c7fff4' : '#fff3d7';
      c.lineWidth = 1.4;
      c.shadowColor = '#09100f';
      c.shadowBlur = 3;
      for (let i = 0; i < 4; i++) {
        c.beginPath();
        c.moveTo(0, -7);
        c.lineTo(0, -17);
        c.stroke();
        c.rotate(Math.PI / 2);
      }
      c.beginPath();
      c.arc(0, 0, 3.1, 0, Math.PI * 2);
      c.stroke();
      c.restore();
      if (this.scoped) {
        const r = Math.min(this.w * .43, this.h * .44),
          cx = this.w / 2,
          cy = this.h / 2;
        c.save();
        c.fillStyle = 'rgba(8,14,17,.46)';
        c.beginPath();
        c.rect(0, 0, this.w, this.h);
        c.arc(cx, cy, r, 0, Math.PI * 2, true);
        c.fill('evenodd');
        c.strokeStyle = 'rgba(240,233,204,.45)';
        c.lineWidth = 1;
        c.beginPath();
        c.arc(cx, cy, r, 0, Math.PI * 2);
        c.stroke();
        c.restore();
      }
    }
    draw() {
      if (!this.ready || !this.w) return;
      const c = this.ctx,
        p = this.toScreen(0, 0),
        im = this.air ? this.skyImage : this.groundImage;
      c.drawImage(im, p.x, p.y, W * this.scale, H * this.scale);
      const items = this.visible();
      for (const entry of items) {
        if (entry.x < -entry.width || entry.x > this.w + entry.width) continue;
        this.air ? this.drawDrone(entry) : this.drawWalker(entry);
      }
      for (const entry of items)
        if (entry.actor.target) this.drawMarker(entry);
      if (!this.reduced && this.d.id === 'vesper' && !this.air) {
        c.save();
        c.strokeStyle = 'rgba(183,224,246,.22)';
        c.lineWidth = 1;
        c.beginPath();
        for (const [a, b] of this.weather) {
          const y = ((b + this.time * .50) % 1) * this.h,
            x = ((a + this.time * .026) % 1) * this.w;
          c.moveTo(x, y);
          c.lineTo(x - 2, y + 8);
        }
        c.stroke();
        c.restore();
      }
      for (const e of this.effects) {
        const v = this.toScreen(e.x, e.y),
          k = e.t / .38;
        c.save();
        c.globalAlpha = 1 - k;
        c.strokeStyle = e.kind === 'hit' ? '#ffd277' : e.kind === 'block' ? '#82f1db' : '#f3f0df';
        c.lineWidth = 2;
        c.beginPath();
        c.arc(v.x, v.y, 7 + 30 * k, 0, Math.PI * 2);
        c.stroke();
        c.restore();
      }
      if (this.relay) this.drawRelay();
      if (!this.air && !this.scoped) this.drawProjector();
      this.drawSight();
      const live = items.filter(v => v.actor.target);
      if (live.length && live.every(v => v.x < 8 || v.x > this.w - 8 || v.markY < 10 || v.y > this.h + v
          .height)) {
        c.save();
        c.fillStyle = 'rgba(13,20,23,.88)';
        const bw = Math.min(this.w - 20, 202);
        c.fillRect((this.w - bw) / 2, this.h - 41, bw, 28);
        c.textAlign = 'center';
        c.fillStyle = '#fff0df';
        c.font = '12px sans-serif';
        c.fillText('Signal off screen · use Find', (this.w) / 2, this.h - 23);
        c.restore();
      }
    }
  }
  globalThis.SignalField = {
    Field,
    resolveShot
  };
})();
