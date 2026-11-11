using System;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Distances, directions and time windows on the ground (height ignored), shared by the swarm's songs.</summary>
    internal static class GroundMath
    {
        // Not game tuning: two points closer than this (squared metres) stand on the same spot.
        private const float SameSpotSqrMetres = 1e-8f;

        /// <summary>The squared distance between two points on the ground.</summary>
        public static float DistanceSquared(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        /// <summary>The point, or its pull-in along the ground to <paramref name="maxRange"/> metres from the core when it is further. Its height is kept.</summary>
        public static Vector3 PullIn(Vector3 point, Vector3 core, float maxRange)
        {
            float distanceSquared = DistanceSquared(point, core);

            if (distanceSquared <= maxRange * maxRange)
            {
                return point;
            }

            float scale = maxRange / Mathf.Sqrt(distanceSquared);
            return new Vector3(core.x + (point.x - core.x) * scale, point.y, core.z + (point.z - core.z) * scale);
        }

        /// <summary>The unit direction along the ground from <paramref name="from"/> to <paramref name="to"/>, or forward when they stand on the same spot.</summary>
        public static Vector3 Direction(Vector3 from, Vector3 to)
        {
            float distanceSquared = DistanceSquared(to, from);

            if (distanceSquared < SameSpotSqrMetres)
            {
                return Vector3.forward;
            }

            float inverse = 1f / Mathf.Sqrt(distanceSquared);
            return new Vector3((to.x - from.x) * inverse, 0f, (to.z - from.z) * inverse);
        }

        /// <summary>How many seconds of the step from <paramref name="start"/> to <paramref name="end"/> fall inside the window from <paramref name="windowStart"/> to <paramref name="windowEnd"/>.</summary>
        public static float Overlap(float start, float end, float windowStart, float windowEnd)
        {
            return Math.Max(0f, Math.Min(end, windowEnd) - Math.Max(start, windowStart));
        }
    }
}
