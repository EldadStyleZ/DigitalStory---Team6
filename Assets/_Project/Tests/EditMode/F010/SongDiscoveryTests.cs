using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Seven songs in this order, with H J K L as holes 0 to 3: Dome H K J; Sting J K H; Tornado J K K H; Gust H L K;
    /// Hive Call L J L; Colossus L J K J L; Meteor J K L K. Only the Sting is known from the start, and only the Colossus is
    /// revealed when Fury fills.
    /// </summary>
    public sealed class SongDiscoveryTests
    {
        private const int HoleCount = 4;
        private const int Dome = 0;
        private const int Sting = 1;
        private const int Colossus = 5;
        private const int Meteor = 6;

        private SwarmConfig _swarmConfig;
        private FuryConfig _furyConfig;
        private HiveConfig _hiveConfig;
        private SongConfig _songConfig;
        private SquadConfig _squads;
        private SwarmBody _body;
        private FuryMeter _fury;
        private SwarmFormMachine _forms;
        private HiveSet _hives;
        private SwarmSongCaster _caster;
        private SongDiscovery _discovery;
        private readonly List<ISwarmTarget> _enemies = new List<ISwarmTarget>();
        private readonly List<SwarmSongEffect> _songs = new List<SwarmSongEffect>();
        private readonly List<int> _discovered = new List<int>();
        private readonly List<int> _revealed = new List<int>();

        [SetUp]
        public void SetUp()
        {
            _swarmConfig = ScriptableObject.CreateInstance<SwarmConfig>();
            _furyConfig = ScriptableObject.CreateInstance<FuryConfig>();
            _hiveConfig = ScriptableObject.CreateInstance<HiveConfig>();
            _songConfig = ScriptableObject.CreateInstance<SongConfig>();
            _squads = ScriptableObject.CreateInstance<SquadConfig>();
            _body = new SwarmBody(_swarmConfig);
            _fury = new FuryMeter(_furyConfig);
            _forms = new SwarmFormMachine(_body, _fury, _squads);
            _hives = new HiveSet(new List<Hive> { new Hive(_hiveConfig, new Vector3(-10f, 0f, 0f)), new Hive(_hiveConfig, new Vector3(10f, 0f, 0f)) });
            _songs.Clear();
            _songs.Add(ScriptableObject.CreateInstance<DomeSong>());
            _songs.Add(ScriptableObject.CreateInstance<StingSong>());
            _songs.Add(ScriptableObject.CreateInstance<TornadoSong>());
            _songs.Add(ScriptableObject.CreateInstance<GustSong>());
            _songs.Add(ScriptableObject.CreateInstance<HiveCallSong>());
            _songs.Add(ScriptableObject.CreateInstance<ColossusSong>());
            _songs.Add(ScriptableObject.CreateInstance<MeteorSong>());
            _caster = new SwarmSongCaster(_songs, _songConfig, HoleCount);
            _discovery = new SongDiscovery(_songs);
            _discovered.Clear();
            _revealed.Clear();
            _discovery.Discovered += song => _discovered.Add(song);
            _discovery.Revealed += song => _revealed.Add(song);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_swarmConfig);
            Object.DestroyImmediate(_furyConfig);
            Object.DestroyImmediate(_hiveConfig);
            Object.DestroyImmediate(_songConfig);
            Object.DestroyImmediate(_squads);

            foreach (SwarmSongEffect song in _songs)
            {
                Object.DestroyImmediate(song);
            }
        }

        // Categories and flags

        [Test]
        public void Category_EachSongClass_MatchesTheDesign()
        {
            Assert.That(_songs[Sting].Category, Is.EqualTo(SongCategory.Attack));
            Assert.That(_songs[2].Category, Is.EqualTo(SongCategory.Attack), "Tornado");
            Assert.That(_songs[Meteor].Category, Is.EqualTo(SongCategory.Attack));
            Assert.That(_songs[Colossus].Category, Is.EqualTo(SongCategory.Power), "the Colossus moved from Attack to Power");
            Assert.That(_songs[Dome].Category, Is.EqualTo(SongCategory.Defense));
            Assert.That(_songs[3].Category, Is.EqualTo(SongCategory.Defense), "Gust");
            Assert.That(_songs[4].Category, Is.EqualTo(SongCategory.Power), "Hive Call");
        }

        [Test]
        public void Flags_EachSongClass_OnlyTheStingIsKnownAndOnlyTheColossusIsRevealedByFury()
        {
            for (int song = 0; song < _songs.Count; song++)
            {
                Assert.That(_songs[song].KnownFromStart, Is.EqualTo(song == Sting), _songs[song].Song.Title);
                Assert.That(_songs[song].RevealedWhenFuryFills, Is.EqualTo(song == Colossus), _songs[song].Song.Title);
            }
        }

        // Known songs

        [Test]
        public void New_KnowsExactlyTheSongsKnownFromTheStart()
        {
            Assert.That(_discovery.Count, Is.EqualTo(7));
            Assert.That(_discovery.KnownCount, Is.EqualTo(1));

            for (int song = 0; song < _songs.Count; song++)
            {
                Assert.That(_discovery.IsKnown(song), Is.EqualTo(song == Sting), _songs[song].Song.Title);
                Assert.That(_discovery.IsRevealed(song), Is.EqualTo(song == Sting), _songs[song].Song.Title);
            }

            Assert.That(_discovered, Is.Empty);
            Assert.That(_revealed, Is.Empty);
        }

        [Test]
        public void New_NullSongs_Throws()
        {
            Assert.That(() => new SongDiscovery(null), Throws.ArgumentNullException);
        }

        [Test]
        public void NoteCast_AnUnknownSong_MakesItKnownAndRaisesDiscoveredOnce()
        {
            _discovery.NoteCast(Dome);
            _discovery.NoteCast(Dome);

            Assert.That(_discovery.IsKnown(Dome), Is.True);
            Assert.That(_discovery.IsRevealed(Dome), Is.True, "a known song is also revealed");
            Assert.That(_discovery.KnownCount, Is.EqualTo(2));
            Assert.That(_discovered, Is.EqualTo(new[] { Dome }));
        }

        [Test]
        public void NoteCast_AKnownSong_RaisesNothing()
        {
            _discovery.NoteCast(Sting);

            Assert.That(_discovery.KnownCount, Is.EqualTo(1));
            Assert.That(_discovered, Is.Empty);
        }

        [Test]
        public void Reveal_AnUnknownSong_ShowsItsNotesButDoesNotMakeItKnown()
        {
            _discovery.Reveal(Colossus);

            Assert.That(_discovery.IsRevealed(Colossus), Is.True);
            Assert.That(_discovery.IsKnown(Colossus), Is.False);
            Assert.That(_discovery.KnownCount, Is.EqualTo(1));
            Assert.That(_revealed, Is.EqualTo(new[] { Colossus }));
        }

        [Test]
        public void Reveal_Twice_RaisesRevealedOnce()
        {
            _discovery.Reveal(Colossus);
            _discovery.Reveal(Colossus);

            Assert.That(_revealed, Is.EqualTo(new[] { Colossus }));
        }

        [Test]
        public void Reveal_ASongThatIsKnown_RaisesNothing()
        {
            _discovery.Reveal(Sting);

            Assert.That(_revealed, Is.Empty);
        }

        [Test]
        public void NoteCast_ARevealedSong_BecomesKnownAndRaisesDiscovered()
        {
            _discovery.Reveal(Colossus);

            _discovery.NoteCast(Colossus);

            Assert.That(_discovery.IsKnown(Colossus), Is.True);
            Assert.That(_discovered, Is.EqualTo(new[] { Colossus }));
        }

        [TestCase(-1)]
        [TestCase(7)]
        public void IsKnownIsRevealedNoteCastReveal_OutOfRange_Throw(int song)
        {
            Assert.That(() => _discovery.IsKnown(song), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => _discovery.IsRevealed(song), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => _discovery.NoteCast(song), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => _discovery.Reveal(song), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        // Hints

        [Test]
        public void HintNotes_NoNotes_IsZero()
        {
            Assert.That(_discovery.HintNotes(_caster), Is.EqualTo(0));
        }

        [TestCase(new[] { 0 }, 1)]
        [TestCase(new[] { 0, 2 }, 2)]
        [TestCase(new[] { 1 }, 1)]
        [TestCase(new[] { 1, 2 }, 2)]
        [TestCase(new[] { 1, 2, 2 }, 3)]
        [TestCase(new[] { 1, 2, 3 }, 3)]
        [TestCase(new[] { 3, 1 }, 2)]
        [TestCase(new[] { 3, 1, 2 }, 3)]
        [TestCase(new[] { 3, 1, 2, 1 }, 4)]
        public void HintNotes_EveryPrefixOfASong_IsItsLength(int[] notes, int expected)
        {
            Play(notes);

            Assert.That(_discovery.HintNotes(_caster), Is.EqualTo(expected));
        }

        [Test]
        public void HintNotes_APrefixTwoSongsShare_CountsOnce()
        {
            Play(0);

            Assert.That(_discovery.HintNotes(_caster), Is.EqualTo(1), "Dome and Gust both begin with hole 0");
        }

        [Test]
        public void HintNotes_AfterTheSharedPrefixSplits_FollowsTheSongThatStillFits()
        {
            Play(0, 3);

            Assert.That(_discovery.HintNotes(_caster), Is.EqualTo(2), "only Gust begins with 0, 3");
        }

        [Test]
        public void HintNotes_OnlyTheLastNoteBeginsASong_IsOne()
        {
            Play(3, 3);

            Assert.That(_discovery.HintNotes(_caster), Is.EqualTo(1));
        }

        [Test]
        public void HintNotes_JunkBeforeTheBeginningOfASong_IsIgnored()
        {
            Play(3, 3, 0, 2);

            Assert.That(_discovery.HintNotes(_caster), Is.EqualTo(2), "0, 2 begins the Dome");
        }

        [Test]
        public void HintNotes_AfterACast_IsZero()
        {
            Play(0, 2, 1);

            Assert.That(_caster.RecentNoteCount, Is.EqualTo(0));
            Assert.That(_discovery.HintNotes(_caster), Is.EqualTo(0));
        }

        [Test]
        public void HintNotes_NotesOlderThanTheWindow_DoNotCount()
        {
            Play(0, 2);
            _caster.Tick(3f);

            Assert.That(_discovery.HintNotes(_caster), Is.EqualTo(0));
        }

        [Test]
        public void HintNotes_OnlyTheOldestNoteIsTooOld_CountsTheRest()
        {
            Play(0);
            _caster.Tick(2f);
            Play(1);
            _caster.Tick(1.5f);

            Assert.That(_discovery.HintNotes(_caster), Is.EqualTo(1), "only 1 is left, and the Sting begins with 1");
        }

        // Changed with the cost rule: the caster owns the cooldowns, so a song completed while it cools is refused and uses
        // up its notes like any other refusal, where before its notes stayed. It still proves the hint never shows a whole song.
        [Test]
        public void HintNotes_WholeSongPlayedWhileItCools_NeverReturnsTheSongsLength()
        {
            Play(0, 2, 1);
            Play(0, 2, 1);

            Assert.That(_caster.RecentNoteCount, Is.EqualTo(0), "the refused song's notes are used up");
            Assert.That(_discovery.HintNotes(_caster), Is.EqualTo(0));
        }

        [Test]
        public void HintNotes_Null_Throws()
        {
            Assert.That(() => _discovery.HintNotes(null), Throws.ArgumentNullException);
        }

        [Test]
        public void HintNotes_Asked_AllocatesNothing()
        {
            Play(3, 1);

            Assert.That(() => { _discovery.HintNotes(_caster); }, Is.Not.AllocatingGCMemory());
        }

        // ---- The song table fills in (plan section N) ----

        private const int Tornado = 2;
        private const int Gust = 3;

        // As the session does: after every note the flute hears, the discovery records how far each song's beginning got.
        private void RecordEveryNote()
        {
            _caster.NotePlayed += _ => _discovery.RecordNotes(_caster);
        }

        [Test]
        public void KnownNotes_New_IsZeroForHiddenSongsAndTheWholeSongForKnownOnes()
        {
            Assert.That(_discovery.KnownNotes(Dome), Is.EqualTo(0));
            Assert.That(_discovery.KnownNotes(Colossus), Is.EqualTo(0));
            Assert.That(_discovery.KnownNotes(Sting), Is.EqualTo(3), "the Sting is known from the start");
        }

        [Test]
        public void KnownNotes_TheBeginningPlayedInOrder_RisesNoteByNote()
        {
            RecordEveryNote();

            Play(0);
            Assert.That(_discovery.KnownNotes(Dome), Is.EqualTo(1));
            Assert.That(_discovery.KnownNotes(Gust), Is.EqualTo(1), "H begins both");

            Play(2);
            Assert.That(_discovery.KnownNotes(Dome), Is.EqualTo(2));
            Assert.That(_discovery.KnownNotes(Gust), Is.EqualTo(1), "H K is not how the Gust goes on");
        }

        [Test]
        public void KnownNotes_LaterNotesThatLeadNowhere_NeverLowerIt()
        {
            RecordEveryNote();
            Play(0, 2);

            Play(3, 3, 3);
            _caster.Tick(10f);

            Assert.That(_discovery.KnownNotes(Dome), Is.EqualTo(2), "kept for the whole match");
        }

        [Test]
        public void KnownNotes_ABeginningReachedInsideALongerRun_Counts()
        {
            RecordEveryNote();

            Play(3, 3, 0, 2);

            Assert.That(_discovery.KnownNotes(Dome), Is.EqualTo(2), "L L H K ends with the Dome's H K");
        }

        [Test]
        public void KnownNotes_NotesOutOfOrder_CountOnlyWhatBeginsTheSong()
        {
            RecordEveryNote();

            Play(2, 0);

            Assert.That(_discovery.KnownNotes(Dome), Is.EqualTo(1), "K H holds only the Dome's first note, the H at its end");
        }

        [Test]
        public void KnownNotes_ABeginningSongsShare_FillsEachOfThem()
        {
            RecordEveryNote();

            Play(1, 2);

            Assert.That(_discovery.KnownNotes(Tornado), Is.EqualTo(2));
            Assert.That(_discovery.KnownNotes(Meteor), Is.EqualTo(2));
        }

        [Test]
        public void KnownNotes_ASongOnceCast_IsTheWholeSong()
        {
            RecordEveryNote();
            _caster.SongCast += _discovery.NoteCast;

            Play(0, 2, 1);

            Assert.That(_discovery.IsKnown(Dome), Is.True);
            Assert.That(_discovery.KnownNotes(Dome), Is.EqualTo(3));
        }

        // Plan section M: a refused cast teaches nothing false. The notes still fill in the table, but the song stays unknown.
        [Test]
        public void KnownNotes_AWholeSongRefused_FillsEveryNoteButTheSongStaysUnknown()
        {
            RecordEveryNote();
            _caster.SongCast += _discovery.NoteCast;

            Play(3, 1, 2, 1, 3);

            Assert.That(_discovery.KnownNotes(Colossus), Is.EqualTo(5));
            Assert.That(_discovery.IsKnown(Colossus), Is.False, "Fury is empty, so it was refused");
        }

        [Test]
        public void KnownNotes_NotesTooFarApartToMakeASong_DoNotJoin()
        {
            RecordEveryNote();
            Play(0);
            _caster.Tick(3f);

            Play(2);

            Assert.That(_discovery.KnownNotes(Dome), Is.EqualTo(1), "the H was forgotten before the K");
        }

        [Test]
        public void KnownNotes_NotesPlayedWhileCollapsed_CountNothing()
        {
            RecordEveryNote();
            _body.Lose(_swarmConfig.StartingWasps);

            Play(0, 2);

            Assert.That(_discovery.KnownNotes(Dome), Is.EqualTo(0), "the flute hears nothing while the swarm is down");
        }

        [TestCase(-1)]
        [TestCase(7)]
        public void KnownNotes_OutOfRange_Throws(int song)
        {
            Assert.That(() => _discovery.KnownNotes(song), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void RecordNotes_Null_Throws()
        {
            Assert.That(() => _discovery.RecordNotes(null), Throws.ArgumentNullException);
        }

        [Test]
        public void RecordNotes_AllocatesNothing()
        {
            Play(3, 1);

            Assert.That(() => { _discovery.RecordNotes(_caster); }, Is.Not.AllocatingGCMemory());
        }

        private void Play(params int[] holes)
        {
            foreach (int hole in holes)
            {
                var context = new SwarmSongContext(Vector3.zero, null, Vector3.zero, _body, _forms, _fury, _hives, _enemies, new FakeSmokeClearer());
                _caster.Press(hole, in context);
            }
        }
    }
}
