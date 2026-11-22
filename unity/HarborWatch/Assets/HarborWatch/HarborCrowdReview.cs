using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HarborWatch
{
    /// <summary>
    /// Phase 2 visual proof: one animated source rig, six independent routes,
    /// and three camera views of the same live 3D concourse.
    /// The original planted-foot FBX animation remains authoritative.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class HarborCrowdReview : MonoBehaviour
    {
        const int PedestrianCount = 6;
        static readonly Vector3[] RouteCentres =
        {
            new Vector3(-4.3f, 0, -2.0f),
            new Vector3(-1.7f, 0, -1.5f),
            new Vector3( 1.2f, 0, -1.8f),
            new Vector3( 4.0f, 0,  0.6f),
            new Vector3(-3.0f, 0,  2.5f),
            new Vector3( 0.5f, 0,  2.6f)
        };
        static readonly float[] Headings = { 0, 180, 90, -90, 180, 0 };
        static readonly float[] Speeds = { 1f, .86f, 1.13f, .93f, 1.06f, .79f };
        static readonly float[] PhaseOffsets = { 0, .18f, .37f, .54f, .71f, .87f };
        static readonly Color[] Jackets =
        {
            new Color(.72f, .36f, .19f),
            new Color(.16f, .44f, .58f),
            new Color(.50f, .53f, .29f),
            new Color(.63f, .34f, .42f),
            new Color(.55f, .43f, .30f),
            new Color(.24f, .48f, .40f)
        };

        readonly List<Material> allocatedMaterials = new List<Material>();
        readonly GameObject[] walkers = new GameObject[PedestrianCount];
        readonly Transform[] hips = new Transform[PedestrianCount];

        Camera crowdCamera;
        GameObject world;
        Material sourceMaterial;
        AnimationClip walkClip;
        Texture2D pavingTexture;
        float elapsed;
        int view; // 0 rooftop, 1 overhead, 2 follow
        int followIndex = -1;
        bool slow;
        bool paused;
        string error;

        void Start()
        {
            crowdCamera = GetComponent<Camera>();
            crowdCamera.clearFlags = CameraClearFlags.SolidColor;
            crowdCamera.backgroundColor = new Color(.65f, .76f, .81f);
            Application.targetFrameRate = 30;

            sourceMaterial = Resources.Load<Material>("ReviewSurface");
            var prefab = Resources.Load<GameObject>("HarborWalker");
            foreach (var clip in Resources.LoadAll<AnimationClip>("HarborWalker"))
            {
                if (clip.length > 0 && !clip.name.StartsWith("__"))
                {
                    walkClip = clip;
                    break;
                }
            }
            if (!sourceMaterial || !prefab || !walkClip)
            {
                Fail("HarborWalker model, animation, or ReviewSurface is missing. Reimport the FBX.");
                return;
            }

            world = new GameObject("Port Meridian - Concourse Review");
            BuildConcourse();
            for (int i = 0; i < PedestrianCount; i++)
            {
                var route = new GameObject("Commuter route " + (i + 1).ToString("00"));
                route.transform.SetParent(world.transform, false);
                route.transform.localPosition = RouteCentres[i];
                route.transform.localRotation = Quaternion.Euler(0, Headings[i], 0);
                walkers[i] = Instantiate(prefab, route.transform);
                walkers[i].name = "Commuter " + (i + 1).ToString("00");
                // Do not let legacy Animation advance in addition to our manual sampling.
                foreach (var animation in walkers[i].GetComponentsInChildren<Animation>(true))
                    animation.enabled = false;
                foreach (var bone in walkers[i].GetComponentsInChildren<Transform>(true))
                    if (bone.name == "Hips") { hips[i] = bone; break; }
                if (!hips[i])
                {
                    Fail("HarborWalker FBX has no Hips bone; reimport the model.");
                    return;
                }
                ColourClothing(walkers[i], Jackets[i]);
            }
            SampleCrowd();
            PositionCamera();
        }

        void Fail(string message)
        {
            error = message;
            Debug.LogError(message, this);
        }

        Material Painted(Color colour)
        {
            var material = new Material(sourceMaterial);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colour);
            if (material.HasProperty("_Color")) material.SetColor("_Color", colour);
            allocatedMaterials.Add(material);
            return material;
        }

        void ColourClothing(GameObject character, Color jacket)
        {
            var colours = new Dictionary<string, Color>
            {
                { "Jacket", jacket },
                { "Hem", jacket * .72f },
                { "Denim", new Color(.20f, .30f, .39f) },
                { "Cuff", new Color(.16f, .24f, .30f) },
                { "Skin", new Color(.70f, .48f, .34f) },
                { "Hair", new Color(.21f, .16f, .13f) },
                { "Leather", new Color(.32f, .25f, .19f) },
                { "Sole", new Color(.69f, .66f, .58f) },
                { "Metal", new Color(.75f, .61f, .39f) },
                { "Shirt", new Color(.78f, .78f, .72f) }
            };
            foreach (var renderer in character.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (!slots[i]) continue;
                    string materialName = slots[i].name.Replace(" (Instance)", "");
                    Color colour;
                    if (colours.TryGetValue(materialName, out colour))
                        slots[i] = Painted(colour);
                }
                renderer.sharedMaterials = slots;
                renderer.updateWhenOffscreen = true;
            }
        }

        GameObject Part(string name, PrimitiveType shape, Vector3 location,
            Vector3 scale, Color colour)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;
            part.transform.SetParent(world.transform, false);
            part.transform.localPosition = location;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = Painted(colour);
            return part;
        }

        void BuildConcourse()
        {
            const int textureSize = 128;
            pavingTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, true);
            pavingTexture.wrapMode = TextureWrapMode.Repeat;
            var pixels = new Color32[textureSize * textureSize];
            for (int y = 0; y < textureSize; y++)
                for (int x = 0; x < textureSize; x++)
                {
                    int shade = x < 3 || y < 3 ? 93 : 153 + (x * 13 + y * 7) % 10;
                    pixels[y * textureSize + x] =
                        new Color32((byte)(shade + 8), (byte)(shade + 7), (byte)shade, 255);
                }
            pavingTexture.SetPixels32(pixels);
            pavingTexture.Apply(true, true);
            var ground = Part("Seamed stone pavement", PrimitiveType.Plane,
                Vector3.zero, new Vector3(1.8f, 1, 1.35f), Color.white);
            var groundMaterial = ground.GetComponent<Renderer>().sharedMaterial;
            if (groundMaterial.HasProperty("_BaseMap"))
            {
                groundMaterial.SetTexture("_BaseMap", pavingTexture);
                groundMaterial.SetTextureScale("_BaseMap", new Vector2(30, 24));
            }
            else
            {
                groundMaterial.mainTexture = pavingTexture;
                groundMaterial.mainTextureScale = new Vector2(30, 24);
            }

            // In-world objects: they share the same depth buffer in every camera view.
            Part("Pier edge", PrimitiveType.Cube, new Vector3(0, -.08f, -6.65f),
                new Vector3(17.7f, .20f, .28f), new Color(.39f, .45f, .46f));
            Part("Harbor water", PrimitiveType.Cube, new Vector3(0, -.28f, -8.0f),
                new Vector3(24, .12f, 4), new Color(.21f, .48f, .59f));
            Part("Terminal left wing", PrimitiveType.Cube, new Vector3(-4.0f, 2.0f, 5.3f),
                new Vector3(4.2f, 4.0f, 1.4f), new Color(.78f, .73f, .61f));
            Part("Terminal right wing", PrimitiveType.Cube, new Vector3(4.0f, 2.0f, 5.3f),
                new Vector3(4.2f, 4.0f, 1.4f), new Color(.78f, .73f, .61f));
            Part("Terminal entrance lintel", PrimitiveType.Cube, new Vector3(0, 3.4f, 5.3f),
                new Vector3(3.9f, 1.3f, 1.4f), new Color(.74f, .69f, .58f));
            Part("Recessed glass doors", PrimitiveType.Cube, new Vector3(0, 1.4f, 5.93f),
                new Vector3(3.5f, 2.8f, .08f), new Color(.19f, .34f, .37f));
            Part("Terminal canopy", PrimitiveType.Cube, new Vector3(0, 4.1f, 4.3f),
                new Vector3(12.3f, .24f, 3), new Color(.38f, .43f, .43f));

            Part("West occluding pillar", PrimitiveType.Cylinder,
                new Vector3(-4.15f, 1.45f, .70f), new Vector3(.26f, 1.45f, .26f),
                new Color(.65f, .63f, .58f));
            Part("East occluding pillar", PrimitiveType.Cylinder,
                new Vector3(2.82f, 1.45f, 1.10f), new Vector3(.26f, 1.45f, .26f),
                new Color(.65f, .63f, .58f));
            Part("Ticket kiosk", PrimitiveType.Cube, new Vector3(-6.2f, .75f, 2.4f),
                new Vector3(1.0f, 1.5f, 1.1f), new Color(.22f, .44f, .48f));
            Part("Bench", PrimitiveType.Cube, new Vector3(5.5f, .40f, -2.7f),
                new Vector3(1.7f, .16f, .55f), new Color(.42f, .27f, .19f));
            Part("Bench back", PrimitiveType.Cube, new Vector3(5.5f, .78f, -2.95f),
                new Vector3(1.7f, .65f, .12f), new Color(.42f, .27f, .19f));

            Part("Observation roof", PrimitiveType.Cube, new Vector3(-7.4f, 5.7f, -5.0f),
                new Vector3(3, .3f, 4), new Color(.43f, .43f, .41f));
            Part("Observation facade", PrimitiveType.Cube, new Vector3(-8.0f, 2.7f, -5.0f),
                new Vector3(1.0f, 5.4f, 4), new Color(.59f, .55f, .48f));
        }

        void SampleCrowd()
        {
            if (!walkClip) return;
            for (int i = 0; i < PedestrianCount; i++)
            {
                if (!walkers[i]) continue;
                float time = Mathf.Repeat(
                    walkClip.length * PhaseOffsets[i] + elapsed * Speeds[i], walkClip.length);
                walkClip.SampleAnimation(walkers[i], time);
            }
        }

        void PositionCamera()
        {
            if (!crowdCamera) return;
            Vector3 focus = new Vector3(0, .6f, .4f);
            if (view == 0)
            {
                crowdCamera.transform.position = new Vector3(-10f, 10.5f, -12f);
                crowdCamera.fieldOfView = 53f;
            }
            else if (view == 1)
            {
                crowdCamera.transform.position = new Vector3(0, 18.0f, -4f);
                crowdCamera.fieldOfView = 49f;
            }
            else
            {
                if (followIndex < 0) followIndex = 0;
                focus = hips[followIndex] ? hips[followIndex].position + Vector3.up * .15f : focus;
                crowdCamera.transform.position = focus + new Vector3(2.5f, 2.0f, -3.9f);
                crowdCamera.fieldOfView = 39f;
            }
            crowdCamera.transform.LookAt(focus);
        }

        Rect Control(int index)
        {
            Rect safe = Screen.safeArea;
            float gap = 6f;
            float height = Mathf.Clamp(safe.height * .055f, 40f, 66f);
            float width = (safe.width - 4f * gap) / 3f;
            int column = index % 3;
            int row = index / 3;
            float y = Screen.height - safe.yMin - 2f * height - 3f * gap;
            return new Rect(safe.xMin + gap + column * (width + gap),
                y + row * (height + gap), width, height);
        }

        void Activate(int index)
        {
            switch (index)
            {
                case 0: view = 0; break;
                case 1: view = 1; break;
                case 2:
                    view = 2;
                    followIndex = (followIndex + 1) % PedestrianCount;
                    break;
                case 3: slow = !slow; break;
                case 4: paused = !paused; break;
                case 5: elapsed = 0; break;
            }
            SampleCrowd();
            PositionCamera();
        }

        void Update()
        {
            if (error != null || !walkClip) return;
            var touch = Touchscreen.current;
            var mouse = Mouse.current;
            bool tapped = touch != null && touch.primaryTouch.press.wasPressedThisFrame;
            if (tapped || (mouse != null && mouse.leftButton.wasPressedThisFrame))
            {
                Vector2 pointer = tapped ? touch.primaryTouch.position.ReadValue() :
                    mouse.position.ReadValue();
                pointer.y = Screen.height - pointer.y;
                for (int i = 0; i < 6; i++)
                    if (Control(i).Contains(pointer)) { Activate(i); break; }
            }

            if (!paused) elapsed += Time.deltaTime * (slow ? .25f : 1f);
            SampleCrowd();
            PositionCamera();
        }

        void OnGUI()
        {
            if (error != null)
            {
                GUI.Box(new Rect(8, 8, Screen.width - 16, 72), error);
                return;
            }
            var caption = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleLeft };
            caption.fontSize = Mathf.Clamp(Screen.width / 40, 11, 19);
            string cameraName = view == 0 ? "ROOFTOP" : view == 1 ? "OVERHEAD" :
                "FOLLOW " + (followIndex + 1);
            GUI.Box(new Rect(8, 8, Screen.width - 16, 55),
                "PORT MERIDIAN  |  6 PEDESTRIANS\n" + cameraName +
                "  |  Same animated crowd in every view", caption);

            string[] labels = {
                "Rooftop", "Overhead", "Follow next", slow ? "Normal" : "Slow",
                paused ? "Resume" : "Pause", "Replay"
            };
            var button = new GUIStyle(GUI.skin.button);
            button.fontSize = Mathf.Clamp(Mathf.RoundToInt(Control(0).width * .15f), 11, 23);
            for (int i = 0; i < labels.Length; i++)
                GUI.Box(Control(i), labels[i], button);
        }

        void OnDestroy()
        {
            if (world) Destroy(world);
            foreach (var material in allocatedMaterials)
                if (material) Destroy(material);
            if (pavingTexture) Destroy(pavingTexture);
        }
    }
}
