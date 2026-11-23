// Browser play-through of Red Signal: Find the One (game/index.html).
// Needs Playwright + Chromium:  node --test tests/find-the-one-browser.cjs
// BROWSER_PATH may point at an existing Chromium/Chrome executable.
const test = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
let chromium;
try { ({ chromium } = require('playwright')); } catch (e) { chromium = null; }

const url = pathToFileURL(path.join(__dirname, '..', 'game', 'index.html')).href;
const launch = () => chromium.launch(process.env.BROWSER_PATH ? { executablePath: process.env.BROWSER_PATH } : {});

test('phone: menus, touch select, hold-to-tag and a win with score', { skip: !chromium && 'playwright not installed' }, async () => {
  const browser = await launch();
  const page = await browser.newPage({ viewport: { width: 390, height: 844 }, isMobile: true, hasTouch: true });
  const errors = [];
  page.on('pageerror', e => errors.push(e.message));
  await page.goto(url);
  await page.waitForSelector('#title-screen:not([hidden])');
  await page.click('#campaign-btn');
  assert.equal(await page.locator('.case-card:not([disabled])').count(), 1, 'only case 1 is open on a fresh save');
  await page.locator('.case-card').first().click();
  await page.click('#begin-btn');
  await page.waitForFunction(() => window.__redSignal.game && window.__redSignal.game.t > .3);
  assert.equal(await page.locator('.clue:not(.locked)').count(), 1, 'first intel line is free');

  // Clear everyone else out of the way, centre the courier, and tap them.
  const box = await page.evaluate(() => {
    const R = window.__redSignal, g = R.game;
    for (const p of g.people) if (p !== g.courier) p.wx = -500;
    g.cam.x = g.courier.wx; g.cam.y = g.courier.wy - 40;
    const e = R.project(g.courier);
    return { x: e.x, y: e.y - e.h * .5 };
  });
  await page.touchscreen.tap(box.x, box.y);
  assert.ok(await page.evaluate(() => window.__redSignal.game.selected === window.__redSignal.game.courier), 'tap selects the courier');

  // Releasing before the ring locks does nothing.
  await page.evaluate(() => { window.__redSignal.startHold(); window.__redSignal.releaseHold(); });
  assert.equal(await page.evaluate(() => window.__redSignal.game.shot), null);
  // Hold until locked, then release.
  await page.evaluate(() => window.__redSignal.startHold());
  await page.waitForTimeout(1100);
  await page.evaluate(() => window.__redSignal.releaseHold());
  await page.waitForSelector('#result-screen:not([hidden])', { timeout: 6000 });
  assert.match(await page.textContent('#result-eyebrow'), /CASE CLOSED/);
  const score = Number((await page.textContent('#result-table tr.total td:last-child')).trim());
  assert.ok(score >= 500, 'score ' + score);
  assert.deepEqual(errors, []);
  await browser.close();
});

test('wrong tag spooks the courier; a second wrong tag loses and reveals the courier', { skip: !chromium && 'playwright not installed' }, async () => {
  const browser = await launch();
  const page = await browser.newPage({ viewport: { width: 1280, height: 760 } });
  await page.goto(url);
  await page.waitForSelector('#title-screen:not([hidden])');
  await page.evaluate(() => window.__redSignal.startCase({ index: 2 }));
  const tagCivilian = async () => {
    await page.evaluate(() => {
      const g = window.__redSignal.game;
      const civ = g.people.find(p => p !== g.courier && !p.wrong);
      for (const p of g.people) if (p !== civ) p.wy = 0;
      g.selected = civ; window.__redSignal.startHold();
    });
    await page.waitForTimeout(1100);
    await page.evaluate(() => window.__redSignal.releaseHold());
    await page.waitForTimeout(2400);
  };
  await tagCivilian();
  const state = await page.evaluate(() => ({ shots: window.__redSignal.game.shots, alert: window.__redSignal.game.alert, left: window.__redSignal.game.timeLeft }));
  assert.equal(state.shots, 1);
  assert.equal(state.alert, true);
  assert.ok(state.left <= 15);
  await tagCivilian();
  await page.waitForSelector('#result-screen:not([hidden])', { timeout: 6000 });
  assert.match(await page.textContent('#result-eyebrow'), /CASE LOST/);
  assert.match(await page.textContent('#result-line'), /The courier was/);
  await browser.close();
});

test('daily case: same courier for the same date, share text without spoilers', { skip: !chromium && 'playwright not installed' }, async () => {
  const browser = await launch();
  const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
  await page.goto(url);
  await page.waitForSelector('#title-screen:not([hidden])');
  await page.click('#daily-btn');
  await page.click('#begin-btn');
  const first = await page.evaluate(() => window.__redSignal.game.kase.target);
  await page.evaluate(() => { const g = window.__redSignal.game; for (const p of g.people) if (p !== g.courier) p.wy = 0; g.selected = g.courier; window.__redSignal.startHold(); });
  await page.waitForTimeout(1100);
  await page.evaluate(() => window.__redSignal.releaseHold());
  await page.waitForSelector('#result-screen:not([hidden])', { timeout: 6000 });
  const share = await page.textContent('#result-share');
  assert.match(share, /RED SIGNAL · Daily #\d+/);
  assert.doesNotMatch(share, /jacket|coat|cap|beanie|briefcase|backpack/i);
  await page.reload();
  await page.waitForSelector('#title-screen:not([hidden])');
  assert.match(await page.textContent('#daily-btn'), /Solved/);
  await page.evaluate(() => window.__redSignal.openDaily());
  assert.match(await page.textContent('#result-share'), /Daily/);
  await page.click('text=Practise again');
  assert.equal(await page.evaluate(() => window.__redSignal.game.kase.target), first);
  await browser.close();
});
