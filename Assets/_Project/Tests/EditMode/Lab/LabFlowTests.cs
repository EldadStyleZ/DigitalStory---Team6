using System.Collections.Generic;
using DigitalStory.Logic;
using NUnit.Framework;

namespace DigitalStory.Tests.EditMode
{
    public sealed class LabFlowTests
    {
        private LabFlow _flow;

        [SetUp]
        public void CreateFlow() => _flow = new LabFlow();

        [Test]
        public void Flow_AtTheStart_IsPlaying()
        {
            Assert.That(_flow.IsPlaying, Is.True);
        }

        [Test]
        public void TogglePause_Twice_ComesBack()
        {
            _flow.TogglePause();
            Assert.That(_flow.Phase, Is.EqualTo(LabPhase.Paused));

            _flow.TogglePause();
            Assert.That(_flow.Phase, Is.EqualTo(LabPhase.Playing));
        }

        [Test]
        public void Die_WhilePlaying_WaitsForAChoice()
        {
            _flow.Die();

            Assert.That(_flow.Phase, Is.EqualTo(LabPhase.Dead));
        }

        [Test]
        public void Revive_AfterDying_PlaysAgain()
        {
            _flow.Die();

            _flow.Revive();

            Assert.That(_flow.IsPlaying, Is.True);
        }

        [Test]
        public void Revive_WithoutDying_DoesNothing()
        {
            _flow.TogglePause();

            _flow.Revive();

            Assert.That(_flow.Phase, Is.EqualTo(LabPhase.Paused));
        }

        [Test]
        public void TogglePause_AfterDying_DoesNothing()
        {
            _flow.Die();

            _flow.TogglePause();

            Assert.That(_flow.Phase, Is.EqualTo(LabPhase.Dead));
        }

        [Test]
        public void Win_WhilePlaying_EndsTheGame()
        {
            _flow.Win();

            Assert.That(_flow.Phase, Is.EqualTo(LabPhase.Won));
        }

        [Test]
        public void Changed_OnEachMove_ReportsTheNewPhase()
        {
            var phases = new List<LabPhase>();
            _flow.Changed += phases.Add;

            _flow.Die();
            _flow.Revive();

            Assert.That(phases, Is.EqualTo(new[] { LabPhase.Dead, LabPhase.Playing }));
        }
    }
}
