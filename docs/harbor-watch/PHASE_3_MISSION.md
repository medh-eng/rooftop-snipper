# Harbor Watch — Phase 3: playable Port Meridian survey

**Status:** implemented in Unity source; GitHub source tests are available.
**Not yet signed off:** Unity Editor compilation, Android runtime performance,
and a recorded end-to-end playthrough on the target handset.

## What is playable

The new first build scene is `Assets/Scenes/HarborMission.unity` in the Unity
project `unity/HarborWatch`. It reuses the original Port Meridian concourse,
its nine recycled pedestrian rigs, entrances/exits and smoother turning.

This first contract is intentionally a **non-violent signal survey**. Three
fictional stationary relay devices are marked `SIGNAL 1–3`. The player observes
from the rooftop and scans the devices; pedestrians are neither targets nor
interactive. This is separate from the browser arcade and does not alter it.

- **Drag** to pan the rooftop camera; **ZOOM + / ZOOM -** adjust the lens.
  Mobile users may also **pinch** to change zoom.
- Align a relay marker with the central **+** reticle, then tap **SCAN**.
  A scan without a relay centred records a missed attempt but has no effect
  on people or the environment.
- **PAUSE / RESUME** freezes the 90-second timer and background traffic.
- **RESTART** gives a fresh 90-second run and restores the three devices.
- A successful first run saves the wallet (starting at **350 credits**), first
  clear flag and best star count via Unity `PlayerPrefs`.
- With three successful scans in three attempts and a chain of three, the
  browser contract's ideal first-run reward is **576 credits**, raising the
  fresh wallet to **926 credits**. Replays pay a reduced bounty.
- The result panel displays success, credits and stars, or a timeout with a
  restart option. Save occurs on success, not on an unsuccessful attempt.

## Desktop or Android test

1. In GitHub Desktop fetch/pull the `chatgpt/harbor-phase3-mission` branch.
   Commit or stash local Unity changes before switching. **Do not delete your
   existing project.**
2. In Unity Hub open the project at `unity/HarborWatch` with Editor
   **6000.3.28f1**. If Unity asks, import assets normally.
3. Open `Assets/Scenes/HarborMission.unity` and press Play in **Game** view.
4. Test three successful scans in 90 seconds. Confirm the first payout is
   576 credits, wallet becomes 926 when starting from a fresh save, and the
   stars/result panel show. Click RESTART and win again to check the smaller
   replay bounty. Exit Play and enter again: credits must be restored.
5. Test a missed scan, pause/resume, touch drag/pinch, zoom controls, timer
   expiration, and restart. Watch pedestrians entering and leaving the scene.
6. Switch to portrait Android Build and Run and check button size, safe area,
   input, visibility of the three signals, 30 FPS target, sound/heat and
   no Console errors. Actual Android testing **requires your phone/Unity**.

### Testing a fresh wallet

Unity PlayerPrefs persist outside Play Mode. To validate the 350-to-926
first-clear path after you have already completed it, use Unity's PlayerPrefs
clearing mechanism for the test environment, or use a clean test application
identifier/profile. **Clearing PlayerPrefs removes other local test saves**;
only do so when you no longer need the previous data. The browser save is
separate and is not touched by this Unity mission.

## Technical notes and limits

`HarborMissionRules.cs` holds scoring, and
`HarborMissionController.cs` owns the state, touch controls, camera, UI,
relay models and save. `HarborCrowdReview.cs` only adds an opt-in
`missionMode` that is enabled in the new scene. The approved
`HarborCrowd.unity` and `WalkReview.unity` scenes remain buildable.

Three stylised devices are created with Unity primitives and the original
ReviewSurface material, with no third-party assets or licence changes.
`tests/harbor-mission.test.cjs` checks the source wiring, payout and
scene metadata, but it **cannot compile or exercise Unity C#**. The camera and
signal geometry, UI readability and performance must be reviewed in Unity.

### Remaining before an APK can be called finished

- First local Unity compilation and visual review, especially marker placement,
  foot planting near stops, phone touch accuracy and lens view in portrait.
- Android build/deployment and ten-minute device profiling.
- Later game phases: drone camera, richer mission content and all remaining
  contracts. None of these later phases is claimed complete here.

## One-click local development APK (new)

The Unity Editor now has **Harbor Watch → Verify Phase 3 Mission**. This
checks that the mission is the first build scene, both locomotion clips
imported, the reward rule pays 576 credits, and Android has the intended
application ID.

Once verification succeeds, switch the active build platform to **Android**
in **File → Build Profiles**, then choose
**Harbor Watch → Build Android Development APK** and select a destination.
This performs a **development/debug** Android build and reveals the APK on
success. The Android SDK/NDK/JDK modules and a working local Unity Editor are
still needed. **No APK is generated by the GitHub changes alone.**

The app now uses the distinct Android ID `com.medhaneeka.harborwatch`, not
the Unity template ID. This means a previously installed template app will
not be updated in-place; Harbor Watch installs as a separate application.
Development builds are not Play Store release packages.
