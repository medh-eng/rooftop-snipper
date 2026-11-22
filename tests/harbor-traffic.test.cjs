const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const root = path.join(__dirname, '..');
const src = fs.readFileSync(path.join(root,
  'unity/HarborWatch/Assets/HarborWatch/HarborCrowdTraffic.cs'), 'utf8');
const block = src.slice(src.indexOf('static readonly Vector3[][] Waypoints'),
  src.indexOf('static Vector3[] Track('));
const routes = [...block.matchAll(/Track\(([^)]*)\)/g)].map(match =>
  match[1].replace(/\/\/[^\n]*/g, '').split(',').map(value =>
    Number(value.trim().replace(/f$/, ''))))
  .filter(numbers => numbers.length > 5)
  .map(numbers => {
    assert.equal(numbers.length % 2, 0);
    const points = [];
    for (let i = 0; i < numbers.length; i += 2)
      points.push({x: numbers[i], z: numbers[i + 1]});
    return points;
  });

const obstacles = [
  {name:'west pillar',x:-4.15,z:.7,rx:.35,rz:.35},
  {name:'east pillar',x:2.82,z:1.1,rx:.35,rz:.35},
  {name:'kiosk',x:-6.2,z:2.4,rx:.55,rz:.60},
  {name:'bench',x:5.5,z:-2.7,rx:.90,rz:.33},
  {name:'observation facade',x:-8,z:-5,rx:.55,rz:2.1},
  {name:'terminal west wing',x:-4,z:5.3,rx:2.1,rz:.75},
  {name:'terminal east wing',x:4,z:5.3,rx:2.1,rz:.75},
];

function lerpSample(controls) {
  const samples = [];
  for (let i = 0; i < controls.length - 1; i++) {
    const a = controls[Math.max(0, i - 1)];
    const b = controls[i], c = controls[i+1];
    const d = controls[Math.min(controls.length-1, i+2)];
    const segment = Math.hypot(c.x-b.x,c.z-b.z);
    const capped = p => {
      const magnitude = Math.hypot(p.x,p.z);
      return magnitude > segment
        ? {x:p.x/magnitude*segment,z:p.z/magnitude*segment}
        : p;
    };
    const left = capped({x:(c.x-a.x)*.5,z:(c.z-a.z)*.5});
    const right = capped({x:(d.x-b.x)*.5,z:(d.z-b.z)*.5});
    for (let j = 0; j < 12; j++) {
      const t = j/12, t2 = t*t, t3 = t2*t;
      const f1 = 2*t3-3*t2+1, f2 = t3-2*t2+t;
      const f3 = -2*t3+3*t2, f4 = t3-t2;
      samples.push({x:f1*b.x+f2*left.x+f3*c.x+f4*right.x,
                    z:f1*b.z+f2*left.z+f3*c.z+f4*right.z});
    }
  }
  samples.push(controls.at(-1));
  return samples;
}

function portal(p) {
  if(p.x < -11) return 'west street';
  if(p.x > 11) return 'east street';
  if(p.z > 7) return 'terminal';
  if(p.z < -8.5) return 'ferry';
  return null;
}

test('harbor walkers use authored original walk and jog gaits', () => {
  assert.match(src, /CrowdWalk/);
  assert.match(src, /CrowdJog/);
  assert.match(src, /NextVisible\(/);
  for (const filename of ['CrowdWalk', 'CrowdJog']) {
    assert.ok(fs.statSync(path.join(root,
      'unity/HarborWatch/Assets/HarborWatch/Resources', filename+'.fbx')).size > 100_000);
    assert.ok(fs.existsSync(path.join(root,
      'unity/HarborWatch/Assets/HarborWatch/Resources', filename+'.fbx.meta')));
  }
});

test('each path enters and leaves by different access points', () => {
  assert.equal(routes.length, 10);
  const origins = new Set(), destinations = new Set();
  for (const points of routes) {
    assert.ok(points.length >= 5);
    const source = portal(points[0]), destination = portal(points.at(-1));
    assert.ok(source, 'missing path entrance');
    assert.ok(destination, 'missing path exit');
    assert.notEqual(source, destination, 'route exits at its entry');
    origins.add(source);
    destinations.add(destination);
  }
  assert.equal(origins.size, 4);
  assert.equal(destinations.size, 4);
});

test('rounded pedestrian paths avoid the main static obstacles', () => {
  for (let index = 0; index < routes.length; index++) {
    const samples = lerpSample(routes[index]);
    for (const obstacle of obstacles) {
      const clearance = Math.min(...samples.map(p =>
        Math.hypot(Math.max(0,Math.abs(p.x-obstacle.x)-obstacle.rx),
                   Math.max(0,Math.abs(p.z-obstacle.z)-obstacle.rz))));
      assert.ok(clearance >= .39,
        'route '+index+' clips '+obstacle.name+' (clearance='+clearance.toFixed(2)+'m)');
    }
  }
});
