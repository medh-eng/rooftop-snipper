# STATUS · Red Signal: Find the One

**Last updated:** 23 Nov 2026 (evening, Echo lens) · branch `redesign/find-the-one` (from `play/port-meridian-shift`)
Plan: [PLAN.md](PLAN.md) · History: [LOG.md](LOG.md)

## At a glance

| Part | State | Verified how |
|---|---|---|
| Shared case engine (JS) `game/case-engine.js` | ✅ Done | 11 Node tests: one courier for 400 seeds × 12 cases and 366 daily cases |
| Browser prototype `game/` | ✅ Playable | Headless Chrome play-throughs at phone (390×844, touch) and desktop size, 3 automated browser tests |
| Case engine (C#) `Core/CaseEngine.cs` | ✅ Done | 6 EditMode tests pass outside Unity; output matches JS bit for bit (`game/vectors.json`) |
| Unity Find the One mode (`FindTheOne.unity`, `CaseCrowd`, `FindTheOneController`) | 🟡 Written, **not yet run in Unity** | Compiles against Unity API stand-ins (`tools/unity-compile-check`). Needs your first Play session |
| ChatGPT's existing Unity scenes | 🟢 Compile errors fixed | 4 errors would have blocked Play mode (see LOG). All 42 of its Node tests still pass |
| Vesper City / Solara 3D sets | ⬜ Not started | Unity plays every case on the Port Meridian concourse for now |
| Echo lens (browser) `game/game.js` | ✅ Playable | Browser test: gated to case 4+, −6 s, replays earlier crowd, exact restore, once per case. Screenshots at phone size |
| Unity start-up fixes (25 Nov) | 🟡 Written, **not yet run in Unity** | Reported: case 4 not visible, game not starting. Fixes: all cases open in Editor/dev builds, FindTheOne first in Build Settings, red on-screen error bar, PlayMode smoke tests |
| Echo lens (Unity) | 🟡 Written, **not yet run in Unity** | Compiles against Unity API stand-ins. Needs your Play session (see "Needs checking") |
| Android APK | ⬜ Not built | Use *Harbor Watch → Find The One → Build Android Development APK* |

## How to play it now

**Browser (any phone or PC):** open `game/index.html` from a checkout, or serve the repo folder (`python3 -m http.server 8000`, then open `http://localhost:8000/game/`). After this branch is merged into `main`, GitHub Pages should serve it at `https://medh-eng.github.io/rooftop-snipper/game/` (Pages is enabled on the repo; the URL is unconfirmed).

**Unity (PC):**
1. GitHub Desktop: fetch, switch to branch `redesign/find-the-one`, pull.
2. Open `unity/HarborWatch` in Unity **6000.3.28f1**. Wait for import and check that the Console shows no red errors.
3. Menu **Harbor Watch → Find The One → Verify**.
4. Open **Assets/Scenes/FindTheOne**, press **Play**, set the Game view to a portrait phone size (e.g. 1080×2400).
5. **Window → General → Test Runner → EditMode → Run All** (6 tests should pass).

## What's in the game today

* **Menu:** Daily Case (status and share), Campaign (12 cases, stars, unlocks), How to play, sound toggle.
* **Case:** dossier drip with tap-to-buy intel, drag to look, Lens (zoom with heat), Mark suspects, Noise and Ping provocations, hold-to-steady TAG with heartbeat and line-of-fire blocking, **Echo lens (case 4 onward)**, two shots, a spooked courier after a wrong tag, counter-sniper glints, slow-motion reveal.
* **End:** score breakdown (bounty, early-tag bonus, time, costs), 1–3 stars, failure reveal showing the courier and the full dossier, spoiler-free daily share text (Web Share / clipboard in the browser; Android share sheet in Unity).
* **Saves:** best stars and score per case, daily results and streak (browser `localStorage`, Unity `PlayerPrefs`).

## Needs checking on first Unity run (I could not run Unity here)

| Item | Where | What to look for |
|---|---|---|
| Briefcase fit | `CaseCrowd.BuildAccessories` | Hangs from the right hand (`Forearm.R` + 0.225 m). If it floats, adjust that offset |
| Beanie and headphones | same | Sit on the head and are visible through the Lens at FOV 21 |
| Head turns | `CaseCrowd.Step` | Noise: heads turn towards the ferry (`NoiseSource`). Glance: an 80° look back. Phone: head tips down |
| Counter-sniper spots | `FindTheOneController.GlintSpots` | Glints should appear on rooftops or cranes. Move the five points if they're inside walls |
| UI scale | `Styles()` scale `u` | Text readable on your phone; buttons not overlapping the safe area |
| Performance | 26 skinned people in case 12 | Target 30 FPS on your phone. Record the model and FPS in LOG.md |
| Echo lens | `CaseCrowd` Echo section, controller `StartEcho` | Play case 4, wait about 6 s, tap ECHO: people should walk their last 8 s again without foot sliding, then return to exactly where they were. Check: phone and briefcase arms, heads at the horn moment, nobody pops in or out at the edges, 5 buttons fit beside TAG on case 6 and later, ECHO bar readable on your phone |
| Old scenes | `HarborMission`, `HarborCrowd`, `HarborDrone` | Still open and play as before (only a new mode flag was added) |

## Crowd motion and props (reworked 23 Nov, after the first Unity play)
Pon's feedback: walking and turning not smooth, some people seemed to come through buildings, briefcase and phone wrong in size and motion. Changes in `FindTheOne/CaseCrowd.cs`:
* Turning: heading is smoothed (SmoothDampAngle) and follows the path 0.9 m ahead. Before, people spun on the spot at route ends and snapped to the tangent.
* Route ends: people now walk off the set, wait 0.5-1.7 s hidden and re-enter facing inwards (no U-turn, no teleport). The courier is hidden for the shortest time. Hidden people cannot be tapped or block the line of fire.
* Speed ramps are gentler (1.1 m/s² up, 2.2 down). When stopping, the gait finishes its step and freezes on a planted-foot pose instead of snapping.
* Buildings: `tools/route-check.cjs` tests every route against every solid object. Route 9 grazed the pier railing (0.04 m); ferry routes now pass a waypoint on the gangway centre line. All routes are now at least 0.5 m clear (body radius 0.3 m). It runs in CI.
* Briefcase: real size (42 x 30 x 8.5 cm), hangs from the hand upright, swings a few degrees like a pendulum, arm held by two-bone reach IK. It is no longer glued rigidly to the forearm.
* Phone: real size (7 x 14.5 cm), held in the hand. Arm raises smoothly (0.35 s), screen faces the face when reading, flat against the ear on the ping with a red blinking back light. Carriers of a briefcase use the other hand.
* Head turns and tilts are smoothed instead of snapping.

Still to check in Unity: the IK finds the forearm axis automatically (no hand bone in CrowdWalk.fbx). If a phone or case sits a few centimetres off the grip, adjust `Arm.ForeLength` clamp in `MakeArm` or the `grip` / `reading` / `atEar` offsets in `Pose`.

## Echo lens (browser, 23 Nov)
From case 4 (and on any Daily with noise or ping) the **⏪ Echo** button replays the last 8 s of the crowd. It exists because the best clues (who didn't flinch at the horn, who touched an earpiece) last about 2 s and are easy to miss.
* Once per case, costs 6 s of the clock, not available with under 8 s left. Key: **E**.
* The live simulation is frozen while it plays and restored exactly afterwards, so it cannot be used to cheat time. TAG, Noise, Ping and selecting people are blocked during a replay; dragging and zooming are free (lens heat does not build).
* Plays at 1× or ½×, or tap *Back to live* to stop early. Replay is tinted blue with scan lines; reactions ("!", earpiece blink) and the horn or surge rings are rebuilt from the replay clock.
* Rules unchanged: `case-engine.js`, scoring and the C# port are not affected (the cost is only time, which the existing time bonus already counts).
* Tuning knobs at the top of `game.js`: `ECHO_WINDOW`, `ECHO_COST`, `ECHO_FROM_CASE`.
* **Unity port (24 Nov):** `CaseCrowd` records each person's full pose state 12 times a second (route position, heading, gait phase, arm weights, head angles, phone and glance flags) and replays it through the same `Pose()` code with dt = 0; `EndEcho()` restores the live state exactly. `FindTheOneController` adds the ECHO button (case 4 onward), a replay bar (progress, time to live, 1x / 1/2x, LIVE), blue tint with scan lines, and the same rules as the browser (once per case, -6 s, refused under 8 s left, no TAG, HORN, PING or selecting while it plays). Constants: `EchoCost`, `EchoFromCase` in the controller, `EchoWindow`, `EchoHz` in `CaseCrowd`.

## Known limitations

* Unity UI is IMGUI (functional, plain). Phase C replaces it.
* Unity clue wording for outfits uses jacket colours ("Charcoal jacket"); the browser uses the photo outfits ("Dark hooded jacket"). Same underlying case, different words.
* The browser uses the old photo-sprite pedestrians, so accessories (caps, bags, headphones) are drawn as overlays. They read well at lens zoom but are stylised.
* Daily Case uses the device's local date; a player in another time zone can be one case ahead or behind.
* No backend: no leaderboards yet.

## Tests and checks

| Command | What it covers | Result (23 Nov) |
|---|---|---|
| `node --test tests/*.test.cjs` | ChatGPT's 42 checks + 11 case-engine checks | 53 / 53 pass |
| `node --test tests/find-the-one-browser.cjs` (needs Playwright) | Phone touch selection, hold-to-tag win, wrong tags, daily determinism and share, Echo lens | 4 / 4 pass |
| `dotnet run --project tools/core-tests` | Unity EditMode case-engine tests incl. JS parity | 6 / 6 pass |
| `dotnet build tools/unity-compile-check` | Every Unity script compiles (runtime + Editor) | Build succeeded |
| GitHub Actions `check-game.yml` | All of the above on each push | Added in this branch |

## Single-file build (for phones and playtesters)

`node tools/build-game-offline.cjs Red-Signal-Find-the-One.html` packs the browser game, artwork and fonts into one ~4 MB HTML file. Open it from Files or Downloads on a phone (or send it to playtesters). The generated file is not committed.
