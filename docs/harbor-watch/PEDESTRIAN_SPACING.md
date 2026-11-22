# Harbor Watch — pedestrian yielding and spacing (Phase 2.3)

## Why this change exists

The original Phase 2 crowd has a successful walking/turning review, but the ambient
routes can cross one another. Before building more environment detail, visitors
should keep a believable interval instead of walking through each other.

## Behaviour added

- **Following distance:** A visitor approaching the back of someone on the
  same path slows down; acceleration is gradual once the space opens up.
- **Crossing anticipation:** A short ~1.35 second lookahead predicts when two
  paths may approach one another within 0.85 m.
- **Deterministic yield:** At a crossing, the higher-numbered pooled visitor
  slows first. This avoids pairs simultaneously deciding to stop forever.
- **Smooth cadence:** Movement speed and animated gait speed change together
  so feet do not continue running at full rate while their root is stopping.
- **Entrance spacing:** If a new walker would appear too close to an existing
  visitor, that arrival is deferred briefly rather than spawned on top of them.
- **Performance:** All calculations are lightweight Vector3 operations over
  only nine pooled pedestrians; speed buffers are reused to avoid allocations
  every frame. No new animation, model, renderer or third-party package.

This uses **simple local yielding**, not a complete NavMesh collision-avoidance
simulation. It stays on the ten previously validated curved routes. Opposing
visitors sharing the same narrow path may still appear closer than desired, and
real collisions and frame-rate must be tested in Unity.

## How to review in Unity

1. Close Unity Play mode and commit or stash local editor changes.
2. In GitHub Desktop select `chatgpt/harbor-crowd-phase2`, Fetch/Pull.
3. Reopen the existing `unity/HarborWatch` project in Unity 6000.3.28f1.
4. Open `Assets/Scenes/HarborCrowd.unity`, press Play in Game view.
5. Watch at least 60 seconds. Check several people following one another or
   crossing paths. Does one slow briefly then resume instead of passing through?
6. Press Follow next, then Slow. Inspect the shoes during deceleration and
   while the pedestrian restarts walking or jogging.
7. Look for the Arrivals/Departures counters increasing normally — yielding
   must not freeze the entire crowd or block terminal and ferry entrances.
8. Try on the intended Android phone; check whether it stays near the 30 FPS
   target after ten minutes. No phone performance claims are made here.

If Unity reports the first Console error, record that error and the script line.
The automated GitHub tests are source/geometry checks, not Unity compilation.

## Accepted work kept intact

- Scene `HarborCrowd.unity` and its existing six buttons
- `WalkReview.unity` and the approved three-step turning clip
- The CrowdWalk/CrowdJog FBXs, accessories and ten entrance-exit routes
- The original browser campaign on `main`

## Remaining after this review

If motion is visually sound, continue with a more believable harbor setting,
walking destinations, occasional pauses near benches and kiosks, ambient sound,
and Android profiling. Full crowd avoidance or collision-free behavior may
require Unity AI Navigation and a later device-tested implementation.
