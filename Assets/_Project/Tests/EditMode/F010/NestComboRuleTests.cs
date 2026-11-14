using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Plan section U: a nest's combo is 4 or 5 notes from the four holes, never the same note twice in a row, drawn from the
    /// match's seed, and no song lies inside it or holds it, so playing a combo never casts a song.
    /// </summary>
    public sealed class NestComboRuleTests
    {
        private const int HoleCount = 4;
        private const int Shortest = 4;
        private const int Longest = 5;
        private const int Seeds = 200;
        private const int NestsPerMatch = 8;

        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in _created)
            {
                Object.DestroyImmediate(asset);
            }

            _created.Clear();
        }

        [Test]
        public void Draw_TwoHundredSeeds_FourOrFiveNotesFromTheFourHolesNeverTheSameTwiceInARow()
        {
            IReadOnlyList<IReadOnlyList<int>> songs = NestComboRule.KeysOf(AllSongs());
            var lengths = new HashSet<int>();
            for (uint seed = 1; seed <= Seeds; seed++)
            {
                var random = new Mulberry32Random(seed);
                for (int nest = 0; nest < NestsPerMatch; nest++)
                {
                    int[] combo = NestComboRule.Draw(random, songs, HoleCount, Shortest, Longest);
                    lengths.Add(combo.Length);
                    Assert.That(combo.Length, Is.InRange(Shortest, Longest), "seed " + seed);
                    for (int index = 0; index < combo.Length; index++)
                    {
                        Assert.That(combo[index], Is.InRange(0, HoleCount - 1), "seed " + seed);
                        if (index > 0)
                        {
                            Assert.That(combo[index], Is.Not.EqualTo(combo[index - 1]), "seed " + seed + ": a note follows itself");
                        }
                    }
                }
            }

            Assert.That(lengths, Is.EquivalentTo(new[] { 4, 5 }), "both lengths are drawn");
        }

        [Test]
        public void Draw_TheSameSeed_DrawsTheSameCombos()
        {
            IReadOnlyList<IReadOnlyList<int>> songs = NestComboRule.KeysOf(AllSongs());
            var first = new Mulberry32Random(1010);
            var second = new Mulberry32Random(1010);
            var other = new Mulberry32Random(2026);
            bool anyDiffers = false;
            for (int nest = 0; nest < NestsPerMatch; nest++)
            {
                int[] combo = NestComboRule.Draw(first, songs, HoleCount, Shortest, Longest);
                Assert.That(NestComboRule.Draw(second, songs, HoleCount, Shortest, Longest), Is.EqualTo(combo), "nest " + nest);
                anyDiffers |= !AreSame(NestComboRule.Draw(other, songs, HoleCount, Shortest, Longest), combo);
            }

            Assert.That(anyDiffers, Is.True, "another seed draws other combos");
        }

        // Played through the song book, which matches songs as the flute does: the last notes heard against each song's keys.
        [Test]
        public void Draw_TwoHundredSeeds_NoComboCastsASong()
        {
            SwarmSongEffect[] effects = AllSongs();
            IReadOnlyList<IReadOnlyList<int>> songs = NestComboRule.KeysOf(effects);
            var definitions = new List<SongDefinition>();
            foreach (SwarmSongEffect effect in effects)
            {
                definitions.Add(new SongDefinition(effect.Song.Title, effect.Song.Description, 0f, Copy(effect.Song.Keys)));
            }

            var config = Make<SongConfig>();
            for (uint seed = 1; seed <= Seeds; seed++)
            {
                var random = new Mulberry32Random(seed);
                for (int nest = 0; nest < NestsPerMatch; nest++)
                {
                    int[] combo = NestComboRule.Draw(random, songs, HoleCount, Shortest, Longest);
                    Assert.That(NestComboRule.CastsNoSong(combo, songs), Is.True, "seed " + seed);
                    var book = new SongBook(definitions, config, HoleCount);
                    for (int index = 0; index < combo.Length; index++)
                    {
                        Assert.That(book.Press(combo[index]), Is.EqualTo(SongBook.NoSong), "seed " + seed + " nest " + nest + " cast a song");
                    }
                }
            }
        }

        [Test]
        public void CastsNoSong_ASongInsideTheComboOrTheComboInsideASong_IsFalse()
        {
            var songs = new IReadOnlyList<int>[] { new[] { 1, 2, 0 }, new[] { 3, 1, 2, 1, 3 } };

            Assert.That(NestComboRule.CastsNoSong(new[] { 3, 1, 2, 0 }, songs), Is.False, "J K H lies inside it");
            Assert.That(NestComboRule.CastsNoSong(new[] { 1, 2, 1, 3 }, songs), Is.False, "the five-note song holds it");
            Assert.That(NestComboRule.CastsNoSong(new[] { 1, 2, 3, 0 }, songs), Is.True);
        }

        [Test]
        public void Draw_ARandomSourceStuckOnOneNumber_StillFindsACombo()
        {
            IReadOnlyList<IReadOnlyList<int>> songs = NestComboRule.KeysOf(AllSongs());

            foreach (double stuck in new[] { 0d, 0.5d, 0.999d })
            {
                int[] combo = NestComboRule.Draw(new FixedRandomSource(stuck), songs, HoleCount, Shortest, Longest);
                Assert.That(NestComboRule.CastsNoSong(combo, songs), Is.True, "stuck on " + stuck);
            }
        }

        [Test]
        public void Draw_EveryComboHoldsASong_Throws()
        {
            var songs = new IReadOnlyList<int>[] { new[] { 0, 1 }, new[] { 1, 0 } };

            Assert.That(() => NestComboRule.Draw(new FixedRandomSource(), songs, 2, 4, 5), Throws.InvalidOperationException);
        }

        private SwarmSongEffect[] AllSongs()
        {
            return new SwarmSongEffect[]
            {
                Make<StingSong>(), Make<TornadoSong>(), Make<MeteorSong>(), Make<HydraSong>(), Make<ScytheSong>(), Make<LanceSong>(),
                Make<NestSong>(), Make<DomeSong>(), Make<GustSong>(), Make<HoneycombSong>(), Make<HiveCallSong>(), Make<ColossusSong>(),
            };
        }

        private T Make<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _created.Add(asset);
            return asset;
        }

        private static int[] Copy(IReadOnlyList<int> keys)
        {
            var copy = new int[keys.Count];
            for (int index = 0; index < copy.Length; index++)
            {
                copy[index] = keys[index];
            }

            return copy;
        }

        private static bool AreSame(int[] first, int[] second)
        {
            if (first.Length != second.Length)
            {
                return false;
            }

            for (int index = 0; index < first.Length; index++)
            {
                if (first[index] != second[index])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
