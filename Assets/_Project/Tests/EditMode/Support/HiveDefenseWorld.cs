using System;
using System.Collections.Generic;
using System.Text;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// A small match for tests: default configs, a short first-wave delay and the waves, hives and spawn points a test chooses.
    /// The Piper stands still at <see cref="Piper"/>, facing <see cref="Facing"/>, unless a test changes them.
    /// </summary>
    internal sealed class HiveDefenseWorld : IDisposable
    {
        public const float Step = 1f / 60f;
        public const int HoleCount = 4;

        /// <summary>
        /// The song indices when the world is built with songs: Dome H K J, Colossus L J K J L and Sting J K H; with all songs,
        /// also Tornado J K K H, Meteor J K L K, Gust H L K and Hive Call L J L.
        /// </summary>
        public const int DomeIndex = 0;
        public const int ColossusIndex = 1;
        public const int StingIndex = 2;
        public const int TornadoIndex = 3;
        public const int MeteorIndex = 4;
        public const int GustIndex = 5;
        public const int HiveCallIndex = 6;

        private readonly List<ScriptableObject> _created = new List<ScriptableObject>();

        public HiveDefenseWorld(
            WaveEntry[] waves = null,
            Vector3[] hives = null,
            Vector3[] spawns = null,
            Vector3 piper = default,
            bool smokerForSecondKind = true,
            bool withSongs = false,
            bool withAllSongs = false)
        {
            Piper = piper;
            Swarm = Make<SwarmConfig>();
            Squads = Make<SquadConfig>();
            PiperMove = Make<PiperMoveConfig>();
            Fury = Make<FuryConfig>();
            Colossus = Make<ColossusConfig>();
            Hive = Make<HiveConfig>();
            Smoke = Make<SmokeConfig>();
            Stick = Make<BeekeeperConfig>();
            Smoker = Make<BeekeeperConfig>();
            Aim = Make<AimConfig>();
            JsonUtility.FromJsonOverwrite(
                "{\"<Kind>k__BackingField\":1,\"<Health>k__BackingField\":40,\"<Speed>k__BackingField\":2.2,"
                + "\"<AttackRange>k__BackingField\":14,\"<WindUpSeconds>k__BackingField\":0.8,"
                + "\"<CooldownSeconds>k__BackingField\":4.5,\"<HiveDamage>k__BackingField\":0,\"<WaspsKnockedOff>k__BackingField\":0}",
                Smoker);
            WaveConfig = Make<WaveConfig>();
            JsonUtility.FromJsonOverwrite(WaveJson(waves ?? new[] { new WaveEntry(1, 0) }), WaveConfig);

            SwarmSongEffect[] songs = Array.Empty<SwarmSongEffect>();
            if (withAllSongs)
            {
                songs = new SwarmSongEffect[]
                {
                    Make<DomeSong>(), Make<ColossusSong>(), Make<StingSong>(), Make<TornadoSong>(), Make<MeteorSong>(), Make<GustSong>(), Make<HiveCallSong>(),
                };
            }
            else if (withSongs)
            {
                songs = new SwarmSongEffect[] { Make<DomeSong>(), Make<ColossusSong>(), Make<StingSong>() };
            }

            var rules = new HiveDefenseRules(
                Swarm, Squads, PiperMove, Fury, Colossus, Hive, Smoke, WaveConfig, Stick, Smoker, Aim,
                Make<SongConfig>(), songs, HoleCount);
            Session = new HiveDefenseSession(
                rules,
                hives ?? new[] { new Vector3(-40f, 0f, 40f), new Vector3(40f, 0f, 40f) },
                spawns ?? new[] { new Vector3(30f, 0f, -30f), new Vector3(-30f, 0f, -30f) },
                Piper,
                new FixedRandomSource());
        }

        public SwarmConfig Swarm { get; }
        public SquadConfig Squads { get; }
        public PiperMoveConfig PiperMove { get; }
        public FuryConfig Fury { get; }
        public ColossusConfig Colossus { get; }
        public HiveConfig Hive { get; }
        public SmokeConfig Smoke { get; }
        public BeekeeperConfig Stick { get; }
        public BeekeeperConfig Smoker { get; }
        public AimConfig Aim { get; }
        public WaveConfig WaveConfig { get; }
        public HiveDefenseSession Session { get; }
        public Vector3 Piper { get; set; }
        public Vector3 Facing { get; set; } = Vector3.forward;

        /// <summary>A frame with the Piper where it stands, facing where it faces, the mouse idle unless a test says, and nothing pressed.</summary>
        public HiveDefenseFrame Frame(
            Vector2 move = default, int holes = 0, bool punch = false, bool kick = false, Vector3? aim = null, bool isMouseLive = false)
        {
            return new HiveDefenseFrame(Piper, Facing, aim ?? Piper, move, holes, punch, kick, isMouseLive);
        }

        /// <summary>Ticks with idle frames for the given seconds.</summary>
        public void Run(float seconds)
        {
            RunWith(seconds, Frame());
        }

        public void RunWith(float seconds, HiveDefenseFrame frame)
        {
            for (float time = 0f; time < seconds; time += Step)
            {
                Session.Tick(frame, Step);
            }
        }

        /// <summary>Plays the holes one a tick, as the player would press them.</summary>
        public void Play(IReadOnlyList<int> holes)
        {
            for (int index = 0; index < holes.Count; index++)
            {
                RunWith(Step, Frame(holes: HiveDefenseFrame.HoleBit(holes[index])));
            }
        }

        /// <summary>Ticks until the condition holds or the time runs out. Returns whether it held.</summary>
        public bool RunUntil(Func<bool> condition, float maxSeconds)
        {
            for (float time = 0f; time < maxSeconds; time += Step)
            {
                if (condition())
                {
                    return true;
                }

                Session.Tick(Frame(), Step);
            }

            return condition();
        }

        /// <summary>Kills every living beekeeper on the field.</summary>
        public void KillAllBeekeepers()
        {
            for (int index = Session.ActiveBeekeeperCount - 1; index >= 0; index--)
            {
                Session.ActiveBeekeeper(index).TakeDamage(10000f);
            }
        }

        public void Dispose()
        {
            foreach (ScriptableObject asset in _created)
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        private T Make<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _created.Add(asset);
            return asset;
        }

        private static string WaveJson(WaveEntry[] waves)
        {
            var json = new StringBuilder("{\"<FirstWaveDelaySeconds>k__BackingField\":1,\"_waves\":[");
            for (int index = 0; index < waves.Length; index++)
            {
                if (index > 0)
                {
                    json.Append(',');
                }

                json.Append("{\"_sticks\":").Append(waves[index].Sticks).Append(",\"_smokers\":").Append(waves[index].Smokers).Append('}');
            }

            return json.Append("]}").ToString();
        }
    }
}
