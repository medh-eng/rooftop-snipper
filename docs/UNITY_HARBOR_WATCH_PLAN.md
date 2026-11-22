# Harbor Watch in Unity

The first deliverable is one playable Android APK of **Harbor Watch**. Its most important test is a pedestrian taking a convincing step when the rooftop lens is zoomed in. Build that walk first, use it for a small crowd, then check the same people from a drone camera.

## GitHub and Unity on your PC

Use this repository as the shared source. GitHub Desktop keeps a local copy on your PC. Unity Hub opens a folder in that copy, and the Unity Editor saves scenes and builds the APK there. A Git push shares your edits with ChatGPT Work. A pull brings my edits back to your Editor.

| Location | Purpose |
| --- | --- |
| `main` | Playable browser campaign and this plan |
| [`unity/harbor-watch` branch](https://github.com/medh-eng/rooftop-snipper/tree/unity/harbor-watch) | Shared working branch for the Android prototype. The first contribution is a Git ignore file for Unity's generated files. |
| `unity/HarborWatch/` on that branch | Unity project folder that your installed Editor creates in Phase 0 |

### Your first session

1. **GitHub Desktop.** Clone `medh-eng/rooftop-snipper`, or open your existing clone. Click **Fetch origin**, then **Current branch** and select `unity/harbor-watch`. Pull if an update is available.
2. **Unity Hub.** Choose **New project**, select your installed Editor and its **Universal 3D** template. Set **Project name** to `HarborWatch` and **Location** to the clone's `unity` folder. For example, a clone at `C:\Games\rooftop-snipper` gives a project at `C:\Games\rooftop-snipper\unity\HarborWatch`. If you already created a Unity project specifically for Harbor Watch, close it, copy its `Assets`, `Packages`, and `ProjectSettings` folders with their contents into that destination, then choose **Projects → Add → Add project from disk**. [Unity Hub](https://docs.unity.com/en-us/hub/project-manage) · [Universal 3D template](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/creating-a-new-project-with-urp.html)
3. **Unity Editor.** Let the first import finish, press **Play**, check the Console for compilation errors, and save. Use **Visible Meta Files** and **Force Text** serialization for Git. [Unity asset metadata](https://docs.unity3d.com/6000.0/Documentation/Manual/AssetMetadata.html)
4. **GitHub Desktop.** Commit the project as `Create Harbor Watch Unity project`, then **Push origin**. Include `Assets/` with Unity's `.meta` files, `Packages/manifest.json`, the generated `Packages/packages-lock.json`, and `ProjectSettings/`. The latter records your exact Editor version in `ProjectVersion.txt`. The branch ignore rules keep import caches and builds on your PC. [Unity package lock file](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-conflicts-auto.html)

For later sessions, **pull → open Unity → test → save → commit and push**. Finish or commit local edits before switching branches. Work reads the pushed commit, adds the next change on the prototype branch, and gives you the commit to pull.

## Which ChatGPT mode to use

| Choose this | Give it this work |
| --- | --- |
| **Regular Chat · GPT-6 Instant** (low effort) | A quick Unity menu question or an explanation of one message. |
| **Regular Chat · GPT-6 Medium** | Review a short sequence of walking screenshots. Ask which shoe slides, when the weight shifts, and whether the turn looks natural. You make the artistic decision on the phone. |
| **Regular Chat · GPT-6 High** (optional) | Compare two animation approaches when the choice is still unclear. Give it the same walk and turn recorded from the same camera with each approach. |
| **Work / Codex · GPT-6.2 Sol · Medium** | Default for work in GitHub. Implement C# features, wire mission data, write tests, and make routine fixes. |
| **Work / Codex · GPT-6.2 Astra · High** | The walking controller, a reproducible animation or camera defect, and Android performance diagnosis from a Profiler capture. |

These are recommended assignments. If your picker shows effort levels only, use **Work Medium** for routine implementation and **Work High** for the walking proof and difficult debugging. One Work thread can follow the plan through successive commits. [OpenAI model selection](https://developers.openai.com/api/docs/guides/model-selection) · [Thinking levels in Chat](https://help.openai.com/en/articles/20001354-gpt-6-and-other-models-in-chatgpt)

## Phases and responsibilities

Phase 0 is ready for the PC handoff. The Unity walk, scenes, and APK are upcoming deliverables. The browser game already supplies the nine mission definitions, four upgrade tracks, reward rules, and examples of expected outcomes. [Campaign data](../src/campaign.js) · [Rules tests](../tests/campaign.test.cjs)

| Phase | My task in Work | Your task on PC or phone | Ready to advance when |
| --- | --- | --- | --- |
| **0 · First project** | **Medium.** Prepare the branch, then check the first Unity commit for the Editor version, package lock and project layout. | Complete the four steps above. Select Android as the build target and use Build and Run to open the template scene on your phone. Record the phone model. | The pushed project opens in the recorded Editor, enters Play mode and launches on the test phone. |
| **1 · One convincing walker** | **High.** Supply one rigged test character and compatible motion clips that we can share in the repository. Build a tiled test scene. Blend idle, walk, stop and turn. Match stride to distance travelled, with one system controlling forward motion. | Open the scene. Watch from rooftop height and at full lens zoom. Record a 10–15 second walk, stop and turn on the phone. Consecutive screenshots also work. | Both feet take a turn bearing weight. A planted shoe stays on the same paving joint while the body advances. The character slows, turns and starts again smoothly. You approve the movement before we duplicate the walker. |
| **2 · Port Meridian and its crowd** | **Medium.** Build a ferry concourse with a rooftop, pillars and a terminal entrance. Give six civilians different destinations, speeds and starting strides. Add rooftop and overhead test cameras to the same scene. | Compare both views on the phone. Follow one person past a pillar and another into the terminal. | Pedestrians pass each other, walk behind objects at the correct depth and keep their positions when the camera changes. |
| **3 · Playable Harbor Watch** | **Medium.** Implement touch pan, lens, red markers, arcade pulses, civilian protection, timer, restart, rewards and a local save. Port the first mission's rules into Unity tests. | Play a clean run, deliberately touch a civilian, let the timer expire, retry, and reopen the app after a win. | Six civilians and up to two active marks share the scene. Clear three marks in 90 seconds. A perfect first run awards **576 credits** and brings a fresh wallet from **350 to 926**. Reopening restores that balance. |
| **4 · Drone camera proof** | **Medium.** Add a controllable drone test mode over the same harbor and a fictional rogue drone. Use **High** if camera movement, building occlusion or touch input needs deeper debugging. | Fly around one building, return to the concourse and check the controls in portrait. | Moving around the building reveals a new view of the same moving crowd. The test view is available to the team. Players unlock drone missions at contract six in the full campaign. |
| **5 · Android test APK** | **High** for profiling and fixes, **Medium** for packaging. Review device captures, tune the measured bottleneck, and record the tested commit and build steps. | Build and Run on your phone. Play for ten minutes, including the busiest view, zoom, drone mode, pause and resume. Record the phone model, frame rate and any repeatable hitch. | An installable APK completes Harbor Watch on your target phone. Aim for a steady **30 FPS** in the busy scene and check the walk again after the device warms up. [Android build guide](https://docs.unity3d.com/6000.0/Documentation/Manual/android-BuildProcess.html) |
| **6 · Full campaign** | **Medium** for content and tests, **High** for defects spanning multiple features. Add Vesper City, Solara, the remaining contracts and the rooftop-to-drone finale. Port the projector, lens, cartridge and pulse-round upgrades, gold relays and district caches. | Approve each district on the phone, replay early levels with upgrades and choose the release build. | All nine contracts, unlocks, purchases and saved progress pass on Android. Bring the tested Unity folder into `main` through a pull request. |

## Assets and handoffs

- **First asset request.** One rigged humanoid, an idle, a looping walk, a stop, and a left and right turn. Use a model and clips suitable for source redistribution in this public repository and record their origin. Original or CC0 assets are practical choices. One good rig can carry multiple appearances. [Unity humanoid animation](https://docs.unity3d.com/6000.0/Documentation/Manual/Retargeting.html) · [Blend Trees](https://docs.unity3d.com/6000.0/Documentation/Manual/class-BlendTree.html)
- **What carries over.** Use the Port Meridian artwork for the harbor's lighting and architecture. Build the pavement, pillars and nearby buildings as 3D geometry so the drone can travel around them. The browser implementation shows the two-pose walk that the new rig is intended to improve. [Environment artwork](../assets/scenes/) · [Browser movement](../src/engine.js)
- **When something looks wrong.** Send the phase, commit, camera view, steps to reproduce, and a clip or consecutive screenshots. For a build failure, attach the first Console error and its stack trace. For a slowdown, add the phone model and a Unity Profiler capture.

**First message to paste into Work after the Unity project is pushed**

> Use medh-eng/rooftop-snipper, branch unity/harbor-watch. Read docs/UNITY_HARBOR_WATCH_PLAN.md and inspect the Unity project I just pushed. Check Phase 0, then implement Phase 1, the single walking pedestrian on tiled pavement. Use the recorded Editor version. Give me the commit to pull, the scene to open, and the walk, stop and turn checks to perform on my PC.
