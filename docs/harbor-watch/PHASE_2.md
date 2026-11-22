> **Natural turn refinement:** User review found the original five-step pivot robotic. See [THREE_STEP_CURVES.md](THREE_STEP_CURVES.md) for the rounded three-step revision, FBX rebuild and retest.

# Harbor Watch — Phase 2 crowd preview

## Scene and setup

This adds a **separate** phase-2 review scene without changing the approved source animation, original WalkReview scene, browser campaign, or Unity packages.

- Editor: **Unity 6000.3.28f1**
- Scene: `unity/HarborWatch/Assets/Scenes/HarborCrowd.unity`
- Driver: `Assets/HarborWatch/HarborCrowdReview.cs`
- Prerequisites: `Resources/HarborWalker.fbx` and `Resources/ReviewSurface.mat`
- Unity project root: `unity/HarborWatch`; do not open the repository root as the Unity project.

## What it shows

A procedural blockout of Port Meridian: tiled pavement, ferry terminal frontage, canopy, ticket kiosk, pier, rooftop vantage and two depth-occluding pillars. Six instances of the same original 14-bone commuter have different clothing colours, route centres, headings, playback speeds and gait phases. All six sample the existing baked, planted-foot clip. This **preserves the FBX's local foot-contact animation**; it does not yet provide individually authored routes or obstacle-avoidance navigation.

Use the six on-screen controls:

- **Rooftop**: wide diagonal view of the whole crowd
- **Overhead**: high view of the *same* animated crowd
- **Follow next**: close camera; tap repeatedly to cycle commuters
- **Slow / Normal**: quarter-speed animation or standard speed
- **Pause / Resume**: freeze and inspect a planted foot
- **Replay**: restart all six gait phases

The walking-review scene is still first in the Build Profiles scene list; the crowd scene is second and enabled. Open `HarborCrowd.unity` manually for Play-mode review. To launch the crowd scene first on Android, move it above `WalkReview.unity` in **File → Build Profiles → Scene List**, then Build and Run. Do not commit local build outputs.

## Device checks (not yet verified)

1. Open the crowd scene, press Play and look for six animated pedestrians with no Console errors.
2. Compare rooftop and overhead views; check the positions are identical when switching and that pillars conceal walkers at the correct depth.
3. Select Follow next for each commuter; watch both shoes at full zoom, particularly at a corner.
4. Pause on a planted heel, inspect its paving seam, and resume. Check for foot sliding, intersection with props or other pedestrians, and camera clipping.
5. Repeat these checks on Android; record the phone model, a 10–15 second capture and any first Console error.

## Known limitations / remaining milestones

This phase is **prepared for review, not certified complete**: no Unity Editor or Android runtime is available in the coding environment. Routes are transformed copies of the Phase-1 square route; destinations, believable queuing, pedestrian-to-pedestrian avoidance and doorway traversal still need Phase-2 refinement after the walking proof is approved. Do not mistake a character passing near the terminal for entering it.

After device/animation approval: refine routes and occlusion, then progress to the touch-driven non-violent mission interface, local save, camera tests, Android performance profiling and full campaign content. Only mark those deliverables complete after they are implemented and tested on device.
