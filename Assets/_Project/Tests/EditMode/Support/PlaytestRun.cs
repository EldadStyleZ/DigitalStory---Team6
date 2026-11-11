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

        /// <summary>Wasps paid for songs: a song's first cast and a farm phase charge nothing, except a farm phase for a tower.</summary>
        public int SpentOnSongs { get; set; }

        /// <summary>Of <see cref="SpentOnSongs"/>, the wasps paid for towers (the Post slot's songs), in either phase.</summary>
        public int SpentOnTowers { get; set; }

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

        /// <summary>The songs' names, in the match's order, which <see cref="DamageBySong"/> and <see cref="CastsBySong"/> follow.</summary>
        public string[] SongNames { get; set; }

        /// <summary>Damage each song dealt to beekeepers, in the match's song order: the health it took, never more than was left.</summary>
        public float[] DamageBySong { get; set; }

        /// <summary>How many times each song was cast, in either phase, in the match's song order.</summary>
        public int[] CastsBySong { get; set; }

        /// <summary>Damage the bot could not tie to a song. It stays 0 while every song that hurts is measured.</summary>
        public float UnattributedDamage { get; set; }

        /// <summary>Wasps spent in all: on songs and on healing.</summary>
        public int Spent => SpentOnSongs + SpentOnHealing;

        /// <summary>The share of the wasps spent that went into healing, from 0 to 1; 0 when nothing was spent.</summary>
        public float HealingShare => Spent > 0 ? (float)SpentOnHealing / Spent : 0f;

        /// <summary>The share of the wasps spent that went into towers, from 0 to 1; 0 when nothing was spent.</summary>
        public float TowerShare => Spent > 0 ? (float)SpentOnTowers / Spent : 0f;

        /// <summary>Damage dealt to beekeepers in all, by every song and the giant.</summary>
        public float DamageDealt
        {
            get
            {
                float sum = UnattributedDamage;
                for (int song = 0; song < DamageBySong.Length; song++)
                {
                    sum += DamageBySong[song];
                }

                return sum;
            }
        }
    }
}
