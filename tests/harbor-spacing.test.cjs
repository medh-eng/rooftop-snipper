const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const spacing = fs.readFileSync(path.join(root,
  'unity/HarborWatch/Assets/HarborWatch/HarborCrowdSpacing.cs'), 'utf8');
const crowd = fs.readFileSync(path.join(root,
  'unity/HarborWatch/Assets/HarborWatch/HarborCrowdTraffic.cs'), 'utf8');

const dot = (a,b) => a.x*b.x + a.z*b.z;
const sub = (a,b) => ({x:a.x-b.x,z:a.z-b.z});
const mul = (a,n) => ({x:a.x*n,z:a.z*n});
const add = (a,b) => ({x:a.x+b.x,z:a.z+b.z});
const norm = a => {const m=Math.hypot(a.x,a.z);return m>0?mul(a,1/m):a};
const clamp = (v,min,max) => Math.max(min,Math.min(max,v));
const invLerp = (from,to,value) => clamp((value-from)/(to-from),0,1);

// JavaScript reference implementation of the simple speed-factor math.
// These checks test intended geometry. Unity C# compilation and runtime
// behaviour must still be checked in the Unity Editor.
function referenceFactor(self,other) {
  const delta=sub(other.pos,self.pos);
  const distance=Math.hypot(delta.x,delta.z);
  if(distance>3.1)return 1;
  const heading=norm(self.heading), otherHeading=norm(other.heading);
  if(Math.hypot(heading.x,heading.z)<.001||
     Math.hypot(otherHeading.x,otherHeading.z)<.001)return 1;
  const ahead=dot(delta,heading);
  const sameDirection=dot(heading,otherHeading);
  const lateral=Math.sqrt(Math.max(0, dot(delta,delta)-ahead*ahead));
  if(sameDirection>.72&&ahead>0&&lateral<.68)
    return invLerp(.75,1.8,ahead);
  const relative=sub(mul(otherHeading,other.speed),mul(heading,self.speed));
  const velocitySquared=dot(relative,relative);
  if(velocitySquared<.0001)return 1;
  const meetingTime=clamp(-dot(delta,relative)/velocitySquared,0,1.35);
  if(meetingTime<.02)return 1;
  const closest=Math.hypot(...Object.values(add(delta,mul(relative,meetingTime))));
  if(closest>=.85)return 1;
  if(self.index<other.index)return 1;
  return invLerp(.85,2.50,distance);
}
const p=(x,z)=>({x,z});

test('helper is wired into the actual ambient-traffic loop',()=>{
  assert.match(spacing,/public static float YieldFactor\s*\(/);
  assert.match(spacing,/public const float LookAheadSeconds = 1\.35f/);
  assert.match(spacing,/public const float BodyClearance = \.85f/);
  assert.match(crowd,/HarborCrowdSpacing\.YieldFactor\(/);
  assert.match(crowd,/Mathf\.MoveTowards\(a\.currentSpeed, target/);
  assert.match(crowd,/a\.time = Mathf\.Repeat\(a\.time \+ dt \* a\.currentSpeed \/ baseSpeed/);
  assert.match(crowd,/EntranceClear\(routes\[routeIndex\]\.At\(0f\)\)/);
  assert.match(crowd,/void OnDrawGizmosSelected\(\)/);
});

test('distant visitors maintain normal movement',()=>{
  const a={pos:p(0,0),heading:p(1,0),speed:1.05,index:0};
  const b={pos:p(6,0),heading:p(1,0),speed:1.05,index:1};
  assert.equal(referenceFactor(a,b),1);
});

test('a follower slows down, but the pedestrian ahead does not',()=>{
  const behind={pos:p(0,0),heading:p(1,0),speed:1.05,index:0};
  const ahead={pos:p(1,0),heading:p(1,0),speed:1.05,index:1};
  assert.ok(referenceFactor(behind,ahead)<.30);
  assert.equal(referenceFactor(ahead,behind),1);
});

test('one visitor yields at an approaching perpendicular crossing',()=>{
  const west={pos:p(-1,0),heading:p(1,0),speed:1.05,index:0};
  const south={pos:p(0,-1),heading:p(0,1),speed:1.05,index:1};
  assert.equal(referenceFactor(west,south),1);
  assert.ok(referenceFactor(south,west)<.4);
});

test('walkers moving away from each other do not brake unnecessarily',()=>{
  const west={pos:p(-1,0),heading:p(-1,0),speed:1.05,index:0};
  const south={pos:p(0,-1),heading:p(0,-1),speed:1.05,index:1};
  assert.equal(referenceFactor(west,south),1);
  assert.equal(referenceFactor(south,west),1);
});
