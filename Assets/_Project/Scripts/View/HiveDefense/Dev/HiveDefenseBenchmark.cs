using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace DigitalStory.View
{
    /// <summary>
    /// Development tool, not game code: the F-010 frame-time benchmark. It does nothing, and does not even exist in the
    /// scene, unless the player is started with "-f010Benchmark &lt;output folder&gt;" (see <see cref="BenchmarkOptions"/>).
    /// With the flag it drives the real hive defense scene through a fixed script, through the entry points a player uses
    /// (the root's notes and wave call, the session's public rules and the core's motor), records every frame to a CSV
    /// and quits. Kept as the regression guard for performance fixes.
    /// </summary>
    /// <remarks>
    /// Every frame advances the game by exactly 1/60 s (Time.captureDeltaTime), and the match's random source is seeded
    /// by the scene, so two runs play the same match whatever their frame rate. The wall clock between frames is measured
    /// separately. Spawning is compressed through the session's own wave director, so a wave of 28 does not take 81 s to
    /// arrive, and the swarm is held at the run's size with the swarm's public Gain and Lose. So that every window measures
    /// what its name says, nothing of one window may linger into the next: the Nest and the Honeycomb stand in the Post slot
    /// long after their windows, so the script loads the scene afresh after each (a fresh match, round 1, nothing standing),
    /// and it does the same when a farm phase is too short for what comes next and no round is left to clear. The hives are
    /// healed whenever they are hurt. The giant's moves are measured in the chase camera, each in a window of its own.
    /// </remarks>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder)] // After every game script, so its notes and moves are read on the next tick.
    [AddComponentMenu("")]
    public sealed class HiveDefenseBenchmark : MonoBehaviour
    {
        private const int ExecutionOrder = 32000;
        private const float FrameSeconds = 1f / 60f;
        private const int Seed = 1010;
        private const int BootFrames = 5;
        private const float PrepSeconds = 5f;
        private const float SettleSeconds = 1f;
        private const float ShortSettleSeconds = 0.5f;
        private const float MeasureSeconds = 4f;
        private const float StationSeconds = 3f;
        private const float LastWaveMeasureSeconds = 5f;
        private const float WaveSettleSeconds = 9f;
        private const float AfterClearSeconds = 2f;
        private const float AfterMoveSeconds = 2f;
        private const float AfterHalfSeconds = 3f;
        private const float MostWaitSeconds = 20f;
        private const float WalkLegSeconds = 1.5f;
        private const float BesideNestMetres = 1.5f;
        private const float BesideHiveMetres = 3f;
        private const float FullFuryDamage = 1000f;
        private const int MostFuryAdds = 100;
        private const float HiveCallRoomShare = 0.1f;
        private const float KillDamage = 1000000f;
        private const float SpawnStepSeconds = 0.5f;
        private const int MostSpawnSteps = 2000;
        private const int WaveOfEight = 8;
        private const int WaveOfEightRound = 4;
        private const float SongPairSeconds = 11f;
        private const float CooldownInWindowSeconds = 5f;
        private const float ColossusSettleSeconds = 2f;
        private const float GiantSwoopSeconds = 1.5f;
        private const float GiantWindowSeconds = 3f;
        private const float MostFormSeconds = 2f;
        private const float GiantWalkLegSeconds = 0.75f;
        private const float GiantPunchEverySeconds = 0.3f;
        private const float GiantKickEverySeconds = 1f;
        private const float GiantSpinEverySeconds = 1.5f;
        private const float GiantOrbitDegreesPerFrame = 1.2f;
        private const float StationLoopSeconds = 16f;
        private const float DomeStingSeconds = 14f;
        private const float NestSeconds = 10f;
        private const float NestComboReadSeconds = 0.6f;
        private const float NestComboNoteSeconds = 0.3f;
        private const double MostRunSeconds = 900.0;
        private const int TimeDecimals = 3;
        private const int SecondsDecimals = 4;

        private const string Boot = "boot";
        private const string Prep = "prep";
        private const string Empty = "empty";
        private const string Walking = "walk";
        private const string DomeSting = "domesting";
        private const string Stand = "stand";
        private const string NestName = "nest";
        private const string WaveEight = "wave8";
        private const string WaveLast = "wave28";
        private const string ClearingRound = "clear_round";
        private const string Reloading = "fresh_match";
        private const string GiantForming = "giant_form";
        private const string Ending = "end";

        // The giant's keys while it stands (plan section I). Holes count from 0 as H J K L.
        private const int RoarHole = 0;
        private const int PunchHole = 1;
        private const int KickHole = 2;
        private const int SlamHole = 3;

        // Phases as written to the CSV, indexed by Phase.
        private static readonly string[] PhaseNames = { "boot", "prep", "setup", "settle", "measure", "wait", "probe" };

        // Switches as written to the CSV, indexed by BenchmarkSwitch.
        private static readonly string[] SwitchNames =
        {
            "none", "noglow", "noswarmlights", "nohivenestlights", "noshadows", "nowaspdraw", "nocompute", "noground",
            "noscenery", "noanimators", "halfwasps", "nohud", "noattacklight", "nocastflash", "nocastsound", "nocamerakick",
            "nodamagenumbers", "nocursorlock",
        };

        // The giant's moves, each measured in a window of its own in the chase camera, over two giants: one cannot stand long
        // enough for all nine.
        private static readonly GiantMove[] FirstGiantMoves =
        {
            GiantMove.Stand, GiantMove.Walk, GiantMove.Orbit, GiantMove.Punch, GiantMove.Kick,
        };

        private static readonly GiantMove[] SecondGiantMoves =
        {
            GiantMove.Slam, GiantMove.Roar, GiantMove.Spin, GiantMove.Leap,
        };

        // Window names, indexed by GiantMove.
        private static readonly string[] GiantWindowNames =
        {
            "giant_stand", "giant_walk", "giant_orbit", "giant_punch", "giant_kick", "giant_slam", "giant_roar", "giant_spin",
            "giant_leap",
        };

        // A square walk relative to the chase camera (y is the camera's way), back to where it started.
        private static readonly Vector2[] GiantWalkLegs = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };

        // All twelve songs in the order the baseline casts them, with the names their windows carry. The first seven keep
        // the first baseline's order, so its windows compare one to one; the Colossus stays last, after full Fury.
        private static readonly Type[] SongOrder =
        {
            typeof(StingSong), typeof(DomeSong), typeof(TornadoSong), typeof(MeteorSong), typeof(GustSong), typeof(HiveCallSong),
            typeof(HydraSong), typeof(ScytheSong), typeof(LanceSong), typeof(NestSong), typeof(HoneycombSong), typeof(ColossusSong),
        };

        private static readonly string[] SongNames =
        {
            "sting", "dome", "tornado", "meteor", "gust", "hivecall", "hydra", "scythe", "lance", "nest", "honeycomb", "colossus",
        };

        private static readonly string[] FirstCastNames = WindowNames("_1");
        private static readonly string[] SecondCastNames = WindowNames("_2");

        // The attacks cast into a wave, and the names of their windows: the first three as before, then the three new attacks.
        private static readonly Type[] WaveAttacks =
        {
            typeof(StingSong), typeof(TornadoSong), typeof(MeteorSong), typeof(HydraSong), typeof(ScytheSong), typeof(LanceSong),
        };

        private static readonly string[] WaveEightAttackNames =
        {
            "wave8_sting", "wave8_tornado", "wave8_meteor", "wave8_hydra", "wave8_scythe", "wave8_lance",
        };

        private static readonly string[] WaveLastAttackNames =
        {
            "wave28_sting", "wave28_tornado", "wave28_meteor", "wave28_hydra", "wave28_scythe", "wave28_lance",
        };

        // A square walk, back to where it started.
        private static readonly Vector3[] WalkLegs = { Vector3.right, Vector3.forward, Vector3.left, Vector3.back };

        private static readonly BenchmarkSwitch[] SongStationSwitches =
        {
            BenchmarkSwitch.None, BenchmarkSwitch.NoGlow, BenchmarkSwitch.NoSwarmLights, BenchmarkSwitch.NoHiveNestLights,
            BenchmarkSwitch.NoShadows, BenchmarkSwitch.NoWaspDraw, BenchmarkSwitch.NoCompute, BenchmarkSwitch.NoGround,
            BenchmarkSwitch.NoScenery, BenchmarkSwitch.HalfWasps, BenchmarkSwitch.None,
        };

        private static readonly BenchmarkSwitch[] WaveStationSwitches =
        {
            BenchmarkSwitch.None, BenchmarkSwitch.NoGlow, BenchmarkSwitch.NoSwarmLights, BenchmarkSwitch.NoHiveNestLights,
            BenchmarkSwitch.NoShadows, BenchmarkSwitch.NoWaspDraw, BenchmarkSwitch.NoCompute, BenchmarkSwitch.NoGround,
            BenchmarkSwitch.NoScenery, BenchmarkSwitch.NoAnimators, BenchmarkSwitch.HalfWasps, BenchmarkSwitch.None,
            BenchmarkSwitch.NoHud,
        };

        private static readonly string[] FrameColumns =
        {
            "frame", "real_s", "dt_ms", "scenario", "phase", "switch", "cast", "hits", "wasps", "target", "beekeepers", "clouds", "round",
            "nests_open", "form", "post", "guard", "strike", "l_on", "l_forcepixel", "l_swarm", "l_hive", "l_nest", "screen_w",
            "screen_h", "focused", "ftm_cpu_ms", "ftm_main_ms", "ftm_render_ms", "ftm_gpu_ms",
        };

        private static readonly string[] OverdrawColumns =
        {
            "frame", "scenario", "switch", "wasps", "width", "height", "covered_px", "covered_share", "fragments",
            "fragments_per_px", "mean_on_covered", "median", "p95", "p99", "max",
        };

        private readonly Stack<IEnumerator> _script = new Stack<IEnumerator>();
        private readonly List<string> _events = new List<string>();
        private readonly List<BeekeeperBrain> _alive = new List<BeekeeperBrain>();

        private BenchmarkOptions _options;
        private HiveDefenseRoot _root;
        private HiveDefenseSession _session;
        private FollowCamera _follow;
        private SwarmCoreMotor _motor;
        private SwarmGpuView _swarmView;
        private Camera _camera;
        private BenchmarkSwitchBoard _switches;
        private BenchmarkRecorders _recorders;
        private BenchmarkFrameLog _log;
        private BenchmarkFrameLog _overdrawLog;
        private GlowOverdrawProbe _overdraw;

        private string _scenario = Boot;
        private Phase _phase = Phase.Boot;
        private int _castThisFrame = -1;
        private int _hitsThisFrame;
        private int _target;
        private bool _isHoldingSwarm;
        private bool _isHealingHives = true;
        private int _beekeeperCap = -1;
        private bool _isWalking;
        private Vector3 _walkDirection;
        private Vector3 _start;
        private long _lastTimestamp;
        private long _startTimestamp;
        private int _frame;
        private bool _isFinished;

        private enum GiantMove
        {
            Stand = 0,
            Walk = 1,
            Orbit = 2,
            Punch = 3,
            Kick = 4,
            Slam = 5,
            Roar = 6,
            Spin = 7,
            Leap = 8,
        }

        private enum Phase
        {
            Boot = 0,
            Prep = 1,
            Setup = 2,
            Settle = 3,
            Measure = 4,
            Wait = 5,
            Probe = 6,
        }

        private void Update()
        {
            if (_isFinished)
            {
                return;
            }

            try
            {
                if (_session != null)
                {
                    KeepUp();
                }

                StepScript();

                // After the root's Update, where a cast sets off its flash and sound, and before any LateUpdate shows them.
                _switches?.ForceAfterUpdate();
                if (Elapsed() > MostRunSeconds)
                {
                    throw new TimeoutException($"The benchmark ran longer than {MostRunSeconds} s.");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _events.Add($"{_frame}: FAILED {exception.GetType().Name}: {exception.Message}");
                Finish();
            }
        }

        private void LateUpdate()
        {
            if (_isFinished)
            {
                return;
            }

            _switches?.ForceEachFrame();
            Record();
        }

        private void OnDestroy()
        {
            if (!_isFinished)
            {
                Finish();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfAsked()
        {
            if (!BenchmarkOptions.TryRead(out BenchmarkOptions options))
            {
                return;
            }

            UnityEngine.Random.InitState(Seed);
            Application.runInBackground = true; // A window that loses focus must not pause the run.
            Time.captureDeltaTime = FrameSeconds;
            if (!options.KeepsGameSettings)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
            }

            var host = new GameObject("F010 Benchmark");
            DontDestroyOnLoad(host);
            host.AddComponent<HiveDefenseBenchmark>().Begin(options);
        }

        private void Begin(BenchmarkOptions options)
        {
            _options = options;
            _target = options.SwarmSize;
            _startTimestamp = Stopwatch.GetTimestamp();
            _script.Push(Run());
        }

        // ------------------------------------------------------------------ script runner

        // Runs the script until it waits a frame. A nested IEnumerator runs to its end before its caller goes on.
        private void StepScript()
        {
            while (_script.Count > 0)
            {
                IEnumerator top = _script.Peek();
                if (!top.MoveNext())
                {
                    _script.Pop();
                    continue;
                }

                if (top.Current is IEnumerator nested)
                {
                    _script.Push(nested);
                    continue;
                }

                return;
            }

            Finish();
        }

        private IEnumerator Run()
        {
            while (!TryFindScene())
            {
                yield return null;
            }

            for (int frame = 0; frame < BootFrames; frame++)
            {
                yield return null;
            }

            Open();
            Bind();
            WriteNotes(_options.PathFor("notes-start.txt"));
            yield return Hold(Prep, Phase.Prep, PrepSeconds);
            if (_options.IsFirstCastOnly)
            {
                yield return RunFirstCast();
            }
            else if (_options.IsSubtraction)
            {
                yield return RunSubtraction();
            }
            else
            {
                yield return RunBaseline();
            }

            _scenario = Ending;
        }

        // Connects the run to the scene's match: the switches, the overdraw probe, the session's events, the swarm's size
        // and the held switch. Called at the start and again after every fresh match.
        private void Bind()
        {
            _switches = new BenchmarkSwitchBoard(_root, _swarmView);
            if (_options.ProbesOverdraw)
            {
                _overdraw?.Dispose();
                _overdraw = new GlowOverdrawProbe(_swarmView, _camera);
            }

            _session.SongCastNow += HandleSongCast;
            _session.SongLanded += HandleSongLanded;
            _session.Caster.SongNotReady += HandleSongNotReady;
            _session.BeekeeperSpawned += HandleBeekeeperSpawned;
            _session.BeekeeperDied += HandleBeekeeperDied;
            _alive.Clear();
            _start = _motor.Position;
            _isHoldingSwarm = true;
            int grow = _target - _session.Swarm.Capacity;
            if (grow > 0)
            {
                _session.Swarm.GrowCapacity(grow);
            }

            HoldSwitch();
        }

        private void Unbind()
        {
            if (_session == null)
            {
                return;
            }

            _session.SongCastNow -= HandleSongCast;
            _session.SongLanded -= HandleSongLanded;
            _session.Caster.SongNotReady -= HandleSongNotReady;
            _session.BeekeeperSpawned -= HandleBeekeeperSpawned;
            _session.BeekeeperDied -= HandleBeekeeperDied;
            _session = null;

            // The board holds the old scene's lights and views; a new one is made for the next match.
            _switches = null;
        }

        // Loads the scene again: a fresh match in round 1's farm phase, full hives, nothing standing in any slot. The frames
        // of the load are not written; the next row's time includes them, in the setup phase.
        private IEnumerator FreshMatch()
        {
            string scenario = _scenario;
            _scenario = Reloading;
            _phase = Phase.Setup;
            _isWalking = false;
            _beekeeperCap = -1;
            _root.HoldMove(Vector2.zero);
            HiveDefenseRoot old = _root;
            Unbind();
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex < 0)
            {
                throw new InvalidOperationException($"Scene '{scene.name}' is not in the build settings, so it cannot be loaded afresh.");
            }

            SceneManager.LoadScene(scene.buildIndex);
            yield return null;
            while (old != null || !TryFindScene())
            {
                yield return null;
            }

            for (int frame = 0; frame < BootFrames; frame++)
            {
                yield return null;
            }

            Bind();

            // The old match is garbage now. Collected here, outside any window, it never lands in one as a long frame.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            _events.Add($"{_frame}: fresh match for {scenario}");
            yield return Hold(scenario, Phase.Setup, AfterClearSeconds);
        }

        // ------------------------------------------------------------------ first-cast script

        // Only the process's first two Sting casts, for runs that hold one first-cast switch each, and then the process's
        // first giant, whose swoop into the chase view is the first look at the horizon and the sky.
        private IEnumerator RunFirstCast()
        {
            int sting = SongIndex(typeof(StingSong));
            yield return CastPair(sting, FirstCastNames[0], SecondCastNames[0], false);
            int giant = SongIndex(typeof(ColossusSong));
            yield return WaitReady(giant, -1);
            yield return Hold(FirstCastNames[SongOrder.Length - 1], Phase.Settle, SettleSeconds);
            yield return CastAndMeasure(FirstCastNames[SongOrder.Length - 1], giant, -1, MeasureSeconds);
        }

        // Applies the switch named by -f010BenchOff for the whole baseline script, so a fix can be checked against it.
        private void HoldSwitch()
        {
            string name = _options.HeldSwitchName;
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            int index = Array.IndexOf(SwitchNames, name.ToLowerInvariant());
            if (index <= 0 || _options.IsSubtraction)
            {
                throw new ArgumentException($"-f010BenchOff takes one of {string.Join(",", SwitchNames, 1, SwitchNames.Length - 1)}, and not with -f010BenchSubtract.");
            }

            _switches.Apply((BenchmarkSwitch)index);
        }

        // ------------------------------------------------------------------ baseline script

        private IEnumerator RunBaseline()
        {
            yield return Hold(Empty, Phase.Settle, SettleSeconds);
            yield return Measure(Empty, MeasureSeconds);
            yield return Walk();

            for (int order = 0; order < SongOrder.Length; order++)
            {
                int song = SongIndex(SongOrder[order]);
                SwarmSongEffect effect = _session.Caster[song];
                float needed = SongPairSeconds + Mathf.Max(0f, effect.Song.CooldownSeconds - CooldownInWindowSeconds);
                if (effect is ColossusSong giant)
                {
                    // The second cast waits for the first giant to end.
                    needed += giant.Seconds + ColossusSettleSeconds;
                }

                bool isTower = effect is NestSong || effect is HoneycombSong;
                yield return EnsureFarm(needed, WaveOfEightRound - 1);
                yield return CastPair(song, FirstCastNames[order], SecondCastNames[order], isTower);
            }

            yield return GiantWindows();
            yield return CastDomeSting(DomeSting, MeasureSeconds);
            yield return CollectNest();
            yield return WaveOfEightBeekeepers();
            yield return AttackWave(WaveOfEight, WaveEightAttackNames);
            yield return LastWave();
            yield return Measure(WaveLast, LastWaveMeasureSeconds);
            yield return AttackWave(int.MaxValue, WaveLastAttackNames);
        }

        // Stands the core beside the first hive, where the beekeepers attack, and casts each attack into them, so the
        // attacks land: hits, knockbacks and defeats. Songs cost wasps here, as in any wave; the swarm is topped up.
        private IEnumerator AttackWave(int alive, string[] names)
        {
            _motor.PlaceAt(_session.Hives[0].Position + (Vector3.back * BesideHiveMetres), true);
            yield return Hold(names[0], Phase.Setup, AfterMoveSeconds);
            for (int attack = 0; attack < WaveAttacks.Length; attack++)
            {
                int song = SongIndex(WaveAttacks[attack]);
                yield return WaitReady(song, -1);
                if (alive != int.MaxValue)
                {
                    _beekeeperCap = alive;
                    RefillWave(alive);
                }

                yield return Hold(names[attack], Phase.Settle, ShortSettleSeconds);
                yield return CastAndMeasure(names[attack], song, -1, MeasureSeconds);
            }

            _beekeeperCap = -1;
        }

        // A tower (the Nest, the Honeycomb) stands in the Post slot long after its window, so a fresh match follows each of
        // its windows, and the next window starts with nothing standing.
        private IEnumerator CastPair(int song, string first, string second, bool isTower)
        {
            yield return WaitReady(song, -1);
            yield return Hold(first, Phase.Settle, SettleSeconds);
            yield return CastAndMeasure(first, song, -1, MeasureSeconds);
            if (isTower)
            {
                yield return FreshMatch();
            }

            yield return WaitReady(song, -1);
            yield return Hold(second, Phase.Settle, SettleSeconds);
            yield return CastAndMeasure(second, song, -1, MeasureSeconds);
            if (isTower)
            {
                yield return FreshMatch();
            }
        }

        // ------------------------------------------------------------------ the giant's moves (plan section I)

        private IEnumerator GiantWindows()
        {
            int giant = SongIndex(typeof(ColossusSong));
            yield return GiantRun(giant, FirstGiantMoves);
            yield return GiantRun(giant, SecondGiantMoves);
        }

        // A fresh match's first farm phase holds a whole giant. It forms through its keys at full Fury, the camera swoops
        // into the chase view, and then each move gets its window.
        private IEnumerator GiantRun(int song, GiantMove[] moves)
        {
            _scenario = GiantForming;
            yield return FreshMatch();
            yield return WaitReady(song, -1);
            Prepare(song);
            _scenario = GiantForming;
            _phase = Phase.Setup;
            foreach (int hole in _session.Caster[song].Song.Keys)
            {
                _root.PressHole(hole);
                yield return null;
            }

            int frames = FramesFor(MostFormSeconds);
            for (int frame = 0; frame < frames && !_session.IsColossusActive; frame++)
            {
                yield return null;
            }

            if (!_session.IsColossusActive)
            {
                _events.Add($"{_frame}: FAILED the giant did not form for {GiantWindowNames[(int)moves[0]]}");
                yield break;
            }

            yield return Hold(GiantForming, Phase.Settle, GiantSwoopSeconds);
            foreach (GiantMove move in moves)
            {
                yield return GiantWindow(move);
            }
        }

        // One move of the giant, played through its keys and the root's held move as a player would, from the window's
        // first frame.
        private IEnumerator GiantWindow(GiantMove move)
        {
            string name = GiantWindowNames[(int)move];
            if (!_session.IsColossusActive || !_follow.IsChasing)
            {
                _events.Add($"{_frame}: FAILED no giant in the chase view for {name}");
                yield break;
            }

            _scenario = name;
            _phase = Phase.Measure;
            int frames = FramesFor(GiantWindowSeconds);
            for (int frame = 0; frame < frames; frame++)
            {
                PlayGiant(move, frame);
                yield return null;
            }

            _root.HoldMove(Vector2.zero);
            if (!_session.IsColossusActive)
            {
                _events.Add($"{_frame}: the giant ended during {name}");
            }

            yield return EndWindow();
        }

        private void PlayGiant(GiantMove move, int frame)
        {
            switch (move)
            {
                case GiantMove.Walk:
                    int leg = (frame / FramesFor(GiantWalkLegSeconds)) % GiantWalkLegs.Length;
                    _root.HoldMove(GiantWalkLegs[leg]);
                    break;
                case GiantMove.Orbit:
                    _follow.Orbit(GiantOrbitDegreesPerFrame, 0f);
                    break;
                case GiantMove.Punch:
                    PressEvery(PunchHole, frame, GiantPunchEverySeconds);
                    break;
                case GiantMove.Kick:
                    PressEvery(KickHole, frame, GiantKickEverySeconds);
                    break;
                case GiantMove.Slam:
                    PressAt(SlamHole, frame, 0);
                    break;
                case GiantMove.Roar:
                    PressAt(RoarHole, frame, 0);
                    break;
                case GiantMove.Spin:
                    // K K, twice: the second K of each pair is the spin kick.
                    int spinFrame = frame % FramesFor(GiantSpinEverySeconds);
                    PressAt(KickHole, spinFrame, 0);
                    PressAt(KickHole, spinFrame, 1);
                    break;
                case GiantMove.Leap:
                    PressAt(SlamHole, frame, 0);
                    PressAt(SlamHole, frame, 1);
                    break;
            }
        }

        private void PressEvery(int hole, int frame, float seconds)
        {
            if (frame % FramesFor(seconds) == 0)
            {
                _root.PressHole(hole);
            }
        }

        private void PressAt(int hole, int frame, int at)
        {
            if (frame == at)
            {
                _root.PressHole(hole);
            }
        }

        private IEnumerator CastDomeSting(string name, float seconds)
        {
            int dome = SongIndex(typeof(DomeSong));
            int sting = SongIndex(typeof(StingSong));
            yield return EnsureFarm(DomeStingSeconds, _options.IsSubtraction ? _session.RoundCount - 1 : WaveOfEightRound - 1);
            yield return WaitReady(dome, sting);
            yield return Hold(name, Phase.Settle, ShortSettleSeconds);
            yield return CastAndMeasure(name, dome, sting, seconds);
        }

        private IEnumerator Walk()
        {
            _scenario = Walking;
            int legFrames = FramesFor(WalkLegSeconds);
            int settleFrames = FramesFor(SettleSeconds);
            int measureFrames = FramesFor(MeasureSeconds);
            _isWalking = true;
            for (int frame = 0; frame < legFrames * WalkLegs.Length; frame++)
            {
                _walkDirection = WalkLegs[frame / legFrames];
                _phase = frame < settleFrames ? Phase.Settle : frame < settleFrames + measureFrames ? Phase.Measure : Phase.Wait;
                yield return null;
            }

            _isWalking = false;
            _log.Flush();
            _motor.PlaceAt(_start, true);
            yield return Hold(Walking, Phase.Wait, AfterMoveSeconds);
        }

        private IEnumerator CollectNest()
        {
            yield return EnsureFarm(NestSeconds, WaveOfEightRound - 1);
            yield return WaitReady(-1, -1);
            NestField nests = _session.Nests;
            int chosen = -1;
            for (int index = 0; index < nests.Count && chosen < 0; index++)
            {
                if (nests[index].IsOpen && !nests[index].IsCollected)
                {
                    chosen = index;
                }
            }

            if (chosen < 0)
            {
                _events.Add($"{_frame}: no open nest to collect");
                yield break;
            }

            yield return Hold(NestName, Phase.Settle, ShortSettleSeconds);
            _motor.PlaceAt(nests[chosen].Position + (Vector3.back * BesideNestMetres), true);
            StartCoroutine(PlayNestCombo(nests[chosen]));
            yield return Measure(NestName, MeasureSeconds);
            _events.Add($"{_frame}: nest {chosen} collected={nests[chosen].IsCollected}");
            _motor.PlaceAt(_start, true);
            yield return Hold(NestName, Phase.Wait, AfterMoveSeconds);
        }

        // Plan section U: a nest is collected by playing its combo, read first and then played at a person's pace, while the
        // frames are measured.
        private IEnumerator PlayNestCombo(Nest nest)
        {
            yield return new WaitForSeconds(NestComboReadSeconds);
            for (int index = 0; index < nest.Combo.Count && !nest.IsCollected; index++)
            {
                _root.PressHole(nest.Combo[index]);
                yield return new WaitForSeconds(NestComboNoteSeconds);
            }
        }

        private IEnumerator WaveOfEightBeekeepers()
        {
            _scenario = WaveEight;
            while (_session.Round < WaveOfEightRound && _session.State == HiveDefenseState.Playing)
            {
                yield return ClearRound();
            }

            _isHealingHives = true;
            _beekeeperCap = WaveOfEight;
            yield return StartWave(WaveOfEight);
            yield return Hold(WaveEight, Phase.Settle, WaveSettleSeconds);
            yield return Measure(WaveEight, MeasureSeconds);
            _beekeeperCap = -1;
        }

        // Brings on the last round with every beekeeper of it on the field, and lets them walk in.
        private IEnumerator LastWave()
        {
            _scenario = WaveLast;
            _beekeeperCap = -1;
            _isHealingHives = true;
            _motor.PlaceAt(_start, true);
            while (_session.Round < _session.RoundCount && _session.State == HiveDefenseState.Playing)
            {
                yield return ClearRound();
            }

            yield return StartWave(int.MaxValue);
            yield return Hold(WaveLast, Phase.Settle, WaveSettleSeconds);
        }

        // ------------------------------------------------------------------ subtraction script

        private IEnumerator RunSubtraction()
        {
            for (int index = 0; index < SongStationSwitches.Length; index++)
            {
                yield return EnsureFarm(StationLoopSeconds, _session.RoundCount - 1);
                BenchmarkSwitch change = SongStationSwitches[index];
                yield return Switch(change);
                foreach (string station in _options.Stations)
                {
                    yield return RunStation(station);
                }

                yield return Unswitch();
            }

            yield return LastWave();
            for (int index = 0; index < WaveStationSwitches.Length; index++)
            {
                yield return Switch(WaveStationSwitches[index]);
                yield return Measure(WaveLast, StationSeconds);
                yield return Unswitch();
            }
        }

        private IEnumerator Switch(BenchmarkSwitch change)
        {
            _phase = Phase.Setup;
            _switches.Apply(change);
            yield return Hold(_scenario, Phase.Settle, change == BenchmarkSwitch.HalfWasps ? AfterHalfSeconds : SettleSeconds);
        }

        private IEnumerator Unswitch()
        {
            bool wasHalf = _switches.Current == BenchmarkSwitch.HalfWasps;
            _switches.Restore();
            if (wasHalf)
            {
                yield return Hold(_scenario, Phase.Wait, AfterHalfSeconds);
            }
        }

        private IEnumerator RunStation(string station)
        {
            if (station == Stand)
            {
                yield return WaitReady(-1, -1);
                yield return Hold(Stand, Phase.Settle, ShortSettleSeconds);
                yield return Measure(Stand, StationSeconds);
                yield break;
            }

            if (station == DomeSting)
            {
                yield return CastDomeSting(DomeSting, StationSeconds);
                yield break;
            }

            int order = Array.IndexOf(SongNames, station);
            if (order < 0)
            {
                throw new ArgumentException($"Unknown station '{station}'. Use stand, domesting or a song: {string.Join(",", SongNames)}.");
            }

            int song = SongIndex(SongOrder[order]);
            yield return WaitReady(song, -1);
            yield return Hold(station, Phase.Settle, ShortSettleSeconds);
            yield return CastAndMeasure(station, song, -1, StationSeconds);
        }

        // ------------------------------------------------------------------ steps

        private IEnumerator Hold(string scenario, Phase phase, float seconds)
        {
            _scenario = scenario;
            _phase = phase;
            int frames = FramesFor(seconds);
            for (int frame = 0; frame < frames; frame++)
            {
                yield return null;
            }
        }

        private IEnumerator Measure(string scenario, float seconds)
        {
            _scenario = scenario;
            _phase = Phase.Measure;
            int frames = FramesFor(seconds);
            for (int frame = 0; frame < frames; frame++)
            {
                yield return null;
            }

            yield return EndWindow();
        }

        // Plays the song's notes one a frame, as fast as a player could, and then a second song's; the window starts with
        // the first note. The song itself is cast on the tick after its last note.
        private IEnumerator CastAndMeasure(string scenario, int song, int secondSong, float seconds)
        {
            Prepare(song);
            _scenario = scenario;
            _phase = Phase.Measure;
            int frames = FramesFor(seconds);
            int done = 0;
            foreach (int hole in _session.Caster[song].Song.Keys)
            {
                _root.PressHole(hole);
                yield return null;
                done++;
            }

            if (secondSong >= 0)
            {
                foreach (int hole in _session.Caster[secondSong].Song.Keys)
                {
                    _root.PressHole(hole);
                    yield return null;
                    done++;
                }
            }

            for (; done < frames; done++)
            {
                yield return null;
            }

            yield return EndWindow();
        }

        // Ends a measured window: writes the rows, and counts the glow's overdraw on the next frame when asked.
        private IEnumerator EndWindow()
        {
            _phase = Phase.Wait;
            _isHoldingSwarm = true;
            _log.Flush();
            if (_overdraw == null || _switches.Current != BenchmarkSwitch.None)
            {
                yield break;
            }

            _phase = Phase.Probe;
            if (_overdraw.TryMeasure(out GlowOverdrawResult result))
            {
                WriteOverdraw(result);
            }

            yield return null;
            _phase = Phase.Wait;
        }

        // The Colossus needs full Fury, and Hive Call needs room in the swarm or it ends on its first tick.
        private void Prepare(int song)
        {
            SwarmSongEffect effect = _session.Caster[song];
            if (effect is ColossusSong)
            {
                // Since songs-v2 V a point of Fury costs more damage, and 1,000 no longer filled it: the giant was refused
                // and its windows went unmeasured. Damage is added until the meter is full, whatever its price.
                for (int add = 0; add < MostFuryAdds && !_session.Fury.IsFull; add++)
                {
                    _session.Fury.AddDamage(FullFuryDamage);
                }
            }
            else if (effect is HiveCallSong)
            {
                _isHoldingSwarm = false;
                _session.Swarm.Lose(Mathf.RoundToInt(_target * HiveCallRoomShare));
            }
        }

        private IEnumerator WaitReady(int song, int secondSong)
        {
            _phase = Phase.Wait;
            int frames = FramesFor(MostWaitSeconds);
            for (int frame = 0; frame < frames && !IsReady(song, secondSong); frame++)
            {
                yield return null;
            }

            if (!IsReady(song, secondSong))
            {
                _events.Add($"{_frame}: still not ready after {MostWaitSeconds} s for song {song}");
            }
        }

        private bool IsReady(int song, int secondSong)
        {
            SwarmFormMachine forms = _session.Forms;
            bool isQuiet = forms.Current == SwarmForm.River
                && !forms.Slot(SongSlot.Post).IsActive
                && !forms.Slot(SongSlot.Guard).IsActive
                && !forms.Slot(SongSlot.Strike).IsActive
                && forms.GustSeconds < 0f;
            return isQuiet && IsCooled(song) && IsCooled(secondSong);
        }

        private bool IsCooled(int song) => song < 0 || _session.Caster.CooldownFraction(song) <= 0f;

        // Keeps the farm phase long enough for what comes next by clearing the round early, as long as rounds remain
        // before the one the script needs later.
        private IEnumerator EnsureFarm(float seconds, int lastClearableRound)
        {
            bool isLongEnough = _session.Phase == MatchPhase.Farm && _session.FarmSecondsLeft >= seconds;
            if (isLongEnough)
            {
                yield break;
            }

            if (_session.Round > lastClearableRound)
            {
                _events.Add($"{_frame}: farm time short in round {_session.Round} ({_session.FarmSecondsLeft:F1} s for {seconds} s), so a fresh match");
                yield return FreshMatch();
                yield break;
            }

            string scenario = _scenario;
            yield return ClearRound();
            yield return Hold(scenario, Phase.Setup, AfterClearSeconds);
        }

        // Plays out the current round at once: calls its wave, then defeats each beekeeper as the wave director brings it,
        // until the director finishes the wave. The next round's nests open, as they would.
        private IEnumerator ClearRound()
        {
            string scenario = _scenario;
            _scenario = ClearingRound;
            _phase = Phase.Setup;
            int round = _session.Round;
            if (_session.Phase == MatchPhase.Farm)
            {
                _root.CallWave();
                yield return null;
            }

            for (int step = 0; step < MostSpawnSteps && _session.Round == round && _session.State == HiveDefenseState.Playing; step++)
            {
                DefeatAll();
                _session.Waves.Tick(SpawnStepSeconds, _session.ActiveBeekeeperCount);
            }

            _events.Add($"{_frame}: cleared round {round}, now round {_session.Round}, nests open {_session.Nests.OpenCount}");
            yield return null;
            _scenario = scenario;
        }

        // Calls the wave and brings its beekeepers on at once, up to the given number alive.
        private IEnumerator StartWave(int alive)
        {
            _phase = Phase.Setup;
            if (_session.Phase == MatchPhase.Farm)
            {
                _root.CallWave();
                yield return null;
            }

            for (int step = 0; step < MostSpawnSteps && _session.ActiveBeekeeperCount < alive; step++)
            {
                int before = _session.ActiveBeekeeperCount;
                _session.Waves.Tick(SpawnStepSeconds, before);
                if (_session.Phase != MatchPhase.Survive)
                {
                    break;
                }
            }

            _events.Add($"{_frame}: wave of round {_session.Round} started with {_session.ActiveBeekeeperCount} beekeepers");
        }

        // Brings on more of the running wave's beekeepers until the given number are alive, never a new wave.
        private void RefillWave(int alive)
        {
            for (int step = 0; step < MostSpawnSteps && _session.Phase == MatchPhase.Survive && _session.ActiveBeekeeperCount < alive; step++)
            {
                _session.Waves.Tick(SpawnStepSeconds, _session.ActiveBeekeeperCount);
            }
        }

        private void DefeatAll()
        {
            for (int index = _session.ActiveBeekeeperCount - 1; index >= 0; index--)
            {
                _session.ActiveBeekeeper(index).TakeDamage(KillDamage);
            }
        }

        // ------------------------------------------------------------------ each frame

        private void KeepUp()
        {
            if (_isWalking)
            {
                _motor.Move(_walkDirection, _session.CoreSpeedMultiplier, false, Time.deltaTime);
            }

            HoldSwarm();
            if (_isHealingHives)
            {
                HealHives();
            }

            if (_beekeeperCap >= 0)
            {
                CapBeekeepers();
            }
        }

        // Keeps the swarm at the run's size: losses are made good at once; extra wasps are only taken away outside a
        // measured window, so a window shows what the scene does.
        private void HoldSwarm()
        {
            if (!_isHoldingSwarm || _session.Swarm.IsCollapsed)
            {
                return;
            }

            SwarmBody swarm = _session.Swarm;
            int wanted = _switches != null && _switches.Current == BenchmarkSwitch.HalfWasps ? _target / 2 : _target;
            if (swarm.Count < wanted)
            {
                swarm.Gain(wanted - swarm.Count);
            }
            else if (swarm.Count > wanted && _phase != Phase.Measure)
            {
                swarm.Lose(swarm.Count - wanted);
            }
        }

        private void HealHives()
        {
            HiveSet hives = _session.Hives;
            for (int index = 0; index < hives.Count; index++)
            {
                Hive hive = hives[index];
                if (!hive.IsDestroyed && hive.Health < hive.MaxHealth)
                {
                    hive.Heal(hive.MaxHealth - hive.Health);
                }
            }
        }

        // Defeats the newest beekeepers beyond the cap, so the wave holds exactly that many.
        private void CapBeekeepers()
        {
            for (int index = _alive.Count - 1; index >= 0 && _session.ActiveBeekeeperCount > _beekeeperCap; index--)
            {
                _alive[index].TakeDamage(KillDamage);
            }
        }

        private void Record()
        {
            long now = Stopwatch.GetTimestamp();
            double frameMs = _lastTimestamp == 0 ? 0.0 : (now - _lastTimestamp) * 1000.0 / Stopwatch.Frequency;
            if (_recorders == null || _session == null || _switches == null)
            {
                return; // Before the scene is found, or while a fresh match loads: the next row's time includes these frames.
            }

            _lastTimestamp = now;

            _recorders.CaptureTiming();
            _frame++;
            BenchmarkFrameLog log = _log;
            log.Integer(_frame);
            log.Fixed(Elapsed(), SecondsDecimals);
            log.Fixed(frameMs, TimeDecimals);
            log.Text(_scenario);
            log.Text(PhaseNames[(int)_phase]);
            log.Text(SwitchNames[(int)_switches.Current]);
            log.Integer(_castThisFrame);
            _castThisFrame = -1;
            log.Integer(_hitsThisFrame);
            _hitsThisFrame = 0;
            log.Integer(_session.Swarm.Count);
            log.Integer(_target);
            log.Integer(_session.ActiveBeekeeperCount);
            log.Integer(_session.Smoke.CloudCount);
            log.Integer(_session.Round);
            log.Integer(_session.Nests.OpenCount);
            SwarmFormMachine forms = _session.Forms;
            log.Integer((int)forms.Current);
            log.Integer(SlotForm(forms.Slot(SongSlot.Post)));
            log.Integer(SlotForm(forms.Slot(SongSlot.Guard)));
            log.Integer(SlotForm(forms.Slot(SongSlot.Strike)));
            _switches.CountLights(out int on, out int forcedPixel, out int swarm, out int hive, out int nest);
            log.Integer(on);
            log.Integer(forcedPixel);
            log.Integer(swarm);
            log.Integer(hive);
            log.Integer(nest);
            log.Integer(Screen.width);
            log.Integer(Screen.height);
            log.Integer(Application.isFocused ? 1 : 0);
            FrameTiming timing = _recorders.LatestTiming;
            log.Fixed(timing.cpuFrameTime, TimeDecimals);
            log.Fixed(timing.cpuMainThreadFrameTime, TimeDecimals);
            log.Fixed(timing.cpuRenderThreadFrameTime, TimeDecimals);
            log.Fixed(timing.gpuFrameTime, TimeDecimals);
            for (int index = 0; index < _recorders.Count; index++)
            {
                if (_recorders.IsValid(index))
                {
                    log.Fixed(_recorders.Value(index), TimeDecimals);
                }
                else
                {
                    log.Empty();
                }
            }

            log.EndRow();
            if (log.IsFilling)
            {
                log.Flush();
            }
        }

        private static int SlotForm(in SlotState slot) => slot.IsActive ? (int)slot.Form : -1;

        // ------------------------------------------------------------------ setup and files

        private bool TryFindScene()
        {
            _root = FindAnyObjectByType<HiveDefenseRoot>();
            if (_root == null || _root.Session == null)
            {
                return false;
            }

            _session = _root.Session;
            _motor = FindAnyObjectByType<SwarmCoreMotor>();
            _swarmView = FindAnyObjectByType<SwarmGpuView>();
            _follow = FindAnyObjectByType<FollowCamera>();
            _camera = Camera.main;
            if (_motor == null || _swarmView == null || _follow == null || _camera == null)
            {
                throw new InvalidOperationException("The hive defense scene has no core motor, swarm view, follow camera or main camera.");
            }

            return true;
        }

        // The run's files and recorders, once; Bind connects them to each match.
        private void Open()
        {
            _recorders = new BenchmarkRecorders(Debug.isDebugBuild, _options.PathFor("markers.txt"));
            _log = new BenchmarkFrameLog(_options.PathFor("frames.csv"));
            WriteHeader();
            if (_options.ProbesOverdraw)
            {
                _overdrawLog = new BenchmarkFrameLog(_options.PathFor("overdraw.csv"));
                foreach (string column in OverdrawColumns)
                {
                    _overdrawLog.Text(column);
                }

                _overdrawLog.EndRow();
            }
        }

        private void WriteHeader()
        {
            foreach (string column in FrameColumns)
            {
                _log.Text(column);
            }

            for (int index = 0; index < _recorders.Count; index++)
            {
                _log.Text(_recorders.Column(index));
            }

            _log.EndRow();
        }

        private void WriteOverdraw(GlowOverdrawResult result)
        {
            double pixels = (double)result.Width * result.Height;
            BenchmarkFrameLog log = _overdrawLog;
            log.Integer(_frame);
            log.Text(_scenario);
            log.Text(SwitchNames[(int)_switches.Current]);
            log.Integer(_session.Swarm.Count);
            log.Integer(result.Width);
            log.Integer(result.Height);
            log.Integer(result.Covered);
            log.Fixed(result.Covered / pixels, SecondsDecimals);
            log.Fixed(result.Fragments, 0);
            log.Fixed(result.Fragments / pixels, SecondsDecimals);
            log.Fixed(result.Covered > 0 ? result.Fragments / result.Covered : 0.0, TimeDecimals);
            log.Integer(result.Median);
            log.Integer(result.P95);
            log.Integer(result.P99);
            log.Integer(result.Most);
            log.EndRow();
            log.Flush();
        }

        private void WriteNotes(string path)
        {
            using (var notes = new StreamWriter(path, false))
            {
                notes.WriteLine($"command line: {_options.CommandLine}");
                notes.WriteLine($"unity {Application.unityVersion}, development build {Debug.isDebugBuild}, platform {Application.platform}");
                notes.WriteLine($"gpu {SystemInfo.graphicsDeviceName}, api {SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsDeviceVersion}");
                notes.WriteLine($"cpu {SystemInfo.processorType} x{SystemInfo.processorCount}, memory {SystemInfo.systemMemorySize} MB, os {SystemInfo.operatingSystem}");
                notes.WriteLine($"screen {Screen.width}x{Screen.height}, mode {Screen.fullScreenMode}, display {Screen.currentResolution.width}x{Screen.currentResolution.height} @ {Screen.currentResolution.refreshRateRatio.value:F2} Hz, dpi {Screen.dpi}");
                notes.WriteLine($"camera pixels {_camera.pixelWidth}x{_camera.pixelHeight}, fov {_camera.fieldOfView}, far {_camera.farClipPlane}, allowMSAA {_camera.allowMSAA}, renderingPath {_camera.actualRenderingPath}, depthTextureMode {_camera.depthTextureMode}");
                notes.WriteLine($"quality {QualitySettings.names[QualitySettings.GetQualityLevel()]}, vSyncCount {QualitySettings.vSyncCount}, targetFrameRate {Application.targetFrameRate}, pixelLightCount {QualitySettings.pixelLightCount}, shadows {QualitySettings.shadows}, shadowResolution {QualitySettings.shadowResolution}, cascades {QualitySettings.shadowCascades}, shadowDistance {QualitySettings.shadowDistance}, msaa {QualitySettings.antiAliasing}");
                notes.WriteLine($"FrameTimingManager enabled {FrameTimingManager.IsFeatureEnabled()}, vsyncs per second {FrameTimingManager.GetVSyncsPerSecond()}, captureDeltaTime {Time.captureDeltaTime}");
                if (_session != null)
                {
                    notes.WriteLine($"swarm target {_target}, count {_session.Swarm.Count}, capacity {_session.Swarm.Capacity}, max {_session.Swarm.Max}, round {_session.Round}/{_session.RoundCount}, nests open {_session.Nests.OpenCount}/{_session.Nests.Count}");
                }

                _recorders?.Describe(notes);
                foreach (Light light in _switches.Lights)
                {
                    if (light == null)
                    {
                        continue;
                    }

                    notes.WriteLine($"light '{light.name}' under '{(light.transform.parent != null ? light.transform.parent.name : "-")}': {light.type}, {light.renderMode}, shadows {light.shadows}, range {light.range:F1}, intensity {light.intensity:F2}, enabled {light.enabled}, active {light.gameObject.activeInHierarchy}");
                }

                Renderer ground = _switches.Ground;
                if (ground != null)
                {
                    notes.WriteLine($"ground: {ground.GetType().Name}, shader {ground.sharedMaterial.shader.name}, castShadows {ground.shadowCastingMode}, receiveShadows {ground.receiveShadows}, bounds {ground.bounds.size}");
                }

                var sceneryShaders = new SortedDictionary<string, int>(StringComparer.Ordinal);
                int casting = 0;
                foreach (Renderer renderer in _switches.Scenery)
                {
                    casting += renderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off ? 1 : 0;
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        string shader = material != null ? material.shader.name : "(none)";
                        sceneryShaders.TryGetValue(shader, out int count);
                        sceneryShaders[shader] = count + 1;
                    }
                }

                notes.WriteLine($"scenery: {_switches.Scenery.Count} renderers, {casting} cast shadows");
                foreach (KeyValuePair<string, int> pair in sceneryShaders)
                {
                    notes.WriteLine($"scenery shader {pair.Key}: {pair.Value} material slots");
                }

                foreach (string entry in _events)
                {
                    notes.WriteLine(entry);
                }
            }
        }

        private void Finish()
        {
            if (_isFinished)
            {
                return;
            }

            _isFinished = true;
            _isHoldingSwarm = false;
            try
            {
                if (_session != null)
                {
                    _events.Add($"{_frame}: finished at {Elapsed():F1} s, state {_session.State}, round {_session.Round}, wasps {_session.Swarm.Count}");
                }

                _log?.Dispose();
                _overdrawLog?.Dispose();
                _overdraw?.Dispose();
                if (_switches != null)
                {
                    WriteNotes(_options.PathFor("notes.txt"));
                }

                Unbind();
                _recorders?.Dispose();
            }
            finally
            {
                Application.Quit();
            }
        }

        private void HandleSongCast(int song)
        {
            _castThisFrame = song;
            _events.Add($"{_frame}: cast {_session.Caster[song].Song.Title} in {_scenario}");
        }

        // Every hit a song lands and every blow a shield blocks, as the felt-hit views see them, so a window's cost can be read
        // against how much was going on.
        private void HandleSongLanded(SongHit hit) => _hitsThisFrame++;

        private void HandleSongNotReady(int song, SongRefusal reason)
        {
            _events.Add($"{_frame}: REFUSED {_session.Caster[song].Song.Title} ({reason}) in {_scenario}");
        }

        private void HandleBeekeeperSpawned(BeekeeperBrain brain) => _alive.Add(brain);

        private void HandleBeekeeperDied(BeekeeperBrain brain) => _alive.Remove(brain);

        private int SongIndex(Type songType)
        {
            SwarmSongCaster caster = _session.Caster;
            for (int song = 0; song < caster.Count; song++)
            {
                if (songType.IsInstanceOfType(caster[song]))
                {
                    return song;
                }
            }

            throw new InvalidOperationException($"The scene has no {songType.Name}.");
        }

        private double Elapsed() => (Stopwatch.GetTimestamp() - _startTimestamp) / (double)Stopwatch.Frequency;

        private static int FramesFor(float seconds) => Mathf.CeilToInt(seconds / FrameSeconds);

        private static string[] WindowNames(string suffix)
        {
            var names = new string[SongNames.Length];
            for (int index = 0; index < names.Length; index++)
            {
                names[index] = SongNames[index] + suffix;
            }

            return names;
        }
    }
}
