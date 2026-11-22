# Harbor Watch · natural pedestrian turns (Phase 2 revision)

The first Unity playtest was successful: the six pedestrians rendered and walked in the Game view. User feedback: **the square-corner turns take five small robotic steps and seem too slow**.

## Motion design revision

- Replaced five short in-place turning contacts with **three weight-bearing contacts** along a **0.62 m radius rounded path**.
- Each corner is approached directly after six regular strides, without the previous 18-frame standstill.
- Each turn distributes approximately 30 degrees of yaw per stepping contact, so the character rounds the corner rather than swivelling in place.
- The whole four-corner route mathematically closes; alternating feet are preserved across the four legs.
- Contact positions are reinitialised from the final frame before baking so the loop starts and ends with matching shoe positions.
- The original model, skeleton, clothing, scene, camera controls and Unity settings are unchanged.

## Asset build and verification

Edit source: `tools/harbor-watch/author_walker.py`.

The included `HarborWalker.fbx` is a **baked Blender-generated binary**. Changes to the source script alone do not change the walking clip used by Unity. The dedicated GitHub Actions workflow `rebuild-harbor-walker.yml` regenerates and pushes the FBX to the same feature branch when the script changes. If this action fails, rerun Blender locally after installing Blender:

```bash
blender --background --python tools/harbor-watch/author_walker.py
```

Then commit and push the updated `unity/HarborWatch/Assets/HarborWatch/Resources/HarborWalker.fbx`. The old `walk-preview.gif` remains evidence of the original animation until a new preview is captured.

## Acceptance checks on Unity

1. **Stop Play mode** before switching branches or pulling.
2. Pull the feature branch `chatgpt/harbor-crowd-phase2` and allow Unity to reimport `HarborWalker.fbx`.
3. Open `Assets/Scenes/HarborCrowd.unity` and press Play in **Game view**.
4. Select **Follow next** and observe one character through a corner. Count about three forward stepping contacts while the body smoothly curves through 90 degrees.
5. Slow down playback. Look for no visible teleport at the loop seam, a planted support foot, fluid hips, and consistent ground contact. Repeat for another character.
6. Switch Rooftop → Overhead; ensure the same pedestrians retain their positions. Test on Android separately.

**Important:** The mathematics of the arc was checked independently, but the updated Blender export and Unity/Android animation remain unverified until the CI job finishes and the Editor is tested. Do not mark the visual result as accepted based solely on the Python source update.
