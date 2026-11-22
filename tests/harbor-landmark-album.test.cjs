const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const read = name => fs.readFileSync(path.join(root, name), 'utf8');
const dir = 'unity/HarborWatch/Assets/HarborWatch/';
const album = read(dir + 'HarborLandmarkAlbum.cs');
const controller = read(dir + 'HarborMissionController.cs');
const verifier = read(dir + 'Editor/HarborAlbumVerification.cs');
const meta = read(dir + 'HarborLandmarkAlbum.cs.meta');

test('three unique static landmarks with saved, bounded bitmask', () => {
  assert.match(album, /PhotoCount = 3/);
  for (const name of ['Ticket kiosk', 'Terminal canopy', 'Harbor crane']) {
    assert.ok(album.includes(name), name);
  }
  assert.match(album, /mask = savedMask & FullMask/);
  assert.match(album, /mask \|= 1 << current/);
  assert.match(album, /CurrentIndex < 0 \? Vector3.zero/);
  assert.match(meta, /guid: [0-9a-f]{32}/);
});

test('photo framing requires zoom, a front-facing landmark and central alignment', () => {
  assert.match(album, /viewport.z <= 0f/);
  assert.match(album, /fov > 48f/);
  assert.match(album, /\(viewport.x - \.5f\) \* aspect/);
  assert.match(album, /x\*x \+ y\*y <= \.09f \* \.09f/);
  assert.match(verifier, /MenuItem\("Harbor Watch\/Verify Landmark Album"\)/);
  assert.match(verifier, /saved <= HarborLandmarkAlbum.FullMask/);
  assert.match(verifier, /Captured behind camera/);
  assert.match(verifier, /Unzoomed photo accepted/);
});

test('photo album appears only after success, preserves original rewards, never scans visitors', () => {
  assert.match(controller, /if \(finished && won && i == 3\)/);
  assert.match(controller, /photoMode = !photoMode/);
  assert.match(controller, /case 0: CapturePhoto\(\); break;/);
  assert.match(controller, /if \(!photoMode \|\| !won\) return;/);
  assert.match(controller, /PlayerPrefs.SetInt\(SaveAlbum, album.Mask\)/);
  assert.match(controller, /album.Load\(PlayerPrefs.GetInt\(SaveAlbum, 0\)\)/);
  assert.match(controller, /if \(!finished\)[\s\S]*?secondsLeft = Mathf.Max/);
  assert.match(controller, /\(!finished \|\| photoMode\)/);
  assert.match(controller, /if \(cleared == HarborMissionRules.RequiredSignals\) FinishMission\(true\)/);
  assert.match(controller, /HarborMissionRules.Settle\(/);
  assert.doesNotMatch(album, /PlayerPrefs.SetInt\(SaveCredits|AddCredits|ApplyDamage/);
});
