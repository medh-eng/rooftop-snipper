# LOG · Red Signal: Find the One

Newest first. Add an entry for every working session: what changed, why, what was verified, and what's next.

---

## 2026-11-23 · Redesign: "Find the One" (Claude)

**Branch:** `redesign/find-the-one`, based on `play/port-meridian-shift` (the newest Unity work).

### Why
Pon wanted the game to be more creative and addictive, less like a routine shooter. A review of the existing build found the core problem: targets wore red diamonds and **Find** centred the camera on them, so the only skill was tapping. The redesign makes deduction the game. Full reasoning is in [PLAN.md §1–2](PLAN.md).

### Added
* `game/case-engine.js`: deterministic case generator (mulberry32 PRNG), 7 observable traits, depth-based crowd construction that guarantees **exactly one** courier matches every clue, 12-case curriculum, scoring, Daily Case (number, weekday difficulty), spoiler-free share text.
* `game/index.html`, `game/style.css`, `game/game.js`: mobile-first browser prototype. Dossier drip and buy-intel, drag and lens, lens heat, mark suspects, noise and ping provocations with readable reactions (a "!" for those who flinch, red signal arcs for cell members), hold-to-steady tag (heartbeat, sway, ring lock, out of breath), line-of-fire blocking, two shots and a spooked courier, counter-sniper glints with an off-screen pointer, slow-motion reveal (letterbox, pulse trail), failure reveal, campaign menu with stars and unlocks, Daily Case with streak and share, coaching tips in case 1, WebAudio sound effects. Reuses the existing city plates and pedestrian sprites; the red armband on the old target sprite is desaturated at load so nobody is visibly marked.
* `tools/case-vectors.cjs` → `game/vectors.json`: reference cases used to keep the C# port identical to the JS engine.
* `tests/case-engine.test.cjs` (11 tests) and `tests/find-the-one-browser.cjs` (3 Playwright play-throughs).
* Unity:
  * `Assets/HarborWatch/Core/CaseEngine.cs` + `HarborWatch.Core.asmdef`: pure C# port (no UnityEngine).
  * `Assets/HarborWatch/Tests/Editor/CaseEngineTests.cs` + asmdef: 6 EditMode tests, including parity with `game/vectors.json`.
  * `Assets/HarborWatch/FindTheOne/CaseCrowd.cs`: persistent case people on the existing tested routes (chained by concourse side), jacket colour per outfit, caps or beanies, backpack, briefcase in the right hand, headphones, a phone prop; pace (jog = in a hurry), habits, noise and ping reactions via head-bone overrides after clip sampling.
  * `Assets/HarborWatch/FindTheOne/FindTheOneController.cs`: full mode (menu, campaign, brief, play, result, pause), camera from the observation roof with drag, pinch, wheel and lens follow, touch hit-testing, dossier, heat, provocations, hold-to-tag with occlusion, slow motion via `Time.timeScale`, counter-sniper, PlayerPrefs saves, Android share sheet, haptics.
  * `FindTheOneText.cs` (clue wording), `FindTheOneAudio.cs` (synthesized sound effects, no audio files).
  * `Assets/Scenes/FindTheOne.unity`: copy of the HarborMission setup with `findTheOneMode: 1`; added (enabled, last) to Build Profiles.
  * `Editor/FindTheOneBuildTools.cs`: *Harbor Watch → Find The One → Verify / Build Android Development APK* (FindTheOne first).
* `tools/core-tests/`: runs the Unity EditMode tests with `dotnet` and a tiny NUnit stand-in (NuGet was unreachable here; inside Unity the real NUnit runs the same file).
* `tools/unity-compile-check/`: compiles every Unity script against API stand-ins.
* CI: browser play-through step; new `unity-csharp` job (core tests + compile check).
* `PLAN.md`, `STATUS.md`, this `LOG.md`.

### Changed (existing files, kept minimal)
* `HarborCrowdTraffic.cs`: `Waypoints` and `Path` are now `internal` so CaseCrowd reuses the tested routes.
* `HarborCrowdReview.cs`: new `findTheOneMode` flag (builds the concourse and environment, then hands over to `FindTheOneController`), plus `Environment` and `SourceMaterial` getters. Ambience start moved into `StartAmbience()`. Mission, drone and review behaviour is unchanged. The exact line ChatGPT's tests match is preserved.

### Fixed (bugs in the existing Unity project that blocked Play mode)
* `HarborCrowdTraffic.cs`: `shoulder` declared twice in `CreateAccessories` (CS0136), introduced in commit 5e98100 today. Renamed the loop's variable to `strapShoulder`.
* `Editor/HarborMissionBuildTools.cs`, `HarborDroneBuildTools.cs`, `HarborEnvironmentBuildTools.cs`: `#if UNITY_EDITOR` without `#endif` (CS1027). Added the missing `#endif`.
  These were found by the new compile check; Unity would have reported them as compiler errors and refused to enter Play mode.

### Verified
* `node --test tests/*.test.cjs`: 53/53 (42 existing + 11 new).
* `node --test tests/find-the-one-browser.cjs`: 3/3 (Chrome; phone touch and desktop).
* `dotnet run --project tools/core-tests`: 6/6; C# output identical to JS for 6 PRNG seeds, 12 campaign cases and 3 daily cases.
* `dotnet build tools/unity-compile-check`: all runtime and Editor scripts compile.
* Screenshots reviewed: title, campaign, brief, play (portrait), lens, horn reaction, hold ring, slow-motion shot, result, ping reaction, counter-sniper glint, fail reveal, daily share.

### Not verified (needs Unity on Pon's PC)
* Running `FindTheOne.unity`: accessory fit, head-turn look, glint positions, UI scale, performance with 26 people. See STATUS.md "Needs checking".

### Next
1. Pon: playtest the browser build on a phone, then give it to 3–5 people without explaining it.
2. Pon: first Unity Play session on this branch; report Console errors or screenshots.
3. Tune from the feedback, then Phase C (real UI, animation, lens and post effects).


## 2026-11-23 (evening): crowd motion and props
Feedback from Pon after the first Unity play: movement not smooth when walking and turning, some people looked like they came through buildings, briefcase and phone size and animation looked wrong.

### Changed
* `CaseCrowd.cs`: smoothed heading, hidden off-set re-entry instead of U-turn and teleport, gentler speed ramps, gait freezes on planted-foot poses, smoothed head motion, two-bone reach IK for the carrying and phone arms, briefcase and phone rebuilt at real size and hung from the wrist.
* Ferry routes get a gangway centre-line waypoint (`GatedControls`). New `tools/route-check.cjs` (also in CI).
* `FindTheOneController.cs`: hidden people are ignored by tap, line-of-fire and selection; the courier is brought back before the reveal.
* Compile-check stubs extended (SmoothDampAngle, Quaternion.Slerp).

### Verified
Compile-check builds, core tests 6/6, `node --test` 53/53, route-check all routes clear.

### Not verified
Not run in Unity again. The arm IK and prop offsets need a look in Play mode (see STATUS.md).


## 2026-11-23 (night): Echo lens in the browser prototype
Next item from PLAN Phase D. Browser first, because it is the design lab and can be verified here; Unity port waits for your feedback on how it plays.

### Added
* `game/game.js`: rolling snapshot log of the crowd (12 per second, last 8 s). **Echo** replays it with the simulation frozen, then restores the live state exactly. Once per case, from case 4 (or a Daily with noise or ping), costs 6 s, refused with under 8 s left. Replay speed 1× or ½×, early exit, **E** key. TAG, provocations and selection are blocked during a replay. Blue tint and scan lines, "!" and earpiece cues, horn and surge rings rebuilt from the replay clock. Synthesised rewind and forward sounds.
* `game/index.html`, `game/style.css`: Echo tool button, replay bar (progress, time to live, speed, *Back to live*), How to play entry, brief line, "NEW: echo" tag on case 4. Tool buttons shrink to 50 px under 430 px wide so five fit in a row on a 390 px phone.
* `tests/find-the-one-browser.cjs`: Echo test (gating, cost, replay shows earlier positions, TAG blocked, exact restore, once only, auto-return).

### Fixed while testing
* Dossier countdowns jumped back during a replay because the HUD read the replay clock. They now use the live clock.

### Verified
`node --test tests/*.test.cjs` 53/53, browser tests 4/4 (Chrome, phone touch and desktop), route-check clear, offline build works. Screenshots checked at 390×844 (live, replay, replay at the horn moment, five tool buttons).

### Not verified
* Not played on a real phone. At 360 px wide the five buttons wrap to two rows (no overlap).
* Not in Unity (see STATUS "Echo lens").

### Next
1. Pon: play case 4 to 6 and a Daily on your phone and say whether Echo feels like a smart choice or a crutch. If it is too strong, raise `ECHO_COST` or shorten `ECHO_WINDOW`.
2. Then either port Echo to Unity or start the next Phase D idea (gadgets that add clue types: thermal, directional mic).


## 2026-11-24: Echo lens ported to Unity
Next step after the browser version. Same rules, so a case plays the same on both builds.

### Added
* `CaseCrowd.cs`: Echo section. 12 Hz ring buffer (last 8 s plus margin) of each person's pose state; `BeginEcho`, `ShowEcho(t)` (blends two snapshots: positions, heading, shortest-way gait phase, arm and head angles; flags from the nearer one), `EndEcho` (exact restore). Replay goes through `Pose()` with dt = 0, and a `replaying` flag makes the head use the recorded angles instead of snapping. The walker remembers the phone, glance and habit-phase flags it was last given.
* `FindTheOneController.cs`: `StartEcho`, `UpdateEcho`, `StopEcho`; ECHO button and replay bar; blue tint and scan lines; brief line; guards so TAG, HORN, PING, tap-select and buying intel are blocked during a replay (drag, pinch and lens stay free); Quit case ends a running replay first. When a case has five tools they shrink to fit beside TAG.
* `FindTheOneAudio.cs` (rewind and forward sounds), `FindTheOneText.cs` (How to play entry).

### Environment
* Installed the .NET 8 SDK in the session (`apt-get install dotnet-sdk-8.0`) so the compile check and core tests run again.

### Verified
`dotnet build tools/unity-compile-check` succeeded (0 errors), `dotnet run --project tools/core-tests` 6/6, `node --test tests/*.test.cjs` 53/53, route-check clear.

### Not verified
Not run in Unity. The compile check cannot catch wrong behaviour. See STATUS "Needs checking" for the Echo items to look at in Play mode.


## 2026-11-25: Unity would not start, case 4 not visible
Pon reported that in Unity the game did not start and case 4 could not be seen. There is no Unity Editor here, so the cause could not be reproduced; these are the problems found by reading the project, each fixed:
* **Case 4 locked by design.** `Unlocked(i)` needs stars on case i-1, so a fresh install could not reach case 4 or Echo. Editor and Development builds now open every case (`DevUnlock`, labelled "DEV: all cases open"); release builds keep the lock.
* **Wrong scene.** Build Settings listed `HarborMission` first and `FindTheOne` fifth, so File > Build ran the old scene. `FindTheOne` is now first. Added menu *Harbor Watch > Find The One > Open scene*.
* **Silent failure.** `Begin()` returned quietly when `CaseCrowd.Build` failed, so "Take position" did nothing. Starting a case, the attract-mode build and `Update` now catch errors and show them as a red bar with an OK button (also logged). 
* **No automatic check in Unity.** Added PlayMode tests (`Tests/PlayMode/FindTheOnePlayTests.cs`): load the scene, start every campaign case and the Daily, check the clock runs and there is no error, and check the Echo lens replays, costs 6 s, restores the crowd and refuses a second use. They use reflection because the controller is in Assembly-CSharp. Not run yet.
* Checked and fine: scene GUIDs and `findTheOneMode: 1`, Input System active (`activeInputHandler: 1`), asmdefs, all eight bones in both FBX files.
* Added `CLAUDE.md` (repo), `unity/HarborWatch/CLAUDE.md` and `docs/CLAUDE_CODE_PROMPTS.md` so Claude Code on Pon's PC can run Unity in batch mode, read the real errors and fix them in a loop.

Verified: compile check, core tests 6/6, JS tests 53/53 (two older scene-order tests updated: Find the One is now first in Build Settings), browser tests 4/4. Not verified: anything in Unity, including the new PlayMode tests.

Next: Pon runs prompt 1 of `docs/CLAUDE_CODE_PROMPTS.md` in Claude Code on the PC (pull the branch first).

## 2026-11-25 (later): repo cleanup, smoother walking, pedestrian model guide
### Removed
* The old marked-target browser game: root `index.html`, `style.css`, `src/`, its two tests (`campaign.test.cjs`, `campaign-browser.cjs`), the two offline build scripts and the CI step that ran them.
* Unused art (aircraft, gun, lens, cartridge, round, two air scenes), old phase docs (`docs/harbor-watch/`, `UNITY_HARBOR_WATCH_PLAN.md`), the Unity template junk (`TutorialInfo`, `Readme.asset`, `SampleScene` and its Build Settings entry).
* Generated Unity cache from the stray `unity/` project (commit eb00f5c) and rewrote `README.md` and `docs/START_HERE_NEW_CHAT.md`.
* Kept on purpose: the other Harbor scenes and scripts (mission, drone, relay, album) and their tests. `FindTheOneController` and `CaseCrowd` still depend on `HarborCrowdReview`, `HarborMissionController` and `HarborCrowdTraffic`, and removing them cannot be checked without Unity.
### Smoother walking
* Browser (`game/game.js`): people now ramp their speed (90 px/s^2 up, 200 down, 320 when startled) instead of changing speed in one frame, and the drawn side turns over about 0.25 s instead of flipping.
* Unity (`CaseCrowd.Step`): the crowd-spacing speed is low-passed (about 0.25 s) so close neighbours do not cause stop-start stutter. Heading, route curves and the gait phase were already smooth.
### Docs
* `docs/PEDESTRIAN_MODEL_GUIDE.md` (ChatGPT image prompts, Tripo settings, Mixamo settings, where to put files) and prompt 7 in `docs/CLAUDE_CODE_PROMPTS.md`.
### Verified
JS tests 44/44 (was 53; the 9 removed tests belonged to the old game), core tests 6/6, compile check 0 errors, route check ok, browser play-through 4/4.
### NOT verified
Nothing run in Unity. The smoother walking in Unity is untested by eye. The Mixamo model is not integrated: its bone names differ from the current rig.
### Next
Pon makes the model and sends the files (guide above), then Claude Code prompt 7. Pon checks in Unity that the walking looks smoother.
