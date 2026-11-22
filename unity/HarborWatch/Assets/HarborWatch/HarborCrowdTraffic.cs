using System;
using System.Collections.Generic;
using UnityEngine;

namespace HarborWatch
{
    /// <summary>
    /// Non-interactive environmental life. Pedestrians follow rounded entrance-to-exit
    /// paths and disappear only beyond the represented concourse. Their rigs are
    /// reused when a different visitor arrives. No target or combat logic.
    /// </summary>
    public sealed class HarborCrowdTraffic : MonoBehaviour
    {
        const int PoolSize = 9;
        const int InitialActive = 6;
        const float WalkMetresPerSecond = 1.05f;
        const float JogMetresPerSecond = 2.05f;
        static readonly Color[] CoatPalette =
        {
            new Color(.72f, .36f, .19f), new Color(.17f, .44f, .60f),
            new Color(.53f, .54f, .30f), new Color(.64f, .34f, .45f),
            new Color(.28f, .54f, .43f), new Color(.74f, .60f, .30f),
            new Color(.43f, .35f, .61f), new Color(.53f, .44f, .34f),
            new Color(.31f, .46f, .54f)
        };
        static readonly Color[] DenimPalette =
        {
            new Color(.19f, .29f, .40f),
            new Color(.27f, .34f, .32f),
            new Color(.32f, .29f, .25f),
            new Color(.13f, .23f, .28f)
        };
        static readonly Color[] SkinPalette =
        {
            new Color(.72f, .49f, .35f), new Color(.61f, .39f, .26f),
            new Color(.82f, .58f, .43f), new Color(.51f, .33f, .25f)
        };

        // Four access points: west street, east street, terminal door and ferry gangway.
        // Endpoints sit beyond the set edges, so no visitor teleports on the paving.
        static readonly Vector3[][] Waypoints =
        {
            Track(-12.4f,-1.2f, -8.1f,-1.2f, -5.1f,-2.0f, -.5f,-2.9f,
                  4.0f,-1.6f, 8.1f,-1.2f, 12.5f,-1.2f),        // west -> east
            Track(12.5f,1.7f, 8.2f,1.7f, 5.1f,3.1f, 1.0f,2.6f,
                  -3.0f,3.8f, -7.9f,4.1f, -12.4f,3.8f),         // east -> west
            Track(0,8.2f, 0,5.4f, -.8f,3.6f, -2.7f,2.5f,
                  -5.1f,1.6f, -8.4f,-.5f, -12.4f,-.7f),         // terminal -> west
            Track(-12.4f,-3.2f, -8.5f,-1.7f, -5.5f,-2.8f,
                  -2.8f,-1.4f, -1.5f,1.2f, -.4f,3.9f, 0,8.2f), // west -> terminal
            Track(12.5f,-1.2f, 8.2f,-.7f, 6.2f,-.4f,
                  4.3f,-.8f, 3.9f,-3.5f, 4.4f,-5.7f,
                  4.4f,-9.0f),                                  // east -> ferry
            Track(4.4f,-9f, 4.6f,-5.9f, 6.6f,-4.0f,
                  8.7f,-2.8f, 12.5f,-2.8f),                    // ferry -> east
            Track(4.4f,-9f, 3.6f,-5.2f, 1.9f,-3.6f,
                  .4f,-1.1f, -.2f,1.8f, -.3f,4.2f, 0,8.2f),   // ferry -> terminal
            Track(.2f,8.2f, .5f,4.2f, 2.2f,3.3f,
                  4.4f,3.7f, 8.3f,1.8f, 12.5f,1.8f),          // terminal -> east
            Track(-12.4f,.7f, -8.5f,.6f, -6.7f,-1.2f,
                  -3.2f,-3.5f, .6f,-4.4f, 3.3f,-5.6f,
                  4.4f,-9f),                                    // west -> ferry
            Track(4.4f,-9f, 2.6f,-5.3f, -.8f,-3.8f,
                  -4.9f,-3.9f, -8.4f,-1.6f, -12.4f,-1.7f)      // ferry -> west
        };

        static Vector3[] Track(params float[] coordinates)
        {
            var result = new Vector3[coordinates.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = new Vector3(coordinates[2 * i], 0, coordinates[2 * i + 1]);
            return result;
        }

        sealed class Path
        {
            readonly Vector3[] samples;
            readonly float[] metres;
            public float Length { get; private set; }

            public Path(Vector3[] controls)
            {
                var nodes = new List<Vector3>();
                for (int i = 0; i < controls.Length - 1; i++)
                {
                    Vector3 a = controls[Mathf.Max(0, i - 1)];
                    Vector3 b = controls[i];
                    Vector3 c = controls[i + 1];
                    Vector3 d = controls[Mathf.Min(controls.Length - 1, i + 2)];
                    float segment = Vector3.Distance(b, c);
                    Vector3 left = Vector3.ClampMagnitude((c - a) * .5f, segment);
                    Vector3 right = Vector3.ClampMagnitude((d - b) * .5f, segment);
                    const int pieces = 12;
                    for (int j = 0; j < pieces; j++)
                    {
                        float t = j / (float)pieces;
                        float t2 = t * t, t3 = t2 * t;
                        nodes.Add((2*t3-3*t2+1)*b + (t3-2*t2+t)*left
                                  + (-2*t3+3*t2)*c + (t3-t2)*right);
                    }
                }
                nodes.Add(controls[controls.Length - 1]);
                samples = nodes.ToArray();
                metres = new float[samples.Length];
                for (int i = 1; i < samples.Length; i++)
                    metres[i] = metres[i-1] + Vector3.Distance(samples[i-1], samples[i]);
                Length = metres[metres.Length - 1];
            }

            public Vector3 At(float distance)
            {
                float clamped = Mathf.Clamp(distance, 0f, Length);
                int low = 0, high = metres.Length - 1;
                while (low < high)
                {
                    int mid = (low + high) / 2;
                    if (metres[mid] < clamped) low = mid + 1;
                    else high = mid;
                }
                int to = Mathf.Max(1, low);
                int from = to - 1;
                float delta = metres[to] - metres[from];
                return Vector3.Lerp(samples[from], samples[to],
                    delta < .0001f ? 0 : (clamped - metres[from]) / delta);
            }

            public Vector3 Forward(float distance)
            {
                Vector3 forward = At(distance + .2f) - At(distance - .2f);
                return forward.sqrMagnitude > .0001f ? forward.normalized : Vector3.forward;
            }
        }

        sealed class Agent
        {
            public GameObject entity;
            public Transform anchor;
            public Transform hips;
            public GameObject hat;
            public GameObject backpack;
            public GameObject satchel;
            public Material accessoryMaterial;
            public readonly Dictionary<string, Material> clothing = new Dictionary<string, Material>();
            public Path route;
            public AnimationClip motion;
            public bool visible, jogging;
            public float distance, time, speedMultiplier, respawnDelay, currentSpeed;
            public int appearance;
        }

        readonly List<Material> ownedMaterials = new List<Material>();
        readonly Agent[] agents = new Agent[PoolSize];
        readonly float[] plannedSpeeds = new float[PoolSize];
        readonly Path[] routes = new Path[Waypoints.Length];
        readonly System.Random random = new System.Random(1307);
        AnimationClip walk, jog;
        GameObject prefab;
        Material sourceMaterial;
        Transform map;
        int lastRoute = -1;
        int arrivals, departures;

        public int ActiveCount
        {
            get { int result = 0; foreach (var a in agents) if (a != null && a.visible) result++; return result; }
        }
        public int Arrivals { get { return arrivals; } }
        public int Departures { get { return departures; } }

        static AnimationClip FindClip(string resource)
        {
            foreach (var clip in Resources.LoadAll<AnimationClip>(resource))
                if (!clip.name.StartsWith("__") && clip.length > 0) return clip;
            return null;
        }

        public bool Initialize(Transform sceneRoot, Material material)
        {
            map = sceneRoot;
            sourceMaterial = material;
            prefab = Resources.Load<GameObject>("CrowdWalk");
            walk = FindClip("CrowdWalk");
            jog = FindClip("CrowdJog");
            if (!prefab || !walk || !jog || !sourceMaterial)
            {
                Debug.LogError("Port Meridian crowd needs CrowdWalk.fbx, CrowdJog.fbx and ReviewSurface.mat.");
                return false;
            }
            for (int i = 0; i < routes.Length; i++) routes[i] = new Path(Waypoints[i]);

            for (int i = 0; i < PoolSize; i++)
            {
                var a = new Agent();
                a.anchor = new GameObject("Visitor " + (i + 1).ToString("00")).transform;
                a.anchor.SetParent(map, false);
                a.entity = Instantiate(prefab, a.anchor);
                a.entity.name = "Original commuter rig";
                foreach (var animation in a.entity.GetComponentsInChildren<Animation>(true))
                    animation.enabled = false;
                Transform head = null, chest = null;
                foreach (var bone in a.entity.GetComponentsInChildren<Transform>(true))
                {
                    if (bone.name == "Hips") a.hips = bone;
                    if (bone.name == "Head") head = bone;
                    if (bone.name == "Chest") chest = bone;
                }
                if (!a.hips || !head || !chest)
                {
                    Debug.LogError("CrowdWalk FBX rig is missing a required bone.");
                    return false;
                }
                CreateClothing(a);
                walk.SampleAnimation(a.entity, 0);
                CreateAccessories(a, head, chest);
                a.entity.SetActive(false);
                agents[i] = a;
            }
            ResetTraffic();
            return true;
        }

        Material CopyMaterial(Color colour)
        {
            var mat = new Material(sourceMaterial);
            ownedMaterials.Add(mat);
            SetTint(mat, colour);
            return mat;
        }

        static void SetTint(Material mat, Color colour)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", colour);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", colour);
        }

        void CreateClothing(Agent a)
        {
            foreach (var renderer in a.entity.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (!slots[i]) continue;
                    string key = slots[i].name.Replace(" (Instance)", "");
                    Material material;
                    if (!a.clothing.TryGetValue(key, out material))
                    {
                        material = CopyMaterial(Color.white);
                        a.clothing.Add(key, material);
                    }
                    slots[i] = material;
                }
                renderer.sharedMaterials = slots;
                renderer.updateWhenOffscreen = false; // Android-friendly at crowd scale.
            }
            a.accessoryMaterial = CopyMaterial(new Color(.31f, .28f, .23f));
        }

        static GameObject AttachToBone(string name, Transform bone,
            Transform walkerRoot, Vector3 expectedRootPosition)
        {
            var pivot = new GameObject(name);
            pivot.transform.position = walkerRoot.TransformPoint(expectedRootPosition);
            pivot.transform.rotation = walkerRoot.rotation;
            pivot.transform.SetParent(bone, true);
            return pivot;
        }

        static void AccessoryPiece(GameObject pivot, string name, PrimitiveType kind,
            Vector3 offset, Vector3 size, Material material, Vector3 euler)
        {
            GameObject piece = GameObject.CreatePrimitive(kind);
            piece.name = name;
            piece.transform.SetParent(pivot.transform, false);
            piece.transform.localPosition = offset;
            piece.transform.localRotation = Quaternion.Euler(euler);
            piece.transform.localScale = size;
            piece.GetComponent<Renderer>().sharedMaterial = material;
            var collider = piece.GetComponent<Collider>();
            if (collider) Destroy(collider);
        }

        void CreateAccessories(Agent a, Transform head, Transform chest)
        {
            a.hat = AttachToBone("Optional cap", head, a.entity.transform,
                new Vector3(0, 1.745f, 0));
            AccessoryPiece(a.hat, "Cap brim", PrimitiveType.Cylinder,
                Vector3.zero, new Vector3(.33f,.018f,.32f), a.accessoryMaterial, Vector3.zero);
            AccessoryPiece(a.hat, "Cap crown", PrimitiveType.Cylinder,
                new Vector3(0,.085f,0), new Vector3(.24f,.09f,.24f),
                a.accessoryMaterial, Vector3.zero);
            AccessoryPiece(a.hat, "Front peak", PrimitiveType.Cube,
                new Vector3(0,0,.16f), new Vector3(.21f,.02f,.13f),
                a.accessoryMaterial, Vector3.zero);

            a.backpack = AttachToBone("Optional backpack", chest, a.entity.transform,
                new Vector3(0, 1.28f, -.18f));
            AccessoryPiece(a.backpack, "Fabric pack", PrimitiveType.Cube,
                Vector3.zero, new Vector3(.32f,.38f,.17f),
                a.accessoryMaterial, Vector3.zero);
            AccessoryPiece(a.backpack, "Pack pocket", PrimitiveType.Cube,
                new Vector3(0,-.055f,-.085f), new Vector3(.23f,.19f,.055f),
                a.accessoryMaterial, Vector3.zero);

            a.satchel = AttachToBone("Optional messenger bag", chest, a.entity.transform,
                new Vector3(-.18f, 1.02f, -.16f));
            AccessoryPiece(a.satchel, "Side satchel", PrimitiveType.Cube,
                Vector3.zero, new Vector3(.25f,.27f,.12f),
                a.accessoryMaterial, new Vector3(0,0,-8));
        }

        void PaintVisitor(Agent a)
        {
            var coat = CoatPalette[random.Next(CoatPalette.Length)];
            var denim = DenimPalette[random.Next(DenimPalette.Length)];
            var skin = SkinPalette[random.Next(SkinPalette.Length)];
            foreach (var entry in a.clothing)
            {
                Color c;
                switch (entry.Key)
                {
                    case "Jacket": c = coat; break;
                    case "Hem": c = coat * .76f; break;
                    case "Denim": c = denim; break;
                    case "Cuff": c = denim * .88f; break;
                    case "Skin": c = skin; break;
                    case "Hair": c = new Color(.17f,.12f,.11f); break;
                    case "Shirt": c = new Color(.82f,.82f,.73f); break;
                    case "Leather": c = new Color(.28f,.22f,.17f); break;
                    case "Sole": c = new Color(.66f,.64f,.58f); break;
                    case "Metal": c = new Color(.75f,.61f,.41f); break;
                    default: c = new Color(.64f,.62f,.58f); break;
                }
                SetTint(entry.Value, c);
            }
            a.appearance = random.Next(5);
            a.hat.SetActive(a.appearance == 1 || a.appearance == 3);
            a.backpack.SetActive(a.appearance == 2 || a.appearance == 3);
            a.satchel.SetActive(a.appearance == 4);
            SetTint(a.accessoryMaterial, CoatPalette[random.Next(CoatPalette.Length)] * .7f);
        }

        int ChooseRoute()
        {
            int choice = random.Next(routes.Length);
            if (choice == lastRoute) choice = (choice + 1 + random.Next(routes.Length - 1)) % routes.Length;
            lastRoute = choice;
            return choice;
        }

        void StartJourney(Agent a, int routeIndex, float startFraction)
        {
            a.route = routes[routeIndex];
            a.jogging = random.NextDouble() < .22;
            a.motion = a.jogging ? jog : walk;
            a.speedMultiplier = .83f + .34f * (float)random.NextDouble();
            a.distance = a.route.Length * Mathf.Clamp01(startFraction);
            a.time = (float)random.NextDouble() * a.motion.length;
            a.currentSpeed = (a.jogging ? JogMetresPerSecond : WalkMetresPerSecond) * a.speedMultiplier;
            a.respawnDelay = 0;
            a.visible = true;
            a.entity.SetActive(true);
            PaintVisitor(a);
            Vector3 forward = a.route.Forward(a.distance);
            a.anchor.position = a.route.At(a.distance);
            a.anchor.rotation = Quaternion.LookRotation(forward, Vector3.up);
            a.motion.SampleAnimation(a.entity, a.time);
            arrivals++;
        }

        public void ResetTraffic()
        {
            arrivals = departures = 0;
            lastRoute = -1;
            for (int i = 0; i < agents.Length; i++)
            {
                Agent a = agents[i];
                a.visible = false;
                a.entity.SetActive(false);
                if (i < InitialActive)
                {
                    // The initial six start partway along separate streets.
                    StartJourney(a, i % routes.Length, .20f + .09f * i);
                }
                else a.respawnDelay = .7f + (i - InitialActive) * 2.1f;
            }
        }

        bool EntranceClear(Vector3 entrance)
        {
            foreach (var other in agents)
            {
                if (other != null && other.visible &&
                    Vector3.SqrMagnitude(other.anchor.position - entrance) < 1.4f * 1.4f)
                    return false;
            }
            return true;
        }

        public void Step(float seconds)
        {
            float dt = Mathf.Min(seconds, .06f); // no large leaps after editor pauses.
            // Plan all movements from the same frame snapshot, not the positions
            // already updated by an earlier pedestrian in this loop.
            System.Array.Clear(plannedSpeeds, 0, plannedSpeeds.Length);
            for (int i = 0; i < agents.Length; i++)
            {
                Agent a = agents[i];
                if (a == null || !a.visible) continue;
                float nominal = (a.jogging ? JogMetresPerSecond : WalkMetresPerSecond)
                    * a.speedMultiplier;
                float factor = 1f;
                Vector3 forward = a.route.Forward(a.distance);
                for (int j = 0; j < agents.Length; j++)
                {
                    if (i == j || agents[j] == null || !agents[j].visible) continue;
                    Agent b = agents[j];
                    float neighbourNominal =
                        (b.jogging ? JogMetresPerSecond : WalkMetresPerSecond)
                        * b.speedMultiplier;
                    factor = Mathf.Min(factor, HarborCrowdSpacing.YieldFactor(
                        a.anchor.position, forward, nominal, i,
                        b.anchor.position, b.route.Forward(b.distance),
                        neighbourNominal, j));
                }
                plannedSpeeds[i] = nominal * factor;
            }

            for (int i = 0; i < agents.Length; i++)
            {
                Agent a = agents[i];
                if (a == null) continue;
                if (!a.visible)
                {
                    a.respawnDelay -= dt;
                    if (a.respawnDelay <= 0f)
                    {
                        int routeIndex = ChooseRoute();
                        if (EntranceClear(routes[routeIndex].At(0f)))
                            StartJourney(a, routeIndex, 0f);
                        else
                            a.respawnDelay = .6f;
                    }
                    continue;
                }

                float nominal = (a.jogging ? JogMetresPerSecond : WalkMetresPerSecond)
                    * a.speedMultiplier;
                // Decelerate quickly for crossings; accelerate more gently.
                float target = plannedSpeeds[i];
                float responsiveness = target < a.currentSpeed ? 3.5f : 1.35f;
                a.currentSpeed = Mathf.MoveTowards(a.currentSpeed, target, responsiveness * dt);
                a.distance += a.currentSpeed * dt;
                // Slow the gait when yielding. This keeps planted shoes closer
                // to the same paving joint than advancing the clip at full speed.
                float baseSpeed = a.jogging ? JogMetresPerSecond : WalkMetresPerSecond;
                a.time = Mathf.Repeat(a.time + dt * a.currentSpeed / baseSpeed, a.motion.length);
                if (a.distance >= a.route.Length)
                {
                    a.entity.SetActive(false);
                    a.visible = false;
                    a.respawnDelay = 1.4f + (float)random.NextDouble() * 3.2f;
                    departures++;
                    continue;
                }
                a.anchor.position = a.route.At(a.distance);
                Quaternion desired = Quaternion.LookRotation(a.route.Forward(a.distance), Vector3.up);
                a.anchor.rotation = Quaternion.RotateTowards(
                    a.anchor.rotation, desired, (a.jogging ? 190f : 145f) * dt);
                a.motion.SampleAnimation(a.entity, a.time);
            }
        }

        // In the Unity Scene view, select the Main Camera to inspect route
        // geometry and the personal-space rings used for traffic review.
        void OnDrawGizmosSelected()
        {
            if (routes == null || routes.Length == 0) return;
            Gizmos.color = new Color(.15f, .75f, .78f, .65f);
            foreach (var path in routes)
            {
                if (path == null) continue;
                Vector3 previous = path.At(0);
                for (float distance = .4f; distance < path.Length; distance += .4f)
                {
                    Vector3 next = path.At(distance);
                    Gizmos.DrawLine(previous + Vector3.up * .05f, next + Vector3.up * .05f);
                    previous = next;
                }
                Gizmos.DrawLine(previous + Vector3.up * .05f,
                    path.At(path.Length) + Vector3.up * .05f);
            }
            Gizmos.color = new Color(1f, .62f, .20f, .85f);
            foreach (var a in agents)
            {
                if (a == null || !a.visible) continue;
                Vector3 marker = a.anchor.position + Vector3.up * .1f;
                Gizmos.DrawWireSphere(marker, HarborCrowdSpacing.BodyClearance * .5f);
            }
        }

        public int NextVisible(int previous)
        {
            for (int n = 1; n <= agents.Length; n++)
            {
                int index = (previous + n + agents.Length) % agents.Length;
                if (agents[index].visible) return index;
            }
            return -1;
        }

        public bool TryFocus(int index, out Vector3 location)
        {
            if (index >= 0 && index < agents.Length && agents[index].visible)
            {
                location = agents[index].hips.position + Vector3.up * .12f;
                return true;
            }
            location = Vector3.zero;
            return false;
        }

        void OnDestroy()
        {
            foreach (var material in ownedMaterials)
                if (material) Destroy(material);
        }
    }
}
