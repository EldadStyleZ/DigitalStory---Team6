using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Replays plan section D's key-table proof (check_final.py) on the real song assets' keys, for the songs built so far:
    /// no song lies inside another, every song casts exactly itself from silence, a cooling song casts nothing, one stray
    /// note never casts another song, and the three laws. Holes 0 to 3 are H, J, K and L.
    /// </summary>
    public sealed class SongKeyTableTests
    {
        private const int HoleCount = 4;
        private const int H = 0;
        private const int L = 3;

        private readonly List<SwarmSongEffect> _songs = new List<SwarmSongEffect>();
        private readonly List<Object> _assets = new List<Object>();
        private readonly List<ISwarmTarget> _enemies = new List<ISwarmTarget>();

        private SwarmConfig _swarmConfig;
        private FuryConfig _furyConfig;
        private SquadConfig _squads;
        private SongConfig _songConfig;
        private HiveSet _hives;

        [SetUp]
        public void SetUp()
        {
            _swarmConfig = Make<SwarmConfig>();
            _furyConfig = Make<FuryConfig>();
            _squads = Make<SquadConfig>();
            _songConfig = Make<SongConfig>();
            HiveConfig hiveConfig = Make<HiveConfig>();
            _hives = new HiveSet(new List<Hive> { new Hive(hiveConfig, new Vector3(-10f, 0f, 0f)), new Hive(hiveConfig, new Vector3(10f, 0f, 0f)) });
            _songs.Clear();
            _songs.Add(Make<StingSong>());
            _songs.Add(Make<TornadoSong>());
            _songs.Add(Make<MeteorSong>());
            _songs.Add(Make<DomeSong>());
            _songs.Add(Make<GustSong>());
            _songs.Add(Make<HiveCallSong>());
            _songs.Add(Make<ColossusSong>());
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in _assets)
            {
                Object.DestroyImmediate(asset);
            }

            _assets.Clear();
        }

        [Test]
        public void Keys_AllFitTheFourHoleFlute()
        {
            for (int song = 0; song < _songs.Count; song++)
            {
                foreach (int key in Keys(song))
                {
                    Assert.That(key, Is.InRange(0, HoleCount - 1), Title(song));
                }
            }

            Assert.That(() => new SwarmSongCaster(_songs, _songConfig, HoleCount), Throws.Nothing);
        }

        [Test]
        public void Pairs_NoSongsNotesLieInsideAnothersBeforeItsLastNote()
        {
            var clashes = new List<string>();

            for (int inner = 0; inner < _songs.Count; inner++)
            {
                for (int outer = 0; outer < _songs.Count; outer++)
                {
                    if (inner == outer)
                    {
                        continue;
                    }

                    for (int end = Keys(inner).Count - 1; end < Keys(outer).Count - 1; end++)
                    {
                        if (EndsAt(Keys(outer), end, Keys(inner)))
                        {
                            clashes.Add(Title(inner) + " inside " + Title(outer));
                        }
                    }
                }
            }

            Assert.That(clashes, Is.Empty);
        }

        [Test]
        public void Pairs_NoSongEndsAnother()
        {
            var clashes = new List<string>();

            for (int inner = 0; inner < _songs.Count; inner++)
            {
                for (int outer = 0; outer < _songs.Count; outer++)
                {
                    if (inner != outer && Keys(inner).Count <= Keys(outer).Count && EndsAt(Keys(outer), Keys(outer).Count - 1, Keys(inner)))
                    {
                        clashes.Add(Title(inner) + " ends " + Title(outer));
                    }
                }
            }

            Assert.That(clashes, Is.Empty);
        }

        [Test]
        public void Silence_EverySong_CastsExactlyItselfOnItsLastNote()
        {
            for (int song = 0; song < _songs.Count; song++)
            {
                var casts = new List<int>();
                var atNotes = new List<int>();

                Replay(Keys(song), casts, atNotes);

                Assert.That(casts, Is.EqualTo(new[] { song }), Title(song));
                Assert.That(atNotes, Is.EqualTo(new[] { Keys(song).Count - 1 }), Title(song));
            }
        }

        [Test]
        public void Cooling_EverySongPlayedAgainRightAway_CastsNothing()
        {
            for (int song = 0; song < _songs.Count; song++)
            {
                var notes = new List<int>(Keys(song));
                notes.AddRange(Keys(song));
                var casts = new List<int>();

                Replay(notes, casts, new List<int>());

                Assert.That(casts, Is.EqualTo(new[] { song }), Title(song) + " played again while it cools");
            }
        }

        [Test]
        public void OneStrayNote_BeforeEverySong_NeverCastsAnotherSong()
        {
            for (int song = 0; song < _songs.Count; song++)
            {
                for (int stray = 0; stray < HoleCount; stray++)
                {
                    var notes = new List<int> { stray };
                    notes.AddRange(Keys(song));
                    var casts = new List<int>();
                    var atNotes = new List<int>();

                    Replay(notes, casts, atNotes);

                    string message = "hole " + stray + " before " + Title(song);
                    Assert.That(casts, Is.EqualTo(new[] { song }), message);
                    Assert.That(atNotes, Is.EqualTo(new[] { Keys(song).Count }), message);
                }
            }
        }

        [Test]
        public void LawOne_TheFirstNoteIsTheColour()
        {
            for (int song = 0; song < _songs.Count; song++)
            {
                int first = Keys(song)[0];
                SongCategory colour = first == H ? SongCategory.Defense : first == L ? SongCategory.Power : SongCategory.Attack;

                Assert.That(_songs[song].Category, Is.EqualTo(colour), Title(song));
            }
        }

        [Test]
        public void LawTwo_TheDomeIsTheStingPlayedBackwards()
        {
            var backwards = new List<int>(Keys(Index<StingSong>()));
            backwards.Reverse();

            Assert.That(Keys(Index<DomeSong>()), Is.EqualTo(backwards));
        }

        [Test]
        public void LawThree_GoldSongsReadTheSameBothWaysAndNoOtherSongDoes()
        {
            for (int song = 0; song < _songs.Count; song++)
            {
                bool isGold = _songs[song].Category == SongCategory.Power;

                Assert.That(ReadsTheSameBothWays(Keys(song)), Is.EqualTo(isGold), Title(song));
            }
        }

        private static bool EndsAt(IReadOnlyList<int> outer, int end, IReadOnlyList<int> inner)
        {
            int start = end - inner.Count + 1;

            if (start < 0)
            {
                return false;
            }

            for (int key = 0; key < inner.Count; key++)
            {
                if (outer[start + key] != inner[key])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ReadsTheSameBothWays(IReadOnlyList<int> keys)
        {
            for (int key = 0; key < keys.Count / 2; key++)
            {
                if (keys[key] != keys[keys.Count - 1 - key])
                {
                    return false;
                }
            }

            return true;
        }

        // Plays the notes from silence on a fresh swarm with full Fury and nothing to hit, and records each cast and the note that cast it.
        private void Replay(IReadOnlyList<int> notes, List<int> casts, List<int> atNotes)
        {
            var body = new SwarmBody(_swarmConfig);
            var fury = new FuryMeter(_furyConfig);
            fury.AddDamage(_furyConfig.Max * _furyConfig.DamagePerFuryPoint);
            var forms = new SwarmFormMachine(body, fury, _squads);
            var caster = new SwarmSongCaster(_songs, _songConfig, HoleCount);
            var context = new SwarmSongContext(Vector3.zero, null, Vector3.forward * 6f, body, forms, fury, _hives, _enemies, null);
            int note = 0;
            caster.SongCast += song =>
            {
                casts.Add(song);
                atNotes.Add(note);
            };

            for (note = 0; note < notes.Count; note++)
            {
                caster.Press(notes[note], in context);
            }
        }

        private IReadOnlyList<int> Keys(int song) => _songs[song].Song.Keys;

        private string Title(int song) => _songs[song].Song.Title;

        private int Index<T>() where T : SwarmSongEffect
        {
            return _songs.FindIndex(song => song is T);
        }

        private T Make<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }
    }
}
