using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;

namespace DigitalStory.Tests.EditMode
{
    public sealed class FluteTests
    {
        private static FluteConfig Config => TestRules.Default.Flute;

        [TestCase(0, FluteMode.Call)]
        [TestCase(1, FluteMode.Call)]
        [TestCase(2, FluteMode.March)]
        [TestCase(3, FluteMode.March)]
        [TestCase(4, FluteMode.Scare)]
        [TestCase(5, FluteMode.Scare)]
        public void Sound_EachHole_SelectsItsMode(int hole, FluteMode expected)
        {
            var flute = new Flute(Config);

            flute.Sound(hole);

            Assert.That(flute.Mode, Is.EqualTo(expected));
            Assert.That(flute.CurrentHole, Is.EqualTo(hole));
        }

        [TestCase(0, 240f)]
        [TestCase(2, 190f)]
        [TestCase(4, 200f)]
        public void Sound_EachMode_UsesItsMusicRadius(int hole, float expected)
        {
            var flute = new Flute(Config);

            flute.Sound(hole);

            Assert.That(flute.MusicRadius, Is.EqualTo(expected));
        }

        [Test]
        public void Sound_NoHole_IsSilent()
        {
            var flute = new Flute(Config);
            flute.Sound(0);

            flute.Sound(Flute.NoHole);

            Assert.That(flute.Mode, Is.EqualTo(FluteMode.None));
            Assert.That(flute.CurrentHole, Is.EqualTo(Flute.NoHole));
        }

        [Test]
        public void ChooseHole_NothingHeld_ReturnsNoHole()
        {
            var flute = new Flute(Config);

            Assert.That(flute.ChooseHole(0), Is.EqualTo(Flute.NoHole));
        }

        [Test]
        public void ChooseHole_LastPressedStillHeld_Wins()
        {
            var flute = new Flute(Config);
            flute.Press(1);

            int chosen = flute.ChooseHole(HolesHeld(1, 4));

            Assert.That(chosen, Is.EqualTo(1));
        }

        [Test]
        public void ChooseHole_LastPressedReleased_TakesHighestHeld()
        {
            var flute = new Flute(Config);
            flute.Press(5);

            int chosen = flute.ChooseHole(HolesHeld(1, 4));

            Assert.That(chosen, Is.EqualTo(4));
        }

        private static int HolesHeld(int first, int second) => (1 << first) | (1 << second);
    }
}
