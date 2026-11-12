using System;
using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class SmokeFieldTests
    {
        private const float Tolerance = 0.001f;
        private const float FlightSeconds = 1.2f;
        private const float Radius = 4f;
        private const float LifeSeconds = 5f;
        private const float HiveDps = 4f;
        private static readonly Vector3 Far = new Vector3(100f, 0f, 100f);
        private static readonly Vector3 Landing = new Vector3(10f, 0f, 0f);

        private SmokeConfig _config;
        private SwarmConfig _swarmConfig;
        private HiveConfig _hiveConfig;
        private SmokeField _field;
        private SwarmBody _swarm;
        private List<SmokeCloud> _formed;
        private List<SmokeCloud> _ended;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<SmokeConfig>();
            _swarmConfig = ScriptableObject.CreateInstance<SwarmConfig>();
            _hiveConfig = ScriptableObject.CreateInstance<HiveConfig>();
            _field = new SmokeField(_config);
            _swarm = new SwarmBody(_swarmConfig);
            _formed = new List<SmokeCloud>();
            _ended = new List<SmokeCloud>();
            _field.CloudFormed += _formed.Add;
            _field.CloudEnded += _ended.Add;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_config);
            UnityEngine.Object.DestroyImmediate(_swarmConfig);
            UnityEngine.Object.DestroyImmediate(_hiveConfig);
        }

        private static HiveSet SetOf(params Hive[] hives)
        {
            return new HiveSet(hives);
        }

        private Hive HiveAt(Vector3 position)
        {
            return new Hive(_hiveConfig, position);
        }

        // With no shield, every loss and all damage gets through.
        private void Tick(float deltaTime, HiveSet hives = null, Vector3? core = null, IDamageShield shield = null)
        {
            _field.Tick(deltaTime, hives ?? SetOf(), _swarm, core ?? Far, shield ?? new FakeDamageShield());
        }

        private void LandACloudAt(Vector3 position)
        {
            _field.Throw(Far, position);
            Tick(FlightSeconds);
        }

        [Test]
        public void Constructor_NullConfig_Throws()
        {
            Assert.That(() => new SmokeField(null), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Throw_RaisesGrenadeThrownWithEndsAndFlightSeconds()
        {
            Vector3 gotFrom = default;
            Vector3 gotTo = default;
            float gotSeconds = -1f;
            _field.GrenadeThrown += (from, to, seconds) =>
            {
                gotFrom = from;
                gotTo = to;
                gotSeconds = seconds;
            };
            var start = new Vector3(1f, 1.5f, 2f);

            _field.Throw(start, Landing);

            Assert.That(gotFrom, Is.EqualTo(start));
            Assert.That(gotTo, Is.EqualTo(Landing));
            Assert.That(gotSeconds, Is.EqualTo(FlightSeconds).Within(Tolerance));
        }

        [Test]
        public void Throw_BeforeLanding_NoCloudYet()
        {
            _field.Throw(Far, Landing);

            Tick(FlightSeconds - 0.1f);

            Assert.That(_field.CloudCount, Is.EqualTo(0));
            Assert.That(_formed, Is.Empty);
        }

        [Test]
        public void Tick_FlightExactlyElapsed_FormsCloudAtTarget()
        {
            _field.Throw(Far, Landing);

            Tick(FlightSeconds);

            Assert.That(_field.CloudCount, Is.EqualTo(1));
            Assert.That(_formed, Has.Count.EqualTo(1));
            Assert.That(_formed[0].Position.x, Is.EqualTo(Landing.x).Within(Tolerance));
            Assert.That(_formed[0].Position.z, Is.EqualTo(Landing.z).Within(Tolerance));
            Assert.That(_formed[0].Radius, Is.EqualTo(Radius).Within(Tolerance));
            Assert.That(_formed[0].SecondsLeft, Is.EqualTo(LifeSeconds).Within(Tolerance));
        }

        [Test]
        public void Tick_FlightInSmallSteps_FormsExactlyOneCloud()
        {
            _field.Throw(Far, Landing);

            for (int i = 0; i < 100; i++)
            {
                Tick(0.05f);
            }

            Assert.That(_formed, Has.Count.EqualTo(1));
        }

        [Test]
        public void Tick_OneHugeStep_LandsOnceAndDoesNotExpireEarly()
        {
            _field.Throw(Far, Landing);

            Tick(FlightSeconds + 0.5f);

            Assert.That(_formed, Has.Count.EqualTo(1));
            Assert.That(_field.CloudCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_CloudOnGround_CountsDownSecondsLeft()
        {
            LandACloudAt(Landing);

            Tick(2f);

            Assert.That(_formed[0].SecondsLeft, Is.EqualTo(LifeSeconds - 2f).Within(Tolerance));
        }

        [Test]
        public void Tick_CloudLivesJustUnderLife_StillThere()
        {
            LandACloudAt(Landing);

            Tick(LifeSeconds - 0.1f);

            Assert.That(_field.CloudCount, Is.EqualTo(1));
            Assert.That(_ended, Is.Empty);
        }

        [Test]
        public void Tick_CloudLivesExactlyLife_EndsOnce()
        {
            LandACloudAt(Landing);

            Tick(LifeSeconds);
            Tick(1f);

            Assert.That(_field.CloudCount, Is.EqualTo(0));
            Assert.That(_ended, Has.Count.EqualTo(1));
            Assert.That(_ended[0], Is.SameAs(_formed[0]));
        }

        [Test]
        public void Tick_ZeroDelta_ChangesNothing()
        {
            var hive = HiveAt(Landing);
            LandACloudAt(Landing);

            Tick(0f, SetOf(hive), Landing);

            Assert.That(_formed[0].SecondsLeft, Is.EqualTo(LifeSeconds).Within(Tolerance));
            Assert.That(hive.Health, Is.EqualTo(hive.MaxHealth).Within(Tolerance));
            Assert.That(_swarm.Count, Is.EqualTo(_swarmConfig.StartingWasps));
        }

        [Test]
        public void Tick_HiveInsideCloud_LosesHealthPerSecond()
        {
            var hive = HiveAt(Landing + new Vector3(1f, 0f, 0f));
            LandACloudAt(Landing);

            Tick(1f, SetOf(hive));

            Assert.That(hive.Health, Is.EqualTo(hive.MaxHealth - HiveDps).Within(Tolerance));
        }

        [Test]
        public void Tick_HiveExactlyOnTheEdge_IsHurt()
        {
            var hive = HiveAt(Landing + new Vector3(Radius, 0f, 0f));
            LandACloudAt(Landing);

            Tick(1f, SetOf(hive));

            Assert.That(hive.Health, Is.EqualTo(hive.MaxHealth - HiveDps).Within(Tolerance));
        }

        [Test]
        public void Tick_HiveJustOutsideTheEdge_IsNotHurt()
        {
            var hive = HiveAt(Landing + new Vector3(Radius + 0.01f, 0f, 0f));
            LandACloudAt(Landing);

            Tick(1f, SetOf(hive));

            Assert.That(hive.Health, Is.EqualTo(hive.MaxHealth).Within(Tolerance));
        }

        [Test]
        public void Tick_HiveHeightIsIgnored()
        {
            var hive = HiveAt(Landing + new Vector3(0f, 30f, 0f));
            LandACloudAt(Landing);

            Tick(1f, SetOf(hive));

            Assert.That(hive.Health, Is.EqualTo(hive.MaxHealth - HiveDps).Within(Tolerance));
        }

        [Test]
        public void Tick_HiveInsideCloudBehindAShield_LosesOnlyTheShareTheShieldLetsThrough()
        {
            var hive = HiveAt(Landing);
            LandACloudAt(Landing);

            Tick(1f, SetOf(hive), shield: new FakeDamageShield { HiveFraction = 0.25f });

            Assert.That(hive.Health, Is.EqualTo(hive.MaxHealth - (HiveDps * 0.25f)).Within(Tolerance));
        }

        [Test]
        public void Tick_NullShield_Throws()
        {
            Assert.That(() => _field.Tick(1f, SetOf(), _swarm, Far, null), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Tick_TwoHives_OnlyTheOneInsideIsHurt()
        {
            var inside = HiveAt(Landing);
            var outside = HiveAt(Landing + new Vector3(25f, 0f, 0f));
            LandACloudAt(Landing);

            Tick(1f, SetOf(inside, outside));

            Assert.That(inside.Health, Is.EqualTo(inside.MaxHealth - HiveDps).Within(Tolerance));
            Assert.That(outside.Health, Is.EqualTo(outside.MaxHealth).Within(Tolerance));
        }

        [Test]
        public void Tick_DestroyedHiveInsideCloud_StaysDestroyed()
        {
            var hive = HiveAt(Landing);
            hive.Damage(hive.MaxHealth);
            LandACloudAt(Landing);

            Tick(1f, SetOf(hive));

            Assert.That(hive.IsDestroyed, Is.True);
            Assert.That(hive.Health, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Tick_AfterCloudExpired_HiveIsNoLongerHurt()
        {
            var hive = HiveAt(Landing);
            LandACloudAt(Landing);
            Tick(LifeSeconds, SetOf(hive));
            float healthAtExpiry = hive.Health;

            Tick(1f, SetOf(hive));

            Assert.That(hive.Health, Is.EqualTo(healthAtExpiry).Within(Tolerance));
        }

        [Test]
        public void Tick_GrenadeStillFlying_HiveNotHurt()
        {
            var hive = HiveAt(Landing);
            _field.Throw(Far, Landing);

            Tick(FlightSeconds - 0.1f, SetOf(hive));

            Assert.That(hive.Health, Is.EqualTo(hive.MaxHealth).Within(Tolerance));
        }

        [Test]
        public void Tick_CoreInsideCloud_SwarmLosesWaspsPerSecond()
        {
            LandACloudAt(Landing);

            Tick(1f, core: Landing);

            Assert.That(_swarm.Count, Is.EqualTo(_swarmConfig.StartingWasps - (int)_config.WaspLossPerSecond));
            Assert.That(_field.IsCoreInSmoke, Is.True);
        }

        [Test]
        public void Tick_CoreExactlyOnTheEdge_IsInSmoke()
        {
            LandACloudAt(Landing);

            Tick(0.1f, core: Landing + new Vector3(0f, 0f, Radius));

            Assert.That(_field.IsCoreInSmoke, Is.True);
        }

        [Test]
        public void Tick_CoreJustOutsideTheEdge_IsSafe()
        {
            LandACloudAt(Landing);

            Tick(1f, core: Landing + new Vector3(0f, 0f, Radius + 0.01f));

            Assert.That(_field.IsCoreInSmoke, Is.False);
            Assert.That(_swarm.Count, Is.EqualTo(_swarmConfig.StartingWasps));
        }

        [Test]
        public void Tick_CoreHeightIsIgnored()
        {
            LandACloudAt(Landing);

            Tick(0.1f, core: Landing + new Vector3(0f, 5f, 0f));

            Assert.That(_field.IsCoreInSmoke, Is.True);
        }

        [Test]
        public void Tick_CoreInSmokeWithDome_LosesOnlyTheDomeShare()
        {
            LandACloudAt(Landing);
            var dome = new FakeDamageShield { WaspLossFraction = 0.15f };
            int expected = _swarmConfig.StartingWasps - (int)Math.Round(_config.WaspLossPerSecond * dome.WaspLossFraction);

            Tick(1f, core: Landing, shield: dome);

            Assert.That(_swarm.Count, Is.EqualTo(expected));
        }

        [Test]
        public void Tick_ManySmallSteps_LossAddsUpWithoutLosingFractions()
        {
            LandACloudAt(Landing);

            for (int i = 0; i < 60; i++)
            {
                Tick(1f / 60f, core: Landing);
            }

            Assert.That(_swarm.Count, Is.EqualTo(_swarmConfig.StartingWasps - (int)_config.WaspLossPerSecond).Within(1));
        }

        [Test]
        public void Tick_TwoCloudsOverlapCore_DrainsOnlyOnce()
        {
            LandACloudAt(Landing);
            LandACloudAt(Landing);
            int before = _swarm.Count;

            Tick(1f, core: Landing);

            Assert.That(before - _swarm.Count, Is.EqualTo((int)_config.WaspLossPerSecond));
        }

        // Plan section O: a collapsed swarm cannot be smoked. It is not in a cloud, so nothing slows the core, and it comes
        // back into the smoke on the respawn.
        [Test]
        public void Tick_CollapsedSwarmInACloud_IsNotInSmoke_AndIsAgainOnceItRespawns()
        {
            LandACloudAt(Landing);
            _swarm.Lose(_swarm.Count);

            Tick(1f, core: Landing);
            Assert.That(_field.IsCoreInSmoke, Is.False);
            Assert.That(_swarm.Count, Is.EqualTo(0));

            _swarm.Tick(_swarmConfig.CollapseBlockSeconds, SetOf(HiveAt(Far)));
            Assert.That(_swarm.IsCollapsed, Is.False);
            int respawned = _swarm.Count;
            Tick(0.25f, core: Landing);
            Assert.That(_field.IsCoreInSmoke, Is.True);
            Assert.That(_swarm.Count, Is.EqualTo(respawned - (int)(_config.WaspLossPerSecond * 0.25f)), "a short breath, so the drain cannot collapse it again");
        }

        [Test]
        public void Tick_NoCloud_CoreIsNotInSmoke()
        {
            Tick(1f, core: Landing);

            Assert.That(_field.IsCoreInSmoke, Is.False);
        }

        [Test]
        public void IsCoreInSmoke_BeforeAnyTick_IsFalse()
        {
            Assert.That(_field.IsCoreInSmoke, Is.False);
        }

        [Test]
        public void Tick_CloudExpires_CoreIsNoLongerInSmoke()
        {
            LandACloudAt(Landing);
            Tick(LifeSeconds, core: Landing);

            Tick(0.1f, core: Landing);

            Assert.That(_field.IsCoreInSmoke, Is.False);
        }

        [Test]
        public void Tick_CoreWalksOut_StopsLosingWasps()
        {
            LandACloudAt(Landing);
            Tick(1f, core: Landing);
            int afterFirst = _swarm.Count;

            Tick(1f, core: Far);

            Assert.That(_swarm.Count, Is.EqualTo(afterFirst));
            Assert.That(_field.IsCoreInSmoke, Is.False);
        }

        [Test]
        public void ClearWithin_CloudNearby_ClearsItAndRaisesCloudEnded()
        {
            LandACloudAt(Landing);

            int cleared = _field.ClearWithin(Landing + new Vector3(2f, 0f, 0f), 3f);

            Assert.That(cleared, Is.EqualTo(1));
            Assert.That(_field.CloudCount, Is.EqualTo(0));
            Assert.That(_ended, Has.Count.EqualTo(1));
            Assert.That(_ended[0], Is.SameAs(_formed[0]));
        }

        [Test]
        public void ClearWithin_CentreExactlyAtRadius_Clears()
        {
            LandACloudAt(Landing);

            int cleared = _field.ClearWithin(Landing + new Vector3(3f, 0f, 0f), 3f);

            Assert.That(cleared, Is.EqualTo(1));
        }

        [Test]
        public void ClearWithin_CloudFarAway_ClearsNothing()
        {
            LandACloudAt(Landing);

            int cleared = _field.ClearWithin(Far, 5f);

            Assert.That(cleared, Is.EqualTo(0));
            Assert.That(_field.CloudCount, Is.EqualTo(1));
            Assert.That(_ended, Is.Empty);
        }

        [Test]
        public void ClearWithin_NoClouds_ReturnsZero()
        {
            Assert.That(_field.ClearWithin(Landing, 50f), Is.EqualTo(0));
        }

        [Test]
        public void ClearWithin_TwoClouds_CountsBoth()
        {
            LandACloudAt(Landing);
            LandACloudAt(Landing + new Vector3(1f, 0f, 0f));

            int cleared = _field.ClearWithin(Landing, 5f);

            Assert.That(cleared, Is.EqualTo(2));
            Assert.That(_field.CloudCount, Is.EqualTo(0));
        }

        [Test]
        public void ClearWithin_ThroughTheInterface_Works()
        {
            ISmokeClearer clearer = _field;
            LandACloudAt(Landing);

            Assert.That(clearer.ClearWithin(Landing, 1f), Is.EqualTo(1));
        }

        [Test]
        public void ClearWithin_ThenTick_HiveAndSwarmAreSafe()
        {
            var hive = HiveAt(Landing);
            LandACloudAt(Landing);
            _field.ClearWithin(Landing, 1f);

            Tick(1f, SetOf(hive), Landing);

            Assert.That(hive.Health, Is.EqualTo(hive.MaxHealth).Within(Tolerance));
            Assert.That(_swarm.Count, Is.EqualTo(_swarmConfig.StartingWasps));
            Assert.That(_field.IsCoreInSmoke, Is.False);
        }

        [Test]
        public void ClearWithin_GrenadeStillFlying_IsNotCleared()
        {
            _field.Throw(Far, Landing);

            int cleared = _field.ClearWithin(Landing, 50f);
            Tick(FlightSeconds);

            Assert.That(cleared, Is.EqualTo(0));
            Assert.That(_field.CloudCount, Is.EqualTo(1));
        }

        [Test]
        public void Throw_MoreThanCapacity_ExtraGrenadesAreIgnored()
        {
            int thrown = 0;
            _field.GrenadeThrown += (a, b, c) => thrown++;

            for (int i = 0; i < SmokeField.Capacity + 1; i++)
            {
                _field.Throw(Far, Landing + new Vector3(i * 20f, 0f, 0f));
            }
            Tick(FlightSeconds);

            Assert.That(thrown, Is.EqualTo(SmokeField.Capacity));
            Assert.That(_field.CloudCount, Is.EqualTo(SmokeField.Capacity));
        }

        [Test]
        public void Throw_AfterCloudsEnded_ReusesThePool()
        {
            for (int i = 0; i < SmokeField.Capacity; i++)
            {
                _field.Throw(Far, Landing);
            }
            Tick(FlightSeconds);
            Tick(LifeSeconds);
            Assert.That(_field.CloudCount, Is.EqualTo(0));

            _field.Throw(Far, Landing);
            Tick(FlightSeconds);

            Assert.That(_field.CloudCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_TwoClouds_ExpireIndependently()
        {
            LandACloudAt(Landing);
            Tick(2f);
            LandACloudAt(Landing + new Vector3(30f, 0f, 0f));
            // first cloud is now 2 + 1.2 s old, second is brand new

            Tick(LifeSeconds - 2f - FlightSeconds + 0.1f);

            Assert.That(_field.CloudCount, Is.EqualTo(1));
            Assert.That(_ended, Has.Count.EqualTo(1));
            Assert.That(_ended[0], Is.SameAs(_formed[0]));
        }
    }
}
