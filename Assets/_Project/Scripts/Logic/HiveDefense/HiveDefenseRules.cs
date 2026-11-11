using System;
using System.Collections.Generic;
using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>Everything a <see cref="HiveDefenseSession"/> needs from the Data layer, in one bundle.</summary>
    public sealed class HiveDefenseRules
    {
        /// <summary>Bundles the configs. None of them may be null.</summary>
        public HiveDefenseRules(
            SwarmConfig swarm,
            SquadConfig squads,
            PiperMoveConfig piperMove,
            FuryConfig fury,
            ColossusConfig colossus,
            HiveConfig hive,
            SmokeConfig smoke,
            WaveConfig waves,
            BeekeeperConfig stick,
            BeekeeperConfig smoker,
            AimConfig aim,
            SongConfig songConfig,
            IReadOnlyList<SwarmSongEffect> songs,
            int holeCount)
        {
            Swarm = swarm ?? throw new ArgumentNullException(nameof(swarm));
            Squads = squads ?? throw new ArgumentNullException(nameof(squads));
            PiperMove = piperMove ?? throw new ArgumentNullException(nameof(piperMove));
            Fury = fury ?? throw new ArgumentNullException(nameof(fury));
            Colossus = colossus ?? throw new ArgumentNullException(nameof(colossus));
            Hive = hive ?? throw new ArgumentNullException(nameof(hive));
            Smoke = smoke ?? throw new ArgumentNullException(nameof(smoke));
            Waves = waves ?? throw new ArgumentNullException(nameof(waves));
            Stick = stick ?? throw new ArgumentNullException(nameof(stick));
            Smoker = smoker ?? throw new ArgumentNullException(nameof(smoker));
            Aim = aim ?? throw new ArgumentNullException(nameof(aim));
            SongConfig = songConfig ?? throw new ArgumentNullException(nameof(songConfig));
            Songs = songs ?? throw new ArgumentNullException(nameof(songs));
            HoleCount = holeCount;
        }

        /// <summary>The swarm's numbers.</summary>
        public SwarmConfig Swarm { get; }

        /// <summary>The song slots' seed bands and the return beat after a song lets go.</summary>
        public SquadConfig Squads { get; }

        /// <summary>How fast the Piper moves and what slows it.</summary>
        public PiperMoveConfig PiperMove { get; }

        /// <summary>The Fury meter's numbers.</summary>
        public FuryConfig Fury { get; }

        /// <summary>The giant's numbers.</summary>
        public ColossusConfig Colossus { get; }

        /// <summary>Each hive's numbers.</summary>
        public HiveConfig Hive { get; }

        /// <summary>The smoke grenade and cloud numbers.</summary>
        public SmokeConfig Smoke { get; }

        /// <summary>The waves and the pauses between them.</summary>
        public WaveConfig Waves { get; }

        /// <summary>The stick beekeeper's numbers.</summary>
        public BeekeeperConfig Stick { get; }

        /// <summary>The smoker beekeeper's numbers.</summary>
        public BeekeeperConfig Smoker { get; }

        /// <summary>The aiming numbers, such as how far ahead of the Piper an aimed song strikes when there is nothing to hit.</summary>
        public AimConfig Aim { get; }

        /// <summary>How long the song window is and how many notes it remembers.</summary>
        public SongConfig SongConfig { get; }

        /// <summary>The songs the Piper can play.</summary>
        public IReadOnlyList<SwarmSongEffect> Songs { get; }

        /// <summary>How many holes the flute has.</summary>
        public int HoleCount { get; }
    }
}
