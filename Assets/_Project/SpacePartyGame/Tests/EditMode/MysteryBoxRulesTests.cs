using NUnit.Framework;
using UnityEngine;

namespace SpacePartyGame.Tests
{
    public sealed class MysteryBoxLifeTests
    {
        // Idle for 6 seconds, then flashes for 2 (4 flashes a second) and is gone.
        private static MysteryBoxLife Make() => new MysteryBoxLife(6f, 2f, 4f);

        [Test]
        public void New_IsShownAndNotFlashing()
        {
            MysteryBoxLife life = Make();

            Assert.That(life.IsVisible, Is.True);
            Assert.That(life.IsFlashing, Is.False);
            Assert.That(life.IsOver, Is.False);
        }

        [Test]
        public void BeforeTheIdleTime_ItDoesNotFlash()
        {
            MysteryBoxLife life = Make();

            life.Tick(5.9f);

            Assert.That(life.IsFlashing, Is.False);
            Assert.That(life.IsVisible, Is.True);
        }

        [Test]
        public void AfterTheIdleTime_ItFlashesOnAndOff()
        {
            MysteryBoxLife life = Make();

            life.Tick(6.05f);
            Assert.That(life.IsFlashing, Is.True);
            Assert.That(life.IsVisible, Is.True);

            life.Tick(0.15f);
            Assert.That(life.IsVisible, Is.False);
        }

        [Test]
        public void AfterTheFlashing_ItIsOver()
        {
            MysteryBoxLife life = Make();

            life.Tick(7.9f);
            Assert.That(life.IsOver, Is.False);

            life.Tick(0.1f);
            Assert.That(life.IsOver, Is.True);
        }

        [Test]
        public void Restart_StartsTheLifeAgain()
        {
            MysteryBoxLife life = Make();
            life.Tick(7f);

            life.Restart();

            Assert.That(life.IsFlashing, Is.False);
            Assert.That(life.IsOver, Is.False);
        }
    }

    public sealed class SpawnWaitTests
    {
        [Test]
        public void TheLowestRoll_GivesTheMinimum()
        {
            Assert.That(SpawnWait.Next(2f, 5f, 0f), Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void ItIsNeverShorterThanTheMinimum()
        {
            for (int i = 0; i <= 100; i++)
            {
                Assert.That(SpawnWait.Next(2f, 5f, i / 100f), Is.GreaterThanOrEqualTo(2f));
            }
        }

        [Test]
        public void TheHighestRoll_IsLongButFinite()
        {
            float wait = SpawnWait.Next(2f, 5f, 1f);

            Assert.That(float.IsInfinity(wait), Is.False);
            Assert.That(wait, Is.GreaterThan(20f));
        }

        [Test]
        public void OnAverage_ItIsTheAverage()
        {
            const int rolls = 100000;
            double sum = 0;

            for (int i = 0; i < rolls; i++)
            {
                sum += SpawnWait.Next(2f, 5f, (i + 0.5f) / rolls);
            }

            Assert.That(sum / rolls, Is.EqualTo(5.0).Within(0.05));
        }

        [Test]
        public void AnAverageBelowTheMinimum_AlwaysWaitsTheMinimum()
        {
            Assert.That(SpawnWait.Next(2f, 1f, 0.7f), Is.EqualTo(2f));
        }
    }

    public sealed class BoxSpinTests
    {
        [TestCase(0f, false)]
        [TestCase(80f, false)]
        [TestCase(100f, true)]
        [TestCase(180f, true)]
        [TestCase(260f, true)]
        [TestCase(280f, false)]
        public void ShowsBack_OnlyWhileTurnedAway(float degrees, bool expected)
        {
            Assert.That(BoxSpin.ShowsBack(degrees), Is.EqualTo(expected));
        }

        [Test]
        public void Bob_GoesUpAndDownByTheHeight()
        {
            Assert.That(BoxSpin.Bob(0f, 0.1f, 2f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(BoxSpin.Bob(0.5f, 0.1f, 2f), Is.EqualTo(0.1f).Within(1e-5f));
            Assert.That(BoxSpin.Bob(1.5f, 0.1f, 2f), Is.EqualTo(-0.1f).Within(1e-5f));
        }
    }
}
