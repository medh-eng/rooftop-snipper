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

        /// <summary>One arm of the rig: reach target and the wrist (the rig has no hand bone).</summary>
        public sealed class Arm
        {
            public Transform Upper, Fore;
            public Vector3 ForeAxis;      // forearm-local axis that points at the hand
            public float ForeLength;      // elbow to the middle of the grip
            public float Side;            // +1 = along Anchor.right, -1 = along -Anchor.right
            public float Weight;          // 0 = clip pose, 1 = fully reaching
        }

        public sealed class Walker
        {
            public Person Person;
            public GameObject Entity;
            public Transform Anchor, Hips, Head;
            public GameObject Cap, CapPeak, Backpack, Briefcase, Headphones, Phone;
            public Material PhoneMaterial, BeaconMaterial;
            public Arm RightArm, LeftArm;
            public readonly Dictionary<string, Material> Clothing = new Dictionary<string, Material>();
            internal HarborCrowdTraffic.Path Route;
            internal int RouteIndex;
            internal float Distance, Phase01, CurrentSpeed, Yaw, YawVelocity, AwayLeft;
            internal float HeadYaw, HeadPitch, HeadRoll;
            internal bool Settled;
            // What the last Step fed into Pose(); the Echo lens records them to replay the same pose.
            internal bool LastPhone, LastGlance;
            internal float LastHabitPhase;
            public bool Moving, Flinching, OnEar, Wrong, Marked;
            /// <summary>False while the person has walked off the set and is about to re-enter.</summary>
            public bool Present = true;
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

        static bool IsFerry(Vector3 p) { return Mathf.Abs(p.x - 4.4f) < .05f && Mathf.Abs(p.z + 9f) < .05f; }

        /// <summary>
        /// The tested routes, with one extra waypoint on the gangway centre line for the
        /// ferry routes so nobody brushes the pier railing (tools/route-check.cjs checks
        /// every route against every solid object of the concourse).
        /// </summary>
        static Vector3[] GatedControls(Vector3[] track)
        {
            var list = new List<Vector3>(track);
            var gate = new Vector3(4.4f, 0f, -7.0f);
            if (IsFerry(list[0])) list.Insert(1, gate);
            if (IsFerry(list[list.Count - 1])) list.Insert(list.Count - 1, gate);
            return list.ToArray();
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
                    var gated = GatedControls(track);
                    routes.Add(new HarborCrowdTraffic.Path(gated));
                    routeStartSide.Add(Side(gated[0]));
                    routeEndSide.Add(Side(gated[gated.Length - 1]));
                }
            }
            // Presentation randomness only (skin tone, denim, route choice);
            // everything the player must deduce comes from the Core case.
            random = new System.Random((int)(kase.Seed & 0x7fffffff));
            clock = 0f; noiseAt = pingAt = -99f; CourierAlert = false;
            log.Clear(); nextRecord = 0f; liveBackup = null; replaying = false;

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
                w.Phase01 = Mathf.Repeat((float)(w.Person.Offset % walk.length) / walk.length, 1f);
                w.CurrentSpeed = NominalSpeed(w);
                w.Yaw = HeadingOf(w);
                w.Anchor.position = w.Route.At(w.Distance);
                w.Anchor.rotation = Quaternion.Euler(0f, w.Yaw, 0f);
                Pose(w, 0f, false, false, false, 0f);
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
            Transform chest = null, leftFoot = null;
            Transform upperR = null, upperL = null, foreR = null, foreL = null;
            foreach (var bone in w.Entity.GetComponentsInChildren<Transform>(true))
            {
                if (bone.name == "Hips") w.Hips = bone;
                if (bone.name == "Head") w.Head = bone;
                if (bone.name == "Chest") chest = bone;
                if (bone.name == "Foot.L") leftFoot = bone;
                if (bone.name == "UpperArm.R") upperR = bone;
                if (bone.name == "UpperArm.L") upperL = bone;
                if (bone.name == "Forearm.R") foreR = bone;
                if (bone.name == "Forearm.L") foreL = bone;
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

            w.RightArm = MakeArm(w, upperR, foreR);
            w.LeftArm = MakeArm(w, upperL, foreL);
            Paint(w);
            BuildAccessories(w, chest);
            return w;
        }

        // The rig has no hand bone, so the forearm's long axis is found from the clip pose
        // (the local axis best aligned with shoulder -> elbow) and the hand sits one grip-length
        // along it. Everything is measured in world space, so it is independent of bone roll.
        static Arm MakeArm(Walker w, Transform upper, Transform fore)
        {
            if (!upper || !fore) return null;
            Vector3 dir = (fore.position - upper.position).normalized;
            Vector3[] axes = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            Vector3 best = Vector3.up; float bestDot = -2f;
            foreach (var axis in axes)
            {
                float d = Vector3.Dot(fore.TransformDirection(axis), dir);
                if (d > bestDot) { bestDot = d; best = axis; }
            }
            float upperLength = (fore.position - upper.position).magnitude;
            return new Arm
            {
                Upper = upper, Fore = fore, ForeAxis = best,
                ForeLength = Mathf.Clamp(upperLength * .95f, .2f, .3f),
                Side = Mathf.Sign(Vector3.Dot(fore.position - w.Hips.position, w.Anchor.right) + .0001f)
            };
        }

        static Vector3 Wrist(Arm a) { return a.Fore.position + a.Fore.TransformDirection(a.ForeAxis) * a.ForeLength; }

        /// <summary>
        /// Two-bone reach by world-space swing rotations, blended in position space so the
        /// arm lifts and drops smoothly. Returns the wrist afterwards.
        /// </summary>
        static Vector3 Reach(Arm a, Vector3 target, Vector3 pole, float weight)
        {
            if (a == null) return Vector3.zero;
            if (weight <= .001f) return Wrist(a);
            Vector3 shoulder = a.Upper.position, elbow = a.Fore.position;
            float l1 = (elbow - shoulder).magnitude, l2 = a.ForeLength;
            Vector3 aim = Vector3.Lerp(Wrist(a), target, Mathf.Clamp01(weight)) - shoulder;
            float d = Mathf.Clamp(aim.magnitude, Mathf.Abs(l1 - l2) + .01f, l1 + l2 - .005f);
            Vector3 dir = aim.normalized;
            float along = (l1 * l1 - l2 * l2 + d * d) / (2f * d);
            float bend = Mathf.Sqrt(Mathf.Max(0f, l1 * l1 - along * along));
            Vector3 sideways = Vector3.ProjectOnPlane(pole, dir);
            sideways = sideways.sqrMagnitude < 1e-4f ? Vector3.Cross(dir, Vector3.up).normalized : sideways.normalized;
            Vector3 newElbow = shoulder + dir * along + sideways * bend;
            a.Upper.rotation = Quaternion.FromToRotation(elbow - shoulder, newElbow - shoulder) * a.Upper.rotation;
            Vector3 hand = shoulder + dir * d;
            a.Fore.rotation = Quaternion.FromToRotation(
                a.Fore.TransformDirection(a.ForeAxis), (hand - a.Fore.position).normalized) * a.Fore.rotation;
            return Wrist(a);
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

        void BuildAccessories(Walker w, Transform chest)
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
            // Briefcase: hangs from the grip in the right hand. It is not parented to the arm bone
            // (that made it swing like a rigid extension of the forearm); Pose() hangs it from the
            // wrist, keeps it upright and swings it only a few degrees like a pendulum.
            // Origin = the grip. Real size: 42 x 30 x 8.5 cm.
            if (p.Carry == 2 && w.RightArm != null)
            {
                var caseMat = NewMaterial(new Color(.36f, .23f, .13f));
                var trimMat = NewMaterial(new Color(.22f, .14f, .08f));
                var metalMat = NewMaterial(new Color(.75f, .61f, .41f));
                var pivot = new GameObject("Briefcase");
                pivot.transform.SetParent(w.Anchor, false);
                Piece(pivot, "Case", PrimitiveType.Cube, new Vector3(0, -.205f, 0), new Vector3(.085f, .30f, .42f), caseMat, Vector3.zero);
                Piece(pivot, "Seam", PrimitiveType.Cube, new Vector3(0, -.115f, 0), new Vector3(.0875f, .012f, .425f), trimMat, Vector3.zero);
                Piece(pivot, "Lock", PrimitiveType.Cube, new Vector3(0, -.1f, 0), new Vector3(.089f, .028f, .045f), metalMat, Vector3.zero);
                Piece(pivot, "Handle bar", PrimitiveType.Cube, new Vector3(0, -.032f, 0), new Vector3(.02f, .014f, .13f), trimMat, Vector3.zero);
                Piece(pivot, "Handle post A", PrimitiveType.Cube, new Vector3(0, -.044f, .065f), new Vector3(.02f, .026f, .014f), trimMat, Vector3.zero);
                Piece(pivot, "Handle post B", PrimitiveType.Cube, new Vector3(0, -.044f, -.065f), new Vector3(.02f, .026f, .014f), trimMat, Vector3.zero);
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
            // Phone, real size 7 x 14.5 x 0.9 cm. It lives in the hand (Pose() places it at the wrist):
            // screen towards the face when reading, held flat against the ear for the ping, where the
            // back beacon blinks red so the call is visible from the roof.
            w.PhoneMaterial = NewMaterial(new Color(.8f, .93f, 1f));
            w.BeaconMaterial = NewMaterial(new Color(.3f, .05f, .04f));
            w.Phone = new GameObject("Phone");
            w.Phone.transform.SetParent(w.Anchor, false);
            Piece(w.Phone, "Body", PrimitiveType.Cube, Vector3.zero, new Vector3(.07f, .145f, .009f), NewMaterial(new Color(.07f, .08f, .09f)), Vector3.zero);
            Piece(w.Phone, "Screen", PrimitiveType.Cube, new Vector3(0, 0, .0052f), new Vector3(.062f, .13f, .002f), w.PhoneMaterial, Vector3.zero);
            Piece(w.Phone, "Beacon", PrimitiveType.Sphere, new Vector3(0, .04f, -.007f), new Vector3(.034f, .034f, .014f), w.BeaconMaterial, Vector3.zero);
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
            if (dt <= 0f) return;
            clock += dt;
            bool noise = NoiseWindow, ping = PingWindow;

            // Plan speeds from one snapshot (spacing uses the ambient crowd's rules).
            for (int i = 0; i < Walkers.Count; i++)
            {
                var a = Walkers[i];
                if (!a.Present) { planned[i] = 0f; continue; }
                float nominal = NominalSpeed(a);
                float factor = 1f;
                Vector3 forward = a.Route.Forward(a.Distance);
                for (int j = 0; j < Walkers.Count; j++)
                {
                    if (i == j || !Walkers[j].Present) continue;
                    var b = Walkers[j];
                    factor = Mathf.Min(factor, HarborCrowdSpacing.YieldFactor(
                        a.Anchor.position, forward, nominal, i,
                        b.Anchor.position, b.Route.Forward(b.Distance), b.CurrentSpeed, j));
                }
                // Low-pass the crowd-spacing answer so a neighbour drifting in and out of range
                // does not make someone stutter between slowing and speeding up.
                planned[i] = Mathf.Lerp(planned[i], nominal * factor, 1f - Mathf.Exp(-4f * dt));
            }

            for (int i = 0; i < Walkers.Count; i++)
            {
                var w = Walkers[i];
                var p = w.Person;
                if (!w.Present)
                {
                    w.AwayLeft -= dt;
                    if (w.AwayLeft <= 0f) Enter(w, false);
                    continue;
                }
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

                // Gentle ramps: 1.1 m/s^2 to speed up, 2.2 to slow down, 3.2 when startled.
                float responsiveness = target < w.CurrentSpeed ? (w.Flinching || w.OnEar ? 3.2f : 2.2f) : 1.1f;
                w.CurrentSpeed = Mathf.MoveTowards(w.CurrentSpeed, target, responsiveness * dt);
                bool wantStop = target < .05f;
                Move(w, w.Settled ? 0f : w.CurrentSpeed * dt, dt);
                if (!w.Present) continue;   // walked off the set this frame
                w.LastPhone = phone; w.LastGlance = glance; w.LastHabitPhase = phase;
                Pose(w, dt, phone, glance, wantStop, phase);
            }
            if (RecordEcho && clock >= nextRecord) Record();
        }

        // Heading follows the path a little ahead of the feet, so corners are taken as one
        // curve instead of snapping to the tangent of the nearest sample.
        float HeadingOf(Walker w)
        {
            Vector3 f = w.Route.At(w.Distance + .9f) - w.Route.At(w.Distance + .05f);
            f.y = 0f;
            if (f.sqrMagnitude < 1e-4f) f = w.Route.Forward(w.Distance);
            return Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
        }

        void Move(Walker w, float advance, float dt)
        {
            w.Distance += advance;
            if (w.Distance >= w.Route.Length) { Leave(w); return; }
            w.Anchor.position = w.Route.At(w.Distance);
            if (advance > 0f) w.Yaw = Mathf.SmoothDampAngle(w.Yaw, HeadingOf(w), ref w.YawVelocity, .25f, 160f, dt);
            w.Anchor.rotation = Quaternion.Euler(0f, w.Yaw, 0f);
        }

        // A person who reaches the end of a route has walked off the represented set (the ends sit
        // beyond its edges, as with the ambient crowd). They come back a moment later on a route
        // from the same side, facing inwards. No U-turn on the spot, no sliding sideways.
        void Leave(Walker w)
        {
            char side = routeEndSide[w.RouteIndex];
            var options = new List<int>();
            for (int r = 0; r < routes.Count; r++) if (routeStartSide[r] == side && r != w.RouteIndex) options.Add(r);
            if (options.Count == 0) for (int r = 0; r < routes.Count; r++) if (routeStartSide[r] == side) options.Add(r);
            w.RouteIndex = options.Count > 0 ? options[random.Next(options.Count)] : (w.RouteIndex + 1) % routes.Count;
            w.Route = routes[w.RouteIndex];
            w.Distance = 0f;
            w.Present = false;
            w.Anchor.gameObject.SetActive(false);
            // The courier is away briefly, so a quick tag never lands on empty paving.
            w.AwayLeft = (w == Courier ? .45f : .9f) + (float)random.NextDouble() * .8f;
        }

        void Enter(Walker w, bool force)
        {
            Vector3 start = w.Route.At(0f);
            if (!force)
                foreach (var other in Walkers)
                    if (other != w && other.Present && (other.Anchor.position - start).sqrMagnitude < 1.2f * 1.2f)
                    {
                        w.AwayLeft = .35f;   // entrance busy: wait a moment
                        return;
                    }
            w.Present = true;
            w.Anchor.gameObject.SetActive(true);
            w.Distance = 0f;
            w.Yaw = HeadingOf(w); w.YawVelocity = 0f;
            w.Anchor.position = start;
            w.Anchor.rotation = Quaternion.Euler(0f, w.Yaw, 0f);
            w.CurrentSpeed = NominalSpeed(w) * .85f;
            int slot = Walkers.IndexOf(w);
            if (slot >= 0 && slot < planned.Length) planned[slot] = NominalSpeed(w);
            w.Settled = false;
            Pose(w, 0f, false, false, false, 0f);
        }

        /// <summary>Makes sure a person is on the set (used when the case ends and the courier is shown).</summary>
        public void EnsurePresent(Walker w)
        {
            if (w != null && !w.Present) Enter(w, true);
        }

        void Pose(Walker w, float dt, bool phone, bool glance, bool wantStop, float habitPhase)
        {
            var p = w.Person;
            AnimationClip motion = Jogging(w) ? jog : walk;
            float baseSpeed = Jogging(w) ? JogMetresPerSecond : WalkMetresPerSecond;
            float cycle = motion.length;
            w.Moving = w.CurrentSpeed > .05f;

            // Gait phase is a 0..1 fraction, so a walk-to-jog switch keeps the same foot forward.
            // When stopping, the gait finishes the step and freezes on the next planted-foot pose
            // (phase 0 or 0.5) instead of snapping to the contact frame.
            bool nearlyStopped = wantStop && w.CurrentSpeed < .25f;
            if (!nearlyStopped) w.Settled = false;
            if (!w.Settled)
            {
                float rate = w.CurrentSpeed / baseSpeed;
                if (nearlyStopped) rate = Mathf.Max(rate, .4f);
                float next = w.Phase01 + dt * rate / cycle;
                if (nearlyStopped)
                {
                    float contact = Mathf.Ceil(w.Phase01 * 2f - .0001f) * .5f;
                    if (next >= contact) { next = contact; w.Settled = true; w.CurrentSpeed = 0f; }
                }
                w.Phase01 = Mathf.Repeat(next, 1f);
            }
            motion.SampleAnimation(w.Entity, w.Phase01 * cycle);

            // ---- arms: applied after sampling, so nothing accumulates frame to frame
            Vector3 forward = w.Anchor.forward, up = Vector3.up;
            Arm caseArm = w.RightArm;
            Arm phoneArm = p.Carry == 2 ? w.LeftArm : w.RightArm;
            if (w.Briefcase && caseArm != null)
            {
                Vector3 outward = caseArm.Side * w.Anchor.right;
                Vector3 grip = w.Hips.position + outward * .26f - up * .17f
                    + forward * (Mathf.Sin(w.Phase01 * Mathf.PI * 2f) * .02f);
                Vector3 wrist = Reach(caseArm, grip, outward * .6f - up * .6f - forward * .4f, 1f);
                float swing = Mathf.Sin(w.Phase01 * Mathf.PI * 2f + .6f) * Mathf.Clamp01(w.CurrentSpeed / baseSpeed) * 5f;
                w.Briefcase.transform.SetPositionAndRotation(wrist, w.Anchor.rotation * Quaternion.Euler(swing, 0f, 0f));
            }
            bool lifting = phone || w.OnEar;
            float earMix = 0f;
            if (phoneArm != null)
            {
                phoneArm.Weight = Mathf.MoveTowards(phoneArm.Weight, lifting ? 1f : 0f, dt * 2.8f);
                // Ear mix lives in the arm's own blend: 0 = reading, 1 = at the ear.
                w.HeadRoll = Mathf.MoveTowards(w.HeadRoll, w.OnEar ? 14f : 0f, dt * 60f);
                earMix = w.HeadRoll / 14f;
                if (phoneArm.Weight > .001f)
                {
                    Vector3 outward = phoneArm.Side * w.Anchor.right;
                    Vector3 reading = w.Head.position + forward * .2f - up * .31f + outward * .03f;
                    Vector3 atEar = w.Head.position + outward * .105f - up * .01f;
                    Vector3 wrist = Reach(phoneArm, Vector3.Lerp(reading, atEar, earMix),
                        outward * .6f - up * .8f - forward * .3f, phoneArm.Weight);
                    bool show = phoneArm.Weight > .55f;
                    w.Phone.SetActive(show);
                    if (show)
                    {
                        Quaternion read = Quaternion.LookRotation((w.Head.position + up * .04f - wrist).normalized, up);
                        Quaternion ear = Quaternion.LookRotation(-outward, up);
                        w.Phone.transform.SetPositionAndRotation(wrist, Quaternion.Slerp(read, ear, earMix));
                        bool blink = Mathf.Sin(clock * 30f) > 0f;
                        Tint(w.PhoneMaterial, earMix > .5f ? new Color(.05f, .06f, .07f) : new Color(.8f, .93f, 1f));
                        Tint(w.BeaconMaterial, w.OnEar ? (blink ? new Color(1f, .2f, .12f) : new Color(.45f, .08f, .05f)) : new Color(.3f, .05f, .04f));
                    }
                }
                else w.Phone.SetActive(false);
            }

            // ---- head: smoothed towards its target, applied after sampling
            float yawTarget = 0f, pitchTarget = 0f;
            if (w.Flinching)
            {
                Vector3 look = NoiseSource - w.Head.position; look.y = 0f;
                yawTarget = Mathf.Clamp(Vector3.SignedAngle(w.Anchor.forward, look, Vector3.up), -75f, 75f);
                pitchTarget = -12f;
            }
            else if (glance) yawTarget = 80f * Mathf.Sin(habitPhase / HabitSeconds[2] * Mathf.PI);
            else if (phone) pitchTarget = 22f;
            // Replaying an Echo: the recorded head angles are used as they are (no smoothing, no snap).
            float k = replaying ? 0f : (dt <= 0f ? 1f : 1f - Mathf.Exp(-9f * dt));
            w.HeadYaw += (yawTarget - w.HeadYaw) * k;
            w.HeadPitch += (pitchTarget - w.HeadPitch) * k;
            w.Head.localRotation *= Quaternion.Euler(w.HeadPitch, w.HeadYaw, w.HeadRoll);
        }


        // ------------------------------------------------------- Echo lens
        // The crowd is recorded about twelve times a second (the pose state that Pose() needs,
        // not just positions), so an Echo can replay the last EchoWindow seconds exactly: same
        // gait phase, same raised phone, same turned heads. While it plays Step() is not called,
        // and EndEcho() puts every person back exactly where the live simulation left them.
        public const float EchoWindow = 8f, EchoHz = 12f;

        struct Frame
        {
            public bool Present, Flinching, OnEar, Phone, Glance, Settled, Moving;
            public Vector3 Pos;
            public float Yaw, Phase01, Speed, HabitPhase, ArmR, ArmL, HeadYaw, HeadPitch, HeadRoll;
        }

        sealed class Snapshot
        {
            public float Clock;
            public bool Alert;
            public Frame[] People;
        }

        /// <summary>Set by the controller for cases where the Echo lens is available.</summary>
        public bool RecordEcho;
        readonly List<Snapshot> log = new List<Snapshot>();
        float nextRecord;
        Snapshot liveBackup;
        bool replaying;
        Frame[] scratch = new Frame[0];

        public bool EchoReady { get { return log.Count >= (int)(EchoHz * 2f); } }
        public bool Echoing { get { return liveBackup != null; } }
        public float OldestEcho { get { return log.Count == 0 ? 0f : Mathf.Max(log[0].Clock, NewestEcho - EchoWindow); } }
        public float NewestEcho { get { return log.Count == 0 ? 0f : log[log.Count - 1].Clock; } }

        Snapshot Capture()
        {
            var s = new Snapshot { Clock = clock, Alert = CourierAlert, People = new Frame[Walkers.Count] };
            for (int i = 0; i < Walkers.Count; i++)
            {
                var w = Walkers[i];
                s.People[i] = new Frame
                {
                    Present = w.Present, Pos = w.Anchor.position, Yaw = w.Yaw, Phase01 = w.Phase01, Speed = w.CurrentSpeed,
                    Flinching = w.Flinching, OnEar = w.OnEar, Phone = w.LastPhone, Glance = w.LastGlance, HabitPhase = w.LastHabitPhase,
                    Settled = w.Settled, Moving = w.Moving,
                    ArmR = w.RightArm != null ? w.RightArm.Weight : 0f, ArmL = w.LeftArm != null ? w.LeftArm.Weight : 0f,
                    HeadYaw = w.HeadYaw, HeadPitch = w.HeadPitch, HeadRoll = w.HeadRoll
                };
            }
            return s;
        }

        void Record()
        {
            nextRecord = clock + 1f / EchoHz;
            log.Add(Capture());
            while (log.Count > 0 && clock - log[0].Clock > EchoWindow + .5f) log.RemoveAt(0);
        }

        /// <summary>Freezes the live state and returns false if there is nothing to replay.</summary>
        public bool BeginEcho()
        {
            if (liveBackup != null || log.Count < 2) return false;
            liveBackup = Capture();
            return true;
        }

        /// <summary>Poses everyone as they were at crowd-clock time <paramref name="t"/>.</summary>
        public void ShowEcho(float t)
        {
            if (liveBackup == null || log.Count < 2) return;
            int i = log.Count - 2;
            while (i > 0 && log[i].Clock > t) i--;
            Snapshot a = log[i], b = log[i + 1];
            float k = Mathf.Clamp01((t - a.Clock) / Mathf.Max(.001f, b.Clock - a.Clock));
            if (scratch.Length != Walkers.Count) scratch = new Frame[Walkers.Count];
            for (int n = 0; n < scratch.Length; n++) scratch[n] = Blend(a.People[n], b.People[n], k);
            ApplyFrames(scratch, t, (k < .5f ? a : b).Alert);
        }

        /// <summary>Puts every person back exactly where the live simulation left them.</summary>
        public void EndEcho()
        {
            if (liveBackup == null) return;
            var backup = liveBackup;
            ApplyFrames(backup.People, backup.Clock, backup.Alert);
            liveBackup = null;
        }

        static Frame Blend(Frame a, Frame b, float k)
        {
            Frame n = k < .5f ? a : b;   // flags and presence come from the nearer snapshot
            if (!a.Present || !b.Present || (a.Pos - b.Pos).sqrMagnitude > 4f) return n;   // entering, leaving or jumped
            n.Pos = Vector3.Lerp(a.Pos, b.Pos, k);
            n.Yaw = Mathf.LerpAngle(a.Yaw, b.Yaw, k);
            float dPhase = Mathf.Repeat(b.Phase01 - a.Phase01 + .5f, 1f) - .5f;   // shortest way round the gait cycle
            n.Phase01 = Mathf.Repeat(a.Phase01 + dPhase * k, 1f);
            n.Speed = Mathf.Lerp(a.Speed, b.Speed, k);
            n.ArmR = Mathf.Lerp(a.ArmR, b.ArmR, k); n.ArmL = Mathf.Lerp(a.ArmL, b.ArmL, k);
            n.HeadYaw = Mathf.Lerp(a.HeadYaw, b.HeadYaw, k); n.HeadPitch = Mathf.Lerp(a.HeadPitch, b.HeadPitch, k);
            n.HeadRoll = Mathf.Lerp(a.HeadRoll, b.HeadRoll, k);
            return n;
        }

        void ApplyFrames(Frame[] frames, float at, bool alert)
        {
            clock = at; CourierAlert = alert;
            for (int i = 0; i < Walkers.Count; i++)
            {
                var w = Walkers[i];
                var f = frames[i];
                if (w.Anchor.gameObject.activeSelf != f.Present) w.Anchor.gameObject.SetActive(f.Present);
                w.Present = f.Present;
                if (!f.Present) continue;
                w.Anchor.SetPositionAndRotation(f.Pos, Quaternion.Euler(0f, f.Yaw, 0f));
                w.Yaw = f.Yaw; w.Phase01 = f.Phase01; w.CurrentSpeed = f.Speed;
                w.Flinching = f.Flinching; w.OnEar = f.OnEar;
                w.LastPhone = f.Phone; w.LastGlance = f.Glance; w.LastHabitPhase = f.HabitPhase;
                if (w.RightArm != null) w.RightArm.Weight = f.ArmR;
                if (w.LeftArm != null) w.LeftArm.Weight = f.ArmL;
                w.HeadYaw = f.HeadYaw; w.HeadPitch = f.HeadPitch; w.HeadRoll = f.HeadRoll;
                replaying = true;
                Pose(w, 0f, f.Phone, f.Glance, false, f.HabitPhase);   // dt = 0: nothing advances
                replaying = false;
                w.Settled = f.Settled; w.Moving = f.Moving;
            }
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
