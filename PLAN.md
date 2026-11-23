# PLAN · Red Signal: Find the One (mobile)

**Final product:** a Unity Android (later iOS) game.
**Working title:** *Red Signal: Find the One*. "Harbor Watch" stays the Unity project name.
**Last updated:** 23 Nov 2026.

See [STATUS.md](STATUS.md) for what works today and [LOG.md](LOG.md) for what changed and why.

---

## 1. Why the redesign

The first version was a shooting gallery. Every target had a red diamond over its head and a **Find** button centred the camera on it. The only skill left was tapping a moving icon. That design has no tension, no decision and no reason to replay.

**The new core idea: the game no longer tells you who the target is.** You get a dossier of clues, a crowd of people with their own looks and habits, and one clean shot. Being a sniper becomes watching, deducing and choosing the moment.

## 2. The core loop (one case, 60–100 s)

```
OBSERVE  ->  PROVOKE  ->  DEDUCE  ->  COMMIT (one shot)  ->  SLOW-MOTION REVEAL  ->  SCORE + SHARE
```

| Mechanic | What it does | Why it hooks |
|---|---|---|
| **Dossier drip** | Intel arrives one line at a time (first line free, then every ~10–13 s). Tap a locked line to buy it now (−8 s). | **Patience vs greed.** Tagging early multiplies the score (+250 per unread line), so every case asks "am I sure enough?" |
| **Unique solution** | The generator guarantees exactly one person matches every line, with near-miss decoys that match all but the last. | Fair, so a mistake feels like *your* mistake. |
| **Provocations** | **Noise** (horn / bell / neon surge): everyone stops and looks, except the trained courier *and anyone wearing headphones*. **Ping**: every Red Signal cell member touches an earpiece, accomplices included. Each costs points. | You *act on* the crowd instead of just watching it. Headphones and accomplices are fair red herrings. |
| **Lens heat** | Zooming builds heat. When it maxes, your glint warns the courier: they start running and you have 15 s. | Stops "zoom forever" play and creates time pressure. |
| **Hold to steady** | Select a person, hold TAG: the ring tightens and your heartbeat plays. Release once it locks. Hold too long and you run out of breath. Passers-by block the line. | Physical tension right before every decision. |
| **Two shots** | A wrong tag spooks the courier: one more shot and 15 s left. | Second chances without removing the stakes. |
| **Counter-sniper** (later cases) | A glint appears on a rooftop. Tap it within 4.5 s or lose 10 s. | Breaks tunnel vision and adds the "hunter becomes hunted" feeling. |
| **Slow-motion reveal** | Letterbox, time at 0.12–0.15×, pulse trail, "COURIER TAGGED" or "CIVILIAN". On failure the courier is shown with the full dossier. | The dopamine moment, and players learn from every loss. |
| **Daily Case** | One seeded case per day, identical for every player on browser and Android. Share text is spoiler-free (🟥⬛⬛ intel used, time, stars). | The daily habit and word-of-mouth engine. Cheap to build because cases are generated. |

**Tone and store-safety:** the "shot" is a fictional tracking pulse that tags the courier for ground teams. There are no wounds or deaths. That keeps the age rating low and stays consistent with the non-violent Unity mission ChatGPT built.

## 3. Content plan

**Campaign:** 12 cases in 3 districts. Each case introduces at most one new idea.

| # | Case | District | New idea |
|---|---|---|---|
| 1 | First Light | Port Meridian | Dossier, select, tag (with coaching) |
| 2 | Ferry Rush | Port Meridian | Habits (phone, glancing back, loitering) |
| 3 | Horn Blast | Port Meridian | **Noise** provocation, headphones red herring |
| 4 | Last Sailing | Port Meridian | 4-line dossier, pace (strolling / hurrying) |
| 5 | Neon Static | Vesper City | **Ping** provocation, cell accomplices |
| 6 | Night Market | Vesper City | Both provocations |
| 7 | Rooftop Glint | Vesper City | **Counter-sniper** |
| 8 | Blackout | Vesper City | 5 lines, everything combined |
| 9 | Bell Tower | Solara | Church bell and pigeons |
| 10 | Market Day | Solara | Several accomplices |
| 11 | Procession | Solara | Dense crowd (24) |
| 12 | Last Signal | Solara | 6 lines, 26 people (finale) |

**Daily Case difficulty** follows the weekday: Monday is gentle (14 people, 3 lines); Sunday is hardest (24 people, 5 lines, counter-sniper).

## 4. Architecture

```
game/case-engine.js                     <- the rules (deterministic, seeded)
        | ported 1:1, checked by game/vectors.json
unity/HarborWatch/Assets/HarborWatch/Core/CaseEngine.cs   (pure C#, asmdef, no UnityEngine)
        |
FindTheOne/CaseCrowd.cs        persistent 3D people (looks, habits, reactions) on the tested routes
FindTheOne/FindTheOneController.cs   camera, input, dossier, provocations, heat, tag, UI, save, share
Scenes/FindTheOne.unity        same concourse and lighting as HarborMission; HarborCrowdReview.findTheOneMode = 1
```

* **Same seed means the same case on every platform.** The JavaScript engine generates `game/vectors.json`. The Unity EditMode tests (`Tests/Editor/CaseEngineTests.cs`) must reproduce it exactly, so the Daily Case is fair everywhere.
* **The browser build (`game/`) is the design lab.** Tune mechanics there in minutes, then port them. It is also a free web demo or marketing page.
* **CI** (`.github/workflows/check-game.yml`) runs the JS tests, the browser play-throughs, the C# case-engine tests and a compile check of every Unity script.

## 5. Roadmap to a store release

Each phase ends with a check **you** do on your phone.

### Phase A · Prove the fun (browser) — *done, needs your playtest*
* [x] Shared engine, 12-case campaign, Daily Case, share text
* [x] Browser prototype with every mechanic in §2
* [ ] **You:** play cases 1–6 and today's Daily on your phone. Give it to 3–5 people **without explaining it**. Watch: do they understand the dossier? Do they say "one more"?
* [ ] Tune timings from that feedback (reveal interval, heat, crowd size)

### Phase B · Unity vertical slice (Port Meridian) — *code written, needs your PC*
* [x] Core C# engine and EditMode tests (pass outside Unity)
* [x] `FindTheOne.unity`, CaseCrowd, controller, IMGUI UI, save, share, haptics, synthesized SFX
* [x] Fixed 4 compile errors in ChatGPT's code that blocked Play mode
* [ ] **You:** pull the branch, open Unity 6000.3.28f1, run *Harbor Watch → Find The One → Verify*, open `FindTheOne`, press Play
* [ ] **You:** Test Runner → EditMode → Run All (6 tests)
* [ ] Fix whatever the first Play session shows. Likely spots: accessory fit (briefcase hand, beanie, headphones), glint positions, UI scale on your phone. See STATUS "Needs checking"
* [ ] Android development APK through *Harbor Watch → Find The One → Build Android Development APK*. Record phone model and FPS with 26 people

### Phase C · Make it beautiful and readable on a phone
* Replace IMGUI with UI Toolkit or uGUI (proper fonts, animation, safe areas)
* Real head-turn, look-up and phone-raise animation clips (Mixamo or authored) instead of bone nudges
* Lens shader (vignette, chromatic edge) and a bullet-time post effect
* Audio: real ambience per district, a heartbeat that layers with tension, music stingers
* Performance: aim for 30 FPS with 26 people on a mid-range Android. Options if short: animation LOD, GPU skinning, crowd cap for low-end devices

### Phase D · Content and retention
* Build **Vesper City** and **Solara** 3D sets (the browser has the art references). Until then Unity plays every case in Port Meridian
* **Echo lens** gadget: rewind the last 8 s of the crowd (the simulation is deterministic, so a snapshot ring buffer makes it cheap)
* Gadgets as unlocks that add new clue types instead of +18% stats: thermal (who carries a device), directional mic (subtitled whispers), drone (look behind pillars)
* Weekly "Most Wanted" boss case: multi-stage, the courier swaps jackets at a stall
* **Optional, strong identity:** Indian city districts (Mumbai CST rush hour, Varanasi ghats at Dev Deepawali, a Nagpur market). A **Holi level** where colour powder makes clothing clues useless, so only behaviour counts
* Streaks, rank titles, cosmetic scopes. Leaderboard for the Daily Case (needs a small backend)

### Phase E · Release
* Play Console internal testing track for your own loop, then **closed testing with at least 12 testers opted in for 14 days in a row**. Google requires this before production access for personal developer accounts created after 13 Nov 2023; organisation accounts are exempt. Recruit 15–20 to allow for drop-outs ([Google's rule](https://support.google.com/googleplay/android-developer/answer/14151465))
* Store listing: 30 s trailer of a slow-motion reveal, screenshots of the dossier and provocations
* Privacy: no account and no ads at first, so the data-safety form is simple
* Monetisation later: optional cosmetic scopes, a case-pack DLC, or a rewarded ad for one extra intel line (never pay-to-win on the Daily)

## 6. Rules for whoever builds next (you, Claude or ChatGPT)

1. **Change rules in `game/case-engine.js` first**, run `node tools/case-vectors.cjs`, then mirror the change in `Core/CaseEngine.cs`. `node --test tests/*.test.cjs` and `dotnet run --project tools/core-tests` must both pass.
2. Run `dotnet build tools/unity-compile-check` before every Unity commit.
3. Don't put back visible target markers or a "Find target" button: deduction is the game.
4. Keep the tag non-lethal.
5. Update STATUS.md and append to LOG.md in the same commit.
