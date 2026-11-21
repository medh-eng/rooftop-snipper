(() => {
  'use strict';
  const C = RedSignal,
    U = SignalUI,
    {
      icon,
      padded,
      format,
      scene,
      photo
    } = U;
  const $ = id => document.getElementById(id),
    main = $('main'),
    dialog = $('dialog');
  const p = C.readSave();
  let selected = Math.min(C.completed(p), C.missions.length - 1),
    page = 'operations',
    field = null,
    modalKind = '',
    toastTimer = 0,
    fieldTimer = 0,
    loading = false;
  let saved = true;
  const m = () => C.missions[selected];
  const btn = (action, name, symbol = 'arrow', style = 'primary') =>
    `<button class="btn ${style}" data-action="${action}">${symbol ? icon(symbol) : ''}<span>${name}</span></button>`;
  const money = n => `${icon('coin')} ${format(n)} <small>CR</small>`;
  const time = s => `${Math.floor(s/60)}:${padded(s%60)}`;
  const rank = () => {
    const n = C.completed(p);
    return n === 9 ? 'City guardian' : n >= 6 ? 'Flight specialist' : n >= 3 ? 'Signal tracker' :
      'Rooftop recruit';
  };
  const signal = enabled => U.tone(enabled, p.sound);

  function flash(message) {
    const el = $('toast');
    el.textContent = message;
    el.classList.add('show');
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => el.classList.remove('show'), 2300);
  }

  function persist() {
    saved = C.writeSave(p);
    $('footer').firstElementChild.innerHTML =
      `<i class="status-dot"></i> ${saved?'LOCAL SAVE':'SESSION SAVE'}`;
    refreshBar();
  }
  async function transaction(change) {
    function update() {
      if (saved) Object.assign(p, C.readSave());
      const result = change();
      if (result !== false && result !== null) persist();
      return result;
    }
    if (navigator.locks?.request) return navigator.locks.request('red-signal-profile', update);
    return update();
  }
  window.addEventListener('storage', e => {
    if (e.key !== 'rooftop-red-signal-v1') return;
    Object.assign(p, C.readSave());
    refreshBar();
    if (page === 'career') renderCareer();
    else if (page === 'armory') renderArmory();
    else if (page === 'operations') renderOperations();
  });

  function refreshBar() {
    $('balance').textContent = format(p.credits);
    const audio = $('audio');
    audio.innerHTML = icon(p.sound ? 'sound' : 'mute');
    audio.setAttribute('aria-label', p.sound ? 'Mute sound' : 'Enable sound');
    audio.title = audio.getAttribute('aria-label');
    audio.setAttribute('aria-pressed', String(p.sound));
    document.querySelectorAll('[data-tab]').forEach(b => {
      if (b.dataset.tab === page) b.setAttribute('aria-current', 'page');
      else b.removeAttribute('aria-current');
    });
  }

  function stars(n) {
    return `<span class="stars" aria-label="${n} of 3 stars">${[1,2,3].map(i=>`<span class="${i<=n?'lit':''}">${icon('star')}</span>`).join('')}</span>`;
  }

  function progress() {
    return `<div class="progress"><span class="micro"><b>${padded(C.completed(p))}</b> / 09 CLEARED</span><span class="ticks" aria-hidden="true">${C.missions.map(a=>`<i class="${p.best[a.id]?'filled':''}"></i>`).join('')}</span></div>`;
  }

  function closeDialog() {
    if (dialog.open) dialog.close();
    modalKind = '';
  }

  function showDialog(kind, content, closable = true) {
    modalKind = kind;
    $('dialog-inner').innerHTML =
      `${closable?`<button class="dialog-x utility" data-action="close" aria-label="Close">${icon('close')}</button>`:''}${content}`;
    if (!dialog.open) dialog.showModal();
    const focus = dialog.querySelector('[data-default]') || dialog.querySelector('button');
    focus?.focus({
      preventScroll: true
    });
  }

  function go(dest, focus = false) {
    closeDialog();
    if (field) {
      field.destroy();
      field = null;
    }
    loading = false;
    clearTimeout(fieldTimer);
    page = dest;
    document.body.classList.remove('in-game');
    main.className = 'shell';
    refreshBar();
    if (dest === 'armory') renderArmory();
    else if (dest === 'career') renderCareer();
    else renderOperations();
    window.scrollTo(0, 0);
    if (focus) main.focus({
      preventScroll: true
    });
  }

  function districtCard(d, i) {
    const open = C.available(p, i * 3),
      count = C.missions.filter(a => a.district === i && p.best[a.id]).length;
    const status = count === 3 ? 'Complete' : open ? 'Open' : 'Locked';
    return `<button class="district-card ${m().district===i?'selected':''}" data-action="district" data-id="${i}" aria-label="${d.name}, ${status}, ${count} of 3 contracts complete"><img src="${scene(d.id)}" alt="" decoding="async"><span class="card-shade"></span><span class="card-top"><span class="card-id">DISTRICT ${padded(i+1)}</span><span class="card-state ${open?'available':''}">${open?'<i class="status-dot"></i>':icon('lock')}${status}</span></span><span class="card-base"><span><span class="card-sub">${d.subtitle}</span><strong>${d.name}</strong></span><span class="card-arrow">${icon('arrow')}</span></span></button>`;
  }

  function renderOperations() {
    const contract = m(),
      d = C.districts[contract.district],
      split = contract.name.lastIndexOf(' '),
      hero = contract.air && !contract.ground ? d.id + '-air' : d.cover;
    main.innerHTML =
      `<div class="section-head top-head"><h2>Mission control</h2>${progress()}</div>
    <section class="hero" aria-labelledby="mission-name"><img class="hero-art" src="${scene(hero)}" fetchpriority="high" alt="${hero==='harbor-cover'?'A rooftop operator overlooks the cranes and water of Port Meridian at dawn':d.name+' city panorama'}"><div class="hero-shade"></div>
    <div class="feed"><i></i>${contract.ground?'ROOFTOP':'AERIAL'} FEED</div><div class="focus-mark" aria-hidden="true"><i></i><i></i><i></i><i></i><b>+</b></div>
    <div class="hero-copy"><div class="eyeline"><span class="active-tag">${p.best[contract.id]?'REPLAY CONTRACT':'ACTIVE CONTRACT'}</span><span class="op-code">OP–${padded(contract.id+1)}</span></div><h1 id="mission-name">${contract.name.slice(0,split)}<br>${contract.name.slice(split+1)}</h1>
    <div class="hero-meta"><span>${icon('pin')}${d.name}</span><i></i><span>${icon('sun')}${d.time} <span class="local">local</span></span></div><p>${contract.brief}</p>
    <div class="hero-actions">${btn('deploy','Deploy mission','play')}${btn('intel','Mission intel','info','glass')}</div></div>
    <div class="hero-prize"><span class="micro">CONTRACT BOUNTY</span><b>${money(p.best[contract.id]?Math.round(contract.base/2):contract.base)}</b></div></section>
    <section class="districts"><div class="section-head"><h2>Campaign districts</h2><span class="section-side">${padded(C.districts.filter((_,i)=>C.available(p,i*3)).length)} / 03 OPEN</span></div><div class="district-grid">${C.districts.map(districtCard).join('')}</div></section>`;
  }

  function districtDialog(id) {
    const d = C.districts[id],
      group = C.missions.filter(v => v.district === id);
    showDialog('district',
      `<div class="dialog-photo"><img src="${scene(d.id)}" alt=""><div><span class="micro">DISTRICT ${padded(id+1)} / ${d.light.toUpperCase()}</span><h2 id="dialog-title">${d.name}</h2></div></div><div class="dialog-body"><div class="contract-list">${group.map(a=>{
      const open=C.available(p,a.id),r=p.best[a.id];return `<button class="contract-row" ${open?`data-action="select" data-id="${a.id}"`:'disabled'}><span class="row-number">${padded(a.id+1)}</span><span><strong>${a.name}</strong><small>${C.mode(a)} · ${a.ground+a.air} signals</small></span><span class="row-end">${r?stars(r.stars):open?icon('arrow'):icon('lock')}</span></button>`;}).join('')}</div><p class="dialog-note">${C.available(p,id*3)?`Clear all three contracts to earn the ${C.cache} CR district cache.`:`Clear ${C.missions[id*3-1].name} to open this district.`}</p></div>`
      );
  }

  function intel() {
    const a = m();
    showDialog('intel',
      `<div class="dialog-pad"><span class="eyebrow">CONTRACT ${padded(a.id+1)} / MISSION INTEL</span><h2 id="dialog-title">${a.name}</h2><p>${a.brief}</p><div class="intel-grid"><span>${icon('target')}<small>Marked signals</small><b>${a.ground+a.air}</b></span><span>${icon('time')}<small>Time limit</small><b>${time(a.seconds)}</b></span><span>${icon(a.air?'drone':'cross')}<small>View</small><b>${C.mode(a)}</b></span></div><div class="note-block"><b>Controls</b><p>Tap a red diamond to fire. Drag to look around. Use Find to centre a mark, Lens to zoom and Reload to refill.</p>${a.air?'<p>In the drone feed, drag or use the arrow keys to fly the camera over the rooftops.</p>':''}</div><div class="note-block"><b>Three-star clear</b><p>${a.ground?'Finish the contract, land at least 80% of your shots and keep every civilian contact clear.':'Earn a star for completion, another for 80% precision and the third for a perfect intercept.'}</p>${a.id>0?'<p>A two-hit chain opens a gold relay for eight seconds. Tap it for a refill and 80 extra credits on completion.</p>':''}</div>${btn('deploy','Deploy mission','play')}</div>`
      );
  }

  function renderArmory() {
    main.innerHTML =
      `<div class="page-title"><span class="eyebrow">OPERATOR EQUIPMENT</span><h1>Armory</h1><p>Spend contract credits on permanent upgrades. Every purchase equips immediately.</p></div><div class="kit-grid">${C.kits.map(k=>{
      const tier=p.upgrades[k.id],cost=k.costs[tier],max=tier===2,afford=p.credits>=cost;
      return `<article class="kit-card"><div class="kit-photo"><img src="${photo(k.image)}" alt="${k.name}"><span class="kit-tier">TIER ${['I','II','III'][tier]} / III</span></div><div class="kit-info"><span class="eyebrow">${k.type}</span><h2>${k.name}</h2><p>${k.detail}</p><div class="kit-stat"><span>${k.stat}</span><strong>${k.values[tier]}${max?'':` ${icon('arrow')} <em>${k.values[tier+1]}</em>`}</strong></div><button class="buy ${!max&&afford?'can-buy':''}" data-action="buy" data-id="${k.id}" ${max||!afford?'disabled':''}>${max?icon('check')+' Fully upgraded':afford?`<span>Upgrade</span><b>${money(cost)}</b>`:`<span>Need ${format(cost-p.credits)} more CR</span><b>${money(cost)}</b>`}</button></div></article>`;}).join('')}</div>`;
  }

  function renderCareer() {
    const n = C.completed(p),
      totalStars = Object.values(p.best).reduce((s, r) => s + r.stars, 0);
    const metrics = [
      [icon('flag'), 'Contracts cleared', `${n} / 9`],
      [icon('star'), 'Contract stars', `${totalStars} / 27`],
      [icon('coin'), 'Credits earned', format(p.earned)],
      [icon('bolt'), 'Best chain', `${p.bestChain}×`]
    ];
    main.innerHTML =
      `<div class="page-title"><span class="eyebrow">OPERATOR RECORD</span><h1>Career</h1><p>${rank()} · ${n===9?'All three districts secured. Replay contracts to improve your records.':'Complete contracts to build your record and unlock new districts.'}</p></div><div class="metrics">${metrics.map(([ic,l,v])=>`<div>${ic}<span>${l}</span><b>${v}</b></div>`).join('')}</div><div class="section-head"><h2>Contract record</h2><span class="section-side">PERSONAL BESTS</span></div><div class="records">${C.missions.map(a=>{const r=p.best[a.id],open=C.available(p,a.id);return `<button class="record-row" ${open?`data-action="select" data-id="${a.id}"`:'disabled'}><span class="row-number">${padded(a.id+1)}</span><span class="record-name"><strong>${a.name}</strong><small>${C.districts[a.district].name} · ${C.mode(a)}</small></span><span class="record-stats">${r?`${r.precision}% · ${time(r.seconds)}`:open?'Available':'Locked'}</span>${r?stars(r.stars):icon(open?'arrow':'lock')}</button>`;}).join('')}</div>`;
  }

  function message(text) {
    const el = $('field-message');
    if (!el) return;
    el.textContent = text;
    el.classList.add('show');
    clearTimeout(fieldTimer);
    fieldTimer = setTimeout(() => el.classList.remove('show'), 1600);
  }

  function hud(v) {
    if (!$('hud-count')) return;
    $('hud-count').textContent = `${v.hits} / ${v.goal}`;
    $('hud-time').textContent = time(v.seconds);
    $('hud-time').classList.toggle('low', v.seconds <= 15);
    $('hud-ammo').textContent = v.reloading ? '…' : `${v.ammo} / ${v.capacity}`;
    $('hud-chain').textContent = `${v.chain}×`;
    const l = $('lens-control');
    l.setAttribute('aria-pressed', String(v.zoom));
    l.querySelector('span').textContent = v.zoom ? `${v.mag}× lens` : 'Lens';
    const rr = $('reload-control');
    rr.querySelector('span').textContent = v.reloading ? 'Charging' : 'Reload';
    rr.disabled = v.reloading || v.ammo === v.capacity;
    $('feed-name').textContent = v.air ? 'DRONE UPLINK' : 'ROOFTOP CAM';
    $('field-stage').classList.toggle('aerial', v.air);
  }
  const control = (action, label, ic, key, id = '') =>
    `<button ${id?`id="${id}"`:''} class="field-control" data-action="${action}" aria-label="${label} (${key})">${icon(ic)}<span>${label}</span><kbd>${key}</kbd></button>`;
  async function deploy() {
    if (loading || !C.available(p, selected)) return;
    closeDialog();
    if (field) field.destroy();
    loading = true;
    page = 'game';
    const a = m(),
      d = C.districts[a.district];
    document.body.classList.add('in-game');
    main.className = 'game-shell';
    window.scrollTo(0, 0);
    main.innerHTML =
      `<section class="game-layout"><header class="field-head"><div class="field-name"><span class="eyebrow">${d.name.toUpperCase()} / OP–${padded(a.id+1)}</span><h1>${a.name}</h1></div><div class="field-top-tools"><button class="utility" data-action="audio" aria-label="Toggle sound">${icon(p.sound?'sound':'mute')}</button><button class="field-pause" data-action="pause" aria-label="Pause mission">${icon('pause')}<span>Pause</span></button></div></header><div class="field-stage" id="field-stage"><canvas tabindex="0" id="field" aria-label="Mission field. Tap a red diamond, or press F to find a signal and Space to fire."></canvas><div class="loading-scene" id="loading-scene">Linking city feed…</div><div class="field-overlay"><span class="live-chip"><i></i><b id="feed-name">CITY FEED</b></span><span class="scene-time">${icon('time')}<b id="hud-time">${time(a.seconds)}</b></span></div><div class="frame-corners" aria-hidden="true"><i></i><i></i><i></i><i></i></div><div class="field-notice" id="field-message" role="status"></div></div><div class="game-bar"><div class="game-stats"><div>${icon('target')}<span><small>Signals</small><b id="hud-count">0 / ${a.ground+a.air}</b></span></div><div>${icon('cell')}<span><small>Charge</small><b id="hud-ammo">–</b></span></div><div>${icon('bolt')}<span><small>Chain</small><b id="hud-chain">0×</b></span></div></div><div class="field-controls">${control('find','Find','focus','F')}${control('lens','Lens','lens','Z','lens-control')}${control('reload','Reload','reload','R','reload-control')}</div></div><div class="play-hint">${icon('info')}<span>Tap a red diamond to fire. Drag to look. Use Find to centre the next signal.</span></div></section>`;
    const current = new SignalField.Field($('field'), a, C.stats(p), {
      sound: signal,
      hud,
      message,
      pause: pauseGame,
      phase: airDialog,
      finish: finishGame
    });
    field = current;
    try {
      await current.start();
      if (field !== current) return;
      loading = false;
      $('loading-scene')?.remove();
      $('field').focus({
        preventScroll: true
      });
      if (document.hidden) pauseGame();
    } catch {
      if (field === current) {
        loading = false;
        current.destroy();
        showDialog('error',
          `<div class="dialog-pad"><span class="eyebrow">CITY FEED</span><h2 id="dialog-title">Feed unavailable</h2><p>The city artwork could not be loaded. Retry the connection or return to mission control.</p><div class="dialog-actions">${btn('deploy','Retry','reload')}${btn('operations','Mission control','grid','secondary')}</div></div>`,
          false);
      }
    }
  }

  function pauseGame() {
    if (!field?.ready || field.done || field.waitingAir || dialog.open) return;
    field.pause();
    showDialog('pause',
      `<div class="dialog-pad"><span class="eyebrow">OP–${padded(selected+1)} / PAUSED</span><h2 id="dialog-title">Signal on hold</h2><p>Your mission timer is paused.</p><div class="note-block"><b>Controls</b><p>Tap or click a red diamond to fire. Drag to pan, or use F to centre a mark and Space to fire. Z toggles the lens and R reloads.</p></div><div class="dialog-actions">${btn('resume','Resume mission','play')}${btn('operations','Leave mission','arrow','secondary')}</div></div>`,
      false);
  }

  function resume() {
    closeDialog();
    field?.resume();
    $('field')?.focus({
      preventScroll: true
    });
  }

  function airDialog() {
    showDialog('phase',
      `<div class="dialog-photo"><img src="${scene(C.districts[m().district].id+'-air')}" alt="Aerial view of the city"><div><span class="micro">GROUND PHASE CLEAR</span><h2 id="dialog-title">Drone uplink ready</h2></div></div><div class="dialog-body"><p>Take the overhead camera and disable the four rogue drones. Drag to fly, then tap their red signals.</p>${btn('uplink','Launch drone','drone')}</div>`,
      false);
  }
  async function finishGame(success, run) {
    const a = m();
    clearTimeout(fieldTimer);
    if (!success) {
      signal('block');
      showDialog('result',
        `<div class="dialog-pad"><span class="eyebrow">CONTRACT ${padded(a.id+1)}</span><h2 id="dialog-title">Signal lost</h2><p>Time expired with ${run.hits} of ${a.ground+a.air} marks cleared. Use Find to bring distant marks back into view.</p><div class="dialog-actions">${btn('deploy','Retry contract','reload')}${btn('operations','Mission control','grid','secondary')}</div></div>`,
        false);
      return;
    }
    const r = await transaction(() => C.settle(p, a, run));
    if (!r) return;
    signal('win');
    let unlock = '';
    if (r.first && a.id === 4) unlock = 'Drone uplink unlocked for Ghost Frequency.';
    else if (r.first && a.id === 8) unlock = 'Campaign complete. All districts are open for replay.';
    else if (r.first && a.id % 3 === 2) unlock = `${C.districts[a.district+1].name} is now open.`;
    showDialog('result',
      `<div class="dialog-pad results"><span class="eyebrow">OP–${padded(a.id+1)} / ${a.name.toUpperCase()}</span><h2 id="dialog-title">Contract clear</h2><div class="result-stars">${stars(r.stars)}</div><div class="reward-total"><span>Total payout</span><b>${money(r.total)}</b></div><div class="reward-list">${r.rewards.filter(v=>v[1]).map(([l,v])=>`<span>${l}<b>+${v}</b></span>`).join('')}</div><div class="run-line">${r.precision}% precision <i></i> ${time(r.seconds)} <i></i> ${run.bestChain}× best chain</div>${unlock?`<p class="unlock">${icon('flag')}${unlock}</p>`:''}<div class="dialog-actions">${a.id<8?btn('next','Next contract','arrow'):btn('career','View career','medal')}${btn('armory','Armory','case','secondary')}${btn('operations','Mission control','grid','text')}</div></div>`,
      false);
  }
  async function purchasing(id) {
    const shown = p.upgrades[id];
    if (!await transaction(() => p.upgrades[id] === shown && C.buy(p, id))) return;
    signal('upgrade');
    renderArmory();
    const k = C.kits.find(a => a.id === id);
    flash(`${k.name} upgraded · ${k.labels[p.upgrades[id]]} equipped`);
    main.querySelector(`[data-id="${id}"]:not(:disabled)`)?.focus({
      preventScroll: true
    });
  }
  document.addEventListener('click', async e => {
    const b = e.target.closest('button[data-action]');
    if (!b || b.disabled) return;
    const action = b.dataset.action;
    switch (action) {
      case 'operations':
      case 'armory':
      case 'career':
        if (page === 'game' && field?.done) selected = Math.min(C.completed(p), 8);
        go(action, true);
        break;
      case 'audio':
        await transaction(() => {
          p.sound = !p.sound;
          return true;
        });
        if (page === 'game') document.querySelector('.field-top-tools [data-action="audio"]')
          .innerHTML = icon(p.sound ? 'sound' : 'mute');
        if (p.sound) signal('click');
        break;
      case 'district':
        districtDialog(Number(b.dataset.id));
        break;
      case 'select':
        if (C.available(p, Number(b.dataset.id))) {
          selected = Number(b.dataset.id);
          go('operations', true);
        }
        break;
      case 'intel':
        intel();
        break;
      case 'close':
        closeDialog();
        break;
      case 'deploy':
        signal('click');
        deploy();
        break;
      case 'buy':
        purchasing(b.dataset.id);
        break;
      case 'pause':
        pauseGame();
        break;
      case 'resume':
        resume();
        break;
      case 'uplink':
        closeDialog();
        field.continueAir();
        $('field').focus({
          preventScroll: true
        });
        break;
      case 'next':
        selected = Math.min(8, selected + 1);
        go('operations', true);
        break;
      case 'find':
      case 'lens':
      case 'reload':
        field?.[action]();
        $('field')?.focus({
          preventScroll: true
        });
        break;
    }
  });
  dialog.addEventListener('cancel', e => {
    e.preventDefault();
    if (modalKind === 'pause') resume();
    else if (['district', 'intel'].includes(modalKind)) closeDialog();
  });
  dialog.addEventListener('click', e => {
    if (e.target !== dialog || !['district', 'intel'].includes(modalKind)) return;
    const r = dialog.getBoundingClientRect();
    if (e.clientX < r.left || e.clientX > r.right || e.clientY < r.top || e.clientY > r.bottom)
      closeDialog();
  });
  document.querySelectorAll('[data-icon]').forEach(n => n.innerHTML = icon(n.dataset.icon));
  go('operations');
})();
