using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DigitalStory.Tests.EditMode
{
    public sealed class SwarmSongCasterTests
    {
        private const int HoleCount = 6;

        private static readonly int[][] SongKeys =
        {
            new[] { 0, 1, 0 },
            new[] { 2, 3, 4 },
            new[] { 5, 4, 3, 5 },
            new[] { 1, 1, 2 },
            new[] { 0, 5, 0 },
            new[] { 3, 4, 5, 4, 3 },
        };

        private static readonly SwarmForm[] SongForms =
        {
            SwarmForm.Dome, SwarmForm.Sting, SwarmForm.Tornado, SwarmForm.Gust, SwarmForm.HiveCall, SwarmForm.Colossus,
        };

        private SwarmConfig _swarmConfig;
        private FuryConfig _furyConfig;
        private HiveConfig _hiveConfig;
        private SongConfig _songConfig;
        private SwarmBody _body;
        private FuryMeter _fury;
        private SwarmFormMachine _forms;
        private HiveSet _hives;
        private SwarmSongCaster _caster;
        private FakeSwarmTarget _aim;
        private readonly List<ISwarmTarget> _enemies = new List<ISwarmTarget>();
        private readonly List<SwarmSongEffect> _songs = new List<SwarmSongEffect>();
        private readonly List<int> _cast = new List<int>();
        private readonly List<int> _played = new List<int>();

        [SetUp]
        public void SetUp()
        {
            _swarmConfig = ScriptableObject.CreateInstance<SwarmConfig>();
            _furyConfig = ScriptableObject.CreateInstance<FuryConfig>();
            _hiveConfig = ScriptableObject.CreateInstance<HiveConfig>();
            _songConfig = ScriptableObject.CreateInstance<SongConfig>();
            _body = new SwarmBody(_swarmConfig);
            _fury = new FuryMeter(_furyConfig);
            _forms = new SwarmFormMachine(_body, _fury);
            _hives = new HiveSet(new List<Hive> { new Hive(_hiveConfig, new Vector3(-10f, 0f, 0f)), new Hive(_hiveConfig, new Vector3(10f, 0f, 0f)) });
            _aim = new FakeSwarmTarget();
            _songs.Clear();
            _songs.Add(ScriptableObject.CreateInstance<DomeSong>());
            _songs.Add(ScriptableObject.CreateInstance<StingSong>());
            _songs.Add(ScriptableObject.CreateInstance<TornadoSong>());
            _songs.Add(ScriptableObject.CreateInstance<GustSong>());
            _songs.Add(ScriptableObject.CreateInstance<HiveCallSong>());
            _songs.Add(ScriptableObject.CreateInstance<ColossusSong>());
            _caster = new SwarmSongCaster(_songs, _songConfig, HoleCount);
            _cast.Clear();
            _played.Clear();
            _caster.SongCast += song => _cast.Add(song);
            _caster.NotePlayed += hole => _played.Add(hole);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_swarmConfig);
            Object.DestroyImmediate(_furyConfig);
            Object.DestroyImmediate(_hiveConfig);
            Object.DestroyImmediate(_songConfig);

            foreach (SwarmSongEffect song in _songs)
            {
                Object.DestroyImmediate(song);
            }
        }

        [Test]
        public void Count_SixSongs_AreExposedByIndex()
        {
            Assert.That(_caster.Count, Is.EqualTo(6));
            Assert.That(_caster[0], Is.SameAs(_songs[0]));
            Assert.That(_caster[5], Is.SameAs(_songs[5]));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void Press_EachSongsKeysFromSilence_CastExactlyThatSong(int song)
        {
            _fury.AddDamage(1000f);

            int result = PlayAll(SongKeys[song]);

            Assert.That(result, Is.EqualTo(song));
            Assert.That(_cast, Is.EqualTo(new[] { song }), "one cast, and it is this song");
            Assert.That(_forms.Current, Is.EqualTo(SongForms[song]));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void Press_EachSongsKeysBeforeTheLastNote_CastNothing(int song)
        {
            _fury.AddDamage(1000f);
            int[] keys = SongKeys[song];

            for (int note = 0; note < keys.Length - 1; note++)
            {
                Assert.That(Press(keys[note]), Is.EqualTo(SongBook.NoSong), "note " + note);
            }

            Assert.That(_cast, Is.Empty);
        }

        [Test]
        public void Press_ColossusKeysPlayedWhole_DoNotCastAnythingElseOnTheWay()
        {
            _fury.AddDamage(1000f);

            PlayAll(SongKeys[5]);

            Assert.That(_cast, Is.EqualTo(new[] { 5 }));
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void Press_WhileTheColossusStands_CastsNothingAndStartsNoCooldown(int song)
        {
            _fury.AddDamage(1000f);
            _forms.StartColossus(12f);

            int result = PlayAll(SongKeys[song]);

            Assert.That(result, Is.EqualTo(SongBook.NoSong));
            Assert.That(_cast, Is.Empty);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Colossus));
            Assert.That(_caster.CooldownFraction(song), Is.EqualTo(0f));
        }

        [Test]
        public void Press_SongOnCooldown_CannotBeRecast()
        {
            PlayAll(SongKeys[0]);
            _cast.Clear();

            int again = PlayAll(SongKeys[0]);

            Assert.That(again, Is.EqualTo(SongBook.NoSong));
            Assert.That(_cast, Is.Empty);
        }

        [Test]
        public void Press_CooldownJustBeforeItsEnd_StillBlocks()
        {
            PlayAll(SongKeys[0]);
            _caster.Tick(7.5f);

            Assert.That(PlayAll(SongKeys[0]), Is.EqualTo(SongBook.NoSong));
        }

        [Test]
        public void Press_CooldownOver_CastsAgain()
        {
            PlayAll(SongKeys[0]);
            _caster.Tick(8f);

            Assert.That(PlayAll(SongKeys[0]), Is.EqualTo(0));
        }

        [Test]
        public void CooldownFraction_AfterCasting_RunsFromOneToZero()
        {
            Assert.That(_caster.CooldownFraction(0), Is.EqualTo(0f));

            PlayAll(SongKeys[0]);
            Assert.That(_caster.CooldownFraction(0), Is.EqualTo(1f).Within(1e-4f));

            _caster.Tick(2f);
            Assert.That(_caster.CooldownFraction(0), Is.EqualTo(0.75f).Within(1e-4f));

            _caster.Tick(10f);
            Assert.That(_caster.CooldownFraction(0), Is.EqualTo(0f));
            Assert.That(_caster.CooldownFraction(1), Is.EqualTo(0f), "other songs are untouched");
        }

        [Test]
        public void CooldownFraction_ColossusHasNoCooldown_IsAlwaysZero()
        {
            _fury.AddDamage(1000f);
            PlayAll(SongKeys[5]);

            Assert.That(_caster.CooldownFraction(5), Is.EqualTo(0f));
        }

        // The Colossus needs full Fury

        [Test]
        public void Press_ColossusWithFuryNotFull_CastsNothing()
        {
            _fury.AddDamage(199.8f);

            int result = PlayAll(SongKeys[5]);

            Assert.That(result, Is.EqualTo(SongBook.NoSong));
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_fury.Value, Is.EqualTo(99.9f).Within(1e-3f), "nothing was used");
        }

        [Test]
        public void Press_ColossusWithFullFury_CastsAndEmptiesTheMeter()
        {
            _fury.AddDamage(200f);

            int result = PlayAll(SongKeys[5]);

            Assert.That(result, Is.EqualTo(5));
            Assert.That(_fury.Value, Is.EqualTo(0f));
        }

        [Test]
        public void Press_ColossusRightAfterUsingFury_CannotBeCastAgain()
        {
            _fury.AddDamage(200f);
            PlayAll(SongKeys[5]);
            _cast.Clear();

            int again = PlayAll(SongKeys[5]);

            Assert.That(again, Is.EqualTo(SongBook.NoSong));
            Assert.That(_cast, Is.Empty);
        }

        // CanCast and collapse

        [Test]
        public void Press_StingWithNothingAimedAt_CastsNothing()
        {
            _aim = null;

            int result = PlayAll(SongKeys[1]);

            Assert.That(result, Is.EqualTo(SongBook.NoSong));
            Assert.That(_cast, Is.Empty);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        [Test]
        public void Press_SongThatCannotBeCast_UsesUpTheNotes()
        {
            _aim = null;

            PlayAll(SongKeys[1]);

            Assert.That(_caster.RecentNoteCount, Is.EqualTo(0));
        }

        [Test]
        public void Press_WhileCollapsed_CastsNothingAndRemembersNothing()
        {
            _body.Lose(4500, false);

            int result = PlayAll(SongKeys[0]);

            Assert.That(result, Is.EqualTo(SongBook.NoSong));
            Assert.That(_cast, Is.Empty);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
            Assert.That(_caster.RecentNoteCount, Is.EqualTo(0));
            Assert.That(_played, Is.Empty);
        }

        [Test]
        public void Press_AfterTheRespawn_CastsAgain()
        {
            _body.Lose(4500, false);
            PlayAll(SongKeys[0]);
            _body.Tick(3f, 2);

            int result = PlayAll(SongKeys[0]);

            Assert.That(result, Is.EqualTo(0));
        }

        // Notes for the HUD

        [Test]
        public void Press_AnyNote_RaisesNotePlayedWithTheHole()
        {
            Press(4);
            Press(2);

            Assert.That(_played, Is.EqualTo(new[] { 4, 2 }));
        }

        [Test]
        public void RecentNotes_Played_AreRememberedOldestFirst()
        {
            Press(4);
            Press(2);

            Assert.That(_caster.RecentNoteCount, Is.EqualTo(2));
            Assert.That(_caster.RecentNoteHole(0), Is.EqualTo(4));
            Assert.That(_caster.RecentNoteHole(1), Is.EqualTo(2));
        }

        [Test]
        public void RecentNoteAge01_AsTimePasses_RunsFromZeroToOneOverTheNoteWindow()
        {
            Press(4);
            Assert.That(_caster.RecentNoteAge01(0), Is.EqualTo(0f));

            _caster.Tick(1.5f);
            Press(2);

            Assert.That(_caster.RecentNoteAge01(0), Is.EqualTo(0.5f).Within(1e-4f), "1.5 s of a 3 s window");
            Assert.That(_caster.RecentNoteAge01(1), Is.EqualTo(0f));
        }

        [Test]
        public void RecentNotes_OlderThanTheWindow_AreForgotten()
        {
            Press(4);
            _caster.Tick(3f);

            Press(2);

            Assert.That(_caster.RecentNoteCount, Is.EqualTo(1));
            Assert.That(_caster.RecentNoteHole(0), Is.EqualTo(2));
        }

        [Test]
        public void RecentNotes_JustInsideTheWindow_AreKept()
        {
            Press(4);
            _caster.Tick(2.5f);

            Press(2);

            Assert.That(_caster.RecentNoteCount, Is.EqualTo(2));
        }

        [Test]
        public void RecentNotes_MoreThanTheFluteRemembers_DropTheOldest()
        {
            int[] notes = { 4, 4, 2, 4, 2, 4 };

            foreach (int note in notes)
            {
                Press(note);
            }

            Assert.That(_caster.RecentNoteCount, Is.EqualTo(5));
            Assert.That(_caster.RecentNoteHole(0), Is.EqualTo(4));
            Assert.That(_caster.RecentNoteHole(1), Is.EqualTo(2));
            Assert.That(_caster.RecentNoteHole(4), Is.EqualTo(4));
        }

        [Test]
        public void RecentNotes_ACast_ClearsThem()
        {
            PlayAll(SongKeys[0]);

            Assert.That(_caster.RecentNoteCount, Is.EqualTo(0));
        }

        [Test]
        public void Press_ANoteThatCastsASong_StillRaisesNotePlayed()
        {
            PlayAll(SongKeys[0]);

            Assert.That(_played, Is.EqualTo(new[] { 0, 1, 0 }));
        }

        [Test]
        public void Tick_AndPressWithoutACast_AllocateNothing()
        {
            Press(4);
            SwarmSongContext context = Context();

            Assert.That(() => { _caster.Tick(0.016f); }, Is.Not.AllocatingGCMemory());
            Assert.That(() => { _caster.Press(4, in context); }, Is.Not.AllocatingGCMemory());
        }

        private int PlayAll(int[] keys)
        {
            int result = SongBook.NoSong;

            foreach (int key in keys)
            {
                result = Press(key);
            }

            return result;
        }

        private int Press(int hole)
        {
            SwarmSongContext context = Context();
            return _caster.Press(hole, in context);
        }

        private SwarmSongContext Context()
        {
            return new SwarmSongContext(Vector3.zero, _aim, _body, _forms, _fury, _hives, _enemies, new FakeSmokeClearer());
        }
    }
}
