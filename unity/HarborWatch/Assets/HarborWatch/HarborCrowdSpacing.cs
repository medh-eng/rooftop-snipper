using UnityEngine;

namespace HarborWatch
{
    /// <summary>
    /// Cheap, deterministic crowd spacing for a small ambient scene. Returns a
    /// 0..1 speed multiplier; never moves visitors off their authored paths.
    /// Priority by pool index prevents both people stopping at a crossing.
    /// </summary>
    public static class HarborCrowdSpacing
    {
        public const float LookAheadSeconds = 1.35f;
        public const float BodyClearance = .85f;
        const float CheckRadius = 3.1f;

        public static float YieldFactor(
            Vector3 location, Vector3 forward, float speed, int index,
            Vector3 neighbourLocation, Vector3 neighbourForward,
            float neighbourSpeed, int neighbourIndex)
        {
            Vector3 delta = neighbourLocation - location;
            delta.y = 0;
            float distance = delta.magnitude;
            if (distance > CheckRadius) return 1f;

            Vector3 heading = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            Vector3 otherHeading = Vector3.ProjectOnPlane(neighbourForward, Vector3.up).normalized;
            if (heading.sqrMagnitude < .001f || otherHeading.sqrMagnitude < .001f)
                return 1f;

            float ahead = Vector3.Dot(delta, heading);
            float sameDirection = Vector3.Dot(heading, otherHeading);
            float lateral = Mathf.Sqrt(Mathf.Max(0, delta.sqrMagnitude - ahead * ahead));

            // Whoever is behind a neighbour in their own lane yields, regardless
            // of identity. Use hysteresis on the caller's smoothed movement.
            if (sameDirection > .72f && ahead > 0f && lateral < .68f)
                return Mathf.InverseLerp(.75f, 1.8f, ahead);

            // Constant-heading prediction is enough for a short crossing
            // horizon and does not require Physics or NavMesh per frame.
            Vector3 relative = otherHeading * neighbourSpeed - heading * speed;
            float velocitySquared = relative.sqrMagnitude;
            if (velocitySquared < .0001f) return 1f;
            float meetingTime = Mathf.Clamp(
                -Vector3.Dot(delta, relative) / velocitySquared, 0f, LookAheadSeconds);
            if (meetingTime < .02f) return 1f;
            float separationAtMeeting = (delta + relative * meetingTime).magnitude;
            if (separationAtMeeting >= BodyClearance) return 1f;

            // At crosswalk intersections the earlier-indexed walker goes first.
            // The other decelerates before the crossing rather than stepping
            // sideways into the kiosk, bench or ferry railing.
            if (index < neighbourIndex) return 1f;

            // Slow as the crossing approaches; stop when within a short step.
            float factor = Mathf.InverseLerp(.85f, 2.50f, distance);
            return Mathf.Clamp01(factor);
        }
    }
}
