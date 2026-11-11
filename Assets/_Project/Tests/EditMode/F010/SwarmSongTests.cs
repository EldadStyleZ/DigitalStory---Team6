using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>The songs' own rows of plan section A. The Meteor has its own <see cref="MeteorSongTests"/>.</summary>
    public sealed class SwarmSongTests
    {
        private const int H = 0;
        private const int J = 1;
        private const int K = 2;
        private const int L = 3;

        private readonly List<Object> _assets = new List<Object>();

        private SwarmConfig _swarmConfig;
        private FuryConfig _furyConfig;
        private HiveConfig _hiveConfig;
        private SquadConfig _squads;
        private SwarmBody _body;
        private FuryMeter _fury;
        private SwarmFormMachine _forms;
        private HiveSet _hives;
        private List<ISwarmTarget> _enemies;
        private FakeSmokeClearer _smoke;

        private SlotState Strike => _forms.Slot(SongSlot.Strike);

        [SetUp]
        public void SetUp()
        {
            _swarmConfig = ScriptableObject.CreateInstance<SwarmConfig>();
            _furyConfig = ScriptableObject.CreateInstance<FuryConfig>();
            _hiveConfig = ScriptableObject.CreateInstance<HiveConfig>();
            _squads = ScriptableObject.CreateInstance<SquadConfig>();
            _body = new SwarmBody(_swarmConfig);
            _fury = new FuryMeter(_furyConfig);
            _forms = new SwarmFormMachine(_body, _fury, _squads);
            _hives = new HiveSet(new List<Hive> { new Hive(_hiveConfig, new Vector3(-10f, 0f, 0f)), new Hive(_hiveConfig, new Vector3(10f, 0f, 0f)) });
            _enemies = new List<ISwarmTarget>();
            _smoke = new FakeSmokeClearer();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_swarmConfig);
            Object.DestroyImmediate(_furyConfig);
            Object.DestroyImmediate(_hiveConfig);
            Object.DestroyImmediate(_squads);

            foreach (Object asset in _assets)
            {
                Object.DestroyImmediate(asset);
            }

            _assets.Clear();
        }

        [Test]
        public void Songs_Defaults_MatchThePlansKeysAndCooldowns()
        {
            AssertSong(Make<StingSong>(), "Sting", 1.5f, J, K, H);
            AssertSong(Make<TornadoSong>(), "Tornado", 10f, J, K, K, H);
            AssertSong(Make<DomeSong>(), "Dome", 8f, H, K, J);
            AssertSong(Make<GustSong>(), "Gust", 6f, H, L, K);
            AssertSong(Make<HiveCallSong>(), "Hive Call", 3f, L, J, L);
            AssertSong(Make<ColossusSong>(), "Colossus", 0f, L, J, K, J, L);
        }

        [Test]
        public void Songs_Defaults_MatchThePlansNumbers()
        {
            var dome = Make<DomeSong>();
            var sting = Make<StingSong>();
            var tornado = Make<TornadoSong>();
            var gust = Make<GustSong>();
            var colossus = Make<ColossusSong>();

            Assert.That(dome.Seconds, Is.EqualTo(5f));
            Assert.That(dome.Radius, Is.EqualTo(2.2f));
            Assert.That(dome.Share, Is.EqualTo(0.35f));
            Assert.That(dome.LossFraction, Is.EqualTo(0.15f), "hits cost 85% fewer wasps");
            Assert.That(dome.SpeedMultiplier, Is.EqualTo(0.5f), "the core at x0.5");
            Assert.That(sting.Seconds, Is.EqualTo(4f));
            Assert.That(sting.Share, Is.EqualTo(0.3f));
            Assert.That(sting.ArriveSeconds, Is.EqualTo(0.6f));
            Assert.That(sting.DamagePerSecond, Is.EqualTo(25f));
            Assert.That(sting.MaxRange, Is.EqualTo(12f));
            Assert.That(sting.AirSeconds, Is.EqualTo(1f));
            Assert.That(tornado.Seconds, Is.EqualTo(1.5f));
            Assert.That(tornado.Share, Is.EqualTo(0.37f));
            Assert.That(tornado.Radius, Is.EqualTo(5f));
            Assert.That(tornado.Height, Is.EqualTo(3f));
            Assert.That(tornado.SpinRadiansPerSecond, Is.EqualTo(6f));
            Assert.That(tornado.HitSeconds, Is.EqualTo(0.3f));
            Assert.That(tornado.WallRadius, Is.EqualTo(1.5f));
            Assert.That(tornado.Damage, Is.EqualTo(30f));
            Assert.That(tornado.KnockbackSpeed, Is.EqualTo(6f));
            Assert.That(gust.Seconds, Is.EqualTo(0.8f));
            Assert.That(gust.ClearRadius, Is.EqualTo(6f));
            Assert.That(gust.PushRadius, Is.EqualTo(3f));
            Assert.That(gust.PushSpeed, Is.EqualTo(4f));
            Assert.That(gust.InhaleSeconds, Is.EqualTo(0.12f), "the inhale, 0 to .12 s");
            Assert.That(gust.RingStartRadius, Is.EqualTo(0.6f), "the ring grows from .6 m");
            Assert.That(gust.RingSeconds, Is.EqualTo(0.5f));
            Assert.That(colossus.Seconds, Is.EqualTo(12f));
        }

        [Test]
        public void Songs_SlotsAndForms_MatchThePlan()
        {
            var sting = Make<StingSong>();
            var tornado = Make<TornadoSong>();
            var dome = Make<DomeSong>();

            Assert.That(sting.Slot, Is.EqualTo(SongSlot.Strike));
            Assert.That(sting.Form, Is.EqualTo(SwarmForm.Sting));
            Assert.That(tornado.Slot, Is.EqualTo(SongSlot.Strike));
            Assert.That(tornado.Form, Is.EqualTo(SwarmForm.Tornado));
            Assert.That(dome.Slot, Is.EqualTo(SongSlot.Guard));
            Assert.That(dome.Form, Is.EqualTo(SwarmForm.Dome));
            Assert.That(Make<GustSong>(), Is.Not.InstanceOf<SlotSongEffect>(), "the Gust is a pulse with no slot");
            Assert.That(Make<HiveCallSong>(), Is.Not.InstanceOf<SlotSongEffect>(), "Hive Call is a whole-swarm form");
            Assert.That(Make<ColossusSong>(), Is.Not.InstanceOf<SlotSongEffect>(), "the Colossus is a whole-swarm form");
        }

        // Dome

        [Test]
        public void Dome_Cast_DomesTheSwarmForItsSeconds()
        {
            var song = Make<DomeSong>();
            SwarmSongContext context = Context();

            Assert.That(song.CanCast(in context), Is.True);
            song.Cast(in context);

            Assert.That(_forms.IsDomed, Is.True);
            _forms.Tick(5f, _hives);
            Assert.That(_forms.IsDomed, Is.False);
        }

        [Test]
        public void Dome_WhileItRuns_LetsThroughItsAssetsShareOfEachLossAndSlowsTheCore()
        {
            var song = Make<DomeSong>();
            JsonUtility.FromJsonOverwrite("{\"<LossFraction>k__BackingField\":0.4,\"<SpeedMultiplier>k__BackingField\":0.7}", song);
            Assert.That(_forms.WaspLossFraction, Is.EqualTo(1f));
            Assert.That(_forms.CoreSpeedMultiplier, Is.EqualTo(1f));

            Cast(song);

            Assert.That(_forms.WaspLossFraction, Is.EqualTo(0.4f).Within(1e-5f));
            Assert.That(_forms.CoreSpeedMultiplier, Is.EqualTo(0.7f).Within(1e-5f));
            Assert.That(_forms.SmokeSlowsCore, Is.True, "the dome does not keep the smoke off");
            _forms.Tick(5f, _hives);
            Assert.That(_forms.WaspLossFraction, Is.EqualTo(1f), "its return beat shields nothing");
            Assert.That(_forms.CoreSpeedMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void Dome_Cast_TakesThirtyFivePercentAsTheGuardSquad()
        {
            var song = Make<DomeSong>();
            SwarmSongContext context = Context();

            song.Cast(in context);

            SlotState guard = _forms.Slot(SongSlot.Guard);
            Assert.That(guard.Form, Is.EqualTo(SwarmForm.Dome));
            Assert.That(guard.Low, Is.EqualTo(0.2f).Within(1e-5f));
            Assert.That(guard.High, Is.EqualTo(0.55f).Within(1e-5f));
        }

        // Sting

        [Test]
        public void Sting_CanCast_EvenWithNothingToHit()
        {
            var song = Make<StingSong>();
            var dead = new FakeSwarmTarget { IsAlive = false };

            SwarmSongContext none = Context(aim: null);
            SwarmSongContext deadAim = Context(aim: dead);
            SwarmSongContext alive = Context(aim: new FakeSwarmTarget());

            Assert.That(song.CanCast(in none), Is.True);
            Assert.That(song.CanCast(in deadAim), Is.True);
            Assert.That(song.CanCast(in alive), Is.True);
        }

        [Test]
        public void Sting_Cast_WrapsAThirtyPercentStrikeSquadAroundTheTarget()
        {
            var target = new FakeSwarmTarget { Position = new Vector3(2f, 0f, 1f) };

            Cast(Make<StingSong>(), aim: target);

            Assert.That(Strike.IsActive, Is.True);
            Assert.That(Strike.Form, Is.EqualTo(SwarmForm.Sting));
            Assert.That(Strike.Low, Is.EqualTo(0.55f).Within(1e-5f));
            Assert.That(Strike.High, Is.EqualTo(0.85f).Within(1e-5f));
            Assert.That((Vector3)Strike.ParamA, Is.EqualTo(new Vector3(2f, 0f, 1f)));
        }

        [Test]
        public void Sting_Tick_HurtsNothingBeforeItArrives()
        {
            var target = new FakeSwarmTarget { Position = new Vector3(2f, 0f, 0f) };
            Cast(Make<StingSong>(), aim: target);

            _forms.Tick(0.59f, _hives);

            Assert.That(target.DamageTaken, Is.EqualTo(0f));
            Assert.That(_fury.Value, Is.EqualTo(0f));
        }

        [Test]
        public void Sting_Tick_HurtsByDamagePerSecondFromItsArrival()
        {
            var target = new FakeSwarmTarget { Position = new Vector3(2f, 0f, 0f) };
            Cast(Make<StingSong>(), aim: target);
            _forms.Tick(0.6f, _hives);

            _forms.Tick(0.5f, _hives);

            Assert.That(target.DamageTaken, Is.EqualTo(12.5f).Within(1e-3f));
        }

        [Test]
        public void Sting_Tick_FillsFuryFromTheDamage()
        {
            var target = new FakeSwarmTarget { Position = new Vector3(2f, 0f, 0f) };
            Cast(Make<StingSong>(), aim: target);

            _forms.Tick(1.6f, _hives);

            Assert.That(_fury.Value, Is.EqualTo(25f / _furyConfig.DamagePerFuryPoint).Within(1e-3f), "25 damage, at DamagePerFuryPoint a point");
        }

        [Test]
        public void Sting_StepPastItsEnd_OnlyHurtsUntilItsEnd()
        {
            var target = new FakeSwarmTarget { Position = new Vector3(2f, 0f, 0f) };
            Cast(Make<StingSong>(), aim: target);

            _forms.Tick(10f, _hives);

            Assert.That(target.DamageTaken, Is.EqualTo(85f).Within(1e-2f), "3.4 s at 25 a second, from 0.6 s to 4 s");
            Assert.That(Strike.IsActive, Is.False);
        }

        [Test]
        public void Sting_TargetDies_LetsGoAndHurtsNoMore()
        {
            var target = new FakeSwarmTarget { Position = new Vector3(2f, 0f, 0f) };
            Cast(Make<StingSong>(), aim: target);
            _forms.Tick(1.1f, _hives);
            target.IsAlive = false;

            _forms.Tick(0.5f, _hives);

            Assert.That(target.DamageTaken, Is.EqualTo(12.5f).Within(1e-3f));
            Assert.That(Strike.ReleasedSeconds, Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void Sting_NoTarget_WrapsTheAirAtTheAimForOneSecond()
        {
            var bystander = new FakeSwarmTarget { Position = new Vector3(6f, 0f, 0f) };
            _enemies.Add(bystander);

            Cast(Make<StingSong>(), aimPoint: new Vector3(6f, 0f, 0f));

            Assert.That(Strike.Form, Is.EqualTo(SwarmForm.Sting));
            Assert.That((Vector3)Strike.ParamA, Is.EqualTo(new Vector3(6f, 0f, 0f)));
            _forms.Tick(0.99f, _hives);
            Assert.That(Strike.ReleasedSeconds, Is.EqualTo(-1f));
            _forms.Tick(0.02f, _hives);
            Assert.That(Strike.ReleasedSeconds, Is.GreaterThanOrEqualTo(0f));
            _forms.Tick(1f, _hives);
            Assert.That(bystander.DamageTaken, Is.EqualTo(0f), "the air is wrapped, not the beekeeper standing in it");
        }

        [Test]
        public void Sting_DeadTarget_WrapsTheAirAtTheAim()
        {
            var dead = new FakeSwarmTarget { Position = new Vector3(2f, 0f, 0f), IsAlive = false };

            Cast(Make<StingSong>(), aim: dead, aimPoint: new Vector3(-5f, 0f, 0f));

            Assert.That((Vector3)Strike.ParamA, Is.EqualTo(new Vector3(-5f, 0f, 0f)));
            _forms.Tick(4f, _hives);
            Assert.That(dead.DamageTaken, Is.EqualTo(0f));
        }

        [Test]
        public void Sting_TargetBeyondItsReach_WrapsTheAirTwelveMetresTowardIt()
        {
            var far = new FakeSwarmTarget { Position = new Vector3(20f, 0f, 0f) };

            Cast(Make<StingSong>(), aim: far);

            Assert.That(Strike.ParamA.x, Is.EqualTo(12f).Within(1e-3f));
            Assert.That(Strike.ParamA.z, Is.EqualTo(0f).Within(1e-3f));
            _forms.Tick(4f, _hives);
            Assert.That(far.DamageTaken, Is.EqualTo(0f));
            Assert.That(Strike.IsActive, Is.False, "the air is wrapped for one second, then the wasps fly home");
        }

        [Test]
        public void Sting_TargetExactlyTwelveMetresAway_IsWrapped()
        {
            var target = new FakeSwarmTarget { Position = new Vector3(0f, 0f, 12f) };
            Cast(Make<StingSong>(), aim: target);

            _forms.Tick(1.6f, _hives);

            Assert.That(target.DamageTaken, Is.EqualTo(25f).Within(1e-3f));
        }

        [Test]
        public void Sting_AimBeyondItsReach_WrapsTheAirTwelveMetresAway()
        {
            Cast(Make<StingSong>(), core: new Vector3(1f, 0f, 1f), aimPoint: new Vector3(1f, 0f, -29f));

            Assert.That(Strike.ParamA.x, Is.EqualTo(1f).Within(1e-3f));
            Assert.That(Strike.ParamA.z, Is.EqualTo(-11f).Within(1e-3f));
        }

        // Tornado

        [Test]
        public void Tornado_Cast_HurtsNobodyBeforeItsHit()
        {
            var near = new FakeSwarmTarget { Position = new Vector3(3f, 0f, 0f) };
            _enemies.Add(near);
            Cast(Make<TornadoSong>());

            _forms.Tick(0.29f, _hives);

            Assert.That(near.DamageTaken, Is.EqualTo(0f));
            Assert.That(near.LastKnockback, Is.EqualTo(Vector3.zero));
            Assert.That(_fury.Value, Is.EqualTo(0f));
        }

        [Test]
        public void Tornado_AtItsHitTime_HurtsAndThrowsEveryBeekeeperWithinFiveMetres()
        {
            var near = new FakeSwarmTarget { Position = new Vector3(3f, 0f, 0f) };
            var far = new FakeSwarmTarget { Position = new Vector3(5.5f, 0f, 0f) };
            _enemies.Add(near);
            _enemies.Add(far);
            Cast(Make<TornadoSong>());

            _forms.Tick(0.3f, _hives);

            Assert.That(near.DamageTaken, Is.EqualTo(30f));
            Assert.That(near.LastKnockback.x, Is.EqualTo(6f).Within(1e-3f), "6 m/s straight away from the core");
            Assert.That(near.LastKnockback.z, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(far.DamageTaken, Is.EqualTo(0f));
            Assert.That(far.LastKnockback, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void Tornado_ReachIncludesExactlyFiveMetresAndIgnoresHeight()
        {
            var edge = new FakeSwarmTarget { Position = new Vector3(0f, 9f, 5f) };
            _enemies.Add(edge);
            Cast(Make<TornadoSong>());

            _forms.Tick(0.3f, _hives);

            Assert.That(edge.DamageTaken, Is.EqualTo(30f), "5 m on the ground counts, however high");
            Assert.That(edge.LastKnockback.y, Is.EqualTo(0f), "thrown along the ground");
        }

        [Test]
        public void Tornado_MeasuresFromTheCore()
        {
            var target = new FakeSwarmTarget { Position = new Vector3(21f, 0f, 20f) };
            _enemies.Add(target);
            Cast(Make<TornadoSong>(), core: new Vector3(20f, 0f, 20f));

            _forms.Tick(0.3f, _hives);

            Assert.That(target.DamageTaken, Is.EqualTo(30f));
            Assert.That(target.LastKnockback.x, Is.EqualTo(6f).Within(1e-3f));
        }

        [Test]
        public void Tornado_CoreWalksBeforeItsHit_HitsAroundWhereTheCoreStandsThen()
        {
            var ahead = new FakeSwarmTarget { Position = new Vector3(7f, 0f, 0f) };
            var behind = new FakeSwarmTarget { Position = new Vector3(-4.5f, 0f, 0f) };
            _enemies.Add(ahead);
            _enemies.Add(behind);
            Cast(Make<TornadoSong>());

            _forms.SetCore(new Vector3(3f, 0f, 0f));
            _forms.Tick(0.3f, _hives);

            Assert.That(ahead.DamageTaken, Is.EqualTo(30f), "4 m from where the column stands when it hits");
            Assert.That(ahead.LastKnockback.x, Is.EqualTo(6f).Within(1e-3f));
            Assert.That(behind.DamageTaken, Is.EqualTo(0f), "within 5 m of the cast, but 7.5 m from the column");
        }

        [Test]
        public void Tornado_SkipsTheDead()
        {
            var dead = new FakeSwarmTarget { Position = new Vector3(1f, 0f, 0f), IsAlive = false };
            _enemies.Add(dead);
            Cast(Make<TornadoSong>());

            _forms.Tick(0.3f, _hives);

            Assert.That(dead.DamageTaken, Is.EqualTo(0f));
            Assert.That(_fury.Value, Is.EqualTo(0f));
        }

        [Test]
        public void Tornado_FillsFuryFromTheTotalDamage()
        {
            _enemies.Add(new FakeSwarmTarget { Position = new Vector3(1f, 0f, 0f) });
            _enemies.Add(new FakeSwarmTarget { Position = new Vector3(0f, 0f, -2f) });
            _enemies.Add(new FakeSwarmTarget { Position = new Vector3(30f, 0f, 0f) });
            Cast(Make<TornadoSong>());

            _forms.Tick(0.3f, _hives);

            Assert.That(_fury.Value, Is.EqualTo(60f / _furyConfig.DamagePerFuryPoint).Within(1e-3f), "60 damage, at DamagePerFuryPoint a point");
        }

        [Test]
        public void Tornado_HitsOnlyOnce()
        {
            var near = new FakeSwarmTarget { Position = new Vector3(1f, 0f, 0f) };
            _enemies.Add(near);
            Cast(Make<TornadoSong>());

            _forms.Tick(0.3f, _hives);
            _forms.Tick(1f, _hives);

            Assert.That(near.DamageTaken, Is.EqualTo(30f));
        }

        [Test]
        public void Tornado_WithNobodyAround_StillSpinsForItsSeconds()
        {
            Cast(Make<TornadoSong>());

            Assert.That(Strike.Form, Is.EqualTo(SwarmForm.Tornado));
            _forms.Tick(1.5f, _hives);
            Assert.That(Strike.ReleasedSeconds, Is.EqualTo(0f).Within(1e-4f));
            _forms.Tick(0.5f, _hives);
            Assert.That(Strike.IsActive, Is.True, "the 0.6 s return beat");
            _forms.Tick(0.2f, _hives);
            Assert.That(Strike.IsActive, Is.False);
        }

        [Test]
        public void Tornado_BeekeeperOnTheSpot_IsStillThrownSixMetresPerSecond()
        {
            var onTop = new FakeSwarmTarget { Position = new Vector3(2f, 0f, 2f) };
            _enemies.Add(onTop);
            Cast(Make<TornadoSong>(), core: new Vector3(2f, 0f, 2f));

            _forms.Tick(0.3f, _hives);

            Assert.That(onTop.LastKnockback.magnitude, Is.EqualTo(6f).Within(1e-3f));
            Assert.That(float.IsNaN(onTop.LastKnockback.x), Is.False);
        }

        [Test]
        public void Tornado_Landed_IsRaisedAtItsHitTimeWhereTheCoreStands()
        {
            var landings = new List<Vector3>();
            _forms.Landed += (form, point) => landings.Add(point);
            Cast(Make<TornadoSong>(), core: new Vector3(2f, 0f, 2f));

            _forms.Tick(0.29f, _hives);
            Assert.That(landings, Is.Empty);
            _forms.Tick(0.02f, _hives);

            Assert.That(landings, Is.EqualTo(new[] { new Vector3(2f, 0f, 2f) }));
        }

        // Gust

        [Test]
        public void Gust_Cast_ClearsSmokeWithinSixMetresOfTheCoreAndPulses()
        {
            var song = Make<GustSong>();
            SwarmSongContext context = Context(core: new Vector3(4f, 0f, 7f));

            song.Cast(in context);

            Assert.That(_smoke.CallCount, Is.EqualTo(1));
            Assert.That(_smoke.LastCenter, Is.EqualTo(new Vector3(4f, 0f, 7f)));
            Assert.That(_smoke.LastRadius, Is.EqualTo(6f));
            Assert.That(_forms.GustSeconds, Is.EqualTo(0f));
            Assert.That(_forms.GustCenter, Is.EqualTo(new Vector3(4f, 0f, 7f)));
            Assert.That(_forms.GustRadius, Is.EqualTo(6f));
            _forms.Tick(0.8f, _hives);
            Assert.That(_forms.GustSeconds, Is.EqualTo(-1f));
        }

        [Test]
        public void Gust_Cast_WithNoSmokeSystemStillPulses()
        {
            var song = Make<GustSong>();
            SwarmSongContext context = Context(smoke: false);

            song.Cast(in context);

            Assert.That(_forms.GustSeconds, Is.EqualTo(0f));
        }

        [Test]
        public void Gust_RingHasPassed_PushedBeekeepersWithinThreeMetresOutAtFourMetresPerSecond()
        {
            var near = new FakeSwarmTarget { Position = new Vector3(3f, 0f, 1f) };
            var edge = new FakeSwarmTarget { Position = new Vector3(1f, 2f, 4f) };
            var far = new FakeSwarmTarget { Position = new Vector3(4.5f, 0f, 1f) };
            var dead = new FakeSwarmTarget { Position = new Vector3(2f, 0f, 1f), IsAlive = false };
            _enemies.Add(near);
            _enemies.Add(edge);
            _enemies.Add(far);
            _enemies.Add(dead);

            Cast(Make<GustSong>(), core: new Vector3(1f, 0f, 1f));
            _forms.Tick(0.8f, _hives);

            Assert.That(near.LastKnockback.x, Is.EqualTo(4f).Within(1e-3f));
            Assert.That(near.LastKnockback.z, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(edge.LastKnockback.z, Is.EqualTo(4f).Within(1e-3f), "3 m on the ground counts, however high");
            Assert.That(edge.LastKnockback.y, Is.EqualTo(0f));
            Assert.That(far.LastKnockback, Is.EqualTo(Vector3.zero));
            Assert.That(dead.LastKnockback, Is.EqualTo(Vector3.zero));
            Assert.That(near.DamageTaken, Is.EqualTo(0f), "a gust pushes, it does not hurt");
            Assert.That(near.KnockbackCount, Is.EqualTo(1), "once");
            Assert.That(edge.KnockbackCount, Is.EqualTo(1));
        }

        // The owner's rule: an impact hurts on the frame it visibly lands. The ring reaches 3 m about 0.25 s after the cast.
        [Test]
        public void Gust_Cast_PushesNobodyBeforeItsRingStarts()
        {
            var onTop = new FakeSwarmTarget { Position = new Vector3(0.5f, 0f, 0f) };
            _enemies.Add(onTop);

            Cast(Make<GustSong>());
            Assert.That(onTop.KnockbackCount, Is.EqualTo(0), "not on the cast frame");

            _forms.Tick(0.11f, _hives);
            Assert.That(onTop.KnockbackCount, Is.EqualTo(0), "the wasps are still drawn in");
        }

        [Test]
        public void Gust_EachBeekeeper_IsPushedOnTheStepTheRingReachesIt()
        {
            var close = new FakeSwarmTarget { Position = new Vector3(0.5f, 0f, 0f) };
            var middle = new FakeSwarmTarget { Position = new Vector3(0f, 0f, 2f) };
            var edge = new FakeSwarmTarget { Position = new Vector3(-3f, 0f, 0f) };
            _enemies.Add(close);
            _enemies.Add(middle);
            _enemies.Add(edge);
            var song = Make<GustSong>();
            Cast(song);

            // The ring reaches 0.5 m as it starts at 0.12 s, 2 m at about 0.19 s and 3 m at about 0.25 s.
            _forms.Tick(0.119f, _hives);
            Assert.That(close.KnockbackCount + middle.KnockbackCount + edge.KnockbackCount, Is.EqualTo(0));

            _forms.Tick(0.061f, _hives);
            Assert.That(close.KnockbackCount, Is.EqualTo(1), "0.18 s: the ring has started");
            Assert.That(middle.KnockbackCount, Is.EqualTo(0), "the ring is at " + song.RingRadius(0.18f) + " m");

            _forms.Tick(0.02f, _hives);
            Assert.That(middle.KnockbackCount, Is.EqualTo(1), "0.20 s");
            Assert.That(middle.LastKnockback.z, Is.EqualTo(4f).Within(1e-3f));
            Assert.That(edge.KnockbackCount, Is.EqualTo(0), "the ring is at " + song.RingRadius(0.2f) + " m");

            _forms.Tick(0.04f, _hives);
            Assert.That(edge.KnockbackCount, Is.EqualTo(0), "0.24 s: just short of 3 m");

            _forms.Tick(0.02f, _hives);
            Assert.That(edge.KnockbackCount, Is.EqualTo(1), "0.26 s");
            Assert.That(edge.LastKnockback.x, Is.EqualTo(-4f).Within(1e-3f));

            _forms.Tick(0.6f, _hives);
            Assert.That(close.KnockbackCount + middle.KnockbackCount + edge.KnockbackCount, Is.EqualTo(3), "each is pushed once");
        }

        [Test]
        public void Gust_RingRadius_DrawsInThenEasesOutFromItsStartToTheClearRadius()
        {
            var song = Make<GustSong>();

            Assert.That(song.RingRadius(0f), Is.EqualTo(-1f));
            Assert.That(song.RingRadius(0.119f), Is.EqualTo(-1f));
            Assert.That(song.RingRadius(0.12f), Is.EqualTo(0.6f).Within(1e-4f));
            Assert.That(song.RingRadius(0.37f), Is.EqualTo(0.6f + (5.4f * 0.75f)).Within(1e-3f), "halfway in time, three quarters of the way out");
            Assert.That(song.RingRadius(0.62f), Is.EqualTo(6f).Within(1e-4f));
            Assert.That(song.RingRadius(5f), Is.EqualTo(6f).Within(1e-4f));
        }

        [Test]
        public void Gust_CollapseBeforeTheRingArrives_PushesNobody()
        {
            var edge = new FakeSwarmTarget { Position = new Vector3(3f, 0f, 0f) };
            _enemies.Add(edge);
            Cast(Make<GustSong>());
            _forms.Tick(0.1f, _hives);

            _body.Lose(_body.Count);
            _forms.Tick(0.7f, _hives);

            Assert.That(edge.KnockbackCount, Is.EqualTo(0));
        }

        [Test]
        public void Gust_Cast_WhileCollapsed_DoesNothing()
        {
            var near = new FakeSwarmTarget { Position = new Vector3(1f, 0f, 0f) };
            _enemies.Add(near);
            _body.Lose(_body.Count);

            Cast(Make<GustSong>());

            Assert.That(_smoke.CallCount, Is.EqualTo(0));
            Assert.That(near.LastKnockback, Is.EqualTo(Vector3.zero));
            Assert.That(_forms.GustSeconds, Is.EqualTo(-1f));
        }

        // Hive Call

        [Test]
        public void HiveCall_CanCast_EvenWithNoStandingHiveOrAFullSwarm()
        {
            var song = Make<HiveCallSong>();
            _hives[0].Damage(10000f);
            _hives[1].Damage(10000f);
            JsonUtility.FromJsonOverwrite("{\"<StartingWasps>k__BackingField\":" + _swarmConfig.StartingCapacity + "}", _swarmConfig);
            _body = new SwarmBody(_swarmConfig);
            _forms = new SwarmFormMachine(_body, _fury, _squads);
            SwarmSongContext context = Context();

            Assert.That(song.CanCast(in context), Is.True);
        }

        [Test]
        public void HiveCall_Cast_StartsHiveCall()
        {
            var song = Make<HiveCallSong>();
            SwarmSongContext context = Context();

            song.Cast(in context);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.HiveCall));
            Assert.That(_forms.IsCoreRooted, Is.True);
        }

        [Test]
        public void HiveCall_CastWithTheSwarmFull_EndsOnTheNextTick()
        {
            JsonUtility.FromJsonOverwrite("{\"<StartingWasps>k__BackingField\":" + _swarmConfig.StartingCapacity + "}", _swarmConfig);
            _body = new SwarmBody(_swarmConfig);
            _forms = new SwarmFormMachine(_body, _fury, _squads);
            Cast(Make<HiveCallSong>());
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.HiveCall));

            _forms.Tick(0.1f, _hives);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        // Colossus

        [Test]
        public void Colossus_CanCast_OnlyWithFullFury()
        {
            var song = Make<ColossusSong>();
            SwarmSongContext context = Context();
            float fillingDamage = _furyConfig.Max * _furyConfig.DamagePerFuryPoint;

            _fury.AddDamage(fillingDamage - 0.1f);
            Assert.That(song.CanCast(in context), Is.False, "just under full");

            _fury.AddDamage(1f);
            Assert.That(song.CanCast(in context), Is.True);
        }

        [Test]
        public void Colossus_Cast_UsesAllFuryAndLastsTwelveSeconds()
        {
            var song = Make<ColossusSong>();
            _fury.AddDamage(1000f);
            SwarmSongContext context = Context();

            song.Cast(in context);

            Assert.That(_fury.Value, Is.EqualTo(0f));
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Colossus));
            _forms.Tick(11.5f, _hives);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Colossus));
            _forms.Tick(0.5f, _hives);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        // Defaults that every song shares

        [Test]
        public void CanCast_EverySongButTheColossus_AlwaysAllowsEvenWithNothingToHit()
        {
            SwarmSongContext context = Context();

            Assert.That(Make<DomeSong>().CanCast(in context), Is.True);
            Assert.That(Make<StingSong>().CanCast(in context), Is.True);
            Assert.That(Make<TornadoSong>().CanCast(in context), Is.True);
            Assert.That(Make<MeteorSong>().CanCast(in context), Is.True);
            Assert.That(Make<GustSong>().CanCast(in context), Is.True);
            Assert.That(Make<HiveCallSong>().CanCast(in context), Is.True);
        }

        // Aiming: an aimed song asks for its target by its own rule as it is cast (plan section E)

        [Test]
        public void Sting_CastWithAnAimChooser_AsksForTheThreatWithinItsRangeAndStingsTheAnswer()
        {
            var chosen = new FakeSwarmTarget { Position = new Vector3(3f, 0f, 4f) };
            var aim = new FakeSongAim { Target = chosen, Point = chosen.Position };
            var ignored = new FakeSwarmTarget { Position = new Vector3(-2f, 0f, 0f) };
            var song = Make<StingSong>();

            var context = new SwarmSongContext(default, ignored, ignored.Position, _body, _forms, _fury, _hives, _enemies, _smoke, aim);
            song.Cast(in context);

            Assert.That(aim.Calls, Is.EqualTo(1));
            Assert.That(aim.LastRule, Is.EqualTo(AimRule.Threat));
            Assert.That(aim.LastRange, Is.EqualTo(song.MaxRange));
            Assert.That((Vector3)Strike.ParamA, Is.EqualTo(new Vector3(3f, 0f, 4f)));
        }

        [Test]
        public void Meteor_CastWithAnAimChooser_AsksForTheCrowdWithinItsRangeAndLandsOnTheAnswer()
        {
            var aim = new FakeSongAim { Target = null, Point = new Vector3(0f, 0f, 9f) };
            var ignored = new FakeSwarmTarget { Position = new Vector3(-2f, 0f, 0f) };
            var song = Make<MeteorSong>();

            var context = new SwarmSongContext(default, ignored, ignored.Position, _body, _forms, _fury, _hives, _enemies, _smoke, aim);
            song.Cast(in context);

            Assert.That(aim.Calls, Is.EqualTo(1));
            Assert.That(aim.LastRule, Is.EqualTo(AimRule.Crowd));
            Assert.That(aim.LastRange, Is.EqualTo(song.MaxRange));
            Assert.That((Vector3)Strike.ParamA, Is.EqualTo(new Vector3(0f, 0f, 9f)));
        }

        [Test]
        public void Cast_SongsAroundTheCore_NeverAskTheAimChooser()
        {
            var aim = new FakeSongAim();
            var context = new SwarmSongContext(default, null, default, _body, _forms, _fury, _hives, _enemies, _smoke, aim);

            Make<DomeSong>().Cast(in context);
            Make<TornadoSong>().Cast(in context);

            Assert.That(aim.Calls, Is.EqualTo(0));
            Assert.That(_forms.Slot(SongSlot.Guard).Form, Is.EqualTo(SwarmForm.Dome));
            Assert.That(Strike.Form, Is.EqualTo(SwarmForm.Tornado));
        }

        private T Make<T>() where T : SwarmSongEffect
        {
            var song = ScriptableObject.CreateInstance<T>();
            _assets.Add(song);
            return song;
        }

        private void Cast(SwarmSongEffect song, Vector3 core = default, ISwarmTarget aim = null, Vector3 aimPoint = default)
        {
            SwarmSongContext context = Context(core, aim, true, aimPoint);
            song.Cast(in context);
        }

        private SwarmSongContext Context(Vector3 core = default, ISwarmTarget aim = null, bool smoke = true, Vector3 aimPoint = default)
        {
            return new SwarmSongContext(core, aim, aimPoint, _body, _forms, _fury, _hives, _enemies, smoke ? _smoke : null);
        }

        private static void AssertSong(SwarmSongEffect song, string title, float cooldown, params int[] keys)
        {
            Assert.That(song.Song.Title, Is.EqualTo(title));
            Assert.That(song.Song.CooldownSeconds, Is.EqualTo(cooldown));
            Assert.That(song.Song.Keys, Is.EqualTo(keys));
        }
    }
}
