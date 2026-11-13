using System;
using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// A whole match as the scene sets it up, for the playtest bot: every config and song at its code default (what
    /// ResetConfigsAndBuild writes into the assets), the smoker as the scene builder makes it, the songs in the builder's
    /// order, and the hives, spawn points and nest trees of the map the game draws for the seed (plan section S), with the
    /// core starting in the middle. The seed draws both the map and the session's random source, so each seed of a sweep
    /// plays on its own map.
    /// </summary>
    internal sealed class PlaytestMatch : IDisposable
    {
        /// <summary>The song indices, in the scene builder's order: the attacks, the defenses, then the gold songs.</summary>
        public const int StingIndex = 0;
        public const int TornadoIndex = 1;
        public const int MeteorIndex = 2;
        public const int HydraIndex = 3;
        public const int ScytheIndex = 4;
        public const int LanceIndex = 5;
        public const int NestIndex = 6;
        public const int DomeIndex = 7;
        public const int GustIndex = 8;
        public const int HoneycombIndex = 9;
        public const int HiveCallIndex = 10;
        public const int ColossusIndex = 11;

        /// <summary>The flute's holes: H J K L.</summary>
        public const int HoleCount = 4;

        // HiveDefenseSceneBuilder.LoadOrCreateSmoker: the smoker's numbers where they differ from the stick's defaults.
        private const string SmokerJson =
            "{\"<Kind>k__BackingField\":1,\"<Health>k__BackingField\":40,\"<Speed>k__BackingField\":2.2,"
            + "\"<NoticeRange>k__BackingField\":7,\"<GiveUpRange>k__BackingField\":14,\"<AttackRange>k__BackingField\":14,"
            + "\"<WindUpSeconds>k__BackingField\":0.8,\"<CooldownSeconds>k__BackingField\":4.5,"
            + "\"<HiveDamage>k__BackingField\":0,\"<WaspsKnockedOff>k__BackingField\":0}";

        // The bot's map has no scenery: the props only stand around the edge, and nothing in the rules touches them.
        private static readonly float[] NoScenery = new float[0];

        private readonly List<ScriptableObject> _created = new List<ScriptableObject>();
        private readonly SwarmSongEffect[] _songs;

        /// <summary>Builds the match. <paramref name="seed"/> seeds the session's random source.</summary>
        public PlaytestMatch(uint seed)
        {
            CoreMove = Make<CoreMoveConfig>();
            Aim = Make<AimConfig>();
            Colossus = Make<ColossusConfig>();
            Hive = Make<HiveConfig>();
            Nests = Make<NestConfig>();
            Waves = Make<WaveConfig>();
            var smoker = Make<BeekeeperConfig>();
            JsonUtility.FromJsonOverwrite(SmokerJson, smoker);

            _songs = new SwarmSongEffect[]
            {
                Make<StingSong>(), Make<TornadoSong>(), Make<MeteorSong>(), Make<HydraSong>(), Make<ScytheSong>(), Make<LanceSong>(),
                Make<NestSong>(), Make<DomeSong>(), Make<GustSong>(), Make<HoneycombSong>(), Make<HiveCallSong>(), Make<ColossusSong>(),
            };

            var rules = new HiveDefenseRules(
                Make<SwarmConfig>(), Make<SquadConfig>(), CoreMove, Make<FuryConfig>(), Colossus, Hive, Nests, Make<SwarmLevelConfig>(),
                Make<SmokeConfig>(), Waves, Make<BeekeeperConfig>(), smoker, Aim, Make<SongConfig>(), _songs,
                HoleCount);
            Layout = Make<LayoutConfig>();
            Map = LayoutGenerator.Generate(unchecked((int)seed), Vector3.zero, Layout, Nests, NoScenery);
            Session = new HiveDefenseSession(rules, Map.Hives, Map.Nests, Map.Spawns, Vector3.zero, new Mulberry32Random(seed));
        }

        /// <summary>The match.</summary>
        public HiveDefenseSession Session { get; }

        public CoreMoveConfig CoreMove { get; }
        public AimConfig Aim { get; }
        public ColossusConfig Colossus { get; }
        public HiveConfig Hive { get; }
        public NestConfig Nests { get; }
        public WaveConfig Waves { get; }
        public LayoutConfig Layout { get; }

        /// <summary>The match's map: the hives, the spawn points and the nests, drawn from the seed.</summary>
        public ArenaLayout Map { get; }

        /// <summary>The Sting's asset, whose reach the aim marker uses.</summary>
        public StingSong Sting => (StingSong)_songs[StingIndex];

        /// <summary>The Tornado's asset.</summary>
        public TornadoSong Tornado => (TornadoSong)_songs[TornadoIndex];

        /// <summary>The Meteor's asset.</summary>
        public MeteorSong Meteor => (MeteorSong)_songs[MeteorIndex];

        /// <summary>The Hydra's asset.</summary>
        public HydraSong Hydra => (HydraSong)_songs[HydraIndex];

        /// <summary>The Scythe's asset.</summary>
        public ScytheSong Scythe => (ScytheSong)_songs[ScytheIndex];

        /// <summary>The Lance's asset.</summary>
        public LanceSong Lance => (LanceSong)_songs[LanceIndex];

        /// <summary>The Dome's asset: the shell around the core, and every hive inside it.</summary>
        public DomeSong Dome => (DomeSong)_songs[DomeIndex];

        /// <summary>The Gust's asset: the pulse that clears smoke and shoves beekeepers out.</summary>
        public GustSong Gust => (GustSong)_songs[GustIndex];

        /// <summary>The Nest's asset: the trap left on a lane.</summary>
        public NestSong Nest => (NestSong)_songs[NestIndex];

        /// <summary>The Honeycomb's asset: the shell on a hive.</summary>
        public HoneycombSong Honeycomb => (HoneycombSong)_songs[HoneycombIndex];

        /// <summary>Where the beekeepers appear, in the order the waves use them: they take turns, one beekeeper each.</summary>
        public IReadOnlyList<Vector3> SpawnPoints => Map.Spawns;

        public void Dispose()
        {
            foreach (ScriptableObject asset in _created)
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }

            _created.Clear();
        }

        private T Make<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _created.Add(asset);
            return asset;
        }
    }
}
