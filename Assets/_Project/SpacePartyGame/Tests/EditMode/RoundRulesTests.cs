using NUnit.Framework;

namespace SpacePartyGame.Tests
{
    public sealed class RoundTimerTests
    {
        [Test]
        public void Shown_New_IsTheStartSeconds()
        {
            Assert.That(new RoundTimer(50f).Shown, Is.EqualTo(50));
        }

        [Test]
        public void Shown_PartWayThroughASecond_RoundsUp()
        {
            var timer = new RoundTimer(50f);

            timer.Tick(0.4f);

            Assert.That(timer.Shown, Is.EqualTo(50));
        }

        [Test]
        public void Tick_ReachingZero_ReturnsTrueOnce()
        {
            var timer = new RoundTimer(2f);

            Assert.That(timer.Tick(1.5f), Is.False);
            Assert.That(timer.Tick(1f), Is.True);
            Assert.That(timer.Tick(1f), Is.False);
            Assert.That(timer.Shown, Is.EqualTo(0));
            Assert.That(timer.IsFinished, Is.True);
        }
    }

    public sealed class RoundClockTests
    {
        [TestCase(6, false)]
        [TestCase(5, true)]
        [TestCase(1, true)]
        [TestCase(0, true)]
        public void IsWarning_FromFiveSeconds(int shown, bool expected)
        {
            Assert.That(RoundClock.IsWarning(shown, 5), Is.EqualTo(expected));
        }

        [TestCase(11, false)]
        [TestCase(10, true)]
        [TestCase(1, true)]
        [TestCase(0, false)]
        public void Ticks_FromTenDownToOne(int shown, bool expected)
        {
            Assert.That(RoundClock.Ticks(shown, 10), Is.EqualTo(expected));
        }
    }

    public sealed class RoundResultTests
    {
        [Test]
        public void WinnerOf_HighestScore_Wins()
        {
            Assert.That(RoundResult.WinnerOf(new[] { 10, 25, 0, 0 }, 2), Is.EqualTo(1));
        }

        [Test]
        public void WinnerOf_Tie_IsADraw()
        {
            Assert.That(RoundResult.WinnerOf(new[] { 15, 15, 0, 0 }, 2), Is.EqualTo(RoundResult.Draw));
        }

        [Test]
        public void WinnerOf_OnlyCountsPlayersInTheMatch()
        {
            Assert.That(RoundResult.WinnerOf(new[] { 5, 0, 40, 0 }, 2), Is.EqualTo(0));
        }

        [Test]
        public void WinnerOf_NoPlayers_IsADraw()
        {
            Assert.That(RoundResult.WinnerOf(new int[0], 0), Is.EqualTo(RoundResult.Draw));
        }
    }

    public sealed class PlayerPermissionsTests
    {
        [Test]
        public void For_Playing_AllowsEverything()
        {
            PlayerPermissions p = PlayerPermissions.For(GameState.Playing, false);

            Assert.That(p.CanMove && p.CanPlay && p.CanCall && p.CanRun && p.CanBlow, Is.True);
        }

        [Test]
        public void For_RoundEndWinner_OnlyPlaysNotes()
        {
            PlayerPermissions p = PlayerPermissions.For(GameState.RoundEnd, true);

            Assert.That(p.CanPlay, Is.True);
            Assert.That(p.CanBlow, Is.False);
            Assert.That(p.CanMove, Is.False);
            Assert.That(p.CanCall, Is.False);
            Assert.That(p.CanRun, Is.False);
        }

        [Test]
        public void For_RoundEndLoser_DoesNothing()
        {
            Assert.That(PlayerPermissions.For(GameState.RoundEnd, false), Is.EqualTo(PlayerPermissions.None));
        }

        [TestCase(GameState.StartMenu)]
        [TestCase(GameState.Countdown)]
        [TestCase(GameState.Paused)]
        public void For_OtherStages_DoesNothing(GameState state)
        {
            Assert.That(PlayerPermissions.For(state, true), Is.EqualTo(PlayerPermissions.None));
        }
    }

    public sealed class GameResetTests
    {
        [TestCase(GameState.Countdown, true)]
        [TestCase(GameState.Playing, true)]
        [TestCase(GameState.Paused, true)]
        [TestCase(GameState.RoundEnd, true)]
        [TestCase(GameState.StartMenu, false)]
        [TestCase(GameState.Join, false)]
        public void CanReset_OnlyDuringAGame(GameState state, bool expected)
        {
            Assert.That(GameFlow.CanReset(state), Is.EqualTo(expected));
        }

        [TestCase(GameState.Countdown)]
        [TestCase(GameState.Playing)]
        [TestCase(GameState.Paused)]
        [TestCase(GameState.RoundEnd)]
        public void CanReset_IsAlwaysAnAllowedMoveToTheStartMenu(GameState state)
        {
            Assert.That(GameFlow.CanMove(state, GameState.StartMenu), Is.True);
        }
    }

    public sealed class TickSynthTests
    {
        private const int Rate = 48000;

        [Test]
        public void Fill_BeforeAnyTick_IsSilent()
        {
            var synth = new TickSynth(Rate);
            var buffer = new float[1024];

            synth.Fill(buffer, 2);

            Assert.That(Peak(buffer), Is.EqualTo(0f));
        }

        [Test]
        public void Fill_AfterATick_SoundsThenDiesAway()
        {
            var synth = new TickSynth(Rate);
            var first = new float[1024];
            var late = new float[1024];
            synth.Tick(false);

            synth.Fill(first, 2);

            // Fill adds to the buffer, so each block starts from silence.
            for (int i = 0; i < 30; i++)
            {
                System.Array.Clear(late, 0, late.Length);
                synth.Fill(late, 2);
            }

            Assert.That(Peak(first), Is.GreaterThan(0.05f));
            Assert.That(Peak(late), Is.LessThan(0.001f));
        }

        [Test]
        public void Fill_NeverClips()
        {
            var synth = new TickSynth(Rate);
            var buffer = new float[4096];
            synth.Tick(true);

            synth.Fill(buffer, 2);

            Assert.That(Peak(buffer), Is.LessThanOrEqualTo(1f));
        }

        private static float Peak(float[] buffer)
        {
            float peak = 0f;

            foreach (float sample in buffer)
            {
                peak = System.Math.Max(peak, System.Math.Abs(sample));
            }

            return peak;
        }
    }
}
