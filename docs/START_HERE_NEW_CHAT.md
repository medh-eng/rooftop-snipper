# Start here (new chat)

**Current direction:** *Red Signal: Find the One* on branch `redesign/find-the-one`.
Read [PLAN.md](../PLAN.md), [STATUS.md](../STATUS.md) and the newest entry in
[LOG.md](../LOG.md) first. Rules live in `game/case-engine.js` and are mirrored in
`unity/HarborWatch/Assets/HarborWatch/Core/CaseEngine.cs`; keep them identical
(`node tools/case-vectors.cjs`, then `dotnet run --project tools/core-tests`).
Run `dotnet build tools/unity-compile-check` before committing Unity scripts.

Paste this into a new chat:

> Read PLAN.md, STATUS.md and LOG.md on the redesign/find-the-one branch of
> medh-eng/rooftop-snipper. Continue from the "Next" list in the newest LOG entry.

---

# Harbor Watch · start here

## Current milestones

Unity Editor **6000.3.28f1** project root: `unity/HarborWatch`.
The [Phase-2 feature branch](https://github.com/medh-eng/rooftop-snipper/tree/chatgpt/harbor-crowd-phase2)
contains the latest crowd scene. The original `unity/harbor-watch` branch
remains the PR base until acceptance.

**Phase 1:** `Assets/Scenes/WalkReview.unity` has the original rigged walker
and smooth, rounded-turn animation. The user has approved the improved turns
visually in Unity. Android verification remains pending.

**Phase 2:** `Assets/Scenes/HarborCrowd.unity` is a separate harbor preview
scene with dynamic street/terminal/ferry arrivals and departures, walkers and
joggers, variable clothing and optional hats/bags. The authored animation
exports and static route checks can be automated, but Unity playback of the
latest dynamic traffic remains for review. See
[traffic instructions](harbor-watch/CONTINUOUS_TRAFFIC.md).

To review: stop Unity Play, pull the latest feature branch in GitHub Desktop,
open `unity/HarborWatch` in Unity Hub, load `Assets/Scenes/HarborCrowd.unity`,
then press Play **in Game view** and watch for 30–40 seconds. Follow a walker
into/out of an entrance and inspect a jogger at close range.

[Development plan](UNITY_HARBOR_WATCH_PLAN.md) ·
[Unity handoff](UNITY_PROJECT_HANDOFF.md)
