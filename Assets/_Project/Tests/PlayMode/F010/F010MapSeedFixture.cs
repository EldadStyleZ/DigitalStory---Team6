using DigitalStory.View;
using NUnit.Framework;

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>
    /// Plan section S: the game draws a new map each match, and the tests draw the same one every time. Before any PlayMode
    /// test of this assembly runs, every match is fixed to <see cref="Seed"/>; afterwards the game's own choice comes back.
    /// A test that needs another map sets <see cref="MatchSeed.Fixed"/> itself and puts <see cref="Seed"/> back.
    /// </summary>
    [SetUpFixture]
    public sealed class F010MapSeedFixture
    {
        /// <summary>The seed of every map the PlayMode tests play on.</summary>
        public const int Seed = 1010;

        [OneTimeSetUp]
        public void FixTheMap()
        {
            MatchSeed.Fixed = Seed;
        }

        [OneTimeTearDown]
        public void FreeTheMap()
        {
            MatchSeed.Fixed = null;
        }
    }
}
