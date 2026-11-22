# Unity project handoff

## Current objective

Turn Rooftop Snipper into a polished Android Unity game called **Harbor Watch**. The first proof is one realistic pedestrian who walks, stops, and turns naturally in a city scene viewed from a rooftop.

The existing browser game remains the design reference for missions, districts, rewards, civilian protection, upgrades, and drone unlocks. The Unity project will be built as a separate Android implementation.

## Current repository state

- Repository: \`https://github.com/medh-eng/rooftop-snipper\`
- Unity branch: \`unity/harbor-watch\`
- Unity project path: \`unity/HarborWatch\`
- Unity project folders required in Git: \`Assets\`, \`Packages\`, \`ProjectSettings\`
- Generated Unity folders to leave on the computer: \`Library\`, \`Temp\`, \`Logs\`, \`Obj\`, \`Build\`, \`Builds\`

## First task after upload

Use Work / Codex at High effort for the walking prototype.

Build a small test scene with:

- one tiled pavement area
- one rigged pedestrian
- idle, walk, stop and turn animations
- a camera at rooftop height
- a close lens view
- a simple route with a visible paving joint

The movement controller must use one source of truth for animation and forward movement. The planted foot should remain on the same paving joint while the body advances. The walker must slow, stop, turn, and restart without sliding or snapping.

## Evidence to return

Return:

1. the commit SHA
2. the Unity scene name
3. the Unity Editor version
4. a short screen recording or consecutive screenshots
5. the exact walk, stop, and turn checks performed
6. any asset licence and source information

## Safe asset policy

Use original or explicitly redistributable assets in the public repository. Record the creator, source URL, licence, and permitted use for every imported model, animation, texture, HDRI, and sound. Poly Haven CC0 assets are suitable for the first sky and pavement tests. Check other sources individually before committing their files.

## Model assignments

- Regular Chat Instant or Low: simple Unity menu questions.
- Regular Chat Medium: review walking screenshots or recordings.
- Regular Chat High: compare two animation approaches.
- Work / Codex GPT-6.2 Sol Medium: ordinary implementation, tests, mission data, UI, and content.
- Work / Codex GPT-6.2 Astra High: walking controller, camera defects, animation bugs, and Android performance diagnosis.

## Important scope rule

Do not begin with the entire campaign, advanced ballistics, or a large crowd. First make the one-person walking test convincing. The crowd, city, red target marker, rewards, upgrades, and drone mode follow after that proof.

