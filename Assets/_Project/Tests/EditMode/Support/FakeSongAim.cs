using System;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// An aim chooser that records what a song asked for and answers with a chosen target and point, or, for a song that
    /// strikes several, with the beekeepers in <see cref="Nearest"/> (the Hydra) or the living ones in <see cref="Threats"/>
    /// (the Sting), as many as were asked for.
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

        /// <summary>
        /// The beekeepers <see cref="ChooseThreats"/> answers with, most threatening first. Only the living ones are written,
        /// as the session writes only living beekeepers.
        /// </summary>
        public ISwarmTarget[] Threats { get; set; } = Array.Empty<ISwarmTarget>();

        public int ThreatCalls { get; private set; }
        public int LastThreatCount { get; private set; }

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

        public int ChooseThreats(float range, int count, ISwarmTarget[] targets, out Vector3 point)
        {
            ThreatCalls++;
            LastRange = range;
            LastThreatCount = count;
            int most = Math.Min(count, targets.Length);
            int written = 0;
            for (int index = 0; index < Threats.Length && written < most; index++)
            {
                if (Threats[index].IsAlive)
                {
                    targets[written] = Threats[index];
                    written++;
                }
            }

            point = Point;
            return written;
        }
    }
}
