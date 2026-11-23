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
        // Keep the approved Crowd scene untouched. The duplicate Mission scene
        // sets this flag; it reuses the same concourse/crowd without review UI.
        public bool missionMode;
        public bool droneMode; // Only set in the separate Phase 4 review scene.
        public Transform ConcourseRoot { get { return world ? world.transform : null; } }
        public HarborCrowdTraffic Traffic { get { return stream; } }
        HarborMissionController mission;
        HarborDroneReview droneReview;
        readonly List<Material> allocatedMaterials = new List<Material>();
        Camera crowdCamera;
        GameObject world;
        Material sourceMaterial;
        Texture2D pavingTexture;
        HarborCrowdTraffic stream;
        HarborEnvironment environment;
        AudioSource ambience;
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
            if (!sourceMaterial)
            {
                Fail("Missing ReviewSurface material.");
                return;
            }
            world = new GameObject("Port Meridian - Concourse Review");
            BuildConcourse();
            environment = world.AddComponent<HarborEnvironment>();
            environment.Initialise(world.transform, sourceMaterial);
            stream = gameObject.AddComponent<HarborCrowdTraffic>();
            if (!stream.Initialize(world.transform, sourceMaterial))
            {
                Fail("Crowd animation assets not loaded. Check CrowdWalk.fbx and CrowdJog.fbx.");
                return;
            }
            ambience = world.AddComponent<AudioSource>();
            ambience.playOnAwake = false;
            ambience.loop = true;
            ambience.clip = Resources.Load<AudioClip>("MeridianAmbience");
            if (ambience.clip) ambience.Play();
            PositionCamera();
            if (missionMode) mission = gameObject.AddComponent<HarborMissionController>();
            else if (droneMode) droneReview = gameObject.AddComponent<HarborDroneReview>();
        }

        public void LaunchShiftFlight()
        {
            mission.enabled = false;
            missionMode = false;
            droneMode = true;
            droneReview = gameObject.AddComponent<HarborDroneReview>();
        }

        public bool IsShiftFlight => droneMode && mission != null;

        public void FinishShiftFlight()
        {
            droneReview.enabled = false;
            Destroy(droneReview);
            droneReview = null;
            droneMode = false;
            missionMode = true;
            mission.FileShiftReport();
            mission.enabled = true;
        }

        Rect PlayShiftArea()
        {
            Rect first = Control(0);
            return new Rect(first.x, first.y - first.height - 6f,
                Screen.safeArea.width - 12f, first.height);
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
            // An open ferry gangway replaces the solid pier wall at x=4.4.
            Part("Pier west railing", PrimitiveType.Cube, new Vector3(-2.55f, -.08f, -6.65f),
                new Vector3(11.8f, .20f, .28f), new Color(.39f, .45f, .46f));
            Part("Pier east railing", PrimitiveType.Cube, new Vector3(7.12f, -.08f, -6.65f),
                new Vector3(3.5f, .20f, .28f), new Color(.39f, .45f, .46f));
            Part("Ferry gangway", PrimitiveType.Cube, new Vector3(4.4f, -.11f, -7.35f),
                new Vector3(2.85f, .20f, 4.1f), new Color(.64f, .63f, .55f));
            Part("West street footpath", PrimitiveType.Cube, new Vector3(-10.6f, -.085f, .2f),
                new Vector3(5.2f, .16f, 8.3f), new Color(.60f, .58f, .53f));
            Part("East street footpath", PrimitiveType.Cube, new Vector3(10.6f, -.085f, .0f),
                new Vector3(5.2f, .16f, 8.3f), new Color(.60f, .58f, .53f));
            var sea = Part("Harbor water", PrimitiveType.Cube, new Vector3(0, -.28f, -8.0f),
                new Vector3(24, .12f, 4), new Color(.21f, .48f, .59f));
            HarborEnvironment.FinishSurface(sea.GetComponent<Renderer>().sharedMaterial, .78f, .05f);
            Part("Terminal left wing", PrimitiveType.Cube, new Vector3(-4.0f, 2.0f, 5.3f),
                new Vector3(4.2f, 4.0f, 1.4f), new Color(.78f, .73f, .61f));
            Part("Terminal right wing", PrimitiveType.Cube, new Vector3(4.0f, 2.0f, 5.3f),
                new Vector3(4.2f, 4.0f, 1.4f), new Color(.78f, .73f, .61f));
            Part("Terminal entrance lintel", PrimitiveType.Cube, new Vector3(0, 3.4f, 5.3f),
                new Vector3(3.9f, 1.3f, 1.4f), new Color(.74f, .69f, .58f));
            // The centre door is kept open for visitors entering/leaving the terminal.
            Part("Terminal west glass panel", PrimitiveType.Cube, new Vector3(-1.36f, 1.4f, 5.93f),
                new Vector3(.75f, 2.8f, .08f), new Color(.19f, .34f, .37f));
            Part("Terminal east glass panel", PrimitiveType.Cube, new Vector3(1.36f, 1.4f, 5.93f),
                new Vector3(.75f, 2.8f, .08f), new Color(.19f, .34f, .37f));
            Part("Terminal interior floor", PrimitiveType.Cube, new Vector3(0, -.085f, 6.55f),
                new Vector3(3.3f, .16f, 3.7f), new Color(.57f, .56f, .52f));
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
                if (followIndex < 0 || !stream.TryFocus(followIndex, out focus))
                {
                    followIndex = stream.NextVisible(followIndex);
                    if (followIndex < 0 || !stream.TryFocus(followIndex, out focus))
                        focus = new Vector3(0, .6f, .4f);
                }
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
                    followIndex = stream.NextVisible(followIndex);
                    break;
                case 3: slow = !slow; break;
                case 4: paused = !paused; break;
                case 5:
                    stream.ResetTraffic();
                    followIndex = -1;
                    break;
            }
            PositionCamera();
        }

        void Update()
        {
            if (error != null || !stream) return;
            if (missionMode)
            {
                if (mission && mission.IsRunning)
                {
                    stream.Step(Time.deltaTime);
                    environment.Step(Time.deltaTime);
                }
                return; // Camera, input and UI belong to HarborMissionController.
            }
            if (droneMode)
            {
                if (droneReview && droneReview.IsPlaying)
                {
                    stream.Step(Time.deltaTime);
                    environment.Step(Time.deltaTime);
                }
                return; // Phase 4 owns its camera, controls, and aerial UI.
            }
            var touch = Touchscreen.current;
            var mouse = Mouse.current;
            bool tapped = touch != null && touch.primaryTouch.press.wasPressedThisFrame;
            if (tapped || (mouse != null && mouse.leftButton.wasPressedThisFrame))
            {
                Vector2 pointer = tapped ? touch.primaryTouch.position.ReadValue() :
                    mouse.position.ReadValue();
                pointer.y = Screen.height - pointer.y;
                if (PlayShiftArea().Contains(pointer))
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("HarborMission");
                    return;
                }
                for (int i = 0; i < 6; i++)
                    if (Control(i).Contains(pointer)) { Activate(i); break; }
            }

            if (!paused)
            {
                float seconds = Time.deltaTime * (slow ? .25f : 1f);
                stream.Step(seconds);
                environment.Step(seconds);
            }
            PositionCamera();
        }

        void LateUpdate()
        {
            if (!ambience || !ambience.clip) return;
            bool playing = missionMode ? mission && mission.IsRunning :
                droneMode ? droneReview && droneReview.IsPlaying : !paused;
            if (playing && !ambience.isPlaying) ambience.UnPause();
            else if (!playing && ambience.isPlaying) ambience.Pause();
        }

        void OnGUI()
        {
            if ((missionMode || droneMode) && error == null) return;
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
                "PORT MERIDIAN  |  " + (stream ? stream.ActiveCount : 0) + " VISITORS\n" +
                cameraName + "  |  In " + (stream ? stream.Arrivals : 0) +
                "  |  Out " + (stream ? stream.Departures : 0) +
                "  |  Stopping " + (stream ? stream.RestingCount : 0), caption);

            string[] labels = {
                "Rooftop", "Overhead", "Follow next", slow ? "Normal" : "Slow",
                paused ? "Resume" : "Pause", "Replay"
            };
            var button = new GUIStyle(GUI.skin.button);
            button.fontSize = Mathf.Clamp(Mathf.RoundToInt(Control(0).width * .15f), 11, 23);
            GUI.Box(PlayShiftArea(), "PLAY HARBOR WATCH", button);
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
