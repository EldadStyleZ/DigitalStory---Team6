using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Replays plan section D's key-table proof (check_final.py) on the real song assets' keys, for the twelve songs built
    /// so far, in the plan's order: no song lies inside another, every song casts exactly itself from silence, a cooling song
    /// casts nothing, one stray note never casts another song, and the three laws. Holes 0 to 3 are H, J, K and L.
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
            _songs.Add(Make<HydraSong>());
            _songs.Add(Make<ScytheSong>());
            _songs.Add(Make<LanceSong>());
            _songs.Add(Make<NestSong>());
            _songs.Add(Make<DomeSong>());
            _songs.Add(Make<GustSong>());
            _songs.Add(Make<HoneycombSong>());
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
        public void Songs_AreTheTwelveBuiltSoFar_InThePlansOrder()
        {
            var titles = new List<string>();
            for (int song = 0; song < _songs.Count; song++)
            {
                titles.Add(Title(song));
            }

            Assert.That(
                titles,
                Is.EqualTo(new[] { "Sting", "Tornado", "Meteor", "Hydra", "Scythe", "Lance", "Nest", "Dome", "Gust", "Honeycomb", "Hive Call", "Colossus" }));
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
            Assert.That(Keys(Index<DomeSong>()), Is.EqualTo(Backwards(Index<StingSong>())));
        }

        [Test]
        public void LawTwo_TheHoneycombIsTheNestPlayedBackwards()
        {
            Assert.That(Keys(Index<HoneycombSong>()), Is.EqualTo(Backwards(Index<NestSong>())));
        }

        [Test]
        public void LawTwo_TheGustIsTheScythePlayedBackwards()
        {
            Assert.That(Keys(Index<GustSong>()), Is.EqualTo(Backwards(Index<ScytheSong>())));
        }

        [Test]
        public void LawTwo_EveryBlueSongIsARedSongPlayedBackwards()
        {
            for (int blue = 0; blue < _songs.Count; blue++)
            {
                if (_songs[blue].Category != SongCategory.Defense)
                {
                    continue;
                }

                int twin = SongWithKeys(Backwards(blue));

                Assert.That(twin, Is.Not.EqualTo(-1), Title(blue) + " played backwards is no song");
                Assert.That(_songs[twin].Category, Is.EqualTo(SongCategory.Attack), Title(blue) + "'s twin");
            }
        }

        // An attack ends on H exactly when it has a twin: the Hydra and the Meteor end on other notes, and their backwards
        // notes are no song. (The Tornado and the Lance end on H for the Maelstrom and the Wings, which are not built yet.)
        [Test]
        public void LawTwo_TheHydraAndTheMeteorEndOffH_AndPlayedBackwardsAreNoSong()
        {
            foreach (int song in new[] { Index<HydraSong>(), Index<MeteorSong>() })
            {
                IReadOnlyList<int> keys = Keys(song);

                Assert.That(keys[keys.Count - 1], Is.Not.EqualTo(H), Title(song));
                Assert.That(SongWithKeys(Backwards(song)), Is.EqualTo(-1), Title(song));
            }
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

        private List<int> Backwards(int song)
        {
            var backwards = new List<int>(Keys(song));
            backwards.Reverse();
            return backwards;
        }

        // The index of the song played with exactly these keys, or -1.
        private int SongWithKeys(IReadOnlyList<int> keys)
        {
            for (int song = 0; song < _songs.Count; song++)
            {
                IReadOnlyList<int> candidate = Keys(song);
                if (candidate.Count != keys.Count)
                {
                    continue;
                }

                bool same = true;
                for (int key = 0; key < keys.Count && same; key++)
                {
                    same = candidate[key] == keys[key];
                }

                if (same)
                {
                    return song;
                }
            }

            return -1;
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
