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
            Assert.That(_forms.IsPiperRooted, Is.False);
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
            Assert.That(_forms.IsPiperRooted, Is.False);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River), "the Dome runs in the Guard slot beside the river");
            Assert.That(_changes, Is.Empty);
        }

        [TestCase(SwarmForm.Dome, SongSlot.Guard)]
        [TestCase(SwarmForm.Sting, SongSlot.Strike)]
        [TestCase(SwarmForm.Tornado, SongSlot.Strike)]
        public void Tick_SlotSongReachesItsDuration_LetsGo(SwarmForm form, SongSlot slot)
        {
            Start(form, 4f);

            _forms.Tick(3.5f, 2);
            Assert.That(_forms.Slot(slot).ReleasedSeconds, Is.EqualTo(-1f), "still running before its duration");

            _forms.Tick(0.5f, 2);
            Assert.That(_forms.Slot(slot).ReleasedSeconds, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(_forms.IsDomed, Is.False);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        [Test]
        public void Tick_GustReachesItsDuration_EndsThePulse()
        {
            Start(SwarmForm.Gust, 4f);

            _forms.Tick(3.5f, 2);
            Assert.That(_forms.GustSeconds, Is.EqualTo(3.5f).Within(1e-4f));

            _forms.Tick(0.5f, 2);
            Assert.That(_forms.GustSeconds, Is.EqualTo(-1f));
        }

        [Test]
        public void Tick_ColossusReachesItsDuration_ReturnsToTheRiver()
        {
            Start(SwarmForm.Colossus, 4f);

            _forms.Tick(3.5f, 2);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Colossus), "still standing before its duration");

            _forms.Tick(0.5f, 2);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_changes[_changes.Count - 1], Is.EqualTo(SwarmForm.River));
            Assert.That(_forms.IsPiperRooted, Is.False);
        }

        [Test]
        public void Tick_OneStepLongerThanTheColossus_EndsItOnce()
        {
            Start(SwarmForm.Colossus, 1f);
            _changes.Clear();

            _forms.Tick(30f, 2);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_changes, Is.EqualTo(new[] { SwarmForm.River }));
        }

        [Test]
        public void Tick_InAForm_CountsSecondsInForm()
        {
            _forms.StartHiveCall();

            _forms.Tick(1.5f, 2);
            _forms.Tick(0.5f, 2);

            Assert.That(_forms.SecondsInForm, Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void Tick_InTheRiver_CountsSecondsInForm()
        {
            _forms.Tick(2f, 2);

            Assert.That(_forms.SecondsInForm, Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void Tick_NegativeTime_Throws()
        {
            Assert.That(() => _forms.Tick(-0.1f, 2), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void SecondsInForm_FormChanges_StartsAgainFromZero()
        {
            _forms.StartHiveCall();
            _forms.Tick(2f, 2);

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

            Assert.That(_fury.Value, Is.EqualTo(22.5f).Within(1e-3f));
        }

        [Test]
        public void AddColossusDamage_OutsideTheColossus_IsIgnored()
        {
            Start(SwarmForm.Dome, 5f);

            _forms.AddColossusDamage(45f);

            Assert.That(_fury.Value, Is.EqualTo(0f));
        }

        [Test]
        public void StartColossus_RootsThePiper()
        {
            _forms.StartColossus(12f);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Colossus));
            Assert.That(_forms.IsPiperRooted, Is.True);
        }

        [Test]
        public void Colossus_SwarmCollapses_EndsInTheCollapse()
        {
            _forms.StartColossus(12f);

            _body.Lose(_config.StartingWasps - 100);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
            Assert.That(_forms.IsPiperRooted, Is.False);
        }

        // Hive Call

        [Test]
        public void StartHiveCall_RootsThePiper()
        {
            _forms.StartHiveCall();

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.HiveCall));
            Assert.That(_forms.IsPiperRooted, Is.True);
        }

        [Test]
        public void Tick_HiveCall_AddsWaspsPerStandingHive()
        {
            _forms.StartHiveCall();

            _forms.Tick(1f, 2);

            Assert.That(_body.Count, Is.EqualTo(_config.StartingWasps + (int)(2 * _config.HiveCallWaspsPerSecondPerHive)), "the hive rate a second for each of 2 hives");
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.HiveCall), "no end before the swarm is full");
        }

        [Test]
        public void Tick_HiveCallWithNoStandingHives_AddsNothing()
        {
            _forms.StartHiveCall();

            _forms.Tick(1f, 0);

            Assert.That(_body.Count, Is.EqualTo(_config.StartingWasps));
        }

        [Test]
        public void NotifyHit_DuringHiveCall_CancelsIt()
        {
            _forms.StartHiveCall();

            _forms.NotifyHit(Vector3.right);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_forms.IsPiperRooted, Is.False);
            Assert.That(_changes, Is.EqualTo(new[] { SwarmForm.HiveCall, SwarmForm.River }));
        }

        [Test]
        public void NotifyPiperMoved_DuringHiveCall_CancelsIt()
        {
            _forms.StartHiveCall();

            _forms.NotifyPiperMoved();

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_forms.IsPiperRooted, Is.False);
        }

        [Test]
        public void NotifyHit_AfterACancel_RaisesNothingMore()
        {
            _forms.StartHiveCall();
            _forms.NotifyHit(Vector3.right);

            _forms.NotifyHit(Vector3.right);
            _forms.NotifyPiperMoved();

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
            _forms.NotifyPiperMoved();

            Assert.That(_forms.Current, Is.EqualTo(wholeSwarm));
            Assert.That(_forms.IsDomed, Is.EqualTo(wasDomed));
            Assert.That(_changes, Is.Empty);
        }

        [Test]
        public void Tick_HiveCallFillsTheSwarm_EndsAtTheMaximum()
        {
            SetInt("StartingWasps", _config.MaxWasps - (int)_config.HiveCallWaspsPerSecondPerHive);
            Build();
            _forms.StartHiveCall();

            _forms.Tick(1f, 1);

            Assert.That(_body.Count, Is.EqualTo(_config.MaxWasps), "capped at the maximum");
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        [Test]
        public void Tick_HiveCallJustBelowTheMaximum_KeepsCalling()
        {
            SetInt("StartingWasps", _config.MaxWasps - (int)(2 * _config.HiveCallWaspsPerSecondPerHive));
            Build();
            _forms.StartHiveCall();

            _forms.Tick(1f, 1);

            Assert.That(_body.Count, Is.EqualTo(_config.MaxWasps - (int)_config.HiveCallWaspsPerSecondPerHive));
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.HiveCall));
        }

        // Collapse

        [Test]
        public void Collapse_WhileDomed_GoesToCollapsedAndDropsTheDome()
        {
            Start(SwarmForm.Dome, 5f);

            _body.Lose(_config.StartingWasps - 100);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
            Assert.That(_forms.IsDomed, Is.False);
            Assert.That(_forms.Slot(SongSlot.Guard).IsActive, Is.False);
            Assert.That(_changes, Is.EqualTo(new[] { SwarmForm.Collapsed }));
        }

        [Test]
        public void Collapse_WhileInTheRiver_GoesToCollapsed()
        {
            _body.Lose(_config.StartingWasps - 100);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
            Assert.That(_forms.IsPiperRooted, Is.False);
        }

        [Test]
        public void Collapse_WhileStinging_EndsTheSting()
        {
            Start(SwarmForm.Sting, 4f);

            _body.Lose(_config.StartingWasps - 100);

            Assert.That(_forms.Slot(SongSlot.Strike).IsActive, Is.False);
        }

        [Test]
        public void Start_WhileCollapsed_IsRefused()
        {
            _body.Lose(_config.StartingWasps - 100);
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
            _forms.Tick(1f, 2);
            Assert.That(_fury.Value, Is.EqualTo(0f), "a refused tornado fills no Fury");
        }

        [Test]
        public void Tick_WhileCollapsed_StaysCollapsedWithoutTheBodyRespawning()
        {
            _body.Lose(_config.StartingWasps - 100);

            _forms.Tick(1f, 2);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
        }

        [Test]
        public void Respawn_AfterTheBlock_GoesBackToTheRiver()
        {
            _body.Lose(_config.StartingWasps - 100);
            _changes.Clear();

            _body.Tick(3f, 2);

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
            Assert.That(() => _forms.Tick(0.016f, 2), Is.Not.AllocatingGCMemory());

            _forms.StartHiveCall();
            Assert.That(() => _forms.Tick(0.016f, 2), Is.Not.AllocatingGCMemory());
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
