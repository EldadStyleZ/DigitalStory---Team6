#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>
    /// Loads the hive defense scene and checks that it starts, draws the swarm, answers the flute, keeps the core inside the
    /// giant, the camera on the core and the core in the play area and out of the hives, and animates the beekeepers. It also
    /// checks the farm and survive phases, the wasp economy and the nests that grow the swarm through the root, as the keys
    /// would drive them. Every song is played with the keys on its own asset, so a change of keys never breaks these tests.
    /// </summary>
    public sealed class HiveDefenseSceneTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const string SwarmConfigPath = "Assets/_Project/GameData/F010/SwarmConfig.asset";
        private const string ColossusConfigPath = "Assets/_Project/GameData/F010/ColossusConfig.asset";
        private const string CoreMoveConfigPath = "Assets/_Project/GameData/F010/CoreMoveConfig.asset";
        private const string WaveConfigPath = "Assets/_Project/GameData/F010/WaveConfig.asset";
        private const string HiveConfigPath = "Assets/_Project/GameData/F010/HiveConfig.asset";
        private const string NestConfigPath = "Assets/_Project/GameData/F010/NestConfig.asset";
        private const string GroundName = "Ground";
        private const string SceneryName = "Scenery";
        private const int FramesToSettle = 3;
        private const int FramesToRun = 60;
        private const int StandingHives = 2;
        private const int SongsKnownAtStart = 1;
        private const float FastTimeScale = 5f;
        private const int MaxFramesForWave = 300000;
        private const float PastTheFarmTimerSeconds = 5f;
        private const int FramesBetweenNotes = 1;
        private const string LegBone = "LeftUpperLeg";
        private const float WatchSeconds = 0.7f;
        private const float SmallestSwingDegrees = 10f;
        private const int SoundedHole = 3;
        private const float FullFuryDamage = 1000f;
        private const float MaxColossusWaitSeconds = 2f;
        private const float PushSeconds = 0.5f;
        private const float SamePlaceMetres = 0.01f;
        private const float CameraBlendSeconds = 2.5f;
        private const float ChaseLeastHeightMetres = 2.5f;
        private const int MaxWarmupFrames = 20000;
        private const int WarmupFailureShare = 10;
        private const float SameViewDegrees = 1f;
        private const float PastEdgeMetres = 6f;
        private const float FallenMetres = 0.5f;
        private const float ShortWayOutMetres = 2f;
        private const float WalkSeconds = 0.2f;
        private const float SameWayDot = 0.95f;
        private const float WallHalfSizeMetres = 0.8f;
        private const float WallInsideEdgeMetres = 2f;
        private const float BesideHiveMetres = 2f;
        private const int CollectHive = 0;
        private const float CollectSeconds = 0.3f;
        private const float GiveSeconds = 0.5f;
        private const float BesideNestMetres = 1.5f;
        private const float StandWithoutPlayingSeconds = 2.5f;
        private const float NestBurstSeconds = 1.2f; // NestView's burst, 0.9 s, and a little more.
        private const string NestObjectName = "WaspNest";
        private const int StartingLevel = 1;

        // The controller rests its skin width away from what it touches, so a capsule this much thinner than the core's
        // overlaps only what the core is really inside.
        private const float TouchingMetres = 0.01f;
        private const int GuardSlot = (int)SongSlot.Guard;
        private const int StrikeSlot = (int)SongSlot.Strike;

        private HiveDefenseRoot _root;
        private SwarmConfig _swarmConfig;
        private float _savedTimeScale;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            _savedTimeScale = Time.timeScale;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));

            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                yield return null;
            }

            _root = Object.FindAnyObjectByType<HiveDefenseRoot>();
            _swarmConfig = AssetDatabase.LoadAssetAtPath<SwarmConfig>(SwarmConfigPath);
            Assert.That(_swarmConfig, Is.Not.Null, "There is no SwarmConfig at " + SwarmConfigPath + ". Rebuild the scene.");
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = _savedTimeScale;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Scene_RunsSixtyFrames_LogsNoError()
        {
            for (int frame = 0; frame < FramesToRun; frame++)
            {
                yield return null;
            }

            LogAssert.NoUnexpectedReceived();
        }

        // The Mono player compiles a method on its first call, which made the first Sting of a session a slow frame. The root
        // compiles the game's code ahead, a little each frame from the start, and nearly every method compiles.
        [UnityTest]
        public IEnumerator Scene_OverItsFirstMoments_CompilesTheGameCodeAhead()
        {
            ScriptWarmup warmup = _root.ScriptWarmup;
            Assert.That(warmup, Is.Not.Null, "the root made no warm-up");
            Assert.That(warmup.MethodCount, Is.GreaterThan(0), "the warm-up found no method");

            int frames = 0;
            while (!warmup.IsDone && frames < MaxWarmupFrames)
            {
                frames++;
                yield return null;
            }

            Assert.That(warmup.IsDone, Is.True, "the warm-up did not finish");
            Assert.That(warmup.FailedCount, Is.LessThan(warmup.MethodCount / WarmupFailureShare), "most methods did not compile ahead");
        }

        [UnityTest]
        public IEnumerator Scene_OnLoad_HasAPlayingSessionWithBothHives()
        {
            Assert.That(_root, Is.Not.Null);
            Assert.That(_root.Session, Is.Not.Null);
            Assert.That(_root.Session.State, Is.EqualTo(HiveDefenseState.Playing));
            Assert.That(_root.Session.Hives.StandingCount, Is.EqualTo(StandingHives));
            Assert.That(_root.Session.Swarm.Count, Is.EqualTo(_swarmConfig.StartingWasps));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Scene_OnLoad_KnowsOnlySting()
        {
            SongDiscovery discovery = _root.Session.Discovery;
            SwarmSongCaster caster = _root.Session.Caster;
            for (int song = 0; song < caster.Count; song++)
            {
                Assert.That(discovery.IsKnown(song), Is.EqualTo(caster[song] is StingSong), caster[song].Song.Title);
            }

            Assert.That(discovery.KnownCount, Is.EqualTo(SongsKnownAtStart));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PressHole_WithTheDomeKeys_MakesTheDomeKnown()
        {
            int dome = SongIndex<DomeSong>();
            Assert.That(_root.Session.Discovery.IsKnown(dome), Is.False, "The Dome is known before it is played.");

            yield return Play(KeysOf<DomeSong>());

            Assert.That(_root.Session.Discovery.IsKnown(dome), Is.True);
        }

        [UnityTest]
        public IEnumerator PressHole_WithTheDomeKeys_ColoursTheGuardSquadInTheDefenseColour()
        {
            SwarmGpuView view = Object.FindAnyObjectByType<SwarmGpuView>();

            yield return Play(KeysOf<DomeSong>());

            Assert.That(_root.Session.Forms.Slot(SongSlot.Guard).Form, Is.EqualTo(SwarmForm.Dome));
            Assert.That(view.IsSlotLive(GuardSlot), Is.True);
            Assert.That(view.SlotColor(GuardSlot), Is.EqualTo(_swarmConfig.DefenseColor));
            Assert.That(_root.Session.Forms.Current, Is.EqualTo(SwarmForm.River), "the rest of the swarm stays the river");
        }

        // Plan section G, M1 is done when the Dome and the Sting run together in Play Mode.
        [UnityTest]
        public IEnumerator PressHole_TheDomeThenTheSting_RunBothAtOnceInTheirOwnColours()
        {
            SwarmGpuView view = Object.FindAnyObjectByType<SwarmGpuView>();

            yield return Play(KeysOf<DomeSong>());
            yield return Play(KeysOf<StingSong>());

            SwarmFormMachine forms = _root.Session.Forms;
            Assert.That(forms.Slot(SongSlot.Guard).Form, Is.EqualTo(SwarmForm.Dome));
            Assert.That(forms.Slot(SongSlot.Strike).Form, Is.EqualTo(SwarmForm.Sting));
            Assert.That(forms.IsDomed, Is.True);
            Assert.That(forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(view.IsSlotLive(GuardSlot), Is.True, "the view does not draw the Dome");
            Assert.That(view.IsSlotLive(StrikeSlot), Is.True, "the view does not draw the Sting");
            Assert.That(view.SlotColor(GuardSlot), Is.EqualTo(_swarmConfig.DefenseColor));
            Assert.That(view.SlotColor(StrikeSlot), Is.EqualTo(_swarmConfig.AttackColor));
        }

        [UnityTest]
        public IEnumerator PressHole_ASecondAttack_ReplacesTheFirstWhileTheDomeStays()
        {
            SwarmGpuView view = Object.FindAnyObjectByType<SwarmGpuView>();
            yield return Play(KeysOf<DomeSong>());
            yield return Play(KeysOf<StingSong>());
            Assert.That(_root.Session.Forms.Slot(SongSlot.Strike).Form, Is.EqualTo(SwarmForm.Sting), "the Sting did not start");

            yield return Play(KeysOf<TornadoSong>());

            SwarmFormMachine forms = _root.Session.Forms;
            Assert.That(forms.Slot(SongSlot.Strike).Form, Is.EqualTo(SwarmForm.Tornado));
            Assert.That(forms.Slot(SongSlot.Guard).Form, Is.EqualTo(SwarmForm.Dome));
            Assert.That(forms.IsDomed, Is.True);
            Assert.That(view.SlotColor(GuardSlot), Is.EqualTo(_swarmConfig.DefenseColor));
            Assert.That(view.SlotColor(StrikeSlot), Is.EqualTo(_swarmConfig.AttackColor));
        }

        [UnityTest]
        public IEnumerator SwarmView_RunsAfterStart_AndStopsWhenDisabled()
        {
            SwarmGpuView view = Object.FindAnyObjectByType<SwarmGpuView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.IsRunning, Is.True, "The buffers are made when the scene starts.");

            view.gameObject.SetActive(false);
            yield return null;

            Assert.That(view.IsRunning, Is.False, "The buffers are released when the object is disabled.");
        }

        [UnityTest]
        public IEnumerator PressHole_WithTheDomeKeys_FormsTheDome()
        {
            yield return Play(KeysOf<DomeSong>());

            Assert.That(_root.Session.Forms.Slot(SongSlot.Guard).Form, Is.EqualTo(SwarmForm.Dome));
        }

        // Plan section A: an aimed song casts even with nobody on the field, and strikes ahead of the core.
        [UnityTest]
        public IEnumerator PressHole_WithTheMeteorKeys_FormsTheMeteor()
        {
            Assert.That(_root.Session.ActiveBeekeeperCount, Is.EqualTo(0), "a beekeeper came before the test played");

            yield return Play(KeysOf<MeteorSong>());

            Assert.That(_root.Session.Forms.Slot(SongSlot.Strike).Form, Is.EqualTo(SwarmForm.Meteor));
        }

        // The five songs added after the Meteor: each one's keys, played in the first farm phase with nobody on the field,
        // put its form in its own slot: the attacks in Strike, the trap and the shell in Post.
        [UnityTest]
        public IEnumerator PressHole_WithTheHydraKeys_FormsTheHydra()
        {
            return ExpectSlotAfterKeys<HydraSong>(SongSlot.Strike, SwarmForm.Hydra);
        }

        [UnityTest]
        public IEnumerator PressHole_WithTheScytheKeys_FormsTheScythe()
        {
            return ExpectSlotAfterKeys<ScytheSong>(SongSlot.Strike, SwarmForm.Scythe);
        }

        [UnityTest]
        public IEnumerator PressHole_WithTheLanceKeys_FormsTheLance()
        {
            return ExpectSlotAfterKeys<LanceSong>(SongSlot.Strike, SwarmForm.Lance);
        }

        [UnityTest]
        public IEnumerator PressHole_WithTheNestKeys_LeavesTheNestInThePostSlot()
        {
            return ExpectSlotAfterKeys<NestSong>(SongSlot.Post, SwarmForm.Nest);
        }

        [UnityTest]
        public IEnumerator PressHole_WithTheHoneycombKeys_LeavesTheHoneycombInThePostSlot()
        {
            return ExpectSlotAfterKeys<HoneycombSong>(SongSlot.Post, SwarmForm.Honeycomb);
        }

        // The scene holds all twelve songs, each once, in the song table's order: the attacks, the defenses, the gold songs.
        [UnityTest]
        public IEnumerator Scene_OnLoad_HoldsTheTwelveSongsInTheTablesOrder()
        {
            yield return null;
            var expected = new[]
            {
                typeof(StingSong), typeof(TornadoSong), typeof(MeteorSong), typeof(HydraSong), typeof(ScytheSong), typeof(LanceSong),
                typeof(NestSong), typeof(DomeSong), typeof(GustSong), typeof(HoneycombSong), typeof(HiveCallSong), typeof(ColossusSong),
            };
            SwarmSongCaster caster = _root.Session.Caster;
            var actual = new System.Type[caster.Count];
            for (int song = 0; song < caster.Count; song++)
            {
                actual[song] = caster[song].GetType();
            }

            Assert.That(actual, Is.EqualTo(expected));
        }

        [UnityTest]
        public IEnumerator PressHole_OnAHole_SoundsThatHoleOnTheFluteVoice()
        {
            SwarmFluteVoice voice = Object.FindAnyObjectByType<SwarmFluteVoice>();
            Assert.That(voice, Is.Not.Null, "The scene has no SwarmFluteVoice.");

            _root.PressHole(SoundedHole);

            Assert.That(voice.HolePlaying, Is.EqualTo(SoundedHole));
            yield return null;
        }

        // Plan section L: the first wave comes when the first farm phase's timer runs out, even if it is never called.
        [UnityTest]
        public IEnumerator Scene_WhenTheFirstFarmTimerRunsOut_HasAnActiveBeekeeper()
        {
            float most = LoadWaves().FirstFarmSeconds + PastTheFarmTimerSeconds;
            Time.timeScale = FastTimeScale;

            // Batch mode runs thousands of frames a second, so count game time, not frames.
            int frames = 0;
            float elapsed = 0f;
            while (_root.Session.ActiveBeekeeperCount < 1 && elapsed < most && frames < MaxFramesForWave)
            {
                elapsed += Time.deltaTime;
                frames++;
                yield return null;
            }

            Assert.That(_root.Session.ActiveBeekeeperCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(_root.Session.Phase, Is.EqualTo(MatchPhase.Survive));
        }

        // Plan section L: a match starts in the first round's farm phase, with nobody on the map and the songs free.
        [UnityTest]
        public IEnumerator Match_OnLoad_StartsInAFarmPhaseWithNoBeekeeper()
        {
            for (int frame = 0; frame < FramesToRun; frame++)
            {
                yield return null;
            }

            HiveDefenseSession session = _root.Session;
            WaveConfig waves = LoadWaves();
            Assert.That(session.Phase, Is.EqualTo(MatchPhase.Farm));
            Assert.That(session.Round, Is.EqualTo(1));
            Assert.That(session.RoundCount, Is.EqualTo(waves.Waves.Count));
            Assert.That(session.FarmSecondsLeft, Is.GreaterThan(0f).And.LessThanOrEqualTo(waves.FirstFarmSeconds));
            Assert.That(session.AreSongsFree, Is.True);
            Assert.That(session.ActiveBeekeeperCount, Is.EqualTo(0));
        }

        // Plan section L: calling the wave, as Enter does, starts it at once, and each second of farm phase skipped earns
        // EarlyBonusPerSecond wasps.
        [UnityTest]
        public IEnumerator CallWave_InTheFarmPhase_StartsTheWaveAndGrantsWasps()
        {
            HiveDefenseSession session = _root.Session;
            WaveConfig waves = LoadWaves();
            float skipped = -1f;
            int granted = 0;
            System.Action<float> onCalled = seconds => skipped = seconds;
            System.Action<int> onGranted = wasps => granted += wasps;
            session.Waves.WaveCalledEarly += onCalled;
            session.WaspsGranted += onGranted;

            // The swarm starts full, and a grant only fills it up to its capacity, so half of it is lost first.
            session.Swarm.Lose(session.Swarm.Count / 2);
            int before = session.Swarm.Count;
            try
            {
                _root.CallWave();
                yield return null;
            }
            finally
            {
                session.Waves.WaveCalledEarly -= onCalled;
                session.WaspsGranted -= onGranted;
            }

            Assert.That(session.Phase, Is.EqualTo(MatchPhase.Survive));
            Assert.That(session.AreSongsFree, Is.False);
            Assert.That(session.ActiveBeekeeperCount, Is.GreaterThanOrEqualTo(1), "the wave did not start at once");
            Assert.That(skipped, Is.GreaterThan(0f).And.LessThanOrEqualTo(waves.FirstFarmSeconds));
            Assert.That(granted, Is.EqualTo(Mathf.FloorToInt(skipped * waves.EarlyBonusPerSecond)));
            Assert.That(granted, Is.GreaterThan(0));
            Assert.That(session.Swarm.Count, Is.EqualTo(before + granted));
        }

        // Plan section M: a song costs nothing in a farm phase, and its wasp price, paid at the cast, in a survive phase. A
        // song's first cast in a match is free in any phase, so the Sting is cast in the farm phase first and paid for second.
        [UnityTest]
        public IEnumerator Song_CostsNothingInAFarmPhaseAndItsPriceInASurvivePhase()
        {
            HiveDefenseSession session = _root.Session;
            int dome = SongIndex<DomeSong>();
            int sting = SongIndex<StingSong>();
            Assert.That(session.Caster[dome].WaspCost, Is.GreaterThan(0), "the Dome is free, so the farm phase proves nothing");
            Assert.That(session.Caster[sting].WaspCost, Is.GreaterThan(0));

            int beforeFree = session.Swarm.Count;
            yield return Play(KeysOf<DomeSong>());
            Assert.That(session.Forms.Slot(SongSlot.Guard).Form, Is.EqualTo(SwarmForm.Dome), "the Dome did not cast");
            yield return Play(KeysOf<StingSong>());
            Assert.That(session.Swarm.Count, Is.EqualTo(beforeFree), "a song in a farm phase cost wasps");

            _root.CallWave();
            yield return null;
            Assert.That(session.Phase, Is.EqualTo(MatchPhase.Survive));

            // The session's steps are clamped, so the Sting's own cooldown is waited for, not the game time it would take.
            int frames = 0;
            while (session.Caster.CooldownFraction(sting) > 0f && frames < MaxFramesForWave)
            {
                yield return null;
                frames++;
            }

            Assert.That(session.Caster.CooldownFraction(sting), Is.EqualTo(0f), "the Sting never rested");
            int beforePaid = session.Swarm.Count;

            // Plan section O: the Sting's price is its wasp cost for each sting the living wasps send, one per 400.
            int price = ((StingSong)session.Caster[sting]).WaspCostFor(beforePaid);
            Assert.That(price, Is.EqualTo(session.Caster[sting].WaspCost * (beforePaid / 400)), "the price is not one cost for each 400 wasps");
            Assert.That(session.SongPrice(sting), Is.EqualTo(price), "the song chip's price is not the price paid");
            yield return Play(KeysOf<StingSong>());
            Assert.That(session.Forms.Slot(SongSlot.Strike).Form, Is.EqualTo(SwarmForm.Sting), "the Sting did not cast");
            Assert.That(session.Swarm.Count, Is.EqualTo(beforePaid - price));
        }

        // Plan section M: beside a hive the core takes the hive's store into the swarm.
        [UnityTest]
        public IEnumerator Core_BesideAHive_TakesItsStoreIntoTheSwarm()
        {
            var motor = Object.FindAnyObjectByType<SwarmCoreMotor>();
            HiveDefenseSession session = _root.Session;
            Hive hive = session.Hives[CollectHive];
            float storeBefore = hive.Store;

            // The swarm starts full, so half of it is lost first, to make room for the store.
            session.Swarm.Lose(session.Swarm.Count / 2);
            int before = session.Swarm.Count;
            Assert.That(storeBefore, Is.GreaterThan(0f), "the hive starts with no store");
            Assert.That(session.Swarm.Count, Is.LessThan(session.Swarm.Capacity), "the swarm is full, so it can take nothing");

            motor.PlaceAt(BesideHive(hive.Position), true);
            yield return null;
            Assert.That(session.NearHive, Is.EqualTo(CollectHive), "the core is not within the hive's collect radius");
            Assert.That(session.IsCollecting, Is.True);
            yield return WaitGameSeconds(CollectSeconds);

            Assert.That(hive.Store, Is.LessThan(storeBefore));
            Assert.That(session.Swarm.Count, Is.GreaterThan(before));
        }

        // Plan section M: holding Give beside the hurt hive pays the swarm's wasps into its health, WaspsPerHealthPoint
        // for each point.
        [UnityTest]
        public IEnumerator Give_BesideTheHurtHive_HealsItAndCostsWasps()
        {
            var motor = Object.FindAnyObjectByType<SwarmCoreMotor>();
            HiveDefenseSession session = _root.Session;
            HiveConfig config = LoadHiveConfig();
            int hurt = session.Hives.Count - 1;
            Hive hive = session.Hives[hurt];
            Assert.That(hive.Health, Is.LessThan(hive.MaxHealth), "the last hive does not start hurt");
            float healthBefore = hive.Health;
            int before = session.Swarm.Count;

            _root.HoldGive(true);
            try
            {
                motor.PlaceAt(BesideHive(hive.Position), true);
                yield return WaitGameSeconds(GiveSeconds);
                Assert.That(session.NearHive, Is.EqualTo(hurt), "the core is not within the hive's collect radius");
                Assert.That(session.IsGiving, Is.True);
                Assert.That(session.IsCollecting, Is.False, "the hive handed out its store while Give was held");
            }
            finally
            {
                _root.HoldGive(false);
            }

            float healed = hive.Health - healthBefore;
            int paid = before - session.Swarm.Count;
            Assert.That(healed, Is.GreaterThan(0f));
            Assert.That(paid, Is.GreaterThan(0));
            Assert.That(paid, Is.EqualTo(Mathf.RoundToInt(healed * config.WaspsPerHealthPoint)));
        }

        // Plan section H: the swarm starts full, at its starting capacity, at level 1, with the first nests open and the
        // rest closed, and the nests' views show which are which.
        [UnityTest]
        public IEnumerator Match_OnLoad_StartsFullAtLevelOneWithTheFirstNestsOpen()
        {
            HiveDefenseSession session = _root.Session;
            NestConfig nests = LoadNestConfig();
            Assert.That(session.Swarm.Count, Is.EqualTo(_swarmConfig.StartingWasps));
            Assert.That(session.Swarm.Capacity, Is.EqualTo(_swarmConfig.StartingCapacity));
            Assert.That(session.Swarm.Count, Is.EqualTo(session.Swarm.Capacity), "the swarm does not start full");
            Assert.That(session.Swarm.Max, Is.EqualTo(_swarmConfig.MaxWasps));
            Assert.That(session.Level, Is.EqualTo(StartingLevel));
            Assert.That(session.Nests.Count, Is.EqualTo(nests.NestCount));
            Assert.That(session.Nests.OpenCount, Is.EqualTo(nests.OpenAtStart));
            Assert.That(session.Nests.CollectedCount, Is.EqualTo(0));
            yield return null;

            for (int index = 0; index < session.Nests.Count; index++)
            {
                bool isOpen = index < nests.OpenAtStart;
                Assert.That(session.Nests[index].IsOpen, Is.EqualTo(isOpen), "nest " + index);
                Assert.That(NestViewAt(session.Nests[index].Position).IsOpenShown, Is.EqualTo(isOpen), "nest view " + index);
            }
        }

        // Plan sections H and U: beside an open nest its combo shows; standing collects nothing, and playing the combo takes
        // the nest: the capacity, the count and the level rise, and once its burst has played the nest is gone from its tree.
        [UnityTest]
        public IEnumerator Core_PlayingAnOpenNestsCombo_CollectsItAndGrowsTheSwarm()
        {
            var motor = Object.FindAnyObjectByType<SwarmCoreMotor>();
            HiveDefenseSession session = _root.Session;
            NestConfig nests = LoadNestConfig();
            Nest nest = session.Nests[0];
            Assert.That(nest.IsOpen, Is.True, "the first nest is not open at the start");
            int capacity = session.Swarm.Capacity;
            int count = session.Swarm.Count;
            NestView view = NestViewAt(nest.Position);

            motor.PlaceAt(BesideNest(nest.Position), true);
            yield return null;
            Assert.That(GroundDistance(motor.Position, nest.Position), Is.LessThan(nests.CollectRadius), "the core is not within the nest's collect radius");
            yield return WaitGameSeconds(StandWithoutPlayingSeconds);
            Assert.That(nest.IsCollected, Is.False, "standing alone collected the nest");
            Assert.That(session.Nests.ActiveNest, Is.EqualTo(0), "the nest's combo is not shown");
            Assert.That(Object.FindAnyObjectByType<HiveDefenseHudView>().NestCombo.IsShown, Is.True, "the HUD shows no combo");
            yield return Play(nest.Combo);

            Assert.That(nest.IsCollected, Is.True);
            Assert.That(session.Nests.CollectedCount, Is.EqualTo(1));
            Assert.That(session.Level, Is.EqualTo(StartingLevel + 1));
            int grownCapacity = Mathf.Min(capacity + nests.CapacityPerNest, session.Swarm.Max);
            Assert.That(session.Swarm.Capacity, Is.EqualTo(grownCapacity));
            Assert.That(session.Swarm.Count, Is.EqualTo(Mathf.Min(count + nests.WaspsPerNest, grownCapacity)));
            Assert.That(view.IsOpenShown, Is.False);

            yield return WaitGameSeconds(NestBurstSeconds);
            Transform shown = FindBone(view.transform, NestObjectName);
            Assert.That(shown, Is.Not.Null, "the nest tree has no " + NestObjectName);
            Assert.That(shown.gameObject.activeInHierarchy, Is.False, "the collected nest is still in its tree");
        }

        // Plan section H: a nest that is not open yet cannot be collected, however long the core stays or whatever it plays.
        [UnityTest]
        public IEnumerator Core_AtAClosedNest_CannotCollectIt()
        {
            var motor = Object.FindAnyObjectByType<SwarmCoreMotor>();
            HiveDefenseSession session = _root.Session;
            NestConfig nests = LoadNestConfig();
            Nest nest = session.Nests[nests.OpenAtStart];
            Assert.That(nest.IsOpen, Is.False, "the first nest that opens after a wave is open at the start");
            int capacity = session.Swarm.Capacity;

            motor.PlaceAt(BesideNest(nest.Position), true);
            yield return null;
            Assert.That(GroundDistance(motor.Position, nest.Position), Is.LessThan(nests.CollectRadius), "the core is not within the nest's collect radius");
            yield return WaitGameSeconds(StandWithoutPlayingSeconds);
            Assert.That(session.Nests.ActiveNest, Is.EqualTo(NestField.NoNest), "a closed nest shows a combo");
            yield return Play(nest.Combo);

            Assert.That(nest.IsCollected, Is.False);
            Assert.That(nest.Progress01, Is.EqualTo(0f));
            Assert.That(session.Nests.CollectedCount, Is.EqualTo(0));
            Assert.That(session.Level, Is.EqualTo(StartingLevel));
            Assert.That(session.Swarm.Capacity, Is.EqualTo(capacity));
            Assert.That(NestViewAt(nest.Position).IsOpenShown, Is.False);
        }

        // Plan section K: there is no Piper. The swarm's core is an invisible point with a CharacterController.
        [UnityTest]
        public IEnumerator Core_IsACharacterControllerWithNothingToSee()
        {
            var motor = Object.FindAnyObjectByType<SwarmCoreMotor>();
            Assert.That(motor, Is.Not.Null, "The scene has no SwarmCoreMotor.");
            Assert.That(motor.GetComponent<CharacterController>(), Is.Not.Null);
            Assert.That(motor.GetComponentsInChildren<Renderer>(true), Is.Empty, "the core has no model");
            Assert.That(motor.GetComponentsInChildren<Animator>(true), Is.Empty, "the core has no Animator");
            yield return null;
        }

        // Plan section K: the core rides inside the giant. A push on the core while the giant stands is undone on the next
        // frame, because the root puts the core where the session says it is.
        [UnityTest]
        public IEnumerator Core_WhileTheColossusStands_RidesInsideTheGiant()
        {
            var motor = Object.FindAnyObjectByType<SwarmCoreMotor>();
            HiveDefenseSession session = _root.Session;
            yield return FormTheColossus();

            Assert.That(session.IsCoreRooted, Is.False);

            motor.Move(Vector3.right, 1f, false, PushSeconds);
            Assert.That(GroundDistance(motor.Position, session.ColossusPosition), Is.GreaterThan(SamePlaceMetres), "the push moved the core");
            yield return null;

            Assert.That(GroundDistance(motor.Position, session.ColossusPosition), Is.LessThan(SamePlaceMetres));
        }

        // Plan section I: while the giant stands the camera chases it from behind and above, looking where it faces, and when
        // the giant ends, here because the swarm collapsed, it swoops back to the top view over the core, which it never
        // stopped following.
        [UnityTest]
        public IEnumerator Camera_WhileTheColossusStands_ChasesItFromBehindAndReturnsAfter()
        {
            var motor = Object.FindAnyObjectByType<SwarmCoreMotor>();
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            Assert.That(follow, Is.Not.Null, "The scene has no FollowCamera.");
            HiveDefenseSession session = _root.Session;
            Quaternion topView = follow.transform.rotation;
            Assert.That(follow.IsChasing, Is.False, "before the giant");

            yield return FormTheColossus();
            Assert.That(_root.IsChasingGiant, Is.True, "the root did not start the chase");
            Time.timeScale = FastTimeScale;
            yield return WaitGameSeconds(CameraBlendSeconds);
            Time.timeScale = _savedTimeScale;

            Assert.That(session.IsColossusActive, Is.True, "the giant ended before the camera was measured");
            Assert.That(follow.IsChasing, Is.True);
            Assert.That(follow.ChaseBlend, Is.EqualTo(1f).Within(1e-4f), "the swoop did not finish");
            Assert.That(follow.Target, Is.SameAs(motor.transform), "the top view still keeps the core");
            Vector3 toGiant = session.ColossusPosition - follow.transform.position;
            toGiant.y = 0f;
            Assert.That(Vector3.Dot(toGiant.normalized, session.ColossusFacing), Is.GreaterThan(SameWayDot), "the camera is not behind the giant");
            Assert.That(Vector3.Dot(follow.GroundForward, session.ColossusFacing), Is.GreaterThan(SameWayDot), "the camera does not look where the giant faces");
            Assert.That(follow.transform.position.y, Is.GreaterThan(ChaseLeastHeightMetres), "the chase camera is not above the giant's shoulders");

            session.Swarm.Lose(session.Swarm.Count - (session.Swarm.CollapseLine / 2));
            Assert.That(session.Forms.Current, Is.EqualTo(SwarmForm.Collapsed));
            yield return null;
            yield return null;

            Assert.That(session.IsColossusActive, Is.False, "the collapse ended the giant");
            Assert.That(follow.IsChasing, Is.False, "the camera still chases the ended giant");
            Time.timeScale = FastTimeScale;
            yield return WaitGameSeconds(CameraBlendSeconds);
            Time.timeScale = _savedTimeScale;

            Assert.That(follow.ChaseBlend, Is.EqualTo(0f), "the camera did not swoop back");
            Assert.That(Quaternion.Angle(follow.transform.rotation, topView), Is.LessThan(SameViewDegrees), "the top view is not the one before the giant");
        }

        // Plan section K: a giant can stand over a hive and carry the core inside the hive's collider. When the giant ends
        // there, the root puts the core down at the same place, clear of the hive. Nothing pops on the next frame, and the
        // first step goes the way the player asked, not the way out of the hive.
        [UnityTest]
        public IEnumerator Core_PutDownWhereTheGiantStoodOverAHive_StandsClearOfIt()
        {
            var motor = Object.FindAnyObjectByType<SwarmCoreMotor>();
            var controller = motor.GetComponent<CharacterController>();
            Vector3 hive = _root.Session.Hives[0].Position;
            Collider[] solids = SolidCollidersOf(NearestHiveView(hive).gameObject);
            Assert.That(solids, Is.Not.Empty, "the hive has no collider to hold the core inside it");

            yield return PutDownInside(motor, controller, solids, hive);

            Vector3 away = motor.Position - hive;
            away.y = 0f;
            Assert.That(away.magnitude, Is.LessThan(ShortWayOutMetres), "the core was not put down beside the hive");
            Vector3 before = motor.Position;
            motor.Move(away.normalized, 1f, false, WalkSeconds);
            Vector3 walked = motor.Position - before;
            walked.y = 0f;
            Assert.That(walked.magnitude, Is.GreaterThan(SamePlaceMetres), "the core did not walk");
            Assert.That(Vector3.Dot(walked.normalized, away.normalized), Is.GreaterThan(SameWayDot), "the first step jumped another way");
        }

        // The same for scenery: a giant that ends inside a ruin leaves the core beside it.
        [UnityTest]
        public IEnumerator Core_PutDownWhereTheGiantStoodInAWall_StandsClearOfIt()
        {
            var motor = Object.FindAnyObjectByType<SwarmCoreMotor>();
            var controller = motor.GetComponent<CharacterController>();
            float edge = LoadCoreMove().PlayAreaHalfSize - WallInsideEdgeMetres;
            GameObject scenery = GameObject.Find(SceneryName);
            Assert.That(scenery, Is.Not.Null, "The scene has no " + SceneryName + ".");
            BoxCollider wall = null;
            foreach (BoxCollider box in scenery.GetComponentsInChildren<BoxCollider>())
            {
                Bounds bounds = box.bounds;
                bool isBig = bounds.extents.x > WallHalfSizeMetres && bounds.extents.z > WallHalfSizeMetres;
                bool isInPlay = Mathf.Abs(bounds.center.x) < edge && Mathf.Abs(bounds.center.z) < edge;
                if (box.enabled && !box.isTrigger && isBig && isInPlay)
                {
                    wall = box;
                    break;
                }
            }

            Assert.That(wall, Is.Not.Null, "the scenery has no wall big enough to stand inside");
            Vector3 spot = new Vector3(wall.bounds.center.x, motor.Position.y, wall.bounds.center.z);

            yield return PutDownInside(motor, controller, SolidCollidersOf(scenery), spot);
        }

        // The core keeps to the play area, which lies inside the ground: a core put past its edge is brought back to the
        // edge on the next frame, on the ground. A wall at the edge may push it a little further.
        [UnityTest]
        public IEnumerator Core_PutPastThePlayArea_IsBackAtItsEdgeOnTheGroundNextFrame()
        {
            var motor = Object.FindAnyObjectByType<SwarmCoreMotor>();
            GameObject ground = GameObject.Find(GroundName);
            Assert.That(ground, Is.Not.Null, "The scene has no " + GroundName + ".");
            float edge = LoadCoreMove().PlayAreaHalfSize;
            Bounds groundBounds = ground.GetComponent<Collider>().bounds;
            Assert.That(groundBounds.max.x, Is.GreaterThan(edge + PastEdgeMetres), "the ground ends inside the play area");
            Assert.That(groundBounds.min.x, Is.LessThan(-edge - PastEdgeMetres));
            Assert.That(groundBounds.max.z, Is.GreaterThan(edge + PastEdgeMetres));
            Assert.That(groundBounds.min.z, Is.LessThan(-edge - PastEdgeMetres));
            float height = motor.Position.y;

            motor.PlaceAt(new Vector3(edge + PastEdgeMetres, 0f, 0f), true);
            yield return null;

            Assert.That(_root.Session.CorePosition.x, Is.EqualTo(edge).Within(SamePlaceMetres), "the session did not hold the core at the edge");
            Assert.That(motor.Position.x, Is.LessThan(edge + (PastEdgeMetres * 0.5f)), "the core was not brought back");
            Assert.That(motor.Position.y, Is.GreaterThan(height - FallenMetres), "the core fell");
        }

        // Carries the core into the solids as the giant would, puts it down at the same place as the root does when the
        // giant ends, and checks it is clear and stays put on the next frame.
        private static IEnumerator PutDownInside(SwarmCoreMotor motor, CharacterController controller, Collider[] solids, Vector3 spot)
        {
            motor.PlaceAt(spot, false);
            Assert.That(IsInside(controller, solids), Is.True, "the giant could not carry the core inside");

            motor.PlaceAt(spot, true);
            Assert.That(IsInside(controller, solids), Is.False, "the core was put down inside");
            Vector3 putDown = motor.Position;
            yield return null;

            Assert.That(GroundDistance(motor.Position, putDown), Is.LessThan(SamePlaceMetres), "the core popped on the next frame");
            Assert.That(IsInside(controller, solids), Is.False);
        }

        private static WaveConfig LoadWaves()
        {
            var waves = AssetDatabase.LoadAssetAtPath<WaveConfig>(WaveConfigPath);
            Assert.That(waves, Is.Not.Null, "There is no WaveConfig at " + WaveConfigPath + ". Rebuild the scene.");
            return waves;
        }

        private static HiveConfig LoadHiveConfig()
        {
            var hive = AssetDatabase.LoadAssetAtPath<HiveConfig>(HiveConfigPath);
            Assert.That(hive, Is.Not.Null, "There is no HiveConfig at " + HiveConfigPath + ". Rebuild the scene.");
            return hive;
        }

        private static NestConfig LoadNestConfig()
        {
            var nests = AssetDatabase.LoadAssetAtPath<NestConfig>(NestConfigPath);
            Assert.That(nests, Is.Not.Null, "There is no NestConfig at " + NestConfigPath + ". Rebuild the scene.");
            return nests;
        }

        // A spot on the camera's side of a nest's tree, clear of its trunk and well inside its collect radius.
        private static Vector3 BesideNest(Vector3 nest) => nest + (Vector3.back * BesideNestMetres);

        private static NestView NestViewAt(Vector3 place)
        {
            NestView nearest = null;
            float best = float.MaxValue;
            foreach (NestView view in Object.FindObjectsByType<NestView>())
            {
                float distance = GroundDistance(view.transform.position, place);
                if (distance < best)
                {
                    best = distance;
                    nearest = view;
                }
            }

            Assert.That(nearest, Is.Not.Null, "The scene has no NestView.");
            Assert.That(best, Is.LessThan(SamePlaceMetres), "no NestView stands at the nest");
            return nearest;
        }

        // A spot on the camera's side of a hive, clear of its model and well inside its collect radius.
        private static Vector3 BesideHive(Vector3 hive) => hive + (Vector3.back * BesideHiveMetres);

        private static CoreMoveConfig LoadCoreMove()
        {
            var coreMove = AssetDatabase.LoadAssetAtPath<CoreMoveConfig>(CoreMoveConfigPath);
            Assert.That(coreMove, Is.Not.Null, "There is no CoreMoveConfig at " + CoreMoveConfigPath + ". Rebuild the scene.");
            return coreMove;
        }

        // Fills Fury, plays the Colossus keys and waits until the giant stands.
        private IEnumerator FormTheColossus()
        {
            HiveDefenseSession session = _root.Session;
            session.Fury.AddDamage(FullFuryDamage);
            Assert.That(session.Fury.IsFull, Is.True);
            yield return Play(KeysOf<ColossusSong>());

            float waited = 0f;
            while (!session.IsColossusActive && waited < MaxColossusWaitSeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(session.IsColossusActive, Is.True, "The Colossus keys at full Fury did not form the Colossus.");
        }

        // Counts game time, not frames, because batch mode runs thousands of frames a second.
        private static IEnumerator WaitGameSeconds(float seconds)
        {
            float elapsed = 0f;
            int frames = 0;
            while (elapsed < seconds && frames < MaxFramesForWave)
            {
                yield return null;
                elapsed += Time.deltaTime;
                frames++;
            }
        }

        private static HiveView NearestHiveView(Vector3 place)
        {
            HiveView nearest = null;
            float best = float.MaxValue;
            foreach (HiveView view in Object.FindObjectsByType<HiveView>())
            {
                float distance = GroundDistance(view.transform.position, place);
                if (distance < best)
                {
                    best = distance;
                    nearest = view;
                }
            }

            Assert.That(nearest, Is.Not.Null, "The scene has no HiveView.");
            return nearest;
        }

        private static Collider[] SolidCollidersOf(GameObject owner)
        {
            var solid = new List<Collider>();
            foreach (Collider collider in owner.GetComponentsInChildren<Collider>())
            {
                if (collider.enabled && !collider.isTrigger)
                {
                    solid.Add(collider);
                }
            }

            return solid.ToArray();
        }

        // Whether the core's capsule, made a hair thinner so touching does not count, overlaps any of the colliders.
        private static bool IsInside(CharacterController controller, Collider[] colliders)
        {
            Vector3 center = controller.transform.TransformPoint(controller.center);
            float radius = controller.radius - TouchingMetres;
            float half = Mathf.Max(0f, (controller.height * 0.5f) - controller.radius);
            Collider[] overlaps = Physics.OverlapCapsule(
                center + (Vector3.up * half), center - (Vector3.up * half), radius, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            foreach (Collider overlap in overlaps)
            {
                if (System.Array.IndexOf(colliders, overlap) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        [UnityTest]
        public IEnumerator Beekeeper_WhileWalkingToAHive_MovesItsLegs()
        {
            // The match starts in a farm phase with nobody on the map, so the wave is called at once.
            _root.CallWave();
            yield return null;
            Assert.That(_root.Session.ActiveBeekeeperCount, Is.GreaterThanOrEqualTo(1), "Calling the wave brought no beekeeper.");

            BeekeeperView view = null;
            foreach (BeekeeperView candidate in Object.FindObjectsByType<BeekeeperView>())
            {
                if (candidate.isActiveAndEnabled && candidate.Brain != null)
                {
                    view = candidate;
                    break;
                }
            }

            Assert.That(view, Is.Not.Null, "No beekeeper is shown.");
            Transform leg = FindBone(view.transform, LegBone);
            Assert.That(leg, Is.Not.Null, "The beekeeper's model has no " + LegBone + " bone.");

            float swing = 0f;
            yield return WatchBone(leg, WatchSeconds, angle => swing = angle);

            Assert.That(swing, Is.GreaterThan(SmallestSwingDegrees));
        }

        private IEnumerator Play(IReadOnlyList<int> holes)
        {
            for (int index = 0; index < holes.Count; index++)
            {
                _root.PressHole(holes[index]);
                for (int frame = 0; frame < FramesBetweenNotes; frame++)
                {
                    yield return null;
                }
            }

            // The last note is read on the next tick.
            yield return null;
            yield return null;
        }

        // Plays the song's keys with nobody on the field, and checks that the song now runs in its slot and is known.
        private IEnumerator ExpectSlotAfterKeys<T>(SongSlot slot, SwarmForm form) where T : SwarmSongEffect
        {
            Assert.That(_root.Session.ActiveBeekeeperCount, Is.EqualTo(0), "a beekeeper came before the test played");

            yield return Play(KeysOf<T>());

            SlotState state = _root.Session.Forms.Slot(slot);
            Assert.That(state.Form, Is.EqualTo(form));
            Assert.That(state.ReleasedSeconds, Is.LessThan(0f), "the song has already let go");
            Assert.That(_root.Session.Discovery.IsKnown(SongIndex<T>()), Is.True);
        }

        // The keys on the scene's own song asset.
        private IReadOnlyList<int> KeysOf<T>() where T : SwarmSongEffect
        {
            return _root.Session.Caster[SongIndex<T>()].Song.Keys;
        }

        private int SongIndex<T>() where T : SwarmSongEffect
        {
            SwarmSongCaster caster = _root.Session.Caster;
            for (int song = 0; song < caster.Count; song++)
            {
                if (caster[song] is T)
                {
                    return song;
                }
            }

            Assert.Fail("The scene has no " + typeof(T).Name + ".");
            return -1;
        }

        private static float GroundDistance(Vector3 from, Vector3 to)
        {
            Vector3 offset = to - from;
            offset.y = 0f;
            return offset.magnitude;
        }

        private static Transform FindBone(Transform root, string boneName)
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == boneName)
                {
                    return candidate;
                }
            }

            return null;
        }

        // Reports the widest angle the bone turned away from where it started, over some game seconds.
        private static IEnumerator WatchBone(Transform bone, float seconds, System.Action<float> report)
        {
            Quaternion start = bone.localRotation;
            float widest = 0f;
            float elapsed = 0f;
            int frames = 0;
            while (elapsed < seconds && frames < MaxFramesForWave)
            {
                yield return null;
                elapsed += Time.deltaTime;
                frames++;
                widest = Mathf.Max(widest, Quaternion.Angle(start, bone.localRotation));
            }

            report(widest);
        }
    }
}
#endif
