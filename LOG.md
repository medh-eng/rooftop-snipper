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
