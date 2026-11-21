const test = require('node:test');
const assert = require('node:assert/strict');
const C = require('../src/campaign.js');
require('../src/interface.js');
require('../src/engine.js');
const { resolveShot, Field } = globalThis.SignalField;

function perfect(m) {
  const hits = m.ground + m.air;
  return { hits, shots: hits, blocks: 0, elapsed: 14.4, bestChain: hits };
}

test('a new player can buy any first upgrade and only the first contract is open', () => {
  for (const kit of C.kits) {
    const p = C.fresh();
    assert.deepEqual(C.missions.filter(m => C.available(p, m.id)).map(m => m.id), [0]);
    assert.equal(C.buy(p, kit.id), true);
    assert.equal(p.credits, 350 - kit.costs[0]);
    assert.equal(p.upgrades[kit.id], 1);
    assert.equal(C.buy(p, kit.id), false);
  }
});

test('two upgrades in each track change play and cap at tier III', () => {
  const p = C.fresh();
  p.credits = 5000;
  for (const kit of C.kits) {
    assert.equal(C.buy(p, kit.id), true);
    assert.equal(C.buy(p, kit.id), true);
    const balance = p.credits;
    assert.equal(C.buy(p, kit.id), false);
    assert.equal(p.credits, balance);
  }
  assert.deepEqual(C.stats(p), { reload: 1.1, recovery: .27, zoom: 2.3, capacity: 9, radius: 1.36 });
});

test('all nine clear in order, aerial and mixed missions unlock, and caches pay once', () => {
  const p = C.fresh();
  let earned = 0;
  assert.equal(C.settle(p, C.missions[3], perfect(C.missions[3])), null);
  for (const m of C.missions) {
    const run = perfect(m);
    const receipt = C.settle(p, m, run);
    assert.equal(receipt.stars, 3);
    assert.equal(receipt.districtCache, m.id % 3 === 2 ? C.cache : 0);
    assert.equal(C.settle(p, m, run), null);
    earned += receipt.total;
  }
  assert.equal(p.earned, earned);
  assert.equal(p.credits, 350 + earned);
  assert.equal(C.completed(p), 9);
  assert.equal(C.mode(C.missions[5]), 'Drone uplink');
  assert.equal(C.mode(C.missions[8]), 'Rooftop + drone');
  const replay = C.settle(p, C.missions[8], perfect(C.missions[8]));
  assert.equal(replay.first, false);
  assert.equal(replay.districtCache, 0);
  assert.equal(replay.rewards[0][1], Math.round(C.missions[8].base / 2));
});

test('partial runs receive no payout, replay keeps personal bests and relays add credits', () => {
  const p = C.fresh(), m = C.missions[0];
  assert.equal(C.settle(p, m, { ...perfect(m), hits: 2 }), null);
  C.settle(p, m, perfect(m));
  const receipt = C.settle(p, m, { ...perfect(m), shots: 8, blocks: 1, elapsed: 21, relay: true });
  assert.equal(receipt.stars, 1);
  assert.equal(receipt.precision, 38);
  assert.equal(receipt.rewards.find(row => row[0] === 'Gold relay')[1], 80);
  assert.equal(p.best[0].stars, 3);
  assert.equal(p.best[0].seconds, 15);
});

test('malformed or older saves cannot skip locked contracts or exceed equipment tiers', () => {
  assert.deepEqual(C.sanitize({ version: 0 }), C.fresh());
  const result = C.sanitize({ version: 1, credits: -500, best: { 1: { stars: 3 } }, upgrades: { gun: 78, lens: 'wrong' } });
  assert.equal(result.credits, 0);
  assert.equal(result.upgrades.gun, 2);
  assert.equal(result.upgrades.lens, 0);
  assert.equal(C.completed(result), 0);
  assert.deepEqual(C.sanitize(C.fresh()), C.fresh());
});

function projected(target, id) {
  return { actor: { target, id }, x: 100, y: 180, width: 40, height: 80, markY: 80, markerSize: 10 };
}

test('the frontmost civilian blocks body shots and a deliberate red diamond selects its owner', () => {
  const rear = projected(true, 1), front = projected(false, 2), actors = [rear, front];
  assert.equal(resolveShot(actors,100,140,1), front.actor);
  assert.equal(resolveShot(actors,100,80,1), rear.actor);
  assert.equal(resolveShot(actors,20,30,1), null);
  assert.equal(resolveShot([rear],124,80,1,false), null);
  assert.equal(resolveShot([rear],124,80,1,true), rear.actor);
  assert.equal(resolveShot([rear],119,80,1.36,false), rear.actor);
});

test('civilian protection spends no ammunition and clears the chain', () => {
  const front = projected(false, 1);
  const f = Object.assign(Object.create(Field.prototype), {
    paused: false, done: false, cooldown: 0, reloading: 0, aim: { x: 100, y: 140 }, ammo: 4, chain: 2,
    cfg: { radius: 1 }, run: { shots: 0, hits: 0, blocks: 0 }, cam: { x: 0, y: 0, zoom: 1 },
    w: 300, h: 200, baseScale: 1, cb: { sound(){}, message(){} },
    visible: () => [front], ripple(){}, hud(){}
  });
  f.fire();
  assert.deepEqual([f.run.blocks,f.run.shots,f.ammo,f.chain], [1,0,4,0]);
});

test('a gold relay refills once, preserves precision, and is never considered a civilian', () => {
  const f = Object.assign(Object.create(Field.prototype), {
    paused: false, done: false, cooldown: 0, reloading: 0, aim: { x: 200, y: 150 }, ammo: 3,
    cfg: { capacity: 7, recovery: .55 }, relay: { x: 100, y: 50 }, run: { shots: 2 },
    cam: { x: 100, y: 50, zoom: 1 }, w: 400, h: 300, baseScale: 1, cb: { sound(){}, message(){} }, hud(){}
  });
  f.fire();
  assert.equal(f.relay, null);
  assert.equal(f.run.relay, true);
  assert.equal(f.ammo, 7);
  assert.equal(f.run.shots, 2);
});

test('the aerial clean-intercept star requires every pulse to connect', () => {
  const p = C.fresh();
  for (const m of C.missions.slice(0,5)) C.settle(p,m,perfect(m));
  const drone = C.missions[5];
  const report = C.settle(p,drone,{...perfect(drone),shots:drone.air+1});
  assert.equal(report.stars,2);
  assert.equal(report.rewards.find(([label]) => label === 'Clean intercept')[1],0);
});
