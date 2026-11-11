using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class ColossusAttackChainTests
    {
        private const float JustOver = 0.01f;

        private ColossusConfig _config;
        private ColossusAttackChain _chain;
        private List<ColossusAttack> _started;
        private List<ColossusAttack> _landed;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<ColossusConfig>();
            _chain = new ColossusAttackChain(_config);
            _started = new List<ColossusAttack>();
            _landed = new List<ColossusAttack>();
            _chain.Started += _started.Add;
            _chain.Landed += _landed.Add;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        private void LandCurrent(float hitDelay)
        {
            _chain.Tick(hitDelay + JustOver);
        }

        [Test]
        public void New_IsNotBusy()
        {
            Assert.That(_chain.IsBusy, Is.False);
        }

        [Test]
        public void TryPunch_WhenIdle_StartsAPunchAndIsBusy()
        {
            bool started = _chain.TryPunch();

            Assert.That(started, Is.True);
            Assert.That(_chain.IsBusy, Is.True);
            Assert.That(_started, Is.EqualTo(new[] { ColossusAttack.Punch }));
            Assert.That(_landed, Is.Empty);
        }

        [Test]
        public void Tick_BeforeTheHitDelay_DoesNotLand()
        {
            _chain.TryPunch();

            _chain.Tick(_config.PunchHitDelay - JustOver);

            Assert.That(_landed, Is.Empty);
            Assert.That(_chain.IsBusy, Is.True);
        }

        [Test]
        public void Tick_ExactlyTheHitDelay_Lands()
        {
            _chain.TryPunch();

            _chain.Tick(_config.PunchHitDelay);

            Assert.That(_landed, Is.EqualTo(new[] { ColossusAttack.Punch }));
            Assert.That(_chain.IsBusy, Is.False);
        }

        [Test]
        public void Tick_HitDelayInSeveralSteps_LandsOnceAtTheEnd()
        {
            _chain.TryPunch();

            _chain.Tick(_config.PunchHitDelay * 0.5f);
            Assert.That(_landed, Is.Empty);
            _chain.Tick(_config.PunchHitDelay * 0.5f + JustOver);
            _chain.Tick(1f);

            Assert.That(_landed, Is.EqualTo(new[] { ColossusAttack.Punch }));
        }

        [Test]
        public void Tick_WhenIdle_RaisesNothing()
        {
            _chain.Tick(5f);

            Assert.That(_started, Is.Empty);
            Assert.That(_landed, Is.Empty);
            Assert.That(_chain.IsBusy, Is.False);
        }

        [Test]
        public void TryPunch_WhileBusy_IsRefused()
        {
            _chain.TryPunch();

            bool second = _chain.TryPunch();

            Assert.That(second, Is.False);
            Assert.That(_started.Count, Is.EqualTo(1));
        }

        [Test]
        public void TryPunch_ThreeInsideTheWindow_ThirdIsHeavy()
        {
            _chain.TryPunch();
            LandCurrent(_config.PunchHitDelay);
            _chain.TryPunch();
            LandCurrent(_config.PunchHitDelay);
            _chain.TryPunch();
            LandCurrent(_config.HeavyPunchHitDelay);

            Assert.That(
                _started,
                Is.EqualTo(new[] { ColossusAttack.Punch, ColossusAttack.Punch, ColossusAttack.HeavyPunch }));
            Assert.That(
                _landed,
                Is.EqualTo(new[] { ColossusAttack.Punch, ColossusAttack.Punch, ColossusAttack.HeavyPunch }));
        }

        [Test]
        public void TryPunch_HeavyPunch_UsesItsOwnHitDelay()
        {
            _chain.TryPunch();
            LandCurrent(_config.PunchHitDelay);
            _chain.TryPunch();
            LandCurrent(_config.PunchHitDelay);
            _chain.TryPunch();

            _chain.Tick(_config.HeavyPunchHitDelay - JustOver);
            Assert.That(_chain.IsBusy, Is.True);
            Assert.That(_landed.Count, Is.EqualTo(2));

            _chain.Tick(JustOver * 2f);
            Assert.That(_chain.IsBusy, Is.False);
            Assert.That(_landed.Count, Is.EqualTo(3));
        }

        [Test]
        public void TryPunch_AfterTheHeavyPunch_TheChainRestarts()
        {
            for (int i = 0; i < 2; i++)
            {
                _chain.TryPunch();
                LandCurrent(_config.PunchHitDelay);
            }

            _chain.TryPunch();
            LandCurrent(_config.HeavyPunchHitDelay);
            _chain.TryPunch();
            LandCurrent(_config.PunchHitDelay);
            _chain.TryPunch();
            LandCurrent(_config.PunchHitDelay);
            _chain.TryPunch();

            Assert.That(_started[3], Is.EqualTo(ColossusAttack.Punch));
            Assert.That(_started[4], Is.EqualTo(ColossusAttack.Punch));
            Assert.That(_started[5], Is.EqualTo(ColossusAttack.HeavyPunch));
        }

        [Test]
        public void TryPunch_WindowStillOpen_ContinuesTheChain()
        {
            _chain.TryPunch();
            LandCurrent(_config.PunchHitDelay);
            _chain.Tick(_config.ChainWindowSeconds - JustOver * 2f);
            _chain.TryPunch();
            LandCurrent(_config.PunchHitDelay);
            _chain.Tick(_config.ChainWindowSeconds - JustOver * 2f);
            _chain.TryPunch();

            Assert.That(_started[2], Is.EqualTo(ColossusAttack.HeavyPunch));
        }

        [Test]
        public void TryPunch_WindowExpiredBeforeTheSecond_ChainRestarts()
        {
            _chain.TryPunch();
            LandCurrent(_config.PunchHitDelay);
            _chain.Tick(_config.ChainWindowSeconds + JustOver);
            _chain.TryPunch();
            LandCurrent(_config.PunchHitDelay);
            _chain.TryPunch();

            Assert.That(
                _started,
                Is.EqualTo(new[] { ColossusAttack.Punch, ColossusAttack.Punch, ColossusAttack.Punch }));
        }

        [Test]
        public void TryPunch_WindowExpiredBeforeTheThird_ThirdIsNotHeavy()
        {
            _chain.TryPunch();
            LandCurrent(_config.PunchHitDelay);
            _chain.TryPunch();
            LandCurrent(_config.PunchHitDelay);
            _chain.Tick(_config.ChainWindowSeconds + JustOver);
            _chain.TryPunch();

            Assert.That(_started[2], Is.EqualTo(ColossusAttack.Punch));
        }

        [Test]
        public void TryPunch_WindowDoesNotRunWhileThePunchIsStillInTheAir()
        {
            _chain.TryPunch();
            _chain.Tick(_config.PunchHitDelay - JustOver);
            _chain.Tick(JustOver * 2f);
            _chain.Tick(_config.ChainWindowSeconds - JustOver * 2f);
            _chain.TryPunch();

            Assert.That(_started.Count, Is.EqualTo(2));
        }

        [Test]
        public void TryKick_WhenIdle_StartsAKickAndLandsAfterItsDelay()
        {
            bool started = _chain.TryKick();

            Assert.That(started, Is.True);
            Assert.That(_chain.IsBusy, Is.True);
            Assert.That(_started, Is.EqualTo(new[] { ColossusAttack.Kick }));

            _chain.Tick(_config.KickHitDelay - JustOver);
            Assert.That(_landed, Is.Empty);
            _chain.Tick(JustOver * 2f);
            Assert.That(_landed, Is.EqualTo(new[] { ColossusAttack.Kick }));
            Assert.That(_chain.IsBusy, Is.False);
        }

        [Test]
        public void TryKick_WhilePunching_IsRefused()
        {
            _chain.TryPunch();

            Assert.That(_chain.TryKick(), Is.False);
            Assert.That(_started, Is.EqualTo(new[] { ColossusAttack.Punch }));
        }

        [Test]
        public void TryPunch_WhileKicking_IsRefused()
        {
            _chain.TryKick();

            Assert.That(_chain.TryPunch(), Is.False);
            Assert.That(_started, Is.EqualTo(new[] { ColossusAttack.Kick }));
        }

        [Test]
        public void TryKick_AfterLandingButStillCoolingDown_IsRefused()
        {
            _chain.TryKick();
            LandCurrent(_config.KickHitDelay);

            Assert.That(_chain.IsBusy, Is.False);
            Assert.That(_chain.TryKick(), Is.False);
            Assert.That(_started.Count, Is.EqualTo(1));
        }

        [Test]
        public void TryKick_JustBeforeTheCooldownEnds_IsRefused()
        {
            _chain.TryKick();
            _chain.Tick(_config.KickCooldownSeconds - JustOver);

            Assert.That(_chain.TryKick(), Is.False);
        }

        [Test]
        public void TryKick_AfterTheCooldown_Works()
        {
            _chain.TryKick();
            _chain.Tick(_config.KickCooldownSeconds + JustOver);

            Assert.That(_chain.TryKick(), Is.True);
            Assert.That(_started, Is.EqualTo(new[] { ColossusAttack.Kick, ColossusAttack.Kick }));
        }

        [Test]
        public void TryPunch_DuringTheKickCooldown_Works()
        {
            _chain.TryKick();
            LandCurrent(_config.KickHitDelay);

            Assert.That(_chain.TryPunch(), Is.True);
        }

        [Test]
        public void Tick_ZeroDelta_ChangesNothing()
        {
            _chain.TryPunch();

            _chain.Tick(0f);

            Assert.That(_landed, Is.Empty);
            Assert.That(_chain.IsBusy, Is.True);
        }

        [Test]
        public void Events_StartedComesBeforeLanded()
        {
            var order = new List<string>();
            _chain.Started += attack => order.Add("started " + attack);
            _chain.Landed += attack => order.Add("landed " + attack);

            _chain.TryKick();
            LandCurrent(_config.KickHitDelay);

            Assert.That(order, Is.EqualTo(new[] { "started Kick", "landed Kick" }));
        }
    }
}
