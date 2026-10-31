using DigitalStory.Logic;
using NUnit.Framework;

namespace DigitalStory.Tests.EditMode
{
    public sealed class GameFlowTests
    {
        [Test]
        public void Constructor_NewGame_StartsOnTheTitleAndNotPlaying()
        {
            var flow = new GameFlow();

            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Title));
            Assert.That(flow.IsPlaying, Is.False);
        }

        [Test]
        public void Begin_OnTheTitle_StartsPlaying()
        {
            var flow = new GameFlow();

            flow.Begin();

            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Playing));
            Assert.That(flow.IsPlaying, Is.True);
        }

        [TestCase(GamePhase.Paused)]
        [TestCase(GamePhase.Ended)]
        public void Begin_AfterTheTitle_DoesNothing(GamePhase phase)
        {
            GameFlow flow = FlowIn(phase);

            flow.Begin();

            Assert.That(flow.Phase, Is.EqualTo(phase));
        }

        [Test]
        public void TogglePause_WhilePlaying_Pauses()
        {
            GameFlow flow = FlowIn(GamePhase.Playing);

            flow.TogglePause();

            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Paused));
            Assert.That(flow.IsPlaying, Is.False);
        }

        [Test]
        public void TogglePause_WhilePaused_Resumes()
        {
            GameFlow flow = FlowIn(GamePhase.Paused);

            flow.TogglePause();

            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Playing));
        }

        [TestCase(GamePhase.Title)]
        [TestCase(GamePhase.Ended)]
        public void TogglePause_WithNoNightRunning_DoesNothing(GamePhase phase)
        {
            GameFlow flow = FlowIn(phase);

            flow.TogglePause();

            Assert.That(flow.Phase, Is.EqualTo(phase));
        }

        [Test]
        public void Pause_WhilePlaying_Pauses()
        {
            GameFlow flow = FlowIn(GamePhase.Playing);

            flow.Pause();

            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Paused));
        }

        [TestCase(GamePhase.Title)]
        [TestCase(GamePhase.Paused)]
        [TestCase(GamePhase.Ended)]
        public void Pause_WhileNotPlaying_DoesNothing(GamePhase phase)
        {
            GameFlow flow = FlowIn(phase);

            flow.Pause();

            Assert.That(flow.Phase, Is.EqualTo(phase));
        }

        [Test]
        public void End_WhilePlaying_EndsTheGame()
        {
            GameFlow flow = FlowIn(GamePhase.Playing);

            flow.End();

            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Ended));
        }

        [TestCase(GamePhase.Title)]
        [TestCase(GamePhase.Paused)]
        public void End_WithNoNightRunning_DoesNothing(GamePhase phase)
        {
            GameFlow flow = FlowIn(phase);

            flow.End();

            Assert.That(flow.Phase, Is.EqualTo(phase));
        }

        [Test]
        public void Changed_OnAMove_ReportsTheNewPhase()
        {
            var flow = new GameFlow();
            GamePhase? reported = null;
            flow.Changed += phase => reported = phase;

            flow.Begin();

            Assert.That(reported, Is.EqualTo(GamePhase.Playing));
        }

        [Test]
        public void Changed_OnAMoveThatDoesNotApply_IsNotRaised()
        {
            var flow = new GameFlow();
            var raised = 0;
            flow.Changed += _ => raised++;

            flow.End();
            flow.TogglePause();

            Assert.That(raised, Is.EqualTo(0));
        }

        private static GameFlow FlowIn(GamePhase phase)
        {
            var flow = new GameFlow();

            if (phase != GamePhase.Title)
            {
                flow.Begin();
            }

            if (phase == GamePhase.Paused)
            {
                flow.TogglePause();
            }

            if (phase == GamePhase.Ended)
            {
                flow.End();
            }

            return flow;
        }
    }
}
