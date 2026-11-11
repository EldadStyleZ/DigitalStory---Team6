using System;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// An aim chooser that records what a song asked for and answers with a chosen target and point, or, for a song that
    /// strikes several (the Hydra), with the beekeepers in <see cref="Nearest"/>, as many as were asked for.
    /// </summary>
    internal sealed class FakeSongAim : ISongAim
    {
        public ISwarmTarget Target { get; set; }
        public Vector3 Point { get; set; }
        public int Calls { get; private set; }
        public AimRule LastRule { get; private set; }
        public float LastRange { get; private set; }

        /// <summary>The beekeepers <see cref="ChooseNearest"/> answers with, nearest first.</summary>
        public ISwarmTarget[] Nearest { get; set; } = Array.Empty<ISwarmTarget>();

        public int NearestCalls { get; private set; }
        public int LastNearestCount { get; private set; }

        public void Choose(AimRule rule, float range, out ISwarmTarget target, out Vector3 point)
        {
            Calls++;
            LastRule = rule;
            LastRange = range;
            target = Target;
            point = Point;
        }

        public int ChooseNearest(float range, int count, ISwarmTarget[] targets, out Vector3 point)
        {
            NearestCalls++;
            LastRange = range;
            LastNearestCount = count;
            int written = Math.Min(Math.Min(count, targets.Length), Nearest.Length);
            for (int index = 0; index < written; index++)
            {
                targets[index] = Nearest[index];
            }

            point = Point;
            return written;
        }
    }
}
