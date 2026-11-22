const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const traffic = fs.readFileSync(path.join(root,
  'unity/HarborWatch/Assets/HarborWatch/HarborCrowdTraffic.cs'), 'utf8');
const review = fs.readFileSync(path.join(root,
  'unity/HarborWatch/Assets/HarborWatch/HarborCrowdReview.cs'), 'utf8');

function floats(src) {
  return src.split(',').map(s => Number(s.trim().replace(/f$/i, '')));
}
const block = traffic.slice(traffic.indexOf('static readonly Vector3[][] Waypoints'),
  traffic.indexOf('static Vector3[] Track('));
const routes = [...block.matchAll(/Track\(([^)]*)\)/g)].map(m => floats(m[1]))
  .filter(a => a.length >= 10)
  .map(a => {
    const result = [];
    for (let i = 0; i < a.length; i += 2) result.push({x:a[i], z:a[i+1]});
    return result;
  });
const stops = [...traffic.matchAll(/new LandmarkStop\(\s*(\d+)\s*,\s*new Vector3\(([^)]*)\)\s*,\s*new Vector3\(([^)]*)\)/g)]
  .map(m => {
    const [x, , z] = floats(m[2]);
    const [targetX, , targetZ] = floats(m[3]);
    return {route:Number(m[1]), point:{x,z}, target:{x:targetX,z:targetZ}};
  });
const distance = (a,b) => Math.hypot(a.x-b.x,a.z-b.z);

test('landmark stops are on the existing rounded routes, not inside props', () => {
  assert.equal(routes.length, 10);
  assert.equal(stops.length, 4);
  assert.equal(new Set(stops.map(s => s.route)).size, stops.length);
  for (const stop of stops) {
    assert.ok(stop.route >= 0 && stop.route < routes.length);
    assert.ok(Math.min(...routes[stop.route].map(p => distance(p, stop.point))) < .01,
      'landmark is not near a route waypoint: ' + stop.route);
    assert.ok(distance(stop.point,stop.target) > .8 &&
              distance(stop.point,stop.target) < 6,
      'look target is not a nearby landmark: ' + stop.route);
    assert.ok(distance(stop.point,{x:-6.2,z:2.4}) > .9,
      'stop is inside the kiosk');
    assert.ok(distance(stop.point,{x:5.5,z:-2.7}) > 1.2,
      'stop is inside the bench');
  }
});

test('stops only happen on walking journeys and are cleared for each arrival', () => {
  assert.match(traffic, /if \(!a\.jogging && random\.NextDouble\(\) < \.72\)/);
  assert.match(traffic, /a\.stopDistance = -1f/);
  assert.match(traffic, /a\.resting = false/);
  assert.match(traffic, /a\.stopCompleted = false/);
  assert.match(traffic, /a\.stopDistance >= 0f && !a\.stopCompleted/);
  assert.match(traffic, /a\.distance = a\.stopDistance/);
  assert.match(traffic, /a\.remainingStop = a\.stopDuration/);
  assert.match(traffic, /a\.stopCompleted = true/);
});

test('resting people stop both their feet and root, without leaving the crowd', () => {
  assert.match(traffic, /if \(a\.resting\)\s*\{\s*a\.currentSpeed = 0f/);
  assert.match(traffic, /walk\.SampleAnimation\(a\.entity, 0f\)/);
  assert.match(traffic, /a\.head\.localRotation \*= Quaternion\.Euler/);
  assert.match(traffic, /plannedSpeeds\[i\] = 0f; continue/);
  assert.match(traffic, /float neighbourNominal = b\.resting \? 0f : b\.currentSpeed/);
  assert.match(traffic, /Mathf\.Sqrt\(2\.1f \* remaining\)/);
  assert.match(review, /stream\.RestingCount/);
});
