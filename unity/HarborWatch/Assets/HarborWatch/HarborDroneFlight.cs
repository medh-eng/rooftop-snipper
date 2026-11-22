using UnityEngine;

namespace HarborWatch
{
    /// <summary>
    /// Lightweight bounds for the fictional spectator drone. These safety volumes
    /// are matched to the fixed Port Meridian blockout, not physical flight advice.
    /// Camera stays above pedestrians and slides around the observation building.
    /// </summary>
    public static class HarborDroneFlight
    {
        public const float MinAltitude = 4.75f;
        public const float MaxAltitude = 15.5f;
        public const float MaxHorizontalSpeed = 5.3f;
        public const float MaxVerticalSpeed = 3.0f;

        // 35 cm spectator camera clearance around the rooftop geometry.
        static readonly Vector3 RoofCentre = new Vector3(-7.4f, 0f, -5f);
        static readonly Vector3 RoofHalfExtent = new Vector3(1.95f, 0f, 2.45f);
        const float RoofBlockedBelow = 6.25f;

        public static Vector3 Constrain(Vector3 position)
        {
            return new Vector3(
                Mathf.Clamp(position.x, -11.4f, 11.4f),
                Mathf.Clamp(position.y, MinAltitude, MaxAltitude),
                Mathf.Clamp(position.z, -8.7f, 7.6f));
        }

        public static bool InsideRoof(Vector3 position)
        {
            return position.y < RoofBlockedBelow &&
                Mathf.Abs(position.x - RoofCentre.x) < RoofHalfExtent.x &&
                Mathf.Abs(position.z - RoofCentre.z) < RoofHalfExtent.z;
        }

        static Vector3 TryMove(Vector3 from, Vector3 candidate)
        {
            candidate = Constrain(candidate);
            return InsideRoof(candidate) ? from : candidate;
        }

        // Test the desired move first, then slide on individual axes if it
        // intersects the observation block. No per-frame Physics allocation.
        public static Vector3 Advance(Vector3 from, Vector3 displacement)
        {
            Vector3 target = Constrain(from + displacement);
            if (!InsideRoof(target)) return target;

            Vector3 slide = TryMove(from, new Vector3(target.x, from.y, from.z));
            slide = TryMove(slide, new Vector3(slide.x, slide.y, target.z));
            slide = TryMove(slide, new Vector3(slide.x, target.y, slide.z));
            return slide;
        }

        public static Vector3 HorizontalMovement(float turnDegrees, Vector2 stick)
        {
            // The view pitch never changes horizontal flight direction.
            float angle = turnDegrees * Mathf.Deg2Rad;
            Vector3 forward = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            return Vector3.ClampMagnitude(right * stick.x + forward * stick.y, 1f);
        }
    }
}
