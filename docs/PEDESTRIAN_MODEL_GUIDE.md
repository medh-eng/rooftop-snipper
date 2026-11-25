# Making the pedestrian 3D model (ChatGPT image, then Tripo, then Mixamo)

Goal: one clean, plain-clothed adult pedestrian (then a few variants) that walks and jogs in the crowd.
The game itself adds the things that make people different: beanie, headphones, briefcase, phone, backpack.
**So the model must not wear or hold any of those.**

## Rules for every model
* Fictional person. Never base it on a real person or a celebrity.
* Adult, normal proportions, about 1.75 m tall, realistic and slightly stylised. Not cartoon, not a superhero.
* Plain everyday commuter clothes: jacket or shirt, trousers, shoes. Simple shapes, flat colours, no logos or text.
* Bare head (short hair is fine). **No hat, glasses, headphones, bag, phone, scarf or loose coat.** Loose clothes
  break the rig and the walk.
* Arms straight out to the sides (T-pose), palms down, feet about shoulder width apart, mouth closed, neutral face.
* Small file: aim for 8,000 triangles or less per person and one 1024 x 1024 texture. A crowd needs dozens of them
  on a phone.

## Step 1: ChatGPT images
Make the **front** first. When you like it, ask for the other views **in the same chat** so the person stays identical.
Tripo works best with a front, back and side view on a plain background.

**Prompt A: front view (copy and change the clothes):**
```
Full-body character reference of a fictional adult man in his early 30s, about 1.75 m tall, standing in a
strict T-pose: arms straight out to the sides, palms facing down, legs straight and slightly apart, feet pointing
forward, neutral calm face, mouth closed, short dark hair, bare head. He wears a plain mid-grey zip jacket, dark
navy trousers and simple black shoes. No hat, no glasses, no headphones, no bag, no phone, no logos, no text.
Realistic, slightly stylised, matte fabrics, soft even studio lighting, no shadows on the background.
Pure white background. Orthographic front view, camera at chest height, whole body visible from head to feet.
```

**Prompt B: back view (same chat, right after A):**
```
Same character, same clothes, same T-pose, same lighting and framing. Show the exact back view, whole body, pure
white background.
```

**Prompt C: side view:**
```
Same character, same clothes, same lighting and framing. Show the exact left side view in a T-pose (arms
straight out, so from the side the body is thin), whole body, pure white background.
```

**Variants** (new chat or the same one, change only the clothes and keep everything else in Prompt A):
* Woman, late 20s, tied-back hair, olive utility jacket, black trousers, white trainers.
* Man, 50s, grey short hair, navy overcoat that ends at the hips (not long), grey trousers, brown shoes.
* Woman, 60s, short silver hair, burgundy cardigan, dark trousers, flat shoes.
* Man, 20s, hoodie in a solid colour, jeans, sneakers.
Keep the clothes short and simple. The game recolours nothing, so pick colours that stay readable from a rooftop:
no pure black or white blobs, and vary them (grey, navy, olive, burgundy, mustard).

If ChatGPT shows hands in fists, a bent pose or a different face in the side view, say:
`Regenerate. Straight arms in a T-pose, identical clothes and face to the front view.`

## Step 2: Tripo 3D
* Use **Image to 3D** with the multi-view option: upload the front, back and side images.
* Choose a **textured** model, with the **T-pose** option on if shown. Choose **low poly** or set the face limit
  to about 8,000 if the option exists.
* Check the result: arms straight out, hands open, legs apart, no extra blobs, no hole under the arms.
  If the legs are fused or the arms droop, generate again or use a better front image. A bad shape cannot be fixed
  later by Mixamo.
* Download as **FBX** (or OBJ with the texture). Name it `Pedestrian_Man01.fbx`.

## Step 3: Mixamo (mixamo.com, free Adobe account)
1. **Upload Character** and choose the Tripo file. Place the markers (chin, wrists, elbows, knees, groin) when asked.
   Choose the skeleton **without fingers** (about 25 joints) if offered.
2. Pick the animation **Walking**. Turn on **In Place**. Keep "Arm-space" at its default.
   Download as **FBX Binary (.fbx)**, **With Skin**, **30 fps**, Keyframe Reduction **none**.
   Save it as `Man01_Walk.fbx`.
3. Pick **Jogging** (or **Running**), **In Place** on, same settings but **Without Skin**.
   Save it as `Man01_Jog.fbx`.
4. Optional, also **Without Skin**: `Man01_Idle.fbx` (Idle), a short surprised or flinch clip for the horn, and
   a looking-around clip. They make stops look natural.
5. In the preview, pick the walk that looks calm and even (not a swaggering or limping one). Smooth walking
   in the game depends on a clean in-place loop.

## Step 4: put the files in the repo
Copy the files into this folder: `unity/HarborWatch/Assets/HarborWatch/Incoming/` (create it if missing).
In GitHub Desktop commit them with the summary `Add Man01 model and Mixamo walk/jog`, then Push.
The Tripo texture file (PNG or JPG) goes in the same folder.

Then give Claude Code **prompt 7** from `docs/CLAUDE_CODE_PROMPTS.md`. The game currently uses a Blender-made rig with its
own bone names. A Mixamo skeleton has different names, so Claude Code needs to adapt the import and the arm
logic (phone, briefcase). Do not expect the drop-in files to work in the crowd before that step.

## Quick checklist
* [ ] Front, back, side images match.
* [ ] T-pose, no hat, no bag, no phone, no headphones.
* [ ] Tripo shape is clean, about 8,000 triangles or less.
* [ ] Mixamo walk and jog are in place, 30 fps, no keyframe reduction.
* [ ] Files named `Man01_Walk.fbx`, `Man01_Jog.fbx` and the model file, in `Assets/HarborWatch/Incoming/`.
