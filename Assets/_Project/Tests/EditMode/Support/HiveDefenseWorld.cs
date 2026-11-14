using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// A small match for tests: default configs, a short first farm phase (1 s unless a test says) and the waves, hives,
    /// nests and spawn points a test chooses. The core stands still at <see cref="Core"/> unless a test moves it. After each
    /// tick the world puts the core where the session says it is, as the View does, so it rides inside the giant and stays
    /// where the giant stood. The default nests (<see cref="DefaultNests"/>) stand away from where the other tests take the
    /// core, so none is collected unless a test goes to one.
    /// </summary>
    internal sealed class HiveDefenseWorld : IDisposable
    {
        public const float Step = 1f / 60f;
        public const int HoleCount = 4;
        public const float ShortFirstFarmSeconds = 1f;

        /// <summary>
        /// The eight nests, in the order they open: two about 12 m from the middle, behind it, then six 22 m out, one every
        /// 60 degrees. They keep the layout rules (<see cref="NestLayout"/>) for the default hives.
        /// </summary>
        public static readonly Vector3[] DefaultNests =
        {
            new Vector3(-9f, 0f, -8f),
            new Vector3(9f, 0f, -8f),
            new Vector3(0f, 0f, 22f),
            new Vector3(19f, 0f, 11f),
            new Vector3(19f, 0f, -11f),
            new Vector3(0f, 0f, -22f),
            new Vector3(-19f, 0f, -11f),
            new Vector3(-19f, 0f, 11f),
        };

        /// <summary>
        /// The song indices when the world is built with songs: Dome H K J, Colossus L J K J L and Sting J K H; with all songs,
        /// also Tornado J K K H, Meteor J K L K, Gust H L K and Hive Call L J L; with the new songs, all those and then Hydra
        /// J J L, Scythe K L H, Lance K L L H, Nest K L J H and Honeycomb H J L K. Without any, the world holds only the Sting
        /// (index 0), because the session's aim marker takes its reach from the starting aimed song.
        /// </summary>
        public const int DomeIndex = 0;
        public const int ColossusIndex = 1;
        public const int StingIndex = 2;
        public const int TornadoIndex = 3;
        public const int MeteorIndex = 4;
        public const int GustIndex = 5;
        public const int HiveCallIndex = 6;
        public const int HydraIndex = 7;
        public const int ScytheIndex = 8;
        public const int LanceIndex = 9;
        public const int NestIndex = 10;
        public const int HoneycombIndex = 11;

        private readonly List<ScriptableObject> _created = new List<ScriptableObject>();

        public HiveDefenseWorld(
            WaveEntry[] waves = null,
            Vector3[] hives = null,
            Vector3[] spawns = null,
            Vector3 core = default,
            Vector3[] nests = null,
            bool smokerForSecondKind = true,
            bool withSongs = false,
            bool withAllSongs = false,
            float firstFarmSeconds = ShortFirstFarmSeconds,
            bool withNewSongs = false)
        {
            Core = core;
            Swarm = Make<SwarmConfig>();
            Squads = Make<SquadConfig>();
            CoreMove = Make<CoreMoveConfig>();
            Fury = Make<FuryConfig>();
            Colossus = Make<ColossusConfig>();
            Hive = Make<HiveConfig>();
            Nests = Make<NestConfig>();
            SwarmLevel = Make<SwarmLevelConfig>();
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
            JsonUtility.FromJsonOverwrite(WaveJson(waves ?? new[] { new WaveEntry(1, 0) }, firstFarmSeconds), WaveConfig);

            Sting = Make<StingSong>();
            SongConfig = Make<SongConfig>();
            SwarmSongEffect[] songs = { Sting };
            if (withNewSongs)
            {
                songs = new SwarmSongEffect[]
                {
                    Make<DomeSong>(), Make<ColossusSong>(), Sting, Make<TornadoSong>(), Make<MeteorSong>(), Make<GustSong>(), Make<HiveCallSong>(),
                    Make<HydraSong>(), Make<ScytheSong>(), Make<LanceSong>(), Make<NestSong>(), Make<HoneycombSong>(),
                };
            }
            else if (withAllSongs)
            {
                songs = new SwarmSongEffect[]
                {
                    Make<DomeSong>(), Make<ColossusSong>(), Sting, Make<TornadoSong>(), Make<MeteorSong>(), Make<GustSong>(), Make<HiveCallSong>(),
                };
            }
            else if (withSongs)
            {
                songs = new SwarmSongEffect[] { Make<DomeSong>(), Make<ColossusSong>(), Sting };
            }

            Songs = songs;
            NestPositions = nests ?? DefaultNests;
            var rules = new HiveDefenseRules(
                Swarm, Squads, CoreMove, Fury, Colossus, Hive, Nests, SwarmLevel, Smoke, WaveConfig, Stick, Smoker, Aim,
                SongConfig, songs, HoleCount);
            Session = new HiveDefenseSession(
                rules,
                hives ?? new[] { new Vector3(-40f, 0f, 40f), new Vector3(40f, 0f, 40f) },
                NestPositions,
                spawns ?? new[] { new Vector3(30f, 0f, -30f), new Vector3(-30f, 0f, -30f) },
                Core,
                new FixedRandomSource());
        }

        public SwarmConfig Swarm { get; }
        public SquadConfig Squads { get; }
        public CoreMoveConfig CoreMove { get; }
        public FuryConfig Fury { get; }
        public ColossusConfig Colossus { get; }
        public HiveConfig Hive { get; }
        public NestConfig Nests { get; }
        public SwarmLevelConfig SwarmLevel { get; }
        public SmokeConfig Smoke { get; }
        public BeekeeperConfig Stick { get; }
        public BeekeeperConfig Smoker { get; }
        public AimConfig Aim { get; }
        public SongConfig SongConfig { get; }
        public StingSong Sting { get; }

        /// <summary>The songs the session plays, in caster order.</summary>
        public IReadOnlyList<SwarmSongEffect> Songs { get; }

        /// <summary>Where the nests stand, in the order they open.</summary>
        public IReadOnlyList<Vector3> NestPositions { get; }

        public WaveConfig WaveConfig { get; }
        public HiveDefenseSession Session { get; }
        public Vector3 Core { get; set; }

        /// <summary>
        /// A frame with the core where it is, the mouse idle unless a test says, nothing pressed or held, and no camera
        /// direction unless a test gives one, so the giant steers on the world's axes.
        /// </summary>
        public HiveDefenseFrame Frame(
            Vector2 move = default,
            int holes = 0,
            bool rightHand = false,
            bool leftHand = false,
            Vector3? aim = null,
            bool isMouseLive = false,
            bool give = false,
            bool callWave = false,
            Vector3 cameraForward = default)
        {
            return new HiveDefenseFrame(Core, aim ?? Core, move, holes, rightHand, leftHand, isMouseLive, give, callWave, cameraForward);
        }

        /// <summary>Ticks with idle frames for the given seconds.</summary>
        public void Run(float seconds)
        {
            RunWith(seconds, Frame());
        }

        /// <summary>Ticks with the frame's input for the given seconds, each tick from where the core is then.</summary>
        public void RunWith(float seconds, HiveDefenseFrame frame)
        {
            for (float time = 0f; time < seconds; time += Step)
            {
                Tick(frame);
            }
        }

        /// <summary>Moves the core to <paramref name="position"/> in one tick, as the View would, so it faces the way it moved.</summary>
        public void MoveCoreTo(Vector3 position)
        {
            Core = position;
            Tick(Frame());
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

                Tick(Frame());
            }

            return condition();
        }

        /// <summary>
        /// Flies the core to nest <paramref name="index"/> and plays its combo (plan section U), one note a tick. Returns
        /// whether it was collected.
        /// </summary>
        public bool CollectNest(int index)
        {
            MoveCoreTo(NestPositions[index]);
            Play(Session.Nests[index].Combo);
            return Session.Nests[index].IsCollected;
        }

        /// <summary>Kills every living beekeeper on the field.</summary>
        public void KillAllBeekeepers()
        {
            for (int index = Session.ActiveBeekeeperCount - 1; index >= 0; index--)
            {
                Session.ActiveBeekeeper(index).TakeDamage(10000f);
            }
        }

        // One tick from where the core is now, then the core goes where the session says it is, as the View puts it.
        private void Tick(HiveDefenseFrame frame)
        {
            var now = new HiveDefenseFrame(
                Core,
                frame.AimPoint,
                frame.Move,
                frame.PressedHoleMask,
                frame.RightHand,
                frame.LeftHand,
                frame.IsMouseLive,
                frame.Give,
                frame.CallWave,
                frame.CameraForward);
            Session.Tick(now, Step);
            Core = Session.CorePosition;
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

        private static string WaveJson(WaveEntry[] waves, float firstFarmSeconds)
        {
            var json = new StringBuilder("{\"<FirstFarmSeconds>k__BackingField\":")
                .Append(firstFarmSeconds.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"_waves\":[");
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
