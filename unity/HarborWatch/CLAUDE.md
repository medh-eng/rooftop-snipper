# Unity project guide (Claude Code, run from `unity/HarborWatch` or the repo root)

Unity **6000.3.28f1**, URP, Input System, IMGUI for all game UI (no Canvas). Scene to play:
`Assets/Scenes/FindTheOne.unity` (it is first in Build Settings). That scene has one camera object with
`HarborCrowdReview` (tick `findTheOneMode`), which builds the concourse, then adds `FindTheOneController`
and `CaseCrowd` at runtime. Nothing else needs wiring.

## Where things are
| Path (under `Assets/HarborWatch/`) | What |
|---|---|
| `Core/` | Rules, no Unity types (`CaseEngine.cs`). Mirrors `game/case-engine.js` |
| `FindTheOne/FindTheOneController.cs` | menu, campaign, dossier, input, UI, save (PlayerPrefs `RedSignal.FindTheOne.v1.*`) |
| `FindTheOne/CaseCrowd.cs` | the 3D crowd, gait and arm/head posing, provocations, **Echo lens** record/replay |
| `HarborCrowdReview.cs`, `HarborCrowdTraffic.cs`, `HarborEnvironment.cs` | concourse and older phases |
| `Editor/FindTheOneBuildTools.cs` | menu: *Harbor Watch > Find The One* (Open scene, Verify, Build Android Development APK) |
| `Tests/Editor`, `Tests/PlayMode` | EditMode rules tests; PlayMode smoke tests that play the real scene |
| `Resources/` | `CrowdWalk.fbx`, `CrowdJog.fbx`, `ReviewSurface.mat`, ambience |

## Playing and debugging
* Editor and Development builds **open every campaign case** (`DevUnlock`); release builds keep the star lock
  (case N needs stars on case N-1).
* A failure to start a case, or an exception in `Update`, shows as a **red bar with an OK button** on screen
  (and in the Console). Read it before anything else.
* Editor log on Windows: `%LOCALAPPDATA%\Unity\Editor\Editor.log`. Mac: `~/Library/Logs/Unity/Editor.log`.
* If Play mode will not start, look for compile errors first (Console, red). The compile check in `tools/`
  cannot see errors that depend on real Unity APIs.

## Running Unity from the terminal (Editor must be closed: a project opens only once)
Windows example (adjust the path to the installed 6000.3.28f1):
```
"C:\Program Files\Unity\Hub\Editor\6000.3.28f1\Editor\Unity.exe" -batchmode -nographics ^
  -projectPath "<repo>\unity\HarborWatch" -runTests -testPlatform EditMode ^
  -testResults "%TEMP%\edit.xml" -logFile "%TEMP%\edit.log"
```
Same with `-testPlatform PlayMode` for `FindTheOnePlayTests` (starts every case, runs the Daily, checks the Echo
lens). PlayMode needs graphics, so omit `-nographics` for that one. Read the log and the XML, fix, rerun.
Batch mode compiles first: a compile error shows up in the log as `error CS####`.

## Rules for editing here
* Runtime classes are in `Assembly-CSharp`; test assemblies cannot reference it (the PlayMode tests use reflection).
* Keep `findTheOneMode` scenes untouched unless the task is about scene wiring.
* New script file means a new `.meta`; let Unity create it (open the Editor once) or copy the pattern of a neighbour.
* After a change: compile check, EditMode tests, PlayMode tests, then say exactly which of these you ran.
