using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HarborWatch
{
    [RequireComponent(typeof(Camera))]
    public sealed class WalkReview : MonoBehaviour
    {
        readonly List<Material> materials = new List<Material>();
        readonly string[] labels = new string[4];
        Camera reviewCamera;
        GameObject walker;
        AnimationClip gait;
        Transform hips;
        Texture2D tiles;
        Material surface;
        float clock;
        bool closeView = true;
        bool slow;
        bool paused;
        string error;

        void Start()
        {
            reviewCamera = GetComponent<Camera>();
            reviewCamera.clearFlags = CameraClearFlags.SolidColor;
            reviewCamera.backgroundColor = new Color(.58f,.68f,.72f);
            Application.targetFrameRate = 30;
            surface = Resources.Load<Material>("ReviewSurface");
            var model = Resources.Load<GameObject>("HarborWalker");
            foreach (var clip in Resources.LoadAll<AnimationClip>("HarborWalker"))
                if (!clip.name.StartsWith("__") && clip.length > 0) { gait = clip; break; }
            if (!surface || !model || !gait) { Fail("Missing walk model, clip or material. Reimport HarborWalker.fbx."); return; }

            walker = Instantiate(model);
            foreach (var animation in walker.GetComponentsInChildren<Animation>()) animation.enabled = false;
            foreach (var t in walker.GetComponentsInChildren<Transform>())
                if (t.name == "Hips") { hips = t; break; }
            if (!hips) { Fail("The walking rig is missing its Hips bone."); return; }
            ApplyClothing();
            BuildPavement();
            Sample();
        }

        void Fail(string reason) { error = reason; Debug.LogError(reason, this); }

        Material Painted(Color tint)
        {
            var material = new Material(surface);
            material.color = tint;
            materials.Add(material);
            return material;
        }

        void ApplyClothing()
        {
            var colors = new Dictionary<string, Color32> {
                {"Denim",new Color32(53,76,89,255)}, {"Cuff",new Color32(64,88,100,255)},
                {"Jacket",new Color32(185,99,54,255)}, {"Hem",new Color32(128,69,44,255)},
                {"Skin",new Color32(171,115,84,255)}, {"Hair",new Color32(53,39,34,255)},
                {"Leather",new Color32(83,63,50,255)}, {"Sole",new Color32(180,172,150,255)},
                {"Metal",new Color32(191,157,102,255)}, {"Shirt",new Color32(203,200,180,255)}
            };
            foreach (var renderer in walker.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var slots = renderer.sharedMaterials;
                for (int i=0; i<slots.Length; i++)
                    if (slots[i] && colors.TryGetValue(slots[i].name, out var color))
                        slots[i] = Painted(color);
                renderer.sharedMaterials = slots;
                renderer.updateWhenOffscreen = true;
            }
        }

        void BuildPavement()
        {
            const int size = 128;
            tiles = new Texture2D(size,size,TextureFormat.RGB24,true);
            tiles.wrapMode = TextureWrapMode.Repeat;
            tiles.anisoLevel = 4;
            var pixels = new Color32[size*size];
            for (int y=0; y<size; y++)
                for (int x=0; x<size; x++)
                {
                    int shade = x<2 || y<2 ? 100 : 155+(x*47+y*19+x*y*7)%9;
                    pixels[y*size+x] = new Color32((byte)(shade+8),(byte)(shade+7),(byte)shade,255);
                }
            tiles.SetPixels32(pixels);
            tiles.Apply(true,true);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Harbor paving";
            floor.transform.localScale = Vector3.one*2;
            var material = Painted(Color.white);
            material.mainTexture = tiles;
            material.mainTextureScale = Vector2.one*32;
            floor.GetComponent<Renderer>().sharedMaterial = material;
        }

        void Sample()
        {
            gait.SampleAnimation(walker, clock);
            Vector3 focus = hips.position + Vector3.up*.03f;
            Vector3 offset = closeView ? new Vector3(2.8f,1.1f,-3.4f) : new Vector3(4,5,-5);
            reviewCamera.transform.position = (closeView ? focus : Vector3.up*.75f) + offset;
            reviewCamera.transform.LookAt(closeView ? focus : Vector3.up*.75f);
            reviewCamera.fieldOfView = closeView ? 32 : 46;
        }

        Rect Control(int i)
        {
            Rect area = Screen.safeArea;
            float gap = 4;
            float width = (area.width-5*gap)/4;
            float height = Mathf.Clamp(Screen.height*.08f,42,82);
            return new Rect(area.x+gap+i*(width+gap),Screen.height-area.yMin-height-8,width,height);
        }

        void Activate(int index)
        {
            switch(index)
            {
                case 0: closeView = !closeView; break;
                case 1: slow = !slow; break;
                case 2: paused = !paused; break;
                case 3: clock = 0; break;
            }
        }

        void Update()
        {
            if (!hips) return;
            var touch = Touchscreen.current;
            var mouse = Mouse.current;
            bool tap = touch != null && touch.primaryTouch.press.wasPressedThisFrame;
            if (tap || (mouse != null && mouse.leftButton.wasPressedThisFrame))
            {
                Vector2 p = tap ? touch.primaryTouch.position.ReadValue() : mouse.position.ReadValue();
                p.y = Screen.height-p.y;
                for (int i=0; i<4; i++) if (Control(i).Contains(p)) Activate(i);
            }
            if (!paused) clock = Mathf.Repeat(clock+Time.deltaTime*(slow ? .25f : 1f),gait.length);
            Sample();
        }

        void OnGUI()
        {
            if (error != null) { GUI.Box(new Rect(8,8,Screen.width-16,60),error); return; }
            labels[0] = closeView ? "Rooftop" : "Close-up";
            labels[1] = slow ? "Normal speed" : "Slow motion";
            labels[2] = paused ? "Resume" : "Pause";
            labels[3] = "Replay";
            var style = new GUIStyle(GUI.skin.button);
            style.fontSize = Mathf.Clamp(Mathf.RoundToInt(Control(0).width*.12f),12,24);
            for (int i=0; i<4; i++) GUI.Box(Control(i), labels[i], style);
        }

        void OnDestroy()
        {
            foreach (var material in materials) Destroy(material);
            if (tiles) Destroy(tiles);
        }
    }
}
