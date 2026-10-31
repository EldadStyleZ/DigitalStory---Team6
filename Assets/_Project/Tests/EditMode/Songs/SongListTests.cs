using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>Checks every song's combo and cooldown against the prototype.</summary>
    public sealed class SongListTests
    {
        [Test]
        public void Lullaby_MatchesThePrototype() => AssertSong<LullabyEffect>("Lullaby", 12f, 2, 1, 0);

        [Test]
        public void Dance_MatchesThePrototype() => AssertSong<DanceEffect>("Dance", 12f, 0, 2, 4);

        private static void AssertSong<T>(string title, float cooldown, params int[] keys) where T : SongEffect
        {
            SongDefinition song = ScriptableObject.CreateInstance<T>().Song;

            Assert.That(song.Title, Is.EqualTo(title));
            Assert.That(song.CooldownSeconds, Is.EqualTo(cooldown));
            Assert.That(song.Keys, Is.EqualTo(keys));
            Assert.That(song.Description, Is.Not.Empty);
        }
    }
}
