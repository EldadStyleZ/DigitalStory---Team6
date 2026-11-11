using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class MeteorSongTests
    {
        private const int HoleCount = 6;

        private static readonly int[] Keys = { 4, 2, 0 };

        private SwarmConfig _swarmConfig;
        private FuryConfig _furyConfig;
        private HiveConfig _hiveConfig;
        private SongConfig _songConfig;
        private SwarmBody _body;
        private FuryMeter _fury;
        private SwarmFormMachine _forms;
        private HiveSet _hives;
        private MeteorSong _song;
        private SwarmSongCaster _caster;
        private readonly List<ISwarmTarget> _enemies = new List<ISwarmTarget>();
        private readonly List<SwarmSongEffect> _songs = new List<SwarmSongEffect>();
        private readonly List<Vector3> _landings = new List<Vector3>();
        private ISwarmTarget _aim;
        private Vector3 _aimPoint;
        private Vector3 _piper;

        [SetUp]
        public void SetUp()
        {
            _swarmConfig = ScriptableObject.CreateInstance<SwarmConfig>();
            _furyConfig = ScriptableObject.CreateInstance<FuryConfig>();
            _hiveConfig = ScriptableObject.CreateInstance<HiveConfig>();
            _songConfig = ScriptableObject.CreateInstance<SongConfig>();
            _body = new SwarmBody(_swarmConfig);
            _fury = new FuryMeter(_furyConfig);
            _forms = new SwarmFormMachine(_body, _fury);
            _hives = new HiveSet(new List<Hive> { new Hive(_hiveConfig, new Vector3(-10f, 0f, 0f)), new Hive(_hiveConfig, new Vector3(10f, 0f, 0f)) });
            _song = ScriptableObject.CreateInstance<MeteorSong>();
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
            _aimPoint = Vector3.zero;
            _piper = Vector3.zero;
            _forms.MeteorLanded += _landings.Add;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_swarmConfig);
            Object.DestroyImmediate(_furyConfig);
            Object.DestroyImmediate(_hiveConfig);
            Object.DestroyImmediate(_songConfig);

            foreach (SwarmSongEffect song in _songs)
            {
                Object.DestroyImmediate(song);
            }
        }

        [Test]
        public void Defaults_MatchTheSpec()
        {
            Assert.That(_song.Song.Title, Is.EqualTo("Meteor"));
            Assert.That(_song.Song.CooldownSeconds, Is.EqualTo(9f));
            Assert.That(_song.Song.Keys, Is.EqualTo(Keys));
            Assert.That(_song.Fraction, Is.EqualTo(0.3f));
            Assert.That(_song.Height, Is.EqualTo(12f));
            Assert.That(_song.RiseSeconds, Is.EqualTo(0.8f));
            Assert.That(_song.DiveSeconds, Is.EqualTo(0.5f));
            Assert.That(_song.AfterSeconds, Is.EqualTo(0.6f));
            Assert.That(_song.MaxRange, Is.EqualTo(18f));
            Assert.That(_song.Radius, Is.EqualTo(3f));
            Assert.That(_song.Damage, Is.EqualTo(60f));
            Assert.That(_song.StunSeconds, Is.EqualTo(0.5f));
        }

        [Test]
        public void Keys_PlayedFromSilence_CastExactlyMeteor()
        {
            var cast = new List<int>();
            _caster.SongCast += cast.Add;

            int result = PlayAll();

            Assert.That(result, Is.EqualTo(6));
            Assert.That(cast, Is.EqualTo(new[] { 6 }), "one cast, and it is Meteor");
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Meteor));
        }

        [Test]
        public void Cast_BecomesMeteorAndExposesTheMachinesNumbers()
        {
            _aimPoint = new Vector3(6f, 0f, 2f);

            Cast();

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Meteor));
            Assert.That(_forms.MeteorTarget, Is.EqualTo(new Vector3(6f, 0f, 2f)));
            Assert.That(_forms.MeteorFraction, Is.EqualTo(0.3f));
            Assert.That(_forms.MeteorRiseSeconds, Is.EqualTo(0.8f));
            Assert.That(_forms.MeteorDiveSeconds, Is.EqualTo(0.5f));
            Assert.That(_forms.MeteorHeight, Is.EqualTo(12f));
        }

        [Test]
        public void Tick_BeforeTheImpact_HurtsNobodyAndRaisesNothing()
        {
            var enemy = new FakeSwarmTarget { Position = new Vector3(1f, 0f, 0f) };
            _enemies.Add(enemy);
            Cast();

            _forms.Tick(1.29f, 2);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Meteor));
            Assert.That(enemy.DamageTaken, Is.EqualTo(0f));
            Assert.That(enemy.LastStunSeconds, Is.EqualTo(0f));
            Assert.That(_fury.Value, Is.EqualTo(0f));
            Assert.That(_landings, Is.Empty);
        }

        [Test]
        public void Tick_AtTheImpact_HurtsAndStunsEveryLivingEnemyWithinTheRadius()
        {
            var near = new FakeSwarmTarget { Position = new Vector3(1f, 0f, 1f) };
            var onTheEdge = new FakeSwarmTarget { Position = new Vector3(3f, 0f, 0f) };
            var high = new FakeSwarmTarget { Position = new Vector3(0f, 9f, 2f) };
            var outside = new FakeSwarmTarget { Position = new Vector3(3.1f, 0f, 0f) };
            var dead = new FakeSwarmTarget { Position = new Vector3(0.5f, 0f, 0f), IsAlive = false };
            _enemies.Add(near);
            _enemies.Add(onTheEdge);
            _enemies.Add(high);
            _enemies.Add(outside);
            _enemies.Add(dead);
            Cast();

            _forms.Tick(1.29f, 2);
            _forms.Tick(0.02f, 2);

            Assert.That(near.DamageTaken, Is.EqualTo(60f));
            Assert.That(near.LastStunSeconds, Is.EqualTo(0.5f));
            Assert.That(onTheEdge.DamageTaken, Is.EqualTo(60f), "3 m on the ground counts");
            Assert.That(high.DamageTaken, Is.EqualTo(60f), "height is ignored");
            Assert.That(outside.DamageTaken, Is.EqualTo(0f));
            Assert.That(outside.LastStunSeconds, Is.EqualTo(0f));
            Assert.That(dead.DamageTaken, Is.EqualTo(0f));
        }

        // The session hands every song its whole beekeeper pool, and a pooled beekeeper waits at the origin until it spawns.
        [Test]
        public void Tick_AtTheImpact_IgnoresAPooledBeekeeperThatHasNotSpawned()
        {
            var config = ScriptableObject.CreateInstance<BeekeeperConfig>();
            try
            {
                var pooled = new BeekeeperBrain(config);
                _enemies.Add(pooled);
                Cast();

                _forms.Tick(1.32f, 2);

                Assert.That(pooled.Health, Is.EqualTo(config.Health));
                Assert.That(_fury.Value, Is.EqualTo(0f));
                Assert.That(_landings.Count, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Tick_AtTheImpact_MeasuresFromTheImpactPointNotThePiper()
        {
            var atTarget = new FakeSwarmTarget { Position = new Vector3(10f, 0f, 1f) };
            var atPiper = new FakeSwarmTarget { Position = new Vector3(0f, 0f, 1f) };
            _enemies.Add(atTarget);
            _enemies.Add(atPiper);
            _aimPoint = new Vector3(10f, 0f, 0f);
            Cast();

            _forms.Tick(1.32f, 2);

            Assert.That(atTarget.DamageTaken, Is.EqualTo(60f));
            Assert.That(atPiper.DamageTaken, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_AtTheImpact_FuryRisesByDamagePerFuryPointForEachEnemyHit()
        {
            _enemies.Add(new FakeSwarmTarget { Position = new Vector3(1f, 0f, 0f) });
            _enemies.Add(new FakeSwarmTarget { Position = new Vector3(0f, 0f, -2f) });
            _enemies.Add(new FakeSwarmTarget { Position = new Vector3(30f, 0f, 0f) });
            Cast();

            _forms.Tick(1.32f, 2);

            Assert.That(_fury.Value, Is.EqualTo(60f).Within(1e-3f), "two hits of 60 damage at 2 damage per Fury point");
        }

        [Test]
        public void Tick_AtTheImpact_RaisesMeteorLandedOnceWithTheImpactPoint()
        {
            _aimPoint = new Vector3(4f, 0f, -3f);
            Cast();

            _forms.Tick(1.32f, 2);
            _forms.Tick(0.2f, 2);

            Assert.That(_landings, Is.EqualTo(new[] { new Vector3(4f, 0f, -3f) }));
        }

        [Test]
        public void Tick_OneBigStepOverTheWholeDuration_StillLandsOnceThenReturnsToRiver()
        {
            var enemy = new FakeSwarmTarget { Position = new Vector3(1f, 0f, 0f) };
            _enemies.Add(enemy);
            Cast();

            _forms.Tick(5f, 2);

            Assert.That(enemy.DamageTaken, Is.EqualTo(60f));
            Assert.That(_landings.Count, Is.EqualTo(1));
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        [Test]
        public void Tick_AfterTheWholeDuration_ReturnsToRiverAndClearsTheMeteor()
        {
            Cast();

            _forms.Tick(1.89f, 2);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Meteor), "1.9 s in all: rise, dive, then the flow back");

            _forms.Tick(0.02f, 2);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_forms.MeteorFraction, Is.EqualTo(0f));
        }

        // The impact point

        [Test]
        public void ImpactPoint_WithAnAimedBeekeeper_IsItsPosition()
        {
            _aim = new FakeSwarmTarget { Position = new Vector3(5f, 0f, 2f) };
            _aimPoint = new Vector3(-8f, 0f, 0f);

            Cast();

            Assert.That(_forms.MeteorTarget, Is.EqualTo(new Vector3(5f, 0f, 2f)));
        }

        [Test]
        public void ImpactPoint_WithNoBeekeeperAimedAt_IsTheAimPoint()
        {
            _aimPoint = new Vector3(-8f, 0f, 3f);

            Cast();

            Assert.That(_forms.MeteorTarget, Is.EqualTo(new Vector3(-8f, 0f, 3f)));
        }

        [Test]
        public void ImpactPoint_WithADeadAimedBeekeeper_IsTheAimPoint()
        {
            _aim = new FakeSwarmTarget { Position = new Vector3(5f, 0f, 2f), IsAlive = false };
            _aimPoint = new Vector3(-8f, 0f, 3f);

            Cast();

            Assert.That(_forms.MeteorTarget, Is.EqualTo(new Vector3(-8f, 0f, 3f)));
        }

        [Test]
        public void ImpactPoint_AimPointBeyondMaxRange_IsPulledInToEighteenMetresFromThePiper()
        {
            _piper = new Vector3(10f, 0f, 10f);
            _aimPoint = new Vector3(40f, 0f, 10f);

            Cast();

            Assert.That(_forms.MeteorTarget.x, Is.EqualTo(28f).Within(1e-3f));
            Assert.That(_forms.MeteorTarget.z, Is.EqualTo(10f).Within(1e-3f));
        }

        [Test]
        public void ImpactPoint_AimedBeekeeperBeyondMaxRange_IsPulledInAlongTheGround()
        {
            _aim = new FakeSwarmTarget { Position = new Vector3(0f, 5f, 40f) };

            Cast();

            Assert.That(_forms.MeteorTarget.x, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(_forms.MeteorTarget.z, Is.EqualTo(18f).Within(1e-3f));
        }

        [Test]
        public void ImpactPoint_ExactlyMaxRange_IsKept()
        {
            _aimPoint = new Vector3(0f, 0f, -18f);

            Cast();

            Assert.That(_forms.MeteorTarget, Is.EqualTo(new Vector3(0f, 0f, -18f)));
        }

        // Following the aimed beekeeper

        [Test]
        public void Follow_ATargetThatWalksThreePointFourMetresDuringTheClimb_IsHit()
        {
            FakeSwarmTarget walker = AimAtWalker(new Vector3(5f, 0f, 0f));

            _forms.Tick(0.4f, 2);
            walker.Position = new Vector3(7f, 0f, 0f);
            _forms.Tick(0.39f, 2);
            walker.Position = new Vector3(8.4f, 0f, 0f);
            _forms.Tick(0.02f, 2);
            _forms.Tick(0.51f, 2);

            Assert.That(walker.DamageTaken, Is.EqualTo(60f));
            Assert.That(_landings, Is.EqualTo(new[] { new Vector3(8.4f, 0f, 0f) }));
        }

        [Test]
        public void Follow_MeteorTarget_ReportsTheFollowedPointEachTickDuringTheClimb()
        {
            FakeSwarmTarget walker = AimAtWalker(new Vector3(5f, 0f, 0f));

            walker.Position = new Vector3(6f, 0f, 1f);
            _forms.Tick(0.2f, 2);
            Assert.That(_forms.MeteorTarget, Is.EqualTo(new Vector3(6f, 0f, 1f)));

            walker.Position = new Vector3(7f, 0f, 2f);
            _forms.Tick(0.2f, 2);
            Assert.That(_forms.MeteorTarget, Is.EqualTo(new Vector3(7f, 0f, 2f)));
        }

        [Test]
        public void Follow_AfterTheDiveStarts_ThePointDoesNotMove()
        {
            FakeSwarmTarget walker = AimAtWalker(new Vector3(5f, 0f, 0f));
            _forms.Tick(0.81f, 2);

            walker.Position = new Vector3(12f, 0f, 0f);
            _forms.Tick(0.2f, 2);

            Assert.That(_forms.MeteorTarget, Is.EqualTo(new Vector3(5f, 0f, 0f)));

            _forms.Tick(0.31f, 2);

            Assert.That(walker.DamageTaken, Is.EqualTo(0f), "it left the 3 m radius during the dive");
            Assert.That(_landings, Is.EqualTo(new[] { new Vector3(5f, 0f, 0f) }), "the landing reports the locked point");
        }

        [Test]
        public void Follow_ATargetThatStaysInTheRadiusDuringTheDive_IsStillHit()
        {
            FakeSwarmTarget walker = AimAtWalker(new Vector3(5f, 0f, 0f));
            _forms.Tick(0.81f, 2);

            walker.Position = new Vector3(7.5f, 0f, 0f);
            _forms.Tick(0.51f, 2);

            Assert.That(walker.DamageTaken, Is.EqualTo(60f));
        }

        [Test]
        public void Follow_ATargetThatDiesDuringTheClimb_LocksTheImpactWhereItWasLastSeen()
        {
            FakeSwarmTarget walker = AimAtWalker(new Vector3(5f, 0f, 0f));
            var bystander = new FakeSwarmTarget { Position = new Vector3(7f, 0f, 1f) };
            _enemies.Add(bystander);
            walker.Position = new Vector3(7f, 0f, 0f);
            _forms.Tick(0.5f, 2);

            walker.IsAlive = false;
            walker.Position = new Vector3(9f, 0f, 0f);
            _forms.Tick(0.2f, 2);
            Assert.That(_forms.MeteorTarget, Is.EqualTo(new Vector3(7f, 0f, 0f)));

            _forms.Tick(0.62f, 2);

            Assert.That(_landings, Is.EqualTo(new[] { new Vector3(7f, 0f, 0f) }));
            Assert.That(bystander.DamageTaken, Is.EqualTo(60f));
            Assert.That(walker.DamageTaken, Is.EqualTo(0f));
        }

        [Test]
        public void Follow_AimedBeekeeperWalkingBeyondMaxRange_StaysPulledInToEighteenMetres()
        {
            FakeSwarmTarget walker = AimAtWalker(new Vector3(10f, 0f, 0f));

            walker.Position = new Vector3(30f, 0f, 0f);
            _forms.Tick(0.3f, 2);

            Assert.That(_forms.MeteorTarget.x, Is.EqualTo(18f).Within(1e-3f));
            Assert.That(_forms.MeteorTarget.z, Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void Follow_WithNoBeekeeperAimedAt_ThePointStaysWhereItWasCast()
        {
            var walker = new FakeSwarmTarget { Position = new Vector3(5f, 0f, 0f) };
            _enemies.Add(walker);
            _aimPoint = new Vector3(5f, 0f, 0f);
            Cast();

            walker.Position = new Vector3(8.4f, 0f, 0f);
            _forms.Tick(0.5f, 2);

            Assert.That(_forms.MeteorTarget, Is.EqualTo(new Vector3(5f, 0f, 0f)));

            _forms.Tick(0.82f, 2);

            Assert.That(walker.DamageTaken, Is.EqualTo(0f));
            Assert.That(_landings, Is.EqualTo(new[] { new Vector3(5f, 0f, 0f) }));
        }

        private FakeSwarmTarget AimAtWalker(Vector3 position)
        {
            var walker = new FakeSwarmTarget { Position = position };
            _enemies.Add(walker);
            _aim = walker;
            Cast();
            return walker;
        }

        // Cooldown, collapse and blocking

        [Test]
        public void Press_WhileOnCooldown_CannotBeRecast()
        {
            PlayAll();
            _caster.Tick(8f);

            int again = PlayAll();

            Assert.That(again, Is.EqualTo(SongBook.NoSong));
            Assert.That(_caster.CooldownFraction(6), Is.GreaterThan(0f));

            _caster.Tick(1.1f);

            Assert.That(PlayAll(), Is.EqualTo(6), "ready after nine seconds");
        }

        [Test]
        public void Collapse_BeforeTheImpact_CancelsItWithNoDamage()
        {
            var enemy = new FakeSwarmTarget { Position = new Vector3(1f, 0f, 0f) };
            _enemies.Add(enemy);
            Cast();
            _forms.Tick(1f, 2);

            _body.Lose(_swarmConfig.StartingWasps - 100, false);
            _forms.Tick(1f, 2);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
            Assert.That(enemy.DamageTaken, Is.EqualTo(0f));
            Assert.That(_fury.Value, Is.EqualTo(0f));
            Assert.That(_landings, Is.Empty);
            Assert.That(_forms.MeteorFraction, Is.EqualTo(0f));
        }

        [Test]
        public void Press_WhileCollapsed_CastsNothing()
        {
            _body.Lose(_swarmConfig.StartingWasps - 100, false);

            int result = PlayAll();

            Assert.That(result, Is.EqualTo(SongBook.NoSong));
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
        }

        [Test]
        public void Cast_WhileCollapsed_ChangesNothing()
        {
            _body.Lose(_swarmConfig.StartingWasps - 100, false);

            Cast();

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
        }

        [Test]
        public void Press_WhileTheColossusStands_CastsNothingAndStartsNoCooldown()
        {
            _forms.StartColossus(12f);

            int result = PlayAll();

            Assert.That(result, Is.EqualTo(SongBook.NoSong));
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Colossus));
            Assert.That(_caster.CooldownFraction(6), Is.EqualTo(0f));
        }

        [Test]
        public void Cast_WhileTheColossusStands_ChangesNothing()
        {
            _forms.StartColossus(12f);

            Cast();

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Colossus));
            Assert.That(_landings, Is.Empty);
        }

        private void Cast()
        {
            SwarmSongContext context = Context();
            _song.Cast(in context);
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

        private SwarmSongContext Context()
        {
            return new SwarmSongContext(_piper, _aim, _aimPoint, _body, _forms, _fury, _hives, _enemies, null);
        }
    }
}
