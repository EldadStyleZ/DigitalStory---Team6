using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace PiperToy.Tests
{
    // The combo rule is plain C#, so these tests need no scene and run in a moment.
    public sealed class ComboReaderTests
    {
        private const float Window = 3f;

        private ComboReader _reader;

        [SetUp]
        public void CreateReader()
        {
            _reader = new ComboReader(Window);
        }

        // A song's keys are private and set in the Inspector, so the test sets them the same way the Inspector does.
        private static Song SongWithKeys(params int[] keys)
        {
            var song = ScriptableObject.CreateInstance<Song>();
            var serialized = new SerializedObject(song);
            SerializedProperty list = serialized.FindProperty("_keys");
            list.arraySize = keys.Length;

            for (int index = 0; index < keys.Length; index++)
            {
                list.GetArrayElementAtIndex(index).intValue = keys[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return song;
        }

        private void Play(float time, params int[] keys)
        {
            foreach (int key in keys)
            {
                _reader.Add(key, time);
            }
        }

        [Test]
        public void FindSong_ItsKeysInOrder_FindsIt()
        {
            Song dance = SongWithKeys(1, 3, 5);
            Play(0f, 1, 3, 5);

            Assert.That(_reader.FindSong(new[] { dance }), Is.SameAs(dance));
        }

        [Test]
        public void FindSong_ItsKeysInAnotherOrder_FindsNothing()
        {
            Song dance = SongWithKeys(1, 3, 5);
            Play(0f, 5, 3, 1);

            Assert.That(_reader.FindSong(new[] { dance }), Is.Null);
        }

        [Test]
        public void FindSong_AfterOtherNotes_StillFindsIt()
        {
            Song dance = SongWithKeys(1, 3, 5);
            Play(0f, 6, 6, 1, 3, 5);

            Assert.That(_reader.FindSong(new[] { dance }), Is.SameAs(dance));
        }

        [Test]
        public void FindSong_OnlyPartOfIt_FindsNothing()
        {
            Song conga = SongWithKeys(2, 1, 1, 2);
            Play(0f, 1, 1, 2);

            Assert.That(_reader.FindSong(new[] { conga }), Is.Null);
        }

        [Test]
        public void FindSong_NotesSlowerThanTheWindow_FindsNothing()
        {
            Song dance = SongWithKeys(1, 3, 5);
            _reader.Add(1, 0f);
            _reader.Add(3, 2f);
            _reader.Add(5, 3.5f);

            Assert.That(_reader.FindSong(new[] { dance }), Is.Null);
        }

        [Test]
        public void FindSong_NotesJustInsideTheWindow_FindsIt()
        {
            Song dance = SongWithKeys(1, 3, 5);
            _reader.Add(1, 0f);
            _reader.Add(3, 1.5f);
            _reader.Add(5, 2.9f);

            Assert.That(_reader.FindSong(new[] { dance }), Is.SameAs(dance));
        }

        // Every key of the long song is also a short song's ending, so only the longer one may win.
        [Test]
        public void FindSong_TwoSongsEndTheSame_TheLongerOneWins()
        {
            Song shortSong = SongWithKeys(1, 4);
            Song longSong = SongWithKeys(4, 1, 4);
            Play(0f, 4, 1, 4);

            Assert.That(_reader.FindSong(new[] { shortSong, longSong }), Is.SameAs(longSong));
        }

        [Test]
        public void Clear_ForgetsEveryNote()
        {
            Play(0f, 1, 3);

            _reader.Clear();

            Assert.That(_reader.Keys, Is.Empty);
        }

        [Test]
        public void Keys_AreTheNotesStillRemembered()
        {
            _reader.Add(6, 0f);
            _reader.Add(1, 5f);
            _reader.Add(3, 5.5f);

            Assert.That(_reader.Keys, Is.EqualTo(new[] { 1, 3 }));
        }
    }
}
