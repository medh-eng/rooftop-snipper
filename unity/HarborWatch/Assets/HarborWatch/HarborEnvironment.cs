using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HarborWatch
{
    // Decorative scene art shared by Crowd, Mission and Drone. No colliders.
    // It advances only when the owning gameplay controller permits it.
    public sealed class HarborEnvironment : MonoBehaviour
    {
        public const float FerryPeriodSeconds = 64f;
        public const float DockX = 4.4f;
        public const float SeaX = -10.1f;
        static readonly Color Blue = new Color(.17f, .35f, .43f);
        static readonly Color Water = new Color(.38f, .64f, .70f);
        static readonly Color Cream = new Color(.84f, .77f, .62f);
        static readonly Color Rust = new Color(.70f, .33f, .21f);
        static readonly Color Slate = new Color(.36f, .43f, .44f);
        static readonly Color Glass = new Color(.24f, .46f, .52f);
        static readonly Color Lamp = new Color(.96f, .81f, .48f);
        readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        readonly List<Transform> ripples = new List<Transform>();
        readonly List<Vector3> rippleStarts = new List<Vector3>();
        readonly Transform[] birds = new Transform[2];
        readonly Transform[] leftWings = new Transform[2];
        readonly Transform[] rightWings = new Transform[2];
        Material template;
        Transform scenery, ferry, wake;
        float clock;
        bool ready;

        static float Ease(float u)
        {
            u = Mathf.Clamp01(u);
            return u * u * (3f - 2f * u);
        }

        // One continuous period: dock, depart, offshore pause, return, dock.
        public static float FerryLocalX(float seconds)
        {
            float t = Mathf.Repeat(seconds, FerryPeriodSeconds);
            if (t < 12f) return DockX;
            if (t < 29f) return Mathf.Lerp(DockX, SeaX, Ease((t - 12f) / 17f));
            if (t < 34f) return SeaX;
            if (t < 52f) return Mathf.Lerp(SeaX, DockX, Ease((t - 34f) / 18f));
            return DockX;
        }

        public void Initialise(Transform world, Material source)
        {
            if (ready || !world || !source) return;
            template = source;
            scenery = new GameObject("Living Harbor - Decorative").transform;
            scenery.SetParent(world, false);
            BuildQuay();
            BuildFerry();
            BuildBirds();
            ready = true;
            Step(0f);
        }

        Material Paint(Color colour)
        {
            Material mat;
            if (materials.TryGetValue(colour, out mat)) return mat;
            mat = new Material(template);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", colour);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", colour);
            materials.Add(colour, mat);
            return mat;
        }

        GameObject Part(Transform parent, string name, PrimitiveType shape,
            Vector3 position, Vector3 size, Color tint)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = Paint(tint);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var collider = part.GetComponent<Collider>();
            if (collider) Destroy(collider);
            return part;
        }

        void BuildQuay()
        {
            // Keep all four pedestrian entrances and the existing stopping
            // landmarks clear. These pieces are outside walking routes.
            Part(scenery, "Quay edge stripe", PrimitiveType.Cube,
                new Vector3(-2f, -.025f, -6.4f), new Vector3(10f, .04f, .07f), Cream);
            foreach (float x in new[] {-7.9f, -5.5f, .75f, 7.75f})
            {
                Part(scenery, "Bollard", PrimitiveType.Cylinder,
                    new Vector3(x, .13f, -6.52f), new Vector3(.12f, .15f, .12f), Slate);
                Part(scenery, "Bollard cap", PrimitiveType.Cube,
                    new Vector3(x, .3f, -6.52f), new Vector3(.3f, .06f, .2f), Blue);
            }
            foreach (float x in new[] {-7.95f, 8f})
            {
                Part(scenery, "Quay lamp pole", PrimitiveType.Cylinder,
                    new Vector3(x, 1.35f, -5.93f), new Vector3(.045f, 1.35f, .045f), Slate);
                Part(scenery, "Warm lamp cap", PrimitiveType.Sphere,
                    new Vector3(x, 2.75f, -5.93f), new Vector3(.2f, .13f, .2f), Lamp);
            }
            foreach (float x in new[] {-5.08f, -3.8f, -2.52f, 2.52f, 3.8f, 5.08f})
            {
                Part(scenery, "Terminal glass panel", PrimitiveType.Cube,
                    new Vector3(x, 2.17f, 4.57f), new Vector3(.8f, 1.26f, .045f), Glass);
                Part(scenery, "Terminal window sill", PrimitiveType.Cube,
                    new Vector3(x, 1.5f, 4.55f), new Vector3(.86f, .08f, .14f), Cream);
            }
            Part(scenery, "Canopy edge gold", PrimitiveType.Cube,
                new Vector3(0f, 4.02f, 2.8f), new Vector3(12.2f, .07f, .08f), Cream);
            Part(scenery, "Kiosk awning", PrimitiveType.Cube,
                new Vector3(-6.2f, 1.64f, 2.4f), new Vector3(1.4f, .12f, 1.42f), Rust);
            Part(scenery, "Ticket window", PrimitiveType.Cube,
                new Vector3(-6.2f, 1.05f, 1.82f), new Vector3(.7f, .5f, .035f), Glass);
            foreach (float x in new[] {4.72f, 6.28f})
                Part(scenery, "Bench support", PrimitiveType.Cube,
                    new Vector3(x, .19f, -2.7f), new Vector3(.10f, .34f, .34f), Slate);
            Part(scenery, "Observation roof air conditioner", PrimitiveType.Cube,
                new Vector3(-7.05f, 6.03f, -3.8f), new Vector3(.92f, .4f, .70f), Slate);
            Part(scenery, "Observation roof vent", PrimitiveType.Cylinder,
                new Vector3(-8.13f, 6.12f, -4.16f), new Vector3(.15f, .27f, .15f), Blue);

            // Cargo stacks beyond the west concourse and a distant crane.
            Part(scenery, "Harbor cargo container lower", PrimitiveType.Cube,
                new Vector3(-11.4f, .78f, -5.45f), new Vector3(2.5f, 1.5f, 1.3f), Rust);
            Part(scenery, "Harbor cargo container upper", PrimitiveType.Cube,
                new Vector3(-11.4f, 2.30f, -5.45f), new Vector3(2.5f, 1.5f, 1.3f), Blue);
            Part(scenery, "Crane mast", PrimitiveType.Cube,
                new Vector3(10.7f, 2.8f, -9.1f), new Vector3(.28f, 5.6f, .3f), Slate);
            Part(scenery, "Crane arm", PrimitiveType.Cube,
                new Vector3(10f, 5.48f, -9.1f), new Vector3(4f, .2f, .3f), Rust);
            Part(scenery, "Crane cable", PrimitiveType.Cube,
                new Vector3(8.5f, 4.05f, -9.1f), new Vector3(.035f, 2.5f, .035f), Slate);
            Part(scenery, "Crane counterweight", PrimitiveType.Cube,
                new Vector3(11.5f, 5.1f, -9.1f), new Vector3(.63f, .7f, .5f), Blue);

            // Opaque thin bands avoid mobile transparent sorting and reflection cost.
            for (int i = 0; i < 5; i++)
            {
                Vector3 start = new Vector3(-8f + i * 4f, -.208f,
                    -9.60f + i % 3 * .48f);
                Transform ripple = Part(scenery, "Water highlight",
                    PrimitiveType.Cube, start,
                    new Vector3(1.2f + i % 2 * .2f, .014f, .035f), Water).transform;
                ripples.Add(ripple);
                rippleStarts.Add(start);
            }
        }

        void BuildFerry()
        {
            ferry = new GameObject("Double-ended ferry").transform;
            ferry.SetParent(scenery, false);
            Part(ferry, "Blue ferry hull", PrimitiveType.Cube,
                new Vector3(0f, .19f, 0f), new Vector3(4.25f, .39f, 1.65f), Blue);
            Part(ferry, "Passenger deck", PrimitiveType.Cube,
                new Vector3(0f, .45f, 0f), new Vector3(4.35f, .16f, 1.91f), Cream);
            Part(ferry, "Ferry wheelhouse", PrimitiveType.Cube,
                new Vector3(0f, .94f, 0f), new Vector3(1.78f, .83f, 1.32f), Cream);
            Part(ferry, "Wheelhouse roof", PrimitiveType.Cube,
                new Vector3(0f, 1.41f, 0f), new Vector3(2.12f, .13f, 1.55f), Blue);
            foreach (float z in new[] {-.67f, .67f})
            {
                Part(ferry, "Wheelhouse window", PrimitiveType.Cube,
                    new Vector3(0f, 1.04f, z), new Vector3(1.40f, .42f, .045f), Glass);
                Part(ferry, "Deck rail", PrimitiveType.Cube,
                    new Vector3(0f, .75f, z * 1.35f),
                    new Vector3(3.89f, .055f, .05f), Slate);
            }
            foreach (float x in new[] {-1.76f, 1.76f})
            {
                Part(ferry, "Boarding rail", PrimitiveType.Cube,
                    new Vector3(x, .79f, 0f), new Vector3(.06f, .09f, 1.72f), Slate);
                Part(ferry, "Ferry signal light", PrimitiveType.Sphere,
                    new Vector3(x * 1.14f, .65f, 0f),
                    new Vector3(.14f, .14f, .14f), Lamp);
            }
            wake = Part(scenery, "Ferry wake", PrimitiveType.Cube,
                Vector3.zero, new Vector3(1f, .012f, .09f), Water).transform;
        }

        void BuildBirds()
        {
            for (int i = 0; i < birds.Length; i++)
            {
                var bird = new GameObject("Flying seabird").transform;
                bird.SetParent(scenery, false);
                Part(bird, "Bird body", PrimitiveType.Sphere,
                    Vector3.zero, new Vector3(.27f, .1f, .16f), Cream);
                var left = new GameObject("Left wing").transform;
                left.SetParent(bird, false);
                left.localPosition = new Vector3(0, 0, -.08f);
                Part(left, "Feather", PrimitiveType.Cube,
                    new Vector3(0, 0, -.24f), new Vector3(.17f, .035f, .48f), Cream);
                var right = new GameObject("Right wing").transform;
                right.SetParent(bird, false);
                right.localPosition = new Vector3(0, 0, .08f);
                Part(right, "Feather", PrimitiveType.Cube,
                    new Vector3(0, 0, .24f), new Vector3(.17f, .035f, .48f), Cream);
                birds[i] = bird;
                leftWings[i] = left;
                rightWings[i] = right;
            }
        }

        public void Step(float elapsed)
        {
            if (!ready) return;
            clock += Mathf.Clamp(elapsed, 0f, .075f);
            float x = FerryLocalX(clock);
            ferry.localPosition = new Vector3(x,
                -.13f + Mathf.Sin(clock * 1.35f) * .027f, -9.40f);
            float t = Mathf.Repeat(clock, FerryPeriodSeconds);
            bool sailing = (t >= 12f && t < 29f) || (t >= 34f && t < 52f);
            wake.gameObject.SetActive(sailing);
            wake.localPosition = new Vector3(x - 2.3f, -.196f, -9.4f);
            for (int i = 0; i < ripples.Count; i++)
                ripples[i].localPosition = rippleStarts[i] +
                    new Vector3(Mathf.Sin(clock * .52f + i) * .36f, 0f, 0f);
            for (int i = 0; i < birds.Length; i++)
            {
                float phase = clock * (.25f + i * .045f) + i * 2.5f;
                birds[i].localPosition = new Vector3(
                    (i == 0 ? -2f : 6f) + Mathf.Cos(phase) * 2.5f,
                    6.85f + i * .68f + Mathf.Sin(phase * 1.3f) * .22f,
                    -7.3f + Mathf.Sin(phase) * 1.15f);
                birds[i].localRotation = Quaternion.Euler(
                    0f, phase * Mathf.Rad2Deg + 90f, 0f);
                float flap = Mathf.Sin(clock * 5f + i) * 21f;
                leftWings[i].localRotation = Quaternion.Euler(-flap, 0f, 0f);
                rightWings[i].localRotation = Quaternion.Euler(flap, 0f, 0f);
            }
        }

        void OnDestroy()
        {
            foreach (var item in materials.Values)
                if (item) Destroy(item);
            materials.Clear();
        }
    }
}
