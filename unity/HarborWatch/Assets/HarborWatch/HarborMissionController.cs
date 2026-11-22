using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HarborWatch
{
    /// <summary>
    /// First playable portrait-friendly Harbor Watch contract. Scan three
    /// fictional signal beacons from the fixed observation roof while the
    /// existing civilian crowd continues to move safely below.
    /// No weapons, projectiles, impact effects or pedestrian interactions.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class HarborMissionController : MonoBehaviour
    {
        const string SaveCredits = "HarborWatch.SignalSurvey.Credits.v1";
        const string SaveCleared = "HarborWatch.SignalSurvey.Cleared.v1";
        const string SaveBestStars = "HarborWatch.SignalSurvey.BestStars.v1";
        const float MinimumFov = 24f;
        const float MaximumFov = 62f;
        const float ScanScreenRadius = .11f;
        static readonly Vector3 Roof = new Vector3(-10f, 10.5f, -12f);
        static readonly Vector3[] SignalLocations =
        {
            new Vector3(-3.0f, 3.1f, -1.5f),
            new Vector3(1.2f, 3.25f, 1.6f),
            new Vector3(7.5f, 3.2f, -1.1f)
        };

        sealed class Signal
        {
            public GameObject Model;
            public Vector3 WorldPosition;
            public bool Cleared;
        }

        readonly List<Material> ownedMaterials = new List<Material>();
        readonly Signal[] signals = new Signal[HarborMissionRules.RequiredSignals];
        HarborCrowdReview review;
        Camera lens;
        Material baseMaterial;
        float yaw, pitch, secondsLeft;
        float lastPinchGap = -1f;
        int cleared, attempts, chain, bestChain;
        readonly HarborRelayDecode decode = new HarborRelayDecode();
        bool decoding;
        string decodeHint;
        int credits;
        float feedbackLeft;
        bool lastScanMatched;
        bool paused, finished, won;
        bool dragging;
        Vector2 previousPointer;
        string message = "Drag to look. Centre a signal and press SCAN.";
        HarborMissionRules.Payout reward;

        public bool IsRunning { get { return !paused && !finished; } }

        void Start()
        {
            review = GetComponent<HarborCrowdReview>();
            lens = GetComponent<Camera>();
            if (!review || !review.ConcourseRoot || !review.Traffic)
            {
                Debug.LogError("Harbor Mission needs the HarborCrowdReview scene setup.");
                enabled = false;
                return;
            }
            baseMaterial = Resources.Load<Material>("ReviewSurface");
            if (!baseMaterial)
            {
                Debug.LogError("Harbor Mission cannot find ReviewSurface material.");
                enabled = false;
                return;
            }

            // This contract's touch HUD is designed for Android portrait.
            // The other two review scenes retain their existing orientation.
            if (Application.isMobilePlatform)
                Screen.orientation = ScreenOrientation.Portrait;

            credits = Mathf.Clamp(PlayerPrefs.GetInt(
                SaveCredits, HarborMissionRules.StartingCredits), 0, 9999999);
            BuildSignals();
            RestartMission();
        }

        Material Painted(Color colour)
        {
            var mat = new Material(baseMaterial);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", colour);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", colour);
            ownedMaterials.Add(mat);
            return mat;
        }

        GameObject Shape(Transform parent, string name, PrimitiveType shape,
            Vector3 localPosition, Vector3 scale, Color colour)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = Painted(colour);
            var collider = part.GetComponent<Collider>();
            if (collider) Destroy(collider);
            return part;
        }

        void BuildSignals()
        {
            for (int i = 0; i < SignalLocations.Length; i++)
            {
                var root = new GameObject("Fictional signal beacon " + (i + 1));
                root.transform.SetParent(review.ConcourseRoot, false);
                root.transform.position = SignalLocations[i];
                var stem = Shape(root.transform, "Beacon mounting",
                    PrimitiveType.Cylinder, new Vector3(0f, -.68f, 0f),
                    new Vector3(.10f, .60f, .10f), new Color(.18f, .28f, .32f));
                stem.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                var emitter = Shape(root.transform, "Unstable relay signal",
                    PrimitiveType.Cube, Vector3.zero, new Vector3(.52f, .52f, .52f),
                    new Color(.95f, .28f, .17f));
                emitter.transform.localRotation = Quaternion.Euler(24f, 45f, 12f);
                Shape(root.transform, "Low signal lamp", PrimitiveType.Sphere,
                    new Vector3(0f, -.3f, 0f),
                    new Vector3(.25f, .16f, .25f), new Color(1f, .72f, .31f));
                signals[i] = new Signal
                {
                    Model = root,
                    WorldPosition = SignalLocations[i],
                    Cleared = false
                };
            }
        }

        void RestartMission()
        {
            decode.Reset();
            decoding = false;
            decodeHint = "Enter the newest relay first.";
            feedbackLeft = 0f;
            cleared = attempts = chain = bestChain = 0;
            secondsLeft = HarborMissionRules.TimeLimitSeconds;
            finished = won = paused = false;
            dragging = false;
            lastPinchGap = -1f;
            reward = new HarborMissionRules.Payout();
            message = "Survey three red signal devices. Visitors are never scanned.";
            foreach (var signal in signals)
            {
                signal.Cleared = false;
                signal.Model.SetActive(true);
            }
            if (review && review.Traffic) review.Traffic.ResetTraffic();
            ResetLens();
        }

        void ResetLens()
        {
            Vector3 aim = (new Vector3(.8f, 2.9f, -.2f) - Roof).normalized;
            yaw = Mathf.Atan2(aim.x, aim.z) * Mathf.Rad2Deg;
            pitch = -Mathf.Asin(aim.y) * Mathf.Rad2Deg;
            lens.fieldOfView = 54f;
            UpdateLens();
        }

        void UpdateLens()
        {
            lens.transform.position = Roof;
            lens.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        Rect ButtonArea(int index)
        {
            Rect safe = Screen.safeArea;
            float margin = Mathf.Clamp(safe.width * .013f, 7f, 15f);
            float height = Mathf.Clamp(safe.height * .072f, 48f, 92f);
            float buttonY = Screen.height - safe.yMin - height - margin;
            float scanWidth = safe.width * .43f;
            float shortWidth = (safe.width - scanWidth - margin * 4f) * .5f;
            if (index == 0) // SCAN, lower right
                return new Rect(safe.xMax - scanWidth - margin, buttonY,
                    scanWidth, height);
            if (index == 1) // ZOOM OUT
                return new Rect(safe.xMin + margin, buttonY, shortWidth, height);
            if (index == 2) // ZOOM IN
                return new Rect(safe.xMin + margin * 2f + shortWidth, buttonY,
                    shortWidth, height);
            // PAUSE and RESTART below the mission header.
            float width = Mathf.Clamp(safe.width * .28f, 100f, 210f);
            float top = Screen.height - safe.yMax + 80f;
            return new Rect(index == 3 ? safe.xMin + margin :
                safe.xMax - margin - width, top, width, height * .68f);
        }

        bool OnControl(Vector2 position)
        {
            for (int i = 0; i < 5; i++)
            {
                if (!ButtonArea(i).Contains(position)) continue;
                if (finished && won && i < 3)
                {
                    if (!decoding && i == 0) decoding = true;
                    else if (decoding && !decode.Complete)
                    {
                        int relay = i == 0 ? 3 : i;
                        decodeHint = decode.Submit(relay) ?
                            "Matched. Continue backwards." : "Check the trace and try again.";
                    }
                    return true;
                }
                switch (i)
                {
                    case 0: Scan(); break;
                    case 1:
                        if (!finished) lens.fieldOfView = Mathf.Clamp(
                            lens.fieldOfView + 5f, MinimumFov, MaximumFov);
                        break;
                    case 2:
                        if (!finished) lens.fieldOfView = Mathf.Clamp(
                            lens.fieldOfView - 5f, MinimumFov, MaximumFov);
                        break;
                    case 3:
                        if (!finished)
                        {
                            paused = !paused;
                            message = paused ? "Survey paused." :
                                "Survey resumed.";
                        }
                        break;
                    case 4: RestartMission(); break;
                }
                return true;
            }
            return false;
        }

        void HandlePointer()
        {
            var touch = Touchscreen.current;
            var mouse = Mouse.current;
            bool useTouch = touch != null &&
                (touch.primaryTouch.press.isPressed ||
                 touch.primaryTouch.press.wasReleasedThisFrame);
            if (useTouch && touch.touches.Count > 1 &&
                touch.touches[0].press.isPressed &&
                touch.touches[1].press.isPressed)
            {
                Vector2 first = touch.touches[0].position.ReadValue();
                Vector2 second = touch.touches[1].position.ReadValue();
                float gap = Vector2.Distance(first, second);
                if (lastPinchGap > 0f && !paused && !finished)
                    lens.fieldOfView = Mathf.Clamp(lens.fieldOfView -
                        (gap - lastPinchGap) * .035f, MinimumFov, MaximumFov);
                lastPinchGap = gap;
                dragging = false;
                return;
            }
            lastPinchGap = -1f;
            if (!useTouch && mouse == null) return;
            bool down = useTouch ? touch.primaryTouch.press.wasPressedThisFrame :
                mouse.leftButton.wasPressedThisFrame;
            bool held = useTouch ? touch.primaryTouch.press.isPressed :
                mouse.leftButton.isPressed;
            Vector2 pointer = useTouch ? touch.primaryTouch.position.ReadValue() :
                mouse.position.ReadValue();
            Vector2 guiPointer = new Vector2(pointer.x, Screen.height - pointer.y);

            if (down)
            {
                dragging = !OnControl(guiPointer) && !paused && !finished;
                previousPointer = pointer;
                return;
            }
            if (!held) { dragging = false; return; }
            if (!dragging) return;
            Vector2 delta = pointer - previousPointer;
            previousPointer = pointer;
            // Frame-independent: pointer delta is already measured in pixels.
            float sensitivity = 92f / Mathf.Max(320f, Screen.width);
            yaw = Mathf.Clamp(yaw - delta.x * sensitivity, -65f, 125f);
            pitch = Mathf.Clamp(pitch + delta.y * sensitivity, 10f, 75f);
            UpdateLens();
        }

        int AimAtSignal()
        {
            float radius = Mathf.Min(Screen.width, Screen.height) * ScanScreenRadius;
            float best = radius * radius;
            int selected = -1;
            for (int i = 0; i < signals.Length; i++)
            {
                if (signals[i].Cleared) continue;
                Vector3 screen = lens.WorldToScreenPoint(signals[i].WorldPosition);
                if (screen.z <= 0f) continue;
                float x = screen.x - Screen.width * .5f;
                float y = screen.y - Screen.height * .5f;
                float square = x*x + y*y;
                if (square < best) { best = square; selected = i; }
            }
            return selected;
        }

        void Scan()
        {
            if (!IsRunning) return;
            attempts++;
            int selected = AimAtSignal();
            lastScanMatched = selected >= 0;
            feedbackLeft = .65f;
            if (selected < 0)
            {
                chain = 0;
                message = "Centre a red relay marker first. No visitor was affected.";
                return;
            }
            signals[selected].Cleared = true;
            signals[selected].Model.SetActive(false);
            decode.Record(selected + 1);
            cleared++;
            chain++;
            bestChain = Mathf.Max(bestChain, chain);
            message = "Signal " + (selected + 1) + " verified safely.";
            if (cleared == HarborMissionRules.RequiredSignals) FinishMission(true);
        }

        void FinishMission(bool success)
        {
            finished = true;
            won = success;
            dragging = false;
            if (!success)
            {
                message = "Time expired. The harbor remains safe. Tap RESTART.";
                return;
            }

            bool firstClear = PlayerPrefs.GetInt(SaveCleared, 0) != 1;
            reward = HarborMissionRules.Settle(
                cleared, attempts, bestChain, firstClear);
            credits = Mathf.Min(9999999, credits + reward.Total);
            PlayerPrefs.SetInt(SaveCredits, credits);
            PlayerPrefs.SetInt(SaveCleared, 1);
            PlayerPrefs.SetInt(SaveBestStars,
                Mathf.Max(PlayerPrefs.GetInt(SaveBestStars, 0), reward.Stars));
            PlayerPrefs.Save();
            message = "Harbor secured by observation. Progress saved.";
        }

        void Update()
        {
            if (!lens || !review || !review.Traffic) return;
            HandlePointer();
            if (IsRunning)
            {
                feedbackLeft = Mathf.Max(0f, feedbackLeft - Time.deltaTime);
                secondsLeft = Mathf.Max(0f, secondsLeft - Time.deltaTime);
                if (secondsLeft <= 0f) FinishMission(false);
            }
        }

        void OnGUI()
        {
            if (!lens || !review || !review.Traffic) return;
            Rect safe = Screen.safeArea;
            float margin = Mathf.Clamp(safe.width * .013f, 7f, 15f);
            float top = Screen.height - safe.yMax + margin;
            var header = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(safe.width / 42f), 13, 22),
                wordWrap = true
            };
            var button = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(safe.width / 27f), 15, 27),
                fontStyle = FontStyle.Bold
            };
            GUI.Box(new Rect(safe.xMin + margin, top,
                safe.width - 2f * margin, 67f),
                "HARBOR WATCH  |  SIGNAL SURVEY\n" +
                "Time " + Mathf.CeilToInt(secondsLeft) +
                "s    Signals " + cleared + "/3    Credits " + credits, header);

            string[] labels = {
                "SCAN", "ZOOM -", "ZOOM +", paused ? "RESUME" : "PAUSE", "RESTART"
            };
            if (finished && won)
            {
                labels[0] = decoding ? "RELAY 3" : "DECODE";
                labels[1] = decoding ? "RELAY 1" : "";
                labels[2] = decoding ? "RELAY 2" : "";
                labels[3] = "";
                if (decode.Complete) labels[0] = labels[1] = labels[2] = "";
            }
            for (int i = 0; i < labels.Length; i++)
                if (labels[i].Length > 0) GUI.Box(ButtonArea(i), labels[i], button);

            float midX = Screen.width * .5f;
            float midY = Screen.height * .5f;
            var reticle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(safe.width / 12f), 23, 55),
                fontStyle = FontStyle.Bold
            };
            if (IsRunning)
            {
                bool aligned = AimAtSignal() >= 0;
                string symbol = aligned ? "[+]" : "+";
                if (feedbackLeft > 0f) symbol = lastScanMatched ? "OK" : "×";
                float pulse = 8f * feedbackLeft / .65f;
                reticle.fontSize = Mathf.Min(reticle.fontSize, 29);
                GUI.Box(new Rect(midX - 30f - pulse, midY - 23f - pulse,
                    60f + 2f * pulse, 46f + 2f * pulse), symbol, reticle);
            }
            var signalStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(safe.width / 49f), 11, 17),
                fontStyle = FontStyle.Bold
            };
            for (int i = 0; i < signals.Length; i++)
            {
                if (signals[i].Cleared) continue;
                Vector3 point = lens.WorldToScreenPoint(signals[i].WorldPosition);
                if (point.z <= 0f ||
                    point.x < 0f || point.x > Screen.width ||
                    point.y < 0f || point.y > Screen.height) continue;
                float y = Screen.height - point.y;
                GUI.Box(new Rect(point.x - 43f, y - 55f, 86f, 27f),
                    "SIGNAL " + (i+1), signalStyle);
            }
            float footerY = ButtonArea(0).y - 86f;
            GUI.Box(new Rect(safe.xMin + margin, footerY,
                safe.width - 2f * margin, 77f),
                message + "\nVisitors: " + review.Traffic.ActiveCount +
                "    Lens " + Mathf.RoundToInt(54f / lens.fieldOfView * 100f) / 100f + "x",
                header);

            if (!finished && !paused) return;
            var popup = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(safe.width / 29f), 15, 25),
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            string text = paused ? "PAUSED\nTap RESUME" :
                won ? "SURVEY COMPLETE\n+" + reward.Total + " credits | " +
                     reward.Stars + " stars\nBalance " + credits + "\nTap DECODE for the return link" :
                      "TIME EXPIRED\n" + cleared + "/3 signals\nTap RESTART";
            if (decoding)
            {
                text = decode.Complete ? "RELAY DECODED\nTerminal link restored.\nTap RESTART for a new survey." :
                    "RELAY DECODE\nScan order: " + decode.Trace +
                    "\nReturn the sequence in reverse.\n" + decode.Matched + " of 3 matched\n" + decodeHint;
            }
            GUI.Box(new Rect(safe.xMin + safe.width * .1f,
                midY - 98f, safe.width * .8f, 196f), text, popup);
        }

        void OnDestroy()
        {
            foreach (var material in ownedMaterials)
                if (material) Destroy(material);
        }
    }
}
