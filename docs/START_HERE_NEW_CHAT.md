# Harbor Watch · start here

## Use the project you already created

Keep your saved copy. In Windows File Explorer, open HarborWatch and check that Assets, Packages and ProjectSettings sit side by side. Keep their contents, including .meta files. Check for Packages/manifest.json and ProjectSettings/ProjectVersion.txt. Unity Hub can reopen this copy through Add project from disk by selecting HarborWatch.

## Upload the whole folder through Chrome

1. If the three folders are loose, create a folder named HarborWatch and copy the three folders into it. Keep the originals until the upload is verified.
2. In Chrome, [open the unity folder on the prototype branch](https://github.com/medh-eng/rooftop-snipper/tree/unity/harbor-watch/unity). Choose **Add file → Upload files**.
3. In Explorer, go up one level so you can see the HarborWatch folder itself. Drag that folder onto the upload area in Chrome. GitHub lists the individual files automatically.
4. Check the upload list for HarborWatch/ProjectSettings/ProjectVersion.txt. Commit directly to unity/harbor-watch. On GitHub the finished path will be unity/HarborWatch/ProjectSettings/ProjectVersion.txt.
5. If GitHub reports too many files, compress the folder containing those three source folders into a ZIP and attach it to the project chat. Ask the assistant to extract it, verify the layout and commit the source files to the prototype branch.

## Continue in a new chat

Give the new chat the [documents folder](https://github.com/medh-eng/rooftop-snipper/tree/unity/harbor-watch/docs). It contains this guide, the [development plan](UNITY_HARBOR_WATCH_PLAN.md) and the [project handoff](UNITY_PROJECT_HANDOFF.md).

> Read the three documents on the unity/harbor-watch branch of medh-eng/rooftop-snipper. Check whether unity/HarborWatch contains Assets, Packages and ProjectSettings. If the project is present, verify the Editor version and begin Phase 1, the pedestrian walking test.
