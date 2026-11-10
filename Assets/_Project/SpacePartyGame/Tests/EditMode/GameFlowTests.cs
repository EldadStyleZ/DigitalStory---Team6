using NUnit.Framework;

namespace SpacePartyGame.Tests
{
    public sealed class GameFlowTests
    {
        [Test]
        public void State_New_IsStartMenu()
        {
            Assert.That(new GameFlow().State, Is.EqualTo(GameState.StartMenu));
        }

        [TestCase(GameState.StartMenu, GameState.Join)]
        [TestCase(GameState.Join, GameState.Countdown)]
        [TestCase(GameState.Join, GameState.StartMenu)]
        [TestCase(GameState.Countdown, GameState.Playing)]
        [TestCase(GameState.Playing, GameState.Paused)]
        [TestCase(GameState.Paused, GameState.Playing)]
        [TestCase(GameState.Paused, GameState.StartMenu)]
        [TestCase(GameState.Playing, GameState.RoundEnd)]
        [TestCase(GameState.RoundEnd, GameState.NameEntry)]
        [TestCase(GameState.RoundEnd, GameState.Leaderboard)]
        [TestCase(GameState.NameEntry, GameState.Leaderboard)]
        [TestCase(GameState.Leaderboard, GameState.StartMenu)]
        [TestCase(GameState.Leaderboard, GameState.Join)]
        public void CanMove_AllowedStep_IsTrue(GameState from, GameState to)
        {
            Assert.That(GameFlow.CanMove(from, to), Is.True);
        }

        [TestCase(GameState.StartMenu, GameState.Playing)]
        [TestCase(GameState.StartMenu, GameState.Countdown)]
        [TestCase(GameState.Join, GameState.Playing)]
        [TestCase(GameState.Countdown, GameState.Join)]
        [TestCase(GameState.Playing, GameState.Countdown)]
        [TestCase(GameState.Playing, GameState.Playing)]
        [TestCase(GameState.RoundEnd, GameState.Playing)]
        public void CanMove_SkippedOrBackwardStep_IsFalse(GameState from, GameState to)
        {
            Assert.That(GameFlow.CanMove(from, to), Is.False);
        }

        [Test]
        public void TryMoveTo_AllowedStep_ChangesStateAndRaisesChanged()
        {
            var flow = new GameFlow();
            GameState? seenFrom = null;
            GameState? seenTo = null;
            flow.Changed += (from, to) => { seenFrom = from; seenTo = to; };

            bool moved = flow.TryMoveTo(GameState.Join);

            Assert.That(moved, Is.True);
            Assert.That(flow.State, Is.EqualTo(GameState.Join));
            Assert.That(seenFrom, Is.EqualTo(GameState.StartMenu));
            Assert.That(seenTo, Is.EqualTo(GameState.Join));
        }

        [Test]
        public void TryMoveTo_RefusedStep_KeepsStateAndStaysQuiet()
        {
            var flow = new GameFlow();
            bool raised = false;
            flow.Changed += (from, to) => raised = true;

            bool moved = flow.TryMoveTo(GameState.Playing);

            Assert.That(moved, Is.False);
            Assert.That(flow.State, Is.EqualTo(GameState.StartMenu));
            Assert.That(raised, Is.False);
        }

        [TestCase(0, false, false)]
        [TestCase(1, false, false)]
        [TestCase(2, false, true)]
        [TestCase(4, false, true)]
        [TestCase(0, true, false)]
        [TestCase(1, true, true)]
        [TestCase(5, true, false)]
        public void CanStartMatch_ByPlayerCount(int joined, bool allowSinglePlayer, bool expected)
        {
            Assert.That(GameFlow.CanStartMatch(joined, allowSinglePlayer), Is.EqualTo(expected));
        }
    }

    public sealed class CountdownTimerTests
    {
        [Test]
        public void Shown_AtStart_IsTheFullCount()
        {
            Assert.That(new CountdownTimer(3).Shown, Is.EqualTo(3));
        }

        [TestCase(0.5f, 3)]
        [TestCase(1.0f, 2)]
        [TestCase(1.5f, 2)]
        [TestCase(2.2f, 1)]
        public void Shown_AfterTime_CountsDown(float elapsed, int expected)
        {
            var timer = new CountdownTimer(3);

            timer.Tick(elapsed);

            Assert.That(timer.Shown, Is.EqualTo(expected));
        }

        [Test]
        public void Tick_ReachingZero_ReportsFinishedOnce()
        {
            var timer = new CountdownTimer(3);

            bool beforeEnd = timer.Tick(2.9f);
            bool atEnd = timer.Tick(0.2f);
            bool afterEnd = timer.Tick(1f);

            Assert.That(beforeEnd, Is.False);
            Assert.That(atEnd, Is.True);
            Assert.That(afterEnd, Is.False);
            Assert.That(timer.IsFinished, Is.True);
            Assert.That(timer.Shown, Is.EqualTo(0));
        }
    }

    public sealed class PlayerInputModesTests
    {
        [TestCase(GameState.StartMenu, null)]
        [TestCase(GameState.Join, PlayerInputModes.JoinMap)]
        [TestCase(GameState.Countdown, null)]
        [TestCase(GameState.Playing, PlayerInputModes.PlayerMap)]
        [TestCase(GameState.Paused, PlayerInputModes.UIMap)]
        [TestCase(GameState.RoundEnd, null)]
        [TestCase(GameState.NameEntry, PlayerInputModes.UIMap)]
        [TestCase(GameState.Leaderboard, PlayerInputModes.UIMap)]
        public void MapFor_EachState(GameState state, string expected)
        {
            Assert.That(PlayerInputModes.MapFor(state), Is.EqualTo(expected));
        }

        [Test]
        public void CanMoveAndPlay_OnlyWhilePlaying()
        {
            foreach (GameState state in System.Enum.GetValues(typeof(GameState)))
            {
                Assert.That(PlayerInputModes.CanMoveAndPlay(state), Is.EqualTo(state == GameState.Playing), state.ToString());
            }
        }
    }
}
