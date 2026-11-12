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
        public void TryKick_AfterThePhraseWindowButStillCoolingDown_IsRefused()
        {
            // Changed with plan section I: a second kick inside the phrase window is now a spin kick, so this waits past
            // the window. It still proves that the cooldown outlasts the kick's hit.
            Assert.That(_config.PhraseWindowSeconds + JustOver, Is.LessThan(_config.KickCooldownSeconds), "the test needs a gap");
            _chain.TryKick();
            _chain.Tick(_config.PhraseWindowSeconds + JustOver);

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

        // A slow second kick, past the phrase window, is a plain kick (plan section I).
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

        // ---- The giant, version 2 (plan section I): the slam, the roar and the two phrases ----

        [Test]
        public void New_TheSlamAndTheRoarAreReady()
        {
            Assert.That(_chain.SlamCooldown01, Is.EqualTo(0f));
            Assert.That(_chain.RoarCooldown01, Is.EqualTo(0f));
        }

        [Test]
        public void TrySlam_WhenIdle_StartsASlamThatLandsAfterItsDelay()
        {
            Assert.That(_chain.TrySlam(), Is.True);
            Assert.That(_chain.IsBusy, Is.True);
            Assert.That(_started, Is.EqualTo(new[] { ColossusAttack.Slam }));

            _chain.Tick(_config.SlamHitDelay - JustOver);
            Assert.That(_landed, Is.Empty);
            _chain.Tick(JustOver * 2f);

            Assert.That(_landed, Is.EqualTo(new[] { ColossusAttack.Slam }));
            Assert.That(_chain.IsBusy, Is.False);
        }

        [Test]
        public void TrySlam_AfterThePhraseWindow_IsRefusedUntilItsCooldownEnds()
        {
            _chain.TrySlam();
            _chain.Tick(_config.PhraseWindowSeconds + JustOver);

            Assert.That(_chain.TrySlam(), Is.False, "still cooling down");

            _chain.Tick(_config.SlamCooldownSeconds - _config.PhraseWindowSeconds);

            Assert.That(_chain.TrySlam(), Is.True);
            Assert.That(_started, Is.EqualTo(new[] { ColossusAttack.Slam, ColossusAttack.Slam }), "a plain slam, not a leap");
        }

        [Test]
        public void SlamCooldown01_IsOneWhenUsedAndFallsToZeroWhenReady()
        {
            _chain.TrySlam();
            Assert.That(_chain.SlamCooldown01, Is.EqualTo(1f).Within(1e-4f));

            _chain.Tick(_config.SlamCooldownSeconds * 0.5f);
            Assert.That(_chain.SlamCooldown01, Is.EqualTo(0.5f).Within(1e-4f));

            _chain.Tick(_config.SlamCooldownSeconds);
            Assert.That(_chain.SlamCooldown01, Is.EqualTo(0f));
            Assert.That(_chain.RoarCooldown01, Is.EqualTo(0f), "the roar has its own cooldown");
        }

        [Test]
        public void TryRoar_WhenIdle_StartsARoarThatLandsAfterItsDelay()
        {
            Assert.That(_chain.TryRoar(), Is.True);
            Assert.That(_started, Is.EqualTo(new[] { ColossusAttack.Roar }));

            _chain.Tick(_config.RoarHitDelay - JustOver);
            Assert.That(_landed, Is.Empty);
            _chain.Tick(JustOver * 2f);

            Assert.That(_landed, Is.EqualTo(new[] { ColossusAttack.Roar }));
        }

        [Test]
        public void TryRoar_CoolsDownForItsSecondsAndSaysHowFar()
        {
            _chain.TryRoar();
            Assert.That(_chain.RoarCooldown01, Is.EqualTo(1f).Within(1e-4f));

            _chain.Tick(_config.RoarCooldownSeconds - JustOver);
            Assert.That(_chain.TryRoar(), Is.False);
            Assert.That(_chain.RoarCooldown01, Is.GreaterThan(0f));

            _chain.Tick(JustOver * 2f);
            Assert.That(_chain.RoarCooldown01, Is.EqualTo(0f));
            Assert.That(_chain.TryRoar(), Is.True);
        }

        [Test]
        public void TryRoar_WhileBusy_IsRefused()
        {
            _chain.TryPunch();

            Assert.That(_chain.TryRoar(), Is.False);
            Assert.That(_chain.RoarCooldown01, Is.EqualTo(0f), "a refused roar starts no cooldown");
        }

        [Test]
        public void TryRoar_BetweenPunches_RestartsThePunchChain()
        {
            _chain.TryPunch();
            LandCurrent(_config.PunchHitDelay);
            _chain.TryPunch();
            LandCurrent(_config.PunchHitDelay);
            _chain.TryRoar();
            LandCurrent(_config.RoarHitDelay);
            _chain.TryPunch();

            Assert.That(_started[3], Is.EqualTo(ColossusAttack.Punch));
        }

        [Test]
        public void TryKick_SecondWithinThePhraseWindowAfterTheFirstLanded_IsASpinKick()
        {
            _chain.TryKick();
            LandCurrent(_config.KickHitDelay);

            Assert.That(_chain.TryKick(), Is.True, "the kick still cools down, but the phrase wins");
            Assert.That(_started, Is.EqualTo(new[] { ColossusAttack.Kick, ColossusAttack.SpinKick }));

            _chain.Tick(_config.SpinKickHitDelay - JustOver);
            Assert.That(_landed, Is.EqualTo(new[] { ColossusAttack.Kick }));
            _chain.Tick(JustOver * 2f);
            Assert.That(_landed, Is.EqualTo(new[] { ColossusAttack.Kick, ColossusAttack.SpinKick }));
        }

        // A quick K K is not lost: the spin kick waits for the first kick to land, then starts.
        [Test]
        public void TryKick_SecondWhileTheFirstIsInTheAir_SpinsAsTheFirstLands()
        {
            const float Early = 0.1f;
            _chain.TryKick();
            _chain.Tick(Early);

            Assert.That(_chain.TryKick(), Is.True);
            Assert.That(_started, Is.EqualTo(new[] { ColossusAttack.Kick }), "the first kick is still in the air");

            _chain.Tick(_config.KickHitDelay - Early + JustOver);
            Assert.That(_landed, Is.EqualTo(new[] { ColossusAttack.Kick }));
            Assert.That(_started, Is.EqualTo(new[] { ColossusAttack.Kick, ColossusAttack.SpinKick }));
            Assert.That(_chain.IsBusy, Is.True);

            _chain.Tick(_config.SpinKickHitDelay + JustOver);
            Assert.That(_landed, Is.EqualTo(new[] { ColossusAttack.Kick, ColossusAttack.SpinKick }));
        }

        [Test]
        public void TryKick_ThirdRightAfterASpinKick_IsNoSecondSpin()
        {
            _chain.TryKick();
            LandCurrent(_config.KickHitDelay);
            _chain.TryKick();
            LandCurrent(_config.SpinKickHitDelay);

            Assert.That(_chain.TryKick(), Is.False, "the phrase is used up and the kick still cools down");
            Assert.That(_started.Count, Is.EqualTo(2));
        }

        [Test]
        public void TrySlam_SecondWithinThePhraseWindow_IsALeapSlamThatLandsAfterTheLeap()
        {
            _chain.TrySlam();
            LandCurrent(_config.SlamHitDelay);

            Assert.That(_chain.TrySlam(), Is.True, "the slam still cools down, but the phrase wins");
            Assert.That(_started, Is.EqualTo(new[] { ColossusAttack.Slam, ColossusAttack.LeapSlam }));

            _chain.Tick(_config.LeapSeconds - JustOver);
            Assert.That(_landed, Is.EqualTo(new[] { ColossusAttack.Slam }), "still in the air");
            Assert.That(_chain.IsBusy, Is.True);
            _chain.Tick(JustOver * 2f);
            Assert.That(_landed, Is.EqualTo(new[] { ColossusAttack.Slam, ColossusAttack.LeapSlam }));
        }

        [Test]
        public void TrySlam_SecondWhileTheSlamWindsUp_LeapsAsItLands()
        {
            const float Early = 0.1f;
            _chain.TrySlam();
            _chain.Tick(Early);

            Assert.That(_chain.TrySlam(), Is.True);
            Assert.That(_started, Is.EqualTo(new[] { ColossusAttack.Slam }));

            _chain.Tick(_config.SlamHitDelay - Early + JustOver);

            Assert.That(_landed, Is.EqualTo(new[] { ColossusAttack.Slam }));
            Assert.That(_started, Is.EqualTo(new[] { ColossusAttack.Slam, ColossusAttack.LeapSlam }));
        }

        [Test]
        public void TrySlam_SecondAfterThePhraseWindow_IsNoLeap()
        {
            _chain.TrySlam();
            _chain.Tick(_config.PhraseWindowSeconds + JustOver);

            Assert.That(_chain.TrySlam(), Is.False);
            Assert.That(_started, Is.EqualTo(new[] { ColossusAttack.Slam }));
        }

        [Test]
        public void Phrase_AnotherMoveInBetween_BreaksIt()
        {
            _chain.TryKick();
            LandCurrent(_config.KickHitDelay);
            _chain.TryPunch();
            LandCurrent(_config.PunchHitDelay);

            Assert.That(_chain.TryKick(), Is.False, "K J K is no spin kick, and the kick still cools down");
            Assert.That(_started, Is.EqualTo(new[] { ColossusAttack.Kick, ColossusAttack.Punch }));
        }

        [Test]
        public void Phrase_KickThenSlam_IsAPlainSlam()
        {
            _chain.TryKick();
            LandCurrent(_config.KickHitDelay);

            Assert.That(_chain.TrySlam(), Is.True);
            Assert.That(_started, Is.EqualTo(new[] { ColossusAttack.Kick, ColossusAttack.Slam }));
        }

        [Test]
        public void Reset_DropsTheAttackUnderWayTheWaitingPhraseAndEveryCooldown()
        {
            _chain.TryRoar();
            LandCurrent(_config.RoarHitDelay);
            _chain.TryKick();
            _chain.Tick(0.1f);
            _chain.TryKick();
            _started.Clear();
            _landed.Clear();

            _chain.Reset();

            Assert.That(_chain.IsBusy, Is.False);
            Assert.That(_chain.RoarCooldown01, Is.EqualTo(0f));
            _chain.Tick(2f);
            Assert.That(_started, Is.Empty, "the waiting spin kick never starts");
            Assert.That(_landed, Is.Empty, "the kick in the air never lands");
            Assert.That(_chain.TryKick(), Is.True, "no cooldown is left");
            Assert.That(_started, Is.EqualTo(new[] { ColossusAttack.Kick }), "and no phrase");
        }
    }
}
