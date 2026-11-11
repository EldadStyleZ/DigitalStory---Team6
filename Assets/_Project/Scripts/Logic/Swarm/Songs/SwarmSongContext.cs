using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Everything a swarm song may read or change when it is cast.</summary>
    public readonly struct SwarmSongContext
    {
        /// <summary>Makes a context with no aim chooser: aimed songs strike <paramref name="aimTarget"/> or <paramref name="aimPoint"/>.</summary>
        public SwarmSongContext(
            Vector3 piperPosition,
            ISwarmTarget aimTarget,
            Vector3 aimPoint,
            SwarmBody swarm,
            SwarmFormMachine forms,
            FuryMeter fury,
            HiveSet hives,
            IReadOnlyList<ISwarmTarget> enemies,
            ISmokeClearer smoke)
            : this(piperPosition, aimTarget, aimPoint, swarm, forms, fury, hives, enemies, smoke, null)
        {
        }

        /// <summary>
        /// Makes a context. When <paramref name="aim"/> is set, an aimed song asks it for its target by its own aim rule
        /// as it is cast, instead of taking <paramref name="aimTarget"/> and <paramref name="aimPoint"/>.
        /// </summary>
        public SwarmSongContext(
            Vector3 piperPosition,
            ISwarmTarget aimTarget,
            Vector3 aimPoint,
            SwarmBody swarm,
            SwarmFormMachine forms,
            FuryMeter fury,
            HiveSet hives,
            IReadOnlyList<ISwarmTarget> enemies,
            ISmokeClearer smoke,
            ISongAim aim)
        {
            PiperPosition = piperPosition;
            AimTarget = aimTarget;
            AimPoint = aimPoint;
            Swarm = swarm;
            Forms = forms;
            Fury = fury;
            Hives = hives;
            Enemies = enemies;
            Smoke = smoke;
            Aim = aim;
        }

        /// <summary>Where the Piper stands.</summary>
        public Vector3 PiperPosition { get; }

        /// <summary>The beekeeper the Piper aims at, or null when there is none.</summary>
        public ISwarmTarget AimTarget { get; }

        /// <summary>
        /// Where an aimed song strikes when it has no living beekeeper in reach: the aim on the ground, or the ground ahead of
        /// the Piper when nothing is aimed at. Songs pull it in to their own range.
        /// </summary>
        public Vector3 AimPoint { get; }

        /// <summary>The swarm's wasps.</summary>
        public SwarmBody Swarm { get; }

        /// <summary>The swarm's shape.</summary>
        public SwarmFormMachine Forms { get; }

        /// <summary>The Fury meter.</summary>
        public FuryMeter Fury { get; }

        /// <summary>The hives.</summary>
        public HiveSet Hives { get; }

        /// <summary>Every beekeeper, alive or not.</summary>
        public IReadOnlyList<ISwarmTarget> Enemies { get; }

        /// <summary>Clears smoke clouds. May be null.</summary>
        public ISmokeClearer Smoke { get; }

        /// <summary>Chooses an aimed song's target by the song's aim rule as it is cast. May be null.</summary>
        public ISongAim Aim { get; }

        /// <summary>The same context, aimed at <paramref name="target"/> (or null) and <paramref name="point"/>.</summary>
        public SwarmSongContext AimedAt(ISwarmTarget target, Vector3 point)
        {
            return new SwarmSongContext(PiperPosition, target, point, Swarm, Forms, Fury, Hives, Enemies, Smoke, Aim);
        }
    }
}
