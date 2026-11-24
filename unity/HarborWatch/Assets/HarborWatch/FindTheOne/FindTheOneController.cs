using System;
using System.Collections.Generic;
using HarborWatch.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HarborWatch
{
    /// <summary>
    /// RED SIGNAL · FIND THE ONE — Unity mobile game mode on the Port Meridian
    /// concourse. Read the dossier, watch a crowd of persistent people, provoke
    /// reactions, and tag the one courier who matches every line.
    /// Rules come from HarborWatch.Core (shared with the browser prototype).
    /// The tag is a fictional tracking pulse: no weapons, wounds or deaths.
    /// UI is IMGUI with manual touch hit-testing, like HarborMissionController.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class FindTheOneController : MonoBehaviour
    {
        enum Page { Menu, Cases, Brief, HowTo, Play, Result }

        const string SavePrefix = "RedSignal.FindTheOne.v1.";
        const float DefaultFov = 54f, LensFov = 21f, MinFov = 18f, MaxFov = 62f, Steady = .95f;
        // Echo lens: replay the last CaseCrowd.EchoWindow seconds once per case, from case 4, for EchoCost seconds.
        const float EchoCost = 6f;
        const int EchoFromCase = 3;
        static readonly Vector3 Roof = new Vector3(-10f, 10.5f, -12f);
        // Rooftop and crane positions around the concourse where a counter-sniper can appear.
        static readonly Vector3[] GlintSpots =
        {
            new Vector3(-8.5f, 6.2f, 9.2f), new Vector3(8.5f, 6.4f, 9.4f), new Vector3(12.8f, 5.4f, -5f),
            new Vector3(-13.2f, 4.6f, 3.5f), new Vector3(2.5f, 7.6f, 10.5f)
        };

        HarborCrowdReview review;
        Camera cam;
        CaseCrowd crowd;
        AudioSource sfx;
        FindTheOneAudio bank;
        Page page = Page.Menu;
        bool paused, soundOn = true;

        // Case state
        CaseConfig cfg;
        Case kase;
        int caseIndex = -1, dailyNumber;
        bool daily;
        uint dailyKey;
        float t, timeLeft, nextReveal, heat, lastBeat, lastTick = 99f;
        int revealed, intelBought, shots, wrongTags, provocations, revealedAtTag;
        bool noiseUsed, pingUsed, over;
        // Echo lens
        bool echoOK, echoUsed, echoing;
        float echoHead, echoTo, echoFrom, echoSpeed = 1f;
        CaseCrowd.Walker selected;
        // Tag hold and slow-motion shot
        bool holding, locked;
        float holdT;
        CaseCrowd.Walker shotTarget;
        float shotT = -1f, shotFovFrom;
        bool shotResolved, shotWin, shotLose;
        // Counter-sniper
        bool glintOn;
        Vector3 glintPos;
        float glintLeft, nextGlint;
        // Camera and pointer
        float yaw, pitch, fov = DefaultFov, targetFov = DefaultFov;
        bool pointerDown, dragging, downOnUi, tagPressed;
        Vector2 downPos, lastPos;
        float lastPinch = -1f;
        // Result
        RunStats run;
        ScoreResult result;
        string resultTitle = "", resultLine = "", share = "";
        // Feedback
        string toast = "";
        float toastLeft, flash;
        Color toastColour = Color.white;
        int freshClue = -1;
        float freshLeft;

        // IMGUI resources and the per-frame button registry.
        readonly List<KeyValuePair<Rect, Action>> buttons = new List<KeyValuePair<Rect, Action>>();
        readonly Dictionary<int, Texture2D> solids = new Dictionary<int, Texture2D>();
        Texture2D ring, disc, vignette, flare;
        GUIStyle label, title, small, chip, button, big;
        float u = 1f;
        Rect tagRect;

        public bool IsRunning { get { return page == Page.Play && !paused; } }
        bool LensOn { get { return targetFov < 34f; } }

        // ------------------------------------------------------------ Setup
        void Start()
        {
            review = GetComponent<HarborCrowdReview>();
            cam = GetComponent<Camera>();
            if (!review || !review.ConcourseRoot || !review.SourceMaterial)
            {
                Debug.LogError("Find the One needs the HarborCrowdReview concourse (findTheOneMode).");
                enabled = false;
                return;
            }
            if (Application.isMobilePlatform) Screen.orientation = ScreenOrientation.Portrait;
            crowd = gameObject.AddComponent<CaseCrowd>();
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            bank = new FindTheOneAudio();
            soundOn = PlayerPrefs.GetInt(SavePrefix + "Sound", 1) == 1;
            BuildTextures();
            ResetCamera();
            // Attract mode: a live crowd behind the menu.
            crowd.Build(review.ConcourseRoot, review.SourceMaterial, CaseEngine.Generate(7u, CaseEngine.Cases[3]));
        }

        void BuildTextures()
        {
            ring = Radial(128, (d) => Mathf.Clamp01(1f - Mathf.Abs(d - .9f) * 18f));
            disc = Radial(64, (d) => Mathf.Clamp01((1f - d) * 8f));
            flare = Radial(64, (d) => Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f));
            vignette = Radial(256, (d) => Mathf.Clamp01((d - .96f) * 30f) * .55f, true);
        }

        static Texture2D Radial(int size, Func<float, float> alpha, bool dark = false)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + .5f) / size * 2f - 1f, dy = (y + .5f) / size * 2f - 1f;
                    float a = alpha(Mathf.Sqrt(dx * dx + dy * dy));
                    px[y * size + x] = dark ? new Color(.02f, .04f, .05f, a) : new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        Texture2D Solid(Color c)
        {
            int key = ((int)(c.r * 255) << 24) | ((int)(c.g * 255) << 16) | ((int)(c.b * 255) << 8) | (int)(c.a * 255);
            Texture2D tex;
            if (!solids.TryGetValue(key, out tex))
            {
                tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, c);
                tex.Apply();
                solids[key] = tex;
            }
            return tex;
        }

        void ResetCamera()
        {
            Vector3 aim = (new Vector3(.8f, 1.2f, -.2f) - Roof).normalized;
            yaw = Mathf.Atan2(aim.x, aim.z) * Mathf.Rad2Deg;
            pitch = -Mathf.Asin(aim.y) * Mathf.Rad2Deg;
            fov = targetFov = DefaultFov;
            ApplyCamera();
        }

        void ApplyCamera()
        {
            cam.transform.position = Roof;
            cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            cam.fieldOfView = fov;
        }

        void Play(AudioClip clip, float volume = 1f)
        {
            if (soundOn && clip) sfx.PlayOneShot(clip, volume);
        }

        static void Buzz()
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        void Toast(string text, Color colour)
        {
            toast = text; toastColour = colour; toastLeft = 2.4f;
        }

        // ------------------------------------------------------- Case flow
        void StartCase(int index)
        {
            daily = false;
            caseIndex = index;
            cfg = CaseEngine.Cases[index];
            Begin((uint)UnityEngine.Random.Range(1, int.MaxValue));
        }

        void StartDaily()
        {
            var now = DateTime.Now;
            daily = true;
            dailyKey = CaseEngine.DayKey(now.Year, now.Month, now.Day);
            dailyNumber = CaseEngine.DailyNumber(now.Year, now.Month, now.Day);
            cfg = CaseEngine.DailyConfig(now.Year, now.Month, now.Day);
            Begin(dailyKey);
        }

        void Begin(uint seed)
        {
            kase = CaseEngine.Generate(seed, cfg);
            if (!crowd.Build(review.ConcourseRoot, review.SourceMaterial, kase)) return;
            t = 0f; timeLeft = cfg.Time; nextReveal = cfg.Reveal; heat = 0f;
            revealed = 1; intelBought = 0; shots = 2; wrongTags = 0; provocations = 0; revealedAtTag = 0;
            noiseUsed = pingUsed = over = false;
            echoOK = (daily || caseIndex >= EchoFromCase) && (cfg.Noise || cfg.Ping);
            echoUsed = echoing = false; echoSpeed = 1f;
            crowd.RecordEcho = echoOK;
            selected = null; holding = locked = false; shotT = -1f;
            glintOn = false; nextGlint = cfg.Hunter ? 13f + UnityEngine.Random.value * 6f : float.MaxValue;
            lastTick = 99f; freshClue = 0; freshLeft = 1.2f;
            Time.timeScale = 1f;
            ResetCamera();
            page = Page.Play; paused = false;
            Play(bank.Clue);
            Toast("Intel: " + FindTheOneText.Clue(kase.Clues[0]), Gold);
        }

        void Reveal(bool bought)
        {
            revealed++;
            nextReveal = t + cfg.Reveal;
            if (bought) { intelBought++; timeLeft -= 8f; }
            freshClue = revealed - 1; freshLeft = 1.2f;
            Play(bank.Clue);
            Toast("New intel: " + FindTheOneText.Clue(kase.Clues[revealed - 1]), Gold);
        }

        void Spook(string why)
        {
            crowd.CourierAlert = true;
            if (timeLeft > 15f) timeLeft = 15f;
            Play(bank.Bad); Buzz();
            Toast(why + " · 15 s!", Red);
        }

        void UseNoise()
        {
            if (!cfg.Noise || noiseUsed || shotT >= 0f || over || echoing) return;
            noiseUsed = true; provocations++;
            crowd.Noise();
            Play(bank.Horn);
            Toast("Ferry horn! Who didn't look?", Color.white);
        }

        void UsePing()
        {
            if (!cfg.Ping || pingUsed || shotT >= 0f || over || echoing) return;
            pingUsed = true; provocations++;
            crowd.Ping();
            Play(bank.Ping);
            Toast("Ping sent. Watch for hands to ears", Color.white);
        }

        void ToggleLens()
        {
            targetFov = LensOn ? DefaultFov : LensFov;
        }

        void ToggleMark()
        {
            if (selected == null) { Toast("Select a person to mark them", Color.white); return; }
            selected.Marked = !selected.Marked;
        }

        // ------------------------------------------------------ Echo lens
        void StartEcho()
        {
            if (page != Page.Play || !echoOK || echoUsed || echoing || over || paused || shotT >= 0f) return;
            if (!crowd.EchoReady) { Toast("Nothing to rewind yet. Watch the crowd a little longer", Color.white); return; }
            if (timeLeft <= EchoCost + 2f) { Toast("Not enough time left for an Echo", Red); return; }
            if (!crowd.BeginEcho()) return;
            holding = locked = false;
            echoUsed = true; timeLeft -= EchoCost;
            echoFrom = echoHead = crowd.OldestEcho; echoTo = crowd.NewestEcho; echoSpeed = 1f;
            echoing = true;
            crowd.ShowEcho(echoHead);
            Play(bank.Rewind);
            Toast("Echo: last " + Mathf.RoundToInt(echoTo - echoFrom) + " s of the crowd. Drag and zoom to study it", Gold);
        }

        void UpdateEcho(float realDt)
        {
            echoHead += realDt * echoSpeed;
            if (echoHead >= echoTo) { StopEcho(); return; }
            crowd.ShowEcho(echoHead);
        }

        void StopEcho()
        {
            if (!echoing) return;
            crowd.EndEcho();
            echoing = false;
            Play(bank.Forward);
            Toast("Back to live. The clock is running", Color.white);
        }

        void StartHold()
        {
            if (over || paused || shotT >= 0f || echoing) return;
            if (selected == null) { Toast("Tap a person first to select them", Color.white); return; }
            holding = true; locked = false; holdT = 0f;
        }

        void ReleaseHold()
        {
            if (!holding) return;
            holding = false;
            if (!locked) { heat = Mathf.Min(1f, heat + .06f); Toast("Too early. Hold until the ring locks", Color.white); return; }
            if (LineBlocked(selected)) { Play(bank.Block); Toast("Line blocked by a passer-by. Wait for a clear shot", Red); return; }
            revealedAtTag = revealed;
            shotTarget = selected; shotT = 0f; shotResolved = shotWin = shotLose = false; shotFovFrom = fov;
            targetFov = DefaultFov;
            Time.timeScale = .15f;
            Play(bank.Shot); Buzz();
        }

        void UpdateShot(float realDt)
        {
            shotT += realDt;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(shotT / .9f), 3f);
            fov = Mathf.Lerp(shotFovFrom, 14f, k);
            AimAt(shotTarget.Chest, 1f - Mathf.Pow(.001f, realDt));
            if (shotT >= 1.15f && !shotResolved)
            {
                shotResolved = true;
                if (shotTarget == crowd.Courier) { shotWin = true; Play(bank.Good); }
                else
                {
                    Play(bank.Bad); Buzz();
                    shotTarget.Wrong = true; shots--; wrongTags++; selected = null;
                    shotLose = shots <= 0;
                }
            }
            if (shotT >= 2.1f)
            {
                shotT = -1f; Time.timeScale = 1f; fov = targetFov = DefaultFov;
                if (shotWin) End(true, "");
                else if (shotLose) End(false, "Two wrong tags. The courier slipped away.");
                else Spook("Wrong person. The courier saw it");
            }
        }

        bool LineBlocked(CaseCrowd.Walker target)
        {
            Vector3 c = cam.WorldToScreenPoint(target.Chest);
            float dist = Vector3.Distance(Roof, target.Chest);
            foreach (var q in crowd.Walkers)
            {
                if (q == target || !q.Present || Vector3.Distance(Roof, q.Chest) >= dist) continue;
                Vector3 feet = cam.WorldToScreenPoint(q.Feet), head = cam.WorldToScreenPoint(q.HeadTop);
                if (feet.z <= 0f) continue;
                float h = head.y - feet.y, w = h * .38f;
                if (Mathf.Abs(feet.x - c.x) < w * .55f && c.y > feet.y && c.y < head.y) return true;
            }
            return false;
        }

        void End(bool win, string why)
        {
            if (over) return;
            over = true; holding = false; glintOn = false; targetFov = DefaultFov;
            run = new RunStats
            {
                Cleared = win, Total = cfg.Clues.Length, Revealed = win ? revealedAtTag : revealed,
                SecondsLeft = timeLeft, Provocations = provocations, WrongTags = wrongTags,
                IntelBought = intelBought, Elapsed = t
            };
            result = CaseEngine.Score(run);
            var parts = new List<string>();
            foreach (var c in kase.Clues) parts.Add(FindTheOneText.Clue(c));
            resultTitle = win ? (wrongTags > 0 ? "Tagged on the second shot" : "Courier tagged") : "The courier escaped";
            resultLine = win
                ? "Found with " + run.Revealed + " of " + run.Total + " intel lines in " + Clock(t) + "."
                : why + "\nThe courier was: " + string.Join(" · ", parts.ToArray()) + ".";
            share = "";
            if (daily)
            {
                share = CaseEngine.ShareText(dailyNumber, run, result);
                string key = SavePrefix + "Daily." + dailyKey;
                if (!PlayerPrefs.HasKey(key)) PlayerPrefs.SetString(key, (win ? 1 : 0) + "|" + result.Score + "|" + result.Stars + "|" + share);
            }
            else if (win)
            {
                string key = SavePrefix + "Case." + caseIndex;
                PlayerPrefs.SetInt(key + ".Stars", Mathf.Max(result.Stars, PlayerPrefs.GetInt(key + ".Stars", 0)));
                PlayerPrefs.SetInt(key + ".Score", Mathf.Max(result.Score, PlayerPrefs.GetInt(key + ".Score", 0)));
            }
            PlayerPrefs.Save();
            if (!win) { crowd.EnsurePresent(crowd.Courier); selected = crowd.Courier; targetFov = 30f; }   // show who it was
            Invoke("ShowResult", win ? .3f : 1.8f);
        }

        void ShowResult() { page = Page.Result; }

        int Stars(int index) { return PlayerPrefs.GetInt(SavePrefix + "Case." + index + ".Stars", 0); }
        bool Unlocked(int index) { return index == 0 || Stars(index - 1) > 0; }

        static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return (s / 60) + ":" + (s % 60).ToString("00");
        }

        void Share(string text)
        {
            GUIUtility.systemCopyBuffer = text;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var intentClass = new AndroidJavaClass("android.content.Intent"))
                using (var intent = new AndroidJavaObject("android.content.Intent"))
                {
                    intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                    intent.Call<AndroidJavaObject>("setType", "text/plain");
                    intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, "Share result"))
                        activity.Call("startActivity", chooser);
                }
                return;
            }
            catch (Exception e) { Debug.LogWarning("Share sheet unavailable: " + e.Message); }
#endif
            Toast("Result copied to clipboard", Gold);
        }

        // ---------------------------------------------------------- Update
        void Update()
        {
            if (!crowd) return;
            float dt = Time.deltaTime, realDt = Time.unscaledDeltaTime;
            HandlePointer();
            if (page == Page.Play && !paused) UpdatePlay(dt, realDt);
            else if (page != Page.Play) crowd.Step(dt);
            toastLeft -= realDt; freshLeft -= realDt; flash = Mathf.Max(0f, flash - realDt * 2f);
            if (shotT < 0f) fov = Mathf.Lerp(fov, targetFov, 1f - Mathf.Pow(.001f, realDt));
            if (page == Page.Play && !paused && LensOn && selected != null && shotT < 0f && !echoing)
                AimAt(selected.Chest, 1f - Mathf.Pow(.02f, realDt));
            ApplyCamera();
        }

        void UpdatePlay(float dt, float realDt)
        {
            if (echoing) { UpdateEcho(realDt); return; }   // the live crowd is frozen while an Echo plays
            if (shotT >= 0f) { UpdateShot(realDt); crowd.Step(dt); return; }
            crowd.Step(dt);
            if (over) return;
            if (selected != null && !selected.Present) { selected = null; holding = locked = false; }   // walked off the set
            t += dt; timeLeft -= dt;
            if (revealed < cfg.Clues.Length && t >= nextReveal) Reveal(false);

            heat = Mathf.Clamp01(heat + (LensOn ? dt / cfg.Heat : -dt / (cfg.Heat * .5f)));
            if (heat >= 1f) { heat = .55f; Spook("Your lens glinted. The courier is on the move"); }

            if (cfg.Hunter)
            {
                if (!glintOn && t >= nextGlint)
                {
                    glintOn = true; glintLeft = 4.5f;
                    glintPos = GlintSpots[UnityEngine.Random.Range(0, GlintSpots.Length)];
                    Play(bank.Glint); Toast("Glint on a rooftop! Tap it", Red);
                }
                if (glintOn && (glintLeft -= dt) <= 0f)
                {
                    glintOn = false; nextGlint = t + 16f + UnityEngine.Random.value * 8f;
                    timeLeft -= 10f; flash = 1f; Play(bank.Bad); Buzz();
                    Toast("Counter-sniper fired! -10 s", Red);
                }
            }

            if (holding)
            {
                holdT += realDt;
                if (t - lastBeat > .62f) { lastBeat = t; Play(bank.Beat, .9f); }
                if (!locked && holdT >= Steady) { locked = true; Play(bank.Lock); }
                if (holdT > Steady + 2.6f) { holding = false; Toast("Out of breath. Steady again", Red); }
            }
            if (timeLeft <= 10f && Mathf.Ceil(timeLeft) != lastTick) { lastTick = Mathf.Ceil(timeLeft); Play(bank.Tick, .6f); }
            if (timeLeft <= 0f) { timeLeft = 0f; End(false, "The courier reached the drop point."); }
        }

        void AimAt(Vector3 point, float blend)
        {
            Vector3 d = (point - Roof).normalized;
            float y = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, p = -Mathf.Asin(d.y) * Mathf.Rad2Deg;
            yaw = Mathf.LerpAngle(yaw, y, blend);
            pitch = Mathf.Lerp(pitch, p, blend);
        }

        // ----------------------------------------------------------- Input
        void HandlePointer()
        {
            var touch = Touchscreen.current;
            var mouse = Mouse.current;
            bool useTouch = touch != null && (touch.primaryTouch.press.isPressed || touch.primaryTouch.press.wasReleasedThisFrame);
            if (useTouch && touch.touches.Count > 1 && touch.touches[0].press.isPressed && touch.touches[1].press.isPressed)
            {
                float gap = Vector2.Distance(touch.touches[0].position.ReadValue(), touch.touches[1].position.ReadValue());
                if (page == Page.Play && lastPinch > 0f && shotT < 0f)
                    targetFov = Mathf.Clamp(targetFov - (gap - lastPinch) * .05f, MinFov, MaxFov);
                lastPinch = gap; dragging = false;
                return;
            }
            lastPinch = -1f;
            if (mouse != null && page == Page.Play)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > .01f) targetFov = Mathf.Clamp(targetFov - Mathf.Sign(scroll) * 4f, MinFov, MaxFov);
            }
            if (!useTouch && mouse == null) return;
            bool down = useTouch ? touch.primaryTouch.press.wasPressedThisFrame : mouse.leftButton.wasPressedThisFrame;
            bool held = useTouch ? touch.primaryTouch.press.isPressed : mouse.leftButton.isPressed;
            Vector2 raw = useTouch ? touch.primaryTouch.position.ReadValue() : mouse.position.ReadValue();
            Vector2 gui = new Vector2(raw.x, Screen.height - raw.y);

            if (down)
            {
                pointerDown = true; dragging = false; downPos = lastPos = raw;
                tagPressed = page == Page.Play && tagRect.Contains(gui);
                downOnUi = tagPressed || HitButton(gui) >= 0;
                if (tagPressed) StartHold();
                return;
            }
            if (pointerDown && held)
            {
                if (!dragging && !downOnUi && (raw - downPos).magnitude > 8f * u) dragging = true;
                if (dragging && page == Page.Play && !paused && shotT < 0f && !(LensOn && selected != null && !echoing))
                {
                    Vector2 delta = raw - lastPos;
                    float sensitivity = 92f / Mathf.Max(320f, Screen.width) * (fov / DefaultFov);
                    yaw = Mathf.Clamp(yaw - delta.x * sensitivity, -65f, 125f);
                    pitch = Mathf.Clamp(pitch + delta.y * sensitivity, 10f, 75f);
                }
                lastPos = raw;
                return;
            }
            if (pointerDown && !held)
            {
                pointerDown = false;
                if (tagPressed) { tagPressed = false; ReleaseHold(); return; }
                if (dragging) { dragging = false; return; }
                int hit = HitButton(gui);
                if (hit >= 0) { buttons[hit].Value(); return; }
                if (page == Page.Play && !paused && !over && shotT < 0f && !echoing) Tap(gui);
            }
        }

        int HitButton(Vector2 gui)
        {
            for (int i = buttons.Count - 1; i >= 0; i--) if (buttons[i].Key.Contains(gui)) return i;
            return -1;
        }

        bool BodyRect(CaseCrowd.Walker w, out Rect r)
        {
            Vector3 feet = cam.WorldToScreenPoint(w.Feet), head = cam.WorldToScreenPoint(w.HeadTop);
            r = new Rect();
            if (!w.Present || feet.z <= 0f) return false;
            float h = head.y - feet.y, width = Mathf.Max(h * .42f, 18f * u);
            r = new Rect(feet.x - width * .5f, Screen.height - head.y, width, h);
            return true;
        }

        void Tap(Vector2 gui)
        {
            if (glintOn)
            {
                Vector3 g = cam.WorldToScreenPoint(glintPos);
                if (g.z > 0f && Vector2.Distance(new Vector2(g.x, Screen.height - g.y), gui) < 48f * u)
                {
                    glintOn = false; nextGlint = t + 16f + UnityEngine.Random.value * 8f;
                    Play(bank.Lock); Toast("Counter-sniper blinded", Gold);
                    return;
                }
            }
            CaseCrowd.Walker best = null;
            float bestDistance = float.MaxValue;
            foreach (var w in crowd.Walkers)
            {
                Rect r;
                if (!BodyRect(w, out r) || !r.Contains(gui)) continue;
                float d = Vector3.Distance(Roof, w.Chest);
                if (d < bestDistance) { bestDistance = d; best = w; }
            }
            if (best == null)
            {
                float near = 40f * u;
                foreach (var w in crowd.Walkers)
                {
                    if (!w.Present) continue;
                    Vector3 c = cam.WorldToScreenPoint(w.Chest);
                    if (c.z <= 0f) continue;
                    float d = Vector2.Distance(new Vector2(c.x, Screen.height - c.y), gui);
                    if (d < near) { near = d; best = w; }
                }
            }
            selected = best != null && !best.Wrong ? best : null;
            if (selected != null) Play(bank.Select, .6f);
        }

        // -------------------------------------------------------------- UI
        static readonly Color Ink = new Color(.96f, .94f, .89f);
        static readonly Color Panel = new Color(.05f, .075f, .085f, .86f);
        static readonly Color Red = new Color(.965f, .30f, .216f);
        static readonly Color Gold = new Color(1f, .82f, .47f);
        static readonly Color Teal = new Color(.51f, .945f, .86f);

        void Styles()
        {
            Rect s = Screen.safeArea;
            u = Mathf.Max(1f, Mathf.Min(s.width / 410f, s.height / 760f));
            if (label != null && Mathf.Approximately(label.fontSize, Mathf.RoundToInt(15 * u))) return;
            label = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(15 * u), wordWrap = true, richText = true };
            label.normal.textColor = Ink;
            small = new GUIStyle(label) { fontSize = Mathf.RoundToInt(12 * u) };
            title = new GUIStyle(label) { fontSize = Mathf.RoundToInt(30 * u), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            big = new GUIStyle(title) { fontSize = Mathf.RoundToInt(56 * u), alignment = TextAnchor.MiddleCenter };
            chip = new GUIStyle(label) { fontSize = Mathf.RoundToInt(14 * u), wordWrap = false, alignment = TextAnchor.MiddleLeft, padding = new RectOffset((int)(9 * u), (int)(9 * u), 0, 0) };
            button = new GUIStyle(label) { fontSize = Mathf.RoundToInt(16 * u), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        }

        Rect Safe()
        {
            Rect s = Screen.safeArea;
            return new Rect(s.x, Screen.height - s.yMax, s.width, s.height);
        }

        void Fill(Rect r, Color c) { GUI.DrawTexture(r, Solid(c)); }

        void Frame(Rect r, Color c, float w)
        {
            Fill(new Rect(r.x, r.y, r.width, w), c); Fill(new Rect(r.x, r.yMax - w, r.width, w), c);
            Fill(new Rect(r.x, r.y, w, r.height), c); Fill(new Rect(r.xMax - w, r.y, w, r.height), c);
        }

        bool Button(Rect r, string text, Action action, Color bg, bool enabled = true)
        {
            Fill(r, enabled ? bg : new Color(bg.r, bg.g, bg.b, bg.a * .35f));
            Frame(r, new Color(1f, 1f, 1f, .14f), Mathf.Max(1f, u));
            var prev = GUI.color;
            if (!enabled) GUI.color = new Color(1f, 1f, 1f, .4f);
            GUI.Label(r, text, button);
            GUI.color = prev;
            if (enabled && Event.current.type == EventType.Repaint) buttons.Add(new KeyValuePair<Rect, Action>(r, action));
            return enabled;
        }

        void Text(Rect r, string text, GUIStyle style, Color colour)
        {
            var prev = style.normal.textColor;
            style.normal.textColor = colour;
            GUI.Label(r, text, style);
            style.normal.textColor = prev;
        }

        void OnGUI()
        {
            if (!crowd) return;
            Styles();
            if (Event.current.type == EventType.Repaint) buttons.Clear();
            Rect s = Safe();
            switch (page)
            {
                case Page.Menu: DrawMenu(s); break;
                case Page.Cases: DrawCases(s); break;
                case Page.Brief: DrawBrief(s); break;
                case Page.HowTo: DrawHowTo(s); break;
                case Page.Play: DrawPlay(s); break;
                case Page.Result: DrawPlay(s); DrawResult(s); break;
            }
        }

        void DrawMenu(Rect s)
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(.03f, .05f, .06f, .55f));
            float w = Mathf.Min(s.width * .86f, 420f * u), x = s.center.x - w * .5f, y = s.y + s.height * .2f;
            Text(new Rect(x, y, w, 70 * u), "RED SIGNAL", big, Ink);
            var tag = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            Text(new Rect(x, y + 66 * u, w, 22 * u), "F I N D   T H E   O N E", tag, Red);
            var pitchStyle = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            Text(new Rect(x, y + 96 * u, w, 50 * u), "A courier is hiding in the crowd. You get a dossier, a lens and one clean shot.", pitchStyle, Ink);
            float by = y + 170 * u, bh = 58 * u;
            var now = DateTime.Now;
            uint key = CaseEngine.DayKey(now.Year, now.Month, now.Day);
            string saved = PlayerPrefs.GetString(SavePrefix + "Daily." + key, "");
            string dailyLabel = "Daily Case #" + CaseEngine.DailyNumber(now.Year, now.Month, now.Day) +
                (saved.Length > 0 ? (saved[0] == '1' ? "\n<size=" + (int)(12 * u) + ">Solved · tap to share</size>" : "\n<size=" + (int)(12 * u) + ">Courier escaped · tap to share</size>")
                                  : "\n<size=" + (int)(12 * u) + ">Same case for everyone today</size>");
            button.richText = true;
            Button(new Rect(x, by, w, bh), dailyLabel, () =>
            {
                if (saved.Length > 0) { Share(saved.Substring(saved.IndexOf('|', saved.IndexOf('|', 2) + 1) + 1)); return; }
                StartDaily();
            }, Red);
            Button(new Rect(x, by + bh + 10 * u, w, bh), "Campaign", () => page = Page.Cases, Panel);
            Button(new Rect(x, by + 2 * (bh + 10 * u), w, bh), "How to play", () => page = Page.HowTo, Panel);
            Button(new Rect(s.xMax - 64 * u, s.y + 10 * u, 54 * u, 40 * u), soundOn ? "SND" : "MUTE", () =>
            {
                soundOn = !soundOn; PlayerPrefs.SetInt(SavePrefix + "Sound", soundOn ? 1 : 0); PlayerPrefs.Save();
            }, Panel);
            var foot = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };
            Text(new Rect(s.x, s.yMax - 30 * u, s.width, 24 * u), "Harbor Watch · Port Meridian prototype", foot, new Color(.7f, .72f, .7f));
        }

        void DrawCases(Rect s)
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(.03f, .05f, .06f, .9f));
            Button(new Rect(s.x + 10 * u, s.y + 10 * u, 54 * u, 44 * u), "<", () => page = Page.Menu, Panel);
            int total = 0;
            for (int i = 0; i < CaseEngine.Cases.Length; i++) total += Stars(i);
            Text(new Rect(s.x + 76 * u, s.y + 10 * u, s.width, 44 * u), "Campaign   <size=" + (int)(16 * u) + ">★ " + total + " / " + CaseEngine.Cases.Length * 3 + "</size>", title, Ink);
            float top = s.y + 66 * u, rows = 6, gap = 8 * u;
            float h = Mathf.Min(86 * u, (s.height - (top - s.y) - 20 * u) / rows - gap), w = (s.width - 30 * u) * .5f;
            for (int i = 0; i < CaseEngine.Cases.Length; i++)
            {
                int index = i;
                var c = CaseEngine.Cases[i];
                var r = new Rect(s.x + 10 * u + (i % 2) * (w + 10 * u), top + (i / 2) * (h + gap), w, h);
                bool open = Unlocked(i);
                int stars = Stars(i);
                string starText = stars > 0 ? new string('★', stars) + new string('☆', 3 - stars) : open ? "" : "locked";
                string text = "<size=" + (int)(11 * u) + ">" + CaseEngine.Districts[c.District].Name.ToUpper() + " · " + (i + 1).ToString("00") + "</size>\n" + c.Name + "\n<size=" + (int)(13 * u) + ">" + starText + "</size>";
                Button(r, text, () => { caseIndex = index; daily = false; cfg = CaseEngine.Cases[index]; page = Page.Brief; }, open ? Panel : new Color(.1f, .1f, .1f, .8f), open);
            }
        }

        void DrawBrief(Rect s)
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(.03f, .05f, .06f, .78f));
            float x = s.x + 18 * u, w = s.width - 36 * u, y = s.y + s.height * .3f;
            Text(new Rect(x, y, w, 20 * u), "CASE " + (caseIndex + 1).ToString("00") + " · " + CaseEngine.Districts[cfg.District].Name.ToUpper(), small, Red);
            Text(new Rect(x, y + 20 * u, w, 44 * u), cfg.Name, title, Ink);
            Text(new Rect(x, y + 66 * u, w, 70 * u), cfg.Brief, label, Ink);
            var facts = new List<string>
            {
                cfg.Crowd + " people on the concourse",
                cfg.Clues.Length + " lines of intel, one every " + cfg.Reveal + " s",
                Clock(cfg.Time) + " before the courier reaches the drop",
                "Two shots. A wrong tag spooks the courier"
            };
            if (cfg.Noise) facts.Add("HORN: everyone looks, except the trained courier");
            if (cfg.Ping) facts.Add("PING: Red Signal members touch their earpiece");
            if (cfg.Hunter) facts.Add("Counter-sniper: tap rooftop glints");
            if (caseIndex >= EchoFromCase && (cfg.Noise || cfg.Ping)) facts.Add("ECHO: replay the last " + (int)CaseCrowd.EchoWindow + " s of the crowd, once (-" + (int)EchoCost + " s)");
            float fy = y + 144 * u;
            foreach (var f in facts) { Fill(new Rect(x, fy, w, 30 * u), new Color(1, 1, 1, .05f)); Text(new Rect(x + 10 * u, fy + 4 * u, w, 26 * u), f, small, Ink); fy += 34 * u; }
            float bw = (w - 10 * u) * .5f, by = Mathf.Min(fy + 14 * u, s.yMax - 70 * u);
            Button(new Rect(x, by, bw, 56 * u), "Back", () => page = Page.Cases, Panel);
            Button(new Rect(x + bw + 10 * u, by, bw, 56 * u), "Take position", () => StartCase(caseIndex), Red);
        }

        void DrawHowTo(Rect s)
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(.03f, .05f, .06f, .92f));
            Button(new Rect(s.x + 10 * u, s.y + 10 * u, 54 * u, 44 * u), "<", () => page = Page.Menu, Panel);
            Text(new Rect(s.x + 76 * u, s.y + 10 * u, s.width, 44 * u), "How to play", title, Ink);
            Text(new Rect(s.x + 18 * u, s.y + 70 * u, s.width - 36 * u, s.height - 90 * u), FindTheOneText.HowTo, label, Ink);
        }

        void DrawPlay(Rect s)
        {
            if (kase == null) return;
            DrawWorldOverlays();
            if (LensOn && shotT < 0f && !over)
            {
                float d = Mathf.Min(Screen.width, Screen.height) * .94f;
                var r = new Rect((Screen.width - d) * .5f, (Screen.height - d) * .5f, d, d);
                GUI.DrawTexture(r, vignette);
                var dark = new Color(.02f, .04f, .05f, .55f);
                Fill(new Rect(0, 0, Screen.width, r.y), dark); Fill(new Rect(0, r.yMax, Screen.width, Screen.height - r.yMax), dark);
                Fill(new Rect(0, r.y, r.x, d), dark); Fill(new Rect(r.xMax, r.y, Screen.width - r.xMax, d), dark);
            }
            if (shotT >= 0f)
            {
                float bar = Mathf.Min(1f, shotT * 4f) * Screen.height * .08f;
                Fill(new Rect(0, 0, Screen.width, bar), Color.black); Fill(new Rect(0, Screen.height - bar, Screen.width, bar), Color.black);
            }
            if (echoing)
            {
                Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(.15f, .36f, .59f, .2f));
                float line = Mathf.Max(1f, u), pitch = 5f * u;
                for (float yy = 0f; yy < Screen.height; yy += pitch) Fill(new Rect(0, yy, Screen.width, line), new Color(0, 0, 0, .13f));
            }
            if (crowd.CourierAlert && !over) Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(Red.r, Red.g, Red.b, .05f + .04f * Mathf.Sin(Time.unscaledTime * 8f)));
            if (flash > 0f) Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(1f, .24f, .16f, flash * .45f));
            if (page != Page.Play) return;

            // Top bar
            float top = s.y + 6 * u, barH = 48 * u;
            Fill(new Rect(0, 0, Screen.width, top + barH + 4 * u), Panel);
            Button(new Rect(s.x + 8 * u, top, 44 * u, 44 * u), paused ? ">" : "II", () => paused = !paused, new Color(0, 0, 0, .35f));
            Text(new Rect(s.x + 60 * u, top - 2 * u, s.width * .5f, 18 * u), daily ? "DAILY" : CaseEngine.Districts[cfg.District].Name.ToUpper(), small, Red);
            Text(new Rect(s.x + 60 * u, top + 14 * u, s.width * .55f, 30 * u), daily ? cfg.Name : (caseIndex + 1) + " · " + cfg.Name, new GUIStyle(chip) { fontStyle = FontStyle.Bold, fontSize = (int)(18 * u), padding = new RectOffset() }, Ink);
            var timer = new GUIStyle(title) { alignment = TextAnchor.MiddleRight, fontSize = (int)(28 * u) };
            Text(new Rect(s.xMax - 150 * u, top, 100 * u, barH), Clock(timeLeft), timer, timeLeft <= 15f && Mathf.Repeat(Time.unscaledTime, 1f) < .6f ? Red : Ink);
            for (int i = 0; i < 2; i++) Fill(new Rect(s.xMax - 40 * u + i * 15 * u, top + 12 * u, 10 * u, 24 * u), i < shots ? Gold : new Color(.25f, .27f, .27f));
            // Lens heat
            float heatY = top + barH + 4 * u;
            Fill(new Rect(0, heatY, Screen.width, 4 * u), new Color(1, 1, 1, .08f));
            Fill(new Rect(0, heatY, Screen.width * heat, 4 * u), Color.Lerp(Gold, Red, heat));
            if (LensOn || heat > .05f) Text(new Rect(s.xMax - 110 * u, heatY + 4 * u, 100 * u, 16 * u), "LENS HEAT", new GUIStyle(small) { alignment = TextAnchor.UpperRight, fontSize = (int)(10 * u) }, new Color(.7f, .72f, .7f));

            // Dossier chips
            float cx = s.x + 8 * u, cy = heatY + 12 * u, ch = 32 * u;
            for (int i = 0; i < kase.Clues.Length; i++)
            {
                var clue = kase.Clues[i];
                bool open = i < revealed;
                string text = (i + 1) + "  " + (open ? "<b>" + FindTheOneText.Clue(clue) + "</b>" + (CaseEngine.Needs(clue.Attr) != null ? "  <size=" + (int)(11 * u) + "><color=#82f1db>(use " + CaseEngine.Needs(clue.Attr) + ")</color></size>" : "")
                    : "locked " + Clock(nextReveal - t + (i - revealed) * cfg.Reveal) + (i == revealed ? "  <size=" + (int)(11 * u) + "><color=#ffd277>tap: now -8s</color></size>" : ""));
                chip.richText = true;
                float w = chip.CalcSize(new GUIContent(text)).x + 4 * u;
                if (cx + w > s.xMax - 8 * u) { cx = s.x + 8 * u; cy += ch + 6 * u; }
                var r = new Rect(cx, cy, w, ch);
                Color bg = open && i == freshClue && freshLeft > 0f ? new Color(.35f, .27f, .08f, .92f) : Panel;
                Fill(r, bg);
                Frame(r, open ? new Color(1, 1, 1, .16f) : new Color(1, .82f, .47f, .3f), Mathf.Max(1f, u));
                Text(r, text, chip, open ? Ink : new Color(.68f, .7f, .68f));
                if (!open && i == revealed && Event.current.type == EventType.Repaint)
                    buttons.Add(new KeyValuePair<Rect, Action>(r, () => { if (!over && shotT < 0f && !echoing) Reveal(true); }));
                cx += w + 6 * u;
            }

            // Toast
            if (toastLeft > 0f && toast.Length > 0)
            {
                var ts = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                float tw = Mathf.Min(s.width - 30 * u, ts.CalcSize(new GUIContent(toast)).x + 30 * u);
                var tr = new Rect(s.center.x - tw * .5f, s.yMax - 170 * u, tw, 40 * u);
                Fill(tr, new Color(.04f, .055f, .06f, .88f));
                Text(tr, toast, ts, toastColour);
            }

            // Bottom controls (smaller when all five fit: lens, mark, horn, ping, echo)
            int tools = 2 + (cfg.Noise ? 1 : 0) + (cfg.Ping ? 1 : 0) + (echoOK ? 1 : 0);
            float bs = (tools >= 5 ? 48 : 62) * u, gap = (tools >= 5 ? 4 : 6) * u, by = s.yMax - bs - 12 * u, bx = s.x + 10 * u;
            Button(new Rect(bx, by, bs, bs), "LENS", ToggleLens, LensOn ? new Color(.35f, .27f, .08f, .92f) : Panel);
            Button(new Rect(bx + (bs + gap), by, bs, bs), selected != null && selected.Marked ? "UNMARK" : "MARK", ToggleMark, Panel, selected != null && !echoing);
            float nx = bx + 2 * (bs + gap);
            if (cfg.Noise) { Button(new Rect(nx, by, bs, bs), "HORN", UseNoise, Panel, !noiseUsed && !echoing); nx += bs + gap; }
            if (cfg.Ping) { Button(new Rect(nx, by, bs, bs), "PING", UsePing, Panel, !pingUsed && !echoing); nx += bs + gap; }
            if (echoOK) Button(new Rect(nx, by, bs, bs), "ECHO", StartEcho, echoing ? new Color(.1f, .26f, .42f, .92f) : Panel, !echoUsed && !echoing);
            if (echoing)
            {
                float ew = s.width - 20 * u, ex = s.x + 10 * u, ey = by - 54 * u, eh = 44 * u;
                Fill(new Rect(ex, ey, ew, eh), new Color(.055f, .15f, .26f, .9f));
                Frame(new Rect(ex, ey, ew, eh), new Color(.5f, .78f, 1f, .5f), Mathf.Max(1f, u));
                Text(new Rect(ex + 8 * u, ey, 60 * u, eh), "<b>ECHO</b>", small, new Color(.62f, .83f, 1f));
                float trackX = ex + 64 * u, trackW = ew - 64 * u - 52 * u - 56 * u - 112 * u;
                float prog = Mathf.Clamp01((echoHead - echoFrom) / Mathf.Max(.01f, echoTo - echoFrom));
                Fill(new Rect(trackX, ey + eh * .5f - 2 * u, trackW, 4 * u), new Color(1, 1, 1, .14f));
                Fill(new Rect(trackX, ey + eh * .5f - 2 * u, trackW * prog, 4 * u), new Color(.62f, .83f, 1f));
                Text(new Rect(trackX + trackW + 6 * u, ey, 50 * u, eh), "-" + (echoTo - echoHead).ToString("0.0") + "s", small, Ink);
                Button(new Rect(ex + ew - 164 * u, ey + 5 * u, 50 * u, eh - 10 * u), echoSpeed < 1f ? "1/2x" : "1x", () => echoSpeed = echoSpeed < 1f ? 1f : .5f, new Color(0, 0, 0, .35f));
                Button(new Rect(ex + ew - 108 * u, ey + 5 * u, 102 * u, eh - 10 * u), "LIVE", StopEcho, new Color(0, 0, 0, .35f));
            }
            float tagSize = 100 * u;
            tagRect = new Rect(s.xMax - tagSize - 12 * u, s.yMax - tagSize - 12 * u, tagSize, tagSize);
            var prev = GUI.color;
            GUI.color = selected == null || echoing ? new Color(.45f, .45f, .45f, .9f) : holding ? Gold : Red;
            GUI.DrawTexture(tagRect, disc);
            GUI.color = new Color(1, 1, 1, .9f);
            GUI.DrawTexture(tagRect, ring);
            GUI.color = prev;
            Text(tagRect, "TAG\n<size=" + (int)(11 * u) + ">hold</size>", button, Color.white);

            if (paused)
            {
                Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(.02f, .03f, .04f, .7f));
                float w = Mathf.Min(s.width * .8f, 340 * u), x = s.center.x - w * .5f, y = s.center.y - 110 * u;
                Text(new Rect(x, y, w, 50 * u), "Paused", new GUIStyle(title) { alignment = TextAnchor.MiddleCenter }, Ink);
                Button(new Rect(x, y + 60 * u, w, 52 * u), "Resume", () => paused = false, Red);
                Button(new Rect(x, y + 120 * u, w, 52 * u), "Restart", () => { if (daily) StartDaily(); else StartCase(caseIndex); }, Panel);
                Button(new Rect(x, y + 180 * u, w, 52 * u), "Quit case", () => { StopEcho(); paused = false; Time.timeScale = 1f; page = daily ? Page.Menu : Page.Cases; }, Panel);
            }
        }

        void DrawWorldOverlays()
        {
            foreach (var w in crowd.Walkers)
            {
                Rect r;
                if (!BodyRect(w, out r)) continue;
                float headY = r.y;
                if (w.Marked) Text(new Rect(r.center.x - 12 * u, headY - 30 * u, 24 * u, 24 * u), "<b>?</b>", new GUIStyle(button) { fontSize = (int)(18 * u) }, Gold);
                if (w.Wrong) Text(new Rect(r.center.x - 12 * u, headY - 30 * u, 24 * u, 24 * u), "<b>x</b>", new GUIStyle(button) { fontSize = (int)(18 * u) }, new Color(.8f, .82f, .82f));
                if (w.Flinching) Text(new Rect(r.center.x - 12 * u, headY - 28 * u, 24 * u, 24 * u), "<b>!</b>", new GUIStyle(button) { fontSize = (int)(20 * u) }, Ink);
                if (w.OnEar && Mathf.Sin(Time.unscaledTime * 25f) > -.3f) Text(new Rect(r.center.x - 30 * u, headY - 30 * u, 60 * u, 24 * u), "<b>((•))</b>", new GUIStyle(button) { fontSize = (int)(14 * u) }, Red);
                if (w == selected && (!over || page == Page.Play))
                {
                    Color c = holding && locked ? Gold : over ? Red : Ink;
                    float k = Mathf.Max(6f * u, r.height * .14f), lw = Mathf.Max(2f, 2f * u);
                    var b = new Rect(r.x - 4 * u, r.y - 4 * u, r.width + 8 * u, r.height + 8 * u);
                    Fill(new Rect(b.x, b.y, k, lw), c); Fill(new Rect(b.x, b.y, lw, k), c);
                    Fill(new Rect(b.xMax - k, b.y, k, lw), c); Fill(new Rect(b.xMax - lw, b.y, lw, k), c);
                    Fill(new Rect(b.x, b.yMax - lw, k, lw), c); Fill(new Rect(b.x, b.yMax - k, lw, k), c);
                    Fill(new Rect(b.xMax - k, b.yMax - lw, k, lw), c); Fill(new Rect(b.xMax - lw, b.yMax - k, lw, k), c);
                    if (holding) DrawRing(r);
                }
            }
            if (glintOn)
            {
                Vector3 g = cam.WorldToScreenPoint(glintPos);
                float pulse = .6f + .4f * Mathf.Sin(Time.unscaledTime * 20f), size = 70f * u;
                if (g.z > 0f && g.x > 0 && g.x < Screen.width && g.y > 0 && g.y < Screen.height)
                {
                    var prev = GUI.color; GUI.color = new Color(1f, .97f, .85f, pulse);
                    GUI.DrawTexture(new Rect(g.x - size * .5f, Screen.height - g.y - size * .5f, size, size), flare);
                    GUI.color = prev;
                }
                else Text(new Rect(Screen.width * .5f - 120 * u, Screen.height * .4f, 240 * u, 30 * u), "<b>Glint off-screen: look around!</b>", new GUIStyle(button) { fontSize = (int)(14 * u) }, Red);
            }
            if (shotT >= 0f && shotTarget != null && shotT < 1.2f)
            {
                Vector3 c = cam.WorldToScreenPoint(shotTarget.Chest);
                Vector2 from = new Vector2(Screen.width * .92f, Screen.height * 1.05f), to = new Vector2(c.x, Screen.height - c.y);
                float k = Mathf.Clamp01((shotT - .1f) / .9f);
                for (int i = 0; i < 10; i++)
                {
                    float kk = k - i * .03f;
                    if (kk < 0f) break;
                    Vector2 p = Vector2.Lerp(from, to, kk);
                    float size = (7f - i * .5f) * u;
                    var prev = GUI.color; GUI.color = new Color(1f, .9f, .66f, 1f - i * .09f);
                    GUI.DrawTexture(new Rect(p.x - size * .5f, p.y - size * .5f, size, size), disc);
                    GUI.color = prev;
                }
            }
            if (shotT >= 1.15f && shotTarget != null)
            {
                Rect r;
                if (BodyRect(shotTarget, out r))
                    Text(new Rect(r.center.x - 120 * u, r.y - 50 * u, 240 * u, 40 * u), shotWin ? "<b>COURIER TAGGED</b>" : "<b>CIVILIAN</b>",
                        new GUIStyle(button) { fontSize = (int)(26 * u) }, shotWin ? Gold : new Color(.84f, .87f, .87f));
            }
        }

        void DrawRing(Rect body)
        {
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(holdT / Steady), 3f);
            float over2 = Mathf.Max(0f, holdT - Steady - 1.2f);
            float sway = ((1f - k) * 14f + over2 * 9f) * u;
            float cxp = body.center.x + Mathf.Sin(Time.unscaledTime * 7.3f) * sway;
            float cyp = body.y + body.height * .45f + Mathf.Cos(Time.unscaledTime * 5.1f) * sway;
            float radius = Mathf.Lerp(Mathf.Max(56f * u, body.height * 1.05f), Mathf.Max(10f * u, body.height * .2f), k);
            var prev = GUI.color;
            GUI.color = locked ? Gold : new Color(1, 1, 1, .85f);
            GUI.DrawTexture(new Rect(cxp - radius, cyp - radius, radius * 2f, radius * 2f), ring);
            GUI.color = prev;
            if (locked) Text(new Rect(cxp - 60 * u, cyp + radius + 6 * u, 120 * u, 22 * u), "<b>RELEASE</b>", new GUIStyle(button) { fontSize = (int)(14 * u) }, Gold);
        }

        void DrawResult(Rect s)
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(.02f, .03f, .04f, .78f));
            float w = Mathf.Min(s.width * .92f, 440 * u), x = s.center.x - w * .5f, y = s.y + s.height * .16f;
            Fill(new Rect(x, y, w, s.height * .7f), new Color(.07f, .1f, .11f, .97f));
            var center = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            Text(new Rect(x, y + 14 * u, w, 20 * u), run.Cleared ? "CASE CLOSED" : "CASE LOST", new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold }, Red);
            Text(new Rect(x, y + 36 * u, w, 44 * u), resultTitle, new GUIStyle(title) { alignment = TextAnchor.MiddleCenter }, Ink);
            Text(new Rect(x, y + 82 * u, w, 40 * u), new string('★', result.Stars) + "<color=#3a4143>" + new string('★', 3 - result.Stars) + "</color>", new GUIStyle(big) { fontSize = (int)(34 * u), richText = true }, Gold);
            Text(new Rect(x + 16 * u, y + 126 * u, w - 32 * u, 70 * u), resultLine, center, Ink);
            float ry = y + 200 * u;
            if (run.Cleared)
            {
                var rows = new List<KeyValuePair<string, int>>
                {
                    new KeyValuePair<string, int>("Case bounty", result.Base),
                    new KeyValuePair<string, int>("Early tag bonus (" + (run.Total - run.Revealed) + " lines unread)", result.Early),
                    new KeyValuePair<string, int>("Time left", result.Time)
                };
                if (result.Provoke > 0) rows.Add(new KeyValuePair<string, int>("Provocations", -result.Provoke));
                if (result.Intel > 0) rows.Add(new KeyValuePair<string, int>("Intel bought", -result.Intel));
                if (result.Penalty > 0) rows.Add(new KeyValuePair<string, int>("Wrong tag", -result.Penalty));
                var right = new GUIStyle(label) { alignment = TextAnchor.MiddleRight };
                foreach (var row in rows)
                {
                    Text(new Rect(x + 18 * u, ry, w - 36 * u, 24 * u), row.Key, small, Ink);
                    Text(new Rect(x + 18 * u, ry, w - 36 * u, 24 * u), (row.Value > 0 ? "+" : "") + row.Value, right, Ink);
                    ry += 26 * u;
                }
                Text(new Rect(x + 18 * u, ry + 4 * u, w - 36 * u, 30 * u), "Score", new GUIStyle(label) { fontStyle = FontStyle.Bold }, Gold);
                Text(new Rect(x + 18 * u, ry + 4 * u, w - 36 * u, 30 * u), result.Score.ToString(), new GUIStyle(right) { fontStyle = FontStyle.Bold }, Gold);
                ry += 40 * u;
            }
            if (share.Length > 0) { Text(new Rect(x + 18 * u, ry, w - 36 * u, 90 * u), share, small, new Color(.85f, .85f, .8f)); ry += 92 * u; }
            float bw = (w - 40 * u) / 3f, by = Mathf.Max(ry + 6 * u, y + s.height * .7f - 70 * u);
            if (share.Length > 0) Button(new Rect(x + 10 * u, by, bw, 56 * u), "Share", () => Share(share), Red);
            else if (run.Cleared && caseIndex < CaseEngine.Cases.Length - 1)
                Button(new Rect(x + 10 * u, by, bw, 56 * u), "Next case", () => { caseIndex++; cfg = CaseEngine.Cases[caseIndex]; page = Page.Brief; }, Red);
            else Button(new Rect(x + 10 * u, by, bw, 56 * u), "Try again", () => StartCase(caseIndex), Red);
            Button(new Rect(x + 20 * u + bw, by, bw, 56 * u), daily ? "Practise" : "Replay", () => { if (daily) StartDaily(); else StartCase(caseIndex); }, Panel);
            Button(new Rect(x + 30 * u + 2 * bw, by, bw, 56 * u), "Menu", () => { page = daily ? Page.Menu : Page.Cases; }, Panel);
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            foreach (var tex in solids.Values) if (tex) Destroy(tex);
            foreach (var tex in new[] { ring, disc, vignette, flare }) if (tex) Destroy(tex);
        }
    }
}
