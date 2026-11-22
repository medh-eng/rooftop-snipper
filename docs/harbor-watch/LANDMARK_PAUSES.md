# Harbor Watch — landmark pauses (Phase 2.4)

## Purpose

The Phase 2 traffic test already has curved roads-to-terminal/ferry journeys,
clothing variants, joggers and local yielding. This increment makes **some
walkers** behave as visitors rather than continuous conveyor-belt traffic.

## Implemented on `chatgpt/harbor-crowd-phase2`

- Selected **walkers only** may make one brief **1.35–3.4 second** stop during
  a journey. Joggers do not stop for landmarks.
- Stops are positioned on four **existing route waypoints**, near the bench,
  ticket kiosk and open waterfront. Routes and exits are unchanged.
- As they approach, walkers progressively lower their travel speed. On stopping,
  the animation holds a contact pose, the root stays at the authored waypoint,
  and the head makes a small look towards the nearby landmark (maximum 36°).
- When the pause ends, visitors smoothly accelerate away and continue to the
  original route exit. They are still counted as active people, not despawned.
- Other people see a stationary visitor at **zero predicted speed** and can
  yield using the existing local crowd-spacing routine.
- The review HUD includes a live **Stopping** count.
- The original WalkReview scene, animation FBX assets, browser campaign, and
  Port Meridian route geometry have not been changed.

## Unity review

1. In GitHub Desktop, commit/stash local Editor changes first. **Fetch origin**
   then **Pull origin** on `chatgpt/harbor-crowd-phase2`.
2. Open `unity/HarborWatch` in **Unity 6000.3.28f1**. Open
   `Assets/Scenes/HarborCrowd.unity` and press Play.
3. Watch for **2–3 minutes** because pauses are deliberately intermittent.
   The HUD **Stopping** count should sometimes rise from 0 to 1 or higher.
4. Use **Follow next** near the kiosk or bench. The visitor should brake,
   briefly look around, and resume along the route; the feet must not cycle
   while stopped, or slide on restart.
5. Check the Arrivals/Departures counters keep rising. A stopped visitor
   should not permanently freeze everyone behind it.
6. Compare rooftop and overhead views, then test on Android if available.
   Capture the first Unity Console error if anything does not compile.

## Verification and remaining risk

`tests/harbor-landmarks.test.cjs` checks route markers and the stop-state
integration. This is a **source/geometry regression check, not proof of Unity
runtime correctness**. Actual gait poses, head-bone orientation, pedestrian
clearance and mobile performance still require an Editor/device review.

This is a small visual behaviour layer, not full pathfinding, queuing,
seating, pedestrian intent modelling or a natural idle motion-capture clip.
Bench/kiosk stops still occupy the walking path. If the visual check reveals
congestion, the next step should be dedicated side-bay routes and stronger
collision avoidance rather than adding more people.

## Next phase

After the crowd look and foot planting are approved in Unity, begin the
touch-friendly **non-violent** Harbor Watch mission view (lens, scanning
fictional marks, timer, restart and local progress save), then Android testing.
