using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Everything a swarm song may read or change when it is cast.</summary>
    public readonly struct SwarmSongContext
    {
        public SwarmSongContext(
            Vector3 piperPosition,
            ISwarmTarget aimTarget,
            SwarmBody swarm,
            SwarmFormMachine forms,
            FuryMeter fury,
            HiveSet hives,
            IReadOnlyList<ISwarmTarget> enemies,
            ISmokeClearer smoke)
        {
            PiperPosition = piperPosition;
            AimTarget = aimTarget;
            Swarm = swarm;
            Forms = forms;
            Fury = fury;
            Hives = hives;
            Enemies = enemies;
            Smoke = smoke;
        }

        /// <summary>Where the Piper stands.</summary>
        public Vector3 PiperPosition { get; }

        /// <summary>The beekeeper the Piper aims at, or null when there is none.</summary>
        public ISwarmTarget AimTarget { get; }

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
    }
}
