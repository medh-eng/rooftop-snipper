# Harbor Watch — continuous pedestrian traffic review

This phase replaces the repeated six-person square loop **only inside the optional
`HarborCrowd.unity` scene**. The original `WalkReview.unity`, existing `HarborWalker.fbx`
and browser implementation remain available. The user previously approved the
smooth three-step turns and requested a living environment with arrivals,
departures and appearance variation.

## Implemented in code

- **Four scene entrances/exits**: west street, east street, ferry gangway,
  and the terminal's open centre doors
- **Ten separate curved routes**, with a destination different from the
  starting access point
- **Nine reused visitor instances**, initially six active; visitors travel
  beyond the scene edge, disappear, then reappear from another route after a
  short variable interval
- New, original, in-place FBX animation loops:
  `CrowdWalk.fbx` (24 frames at nominal 1.05 m/s) and `CrowdJog.fbx`
  (18 frames at nominal 2.05 m/s), produced by
  `tools/harbor-watch/author_commuter_gaits.py`
- Approximately **22% of arriving visitors jog**, with independent
  speed multipliers and phase offsets
- Different jacket/trouser/skin colours and optional cap, backpack or
  shoulder bag, made from lightweight Unity primitives
- Rooftop, overhead, follow-next, pause, quarter-speed and replay controls
  retained. The HUD shows live visitors, arrivals and departures
- West/east footpath extensions, a ferry gangway and a visible gap in the
  terminal doors match the routes
- No targeting, combat, or interaction with pedestrians; visual ambience only

## How to test in Unity

1. **Stop Play mode and close Unity**. In GitHub Desktop stay on
   `chatgpt/harbor-crowd-phase2`, Fetch origin then Pull origin. Stash any
   uncommitted Unity-generated changes first if GitHub Desktop asks.
2. Open the *existing* project at `unity/HarborWatch` in Unity
   **6000.3.28f1**, then open `Assets/Scenes/HarborCrowd.unity`.
3. In **Game** view, press Play and watch for at least **30–40 seconds**.
   Pedestrians should exit off one boundary and fresh visitors should enter
   from a different entrance. The arrivals/departures numbers should rise.
4. Use **Follow next** to check a walker and a jogger. They should advance
   along a curved path rather than walk in a square. Inspect shoe planting.
5. Look for occasional coloured hats, rucksacks or messenger bags. Confirm
   props move with heads or torsos without hovering.
6. Check the ferry path and terminal door openings. Check nobody visibly
   walks through the kiosk, pillars or bench.
7. Repeat on Android and check frame rate and animation; those tests are
   **not yet completed** from the repository environment.

If the crowd scene reports missing `CrowdWalk` or `CrowdJog`, right-click
each FBX under `Assets/HarborWatch/Resources` and choose **Reimport**.
The import script sets Legacy animation on both clips.

## Status and known limitations

- The Blender source exports both clips successfully in GitHub Actions.
- Static route checks assert four access points, ten distinct entrance-to-exit
  paths and a margin around key props.
- **Unity compilation and on-device playback of this revision have not yet
  been tested**. This is a candidate for visual acceptance, not a signed-off APK.
- The route paths are hand-authored and rounded, *not* a full NavMesh
  crowd simulation: pedestrians can still cross each other's paths.
- Props are stylised blockout meshes and accessories. Future polish may add
  better clothing silhouettes, more visitor body types, staggered waiting
  and dynamic collision avoidance after visual review.

## Phase 2.3 — local crowd yielding

Added following-distance and crossing-yield logic to reduce walkers moving through
one another. The visitor's route speed and gait playback speed are coordinated,
and new visitors wait briefly if their entrance is occupied. In Unity Scene
view, select the Main Camera to see route lines and personal-space rings.

See [PEDESTRIAN_SPACING.md](PEDESTRIAN_SPACING.md) for the specific acceptance
checks and limitations. **Not yet verified in the Unity Editor or Android.**
