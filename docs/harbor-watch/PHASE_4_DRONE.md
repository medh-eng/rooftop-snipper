# Harbor Watch — Phase 4: aerial camera proof

**Repository:** `medh-eng/rooftop-snipper`  
**Branch:** `chatgpt/harbor-phase4-drone`  
**Unity project:** `unity/HarborWatch` (Editor **6000.3.28f1**)  
**Review scene:** `Assets/Scenes/HarborDrone.unity`

## What is implemented

Phase 4 contains a dedicated **non-violent, portrait-friendly aerial observation**
scene. It reuses the exact same Port Meridian blockout and the same nine
pooled pedestrians from the approved crowd implementation. It does not
spawn a copy of the crowd or alter the existing browser campaign.

- **DRONE / ROOFTOP:** toggles between a freely moving drone camera and the
  original rooftop viewpoint. Visitors continue their journeys when the camera
  changes. The drone retains its position, heading and zoom while on the roof.
- **FLY stick (left thumb):** move forward, backward, sideways and diagonally.
  Velocity accelerates and stops smoothly, without a sudden snap.
- **Right-thumb drag:** aim camera independently of horizontal flight.
- **UP (HOLD) / DOWN (HOLD):** change camera altitude.
- **ZOOM + / ZOOM -**, **RESET**, **PAUSE / RESUME** and **FIND** for quickly
  locating the fictional aerial survey signal.
- The little stylised **unregistered survey drone** glides on a short orbit
  over the harbor. Centring it and pressing **IDENTIFY** records observation
  only; no weapons, collisions, projectiles or damage.
- The camera stays inside the harbor's visible bounds and above pedestrian
  level, with a simple safety volume around the observation roof. Navigation
  round this roof reveals different views of the **same** pedestrians and
  actual geometry occlusion.
- Mobile portrait is selected at scene start. The scene uses procedural
  Unity primitives only, with no external models or music.

This is a **drone camera proof**, not the sixth campaign contract, not a
production flight simulator, and not an Android performance certificate.

## How to review in Unity Editor

1. Commit or stash local Unity changes. In GitHub Desktop choose
   `chatgpt/harbor-phase4-drone`, Fetch origin, then Pull origin.
2. Open the existing Unity folder `unity/HarborWatch` in Unity Hub, using
   **6000.3.28f1**. Do not create another project.
3. Choose **Harbor Watch → Verify Phase 4 Drone** and inspect the Console.
4. Open `Assets/Scenes/HarborDrone.unity`, press **Play** in **Game** view.
5. Fly left/right in front of the terminal, then move around the observation
   building. Check that the kiosk and pillars occlude the same walking
   characters when the camera moves.
6. Tap **ROOFTOP**, observe the crowd, and tap **DRONE** again. Positions
   should have advanced continuously, and the drone should be where it was.
7. Hold **UP** and **DOWN** while flying. The camera must stay above the
   pedestrians and not enter the roof geometry. Check that it cannot fly
   outside the district.
8. Tap **FIND** to turn toward the small fictional drone; centre the signal
   and press **IDENTIFY**. Verify the success text. This action should never
   affect a person or building.
9. Test **PAUSE / RESUME**, **RESET**, zoom, and two-thumb input for at least
   one minute. Record the first Unity Console error if any.

### Keyboard / mouse for quick Editor testing

| Control | Action |
| --- | --- |
| W A S D | Move on the horizontal plane |
| Q / E | Descend / ascend |
| Arrow keys | Rotate the view |
| Drag mouse | Look, or move stick when dragging inside it |
| Tab | Switch rooftop / drone |
| F | Look toward the fictional aerial signal |
| Space | Identify if centred and unobstructed |
| P | Pause |
| R | Reset |
| Mouse wheel | Drone camera zoom |

## Standalone Android review APK

Choose **File → Build Profiles → Android → Switch Platform** and make sure
Android Build Support (SDK, NDK, OpenJDK) is installed for your Unity Editor.
Then choose **Harbor Watch → Build Drone Review APK** and select an APK
destination. This menu deliberately builds **only** `HarborDrone.unity`,
without changing the normal scene order: `HarborMission.unity` remains
first in the standard application build.

**Note:** Both development APKs currently use the app ID
`com.medhaneeka.harborwatch`. Installing the drone review build can
replace the first-mission development build on the phone, not your other
Unity projects. You can rebuild the mission APK afterwards.

### Android acceptance checks

- Portrait safe areas: camera controls and floating labels do not overlap
  the device notch, gesture zone or one another.
- Two fingers independently fly and look. Ascend/descend holds work.
- Drone stays outside the observation roof on low passes.
- Switching roof/drone shows the same crowd in different positions, with
  correct depth occlusion and no duplicated rigs.
- The on-screen signal can be identified after moving into clear view.
- Busiest view: aim for 30 FPS after ten minutes on the target Android
  handset. Record the model, FPS, first Console error, and any stutter/heat.

## Verification / limitations

`tests/harbor-drone.test.cjs` validates the scene wiring, pure flight
bounds, movement directions, mobile gesture integration and build script.
GitHub CI also runs the existing browser and crowd regression suite.

**GitHub source tests cannot compile or render Unity scenes.** This branch
still needs Unity 6000.3.28f1 compilation, camera/marker visual review,
portrait device interaction and ten-minute Android profiling before approval.
The blocker around the observation roof is intentionally simple, and does not
replace a full collision system or an aerial physics model.

## Dependencies and sequencing

- [Phase 2 crowd PR #1](https://github.com/medh-eng/rooftop-snipper/pull/1)
- [Phase 3 playable mission PR #2](https://github.com/medh-eng/rooftop-snipper/pull/2)
- Phase 4 stacks on Phase 3 until those unmerged PRs are visually approved.

**Next:** Unity/Android runtime checks, mobile performance tuning, then the
remaining districts and contracts. Do not call a Play Store APK finished until
an actual Android build is tested on-device.
