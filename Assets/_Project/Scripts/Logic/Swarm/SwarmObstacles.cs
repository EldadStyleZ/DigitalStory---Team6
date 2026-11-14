using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Plan section X: what the free swarm flies round. Each tall prop, nest tree and hive stands for an upright cylinder
    /// taken from its box: the box's middle on the ground, a radius from its width and the height of its top. The
    /// composition root sends them to WaspSwarm.compute once a match, which bends a free wasp's goal round each one below
    /// its top.
    /// </summary>
    public static class SwarmObstacles
    {
        /// <summary>The most obstacles the swarm's shader holds; it must match OBSTACLE_MOST in WaspSwarm.compute.</summary>
        public const int MostObstacles = 96;

        /// <summary>
        /// One obstacle as the shader reads it: x and z of the box's middle, a radius of <paramref name="radiusShare"/> times
        /// the mean of the box's half-width and half-depth, and the height of the box's top.
        /// </summary>
        public static Vector4 FromBox(Bounds box, float radiusShare)
        {
            float halfWidth = (box.extents.x + box.extents.z) * 0.5f;
            return new Vector4(box.center.x, box.center.z, halfWidth * Mathf.Max(radiusShare, 0f), box.max.y);
        }

        /// <summary>
        /// Writes one obstacle per box into <paramref name="obstacles"/>, in order, skipping a box with no width, and stops
        /// when the array or <see cref="MostObstacles"/> is full.
        /// </summary>
        /// <returns>How many obstacles it wrote.</returns>
        /// <exception cref="ArgumentNullException">The boxes or the array are missing.</exception>
        public static int Fill(IReadOnlyList<Bounds> boxes, float radiusShare, Vector4[] obstacles)
        {
            if (boxes == null)
            {
                throw new ArgumentNullException(nameof(boxes));
            }

            if (obstacles == null)
            {
                throw new ArgumentNullException(nameof(obstacles));
            }

            int room = Math.Min(obstacles.Length, MostObstacles);
            int count = 0;
            for (int index = 0; index < boxes.Count && count < room; index++)
            {
                Vector4 obstacle = FromBox(boxes[index], radiusShare);
                if (obstacle.z > 0f)
                {
                    obstacles[count] = obstacle;
                    count++;
                }
            }

            return count;
        }
    }
}
