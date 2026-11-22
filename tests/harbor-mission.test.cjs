const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const file = name => fs.readFileSync(path.join(root, name), 'utf8');
const folder = 'unity/HarborWatch/Assets/';
const rules = file(folder + 'HarborWatch/HarborMissionRules.cs');
const mission = file(folder + 'HarborWatch/HarborMissionController.cs');
const review = file(folder + 'HarborWatch/HarborCrowdReview.cs');
const scene = file(folder + 'Scenes/HarborMission.unity');
const crowd = file(folder + 'Scenes/HarborCrowd.unity');
const build = file('unity/HarborWatch/ProjectSettings/EditorBuildSettings.asset');

function payout(signals, attempts, bestChain, firstClear) {
  if (signals !== 3 || attempts < signals) return 0;
  const precision = Math.round(100 * signals / attempts);
  const clean = attempts === signals;
  return (firstClear ? 240 : 120)
    + Math.round(precision * .8)
    + (clean ? 100 : 0)
    + Math.min(3, Math.max(0, bestChain)) * 12
    + (firstClear ? 120 : 0);
}

test('first complete survey pays the contract-perfect 576 credits', () => {
  assert.equal(payout(3,3,3,true), 576);
  assert.equal(350 + payout(3,3,3,true), 926);
  assert.equal(payout(3,3,3,false), 336);
  assert.ok(payout(3,5,1,true) < 576);
  assert.equal(payout(2,2,2,true), 0);
  assert.match(rules, /TimeLimitSeconds = 90/);
  assert.match(rules, /RequiredSignals = 3/);
  assert.match(rules, /StartingCredits = 350/);
  assert.match(rules, /firstClear \? 240 : 120/);
  assert.match(rules, /firstClear \? 120 : 0/);
  assert.match(rules, /precision \* \.8f/);
  assert.match(rules, /clean \? 100 : 0/);
  assert.match(rules, /Mathf\.Clamp\(bestChain, 0, RequiredSignals\) \* 12/);
});

test('mission uses a separate valid scene but preserves the old crowd review', () => {
  assert.match(scene, /missionMode: 1/);
  assert.doesNotMatch(crowd, /missionMode: 1/);
  assert.match(scene, /guid: 9b62f04942e44ff4a73a42dacc5902b1/);
  assert.match(review, /public bool missionMode/);
  assert.match(review, /AddComponent<HarborMissionController>\(\)/);
  assert.match(review, /mission && mission\.IsRunning/);
  assert.match(review, /if \(missionMode && error == null\) return;/);
  const scenes = [...build.matchAll(/path: (Assets\/Scenes\/[^\n]+)/g)].map(x=>x[1]);
  assert.deepEqual(scenes.slice(0,3),
    ['Assets/Scenes/HarborMission.unity',
     'Assets/Scenes/WalkReview.unity',
     'Assets/Scenes/HarborCrowd.unity']);
  assert.match(build, /guid: e0c148431ad944be88ef3a342ad424ae/);
  assert.match(file(folder+'Scenes/HarborMission.unity.meta'),
    /guid: e0c148431ad944be88ef3a342ad424ae/);
});

test('scanner targets three static fictional relays and never pedestrians', () => {
  assert.match(mission, /class HarborMissionController/);
  assert.match(mission, /SignalLocations/);
  const positions = [...mission.matchAll(/new Vector3\((-?\d+\.?\d*)f?,\s*(-?\d+\.?\d*)f?,\s*(-?\d+\.?\d*)f?\)/g)]
    .map(m => m.slice(1).map(Number));
  for (const key of ['(-3.0f, 3.1f, -1.5f)', '(1.2f, 3.25f, 1.6f)',
                      '(7.5f, 3.2f, -1.1f)']) {
    assert.ok(mission.includes('new Vector3'+key), 'missing distinct signal '+key);
  }
  assert.equal(new Set(positions.slice(1,4).map(p=>p.join(','))).size, 3);
  assert.match(mission, /AimAtSignal\(\)/);
  assert.match(mission, /lens\.WorldToScreenPoint\(signals\[i\]\.WorldPosition\)/);
  assert.match(mission, /signals\[selected\]\.Cleared = true/);
  assert.match(mission, /if \(cleared == HarborMissionRules\.RequiredSignals\) FinishMission\(true\)/);
  assert.doesNotMatch(mission, /Physics\.Raycast|Instantiate\(\s*projectile|ApplyDamage|Shoot/);
});

test('mission has touch drag, zoom, pause, restart, timer and permanent save', () => {
  assert.match(mission, /Touchscreen\.current/);
  assert.match(mission, /Mouse\.current/);
  assert.match(mission, /touch\.touches\[1\]\.press\.isPressed/);
  assert.match(mission, /delta\.x \* sensitivity/);
  assert.match(mission, /Scan\(\); break;/);
  assert.match(mission, /case 1:[\s\S]*?lens\.fieldOfView/);
  assert.match(mission, /case 3:[\s\S]*?paused = !paused/);
  assert.match(mission, /case 4: RestartMission\(\); break;/);
  assert.match(mission, /secondsLeft - Time\.deltaTime/);
  assert.match(mission, /if \(secondsLeft <= 0f\) FinishMission\(false\)/);
  assert.match(mission, /PlayerPrefs\.SetInt\(SaveCredits, credits\)/);
  assert.match(mission, /PlayerPrefs\.SetInt\(SaveCleared, 1\)/);
  assert.match(mission, /PlayerPrefs\.Save\(\)/);
});
