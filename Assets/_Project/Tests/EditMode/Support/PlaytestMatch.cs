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
    /// order, and the scene's hives, spawn points and nest trees, with the core starting in the middle. Only the seed of the
    /// session's random source changes between runs.
    /// </summary>
    internal sealed class PlaytestMatch : IDisposable
    {
        /// <summary>The song indices, in the scene builder's order: the spec's table.</summary>
        public const int DomeIndex = 0;
        public const int StingIndex = 1;
        public const int TornadoIndex = 2;
        public const int GustIndex = 3;
        public const int MeteorIndex = 4;
        public const int HiveCallIndex = 5;
        public const int ColossusIndex = 6;

        /// <summary>The flute's holes: H J K L.</summary>
        public const int HoleCount = 4;

        // HiveDefenseSceneBuilder.LoadOrCreateSmoker: the smoker's numbers where they differ from the stick's defaults.
        private const string SmokerJson =
            "{\"<Kind>k__BackingField\":1,\"<Health>k__BackingField\":40,\"<Speed>k__BackingField\":2.2,"
            + "\"<NoticeRange>k__BackingField\":7,\"<GiveUpRange>k__BackingField\":14,\"<AttackRange>k__BackingField\":14,"
            + "\"<WindUpSeconds>k__BackingField\":0.8,\"<CooldownSeconds>k__BackingField\":4.5,"
            + "\"<HiveDamage>k__BackingField\":0,\"<WaspsKnockedOff>k__BackingField\":0}";

        // HiveDefenseSceneBuilder: the hives 25 m apart across the middle, the spawn points 32 m out (in its child order:
        // north, south, east, west), and the nest trees its seeded layout picks, in the order they open, as the generated
        // scene holds them. A change to the builder's layout belongs here too.
        private static readonly Vector3[] HivePositions =
        {
            new Vector3(-12.5f, 0f, 0f),
            new Vector3(12.5f, 0f, 0f),
        };

        private static readonly Vector3[] SpawnPositions =
        {
            new Vector3(0f, 0f, 32f),
            new Vector3(0f, 0f, -32f),
            new Vector3(32f, 0f, 0f),
            new Vector3(-32f, 0f, 0f),
        };

        private static readonly Vector3[] NestPositions =
        {
            new Vector3(-4.2303505f, 0f, 9.91818f),
            new Vector3(6.0840244f, 0f, 12.559615f),
            new Vector3(-1.1644772f, 0f, -17.937988f),
            new Vector3(-0.48996973f, 0f, 18.718933f),
            new Vector3(16.543379f, 0f, 13.702695f),
            new Vector3(-18.956007f, 0f, -12.818432f),
            new Vector3(-19.761074f, 0f, 11.611771f),
            new Vector3(23.381496f, 0f, -8.659761f),
        };

        private readonly List<ScriptableObject> _created = new List<ScriptableObject>();
        private readonly SwarmSongEffect[] _songs;

        /// <summary>Builds the match. <paramref name="seed"/> seeds the session's random source.</summary>
        public PlaytestMatch(uint seed)
        {
            CoreMove = Make<CoreMoveConfig>();
            Colossus = Make<ColossusConfig>();
            Hive = Make<HiveConfig>();
            Nests = Make<NestConfig>();
            Waves = Make<WaveConfig>();
            var smoker = Make<BeekeeperConfig>();
            JsonUtility.FromJsonOverwrite(SmokerJson, smoker);

            _songs = new SwarmSongEffect[]
            {
                Make<DomeSong>(), Make<StingSong>(), Make<TornadoSong>(), Make<GustSong>(), Make<MeteorSong>(),
                Make<HiveCallSong>(), Make<ColossusSong>(),
            };

            var rules = new HiveDefenseRules(
                Make<SwarmConfig>(), Make<SquadConfig>(), CoreMove, Make<FuryConfig>(), Colossus, Hive, Nests, Make<SwarmLevelConfig>(),
                Make<SmokeConfig>(), Waves, Make<BeekeeperConfig>(), smoker, Make<AimConfig>(), Make<SongConfig>(), _songs,
                HoleCount);
            Session = new HiveDefenseSession(rules, HivePositions, NestPositions, SpawnPositions, Vector3.zero, new Mulberry32Random(seed));
        }

        /// <summary>The match.</summary>
        public HiveDefenseSession Session { get; }

        public CoreMoveConfig CoreMove { get; }
        public ColossusConfig Colossus { get; }
        public HiveConfig Hive { get; }
        public NestConfig Nests { get; }
        public WaveConfig Waves { get; }

        /// <summary>The Sting's asset, whose reach the aim marker uses.</summary>
        public StingSong Sting => (StingSong)_songs[StingIndex];

        /// <summary>The Tornado's asset.</summary>
        public TornadoSong Tornado => (TornadoSong)_songs[TornadoIndex];

        /// <summary>The Meteor's asset.</summary>
        public MeteorSong Meteor => (MeteorSong)_songs[MeteorIndex];

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
