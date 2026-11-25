const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const file = name => fs.readFileSync(path.join(root,name),'utf8');
const base = 'unity/HarborWatch/';
const controller = file(base+'Assets/HarborWatch/HarborDroneReview.cs');
const flight = file(base+'Assets/HarborWatch/HarborDroneFlight.cs');
const review = file(base+'Assets/HarborWatch/HarborCrowdReview.cs');
const droneScene = file(base+'Assets/Scenes/HarborDrone.unity');
const missionScene = file(base+'Assets/Scenes/HarborMission.unity');
const crowdScene = file(base+'Assets/Scenes/HarborCrowd.unity');
const buildSettings = file(base+'ProjectSettings/EditorBuildSettings.asset');
const editor = file(base+'Assets/HarborWatch/Editor/HarborDroneBuildTools.cs');

const limit = (v,min,max) => Math.max(min,Math.min(max,v));
const constrain = p => ({
  x: limit(p.x,-11.4,11.4),
  y: limit(p.y,4.75,15.5),
  z: limit(p.z,-8.7,7.6)
});
const blocked = p => p.y<6.25 && Math.abs(p.x+7.4)<1.95
  && Math.abs(p.z+5)<2.45;
const move=(p,delta)=>{
  const target=constrain({x:p.x+delta.x,y:p.y+delta.y,z:p.z+delta.z});
  if (!blocked(target))return target;
  let result=p;
  const tryMove = q => blocked(q) ? result : constrain(q);
  result=tryMove({x:target.x,y:result.y,z:result.z});
  result=tryMove({x:result.x,y:result.y,z:target.z});
  result=tryMove({x:result.x,y:target.y,z:result.z});
  return result;
};
const horizontal=(degrees,stick)=>{
  const a=degrees*Math.PI/180;
  const f={x:Math.sin(a),z:Math.cos(a)};
  const x=f.z*stick.x + f.x*stick.y;
  const z=-f.x*stick.x + f.z*stick.y;
  const norm=Math.max(1,Math.hypot(x,z));
  return {x:x/norm,y:0,z:z/norm};
};

test('Phase 4 adds a separate drone scene while keeping Phase 3 first',()=>{
  assert.match(droneScene,/droneMode: 1/);
  assert.doesNotMatch(droneScene,/missionMode: 1/);
  assert.match(missionScene,/missionMode: 1/);
  assert.doesNotMatch(missionScene,/droneMode: 1/);
  assert.doesNotMatch(crowdScene,/droneMode: 1/);
  const scenes = [...buildSettings.matchAll(/path: (Assets\/Scenes\/[^\n]+)/g)]
    .map(m=>m[1]);
  // Find the One is the product now and comes first; the older phases keep this relative order.
  assert.equal(scenes[0], 'Assets/Scenes/FindTheOne.unity');
  assert.deepEqual(scenes.slice(1,5),[
    'Assets/Scenes/HarborMission.unity',
    'Assets/Scenes/WalkReview.unity',
    'Assets/Scenes/HarborCrowd.unity',
    'Assets/Scenes/HarborDrone.unity'
  ]);
  assert.match(droneScene,/guid: 9b62f04942e44ff4a73a42dacc5902b1/);
  assert.match(buildSettings,/guid: 81205d3c6aef42c68bc6a5efd8151144/);
  assert.match(file(base+'Assets/Scenes/HarborDrone.unity.meta'),
    /guid: 81205d3c6aef42c68bc6a5efd8151144/);
  assert.match(review,/else if \(droneMode\) droneReview = gameObject\.AddComponent<HarborDroneReview>\(\)/);
  assert.match(review,/droneReview && droneReview\.IsPlaying/);
  assert.match(review,/if \(\(missionMode \|\| droneMode\) && error == null\) return/);
});

test('flight bounds stay over pedestrian level and outside observer rooftop',()=>{
  assert.match(flight,/MinAltitude = 4\.75f/);
  assert.match(flight,/MaxAltitude = 15\.5f/);
  assert.match(flight,/RoofBlockedBelow = 6\.25f/);
  assert.match(flight,/Vector3 Advance\(Vector3 from, Vector3 displacement\)/);
  const initial={x:-5.1,y:8.5,z:-3.3};
  assert.equal(blocked(initial),false);
  assert.deepEqual(constrain({x:100,y:-6,z:-100}),
    {x:11.4,y:4.75,z:-8.7});
  const from={x:-5.1,y:5.1,z:-5};
  const movingIn=move(from,{x:-2.3,y:0,z:0});
  assert.equal(blocked(movingIn),false);
  assert.equal(movingIn.x,from.x,'side blocker must not tunnel into building');
  const above={x:-7.4,y:7,z:-5};
  const descending=move(above,{x:0,y:-2,z:0});
  assert.ok(!blocked(descending),'must not descend into the roof');
  assert.equal(descending.y,7,'ceiling blocks descent when directly over rooftop');
  const around={x:-5.1,y:6.9,z:-2.0};
  assert.equal(blocked(move(around,{x:-.2,y:-1.8,z:0})),false);
  assert.match(controller,/HarborDroneFlight\.Advance\(cameraPosition, velocity \* dt\)/);
});

test('horizontal flight respects heading without accelerating diagonal input',()=>{
  assert.match(flight,/Vector3\.ClampMagnitude\(right \* stick\.x \+ forward \* stick\.y, 1f\)/);
  const north=horizontal(0,{x:0,y:1});
  assert.ok(Math.abs(north.x)<1e-9);
  assert.ok(Math.abs(north.z-1)<1e-9);
  const east=horizontal(90,{x:0,y:1});
  assert.ok(Math.abs(east.x-1)<1e-9);
  assert.ok(Math.abs(east.z)<1e-9);
  const diagonal=horizontal(34,{x:1,y:1});
  assert.ok(Math.abs(Math.hypot(diagonal.x,diagonal.z)-1)<1e-9);
});

test('same live crowd and camera transition without spawning a second simulation',()=>{
  assert.match(controller,/GetComponent<HarborCrowdReview>\(\)/);
  assert.match(controller,/concourse\.ConcourseRoot/);
  assert.match(controller,/concourse\.Traffic\.ActiveCount/);
  assert.match(controller,/RooftopPosition/);
  assert.match(controller,/void ToggleCamera\(\)/);
  assert.match(controller,/cameraView\.transform\.position = cameraPosition/);
  assert.match(controller,/cameraView\.transform\.position = RooftopPosition/);
  assert.doesNotMatch(controller,/new HarborCrowdTraffic|AddComponent<HarborCrowdTraffic>/);
  assert.match(controller,/Screen\.orientation = ScreenOrientation\.Portrait/);
});

test('two finger input, pause, vertical movement and nonviolent drone identification',()=>{
  assert.match(controller,/foreach \(var touch in screen\.touches\)/);
  assert.match(controller,/touch\.touchId\.ReadValue\(\)/);
  assert.match(controller,/moveFinger/);
  assert.match(controller,/lookFinger/);
  assert.match(controller,/ReadMouse\(\)/);
  assert.match(controller,/ReadKeyboard\(\)/);
  assert.match(controller,/if \(ButtonArea\(4\)\.Contains\(gui\)\) climb = 1f/);
  assert.match(controller,/else if \(ButtonArea\(5\)\.Contains\(gui\)\) climb = -1f/);
  assert.match(controller,/public bool IsPlaying \{ get \{ return !paused; \} \}/);
  assert.match(controller,/float dt = Mathf\.Min\(Time\.deltaTime, \.05f\)/);
  assert.match(controller,/Physics\.Raycast\(cameraView\.transform\.position/);
  assert.match(controller,/HasLineOfSight\(beacon\.transform\.position\)/);
  assert.match(controller,/float messageY = ButtonArea\(8\)\.yMax \+ 7f/);
  assert.match(controller,/identified = true/);
  assert.match(controller,/No contact or damage/);
  assert.doesNotMatch(controller,/Fire\(|Shoot\(|Instantiate\(\s*projectile|Damage\(/);
});

test('Unity menu provides isolated test APK and does not replace default mission',()=>{
  assert.match(editor,/MenuItem\("Harbor Watch\/Verify Phase 4 Drone"\)/);
  assert.match(editor,/MenuItem\("Harbor Watch\/Build Drone Review APK"\)/);
  assert.match(editor,/scenes = new\[\] \{ DroneScene \}/);
  assert.match(editor,/BuildOptions\.Development \| BuildOptions\.AllowDebugging/);
  assert.match(editor,/HarborDroneFlight\.InsideRoof\(resolved\)/);
  assert.match(editor,/com\.medhaneeka\.harborwatch/);
});
