# STATUS · Red Signal: Find the One

**Last updated:** 23 Nov 2026 · branch `redesign/find-the-one` (from `play/port-meridian-shift`)
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
* **Case:** dossier drip with tap-to-buy intel, drag to look, Lens (zoom with heat), Mark suspects, Noise and Ping provocations, hold-to-steady TAG with heartbeat and line-of-fire blocking, two shots, a spooked courier after a wrong tag, counter-sniper glints, slow-motion reveal.
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
| Old scenes | `HarborMission`, `HarborCrowd`, `HarborDrone` | Still open and play as before (only a new mode flag was added) |

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
| `node --test tests/find-the-one-browser.cjs` (needs Playwright) | Phone touch selection, hold-to-tag win, wrong tags, daily determinism and share | 3 / 3 pass |
| `dotnet run --project tools/core-tests` | Unity EditMode case-engine tests incl. JS parity | 6 / 6 pass |
| `dotnet build tools/unity-compile-check` | Every Unity script compiles (runtime + Editor) | Build succeeded |
| GitHub Actions `check-game.yml` | All of the above on each push | Added in this branch |

## Single-file build (for phones and playtesters)

`node tools/build-game-offline.cjs Red-Signal-Find-the-One.html` packs the browser game, artwork and fonts into one ~4 MB HTML file. Open it from Files or Downloads on a phone (or send it to playtesters). The generated file is not committed.
