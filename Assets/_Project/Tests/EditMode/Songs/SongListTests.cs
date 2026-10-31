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

        [Test]
        public void Faint_MatchesThePrototype() => AssertSong<FaintEffect>("Faint", 10f, 5, 3, 1);

        [Test]
        public void Balloons_MatchesThePrototype() => AssertSong<BalloonsEffect>("Balloons", 10f, 1, 3, 5);

        [Test]
        public void Disco_MatchesThePrototype() => AssertSong<DiscoEffect>("Disco", 10f, 5, 0, 5);

        [Test]
        public void Conga_MatchesThePrototype() => AssertSong<CongaEffect>("Conga", 10f, 0, 1, 0, 1);

        [Test]
        public void Heart_MatchesThePrototype() => AssertSong<HeartEffect>("Heart", 10f, 3, 0, 3);

        [Test]
        public void CrowdSurf_MatchesThePrototype() => AssertSong<CrowdSurfEffect>("Crowd Surf", 10f, 2, 2, 5);

        [Test]
        public void Choir_MatchesThePrototype() => AssertSong<ChoirEffect>("Choir", 10f, 0, 1, 2, 3);

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
