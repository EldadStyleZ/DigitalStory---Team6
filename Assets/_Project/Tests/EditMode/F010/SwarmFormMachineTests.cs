using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DigitalStory.Tests.EditMode
{
    public sealed class SwarmFormMachineTests
    {
        private SwarmConfig _config;
        private FuryConfig _furyConfig;
        private SwarmBody _body;
        private FuryMeter _fury;
        private SwarmFormMachine _forms;
        private List<SwarmForm> _changes;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<SwarmConfig>();
            _furyConfig = ScriptableObject.CreateInstance<FuryConfig>();
            Build();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_furyConfig);
        }

        [Test]
        public void New_Machine_StartsInTheRiver()
        {
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_forms.SecondsInForm, Is.EqualTo(0f));
            Assert.That(_forms.IsDomed, Is.False);
            Assert.That(_forms.IsPiperRooted, Is.False);
            Assert.That(_forms.StingTarget, Is.Null);
            Assert.That(_forms.StingFraction, Is.EqualTo(0f));
        }

        [Test]
        public void StartDome_InTheRiver_DomesTheSwarmAndRaisesFormChanged()
        {
            bool started = _forms.StartDome(5f);

            Assert.That(started, Is.True);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Dome));
            Assert.That(_forms.IsDomed, Is.True);
            Assert.That(_forms.IsPiperRooted, Is.False);
            Assert.That(_changes, Is.EqualTo(new[] { SwarmForm.Dome }));
        }

        [TestCase(SwarmForm.Dome)]
        [TestCase(SwarmForm.Sting)]
        [TestCase(SwarmForm.Tornado)]
        [TestCase(SwarmForm.Gust)]
        [TestCase(SwarmForm.Colossus)]
        public void Tick_FormReachesItsDuration_ReturnsToTheRiver(SwarmForm form)
        {
            Start(form, 4f);

            _forms.Tick(3.5f, 2);
            Assert.That(_forms.Current, Is.EqualTo(form), "still running before its duration");

            _forms.Tick(0.5f, 2);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_changes[_changes.Count - 1], Is.EqualTo(SwarmForm.River));
            Assert.That(_forms.IsDomed, Is.False);
            Assert.That(_forms.IsPiperRooted, Is.False);
        }

        [TestCase(SwarmForm.Dome)]
        [TestCase(SwarmForm.Tornado)]
        [TestCase(SwarmForm.Gust)]
        [TestCase(SwarmForm.Colossus)]
        public void Tick_OneStepLongerThanTheDuration_EndsTheFormOnce(SwarmForm form)
        {
            Start(form, 1f);
            _changes.Clear();

            _forms.Tick(30f, 2);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_changes, Is.EqualTo(new[] { SwarmForm.River }));
        }

        [Test]
        public void Tick_InAForm_CountsSecondsInForm()
        {
            _forms.StartDome(5f);

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
        public void SecondsInForm_FormChanges_StartsAgainFromZero()
        {
            _forms.StartDome(5f);
            _forms.Tick(2f, 2);

            _forms.StartGust(1f);

            Assert.That(_forms.SecondsInForm, Is.EqualTo(0f));
        }

        [Test]
        public void StartGust_WhileDomed_ReplacesTheDome()
        {
            _forms.StartDome(5f);

            bool started = _forms.StartGust(0.8f);

            Assert.That(started, Is.True);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Gust));
            Assert.That(_forms.IsDomed, Is.False);
            Assert.That(_changes, Is.EqualTo(new[] { SwarmForm.Dome, SwarmForm.Gust }));
        }

        // Sting

        [Test]
        public void StartSting_AliveTarget_WrapsTheTarget()
        {
            var target = new FakeSwarmTarget();

            bool started = _forms.StartSting(target, 4f, 0.4f, 25f);

            Assert.That(started, Is.True);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Sting));
            Assert.That(_forms.StingTarget, Is.SameAs(target));
            Assert.That(_forms.StingFraction, Is.EqualTo(0.4f));
        }

        [Test]
        public void StartSting_NoTarget_ChangesNothing()
        {
            bool started = _forms.StartSting(null, 4f, 0.4f, 25f);

            Assert.That(started, Is.False);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_changes, Is.Empty);
        }

        [Test]
        public void StartSting_DeadTarget_ChangesNothing()
        {
            var target = new FakeSwarmTarget { IsAlive = false };

            bool started = _forms.StartSting(target, 4f, 0.4f, 25f);

            Assert.That(started, Is.False);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        [Test]
        public void Tick_Sting_DamagesTheTargetByDamagePerSecond()
        {
            var target = new FakeSwarmTarget();
            _forms.StartSting(target, 4f, 0.4f, 25f);

            _forms.Tick(0.5f, 2);

            Assert.That(target.DamageTaken, Is.EqualTo(12.5f).Within(1e-3f));
        }

        [Test]
        public void Tick_Sting_FillsFuryFromTheDamage()
        {
            var target = new FakeSwarmTarget();
            _forms.StartSting(target, 4f, 0.4f, 25f);

            _forms.Tick(1f, 2);

            Assert.That(_fury.Value, Is.EqualTo(12.5f).Within(1e-3f), "25 damage at 2 damage per Fury point");
        }

        [Test]
        public void Tick_StingStepPastItsEnd_OnlyDamagesForTheTimeLeft()
        {
            var target = new FakeSwarmTarget();
            _forms.StartSting(target, 4f, 0.4f, 25f);

            _forms.Tick(10f, 2);

            Assert.That(target.DamageTaken, Is.EqualTo(100f).Within(1e-2f), "4 s at 25 a second, not 10 s");
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        [Test]
        public void Tick_StingTargetDies_EndsTheStingAndDealsNoMoreDamage()
        {
            var target = new FakeSwarmTarget();
            _forms.StartSting(target, 4f, 0.4f, 25f);
            _forms.Tick(0.5f, 2);
            target.IsAlive = false;

            _forms.Tick(0.5f, 2);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(target.DamageTaken, Is.EqualTo(12.5f).Within(1e-3f));
        }

        [Test]
        public void Tick_StingEnds_ForgetsTheTarget()
        {
            _forms.StartSting(new FakeSwarmTarget(), 1f, 0.4f, 25f);

            _forms.Tick(1f, 2);

            Assert.That(_forms.StingTarget, Is.Null);
            Assert.That(_forms.StingFraction, Is.EqualTo(0f));
        }

        // Tornado and Colossus fill Fury

        [Test]
        public void StartTornado_DamageDealt_FillsFury()
        {
            _forms.StartTornado(1.5f, 60f);

            Assert.That(_fury.Value, Is.EqualTo(30f).Within(1e-3f));
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Tornado));
        }

        [Test]
        public void StartTornado_NoDamage_StillSpins()
        {
            bool started = _forms.StartTornado(1.5f, 0f);

            Assert.That(started, Is.True);
            Assert.That(_fury.Value, Is.EqualTo(0f));
        }

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
            _forms.StartDome(5f);

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

            _body.Lose(4500, false);

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

            Assert.That(_body.Count, Is.EqualTo(5500), "250 a second for each of 2 hives");
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.HiveCall), "no end before the swarm is full");
        }

        [Test]
        public void Tick_HiveCallWithNoStandingHives_AddsNothing()
        {
            _forms.StartHiveCall();

            _forms.Tick(1f, 0);

            Assert.That(_body.Count, Is.EqualTo(5000));
        }

        [Test]
        public void NotifyHit_DuringHiveCall_CancelsIt()
        {
            _forms.StartHiveCall();

            _forms.NotifyHit();

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
            _forms.NotifyHit();

            _forms.NotifyHit();
            _forms.NotifyPiperMoved();

            Assert.That(_changes, Is.EqualTo(new[] { SwarmForm.HiveCall, SwarmForm.River }));
        }

        [TestCase(SwarmForm.Dome)]
        [TestCase(SwarmForm.Colossus)]
        [TestCase(SwarmForm.River)]
        public void NotifyHitAndMoved_OutsideHiveCall_ChangeNothing(SwarmForm form)
        {
            Start(form, 5f);
            _changes.Clear();

            _forms.NotifyHit();
            _forms.NotifyPiperMoved();

            Assert.That(_forms.Current, Is.EqualTo(form));
            Assert.That(_changes, Is.Empty);
        }

        [Test]
        public void Tick_HiveCallFillsTheSwarm_EndsAtTheMaximum()
        {
            SetInt("StartingWasps", 9900);
            Build();
            _forms.StartHiveCall();

            _forms.Tick(1f, 1);

            Assert.That(_body.Count, Is.EqualTo(10000), "capped at the maximum");
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        [Test]
        public void Tick_HiveCallJustBelowTheMaximum_KeepsCalling()
        {
            SetInt("StartingWasps", 9000);
            Build();
            _forms.StartHiveCall();

            _forms.Tick(1f, 1);

            Assert.That(_body.Count, Is.EqualTo(9250));
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.HiveCall));
        }

        // Collapse

        [Test]
        public void Collapse_WhileDomed_GoesToCollapsedAndDropsTheDome()
        {
            _forms.StartDome(5f);

            _body.Lose(4500, false);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
            Assert.That(_forms.IsDomed, Is.False);
            Assert.That(_changes, Is.EqualTo(new[] { SwarmForm.Dome, SwarmForm.Collapsed }));
        }

        [Test]
        public void Collapse_WhileInTheRiver_GoesToCollapsed()
        {
            _body.Lose(4500, false);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
            Assert.That(_forms.IsPiperRooted, Is.False);
        }

        [Test]
        public void Collapse_WhileStinging_ForgetsTheTarget()
        {
            _forms.StartSting(new FakeSwarmTarget(), 4f, 0.4f, 25f);

            _body.Lose(4500, false);

            Assert.That(_forms.StingTarget, Is.Null);
        }

        [Test]
        public void Start_WhileCollapsed_IsRefused()
        {
            _body.Lose(4500, false);
            _changes.Clear();

            Assert.That(_forms.StartDome(5f), Is.False);
            Assert.That(_forms.StartSting(new FakeSwarmTarget(), 4f, 0.4f, 25f), Is.False);
            Assert.That(_forms.StartTornado(1.5f, 30f), Is.False);
            Assert.That(_forms.StartGust(0.8f), Is.False);
            Assert.That(_forms.StartHiveCall(), Is.False);
            Assert.That(_forms.StartColossus(12f), Is.False);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
            Assert.That(_changes, Is.Empty);
            Assert.That(_fury.Value, Is.EqualTo(0f), "a refused tornado fills no Fury");
        }

        [Test]
        public void Tick_WhileCollapsed_StaysCollapsedWithoutTheBodyRespawning()
        {
            _body.Lose(4500, false);

            _forms.Tick(1f, 2);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
        }

        [Test]
        public void Respawn_AfterTheBlock_GoesBackToTheRiver()
        {
            _body.Lose(4500, false);
            _changes.Clear();

            _body.Tick(3f, 2);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_changes, Is.EqualTo(new[] { SwarmForm.River }));
            Assert.That(_forms.StartDome(5f), Is.True, "songs work again after the respawn");
        }

        // Allocation

        [Test]
        public void Tick_StingAndHiveCall_AllocateNothing()
        {
            var target = new FakeSwarmTarget();
            _forms.StartSting(target, 100f, 0.4f, 25f);
            Assert.That(() => _forms.Tick(0.016f, 2), Is.Not.AllocatingGCMemory());

            _forms.StartHiveCall();
            Assert.That(() => _forms.Tick(0.016f, 2), Is.Not.AllocatingGCMemory());
        }

        private void Build()
        {
            _body = new SwarmBody(_config);
            _fury = new FuryMeter(_furyConfig);
            _forms = new SwarmFormMachine(_body, _fury);
            _changes = new List<SwarmForm>();
            _forms.FormChanged += form => _changes.Add(form);
        }

        private void SetInt(string name, int value)
        {
            JsonUtility.FromJsonOverwrite("{\"<" + name + ">k__BackingField\":" + value + "}", _config);
        }

        private void Start(SwarmForm form, float seconds)
        {
            switch (form)
            {
                case SwarmForm.Dome:
                    _forms.StartDome(seconds);
                    break;
                case SwarmForm.Sting:
                    _forms.StartSting(new FakeSwarmTarget(), seconds, 0.4f, 25f);
                    break;
                case SwarmForm.Tornado:
                    _forms.StartTornado(seconds, 0f);
                    break;
                case SwarmForm.Gust:
                    _forms.StartGust(seconds);
                    break;
                case SwarmForm.HiveCall:
                    _forms.StartHiveCall();
                    break;
                case SwarmForm.Colossus:
                    _forms.StartColossus(seconds);
                    break;
            }
        }
    }
}
