# Harbor Watch · start here

## Current milestones

The Unity 6000.3.28f1 project is in [unity/HarborWatch](../unity/HarborWatch) on the `unity/harbor-watch` branch.

**Phase 1:** [WalkReview.unity](../unity/HarborWatch/Assets/Scenes/WalkReview.unity) includes the original 14-bone commuter, a baked planted-foot gait and [source-animation preview](harbor-watch/PHASE_1.md). Blender re-import was checked; Unity Editor playback and the Android device test are still pending.

**Phase 2 review candidate:** a separate [HarborCrowd.unity](../unity/HarborWatch/Assets/Scenes/HarborCrowd.unity) scene samples six variations of that gait in a blockout of Port Meridian with rooftop, overhead and follow cameras. See the [crowd review procedure](harbor-watch/PHASE_2.md). This is an implementation preview, not proof of individual pathfinding or device performance.

To test, pull the latest feature branch, open `unity/HarborWatch` from Unity Hub, then open `Assets/Scenes/HarborCrowd.unity`. Press Play and inspect the six pedestrians. For the initial build scene, `WalkReview.unity` remains first.

[Full development plan](UNITY_HARBOR_WATCH_PLAN.md) · [Project handoff](UNITY_PROJECT_HANDOFF.md)
