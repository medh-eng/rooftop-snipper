using System.Collections.Generic;
using HarborWatch.Core;
using UnityEngine;

namespace HarborWatch
{
    /// <summary>
    /// Find the One crowd. Every pedestrian is a persistent HarborWatch.Core.Person
    /// with a visible look (coat colour, headwear, bag, headphones) and observable
    /// behaviour (pace, habit, reaction to noise, reaction to the cell ping).
    /// Walkers reuse the tested HarborCrowdTraffic routes and the original
    /// planted-foot walk and jog clips; at a route's end they continue on a route
    /// that starts from the same side of the concourse, so identities never reset.
    /// </summary>
    public sealed class CaseCrowd : MonoBehaviour
    {
        const float WalkMetresPerSecond = 1.05f;
        const float JogMetresPerSecond = 2.05f;
        static readonly float[] HabitSeconds = { 0f, 1.9f, 1.0f, 2.4f };

        // Outfit index -> jacket colour. Same index meaning as the browser build.
        public static readonly Color[] OutfitColours =
        {
            new Color(.74f, .58f, .24f),  // 0 mustard
            new Color(.24f, .52f, .40f),  // 1 green
            new Color(.86f, .82f, .70f),  // 2 pale beige
            new Color(.20f, .21f, .23f)   // 3 charcoal
        };
        public static readonly Color[] HeadwearColours =
        {
            Color.clear, new Color(.85f, .2f, .16f), new Color(.18f, .42f, .84f),
            new Color(.95f, .76f, .18f), new Color(.95f, .94f, .9f), new Color(.1f, .1f, .12f)
        };
        static readonly Color[] DenimPalette =
        {
            new Color(.19f, .29f, .40f), new Color(.27f, .34f, .32f),
            new Color(.32f, .29f, .25f), new Color(.13f, .23f, .28f)
        };
        static readonly Color[] SkinPalette =
        {
            new Color(.72f, .49f, .35f), new Color(.61f, .39f, .26f),
            new Color(.82f, .58f, .43f), new Color(.51f, .33f, .25f)
        };

        public sealed class Walker
        {
            public Person Person;
            public GameObject Entity;
            public Transform Anchor, Hips, Head;
            public GameObject Cap, CapPeak, Backpack, Briefcase, Headphones, Phone;
            public Material PhoneMaterial;
            public readonly Dictionary<string, Material> Clothing = new Dictionary<string, Material>();
            internal HarborCrowdTraffic.Path Route;
            internal int RouteIndex;
            internal float Distance, AnimTime, CurrentSpeed;
            public bool Moving, Flinching, OnEar, Wrong, Marked;
            public Vector3 Chest { get { return Hips.position + Vector3.up * .38f; } }
            public Vector3 HeadTop { get { return Head.position + Vector3.up * .3f; } }
            public Vector3 Feet { get { return Anchor.position; } }
        }

        public readonly List<Walker> Walkers = new List<Walker>();
        public Walker Courier { get; private set; }
        public bool CourierAlert;
        /// <summary>World point people turn towards when the noise provocation fires.</summary>
        public Vector3 NoiseSource = new Vector3(5.5f, 3f, -10.5f);

        readonly List<Material> ownedMaterials = new List<Material>();
        readonly List<HarborCrowdTraffic.Path> routes = new List<HarborCrowdTraffic.Path>();
        readonly List<char> routeStartSide = new List<char>();
        readonly List<char> routeEndSide = new List<char>();
        float[] planned = new float[0];
        System.Random random;
        AnimationClip walk, jog;
        GameObject prefab;
        Material source;
        Transform root;
        float clock, noiseAt = -99f, pingAt = -99f;

        static AnimationClip FindClip(string resource)
        {
            foreach (var clip in Resources.LoadAll<AnimationClip>(resource))
                if (!clip.name.StartsWith("__") && clip.length > 0) return clip;
            return null;
        }

        static char Side(Vector3 p)
        {
            if (p.x < -10f) return 'W';
            if (p.x > 10f) return 'E';
            if (p.z > 7f) return 'T';
            return 'F';
        }

        public bool Build(Transform concourse, Material sourceMaterial, Case kase)
        {
            Clear();
            root = concourse;
            source = sourceMaterial;
            prefab = Resources.Load<GameObject>("CrowdWalk");
            walk = FindClip("CrowdWalk");
            jog = FindClip("CrowdJog");
            if (!prefab || !walk || !jog || !source)
            {
                Debug.LogError("Find the One needs CrowdWalk.fbx, CrowdJog.fbx and ReviewSurface.mat.");
                return false;
            }
            if (routes.Count == 0)
            {
                foreach (var track in HarborCrowdTraffic.Waypoints)
                {
                    routes.Add(new HarborCrowdTraffic.Path(track));
                    routeStartSide.Add(Side(track[0]));
                    routeEndSide.Add(Side(track[track.Length - 1]));
                }
            }
            // Presentation randomness only (skin tone, denim, route choice);
            // everything the player must deduce comes from the Core case.
            random = new System.Random((int)(kase.Seed & 0x7fffffff));
            clock = 0f; noiseAt = pingAt = -99f; CourierAlert = false;

            foreach (var person in kase.People)
            {
                var w = CreateWalker(person);
                if (w == null) return false;
                Walkers.Add(w);
            }
            Courier = Walkers[kase.Target];
            planned = new float[Walkers.Count];
            // Spread people along routes; X (0..1600) gives how far along they start.
            for (int i = 0; i < Walkers.Count; i++)
            {
                var w = Walkers[i];
                w.RouteIndex = w.Person.Id % routes.Count;
                w.Route = routes[w.RouteIndex];
                float along = Mathf.Clamp01((float)((w.Person.X - 90.0) / 1420.0));
                w.Distance = w.Route.Length * (.08f + .84f * along);
                w.AnimTime = (float)(w.Person.Offset % walk.length);
                w.CurrentSpeed = NominalSpeed(w);
                Place(w, 0f);
            }
            return true;
        }

        Walker CreateWalker(Person person)
        {
            var w = new Walker { Person = person };
            w.Anchor = new GameObject("Case person " + person.Id.ToString("00")).transform;
            w.Anchor.SetParent(root, false);
            Transform model = new GameObject("Facing").transform;
            model.SetParent(w.Anchor, false);
            w.Entity = Instantiate(prefab, model);
            foreach (var animation in w.Entity.GetComponentsInChildren<Animation>(true)) animation.enabled = false;
            Transform chest = null, leftFoot = null, rightForearm = null;
            foreach (var bone in w.Entity.GetComponentsInChildren<Transform>(true))
            {
                if (bone.name == "Hips") w.Hips = bone;
                if (bone.name == "Head") w.Head = bone;
                if (bone.name == "Chest") chest = bone;
                if (bone.name == "Foot.L") leftFoot = bone;
                if (bone.name == "Forearm.R") rightForearm = bone;
            }
            if (!w.Hips || !w.Head || !chest || !leftFoot)
            {
                Debug.LogError("CrowdWalk FBX rig is missing a required bone.");
                return null;
            }
            // Same facing correction as HarborCrowdTraffic: the planted left
            // foot's backwards motion gives the mesh's travel axis.
            walk.SampleAnimation(w.Entity, 0f);
            Vector3 contact = leftFoot.position;
            walk.SampleAnimation(w.Entity, walk.length * .25f);
            Vector3 forward = Vector3.ProjectOnPlane(contact - leftFoot.position, w.Anchor.up);
            if (forward.sqrMagnitude > .0001f)
                model.rotation = Quaternion.AngleAxis(
                    Vector3.SignedAngle(forward, w.Anchor.forward, w.Anchor.up), w.Anchor.up) * model.rotation;
            walk.SampleAnimation(w.Entity, 0f);

            Paint(w);
            BuildAccessories(w, chest, rightForearm);
            return w;
        }

        Material NewMaterial(Color colour)
        {
            var mat = new Material(source);
            ownedMaterials.Add(mat);
            Tint(mat, colour);
            return mat;
        }

        static void Tint(Material mat, Color colour)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", colour);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", colour);
        }

        void Paint(Walker w)
        {
            Color coat = OutfitColours[w.Person.Outfit];
            Color denim = DenimPalette[random.Next(DenimPalette.Length)];
            Color skin = SkinPalette[random.Next(SkinPalette.Length)];
            foreach (var renderer in w.Entity.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (!slots[i]) continue;
                    string key = slots[i].name.Replace(" (Instance)", "");
                    Material material;
                    if (!w.Clothing.TryGetValue(key, out material))
                    {
                        Color c;
                        switch (key)
                        {
                            case "Jacket": c = coat; break;
                            case "Hem": c = coat * .76f; break;
                            case "Denim": c = denim; break;
                            case "Cuff": c = denim * .88f; break;
                            case "Skin": c = skin; break;
                            case "Hair": c = new Color(.17f, .12f, .11f); break;
                            case "Shirt": c = new Color(.82f, .82f, .73f); break;
                            case "Leather": c = new Color(.28f, .22f, .17f); break;
                            case "Sole": c = new Color(.66f, .64f, .58f); break;
                            case "Metal": c = new Color(.75f, .61f, .41f); break;
                            default: c = new Color(.64f, .62f, .58f); break;
                        }
                        material = NewMaterial(c);
                        w.Clothing.Add(key, material);
                    }
                    slots[i] = material;
                }
                renderer.sharedMaterials = slots;
                renderer.updateWhenOffscreen = false;
            }
        }

        GameObject Pivot(string name, Transform bone, Transform frame, Vector3 offsetMetres)
        {
            var pivot = new GameObject(name);
            pivot.transform.SetPositionAndRotation(bone.position + frame.rotation * offsetMetres, frame.rotation);
            pivot.transform.SetParent(bone, true);
            return pivot;
        }

        static GameObject Piece(GameObject pivot, string name, PrimitiveType kind, Vector3 offset,
            Vector3 size, Material material, Vector3 euler)
        {
            var piece = GameObject.CreatePrimitive(kind);
            piece.name = name;
            piece.transform.SetParent(pivot.transform, false);
            piece.transform.localPosition = offset;
            piece.transform.localRotation = Quaternion.Euler(euler);
            piece.transform.localScale = size;
            piece.GetComponent<Renderer>().sharedMaterial = material;
            var collider = piece.GetComponent<Collider>();
            if (collider) Destroy(collider);
            return piece;
        }

        void BuildAccessories(Walker w, Transform chest, Transform rightForearm)
        {
            var p = w.Person;
            // Headwear: cap (crown + peak) or beanie (taller crown, no peak).
            if (p.Headwear > 0)
            {
                var hatMat = NewMaterial(HeadwearColours[p.Headwear]);
                bool beanie = p.Headwear >= 4;
                w.Cap = Pivot("Headwear", w.Head, w.Anchor, new Vector3(0, beanie ? .225f : .213f, -.017f));
                Piece(w.Cap, "Crown", PrimitiveType.Sphere, Vector3.zero,
                    beanie ? new Vector3(.212f, .2f, .222f) : new Vector3(.216f, .153f, .226f), hatMat, Vector3.zero);
                if (!beanie)
                    w.CapPeak = Piece(w.Cap, "Peak", PrimitiveType.Sphere, new Vector3(0, -.01f, .094f),
                        new Vector3(.184f, .019f, .155f), hatMat, new Vector3(6, 0, 0));
                else
                    Piece(w.Cap, "Fold", PrimitiveType.Cylinder, new Vector3(0, -.055f, 0),
                        new Vector3(.225f, .022f, .235f), NewMaterial(HeadwearColours[p.Headwear] * .8f), Vector3.zero);
            }
            // Backpack on the chest bone (same fit as the ambient crowd).
            if (p.Carry == 1)
            {
                var bagMat = NewMaterial(new Color(.24f, .3f, .24f));
                w.Backpack = Pivot("Backpack", chest, w.Anchor, new Vector3(0, .213f, -.187f));
                Piece(w.Backpack, "Pack", PrimitiveType.Sphere, Vector3.zero, new Vector3(.285f, .356f, .169f), bagMat, Vector3.zero);
                Piece(w.Backpack, "Pocket", PrimitiveType.Sphere, new Vector3(0, -.056f, -.064f), new Vector3(.224f, .175f, .086f), bagMat, Vector3.zero);
            }
            // Briefcase held in the right hand (end of Forearm.R).
            if (p.Carry == 2 && rightForearm)
            {
                var caseMat = NewMaterial(new Color(.36f, .23f, .13f));
                Vector3 hand = rightForearm.position + rightForearm.up * .225f;
                var pivot = new GameObject("Briefcase");
                pivot.transform.SetPositionAndRotation(hand, w.Anchor.rotation);
                pivot.transform.SetParent(rightForearm, true);
                Piece(pivot, "Case", PrimitiveType.Cube, new Vector3(.02f, -.15f, 0), new Vector3(.07f, .24f, .34f), caseMat, Vector3.zero);
                Piece(pivot, "Handle", PrimitiveType.Cube, new Vector3(.02f, -.02f, 0), new Vector3(.025f, .03f, .1f), caseMat, Vector3.zero);
                w.Briefcase = pivot;
            }
            // Headphones: bright band and ear cups, visible through the lens.
            if (p.Headphones == 1)
            {
                var phonesMat = NewMaterial(new Color(.92f, .97f, .95f));
                w.Headphones = Pivot("Headphones", w.Head, w.Anchor, new Vector3(0, .13f, -.01f));
                for (int side = -1; side <= 1; side += 2)
                    Piece(w.Headphones, "Ear cup", PrimitiveType.Sphere, new Vector3(side * .1f, 0, 0), new Vector3(.05f, .085f, .085f), phonesMat, Vector3.zero);
                Piece(w.Headphones, "Band", PrimitiveType.Cube, new Vector3(0, .155f, 0), new Vector3(.2f, .025f, .04f), phonesMat, Vector3.zero);
                for (int side = -1; side <= 1; side += 2)
                    Piece(w.Headphones, "Band side", PrimitiveType.Cube, new Vector3(side * .1f, .08f, 0), new Vector3(.022f, .16f, .04f), phonesMat, Vector3.zero);
            }
            // Phone: shown for the phone habit (bright screen) and the ping (red, at the ear).
            w.PhoneMaterial = NewMaterial(new Color(.8f, .93f, 1f));
            w.Phone = Pivot("Phone", w.Head, w.Anchor, new Vector3(0, .1f, .16f));
            Piece(w.Phone, "Screen", PrimitiveType.Cube, Vector3.zero, new Vector3(.045f, .085f, .012f), w.PhoneMaterial, new Vector3(-20, 0, 0));
            w.Phone.SetActive(false);
        }

        float NominalSpeed(Walker w)
        {
            float jitter = (float)w.Person.Jitter;
            if (w == Courier && CourierAlert) return JogMetresPerSecond * 1.1f;
            switch (w.Person.Pace)
            {
                case 0: return WalkMetresPerSecond * .68f * jitter;
                case 2: return JogMetresPerSecond * jitter;
                default: return WalkMetresPerSecond * jitter;
            }
        }

        bool Jogging(Walker w) { return w.Person.Pace == 2 || (w == Courier && CourierAlert); }

        public void Noise() { noiseAt = clock; }
        public void Ping() { pingAt = clock; }
        public bool NoiseWindow { get { return clock - noiseAt > .15f && clock - noiseAt < 2.3f; } }
        public bool PingWindow { get { return clock - pingAt > .25f && clock - pingAt < 2.0f; } }

        public void Step(float seconds)
        {
            float dt = Mathf.Min(seconds, .06f);
            clock += dt;
            bool noise = NoiseWindow, ping = PingWindow;

            // Plan speeds from one snapshot (spacing uses the ambient crowd's rules).
            for (int i = 0; i < Walkers.Count; i++)
            {
                var a = Walkers[i];
                float nominal = NominalSpeed(a);
                float factor = 1f;
                Vector3 forward = a.Route.Forward(a.Distance);
                for (int j = 0; j < Walkers.Count; j++)
                {
                    if (i == j) continue;
                    var b = Walkers[j];
                    factor = Mathf.Min(factor, HarborCrowdSpacing.YieldFactor(
                        a.Anchor.position, forward, nominal, i,
                        b.Anchor.position, b.Route.Forward(b.Distance), b.CurrentSpeed, j));
                }
                planned[i] = nominal * factor;
            }

            for (int i = 0; i < Walkers.Count; i++)
            {
                var w = Walkers[i];
                var p = w.Person;
                float target = planned[i];
                float phase = (float)((clock + p.Offset) % p.Period);
                bool habitOn = p.Habit > 0 && phase < HabitSeconds[p.Habit];
                bool phone = false, glance = false;
                if (habitOn)
                {
                    if (p.Habit == 1) { phone = true; target *= .55f; }
                    if (p.Habit == 2) glance = true;
                    if (p.Habit == 3) target = 0f;
                }
                w.Flinching = noise && !p.Calm;
                if (w.Flinching) target = 0f;
                w.OnEar = ping && p.Cell;
                if (w.OnEar) target = 0f;
                if (w.Wrong) target *= .3f;

                float responsiveness = target < w.CurrentSpeed ? 4f : 1.6f;
                w.CurrentSpeed = Mathf.MoveTowards(w.CurrentSpeed, target, responsiveness * dt);
                Place(w, w.CurrentSpeed * dt);

                // Pose: gait clip while moving, planted contact frame when stopped.
                w.Moving = w.CurrentSpeed > .05f;
                AnimationClip motion = Jogging(w) ? jog : walk;
                float baseSpeed = Jogging(w) ? JogMetresPerSecond : WalkMetresPerSecond;
                if (w.Moving)
                {
                    w.AnimTime = Mathf.Repeat(w.AnimTime + dt * w.CurrentSpeed / baseSpeed, motion.length);
                    motion.SampleAnimation(w.Entity, w.AnimTime);
                }
                else walk.SampleAnimation(w.Entity, 0f);

                // Head overrides are applied after sampling, so they never accumulate.
                if (w.Flinching)
                {
                    Vector3 look = NoiseSource - w.Head.position; look.y = 0f;
                    float yaw = Mathf.Clamp(Vector3.SignedAngle(w.Anchor.forward, look, Vector3.up), -75f, 75f);
                    w.Head.localRotation *= Quaternion.Euler(-12f, yaw, 0f);
                }
                else if (glance) w.Head.localRotation *= Quaternion.Euler(0f, 80f * Mathf.Sin(phase / HabitSeconds[2] * Mathf.PI), 0f);
                else if (phone) w.Head.localRotation *= Quaternion.Euler(22f, 0f, 0f);
                if (w.OnEar) w.Head.localRotation *= Quaternion.Euler(0f, 0f, 14f);

                w.Phone.SetActive(phone || w.OnEar);
                if (w.Phone.activeSelf)
                {
                    w.Phone.transform.localPosition = w.OnEar ? new Vector3(-.11f, .12f, .0f) : new Vector3(0f, .08f, .17f);
                    bool blink = Mathf.Sin(clock * 30f) > 0f;
                    Tint(w.PhoneMaterial, w.OnEar ? (blink ? new Color(1f, .2f, .12f) : new Color(.45f, .08f, .05f)) : new Color(.8f, .93f, 1f));
                }
            }
        }

        void Place(Walker w, float advance)
        {
            w.Distance += advance;
            if (w.Distance >= w.Route.Length)
            {
                // Continue from the same side of the concourse; identity persists.
                char side = routeEndSide[w.RouteIndex];
                var options = new List<int>();
                for (int r = 0; r < routes.Count; r++) if (routeStartSide[r] == side && r != w.RouteIndex) options.Add(r);
                if (options.Count == 0) for (int r = 0; r < routes.Count; r++) if (routeStartSide[r] == side) options.Add(r);
                w.RouteIndex = options.Count > 0 ? options[random.Next(options.Count)] : (w.RouteIndex + 1) % routes.Count;
                w.Route = routes[w.RouteIndex];
                w.Distance = 0f;
            }
            w.Anchor.position = w.Route.At(w.Distance);
            Quaternion desired = Quaternion.LookRotation(w.Route.Forward(w.Distance), Vector3.up);
            w.Anchor.rotation = advance <= 0f ? desired : Quaternion.RotateTowards(w.Anchor.rotation, desired, 170f * Time.deltaTime);
        }

        public void Clear()
        {
            foreach (var w in Walkers) if (w.Anchor) Destroy(w.Anchor.gameObject);
            Walkers.Clear();
            foreach (var m in ownedMaterials) if (m) Destroy(m);
            ownedMaterials.Clear();
            Courier = null;
        }

        void OnDestroy() { Clear(); }
    }
}
