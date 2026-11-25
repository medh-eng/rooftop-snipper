# Prompts for Claude Code (copy and paste)

**Why use Claude Code on your PC for the Unity part.** Unity can only be run on a computer with the Editor.
Claude Code on your PC can open the project, run the PlayMode tests, read the real error messages and fix
them in a loop. The chat version can write code but cannot run Unity, so every mistake costs you a round trip.
Use the chat for design ideas and the browser prototype; use Claude Code for anything that must run in Unity.

## One-time setup
1. In GitHub Desktop: switch to branch `redesign/find-the-one`, click *Fetch origin* and *Pull*.
2. Open a terminal in the repo folder (the one that contains `CLAUDE.md`) and run `claude`.
3. Close the Unity Editor before asking it to run tests (Unity allows one open copy per project).

## 1. First run: make it play, and tell me what you found
> Read CLAUDE.md, unity/HarborWatch/CLAUDE.md and STATUS.md. The Unity game would not start for me and I could not
> see case 4. Pull the latest, then run the Unity EditMode and PlayMode tests in batch mode with the Unity
> 6000.3.28f1 installed on this PC. Fix every compile error and failing test. Do not guess: read the Unity log.
> When all pass, tell me in plain words what was wrong, what you changed, and what you could not check. Update
> STATUS.md and LOG.md, commit and push to redesign/find-the-one.

## 2. I will play it; fix what I report
> Read CLAUDE.md and STATUS.md "Needs checking". I played Find the One in Unity. Here is what I saw:
> [describe, or paste the red error bar text / Console message]. Reproduce it with a PlayMode test if you can,
> fix it, run all checks, and explain the fix in two or three plain sentences.

## 3. Check the Echo lens and the crowd look
> Read STATUS.md "Needs checking". You cannot watch the screen, so use the PlayMode tests, and where a test cannot
> show something add a small Editor-only debug log or overlay. Review CaseCrowd's Echo section for foot sliding,
> people popping in or out at the edges, and a wrong restore, and fix what you find. Tell me what I should still
> look at with my own eyes.

## 4. Build a phone APK
> Read unity/HarborWatch/CLAUDE.md. Switch the project to Android if it is not, run the menu command
> Harbor Watch > Find The One > Build Android Development APK through a batch-mode -executeMethod call
> (add a static method that skips the file dialog and takes the output path from a command-line argument),
> and tell me where the APK is.

## 5. Next feature (after Echo is confirmed)
> Read PLAN.md Phase D. Prototype the next gadget in the browser build first (game/), with a Playwright test, then
> port it to Unity with the same rules. Keep each case to one new idea. Update STATUS.md, LOG.md and PLAN.md.

## 6. Close a session
> Run every check in CLAUDE.md, update STATUS.md and LOG.md (added, fixed, verified, NOT verified, next), commit and
> push to redesign/find-the-one, then give me a five-line summary in plain words.

## 7. Use my new pedestrian model (Tripo + Mixamo)
> Read CLAUDE.md, docs/PEDESTRIAN_MODEL_GUIDE.md and FindTheOne/CaseCrowd.cs. I put a Mixamo-rigged model and
> its walk and jog FBX files in Assets/HarborWatch/Incoming/. Make the crowd use them: import them with the right
> settings (add the files to the importer script, using a Humanoid or Generic rig, whichever keeps the planted-foot
> walk and jog working), map the Mixamo bone names (mixamorig:Hips, Head, arms) to what CaseCrowd looks for, and keep
> the phone, briefcase, headphones, beanie and head-turn logic working. Keep the old Blender walker as a fallback
> until I confirm the new one looks right. Run every check, add a PlayMode test that loads the new model, and tell me
> in plain words what I should look at on screen (feet sliding, arm positions, size against the buildings).
