using UnityEngine;

namespace HarborWatch
{
    /// <summary>
    /// Optional, non-violent landmark collection for Port Meridian.
    /// A three-bit save survives survey retries. No credits are awarded.
    /// Landmarks refer to existing static scenery, never moving visitors.
    /// </summary>
    public sealed class HarborLandmarkAlbum
    {
        public const int PhotoCount = 3;
        public const int FullMask = (1 << PhotoCount) - 1;

        static readonly string[] Names =
        {
            "Ticket kiosk", "Terminal canopy", "Harbor crane"
        };
        static readonly Vector3[] Positions =
        {
            new Vector3(-6.2f, 1.05f, 1.82f),
            new Vector3(0f, 4.02f, 2.8f),
            new Vector3(10f, 5.48f, -9.1f)
        };

        int mask;
        public int Mask => mask;
        public int CapturedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < PhotoCount; i++)
                    if ((mask & (1 << i)) != 0) count++;
                return count;
            }
        }
        public bool Complete => mask == FullMask;
        public int CurrentIndex
        {
            get
            {
                for (int i = 0; i < PhotoCount; i++)
                    if ((mask & (1 << i)) == 0) return i;
                return -1;
            }
        }
        public string CurrentTitle => CurrentIndex < 0 ? "Album complete" :
            Names[CurrentIndex];
        public Vector3 CurrentWorldPosition => CurrentIndex < 0 ? Vector3.zero :
            Positions[CurrentIndex];

        public void Load(int savedMask)
        {
            mask = savedMask & FullMask;
        }

        // Camera viewport coordinates are width-normalised. Account for
        // aspect ratio so portrait and landscape use the same angular framing.
        public static bool IsFramed(Vector3 viewport, float aspect, float fov)
        {
            if (viewport.z <= 0f || aspect <= 0f || fov > 48f || fov < 1f)
                return false;
            float x = (viewport.x - .5f) * aspect;
            float y = viewport.y - .5f;
            return x*x + y*y <= .09f * .09f;
        }

        public bool TryCapture(Vector3 viewport, float aspect, float fov)
        {
            int current = CurrentIndex;
            if (current < 0 || !IsFramed(viewport, aspect, fov)) return false;
            mask |= 1 << current;
            return true;
        }
    }
}
