namespace DigitalStory.Data
{
    /// <summary>The moments an optional recorded sound can mark (plan section AC). Numbered for the library's slots.</summary>
    public enum GameSoundEvent
    {
        /// <summary>The calm swarm's hum, a loop under everything.</summary>
        SwarmLoop = 0,

        /// <summary>An angrier buzz, a loop the hum cross-fades to while a strike runs or the giant stands.</summary>
        SwarmAngry = 1,

        /// <summary>The Sting cast: a quick jab.</summary>
        CastSting = 2,

        /// <summary>The Tornado cast: a swirling whoosh.</summary>
        CastTornado = 3,

        /// <summary>The Meteor cast: the swarm's climb.</summary>
        CastMeteor = 4,

        /// <summary>The Meteor striking the ground.</summary>
        MeteorImpact = 5,

        /// <summary>The Hydra cast: the heads rearing.</summary>
        CastHydra = 6,

        /// <summary>The Scythe cast: a sweep.</summary>
        CastScythe = 7,

        /// <summary>The Lance cast: a thrust.</summary>
        CastLance = 8,

        /// <summary>The Dome cast: a shield rising.</summary>
        CastDome = 9,

        /// <summary>The Gust cast: a gust of wind.</summary>
        CastGust = 10,

        /// <summary>The Hive Call cast: the swarm rallying.</summary>
        CastHiveCall = 11,

        /// <summary>A tower placed: the Honeycomb or the Nest trap.</summary>
        TowerPlaced = 12,

        /// <summary>A song refused: not enough wasps, cooling down, and the like.</summary>
        SongFailed = 13,

        /// <summary>A beekeeper hit and not killed.</summary>
        BeekeeperHit = 14,

        /// <summary>A beekeeper killed; it climbs with the kill chain.</summary>
        BeekeeperDeath = 15,

        /// <summary>A note that fills a nest combo's slot.</summary>
        ComboRightNote = 16,

        /// <summary>A note that breaks a nest's combo.</summary>
        ComboWrongNote = 17,

        /// <summary>A wild nest collected by its combo.</summary>
        NestCollected = 18,

        /// <summary>A wave of beekeepers coming.</summary>
        WaveStart = 19,

        /// <summary>A wave's last beekeeper gone.</summary>
        WaveCleared = 20,

        /// <summary>A hive struck.</summary>
        HiveHit = 21,

        /// <summary>A hive destroyed.</summary>
        HiveLost = 22,

        /// <summary>The match won.</summary>
        Win = 23,

        /// <summary>The match lost.</summary>
        Lose = 24,

        /// <summary>The swarm becoming the giant.</summary>
        GiantForm = 25,

        /// <summary>The giant's punches and its thrown fists landing.</summary>
        GiantPunch = 26,

        /// <summary>The giant's kick and spin kick.</summary>
        GiantKick = 27,

        /// <summary>The giant's ground slam.</summary>
        GiantSlam = 28,

        /// <summary>The giant's roar.</summary>
        GiantRoar = 29,

        /// <summary>The giant landing its leap.</summary>
        GiantLeapLanding = 30,

        /// <summary>The giant falling back into a swarm.</summary>
        GiantEnd = 31,
    }
}
