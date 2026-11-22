# Harbor Watch — Rich Game & Long-Term Engagement Roadmap

**Version:** Planning draft 1.0 · 22 November 2026  
**Project:** `medh-eng/rooftop-snipper` · Unity project `unity/HarborWatch` · Unity 6000.3.28f1  
**Planning base:** `chatgpt/harbor-phase4-drone` (stacked after Phase 3 and Phase 2 PRs)  
**Vision:** A cinematic, replayable **urban observation and investigation game** for portrait Android. Play a satisfying 3–6 minute contract, make progress, discover a new story or landmark, and return for a meaningful new challenge. Fictional signals and drones are the interactive targets; civilians remain protected ambient characters.

## A. What is actually available / not available

| System | Repository state | Main gap |
| --- | --- | --- |
| Browser campaign | Nine mission definitions in three fictional districts, credits, four upgrade lines, gold relays, local save, browser tests | Different implementation from Unity, not automatically ported |
| Unity walking/crowd | One walking review, Port Meridian blockout, moving visitor routes, walk/jog, accessories, curved turns, yielding and landmark pauses | Unity Editor and Android runtime not verified on current feature branches |
| Phase 3 Unity mission | One separate playable 90-second Port Meridian fictional-signal survey, 350-credit start and 576 perfect first clear, persistent local credits, touch UI | C# compile/portrait usability and real scan playthrough need testing |
| Phase 4 Unity drone | Separate controllable aerial camera proof over the same crowd and harmless fictional survey drone | Camera controls, visibility and FPS need Unity/device verification |
| Production assets / full campaign | Browser illustrations and low-poly Unity primitives | No polished 3D art kit, final sound mix, nine-contract Unity port, reliable production APK |

**Do not merge or mark as shipped until** Phase 2 PR #1, Phase 3 PR #2 and Phase 4 PR #3 pass runtime review in order. Retain the approved `WalkReview` and `HarborCrowd` scenes.

## B. Design pillars

1. **Living observation spaces:** pedestrians follow believable daily routines; vehicles, boats, birds, weather and buildings make each area feel inhabited.
2. **Mission variety:** rotate detection, tracking, decoding, exploration, escort-by-observation and aerial survey. No procedural ballistics or real-world tactics.
3. **Discoverable stories:** 3-act structure with distinct characters and districts, opt-in dialogues, clues, city mysteries and memorable finale.
4. **Skill mastery, not grind:** optional objectives, grading, clean-completion records, weekly challenges and collectibles; no forced waiting or energy limits.
5. **Portrait-first quality:** clear silhouettes, large touch targets, responsive controls and stable performance on mid-range Android phones.

## C. The gameplay loop

**Moment-to-moment (10–90 s):** read a hint → choose rooftop/drone view → inspect the living space → identify/scan fictional signal → get readable audiovisual feedback.

**Short session (3–6 min):** choose a contract → perform 2–4 varied objectives → receive stars, credits, progress, and a clue/collectible → choose replay or next contract.

**Medium-term (30–90 min):** unlock a new district block, scanner tool, visual style, character encounter or storyline chapter. Completion unlocks optional harder variants.

**Long-term (days/weeks):** a rotating daily challenge; a themed weekly set; achievement book, district exploration album and ranked personal bests. Time-limited content should be fair, optional and have no fear-of-missing-out pressure.

## D. Mission archetypes and replay parameters

### Five launch-ready archetypes

| Type | Core play | Meaningful variation |
| --- | --- | --- |
| Signal survey | Locate 3–5 fictional relay devices, obtain clear scan, verify location | Signal placement, environmental occlusion, angle and light |
| Observation / follow | Observe a marked fictional **device carrier**, without interacting with civilians; identify the correct handoff point | Route timing, crowd density, optional observation vantage |
| Decode relay | Inspect two or three clues and solve a simple pattern or matching puzzle | Puzzle seed, signal colour/symbol sequence, clue order |
| Aerial reconnaissance | Fly drone through a district, photograph landmarks, identify aerial signal | Route/landmark set, weather, altitude constraints |
| City event response | During festival, ferry arrival or temporary power outage, finish a sequence of harmless objectives | Event timeline, location, objective order, allowed time |

### Expansion archetypes (after first phone-tested vertical slice)

- Photo/documentary missions: collect images of 3 architectural landmarks at the correct time of day.
- Hidden beacon hunts: search broad areas with directional signal strength, not explicit map pins.
- Multi-view investigations: combine rooftop clues with drone photographs to resolve a fictional mystery.
- Escort monitoring: monitor a VIP arrival and confirm route checkpoints without targeting pedestrians.
- Narrative missions: choose an observation priority; branching briefings alter optional rewards or story flavour.

### Replay modifiers

Mix 1–2 modifiers at a time, not random chaos: dusk/dawn, light rain/clear, pedestrian density, limited scan power, fewer hint pings, moving signal, different entry point, optional challenge timer, and an extra hidden photo collectible. Every generated combination must be solvable and tested. Use deterministic seeds to reproduce bugs and share a daily challenge.

## E. Three visually distinct districts

### 1. Port Meridian — dawn harbor (build first)

**Environment:** harbour water, ferry departure/arrival, pier mooring, cranes and shipping containers, mist bands, terminal windows, rooftop AC units, benches, street food cart, flags, streetlights and different paving/material zones.

**Living-world motion:** pedestrians with umbrellas or bags, natural group arrivals, small queue at ticket kiosk, occasional joggers, birds, moving ferry on a contained path, 1–2 distant service vehicles. Add one visible scheduled event (ferry departs, terminal doors light up).

**Lighting/audio:** warm morning key light, contact shadows, restrained water highlights, distant harbor horns, gulls, crowd hubbub, footsteps, scanner feedback.

**Gameplay:** tutorial district; 6 meaningful contract variants. Unlock roof/drone switches and the first collectible album.

### 2. Vesper City — rainy neon metropolis

**Environment:** stylised wet road surfaces, city canyons, overpasses, rooftop billboards, illuminated shop windows, puddles and low-cost reflection tricks, occasional trains and street traffic. Use signage in original fictional language/brand art.

**Atmosphere:** cool violet/blue light, amber storefronts, rain streaks, selective neon bloom, traffic ambience and wet footsteps. Provide graphics setting to disable expensive reflections/bloom.

**Gameplay:** occlusion and multi-view observation, signal decoding, blackout events and moving relays.

### 3. Solara — golden historic quarter

**Environment:** warm sandstone, domes, terraces, roof gardens, awnings, courtyards, open market, cloth movement and patterned walls.

**Atmosphere:** orange-hour shadows, heat haze only on high quality, birds, bells and human market chatter.

**Gameplay:** landmark photography, hidden-path clue trails, festival monitoring and rooftop-to-drone finale.

**Style rule:** grounded stylised realism, clear silhouettes and low-cost atmospheric detail. One coherent look across all districts; do not mix photoreal characters with primitive block buildings in the final art.

## F. Character and environment polish

- **Pedestrian diversity:** 8–12 base silhouettes with palette/texture combinations; different heights, silhouettes, headwear and carried accessories. Idle/look/phone/point, seated, short jog, entrances/exits, turning and queueing. Avoid clipping and keep consistent contact with pavement.
- **Vehicles and boats:** simple looped low-poly vehicle paths with traffic signals, and one ferry with a dock/undock cycle. Add collision-aware crowd paths only after visual review.
- **Environmental storytelling:** kiosks with props, advert boards, signs, open/closed shop states, pigeons and seagulls, rooftop details, weather props and small animated objects.
- **VFX:** restrained scan pulse, signal response, camera zoom vignette, win ribbon, particles near water; use readable FX without obscuring civilians.
- **Audio:** separate ambience/music/UI channels, gentle original instrumental tracks, variation by district, distance-based harbour and city sounds, individual volume toggles and accessibility mute.
- **UI:** compact mission briefing, clear reticle, objective checklist, countdown, scan indicator, challenge progress, result report, collection album, map, progress ribbon, upgrades, settings and reviewable tutorial prompts.
- **Accessibility:** text size controls, high-contrast marker setting, colour-independent symbols, reduced motion, left-handed mode, adjustable joystick sensitivity, subtitles and no forced audio cues.

## G. Progression and reason to return

### Campaign v1 proposal

**24 handcrafted contracts**: 8 per district, across the five archetypes. Each contract has a clear unique mechanic or story beat; do not manufacture 24 near-identical scans. Three main acts with a visible investigation board and a short final chapter.

**Three-star scoring:** finish, meet precision/quality threshold, complete a no-error or optional objective. Make the highest star optional; avoid punishing casual users. Unlock the next contract on normal success rather than perfect completion.

**Progression currency:** credits earned from missions/challenges. Upgrade scanner clarity, lens, drone battery/comfort and signal decoding aids, with a limited, balanced progression curve. Cosmetics are non-power alternatives for long-term collectors.

**Collections:** 30–50 optional landmark photographs, achievement badges and a district completion gallery.

**Daily / weekly (after core quality):** daily set of 3 objectives using a deterministic seeded pool, a weekly 5-contract challenge board and records for personal best. No always-online requirement for solo daily/weekly missions.

**Optional online later:** fair leaderboard with anti-cheat, opt-in nickname, privacy and a suitable backend. Do not make the first release depend on multiplayer or social services.

**No dark patterns:** no forced timed energy, loot boxes, mandatory ads or streak loss penalties. Offer useful rewards for meaningful play, not login-only compulsion.

## H. Practical Unity architecture

1. `MissionDefinition` ScriptableObjects: ID, district, objectives, weather, duration, payout, variation seed, tutorials, required assets.
2. One `MissionDirector` orchestrates objective state, timer, grading and completion; isolate input, camera, crowds, markers and persistence.
3. `Objective` interfaces/components: scan, observe, decode, photograph and identify.
4. `DistrictScene` is shared between rooftop and drone camera modes; never duplicate crowds or run two separate world simulations.
5. `CrowdDirector` uses pooled visitor prefabs, paths, identity/style variation and activity schedules with a clear performance budget.
6. `SaveRepository` uses a versioned local save, integrity checking and migration tests; keep browser storage separate. Prevent double rewards on retry/resume.
7. `ChallengeGenerator` uses deterministic seeds, valid objective locations and reproducible test fixtures. Author mission-safe spawn zones rather than arbitrary free placement.
8. `SettingsService` manages audio, render quality, text scale, sensitivity, reduced motion and haptics.
9. Only later, when district scale warrants, use Addressables for modular scenery and audio; avoid migrating every existing asset before the first good APK.
10. Preserve Unity `.meta` files in Git and keep Library/Temp/Logs/build binaries out of source control. Every gameplay PR should add logic tests and a Unity/manual device acceptance checklist.

## I. Android performance and release gates

**Target device class:** mid-range Android, portrait. Measure actual handset rather than assuming 60 FPS.

| Gate | Proposed threshold / evidence |
| --- | --- |
| Stage 0 baseline | Compile Phase 2→3→4 in Unity 6000.3.28f1; no Console errors, functional touch input; at least one installable development APK |
| Motion | Correct foot contact, no visible snap at corners; 60 s crowded play at close camera |
| Mobile rendering | Aim for stable 30 FPS in busy scene after 10-min warm-up; test low/medium/high presets |
| Frame pacing | Target <33.3 ms per frame at 30 FPS; log p95 frame time, CPU, GPU and thermal behaviour |
| UI | All controls within portrait safe area, one-handed access where possible, readable text/markers |
| Memory and load | Measure peak usage and cold-start level load on test handset; define budget from measured baseline before expanding districts |
| Mission correctness | No impossible objective seeds; full success/fail/retry/save/reload tests; first-perfect payout consistent with 576-credit benchmark |
| Regression | All source and browser checks green and a manual Unity/Android replay capture for each changed scene |

**Optimisation approach:** URP Forward path on mobile; keep lighting economical, bake static scenery, use LOD groups for buildings/crowds, object pooling, atlased materials, profiling-driven batching/instancing and conservative transparent VFX. Check expensive rain, reflections, bloom, shadow distance and full-screen post effects on phone before enabling them by default.

## J. Milestones, scope and decisions

| Milestone | Proposed deliverables | Acceptance / exit |
| --- | --- | --- |
| **0. Stabilise** | Validate stacked PRs 1–3, Unity C# compilation, on-phone control and 30 FPS baseline, build/debug scripts | First repeatable Unity Android APK and recorded device result |
| **1. Premium Port Meridian vertical slice** | 1 polished harbour art kit, daylight/weather, realistic crowd quality, 3 different objective archetypes, clear UI/audio | 15-minute playtest with varied objectives, no major obstruction/animation issue |
| **2. Progression and narrative** | Data-driven mission director, versioned save, upgrade UI, 8 harbour missions, story briefing and one city event | All 8 complete without duplicate rewards; save/reload reliable |
| **3. Additional districts** | Vesper and Solara kits, lighting/weather, 8 missions each, landmark collection | 24 unique contracts in 3 cities; device quality gate in each |
| **4. Retention systems** | Daily seeded contracts, weekly challenges, achievement book, cosmetic unlocks and optional personal records | No impossible seeds; reward economy stays balanced; offline functional |
| **5. Release candidate** | Optimisation, accessibility, onboarding, tutorial, QA, graphics presets, store metadata/privacy, no errors | Full Android test matrix, a signed release build, tested migration and player feedback |

**Recommended immediate next work:** Milestone 0, then Milestone 1's **one polished Port Meridian vertical slice**. Do not simultaneously implement three cities, twenty-four missions, co-op, cloud multiplayer and high-end weather before the first Android proof.

### What not to build yet

- Multiplayer/PvP, full open world, realistic weapon systems/ballistics, physics-driven traffic or detailed interiors for every building.
- High-resolution photoreal character packs before a coherent low-cost mobile art direction is chosen.
- Huge randomised levels before hand-authored missions are genuinely fun.
- Monetisation or leaderboards without data privacy, stability and actual retention evidence.

## K. Measurement / iteration

Instrument anonymous opt-in events: tutorial_start/completed, mission_start/outcome, difficulty setting, objective type, scan attempts, mission time, restart, level load time, selected camera mode and coarse performance. Never log exact user addresses, camera contents or personal messages.

**Initial design targets**, not predictions: tutorial completed in 3–5 minutes; standard mission 3–6 minutes; 10–15-minute voluntary play sessions; first-release storyline with several hours of varied content. Track retention D1/D7/D30 after a small test cohort, without treating any benchmark as a guarantee.

Test early with **5–10 people** using their own Android phones, watch the first 15 minutes without guiding them, and fix obvious confusion before increasing content volume. Experiment with objective mix and hints only after playtesting.

## L. Source context

- [Original Unity phases](../UNITY_HARBOR_WATCH_PLAN.md)
- [Phase 2 crowd](CONTINUOUS_TRAFFIC.md), [landmark pauses](LANDMARK_PAUSES.md)
- [Phase 3 mission](PHASE_3_MISSION.md), [Phase 4 drone](PHASE_4_DRONE.md)
- [Unity 2026 game development report](https://unity.com/resources/gaming-report)
- [Unity 6000.3 URP mobile rendering paths](https://docs.unity.com/en-us/engine/6000.3/manual/render-pipelines/universal-render-pipeline/introduction/urp-concepts/rendering-paths/comparison)
- [Unity profiling guidance](https://unity.com/how-to/best-practices-for-profiling-game-performance)
- [GameAnalytics 2026 benchmarks](https://www.gameanalytics.com/reports/2026-mobile-pc-gaming-benchmarks)

This document is a **plan**, not a claim that features beyond the existing Phase 2–4 prototypes are already implemented.
