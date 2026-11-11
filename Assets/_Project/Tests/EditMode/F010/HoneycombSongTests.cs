using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class HoneycombSongTests
    {
        private const int HoleCount = 4;

        // H J L K.
        private static readonly int[] Keys = { 0, 1, 3, 2 };

        private static readonly Vector3 WestHive = new Vector3(-10f, 0f, 0f);
        private static readonly Vector3 EastHive = new Vector3(10f, 0f, 0f);

        private SwarmConfig _swarmConfig;
        private FuryConfig _furyConfig;
        private HiveConfig _hiveConfig;
        private SongConfig _songConfig;
        private SquadConfig _squads;
        private SwarmBody _body;
        private FuryMeter _fury;
        private SwarmFormMachine _forms;
        private HiveSet _hives;
        private HoneycombSong _song;
        private SwarmSongCaster _caster;
        private readonly List<ISwarmTarget> _enemies = new List<ISwarmTarget>();
        private readonly List<SwarmSongEffect> _songs = new List<SwarmSongEffect>();
        private Vector3 _aimPoint;
        private bool _isFarm;
        private float _sizeScale;
        private float _damageScale;

        [SetUp]
        public void SetUp()
        {
            _swarmConfig = ScriptableObject.CreateInstance<SwarmConfig>();
            _furyConfig = ScriptableObject.CreateInstance<FuryConfig>();
            _hiveConfig = ScriptableObject.CreateInstance<HiveConfig>();
            _songConfig = ScriptableObject.CreateInstance<SongConfig>();
            _squads = ScriptableObject.CreateInstance<SquadConfig>();
            _body = new SwarmBody(_swarmConfig);
            _fury = new FuryMeter(_furyConfig);
            _forms = new SwarmFormMachine(_body, _fury, _squads);
            _hives = new HiveSet(new List<Hive> { new Hive(_hiveConfig, WestHive), new Hive(_hiveConfig, EastHive) });
            _song = ScriptableObject.CreateInstance<HoneycombSong>();
            _songs.Clear();
            _songs.Add(ScriptableObject.CreateInstance<DomeSong>());
            _songs.Add(ScriptableObject.CreateInstance<StingSong>());
            _songs.Add(ScriptableObject.CreateInstance<TornadoSong>());
            _songs.Add(ScriptableObject.CreateInstance<GustSong>());
            _songs.Add(ScriptableObject.CreateInstance<HiveCallSong>());
            _songs.Add(ScriptableObject.CreateInstance<ColossusSong>());
            _songs.Add(_song);
            _caster = new SwarmSongCaster(_songs, _songConfig, HoleCount);
            _enemies.Clear();
            _aimPoint = EastHive;
            _isFarm = false;
            _sizeScale = 1f;
            _damageScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_swarmConfig);
            Object.DestroyImmediate(_furyConfig);
            Object.DestroyImmediate(_hiveConfig);
            Object.DestroyImmediate(_songConfig);
            Object.DestroyImmediate(_squads);

            foreach (SwarmSongEffect song in _songs)
            {
                Object.DestroyImmediate(song);
            }
        }

        [Test]
        public void Defaults_MatchTheSpec()
        {
            Assert.That(_song.Song.Title, Is.EqualTo("Honeycomb"));
            Assert.That(_song.Song.CooldownSeconds, Is.EqualTo(15f));
            Assert.That(_song.Song.Keys, Is.EqualTo(Keys));
            Assert.That(_song.Category, Is.EqualTo(SongCategory.Defense));
            Assert.That(_song.WaspCost, Is.EqualTo(150));
            Assert.That(_song.Slot, Is.EqualTo(SongSlot.Post));
            Assert.That(_song.Form, Is.EqualTo(SwarmForm.Honeycomb));
            Assert.That(_song.Share, Is.EqualTo(0.2f));
            Assert.That(_song.IsAimed, Is.True);
            Assert.That(_song.AimRule, Is.EqualTo(AimRule.Hive));
            Assert.That(_song.FarmSeconds, Is.EqualTo(60f));
            Assert.That(_song.DamageFraction, Is.EqualTo(0.25f));
            Assert.That(_song.ThornDamage, Is.EqualTo(10f));
            Assert.That(_song.ShellRadius, Is.EqualTo(1.6f));
            Assert.That(_song.BurstRadius, Is.EqualTo(2.2f));
        }

        [Test]
        public void Keys_PlayedFromSilence_CastExactlyHoneycomb()
        {
            var cast = new List<int>();
            _caster.SongCast += cast.Add;

            int result = PlayAll();

            Assert.That(result, Is.EqualTo(6));
            Assert.That(cast, Is.EqualTo(new[] { 6 }), "one cast, and it is Honeycomb");
            Assert.That(Post().IsActive, Is.True);
            Assert.That(Post().Form, Is.EqualTo(SwarmForm.Honeycomb));
        }

        [Test]
        public void Press_WhileOnCooldown_CannotBeRecast()
        {
            PlayAll();
            _caster.Tick(14f);

            Assert.That(PlayAll(), Is.EqualTo(SongBook.NoSong));

            _caster.Tick(1.1f);

            Assert.That(PlayAll(), Is.EqualTo(6), "ready after fifteen seconds");
        }

        // The Post slot

        [Test]
        public void Cast_RunsInThePostSlotAndGivesItsNumbersToThePicture()
        {
            Cast();

            SlotState post = Post();
            Assert.That(post.IsActive, Is.True);
            Assert.That(post.Category, Is.EqualTo(SongCategory.Defense));
            Assert.That(post.Low, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(post.High, Is.EqualTo(0.2f).Within(1e-5f), "20% of the swarm from the start of the Post band");
            Assert.That(post.ParamA, Is.EqualTo(new Vector4(10f, 0f, 0f, 1.6f)), "the hive and the shell's wall");
            Assert.That(post.ParamB, Is.EqualTo(new Vector4(0f, -1f, 2.2f, 0f)), "no stick hit yet, and the burst radius");
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River), "the rest of the swarm stays the river");
        }

        [Test]
        public void Cast_ReplacesWhateverStoodInThePostSlot_AndKeepsTheGuardAndStrikeSongs()
        {
            Cast<DomeSong>();
            Cast<StingSong>();
            Cast();
            Assert.That(Guard().Form, Is.EqualTo(SwarmForm.Dome));
            Assert.That(Strike().Form, Is.EqualTo(SwarmForm.Sting));
            Assert.That(_forms.HiveDamageFraction(1), Is.EqualTo(0.25f).Within(1e-5f));

            _aimPoint = WestHive;
            Cast();

            Assert.That(Post().ParamA, Is.EqualTo(new Vector4(-10f, 0f, 0f, 1.6f)));
            Assert.That(_forms.HiveDamageFraction(0), Is.EqualTo(0.25f).Within(1e-5f));
            Assert.That(_forms.HiveDamageFraction(1), Is.EqualTo(1f), "the shell moved");
            Assert.That(Guard().Form, Is.EqualTo(SwarmForm.Dome));
            Assert.That(Strike().Form, Is.EqualTo(SwarmForm.Sting));
        }

        [Test]
        public void Cast_OverANest_ReplacesIt()
        {
            Cast<NestSong>();
            Assert.That(Post().Form, Is.EqualTo(SwarmForm.Nest));

            Cast();

            Assert.That(Post().Form, Is.EqualTo(SwarmForm.Honeycomb));
        }

        // Which hive

        [Test]
        public void Cast_AsksAutoAimForTheHiveRule_AndShieldsTheHiveItsPointIsOn()
        {
            var aim = new FakeSongAim { Point = WestHive };

            SwarmSongContext context = Context(aim);
            _song.Cast(in context);

            Assert.That(aim.Calls, Is.EqualTo(1));
            Assert.That(aim.LastRule, Is.EqualTo(AimRule.Hive));
            Assert.That(aim.LastRange, Is.EqualTo(0f), "the hive rule has no reach");
            Assert.That(Post().ParamA, Is.EqualTo(new Vector4(-10f, 0f, 0f, 1.6f)));
            Assert.That(_forms.HiveDamageFraction(0), Is.EqualTo(0.25f).Within(1e-5f));
        }

        [Test]
        public void Cast_ShieldsOnlyTheChosenHive()
        {
            _aimPoint = new Vector3(-9f, 0f, 3f);

            Cast();

            Assert.That(_forms.HiveDamageFraction(0), Is.EqualTo(0.25f).Within(1e-5f), "the nearest hive to the point");
            Assert.That(_forms.HiveDamageFraction(1), Is.EqualTo(1f), "the other hive is not protected");
        }

        [Test]
        public void Cast_WithTheChosenPointOnADestroyedHive_ShieldsTheNearestStandingHive()
        {
            _hives[1].Damage(_hives[1].MaxHealth);
            _aimPoint = EastHive;

            Cast();

            Assert.That(Post().ParamA, Is.EqualTo(new Vector4(-10f, 0f, 0f, 1.6f)));
            Assert.That(_forms.HiveDamageFraction(0), Is.EqualTo(0.25f).Within(1e-5f));
            Assert.That(_forms.HiveDamageFraction(1), Is.EqualTo(1f));
        }

        [Test]
        public void Cast_WithNoStandingHive_LetsGoAtOnceAndShieldsNothing()
        {
            _hives[0].Damage(_hives[0].MaxHealth);
            _hives[1].Damage(_hives[1].MaxHealth);

            Cast();

            Assert.That(Post().ReleasedSeconds, Is.EqualTo(0f));
            Assert.That(_forms.HiveDamageFraction(0), Is.EqualTo(1f));
            Assert.That(_forms.HiveDamageFraction(1), Is.EqualTo(1f));
        }

        // The shell

        [Test]
        public void Shell_LetsThroughAQuarterOfTheDamageFromSticksAndSmoke()
        {
            Cast();

            IDamageShield shield = _forms;

            Assert.That(shield.HiveDamageFraction(1), Is.EqualTo(0.25f).Within(1e-5f), "75% less");
            Assert.That(shield.HiveDamageFraction(0), Is.EqualTo(1f));
            Assert.That(shield.WaspLossFraction, Is.EqualTo(1f), "it shields the hive, not the core");
        }

        [Test]
        public void Shell_StacksWithADomeOnlyOnTheHive_NotOnTheCoreLoss()
        {
            Cast<DomeSong>();
            Cast();

            Assert.That(_forms.HiveDamageFraction(1), Is.EqualTo(0.25f).Within(1e-5f), "the dome does not touch the hives");
            Assert.That(_forms.WaspLossFraction, Is.EqualTo(0.15f).Within(1e-5f), "the shell does not touch the core");
        }

        [Test]
        public void Thorns_AStickHittingTheShieldedHive_TakesTenDamage()
        {
            var stick = new FakeSwarmTarget { Position = new Vector3(10f, 0f, 5f) };
            Cast();
            _forms.Tick(2f, _hives);

            _forms.NotifyHiveHit(1, stick);

            Assert.That(stick.DamageTaken, Is.EqualTo(10f));
            Assert.That(Post().ParamB.x, Is.EqualTo(Mathf.PI / 2f).Within(1e-4f), "the hit came from +z of the hive");
            Assert.That(Post().ParamB.y, Is.EqualTo(2f).Within(1e-4f), "two seconds after the cast");
            Assert.That(_fury.Value, Is.EqualTo(10f / _furyConfig.DamagePerFuryPoint).Within(1e-3f));

            _forms.NotifyHiveHit(1, stick);

            Assert.That(stick.DamageTaken, Is.EqualTo(20f), "each hit pays");
        }

        [Test]
        public void Thorns_AStickHittingTheOtherHive_TakesNothing()
        {
            var stick = new FakeSwarmTarget { Position = new Vector3(-10f, 0f, 5f) };
            Cast();

            _forms.NotifyHiveHit(0, stick);

            Assert.That(stick.DamageTaken, Is.EqualTo(0f));
            Assert.That(Post().ParamB.y, Is.EqualTo(-1f));
        }

        [Test]
        public void Thorns_ADeadStick_TakesNothingAndTheHitIsNotShown()
        {
            var stick = new FakeSwarmTarget { Position = new Vector3(10f, 0f, 5f), IsAlive = false };
            Cast();

            _forms.NotifyHiveHit(1, stick);

            Assert.That(stick.DamageTaken, Is.EqualTo(0f));
            Assert.That(Post().ParamB.y, Is.EqualTo(-1f));
        }

        [Test]
        public void Thorns_APooledBeekeeperThatIsNotAlive_IsIgnored()
        {
            var config = ScriptableObject.CreateInstance<BeekeeperConfig>();
            try
            {
                var brain = new BeekeeperBrain(config);
                Cast();

                Assert.DoesNotThrow(() => _forms.NotifyHiveHit(1, brain));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Thorns_AfterTheShellEnded_TakeNothing()
        {
            var stick = new FakeSwarmTarget { Position = new Vector3(10f, 0f, 5f) };
            Cast();
            _forms.NotifyPhase(MatchPhase.Farm);

            _forms.NotifyHiveHit(1, stick);

            Assert.That(stick.DamageTaken, Is.EqualTo(0f));
        }

        // How long it lasts

        [Test]
        public void Duration_CastInAWave_LastsPastSixtySecondsUntilTheWaveEnds()
        {
            Cast();

            _forms.Tick(100f, _hives);

            Assert.That(Post().IsActive, Is.True);
            Assert.That(Post().ReleasedSeconds, Is.EqualTo(-1f));
            Assert.That(_forms.HiveDamageFraction(1), Is.EqualTo(0.25f).Within(1e-5f));

            _forms.NotifyPhase(MatchPhase.Farm);

            Assert.That(Post().ReleasedSeconds, Is.GreaterThanOrEqualTo(0f), "the wave is over");
            Assert.That(_forms.HiveDamageFraction(1), Is.EqualTo(1f));

            _forms.Tick(1f, _hives);
            Assert.That(Post().IsActive, Is.False, "the return beat is over");
        }

        [Test]
        public void Duration_CastInAWave_AFurtherSurviveNoticeChangesNothing()
        {
            Cast();
            _forms.Tick(10f, _hives);

            _forms.NotifyPhase(MatchPhase.Survive);
            _forms.Tick(100f, _hives);

            Assert.That(Post().ReleasedSeconds, Is.EqualTo(-1f));
        }

        [Test]
        public void Duration_CastInAFarmPhase_EndsAtSixtySeconds()
        {
            _isFarm = true;
            Cast();

            _forms.Tick(59f, _hives);
            Assert.That(Post().ReleasedSeconds, Is.EqualTo(-1f), "still standing at 59 s");

            _forms.Tick(1.5f, _hives);

            Assert.That(Post().ReleasedSeconds, Is.GreaterThanOrEqualTo(0f));
            Assert.That(_forms.HiveDamageFraction(1), Is.EqualTo(1f));
        }

        [Test]
        public void Duration_CastInAFarmPhase_ALongStepStillEndsItOnce()
        {
            _isFarm = true;
            Cast();

            _forms.Tick(200f, _hives);

            Assert.That(Post().IsActive, Is.False);
        }

        [Test]
        public void Duration_CastInAFarmPhase_AWaveThatStartsFirstMakesItLastUntilThatWaveEnds()
        {
            _isFarm = true;
            Cast();
            _forms.Tick(30f, _hives);

            _forms.NotifyPhase(MatchPhase.Survive);
            _forms.Tick(100f, _hives);

            Assert.That(Post().ReleasedSeconds, Is.EqualTo(-1f), "past 60 s, and the wave goes on");

            _forms.NotifyPhase(MatchPhase.Farm);

            Assert.That(Post().ReleasedSeconds, Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void Duration_CastInAFarmPhase_ANoticeOfTheFarmPhaseChangesNothing()
        {
            _isFarm = true;
            Cast();
            _forms.Tick(10f, _hives);

            _forms.NotifyPhase(MatchPhase.Farm);
            _forms.Tick(10f, _hives);

            Assert.That(Post().ReleasedSeconds, Is.EqualTo(-1f));
        }

        [Test]
        public void Duration_TheHiveIsDestroyed_EndsItAtOnce()
        {
            Cast();
            _forms.Tick(5f, _hives);

            _hives[1].Damage(_hives[1].MaxHealth);
            _forms.Tick(0.1f, _hives);

            Assert.That(Post().ReleasedSeconds, Is.GreaterThanOrEqualTo(0f));
            Assert.That(_forms.HiveDamageFraction(1), Is.EqualTo(1f));
        }

        [Test]
        public void Duration_TheFatalStickHit_EndsItWithNoThorns()
        {
            var stick = new FakeSwarmTarget { Position = new Vector3(10f, 0f, 5f) };
            Cast();
            _hives[1].Damage(_hives[1].MaxHealth);

            _forms.NotifyHiveHit(1, stick);

            Assert.That(Post().ReleasedSeconds, Is.GreaterThanOrEqualTo(0f));
            Assert.That(stick.DamageTaken, Is.EqualTo(0f));
        }

        [Test]
        public void Duration_TheOtherHiveBeingDestroyed_ChangesNothing()
        {
            Cast();

            _hives[0].Damage(_hives[0].MaxHealth);
            _forms.Tick(5f, _hives);

            Assert.That(Post().ReleasedSeconds, Is.EqualTo(-1f));
        }

        // The swarm's level

        [Test]
        public void Level_GrowsTheShellAndTheThorns_ButNotTheShare()
        {
            _sizeScale = 2f;
            _damageScale = 1.5f;
            var stick = new FakeSwarmTarget { Position = new Vector3(10f, 0f, 5f) };

            Cast();
            _forms.NotifyHiveHit(1, stick);

            Assert.That(Post().ParamA.w, Is.EqualTo(3.2f).Within(1e-5f));
            Assert.That(Post().ParamB.z, Is.EqualTo(4.4f).Within(1e-5f));
            Assert.That(stick.DamageTaken, Is.EqualTo(15f).Within(1e-5f));
            Assert.That(_forms.HiveDamageFraction(1), Is.EqualTo(0.25f).Within(1e-5f), "75% less at every level");
        }

        // The Colossus and the collapse

        [Test]
        public void Colossus_KeepsTheShell()
        {
            Cast();

            _forms.StartColossus(12f);

            Assert.That(Post().IsActive, Is.True);
            Assert.That(_forms.HiveDamageFraction(1), Is.EqualTo(0.25f).Within(1e-5f));
        }

        [Test]
        public void Collapse_EndsTheShellAtOnce()
        {
            Cast();

            _body.Lose(_body.Count);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
            Assert.That(Post().IsActive, Is.False);
            Assert.That(_forms.HiveDamageFraction(1), Is.EqualTo(1f));
        }

        [Test]
        public void Cast_WhileTheColossusStands_ChangesNothing()
        {
            _forms.StartColossus(12f);

            Cast();

            Assert.That(Post().IsActive, Is.False);
        }

        // Helpers

        private SlotState Post() => _forms.Slot(SongSlot.Post);

        private SlotState Guard() => _forms.Slot(SongSlot.Guard);

        private SlotState Strike() => _forms.Slot(SongSlot.Strike);

        private void Cast()
        {
            SwarmSongContext context = Context();
            _song.Cast(in context);
        }

        private void Cast<T>() where T : SlotSongEffect
        {
            T other = ScriptableObject.CreateInstance<T>();
            _songs.Add(other);
            SwarmSongContext context = Context();
            other.Cast(in context);
        }

        private int PlayAll()
        {
            int result = SongBook.NoSong;
            SwarmSongContext context = Context();

            foreach (int key in Keys)
            {
                result = _caster.Press(key, in context);
            }

            return result;
        }

        private SwarmSongContext Context(ISongAim aim = null)
        {
            return new SwarmSongContext(Vector3.zero, null, _aimPoint, _body, _forms, _fury, _hives, _enemies, null, aim, _isFarm, _sizeScale, _damageScale);
        }
    }
}
