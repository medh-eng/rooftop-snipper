const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const read = name => fs.readFileSync(path.join(__dirname, '..', 'unity/HarborWatch/Assets/HarborWatch', name + '.cs'), 'utf8');
const mission = read('HarborMissionController');
const review = read('HarborCrowdReview');
const drone = read('HarborDroneReview');
const section = (source, start, end) => source.split(start)[1].split(end)[0];

test('briefing gates the survey clock and a legacy clear can continue to flight', () => {
  const start = section(mission, 'void Start()', 'void OpenFlight()');
  assert.match(start, /SaveCleared, 0\) == 1 \? 1 : 0/);
  assert.match(start, /GetInt\(SaveShift, previous\)/);
  assert.match(start, /showDispatch = true/);
  assert.match(mission, /return !showDispatch && !paused/);
  assert.match(mission, /checkpoint == Checkpoint.Flight\) OpenFlight\(\)/);
  assert.match(mission, /Time.frameCount == dispatchFrame/);
});

test('flight checkpoint is committed with the survey payout and the final report never pays twice', () => {
  const win = section(mission, 'void FinishMission(bool success)', 'void Update()');
  assert.match(win, /if \(!success\)[\s\S]*?return;[\s\S]*?SetInt\(SaveCredits/);
  assert.match(win, /checkpoint = Checkpoint.Flight;[\s\S]+SetInt\(SaveShift[\s\S]+PlayerPrefs.Save\(\)/);
  const report = section(mission, 'public void FileShiftReport()', 'Rect DispatchArea()');
  assert.match(report, /checkpoint = Checkpoint.Complete/);
  assert.doesNotMatch(report, /Settle\(|SaveCredits/);
});

test('the crowd scene enters the game and a successful flight returns camera control to the report', () => {
  assert.match(review, /PlayShiftArea\(\).Contains\(pointer\)[\s\S]*?LoadScene\("HarborMission"\)/);
  const flight = section(review, 'public void LaunchShiftFlight()', 'public bool IsShiftFlight');
  assert.match(flight, /mission.enabled = false;[\s\S]+AddComponent<HarborDroneReview>/);
  assert.doesNotMatch(flight, /ResetTraffic|LoadScene/);
  assert.match(drone, /identified && concourse.IsShiftFlight[\s\S]*?FinishShiftFlight\(\)/);
  assert.match(review, /droneReview.enabled = false;[\s\S]+mission.FileShiftReport\(\);\s+mission.enabled = true;/);
});
