using DigitalStory.Logic;
using NUnit.Framework;

namespace DigitalStory.Tests.EditMode
{
    public sealed class GameRulesTests
    {
        [Test]
        public void Constructor_MissingSong_FailsWithAClearError()
        {
            var songs = new SongEffect[] { null };

            Assert.That(() => TestRules.With(songs: songs), Throws.ArgumentException.With.Message.Contains("song"));
        }
    }
}
