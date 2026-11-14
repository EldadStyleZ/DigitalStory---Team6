namespace DigitalStory.Data
{
    /// <summary>The moments an optional recorded sound can mark (plan section AC). Numbered for the library's slots.</summary>
    public enum GameSoundEvent
    {
        /// <summary>The swarm's hum, a loop under everything.</summary>
        SwarmLoop = 0,

        /// <summary>A beekeeper killed.</summary>
        BeekeeperDeath = 1,

        /// <summary>The Meteor's landing, and the giant's slam and leap landing.</summary>
        HeavyImpact = 2,

        /// <summary>A song cast.</summary>
        SongCast = 3,

        /// <summary>A wild nest collected by its combo.</summary>
        NestCollected = 4,

        /// <summary>A hive struck.</summary>
        HiveHit = 5,

        /// <summary>A wrong note that breaks a nest's combo.</summary>
        ComboWrongNote = 6,
    }
}
