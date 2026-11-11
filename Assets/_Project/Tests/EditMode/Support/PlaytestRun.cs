namespace DigitalStory.Tests.EditMode
{
    /// <summary>What one playtest match measured: how it ended, what was left, and where the wasps came from and went.</summary>
    internal sealed class PlaytestRun
    {
        /// <summary>The plan's name.</summary>
        public string Plan { get; set; }

        /// <summary>The seed of the match and of the bot's own timing.</summary>
        public uint Seed { get; set; }

        /// <summary>True when the fifth wave was cleared.</summary>
        public bool Won { get; set; }

        /// <summary>True when the match ran out of time with both still standing: neither won nor lost.</summary>
        public bool TimedOut { get; set; }

        /// <summary>The round being played when the match ended: the round lost in, or the last round when won.</summary>
        public int Round { get; set; }

        /// <summary>Health left in every hive together, as a share of their full health, from 0 to 1.</summary>
        public float HiveHealthShare { get; set; }

        /// <summary>The lowest health share any hive had at the end; 0 when one was destroyed.</summary>
        public float WeakestHiveShare { get; set; }

        /// <summary>Hives standing at the end.</summary>
        public int HivesStanding { get; set; }

        /// <summary>Wasps alive at the end.</summary>
        public int WaspsLeft { get; set; }

        /// <summary>The swarm's capacity at the end.</summary>
        public int Capacity { get; set; }

        /// <summary>Wasps paid for songs (only a survive phase charges).</summary>
        public int SpentOnSongs { get; set; }

        /// <summary>Wasps paid into the hives' health.</summary>
        public int SpentOnHealing { get; set; }

        /// <summary>Wasps that came from the hives' stores: collected beside a hive, called by Hive Call, or taken by a respawn.</summary>
        public int EarnedFromStores { get; set; }

        /// <summary>Wasps that came from nests.</summary>
        public int EarnedFromNests { get; set; }

        /// <summary>Wasps given for calling a wave early.</summary>
        public int EarnedFromEarlyCalls { get; set; }

        /// <summary>Wasps lost to stick hits and smoke.</summary>
        public int LostToHits { get; set; }

        /// <summary>How many times the swarm collapsed.</summary>
        public int Collapses { get; set; }

        /// <summary>The swarm's level at the end: 1 plus the nests collected.</summary>
        public int Level { get; set; }

        /// <summary>Songs cast in survive phases.</summary>
        public int SongsCast { get; set; }

        /// <summary>Seconds of game time the match lasted.</summary>
        public float Seconds { get; set; }

        /// <summary>Of those, the seconds spent in survive phases; the rest were farm phases.</summary>
        public float SurviveSeconds { get; set; }

        /// <summary>
        /// The longest stretch, in seconds, of a survive phase with nothing to decide: no beekeeper within the Sting's reach of
        /// the core and no hive under attack.
        /// </summary>
        public float LongestQuietSeconds { get; set; }

        /// <summary>Wasps spent in all: on songs and on healing.</summary>
        public int Spent => SpentOnSongs + SpentOnHealing;

        /// <summary>The share of the wasps spent that went into healing, from 0 to 1; 0 when nothing was spent.</summary>
        public float HealingShare => Spent > 0 ? (float)SpentOnHealing / Spent : 0f;
    }
}
