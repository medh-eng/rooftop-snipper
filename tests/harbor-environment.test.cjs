const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const base = 'unity/HarborWatch/Assets/HarborWatch/';
const read = p => fs.readFileSync(path.join(root, p), 'utf8');
const env = read(base + 'HarborEnvironment.cs');
const crowd = read(base + 'HarborCrowdReview.cs');
const editor = read(base + 'Editor/HarborEnvironmentBuildTools.cs');

const smooth = t => { t = Math.max(0, Math.min(1, t)); return t*t*(3-2*t); };
const ferryX = seconds => {
  const t = ((seconds%64)+64)%64;
  if(t<12) return 4.4;
  if(t<29) return 4.4 + (-10.1-4.4)*smooth((t-12)/17);
  if(t<34) return -10.1;
  if(t<52) return -10.1+(4.4+10.1)*smooth((t-34)/18);
  return 4.4;
};

test('environment shares the existing world across all three scenes', () => {
  assert.match(crowd, /environment = world\.AddComponent<HarborEnvironment>\(\)/);
  assert.match(crowd, /environment\.Initialise\(world\.transform, sourceMaterial\)/);
  assert.match(crowd, /stream = gameObject\.AddComponent<HarborCrowdTraffic>\(\)/);
  assert.match(crowd, /if \(missionMode\) mission = gameObject\.AddComponent<HarborMissionController>\(\)/);
  assert.match(crowd, /else if \(droneMode\) droneReview = gameObject\.AddComponent<HarborDroneReview>\(\)/);
  assert.match(read('unity/HarborWatch/Assets/Scenes/HarborDrone.unity'), /droneMode: 1/);
  assert.match(read('unity/HarborWatch/Assets/Scenes/HarborMission.unity'), /missionMode: 1/);
});
test('shared ambient motion respects game pause and slow modes', () => {
  assert.match(crowd, /if \(mission && mission\.IsRunning\)\s*\{\s*stream\.Step\(Time\.deltaTime\);\s*environment\.Step\(Time\.deltaTime\);/);
  assert.match(crowd, /if \(droneReview && droneReview\.IsPlaying\)\s*\{\s*stream\.Step\(Time\.deltaTime\);\s*environment\.Step\(Time\.deltaTime\);/);
  assert.match(crowd, /if \(!paused\)\s*\{\s*float seconds = Time\.deltaTime \* \(slow \? \.25f : 1f\);/);
  assert.match(env, /clock \+= Mathf\.Clamp\(elapsed, 0f, \.075f\)/);
  assert.doesNotMatch(env, /void Update\(/);
});
test('ferry eases both ways, dwells at quay and never teleports', () => {
  assert.match(env, /Mathf\.Repeat\(seconds, FerryPeriodSeconds\)/);
  for(let time=-64;time<128;time+=.25){
    const x=ferryX(time);
    assert.ok(x>=-10.100001 && x<=4.400001);
    assert.ok(Math.abs(x-ferryX(time+64))<1e-8);
    assert.ok(Math.abs(x-ferryX(time+.001))<.005);
  }
  for(const t of [0,5,11.99,52,63.99,64]) assert.equal(ferryX(t),4.4);
  for(const t of [29,31,33.99]) assert.equal(ferryX(t),-10.1);
  assert.ok(Math.abs(ferryX(20.5)-(-10.1+4.4)/2)<1e-7);
});
test('decor is lightweight, not interactive, and disposes created materials', () => {
  assert.match(env, /Destroy\(collider\)/);
  assert.match(env, /ShadowCastingMode\.Off/);
  assert.match(env, /Dictionary<Color, Material>/);
  assert.match(env, /void OnDestroy\(\)/);
  assert.match(env, /BuildFerry\(\)/);
  assert.match(env, /BuildBirds\(\)/);
  assert.doesNotMatch(env, /Rigidbody|Physics\.|AudioSource|Raycast/);
});
test('Unity menu and asset metadata permit a manual preflight', () => {
  assert.match(editor,/MenuItem\("Harbor Watch\/Verify Phase 5 Living Harbor"\)/);
  assert.match(editor,/HarborEnvironment\.FerryLocalX\(64f\)/);
  for(const name of ['HarborEnvironment.cs','Editor/HarborEnvironmentBuildTools.cs']){
    assert.match(read(base+name+'.meta'),/^fileFormatVersion: 2\nguid: [a-f0-9]{32}/);
  }
  const build=read('unity/HarborWatch/ProjectSettings/EditorBuildSettings.asset');
  for(const scene of ['HarborMission','HarborDrone','HarborCrowd','WalkReview'])
    assert.ok(build.includes('Assets/Scenes/'+scene+'.unity'));
});
