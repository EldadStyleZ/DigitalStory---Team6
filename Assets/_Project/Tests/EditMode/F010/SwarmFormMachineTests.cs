using System.Collections.Generic;
using System.Globalization;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// The whole swarm's form (the river, Hive Call, the Colossus and the collapse), the Gust pulse, and how they meet the
    /// slot songs. The slot engine itself is in <see cref="SongSlotsTests"/>, and each song's own rule in <see cref="SwarmSongTests"/>.
    /// </summary>
    public sealed class SwarmFormMachineTests
    {
        private readonly List<Object> _assets = new List<Object>();

        private SwarmConfig _config;
        private FuryConfig _furyConfig;
        private SquadConfig _squads;
        private HiveConfig _hiveConfig;
        private HiveSet _hives;
        private SwarmBody _body;
        private FuryMeter _fury;
        private SwarmFormMachine _forms;
        private List<SwarmForm> _changes;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<SwarmConfig>();
            _furyConfig = ScriptableObject.CreateInstance<FuryConfig>();
            _squads = ScriptableObject.CreateInstance<SquadConfig>();
            _hiveConfig = ScriptableObject.CreateInstance<HiveConfig>();
            _hives = new HiveSet(new List<Hive> { new Hive(_hiveConfig, new Vector3(-10f, 0f, 0f)), new Hive(_hiveConfig, new Vector3(10f, 0f, 0f)) });

            // The swarm starts full at its capacity (plan section H), so these start half full to leave Hive Call room.
            SetInt("StartingWasps", _config.StartingCapacity / 2);
            Build();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_furyConfig);
            Object.DestroyImmediate(_squads);
            Object.DestroyImmediate(_hiveConfig);

            foreach (Object asset in _assets)
            {
                Object.DestroyImmediate(asset);
            }

            _assets.Clear();
        }

        [Test]
        public void New_Machine_StartsInTheRiverWithEverySlotEmpty()
        {
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_forms.SecondsInForm, Is.EqualTo(0f));
            Assert.That(_forms.IsDomed, Is.False);
            Assert.That(_forms.IsCoreRooted, Is.False);
            Assert.That(_forms.GustSeconds, Is.EqualTo(-1f));
            Assert.That(_forms.Slot(SongSlot.Post).IsActive, Is.False);
            Assert.That(_forms.Slot(SongSlot.Guard).IsActive, Is.False);
            Assert.That(_forms.Slot(SongSlot.Strike).IsActive, Is.False);
        }

        [Test]
        public void New_MissingBodyOrFury_Throws()
        {
            Assert.That(() => new SwarmFormMachine(null, _fury, _squads), Throws.ArgumentNullException);
            Assert.That(() => new SwarmFormMachine(_body, null, _squads), Throws.ArgumentNullException);
        }

        [Test]
        public void CastDome_InTheRiver_DomesTheSwarmButLeavesItsFormAlone()
        {
            Start(SwarmForm.Dome, 5f);

            Assert.That(_forms.IsDomed, Is.True);
            Assert.That(_forms.IsCoreRooted, Is.False);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River), "the Dome runs in the Guard slot beside the river");
            Assert.That(_changes, Is.Empty);
        }

        [TestCase(SwarmForm.Dome, SongSlot.Guard)]
        [TestCase(SwarmForm.Sting, SongSlot.Strike)]
        [TestCase(SwarmForm.Tornado, SongSlot.Strike)]
        public void Tick_SlotSongReachesItsDuration_LetsGo(SwarmForm form, SongSlot slot)
        {
            Start(form, 4f);

            _forms.Tick(3.5f, _hives);
            Assert.That(_forms.Slot(slot).ReleasedSeconds, Is.EqualTo(-1f), "still running before its duration");

            _forms.Tick(0.5f, _hives);
            Assert.That(_forms.Slot(slot).ReleasedSeconds, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(_forms.IsDomed, Is.False);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        [Test]
        public void Tick_GustReachesItsDuration_EndsThePulse()
        {
            Start(SwarmForm.Gust, 4f);

            _forms.Tick(3.5f, _hives);
            Assert.That(_forms.GustSeconds, Is.EqualTo(3.5f).Within(1e-4f));

            _forms.Tick(0.5f, _hives);
            Assert.That(_forms.GustSeconds, Is.EqualTo(-1f));
        }

        [Test]
        public void Tick_ColossusReachesItsDuration_ReturnsToTheRiver()
        {
            Start(SwarmForm.Colossus, 4f);

            _forms.Tick(3.5f, _hives);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Colossus), "still standing before its duration");

            _forms.Tick(0.5f, _hives);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_changes[_changes.Count - 1], Is.EqualTo(SwarmForm.River));
            Assert.That(_forms.IsCoreRooted, Is.False);
        }

        [Test]
        public void Tick_OneStepLongerThanTheColossus_EndsItOnce()
        {
            Start(SwarmForm.Colossus, 1f);
            _changes.Clear();

            _forms.Tick(30f, _hives);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_changes, Is.EqualTo(new[] { SwarmForm.River }));
        }

        // The Colossus, not Hive Call: Hive Call now ends once the hives' stores run dry, which they do within these 2 s.
        [Test]
        public void Tick_InAForm_CountsSecondsInForm()
        {
            _forms.StartColossus(12f);

            _forms.Tick(1.5f, _hives);
            _forms.Tick(0.5f, _hives);

            Assert.That(_forms.SecondsInForm, Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void Tick_InTheRiver_CountsSecondsInForm()
        {
            _forms.Tick(2f, _hives);

            Assert.That(_forms.SecondsInForm, Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void Tick_NegativeTime_Throws()
        {
            Assert.That(() => _forms.Tick(-0.1f, _hives), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void SecondsInForm_FormChanges_StartsAgainFromZero()
        {
            _forms.StartHiveCall();
            _forms.Tick(0.2f, _hives);
            Assert.That(_forms.SecondsInForm, Is.GreaterThan(0f));

            _forms.StartColossus(12f);

            Assert.That(_forms.SecondsInForm, Is.EqualTo(0f));
        }

        [Test]
        public void StartGust_WhileDomed_KeepsTheDome()
        {
            Start(SwarmForm.Dome, 5f);

            bool started = _forms.StartGust(Vector3.zero, 6f, 0.8f);

            Assert.That(started, Is.True);
            Assert.That(_forms.IsDomed, Is.True, "the Gust is a pulse on top of the Dome, not a form that replaces it");
            Assert.That(_forms.GustSeconds, Is.EqualTo(0f));
            Assert.That(_changes, Is.Empty);
        }

        [Test]
        public void StartingAnything_WhileTheColossusStands_IsRefused()
        {
            _forms.StartColossus(12f);
            DomeSong dome = Song<DomeSong>(5f);
            SwarmSongContext context = Context();

            Assert.That(_forms.StartSong(dome, in context), Is.False);
            Assert.That(_forms.StartGust(Vector3.zero, 6f, 0.8f), Is.False);
            Assert.That(_forms.StartHiveCall(), Is.False);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Colossus));
            Assert.That(_forms.IsDomed, Is.False);
            Assert.That(_changes, Is.EqualTo(new[] { SwarmForm.Colossus }));
        }

        // Colossus and Fury

        [Test]
        public void AddColossusDamage_WhileColossus_FillsFury()
        {
            _forms.StartColossus(12f);

            _forms.AddColossusDamage(45f);

            Assert.That(_fury.Value, Is.EqualTo(45f / _furyConfig.DamagePerFuryPoint).Within(1e-3f));
        }

        [Test]
        public void AddColossusDamage_OutsideTheColossus_IsIgnored()
        {
            Start(SwarmForm.Dome, 5f);

            _forms.AddColossusDamage(45f);

            Assert.That(_fury.Value, Is.EqualTo(0f));
        }

        // Plan section K: the core rides inside the giant, so nothing is left rooted behind. Only Hive Call roots it.
        [Test]
        public void StartColossus_DoesNotRootTheCore()
        {
            _forms.StartColossus(12f);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Colossus));
            Assert.That(_forms.IsCoreRooted, Is.False);
        }

        // The giant no longer roots the core, so the collapse has no root to lift. What it must do is end the giant at once,
        // with one change of form that the session and the View follow.
        [Test]
        public void Colossus_SwarmCollapses_EndsInTheCollapse()
        {
            _forms.StartColossus(12f);

            _body.Lose(_body.Count);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
            Assert.That(_changes, Is.EqualTo(new[] { SwarmForm.Colossus, SwarmForm.Collapsed }));
        }

        // The giant's time (plan section I): its song's seconds, more for each kill, never past its most.

        [Test]
        public void ColossusSeconds_OutsideTheGiant_AreZero()
        {
            Assert.That(_forms.ColossusSecondsLeft, Is.EqualTo(0f));
            Assert.That(_forms.ColossusSecondsTotal, Is.EqualTo(0f));

            _forms.StartColossus(4f);
            _forms.Tick(4.5f, _hives);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_forms.ColossusSecondsLeft, Is.EqualTo(0f));
            Assert.That(_forms.ColossusSecondsTotal, Is.EqualTo(0f));
        }

        [Test]
        public void ColossusSeconds_CountDownFromTheStart()
        {
            _forms.StartColossus(20f, 2f, 30f);
            Assert.That(_forms.ColossusSecondsLeft, Is.EqualTo(20f).Within(1e-4f));
            Assert.That(_forms.ColossusSecondsTotal, Is.EqualTo(20f).Within(1e-4f));

            _forms.Tick(5f, _hives);

            Assert.That(_forms.ColossusSecondsLeft, Is.EqualTo(15f).Within(1e-4f));
            Assert.That(_forms.ColossusSecondsTotal, Is.EqualTo(20f).Within(1e-4f));
        }

        [TestCase(1, 22f)]
        [TestCase(4, 28f)]
        [TestCase(5, 30f)]
        [TestCase(9, 30f)]
        public void AddColossusKills_EachAddsItsSecondsNeverPastTheMost(int kills, float total)
        {
            _forms.StartColossus(20f, 2f, 30f);
            _forms.Tick(5f, _hives);

            _forms.AddColossusKills(kills);

            Assert.That(_forms.ColossusSecondsTotal, Is.EqualTo(total).Within(1e-4f));
            Assert.That(_forms.ColossusSecondsLeft, Is.EqualTo(total - 5f).Within(1e-4f));
            _forms.Tick(total - 5f - 0.01f, _hives);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Colossus), "still standing just before its new end");
            _forms.Tick(0.02f, _hives);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        [Test]
        public void AddColossusKills_OneAtATime_StopsAtTheMost()
        {
            _forms.StartColossus(20f, 2f, 30f);

            for (int kill = 0; kill < 8; kill++)
            {
                _forms.AddColossusKills(1);
            }

            Assert.That(_forms.ColossusSecondsTotal, Is.EqualTo(30f).Within(1e-4f));
        }

        [Test]
        public void AddColossusKills_OutsideTheGiant_IsIgnored()
        {
            _forms.AddColossusKills(3);
            _forms.StartColossus(20f, 2f, 30f);

            Assert.That(_forms.ColossusSecondsTotal, Is.EqualTo(20f).Within(1e-4f), "kills before it stood count for nothing");

            _forms.AddColossusKills(0);
            _forms.AddColossusKills(-2);
            Assert.That(_forms.ColossusSecondsTotal, Is.EqualTo(20f).Within(1e-4f));
        }

        // The single-number start is a giant that kills cannot lengthen, as the old song was.
        [Test]
        public void StartColossus_WithSecondsOnly_KillsAddNothing()
        {
            _forms.StartColossus(12f);

            _forms.AddColossusKills(3);

            Assert.That(_forms.ColossusSecondsTotal, Is.EqualTo(12f).Within(1e-4f));
        }

        [Test]
        public void StartColossus_AgainAfterAGiantThatGrew_StartsFromItsOwnSeconds()
        {
            _forms.StartColossus(20f, 2f, 30f);
            _forms.AddColossusKills(4);
            _body.Lose(_body.Count);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
            _forms.Tick(_config.CollapseBlockSeconds + 0.1f, _hives);
            _body.Tick(_config.CollapseBlockSeconds + 0.1f, _hives);

            _forms.StartColossus(20f, 2f, 30f);

            Assert.That(_forms.ColossusSecondsTotal, Is.EqualTo(20f).Within(1e-4f));
        }

        [TestCase(-1f, 2f, 30f)]
        [TestCase(20f, -2f, 30f)]
        [TestCase(20f, 2f, 19f)]
        public void StartColossus_ImpossibleTimes_Throw(float seconds, float secondsPerKill, float mostSeconds)
        {
            Assert.That(
                () => _forms.StartColossus(seconds, secondsPerKill, mostSeconds), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        // Plan section I: the leap slam's giant lands before its time can end it.
        [Test]
        public void SetColossusAirborne_TimeRunsOutInTheAir_EndsOnlyAfterItLands()
        {
            _forms.StartColossus(1f);
            _forms.SetColossusAirborne(true);

            _forms.Tick(1.5f, _hives);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Colossus), "still in the air");
            Assert.That(_forms.ColossusSecondsLeft, Is.EqualTo(0f));

            _forms.SetColossusAirborne(false);
            _forms.Tick(0.01f, _hives);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        [Test]
        public void SetColossusAirborne_ACollapseInTheAir_StillEndsIt()
        {
            _forms.StartColossus(12f);
            _forms.SetColossusAirborne(true);

            _body.Lose(_body.Count);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
        }

        [Test]
        public void StartColossus_AfterAGiantThatCollapsedInTheAir_IsNotHeldUp()
        {
            _forms.StartColossus(12f);
            _forms.SetColossusAirborne(true);
            _body.Lose(_body.Count);
            _forms.Tick(_config.CollapseBlockSeconds + 0.1f, _hives);
            _body.Tick(_config.CollapseBlockSeconds + 0.1f, _hives);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River), "the swarm came back");

            _forms.StartColossus(1f);
            _forms.Tick(1.1f, _hives);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River), "the new giant is on the ground");
        }

        [Test]
        public void SetColossusAirborne_OutsideTheGiant_HoldsUpNoLaterGiant()
        {
            _forms.SetColossusAirborne(true);
            _forms.StartColossus(1f);

            _forms.Tick(1.1f, _hives);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        // Hive Call

        [Test]
        public void StartHiveCall_RootsTheCore()
        {
            _forms.StartHiveCall();

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.HiveCall));
            Assert.That(_forms.IsCoreRooted, Is.True);
        }

        // Plan section M: Hive Call calls every standing hive's store from anywhere, at the rate a store flows to a core
        // beside it. Each default hive holds StartStore (100) and hands out CollectPerSecond (150) a second.
        [Test]
        public void Tick_HiveCall_EveryStandingHivesStoreFlowsInAtTheCollectRate()
        {
            _forms.StartHiveCall();

            _forms.Tick(0.5f, _hives);

            float each = 0.5f * _hiveConfig.CollectPerSecond;
            Assert.That(_body.Count, Is.EqualTo(_config.StartingWasps + (int)(2f * each)), "the collect rate from each of 2 hives");
            Assert.That(_hives[0].Store, Is.EqualTo(_hiveConfig.StartStore - each).Within(0.001f));
            Assert.That(_hives[1].Store, Is.EqualTo(_hiveConfig.StartStore - each).Within(0.001f));
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.HiveCall), "no end while the stores hold wasps and the swarm has room");
        }

        [Test]
        public void Tick_HiveCall_MakesNoWaspFromNothing()
        {
            _forms.StartHiveCall();
            float stored = _hives[0].Store + _hives[1].Store;

            _forms.Tick(0.3f, _hives);
            _forms.Tick(0.3f, _hives);

            float taken = stored - (_hives[0].Store + _hives[1].Store);
            Assert.That(_body.Count - _config.StartingWasps, Is.EqualTo(Mathf.FloorToInt(taken + 0.001f)), "the swarm gains what the stores lose");
        }

        [Test]
        public void Tick_HiveCallSkipsADestroyedHive()
        {
            _hives[0].Damage(10000f);
            _forms.StartHiveCall();

            _forms.Tick(0.5f, _hives);

            Assert.That(_body.Count, Is.EqualTo(_config.StartingWasps + (int)(0.5f * _hiveConfig.CollectPerSecond)));
        }

        [Test]
        public void Tick_HiveCallDrainsTheStores_EndsIt()
        {
            _forms.StartHiveCall();

            _forms.Tick(1f, _hives);

            Assert.That(_body.Count, Is.EqualTo(_config.StartingWasps + (int)(2f * _hiveConfig.StartStore)), "all that both stores held");
            Assert.That(_hives.HasStoredWasps, Is.False);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_forms.IsCoreRooted, Is.False);
        }

        [Test]
        public void Tick_HiveCallWithTheStoresAlreadyEmpty_EndsOnTheNextTickAndAddsNothing()
        {
            _hives[0].TakeFromStore(_hives[0].Store);
            _hives[1].TakeFromStore(_hives[1].Store);

            Assert.That(_forms.StartHiveCall(), Is.True, "it is never refused, so the player still finds it");
            _forms.Tick(0.016f, _hives);

            Assert.That(_body.Count, Is.EqualTo(_config.StartingWasps));
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        [Test]
        public void Tick_HiveCallWithNoStandingHives_AddsNothingAndEnds()
        {
            _hives[0].Damage(10000f);
            _hives[1].Damage(10000f);
            _forms.StartHiveCall();

            _forms.Tick(1f, _hives);

            Assert.That(_body.Count, Is.EqualTo(_config.StartingWasps));
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        [Test]
        public void Tick_NullHives_Throws()
        {
            Assert.That(() => _forms.Tick(0.1f, null), Throws.ArgumentNullException);
        }

        [Test]
        public void NotifyHit_DuringHiveCall_CancelsIt()
        {
            _forms.StartHiveCall();

            _forms.NotifyHit(Vector3.right);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_forms.IsCoreRooted, Is.False);
            Assert.That(_changes, Is.EqualTo(new[] { SwarmForm.HiveCall, SwarmForm.River }));
        }

        [Test]
        public void NotifyCoreMoved_DuringHiveCall_CancelsIt()
        {
            _forms.StartHiveCall();

            _forms.NotifyCoreMoved();

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_forms.IsCoreRooted, Is.False);
        }

        [Test]
        public void NotifyHit_AfterACancel_RaisesNothingMore()
        {
            _forms.StartHiveCall();
            _forms.NotifyHit(Vector3.right);

            _forms.NotifyHit(Vector3.right);
            _forms.NotifyCoreMoved();

            Assert.That(_changes, Is.EqualTo(new[] { SwarmForm.HiveCall, SwarmForm.River }));
        }

        [TestCase(SwarmForm.Dome, SwarmForm.River)]
        [TestCase(SwarmForm.Colossus, SwarmForm.Colossus)]
        [TestCase(SwarmForm.River, SwarmForm.River)]
        public void NotifyHitAndMoved_OutsideHiveCall_ChangeNothing(SwarmForm started, SwarmForm wholeSwarm)
        {
            Start(started, 5f);
            bool wasDomed = _forms.IsDomed;
            _changes.Clear();

            _forms.NotifyHit(Vector3.right);
            _forms.NotifyCoreMoved();

            Assert.That(_forms.Current, Is.EqualTo(wholeSwarm));
            Assert.That(_forms.IsDomed, Is.EqualTo(wasDomed));
            Assert.That(_changes, Is.Empty);
        }

        // 30 wasps of room and 200 in the stores: the swarm fills, and the stores keep what it had no room for. Full means
        // the capacity, which nests raise (plan section H), not the 4,000 ceiling.
        [Test]
        public void Tick_HiveCallFillsTheSwarm_EndsAtTheCapacity()
        {
            SetInt("StartingWasps", _config.StartingCapacity - 30);
            Build();
            _forms.StartHiveCall();

            _forms.Tick(1f, _hives);

            Assert.That(_body.Count, Is.EqualTo(_config.StartingCapacity), "capped at the capacity");
            Assert.That(_body.Count, Is.LessThan(_body.Max));
            Assert.That(_hives[0].Store + _hives[1].Store, Is.EqualTo((2f * _hiveConfig.StartStore) - 30f).Within(0.001f));
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        [Test]
        public void Tick_HiveCallJustBelowTheCapacity_KeepsCalling()
        {
            SetInt("StartingWasps", _config.StartingCapacity - 300);
            Build();
            _forms.StartHiveCall();

            _forms.Tick(0.5f, _hives);

            Assert.That(_body.Count, Is.EqualTo(_config.StartingCapacity - 300 + (int)_hiveConfig.CollectPerSecond));
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.HiveCall));
        }

        // Collapse

        [Test]
        public void Collapse_WhileDomed_GoesToCollapsedAndDropsTheDome()
        {
            Start(SwarmForm.Dome, 5f);

            _body.Lose(_body.Count);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
            Assert.That(_forms.IsDomed, Is.False);
            Assert.That(_forms.Slot(SongSlot.Guard).IsActive, Is.False);
            Assert.That(_changes, Is.EqualTo(new[] { SwarmForm.Collapsed }));
        }

        [Test]
        public void Collapse_WhileInTheRiver_GoesToCollapsed()
        {
            _body.Lose(_body.Count);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
            Assert.That(_forms.IsCoreRooted, Is.False);
        }

        [Test]
        public void Collapse_WhileStinging_EndsTheSting()
        {
            Start(SwarmForm.Sting, 4f);

            _body.Lose(_body.Count);

            Assert.That(_forms.Slot(SongSlot.Strike).IsActive, Is.False);
        }

        [Test]
        public void Start_WhileCollapsed_IsRefused()
        {
            _body.Lose(_body.Count);
            _changes.Clear();

            Start(SwarmForm.Dome, 5f);
            Start(SwarmForm.Sting, 4f);
            Start(SwarmForm.Tornado, 1.5f);

            Assert.That(_forms.StartGust(Vector3.zero, 6f, 0.8f), Is.False);
            Assert.That(_forms.StartHiveCall(), Is.False);
            Assert.That(_forms.StartColossus(12f), Is.False);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
            Assert.That(_forms.Slot(SongSlot.Guard).IsActive, Is.False);
            Assert.That(_forms.Slot(SongSlot.Strike).IsActive, Is.False);
            Assert.That(_changes, Is.Empty);
            _forms.Tick(1f, _hives);
            Assert.That(_fury.Value, Is.EqualTo(0f), "a refused tornado fills no Fury");
        }

        [Test]
        public void Tick_WhileCollapsed_StaysCollapsedWithoutTheBodyRespawning()
        {
            _body.Lose(_body.Count);

            _forms.Tick(1f, _hives);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
        }

        [Test]
        public void Respawn_AfterTheBlock_GoesBackToTheRiver()
        {
            _body.Lose(_body.Count);
            _changes.Clear();

            _body.Tick(3f, _hives);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_changes, Is.EqualTo(new[] { SwarmForm.River }));
            Start(SwarmForm.Dome, 5f);
            Assert.That(_forms.IsDomed, Is.True, "songs work again after the respawn");
        }

        // Allocation

        [Test]
        public void Tick_StingAndHiveCall_AllocateNothing()
        {
            Start(SwarmForm.Sting, 100f);
            Assert.That(() => _forms.Tick(0.016f, _hives), Is.Not.AllocatingGCMemory());

            _forms.StartHiveCall();
            Assert.That(() => _forms.Tick(0.016f, _hives), Is.Not.AllocatingGCMemory());
        }

        private void Build()
        {
            _body = new SwarmBody(_config);
            _fury = new FuryMeter(_furyConfig);
            _forms = new SwarmFormMachine(_body, _fury, _squads);
            _changes = new List<SwarmForm>();
            _forms.FormChanged += form => _changes.Add(form);
        }

        private void SetInt(string name, int value)
        {
            JsonUtility.FromJsonOverwrite("{\"<" + name + ">k__BackingField\":" + value + "}", _config);
        }

        // Starts a form or a song as its song asset would, with its seconds changed. The Sting's target stands 1 m away.
        private void Start(SwarmForm form, float seconds)
        {
            switch (form)
            {
                case SwarmForm.Dome:
                    Cast(Song<DomeSong>(seconds));
                    break;
                case SwarmForm.Sting:
                    Cast(Song<StingSong>(seconds));
                    break;
                case SwarmForm.Tornado:
                    Cast(Song<TornadoSong>(seconds));
                    break;
                case SwarmForm.Gust:
                    Cast(Song<GustSong>(seconds));
                    break;
                case SwarmForm.HiveCall:
                    _forms.StartHiveCall();
                    break;
                case SwarmForm.Colossus:
                    _forms.StartColossus(seconds);
                    break;
            }
        }

        private T Song<T>(float seconds) where T : SwarmSongEffect
        {
            var song = ScriptableObject.CreateInstance<T>();
            _assets.Add(song);
            JsonUtility.FromJsonOverwrite("{\"<Seconds>k__BackingField\":" + seconds.ToString("R", CultureInfo.InvariantCulture) + "}", song);
            return song;
        }

        private void Cast(SwarmSongEffect song)
        {
            SwarmSongContext context = Context();
            song.Cast(in context);
        }

        private SwarmSongContext Context()
        {
            var aim = new FakeSwarmTarget { Position = new Vector3(1f, 0f, 0f) };
            return new SwarmSongContext(Vector3.zero, aim, Vector3.zero, _body, _forms, _fury, _hives, new List<ISwarmTarget>(), null);
        }
    }
}
