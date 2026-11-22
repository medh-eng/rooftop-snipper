# Rooftop Snipper Unity handoff

## The important folder decision

Do not put the downloaded repository ZIP inside the Unity project.

The GitHub repository is the outer project folder. The Unity project is one folder inside it:

\`\`\`text
rooftop-snipper/
├── src/
├── docs/
└── unity/
    └── HarborWatch/
        ├── Assets/
        ├── Packages/
        └── ProjectSettings/
\`\`\`

Unity must open \`unity/HarborWatch/\`, not the outer \`rooftop-snipper/\` folder.

## You already created HarborWatch

If Unity created the project somewhere else, close Unity and copy these three folders from the existing project:

- \`Assets\`
- \`Packages\`
- \`ProjectSettings\`

Copy them into:

\`\`\`text
rooftop-snipper/unity/HarborWatch/
\`\`\`

Keep the \`.meta\` files inside \`Assets\`. Do not copy \`Library\`, \`Temp\`, \`Logs\`, \`Obj\`, \`Build\`, or \`Builds\`.

Then open Unity Hub and choose **Add project from disk**. Select the \`HarborWatch\` project folder.

## Upload through Chrome

1. Open the repository in Chrome.
2. Open the \`unity/harbor-watch\` branch.
3. Open the \`unity\` folder.
4. Click **Add file → Upload files**.
5. Upload the contents of \`HarborWatch\`, keeping the folders named \`Assets\`, \`Packages\`, and \`ProjectSettings\`.
6. Commit with the message \`Create Harbor Watch Unity project\`.

GitHub’s browser upload has a 25 MiB limit per file and allows up to 100 files in one upload. If the upload stops at 100 files, repeat it in batches. The paths must remain \`unity/HarborWatch/Assets/...\`, \`unity/HarborWatch/Packages/...\`, and \`unity/HarborWatch/ProjectSettings/...\`.

## What happens next

After the upload, start a new chat and attach or paste these files:

- \`docs/START_HERE_NEW_CHAT.md\`
- \`docs/UNITY_PROJECT_HANDOFF.md\`
- \`docs/UNITY_HARBOR_WATCH_PLAN.md\`

Tell the new chat:

> The Unity project is uploaded to \`medh-eng/rooftop-snipper\`, branch \`unity/harbor-watch\`, under \`unity/HarborWatch\`. Read the three Markdown files in \`docs\`. Inspect the Unity project and begin Phase 1, the convincing pedestrian walk.

