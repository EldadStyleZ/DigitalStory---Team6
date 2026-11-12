using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class NestSongTests
    {
        private const int HoleCount = 4;

        // K L J H.
        private static readonly int[] Keys = { 2, 3, 1, 0 };

        private SwarmConfig _swarmConfig;
        private FuryConfig _furyConfig;
        private HiveConfig _hiveConfig;
        private SongConfig _songConfig;
        private SquadConfig _squads;
        private SwarmBody _body;
        private FuryMeter _fury;
        private SwarmFormMachine _forms;
        private HiveSet _hives;
        private NestSong _song;
        private SwarmSongCaster _caster;
        private readonly List<ISwarmTarget> _enemies = new List<ISwarmTarget>();
        private readonly List<SwarmSongEffect> _songs = new List<SwarmSongEffect>();
        private readonly List<Vector3> _landings = new List<Vector3>();
        private ISwarmTarget _aim;
        private Vector3 _aimPoint;
        private Vector3 _core;
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
            _hives = new HiveSet(new List<Hive> { new Hive(_hiveConfig, new Vector3(-10f, 0f, 0f)), new Hive(_hiveConfig, new Vector3(10f, 0f, 0f)) });
            _song = ScriptableObject.CreateInstance<NestSong>();
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
            _landings.Clear();
            _aim = null;
            _aimPoint = new Vector3(6f, 0f, 0f);
            _core = Vector3.zero;
            _sizeScale = 1f;
            _damageScale = 1f;
            _forms.Landed += (form, point) =>
            {
                if (form == SwarmForm.Nest)
                {
                    _landings.Add(point);
                }
            };
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
            Assert.That(_song.Song.Title, Is.EqualTo("Nest"));
            Assert.That(_song.Song.CooldownSeconds, Is.EqualTo(8f));
            Assert.That(_song.Song.Keys, Is.EqualTo(Keys));
            Assert.That(_song.Category, Is.EqualTo(SongCategory.Attack));
            Assert.That(_song.WaspCost, Is.EqualTo(200), "the playtest bot after plan section O: was 100");
            Assert.That(_song.Slot, Is.EqualTo(SongSlot.Post));
            Assert.That(_song.Form, Is.EqualTo(SwarmForm.Nest));
            Assert.That(_song.Share, Is.EqualTo(0.15f));
            Assert.That(_song.IsAimed, Is.True);
            Assert.That(_song.AimRule, Is.EqualTo(AimRule.Crowd));
            Assert.That(_song.AimRange, Is.EqualTo(10f));
            Assert.That(_song.MaxRange, Is.EqualTo(10f));
            Assert.That(_song.ArmSeconds, Is.EqualTo(0.6f));
            Assert.That(_song.TriggerRadius, Is.EqualTo(2.5f));
            Assert.That(_song.FuseSeconds, Is.EqualTo(0.3f));
            Assert.That(_song.BlastRadius, Is.EqualTo(3.5f));
            Assert.That(_song.Damage, Is.EqualTo(70f));
            Assert.That(_song.StunSeconds, Is.EqualTo(1f));
            Assert.That(_song.MaxSeconds, Is.EqualTo(60f));
        }

        [Test]
        public void Keys_PlayedFromSilence_CastExactlyNest()
        {
            var cast = new List<int>();
            _caster.SongCast += cast.Add;

            int result = PlayAll();

            Assert.That(result, Is.EqualTo(6));
            Assert.That(cast, Is.EqualTo(new[] { 6 }), "one cast, and it is Nest");
            Assert.That(Post().IsActive, Is.True);
            Assert.That(Post().Form, Is.EqualTo(SwarmForm.Nest));
        }

        [Test]
        public void Press_WhileOnCooldown_CannotBeRecast()
        {
            PlayAll();
            _caster.Tick(7f);

            Assert.That(PlayAll(), Is.EqualTo(SongBook.NoSong));

            _caster.Tick(1.1f);

            Assert.That(PlayAll(), Is.EqualTo(6), "ready after eight seconds");
        }

        // The Post slot

        [Test]
        public void Cast_RunsInThePostSlotAndGivesItsNumbersToThePicture()
        {
            _aimPoint = new Vector3(6f, 0f, 2f);

            Cast();

            SlotState post = Post();
            Assert.That(post.IsActive, Is.True);
            Assert.That(post.Category, Is.EqualTo(SongCategory.Attack));
            Assert.That(post.Low, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(post.High, Is.EqualTo(0.15f).Within(1e-5f), "15% of the swarm from the start of the Post band");
            Assert.That(post.ParamA, Is.EqualTo(new Vector4(6f, 0f, 2f, -1f)), "where it lies, and not set off yet");
            Assert.That(post.ParamB, Is.EqualTo(new Vector4(2.5f, 3.5f, 0.3f, 0f)), "the trigger radius, the burst radius and the fuse");
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
            Assert.That(Post().Form, Is.EqualTo(SwarmForm.Nest));

            _aimPoint = new Vector3(-4f, 0f, 3f);
            Cast();

            Assert.That(Post().ParamA, Is.EqualTo(new Vector4(-4f, 0f, 3f, -1f)), "the new nest stands where the old one stood");
            Assert.That(Post().ParamA.w, Is.EqualTo(-1f));
            Assert.That(Guard().Form, Is.EqualTo(SwarmForm.Dome));
            Assert.That(Strike().Form, Is.EqualTo(SwarmForm.Sting));
        }

        [Test]
        public void Cast_OverAHoneycomb_ReplacesIt()
        {
            var honeycomb = ScriptableObject.CreateInstance<HoneycombSong>();
            _songs.Add(honeycomb);
            SwarmSongContext context = Context();
            honeycomb.Cast(in context);
            Assert.That(Post().Form, Is.EqualTo(SwarmForm.Honeycomb));
            Assert.That(_forms.HiveDamageFraction(1), Is.EqualTo(0.25f).Within(1e-5f));

            Cast();

            Assert.That(Post().Form, Is.EqualTo(SwarmForm.Nest));
            Assert.That(_forms.HiveDamageFraction(1), Is.EqualTo(1f), "the replaced shell protects nothing");
        }

        [Test]
        public void Cast_AsksAutoAimForTheCrowdRuleWithinTenMeters_AndTenTimesTheSizeScale()
        {
            var aim = new FakeSongAim { Point = new Vector3(2f, 0f, 2f) };
            _sizeScale = 1.5f;

            SwarmSongContext context = Context(aim);
            _song.Cast(in context);

            Assert.That(aim.Calls, Is.EqualTo(1));
            Assert.That(aim.LastRule, Is.EqualTo(AimRule.Crowd));
            Assert.That(aim.LastRange, Is.EqualTo(15f).Within(1e-4f));
            Assert.That(Post().ParamA, Is.EqualTo(new Vector4(2f, 0f, 2f, -1f)));
        }

        // Where it lands

        [Test]
        public void Position_WithAnAimedBeekeeper_IsItsPosition()
        {
            _aim = new FakeSwarmTarget { Position = new Vector3(5f, 0f, 2f) };
            _aimPoint = new Vector3(-8f, 0f, 0f);

            Cast();

            Assert.That(Position(), Is.EqualTo(new Vector3(5f, 0f, 2f)));
        }

        [Test]
        public void Position_WithNoLivingBeekeeperAimedAt_IsTheAimPoint()
        {
            _aim = new FakeSwarmTarget { Position = new Vector3(5f, 0f, 2f), IsAlive = false };
            _aimPoint = new Vector3(-8f, 0f, 3f);

            Cast();

            Assert.That(Position(), Is.EqualTo(new Vector3(-8f, 0f, 3f)));
        }

        [Test]
        public void Position_BeyondTenMeters_IsPulledInAlongTheGround()
        {
            _core = new Vector3(10f, 0f, 10f);
            _aimPoint = new Vector3(40f, 0f, 10f);

            Cast();

            Assert.That(Position().x, Is.EqualTo(20f).Within(1e-3f));
            Assert.That(Position().z, Is.EqualTo(10f).Within(1e-3f));
        }

        [Test]
        public void Position_ExactlyTenMeters_IsKept()
        {
            _aimPoint = new Vector3(0f, 0f, -10f);

            Cast();

            Assert.That(Position(), Is.EqualTo(new Vector3(0f, 0f, -10f)));
        }

        [Test]
        public void Position_NeverMovesAfterTheLanding()
        {
            var walker = new FakeSwarmTarget { Position = new Vector3(6f, 0f, 8f) };
            _enemies.Add(walker);
            _aim = walker;
            Cast();

            walker.Position = new Vector3(9f, 0f, 9f);
            _forms.Tick(0.3f, _hives);

            Assert.That(Position(), Is.EqualTo(new Vector3(6f, 0f, 8f)));
        }

        // Setting it off

        [Test]
        public void Tick_WithNobodyNear_StaysArmedAndHurtsNobody()
        {
            var far = new FakeSwarmTarget { Position = new Vector3(6f, 0f, 2.6f) };
            _enemies.Add(far);
            Cast();

            _forms.Tick(5f, _hives);

            Assert.That(Post().IsActive, Is.True);
            Assert.That(Post().ReleasedSeconds, Is.EqualTo(-1f));
            Assert.That(Position4().w, Is.EqualTo(-1f));
            Assert.That(far.DamageTaken, Is.EqualTo(0f));
            Assert.That(_landings, Is.Empty);
        }

        [Test]
        public void Tick_ABeekeeperWithinTheTriggerRadiusOfAnArmedNest_SetsItOffAndTheBurstComesPointThreeSecondsLater()
        {
            var enemy = new FakeSwarmTarget { Position = new Vector3(6f, 0f, 2f) };
            _enemies.Add(enemy);
            Cast();
            _forms.Tick(0.5f, _hives);
            Assert.That(Position4().w, Is.EqualTo(-1f), "not armed yet");

            _forms.Tick(0.2f, _hives);

            Assert.That(Position4().w, Is.EqualTo(0.6f).Within(1e-4f), "the one who was there as it armed sets it off when it arms");
            Assert.That(enemy.DamageTaken, Is.EqualTo(0f), "the fuse is still burning");
            Assert.That(_landings, Is.Empty);

            _forms.Tick(0.15f, _hives);
            Assert.That(enemy.DamageTaken, Is.EqualTo(0f), "0.85 s: 0.05 s to go");

            _forms.Tick(0.1f, _hives);

            Assert.That(enemy.DamageTaken, Is.EqualTo(70f));
            Assert.That(_landings, Is.EqualTo(new[] { new Vector3(6f, 0f, 0f) }));
        }

        // The picture erupts at the set-off plus the fuse it is sent, so a tuned fuse must move the hurt with it: an impact
        // hurts on the frame it visibly lands.
        [Test]
        public void Tick_WithATunedFuse_ThePictureGetsTheFuse_AndTheBurstComesThatLongAfterTheSetOff()
        {
            JsonUtility.FromJsonOverwrite("{\"<FuseSeconds>k__BackingField\":0.5}", _song);
            var enemy = new FakeSwarmTarget { Position = new Vector3(6f, 0f, 2f) };
            _enemies.Add(enemy);
            Cast();

            Assert.That(Post().ParamB.z, Is.EqualTo(0.5f), "the picture's fuse");

            _forms.Tick(0.7f, _hives);
            Assert.That(Position4().w, Is.EqualTo(0.6f).Within(1e-4f));

            _forms.Tick(0.35f, _hives);
            Assert.That(enemy.DamageTaken, Is.EqualTo(0f), "1.05 s: the burst is due at 1.1 s");

            _forms.Tick(0.1f, _hives);
            Assert.That(enemy.DamageTaken, Is.EqualTo(70f));
        }

        [Test]
        public void Tick_ABeekeeperWhoWalksInAfterItArmed_SetsItOffAtThatMoment()
        {
            var enemy = new FakeSwarmTarget { Position = new Vector3(6f, 0f, 9f) };
            _enemies.Add(enemy);
            Cast();
            _forms.Tick(1f, _hives);

            enemy.Position = new Vector3(6f, 0f, 2.4f);
            _forms.Tick(0.1f, _hives);

            Assert.That(Position4().w, Is.EqualTo(1.1f).Within(1e-4f));
            _forms.Tick(0.2f, _hives);
            Assert.That(enemy.DamageTaken, Is.EqualTo(0f), "1.3 s: the burst is due at 1.4 s");

            _forms.Tick(0.2f, _hives);

            Assert.That(enemy.DamageTaken, Is.EqualTo(70f));
        }

        [Test]
        public void Tick_ExactlyTwoAndAHalfMetersAway_SetsItOff()
        {
            _enemies.Add(new FakeSwarmTarget { Position = new Vector3(6f, 0f, 2.5f) });
            Cast();

            _forms.Tick(0.7f, _hives);

            Assert.That(Position4().w, Is.EqualTo(0.6f).Within(1e-4f));
        }

        [Test]
        public void Tick_ADeadBeekeeperNearIt_DoesNotSetItOff()
        {
            _enemies.Add(new FakeSwarmTarget { Position = new Vector3(6f, 0f, 1f), IsAlive = false });
            Cast();

            _forms.Tick(3f, _hives);

            Assert.That(Position4().w, Is.EqualTo(-1f));
            Assert.That(Post().ReleasedSeconds, Is.EqualTo(-1f));
        }

        [Test]
        public void Tick_AHeightedBeekeeper_CountsByItsPlaceOnTheGround()
        {
            _enemies.Add(new FakeSwarmTarget { Position = new Vector3(6f, 7f, 1f) });
            Cast();

            _forms.Tick(0.7f, _hives);

            Assert.That(Position4().w, Is.EqualTo(0.6f).Within(1e-4f));
        }

        [Test]
        public void Tick_APooledBeekeeperThatHasNotSpawned_DoesNotSetItOff()
        {
            var config = ScriptableObject.CreateInstance<BeekeeperConfig>();
            try
            {
                _aimPoint = Vector3.zero;
                _enemies.Add(new BeekeeperBrain(config));
                Cast();

                _forms.Tick(3f, _hives);

                Assert.That(Position4().w, Is.EqualTo(-1f));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        // The burst

        [Test]
        public void Burst_HurtsAndStunsEveryLivingBeekeeperWithinThreeAndAHalfMeters_NotOnlyTheOneThatSetItOff()
        {
            var trigger = new FakeSwarmTarget { Position = new Vector3(6f, 0f, 2f) };
            var onTheEdge = new FakeSwarmTarget { Position = new Vector3(9.5f, 0f, 0f) };
            var outside = new FakeSwarmTarget { Position = new Vector3(9.6f, 0f, 0f) };
            var dead = new FakeSwarmTarget { Position = new Vector3(6f, 0f, 1f), IsAlive = false };
            _enemies.Add(trigger);
            _enemies.Add(onTheEdge);
            _enemies.Add(outside);
            _enemies.Add(dead);
            Cast();

            _forms.Tick(1.2f, _hives);

            Assert.That(trigger.DamageTaken, Is.EqualTo(70f));
            Assert.That(trigger.LastStunSeconds, Is.EqualTo(1f));
            Assert.That(onTheEdge.DamageTaken, Is.EqualTo(70f), "3.5 m counts");
            Assert.That(onTheEdge.LastStunSeconds, Is.EqualTo(1f));
            Assert.That(outside.DamageTaken, Is.EqualTo(0f));
            Assert.That(outside.LastStunSeconds, Is.EqualTo(0f));
            Assert.That(dead.DamageTaken, Is.EqualTo(0f));
        }

        [Test]
        public void Burst_MeasuresFromTheNestAtTheMomentOfTheBurst_SoWhoWalksAwayEscapesAndWhoWalksInIsHit()
        {
            var leaver = new FakeSwarmTarget { Position = new Vector3(6f, 0f, 2f) };
            var arriver = new FakeSwarmTarget { Position = new Vector3(6f, 0f, 20f) };
            _enemies.Add(leaver);
            _enemies.Add(arriver);
            Cast();
            _forms.Tick(0.7f, _hives);

            leaver.Position = new Vector3(6f, 0f, 6f);
            arriver.Position = new Vector3(6f, 0f, -3f);
            _forms.Tick(0.3f, _hives);

            Assert.That(leaver.DamageTaken, Is.EqualTo(0f));
            Assert.That(arriver.DamageTaken, Is.EqualTo(70f));
        }

        [Test]
        public void Burst_FuryRisesByDamagePerFuryPointForEachBeekeeperHit()
        {
            _enemies.Add(new FakeSwarmTarget { Position = new Vector3(6f, 0f, 1f) });
            _enemies.Add(new FakeSwarmTarget { Position = new Vector3(6f, 0f, -2f) });
            _enemies.Add(new FakeSwarmTarget { Position = new Vector3(30f, 0f, 0f) });
            Cast();

            _forms.Tick(1.2f, _hives);

            Assert.That(_fury.Value, Is.EqualTo(140f / _furyConfig.DamagePerFuryPoint).Within(1e-3f));
        }

        [Test]
        public void Burst_HappensOnceThenTheRuleLetsGoAndTheSlotEmptiesAfterItsReturnBeat()
        {
            var enemy = new FakeSwarmTarget { Position = new Vector3(6f, 0f, 1f) };
            _enemies.Add(enemy);
            Cast();

            _forms.Tick(5f, _hives);
            _forms.Tick(5f, _hives);

            Assert.That(enemy.DamageTaken, Is.EqualTo(70f), "one burst only");
            Assert.That(_landings.Count, Is.EqualTo(1));
            Assert.That(Post().IsActive, Is.False);
            Assert.That(Post().High, Is.EqualTo(Post().Low), "no wasps left in the squad");
        }

        [Test]
        public void Burst_InTheReturnBeat_HurtsNobodyMore()
        {
            var enemy = new FakeSwarmTarget { Position = new Vector3(6f, 0f, 1f) };
            _enemies.Add(enemy);
            Cast();
            _forms.Tick(1.0f, _hives);
            Assert.That(enemy.DamageTaken, Is.EqualTo(70f));
            Assert.That(Post().ReleasedSeconds, Is.GreaterThanOrEqualTo(0f));

            var late = new FakeSwarmTarget { Position = new Vector3(6f, 0f, 0f) };
            _enemies.Add(late);
            _forms.Tick(0.3f, _hives);

            Assert.That(late.DamageTaken, Is.EqualTo(0f));
        }

        // Time limit

        [Test]
        public void Tick_AnUnsetNest_StaysArmedForFiftyNineSecondsAndThenStillWorks()
        {
            var enemy = new FakeSwarmTarget { Position = new Vector3(6f, 0f, 9f) };
            _enemies.Add(enemy);
            Cast();
            _forms.Tick(59f, _hives);
            Assert.That(Post().ReleasedSeconds, Is.EqualTo(-1f));

            enemy.Position = new Vector3(6f, 0f, 1f);
            _forms.Tick(0.5f, _hives);
            _forms.Tick(0.4f, _hives);

            Assert.That(enemy.DamageTaken, Is.EqualTo(70f));
        }

        [Test]
        public void Tick_AnUnsetNest_EndsAtSixtySecondsHurtingNobody()
        {
            var enemy = new FakeSwarmTarget { Position = new Vector3(6f, 0f, 9f) };
            _enemies.Add(enemy);
            Cast();

            _forms.Tick(60.1f, _hives);

            Assert.That(Post().ReleasedSeconds, Is.GreaterThanOrEqualTo(0f));
            enemy.Position = new Vector3(6f, 0f, 0f);
            _forms.Tick(0.3f, _hives);
            _forms.Tick(5f, _hives);
            Assert.That(enemy.DamageTaken, Is.EqualTo(0f));
            Assert.That(_landings, Is.Empty);
            Assert.That(_fury.Value, Is.EqualTo(0f));
            Assert.That(Post().IsActive, Is.False);
        }

        [Test]
        public void Tick_SomeoneWhoArrivesTooLate_DoesNotSetOffANestThatEndedAtSixtySeconds()
        {
            var enemy = new FakeSwarmTarget { Position = new Vector3(6f, 0f, 1f) };
            Cast();
            _forms.Tick(59.5f, _hives);
            _enemies.Add(enemy);

            _forms.Tick(0.6f, _hives);
            _forms.Tick(2f, _hives);

            Assert.That(enemy.DamageTaken, Is.EqualTo(0f));
            Assert.That(_landings, Is.Empty);
        }

        // The swarm's level

        [Test]
        public void Level_GrowsTheRangeTheRadiiAndTheDamage_AndThePictureGetsTheGrownRadii()
        {
            _sizeScale = 2f;
            _damageScale = 1.5f;
            _aimPoint = new Vector3(0f, 0f, 30f);
            var setsItOff = new FakeSwarmTarget { Position = new Vector3(0f, 0f, 24f) };
            var blastOnly = new FakeSwarmTarget { Position = new Vector3(0f, 0f, 26.9f) };
            var beyond = new FakeSwarmTarget { Position = new Vector3(0f, 0f, 27.2f) };
            _enemies.Add(setsItOff);
            _enemies.Add(blastOnly);
            _enemies.Add(beyond);

            Cast();

            Assert.That(Position(), Is.EqualTo(new Vector3(0f, 0f, 20f)), "20 m: the range doubled");
            Assert.That(Post().ParamB, Is.EqualTo(new Vector4(5f, 7f, 0.3f, 0f)), "the radii grow and the fuse does not");

            _forms.Tick(1f, _hives);

            Assert.That(setsItOff.DamageTaken, Is.EqualTo(105f), "4 m is inside the doubled 5 m trigger radius, and the damage is 70 x 1.5");
            Assert.That(blastOnly.DamageTaken, Is.EqualTo(105f), "6.9 m is inside the doubled 7 m burst");
            Assert.That(beyond.DamageTaken, Is.EqualTo(0f));
            Assert.That(_fury.Value, Is.EqualTo(210f / _furyConfig.DamagePerFuryPoint).Within(1e-3f));
        }

        [Test]
        public void Level_AtTheFirstLevel_ABeekeeperFourMetersAway_DoesNotSetItOff()
        {
            _enemies.Add(new FakeSwarmTarget { Position = new Vector3(6f, 0f, 4f) });
            Cast();

            _forms.Tick(1f, _hives);

            Assert.That(Position4().w, Is.EqualTo(-1f));
        }

        // The Colossus and the collapse

        [Test]
        public void Colossus_KeepsTheNest_AndItStillBursts()
        {
            var enemy = new FakeSwarmTarget { Position = new Vector3(6f, 0f, 1f) };
            _enemies.Add(enemy);
            Cast();

            _forms.StartColossus(12f);

            Assert.That(Post().IsActive, Is.True);
            _forms.Tick(1.2f, _hives);
            Assert.That(enemy.DamageTaken, Is.EqualTo(70f));
        }

        [Test]
        public void Collapse_EndsTheNestAtOnceWithNoBurst()
        {
            var enemy = new FakeSwarmTarget { Position = new Vector3(6f, 0f, 1f) };
            _enemies.Add(enemy);
            Cast();
            _forms.Tick(0.7f, _hives);

            _body.Lose(_body.Count);
            _forms.Tick(1f, _hives);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
            Assert.That(Post().IsActive, Is.False);
            Assert.That(enemy.DamageTaken, Is.EqualTo(0f));
            Assert.That(_landings, Is.Empty);
        }

        [Test]
        public void Cast_WhileTheColossusStandsOrTheSwarmIsCollapsed_ChangesNothing()
        {
            _forms.StartColossus(12f);
            Cast();
            Assert.That(Post().IsActive, Is.False);

            _forms.Tick(13f, _hives);
            _body.Lose(_body.Count);
            Cast();

            Assert.That(Post().IsActive, Is.False);
        }

        // Helpers

        private SlotState Post() => _forms.Slot(SongSlot.Post);

        private SlotState Guard() => _forms.Slot(SongSlot.Guard);

        private SlotState Strike() => _forms.Slot(SongSlot.Strike);

        // Where the nest lies, as the Post slot gives it to the picture.
        private Vector3 Position()
        {
            Vector4 a = Post().ParamA;
            return new Vector3(a.x, a.y, a.z);
        }

        private Vector4 Position4() => Post().ParamA;

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
            return new SwarmSongContext(_core, _aim, _aimPoint, _body, _forms, _fury, _hives, _enemies, null, aim, false, _sizeScale, _damageScale);
        }
    }
}
