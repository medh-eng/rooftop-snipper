using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HarborWatch
{
    /// <summary>
    /// Phase 4 non-violent aerial camera proof over the SAME moving harbor.
    /// The player pilots a spectator camera, observes real geometry occlusion,
    /// and identifies a fictional maintenance drone. No combat or projectiles.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class HarborDroneReview : MonoBehaviour
    {
        const float LookSensitivity = 95f;
        const float Acceleration = 7.5f;
        const float ScanRadius = .115f;
        static readonly Vector3 InitialPosition = new Vector3(-5.1f, 8.5f, -3.3f);
        static readonly Vector3 RooftopPosition = new Vector3(-10f, 10.5f, -12f);
        static readonly Vector3 RooftopFocus = new Vector3(0f, .6f, .4f);
        static readonly Vector3 BeaconCentre = new Vector3(2.3f, 6.6f, -.2f);

        readonly List<Material> ownedMaterials = new List<Material>();
        readonly Transform[] spinningDiscs = new Transform[4];
        HarborCrowdReview concourse;
        Camera cameraView;
        GameObject beacon;
        Material sourceMaterial;
        Vector3 cameraPosition, velocity;
        Vector2 stick;
        float yaw, pitch, droneFov, climb, orbitTime;
        bool droneMode = true;
        bool paused, identified;
        int moveFinger = -1, lookFinger = -1;
        Vector2 lastLook;
        bool mouseMoving, mouseLooking;
        Vector2 mouseLast;
        string hint = "Use the left stick to fly, drag right to look.";

        public bool IsPlaying { get { return !paused; } }

        void Start()
        {
            concourse = GetComponent<HarborCrowdReview>();
            cameraView = GetComponent<Camera>();
            sourceMaterial = Resources.Load<Material>("ReviewSurface");
            if (!concourse || !concourse.ConcourseRoot || !concourse.Traffic ||
                !sourceMaterial || !cameraView)
            {
                Debug.LogError("Harbor Drone needs the Port Meridian crowd scene and ReviewSurface.");
                enabled = false;
                return;
            }
            if (Application.isMobilePlatform)
                Screen.orientation = ScreenOrientation.Portrait;

            BuildBeacon();
            Restart();
        }

        Material Painted(Color color)
        {
            var material = new Material(sourceMaterial);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            ownedMaterials.Add(material);
            return material;
        }

        Transform Piece(Transform parent, string title, PrimitiveType type,
            Vector3 local, Vector3 scale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = title;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = local;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = Painted(color);
            var collider = part.GetComponent<Collider>();
            if (collider) Destroy(collider);
            return part.transform;
        }

        void BuildBeacon()
        {
            beacon = new GameObject("Fictional unregistered survey drone");
            beacon.transform.SetParent(concourse.ConcourseRoot, false);
            Piece(beacon.transform, "Central body", PrimitiveType.Cube,
                Vector3.zero, new Vector3(.62f, .18f, .44f), new Color(.18f,.35f,.43f));
            Piece(beacon.transform, "Survey light", PrimitiveType.Sphere,
                new Vector3(0f,-.15f,.10f), new Vector3(.19f,.15f,.19f),
                new Color(.95f,.64f,.21f));
            int index = 0;
            foreach (int x in new[] {-1,1})
                foreach (int z in new[] {-1,1})
                {
                    var offset = new Vector3(.48f*x, .02f, .37f*z);
                    Piece(beacon.transform, "Rotor support " + index,
                        PrimitiveType.Cube, offset * .5f,
                        new Vector3(.56f,.065f,.06f), new Color(.20f,.27f,.29f));
                    spinningDiscs[index++] = Piece(beacon.transform,
                        "Rotor disc", PrimitiveType.Cylinder, offset,
                        new Vector3(.23f,.013f,.23f), new Color(.46f,.72f,.73f));
                }
            beacon.transform.position = BeaconCentre;
        }

        void Restart()
        {
            cameraPosition = InitialPosition;
            velocity = Vector3.zero;
            yaw = 50f;
            pitch = 39f;
            droneFov = 54f;
            orbitTime = 0f;
            droneMode = true;
            paused = false;
            identified = false;
            moveFinger = lookFinger = -1;
            stick = Vector2.zero;
            climb = 0f;
            if (beacon) beacon.SetActive(true);
            hint = "Fly around the terminal and identify the unregistered survey drone.";
            if (concourse && concourse.Traffic && !concourse.IsShiftFlight)
                concourse.Traffic.ResetTraffic();
            if (concourse.IsShiftFlight)
                hint = "Final inspection: tap FIND, then IDENTIFY the survey drone in clear view.";
            UpdateCamera();
        }

        void UpdateCamera()
        {
            if (!cameraView) return;
            if (droneMode)
            {
                cameraView.transform.position = cameraPosition;
                cameraView.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                cameraView.fieldOfView = droneFov;
            }
            else
            {
                cameraView.transform.position = RooftopPosition;
                cameraView.transform.LookAt(RooftopFocus);
                cameraView.fieldOfView = 53f;
            }
        }

        void ToggleCamera()
        {
            droneMode = !droneMode;
            stick = Vector2.zero;
            climb = 0f;
            velocity = Vector3.zero;
            moveFinger = lookFinger = -1;
            if (!droneMode)
                hint = "Rooftop view of the SAME living crowd. Tap DRONE to return.";
            else
                hint = "Aerial camera active. Both views share the same pedestrians.";
            UpdateCamera();
        }

        void FocusBeacon()
        {
            if (!droneMode || identified) return;
            Vector3 delta = beacon.transform.position - cameraPosition;
            if (delta.sqrMagnitude < .01f) return;
            yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(-Mathf.Atan2(delta.y,
                new Vector2(delta.x,delta.z).magnitude) * Mathf.Rad2Deg, -20f, 80f);
            hint = "Signal centred. Move for a clear view if a building blocks it.";
            UpdateCamera();
        }

        // Screen-safe HUD dimensions use GUI coordinates (origin at top left).
        Rect ButtonArea(int id)
        {
            Rect safe = Screen.safeArea;
            float margin = Mathf.Clamp(safe.width * .015f, 8f, 17f);
            float top = Screen.height - safe.yMax + margin + 73f;
            float gap = margin;
            float rowHeight = Mathf.Clamp(safe.height * .055f, 38f, 58f);
            float width = (safe.width - margin * 2f - gap * 2f) / 3f;
            if (id <= 2)
                return new Rect(safe.xMin + margin + id * (width + gap),
                    top, width, rowHeight);
            if (id >= 6 && id <= 8)
                return new Rect(safe.xMin + margin + (id - 6)*(width + gap),
                    top + rowHeight + 5f, width, rowHeight);
            float bottom = Screen.height - safe.yMin - margin;
            float bottomWidth = Mathf.Min(safe.width * .37f, 215f);
            float right = safe.xMax - margin - bottomWidth;
            if (id == 3) // IDENTIFY
                return new Rect(right, bottom - rowHeight, bottomWidth, rowHeight);
            if (id == 4) // UP
                return new Rect(right, bottom - rowHeight * 2f - gap, bottomWidth, rowHeight);
            // DOWN
            return new Rect(right, bottom - rowHeight * 3f - gap*2f,
                bottomWidth, rowHeight);
        }

        Vector2 JoystickCentre()
        {
            Rect safe = Screen.safeArea;
            float radius = JoystickRadius();
            return new Vector2(safe.xMin + Mathf.Max(radius + 14f, safe.width * .22f),
                safe.yMin + Mathf.Max(radius + 20f, safe.height * .18f));
        }

        float JoystickRadius()
        {
            return Mathf.Clamp(Screen.safeArea.width * .145f, 53f, 95f);
        }

        bool InStick(Vector2 screenPoint)
        {
            return Vector2.Distance(screenPoint, JoystickCentre()) < JoystickRadius() * 1.65f;
        }

        void ApplyLook(Vector2 delta)
        {
            if (!droneMode || paused) return;
            float sensitivity = LookSensitivity / Mathf.Max(340f, Screen.width);
            yaw += delta.x * sensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -20f, 80f);
            UpdateCamera();
        }

        bool HasLineOfSight(Vector3 destination)
        {
            Vector3 direction = destination - cameraView.transform.position;
            if (direction.magnitude <= .30f) return true;
            RaycastHit hit;
            return !Physics.Raycast(cameraView.transform.position,
                direction.normalized, out hit, direction.magnitude - .30f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        void Identify()
        {
            if (paused || !droneMode) return;
            if (identified)
            {
                hint = "The fictional drone is already identified.";
                return;
            }
            Vector3 point = cameraView.WorldToScreenPoint(beacon.transform.position);
            float radius = ScanRadius * Mathf.Min(Screen.width, Screen.height);
            Vector2 offset = new Vector2(point.x - Screen.width * .5f,
                point.y - Screen.height * .5f);
            if (point.z <= 0f || offset.sqrMagnitude > radius * radius)
            {
                hint = "Centre the aerial signal first; the drone is not yet identified.";
                return;
            }
            if (!HasLineOfSight(beacon.transform.position))
            {
                hint = "A building obscures the signal. Fly around it for a clear view.";
                return;
            }
            identified = true;
            hint = "Unregistered survey drone IDENTIFIED. No contact or damage.";
        }

        bool PressControl(Vector2 guiPoint)
        {
            for (int i = 0; i < 9; i++)
            {
                if (!ButtonArea(i).Contains(guiPoint)) continue;
                switch (i)
                {
                    case 0: ToggleCamera(); break;
                    case 1: Restart(); break;
                    case 2: paused = !paused; velocity = Vector3.zero; break;
                    case 3: Identify(); break;
                    case 4: case 5: break; // Hold for vertical movement.
                    case 6: if (droneMode) droneFov = Mathf.Max(31f, droneFov-5f); break;
                    case 7: if (droneMode) droneFov = Mathf.Min(68f, droneFov+5f); break;
                    case 8: FocusBeacon(); break;
                }
                UpdateCamera();
                return true;
            }
            return false;
        }

        void ReadTouch()
        {
            var screen = Touchscreen.current;
            if (screen == null) return;
            foreach (var touch in screen.touches)
            {
                int id = touch.touchId.ReadValue();
                Vector2 point = touch.position.ReadValue();
                Vector2 gui = new Vector2(point.x, Screen.height - point.y);
                if (touch.press.wasReleasedThisFrame)
                {
                    if (id == moveFinger) { moveFinger = -1; stick = Vector2.zero; }
                    if (id == lookFinger) lookFinger = -1;
                    continue;
                }
                if (!touch.press.isPressed) continue;
                if (touch.press.wasPressedThisFrame)
                {
                    if (PressControl(gui)) continue;
                    if (droneMode && InStick(point) && moveFinger < 0)
                        moveFinger = id;
                    else if (droneMode && lookFinger < 0)
                    {
                        lookFinger = id;
                        lastLook = point;
                    }
                }
                if (id == moveFinger)
                    stick = Vector2.ClampMagnitude(
                        (point - JoystickCentre()) / JoystickRadius(), 1f);
                if (id == lookFinger)
                {
                    ApplyLook(point - lastLook);
                    lastLook = point;
                }
                if (ButtonArea(4).Contains(gui)) climb = 1f;
                else if (ButtonArea(5).Contains(gui)) climb = -1f;
            }
            if (moveFinger < 0) stick = Vector2.zero;
        }

        void ReadMouse()
        {
            // Mobile OSes can emulate mouse clicks from touches. Avoid applying
            // the same gesture twice or stealing a second thumb's input.
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
                foreach (var touch in touchscreen.touches)
                    if (touch.press.isPressed || touch.press.wasReleasedThisFrame) return;
            var mouse = Mouse.current;
            if (mouse == null) return;
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > .01f && droneMode)
                droneFov = Mathf.Clamp(droneFov - Mathf.Sign(scroll) * 3.0f, 31f, 68f);

            Vector2 point = mouse.position.ReadValue();
            Vector2 gui = new Vector2(point.x, Screen.height - point.y);
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (!PressControl(gui))
                {
                    mouseMoving = droneMode && InStick(point);
                    mouseLooking = droneMode && !mouseMoving;
                    mouseLast = point;
                }
            }
            if (!mouse.leftButton.isPressed)
            {
                mouseMoving = mouseLooking = false;
                return;
            }
            if (mouseMoving)
                stick = Vector2.ClampMagnitude(
                    (point - JoystickCentre()) / JoystickRadius(), 1f);
            if (mouseLooking)
            {
                ApplyLook(point - mouseLast);
                mouseLast = point;
            }
            if (ButtonArea(4).Contains(gui)) climb = 1f;
            else if (ButtonArea(5).Contains(gui)) climb = -1f;
        }

        void ReadKeyboard()
        {
            var keys = Keyboard.current;
            if (keys == null) return;
            if (keys.tabKey.wasPressedThisFrame) ToggleCamera();
            if (keys.rKey.wasPressedThisFrame) Restart();
            if (keys.pKey.wasPressedThisFrame) paused = !paused;
            if (keys.fKey.wasPressedThisFrame) FocusBeacon();
            if (keys.spaceKey.wasPressedThisFrame) Identify();
            if (!droneMode || paused) return;
            var movement = Vector2.zero;
            if (keys.wKey.isPressed) movement.y++;
            if (keys.sKey.isPressed) movement.y--;
            if (keys.dKey.isPressed) movement.x++;
            if (keys.aKey.isPressed) movement.x--;
            if (movement.sqrMagnitude > .01f) stick = Vector2.ClampMagnitude(movement, 1f);
            if (keys.eKey.isPressed) climb = 1f;
            if (keys.qKey.isPressed) climb = -1f;
            Vector2 look = Vector2.zero;
            if (keys.rightArrowKey.isPressed) look.x = 1f;
            if (keys.leftArrowKey.isPressed) look.x = -1f;
            if (keys.upArrowKey.isPressed) look.y = 1f;
            if (keys.downArrowKey.isPressed) look.y = -1f;
            if (look.sqrMagnitude > .01f) ApplyLook(look * 2.7f);
        }

        void MoveCamera(float dt)
        {
            if (!droneMode || paused) { velocity = Vector3.zero; return; }
            Vector3 desired = HarborDroneFlight.HorizontalMovement(yaw, stick) *
                HarborDroneFlight.MaxHorizontalSpeed + Vector3.up *
                (climb * HarborDroneFlight.MaxVerticalSpeed);
            velocity = Vector3.MoveTowards(velocity, desired, Acceleration * dt);
            Vector3 next = HarborDroneFlight.Advance(cameraPosition, velocity * dt);
            if ((next - cameraPosition).sqrMagnitude < .000001f &&
                desired.sqrMagnitude > .001f) velocity = Vector3.zero;
            cameraPosition = next;
            droneFov = Mathf.Clamp(droneFov, 31f, 68f);
            UpdateCamera();
        }

        void MoveBeacon(float dt)
        {
            orbitTime += dt;
            float angle = orbitTime * .42f;
            beacon.transform.position = BeaconCentre + new Vector3(
                Mathf.Cos(angle) * 1.55f, Mathf.Sin(orbitTime * 1.25f) * .22f,
                Mathf.Sin(angle) * 1.25f);
            beacon.transform.rotation = Quaternion.Euler(0f,
                orbitTime * 21f, Mathf.Sin(orbitTime * .8f) * 4f);
            for (int i = 0; i < spinningDiscs.Length; i++)
                spinningDiscs[i].Rotate(0f, (i % 2 == 0 ? 1f : -1f) * 310f * dt, 0f,
                    Space.Self);
        }

        void Update()
        {
            if (!concourse || !concourse.Traffic || !cameraView) return;
            // One UI tick can use two simultaneous touch fingers; mouse and
            // keyboard are supported for Editor review without Android.
            climb = 0f;
            stick = moveFinger >= 0 || mouseMoving ? stick : Vector2.zero;
            ReadTouch();
            ReadMouse();
            ReadKeyboard();
            if (identified && concourse.IsShiftFlight)
            {
                concourse.FinishShiftFlight();
                return;
            }
            float dt = Mathf.Min(Time.deltaTime, .05f);
            MoveCamera(dt);
            if (!paused) MoveBeacon(dt);
        }

        void OnGUI()
        {
            if (!concourse || !concourse.Traffic || !cameraView) return;
            Rect safe = Screen.safeArea;
            float margin = Mathf.Clamp(safe.width * .015f, 8f, 17f);
            float top = Screen.height - safe.yMax + margin;
            var caption = new GUIStyle(GUI.skin.box)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(safe.width / 43f), 12, 21),
                alignment = TextAnchor.MiddleLeft, wordWrap = true
            };
            var button = new GUIStyle(GUI.skin.box)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(safe.width / 35f), 12, 21),
                alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold
            };
            string mode = droneMode ? "DRONE FLIGHT" : "ROOFTOP OVERVIEW";
            GUI.Box(new Rect(safe.xMin+margin, top, safe.width-2f*margin, 69f),
                "PORT MERIDIAN | " + mode + "\n" +
                "Visitors " + concourse.Traffic.ActiveCount +
                "    Height " + (droneMode ? cameraPosition.y.ToString("F1")+"m" : "roof") +
                "    " + (identified ? "Signal identified" : "Signal active"), caption);
            string[] names =
            {
                droneMode ? "ROOFTOP" : "DRONE", "RESET", paused ? "RESUME" : "PAUSE",
                "IDENTIFY", "UP (HOLD)", "DOWN (HOLD)", "ZOOM +", "ZOOM -", "FIND"
            };
            for (int i = 0; i < names.Length; i++)
                GUI.Box(ButtonArea(i), names[i], button);

            Vector2 centre = JoystickCentre();
            float radius = JoystickRadius();
            if (droneMode)
            {
                var stickStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter, fontSize = 17
                };
                GUI.Box(new Rect(centre.x-radius, Screen.height-centre.y-radius,
                    radius*2f, radius*2f), "FLY\n+\n", stickStyle);
                GUI.Box(new Rect(centre.x+stick.x*radius-13f,
                    Screen.height-centre.y-stick.y*radius-13f, 26f, 26f), "●");
            }
            GUI.Box(new Rect(Screen.width*.5f-17f, Screen.height*.5f-17f,
                34f, 34f), "+", button);
            if (droneMode && !identified)
            {
                Vector3 point = cameraView.WorldToScreenPoint(beacon.transform.position);
                if (point.z > 0f && point.x > 0f && point.x < Screen.width &&
                    point.y > 0f && point.y < Screen.height &&
                    HasLineOfSight(beacon.transform.position))
                    GUI.Box(new Rect(point.x-62f, Screen.height-point.y-47f,124f,29f),
                        "SURVEY SIGNAL", button);
            }
            // Keep the help line above the flying joystick and altitude keys.
            float messageY = ButtonArea(8).yMax + 7f;
            GUI.Box(new Rect(safe.xMin+margin, messageY,
                safe.width-2f*margin, 60f),
                paused ? "PAUSED — use RESUME to continue." : hint, caption);
        }

        void OnDestroy()
        {
            if (beacon) Destroy(beacon);
            foreach (var material in ownedMaterials)
                if (material) Destroy(material);
        }
    }
}
