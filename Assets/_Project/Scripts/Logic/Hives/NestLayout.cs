using System;
using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Where the nests may stand (plan section H): each at least <see cref="NestConfig.HiveClearance"/> from every hive; the
    /// ones open at the start <see cref="NestConfig.FirstNestsNearest"/> to <see cref="NestConfig.FirstNestsFarthest"/> from
    /// the middle of the map; the rest <see cref="NestConfig.LaterNestsNearest"/> to <see cref="NestConfig.LaterNestsFarthest"/>
    /// from it and spread around it; and all inside the play area. Distances are on the ground, from the world origin.
    /// </summary>
    /// <remarks>
    /// The scene builder places the nests in trees, so it checks its layout here, and so does the scene's composition root
    /// before it starts a match. It runs once, not each frame, so it may allocate.
    /// </remarks>
    public static class NestLayout
    {
        private const float FullCircleDegrees = 360f;

        /// <summary>
        /// Checks <paramref name="nests"/>, in the order they open, against <paramref name="hives"/> and the play area, a
        /// square <paramref name="playAreaHalfSize"/> from the middle on each side.
        /// </summary>
        /// <exception cref="ArgumentException">A rule is broken. The message names the first nest that breaks one.</exception>
        public static void Check(IReadOnlyList<Vector3> nests, IReadOnlyList<Vector3> hives, NestConfig config, float playAreaHalfSize)
        {
            if (nests == null)
            {
                throw new ArgumentNullException(nameof(nests));
            }

            if (hives == null)
            {
                throw new ArgumentNullException(nameof(hives));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (nests.Count != config.NestCount)
            {
                throw new ArgumentException($"NestConfig opens {config.NestCount} nests in all, but the layout has {nests.Count}.", nameof(nests));
            }

            for (int index = 0; index < nests.Count; index++)
            {
                CheckNest(index, nests[index], hives, config, playAreaHalfSize);
            }

            CheckSpread(nests, config);
        }

        private static void CheckNest(int index, Vector3 nest, IReadOnlyList<Vector3> hives, NestConfig config, float playAreaHalfSize)
        {
            if (Mathf.Abs(nest.x) > playAreaHalfSize || Mathf.Abs(nest.z) > playAreaHalfSize)
            {
                throw new ArgumentException($"Nest {index} at {nest} is outside the play area, {playAreaHalfSize} m from the middle.", "nests");
            }

            float clearanceSquared = config.HiveClearance * config.HiveClearance;
            for (int hive = 0; hive < hives.Count; hive++)
            {
                if (GroundMath.DistanceSquared(nest, hives[hive]) < clearanceSquared)
                {
                    throw new ArgumentException($"Nest {index} at {nest} is closer than {config.HiveClearance} m to hive {hive}.", "nests");
                }
            }

            bool isFirst = index < config.OpenAtStart;
            float nearest = isFirst ? config.FirstNestsNearest : config.LaterNestsNearest;
            float farthest = isFirst ? config.FirstNestsFarthest : config.LaterNestsFarthest;
            float fromMiddle = Mathf.Sqrt(GroundMath.DistanceSquared(nest, Vector3.zero));
            if (fromMiddle < nearest || fromMiddle > farthest)
            {
                string which = isFirst ? "open at the start" : "that opens after a wave";
                throw new ArgumentException(
                    $"Nest {index}, {which}, is {fromMiddle:0.##} m from the middle; it must be {nearest} to {farthest} m.", "nests");
            }
        }

        // Seen from the middle, the later nests' bearings leave no gap wider than the config allows.
        private static void CheckSpread(IReadOnlyList<Vector3> nests, NestConfig config)
        {
            int later = nests.Count - config.OpenAtStart;
            if (later < 2)
            {
                return;
            }

            var bearings = new float[later];
            for (int index = 0; index < later; index++)
            {
                Vector3 nest = nests[config.OpenAtStart + index];
                bearings[index] = Mathf.Atan2(nest.x, nest.z) * Mathf.Rad2Deg;
            }

            Array.Sort(bearings);
            float widest = bearings[0] + FullCircleDegrees - bearings[later - 1];
            for (int index = 1; index < later; index++)
            {
                widest = Mathf.Max(widest, bearings[index] - bearings[index - 1]);
            }

            if (widest > config.LaterNestsWidestGapDegrees)
            {
                throw new ArgumentException(
                    $"The nests that open after a wave are not spread around the middle: {widest:0} degrees hold none of them, more than {config.LaterNestsWidestGapDegrees}.",
                    "nests");
            }
        }
    }
}
