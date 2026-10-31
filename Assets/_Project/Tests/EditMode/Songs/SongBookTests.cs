using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class SongBookTests
    {
        private const int HoleCount = 6;
        private const float NoteWindow = 3f;
        private const float Cooldown = 10f;

        private static readonly SongDefinition Lullaby = new SongDefinition("Lullaby", string.Empty, Cooldown, 2, 1, 0);
        private static readonly SongDefinition Dance = new SongDefinition("Dance", string.Empty, Cooldown, 0, 2, 4);
        private static readonly SongDefinition Choir = new SongDefinition("Choir", string.Empty, Cooldown, 0, 1, 2, 3);
        private static readonly SongDefinition Ending = new SongDefinition("Ending", string.Empty, Cooldown, 1, 2, 3);

        private static SongConfig Config => ScriptableObject.CreateInstance<SongConfig>();

        [Test]
        public void Press_KeysInOrder_CastsTheSong()
        {
            SongBook book = BookOf(Dance, Lullaby);

            int cast = PressAll(book, 2, 1, 0);

            Assert.That(cast, Is.EqualTo(1));
        }

        [Test]
        public void Press_KeysInTheWrongOrder_CastsNothing()
        {
            SongBook book = BookOf(Lullaby);

            int cast = PressAll(book, 0, 1, 2);

            Assert.That(cast, Is.EqualTo(SongBook.NoSong));
        }

        [Test]
        public void Press_PartOfACombo_CastsNothingYet()
        {
            SongBook book = BookOf(Lullaby);

            Assert.That(PressAll(book, 2, 1), Is.EqualTo(SongBook.NoSong));
        }

        [Test]
        public void Press_ExtraNotesBeforeTheCombo_StillCasts()
        {
            SongBook book = BookOf(Lullaby);

            Assert.That(PressAll(book, 5, 4, 2, 1, 0), Is.EqualTo(0));
        }

        [Test]
        public void Press_TwoSongsMatch_LongestWins()
        {
            SongBook book = BookOf(Ending, Choir);

            Assert.That(PressAll(book, 0, 1, 2, 3), Is.EqualTo(1));
        }

        [Test]
        public void Press_NoteOlderThanTheWindow_IsForgotten()
        {
            SongBook book = BookOf(Lullaby);
            book.Press(2);
            book.Tick(NoteWindow + 0.1f);

            Assert.That(PressAll(book, 1, 0), Is.EqualTo(SongBook.NoSong));
        }

        [Test]
        public void Press_NoteJustInsideTheWindow_StillCounts()
        {
            SongBook book = BookOf(Lullaby);
            book.Press(2);
            book.Tick(NoteWindow - 0.1f);

            Assert.That(PressAll(book, 1, 0), Is.EqualTo(0));
        }

        [Test]
        public void Press_NoGameTimePassed_NotesNeverExpire()
        {
            SongBook book = BookOf(Lullaby);
            book.Press(2);
            book.Press(1);

            Assert.That(book.Press(0), Is.EqualTo(0));
        }

        [Test]
        public void Press_AfterACast_NotesAreCleared()
        {
            var overlapping = new SongDefinition("Overlap", string.Empty, Cooldown, 0, 1, 2);
            SongBook book = BookOf(Lullaby, overlapping);
            PressAll(book, 2, 1, 0);

            Assert.That(PressAll(book, 1, 2), Is.EqualTo(SongBook.NoSong));
        }

        [Test]
        public void Press_SongCoolingDown_IsNotCast()
        {
            SongBook book = BookOf(Lullaby);
            PressAll(book, 2, 1, 0);

            Assert.That(book.IsCoolingDown(0), Is.True);
            Assert.That(PressAll(book, 2, 1, 0), Is.EqualTo(SongBook.NoSong));
        }

        [Test]
        public void Press_LongestSongCoolingDown_FallsBackToTheShorterMatch()
        {
            SongBook book = BookOf(Ending, Choir);
            PressAll(book, 0, 1, 2, 3);

            Assert.That(PressAll(book, 0, 1, 2, 3), Is.EqualTo(0));
        }

        [Test]
        public void Tick_CooldownRunsOut_SongCanBeCastAgain()
        {
            SongBook book = BookOf(Lullaby);
            PressAll(book, 2, 1, 0);

            book.Tick(Cooldown + 0.1f);

            Assert.That(book.IsCoolingDown(0), Is.False);
            Assert.That(PressAll(book, 2, 1, 0), Is.EqualTo(0));
        }

        [Test]
        public void Press_MoreNotesThanRemembered_KeepsTheNewest()
        {
            SongBook book = BookOf(Lullaby);
            PressAll(book, 5, 5, 5, 5, 5, 5, 5, 5);

            Assert.That(PressAll(book, 2, 1, 0), Is.EqualTo(0));
        }

        [Test]
        public void Constructor_SongWithNoKeys_FailsWithAClearError()
        {
            var empty = new SongDefinition("Silent", string.Empty, Cooldown);

            Assert.That(() => BookOf(empty), Throws.ArgumentException.With.Message.Contains("Silent"));
        }

        [Test]
        public void Constructor_ComboLongerThanTheNoteMemory_FailsWithAClearError()
        {
            var tooLong = new SongDefinition("Epic", string.Empty, Cooldown, 0, 1, 2, 3, 4, 5);

            Assert.That(() => BookOf(tooLong), Throws.ArgumentException.With.Message.Contains("Epic"));
        }

        [Test]
        public void Constructor_KeyThatIsNotAHole_FailsWithAClearError()
        {
            var badKey = new SongDefinition("Wrong", string.Empty, Cooldown, 0, 1, HoleCount);

            Assert.That(() => BookOf(badKey), Throws.ArgumentException.With.Message.Contains("Wrong"));
        }

        private static SongBook BookOf(params SongDefinition[] songs) => new SongBook(songs, Config, HoleCount);

        private static int PressAll(SongBook book, params int[] holes)
        {
            int cast = SongBook.NoSong;

            foreach (int hole in holes)
            {
                cast = book.Press(hole);
            }

            return cast;
        }
    }
}
