using DigitalStory.Logic;
using NUnit.Framework;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Plan section S, which seed a match's map is drawn from: a fixed seed (the tests') wins; otherwise "-f010Seed n" on the
    /// command line draws n for the first match and n + 1, n + 2 for each restart, so a demo can be played again; otherwise
    /// each match takes a fresh seed.
    /// </summary>
    public sealed class MatchSeedRuleTests
    {
        private const string Option = "-f010Seed";

        [Test]
        public void Choose_AFixedSeed_WinsOverTheCommandLineAndTheFreshSeed()
        {
            Assert.That(MatchSeedRule.Choose(1010, 42, 3, 999), Is.EqualTo(1010));
        }

        [TestCase(0, 42)]
        [TestCase(1, 43)]
        [TestCase(5, 47)]
        public void Choose_ACommandLineSeed_DrawsItForTheFirstMatchAndTheNextForEachRestart(int match, int expected)
        {
            Assert.That(MatchSeedRule.Choose(null, 42, match, 999), Is.EqualTo(expected));
        }

        [Test]
        public void Choose_NoSeedGiven_TakesTheFreshSeed()
        {
            Assert.That(MatchSeedRule.Choose(null, null, 2, 999), Is.EqualTo(999));
        }

        [Test]
        public void TryRead_TheOptionFollowedByANumber_ReadsIt()
        {
            string[] arguments = { "WaspPiper", "-batchmode", Option, "42" };

            Assert.That(MatchSeedRule.TryRead(arguments, Option, out int seed), Is.True);
            Assert.That(seed, Is.EqualTo(42));
        }

        [Test]
        public void TryRead_ANegativeNumber_ReadsIt()
        {
            Assert.That(MatchSeedRule.TryRead(new[] { Option, "-7" }, Option, out int seed), Is.True);
            Assert.That(seed, Is.EqualTo(-7));
        }

        // The command lines, split at their spaces.
        [TestCase("WaspPiper")]
        [TestCase("WaspPiper " + Option)]
        [TestCase("WaspPiper " + Option + " dawn")]
        [TestCase("WaspPiper -f010SeedX 4")]
        public void TryRead_NoOptionOrNoNumberAfterIt_ReadsNothing(string commandLine)
        {
            Assert.That(MatchSeedRule.TryRead(commandLine.Split(' '), Option, out _), Is.False);
        }

        [Test]
        public void TryRead_NoArguments_ReadsNothing()
        {
            Assert.That(MatchSeedRule.TryRead(null, Option, out _), Is.False);
        }
    }
}
