// Optional end-to-end check. Requires Playwright and a Chromium installation.
const test = require('node:test');
const assert = require('node:assert/strict');
const http = require('node:http');
const path = require('node:path');
const fs = require('node:fs/promises');
const os = require('node:os');
const { pathToFileURL } = require('node:url');
const { execFileSync } = require('node:child_process');
const { chromium } = require('playwright');
const C = require('../src/campaign.js');
const root = path.resolve(__dirname, '..');

const types = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.webp': 'image/webp', '.svg': 'image/svg+xml', '.woff': 'font/woff' };
function serve(req, res) {
  const url = new URL(req.url, 'http://localhost');
  const file = path.resolve(root, '.' + (url.pathname === '/' ? '/index.html' : url.pathname));
  if (!file.startsWith(root + path.sep)) { res.writeHead(404).end(); return; }
  fs.readFile(file).then(data => res.writeHead(200, { 'content-type': types[path.extname(file)] || 'text/plain' }).end(data), () => res.writeHead(404).end());
}

async function until(check, ms = 9000) {
  const limit = Date.now() + ms;
  while (Date.now() < limit) { if (await check()) return; await new Promise(resolve => setTimeout(resolve, 45)); }
  assert.fail('The expected gameplay state did not appear');
}

// Locate a rendered UI marker of the exact palette colour, independently of game actor state.
async function marking(page, rgb) {
  return page.locator('canvas').evaluate((canvas, colour) => {
    const { width, height } = canvas;
    const bounds = canvas.getBoundingClientRect();
    const data = canvas.getContext('2d').getImageData(0,0,width,height).data;
    const first = [], ratio = width / bounds.width;
    for (let y = 0; y < height; y += 2) {
      for (let x = 0; x < width; x += 2) {
        const j = (y * width + x) * 4;
        if (data[j] === colour[0] && data[j+1] === colour[1] && data[j+2] === colour[2]) first.push({ x, y });
        if (first.length >= 5) break;
      }
      if (first.length >= 5) break;
    }
    if (!first.length) return null;
    return { x: bounds.left + first[0].x / ratio, y: bounds.top + first[0].y / ratio + 5 };
  }, rgb);
}

const start = async page => {
  await page.locator('.hero-actions [data-action="deploy"]').click();
  await page.locator('#loading-scene').waitFor({ state:'detached' });
};
const click = async (page,action) => page.locator(`button[data-action="${action}"]:visible`).first().click();
const errors = page => { const list = []; page.on('pageerror', e => list.push(e.message)); page.on('response', r => { if (r.status() >= 400) list.push(r.status() + ' ' + r.url()); }); return list; };

async function playContract(page,m) {
  await start(page);
  if (m.id === 5 || m.id === 7) assert.equal(await page.locator('#feed-name').innerText(),'DRONE UPLINK');
  let relayClaimed = false;
  for(let count = 0; count < m.ground + m.air; count++) {
    if (m.ground && m.air && count === m.ground) {
      await page.getByRole('button',{name:'Launch drone'}).click();
      assert.equal(await page.locator('#feed-name').innerText(),'DRONE UPLINK');
    }
    const desired = count + 1;
    await until(async () => {
      if (await page.getByRole('dialog').isVisible()) return true;
      if (Number((await page.locator('#hud-count').innerText()).split('/')[0]) >= desired) return true;
      await page.keyboard.press('f'); await page.keyboard.press('Space');
      return false;
    });
    if (m.id === 1 && count === 1) {
      const spot = await marking(page,[82,59,24]); assert.ok(spot,'visible gold relay');
      await new Promise(resolve=>setTimeout(resolve,650));
      await page.mouse.click(spot.x,spot.y);
      await until(async()=> (await page.locator('#field-message').innerText()).includes('refilled'));
      relayClaimed = true;
    }
  }
  await page.getByRole('heading',{name:'Contract clear'}).waitFor();
  if (relayClaimed) assert.ok((await page.locator('.reward-list').innerText()).includes('Gold relay'));
  if (m.id%3===2) assert.ok((await page.locator('.reward-list').innerText()).includes('District cache'));
}

test('desktop campaign and mobile touch, saved upgrades, locks, gold relay and mixed finale', {timeout:180000}, async t => {
  const server = http.createServer(serve); await new Promise(resolve => server.listen(0,'127.0.0.1',resolve)); t.after(()=>server.close());
  const url = `http://127.0.0.1:${server.address().port}/`;
  const browser=await chromium.launch({ executablePath:process.env.BROWSER_PATH || undefined, headless:true, args:['--no-sandbox'] }); t.after(()=>browser.close());
  const desktop = await browser.newContext({viewport:{width:1366,height:880}}), page = await desktop.newPage(); const problems=errors(page);
  // Exercise the actual downloadable edition through file:// with networking
  // disabled. The touch run below still covers the hosted entry point.
  const temp = await fs.mkdtemp(path.join(os.tmpdir(), 'harbor-offline-'));
  t.after(() => fs.rm(temp, { recursive: true, force: true }));
  const packed = path.join(temp, 'Harbor-Watch.html');
  execFileSync(process.execPath, [path.join(root, 'tools/build-offline.cjs'), packed]);
  await desktop.setOffline(true);
  await page.goto(pathToFileURL(packed).href);await page.getByRole('heading',{name:'Harbor Watch'}).waitFor();
  await page.getByRole('button',{name:/Vesper City, Locked/}).click();assert.equal(await page.locator('.contract-row:disabled').count(),3);
  await page.keyboard.press('Escape');await click(page,'intel');await page.getByRole('heading',{name:'Harbor Watch',exact:true}).last().waitFor();await page.keyboard.press('Escape');
  await click(page,'armory');await page.locator('button[data-id="cartridge"]').click();await until(async()=>await page.locator('#balance').innerText()==='140');
  await page.reload();await click(page,'armory');assert.ok((await page.locator('.kit-card').nth(2).innerText()).includes('TIER II'));await click(page,'operations');
  for(const mission of C.missions) {
    await playContract(page,mission);
    t.diagnostic(`Cleared ${mission.id+1}. ${mission.name}`);
    if(mission.id<8)await click(page,'next');
  }
  await click(page,'career');assert.equal(await page.locator('.record-row:not([disabled])').count(),9);
  await page.reload();await click(page,'career');assert.equal(await page.locator('.record-row .stars').count(),9);
  await desktop.close();assert.deepEqual(problems,[]);

  const phone=await browser.newContext({viewport:{width:390,height:844},deviceScaleFactor:2,isMobile:true,hasTouch:true});
  const mobile=await phone.newPage(), mobileProblems=errors(mobile);await mobile.goto(url);assert.equal(await mobile.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),true);
  await start(mobile);await mobile.locator('[data-action="pause"]').tap();const paused=await mobile.locator('#hud-time').innerText();await new Promise(r=>setTimeout(r,1150));assert.equal(await mobile.locator('#hud-time').innerText(),paused);
  await mobile.locator('[data-action="resume"]').tap();await mobile.locator('#lens-control').tap();assert.equal(await mobile.locator('#lens-control').getAttribute('aria-pressed'),'true');
  await mobile.locator('[data-action="find"]').tap();let mark=await marking(mobile,[246,77,55]);assert.ok(mark,'visible red diamond');await mobile.touchscreen.tap(mark.x,mark.y);await until(async()=>await mobile.locator('#hud-count').innerText()==='1 / 3');
  await mobile.locator('#reload-control').tap();await until(async()=>await mobile.locator('#hud-ammo').innerText()==='5 / 5');
  for(let i=2;i<=3;i++){
    await mobile.locator('[data-action="find"]').tap();mark=await marking(mobile,[246,77,55]);assert.ok(mark);await mobile.touchscreen.tap(mark.x,mark.y);
    if(i===2){await until(async()=>await mobile.locator('#hud-count').innerText()==='2 / 3');await new Promise(r=>setTimeout(r,650));}
  }
  await mobile.getByRole('heading',{name:'Contract clear'}).waitFor();await click(mobile,'operations');await mobile.reload();
  assert.ok((await mobile.locator('#mission-name').innerText()).toLowerCase().includes('ferry'));
  assert.deepEqual(mobileProblems,[]);await phone.close();
});
