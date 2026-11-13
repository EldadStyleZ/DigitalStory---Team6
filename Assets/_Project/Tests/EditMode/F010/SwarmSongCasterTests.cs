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
        private const int HoleCount = 4;
        private const int H = 0;
        private const int J = 1;
        private const int K = 2;
        private const int L = 3;
        private const int GustIndex = 3;
        private const int ColossusIndex = 5;
        private const int NestIndex = 10;
        private const int HoneycombIndex = 11;

        private static readonly int[][] SongKeys =
        {
            new[] { H, K, J },
            new[] { J, K, H },
            new[] { J, K, K, H },
            new[] { H, L, K },
            new[] { L, J, L },
            new[] { L, J, K, J, L },
            new[] { J, K, L, K },
            new[] { J, J, L },
            new[] { K, L, H },
            new[] { K, L, L, H },
            new[] { K, L, J, H },
            new[] { H, J, L, K },
        };

        private static readonly SwarmForm[] SongForms =
        {
            SwarmForm.Dome, SwarmForm.Sting, SwarmForm.Tornado, SwarmForm.Gust, SwarmForm.HiveCall, SwarmForm.Colossus, SwarmForm.Meteor,
            SwarmForm.Hydra, SwarmForm.Scythe, SwarmForm.Lance, SwarmForm.Nest, SwarmForm.Honeycomb,
        };

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
        private FakeSwarmTarget _aim;
        private readonly List<ISwarmTarget> _enemies = new List<ISwarmTarget>();
        private readonly List<SwarmSongEffect> _songs = new List<SwarmSongEffect>();
        private readonly List<int> _cast = new List<int>();
        private readonly List<int> _played = new List<int>();
        private readonly List<int> _notReady = new List<int>();
        private readonly List<SongRefusal> _reasons = new List<SongRefusal>();
        private bool _areSongsFree;

        // Damage that fills the Fury meter, read from the config.
        private float FillingDamage => _furyConfig.Max * _furyConfig.DamagePerFuryPoint;

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
            _aim = new FakeSwarmTarget();
            _songs.Clear();
            _songs.Add(ScriptableObject.CreateInstance<DomeSong>());
            _songs.Add(ScriptableObject.CreateInstance<StingSong>());
            _songs.Add(ScriptableObject.CreateInstance<TornadoSong>());
            _songs.Add(ScriptableObject.CreateInstance<GustSong>());
            _songs.Add(ScriptableObject.CreateInstance<HiveCallSong>());
            _songs.Add(ScriptableObject.CreateInstance<ColossusSong>());
            _songs.Add(ScriptableObject.CreateInstance<MeteorSong>());
            _songs.Add(ScriptableObject.CreateInstance<HydraSong>());
            _songs.Add(ScriptableObject.CreateInstance<ScytheSong>());
            _songs.Add(ScriptableObject.CreateInstance<LanceSong>());
            _songs.Add(ScriptableObject.CreateInstance<NestSong>());
            _songs.Add(ScriptableObject.CreateInstance<HoneycombSong>());
            _caster = new SwarmSongCaster(_songs, _songConfig, HoleCount);
            _cast.Clear();
            _played.Clear();
            _notReady.Clear();
            _reasons.Clear();
            _areSongsFree = false;
            _caster.SongNotReady += (song, reason) =>
            {
                _notReady.Add(song);
                _reasons.Add(reason);
            };
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
            Object.DestroyImmediate(_squads);

            foreach (SwarmSongEffect song in _songs)
            {
                Object.DestroyImmediate(song);
            }
        }

        [Test]
        public void Count_TwelveSongs_AreExposedByIndex()
        {
            Assert.That(_caster.Count, Is.EqualTo(12));
            Assert.That(_caster[0], Is.SameAs(_songs[0]));
            Assert.That(_caster[5], Is.SameAs(_songs[5]));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        public void Press_EachSongsKeysFromSilence_CastExactlyThatSong(int song)
        {
            _fury.AddDamage(1000f);

            int result = PlayAll(SongKeys[song]);

            Assert.That(result, Is.EqualTo(song));
            Assert.That(_cast, Is.EqualTo(new[] { song }), "one cast, and it is this song");
            Assert.That(Showing(song), Is.EqualTo(SongForms[song]));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
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
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
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
            _fury.AddDamage(FillingDamage - 0.2f);

            int result = PlayAll(SongKeys[5]);

            Assert.That(result, Is.EqualTo(SongBook.NoSong));
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_fury.Value, Is.EqualTo((FillingDamage - 0.2f) / _furyConfig.DamagePerFuryPoint).Within(1e-3f), "nothing was used");
        }

        [Test]
        public void Press_ColossusWithFullFury_CastsAndEmptiesTheMeter()
        {
            _fury.AddDamage(FillingDamage);

            int result = PlayAll(SongKeys[5]);

            Assert.That(result, Is.EqualTo(5));
            Assert.That(_fury.Value, Is.EqualTo(0f));
        }

        [Test]
        public void Press_ColossusRightAfterUsingFury_CannotBeCastAgain()
        {
            _fury.AddDamage(FillingDamage);
            PlayAll(SongKeys[5]);
            _cast.Clear();

            int again = PlayAll(SongKeys[5]);

            Assert.That(again, Is.EqualTo(SongBook.NoSong));
            Assert.That(_cast, Is.Empty);
        }

        // CanCast and collapse

        [Test]
        public void Press_StingWithNothingAimedAt_StillCasts()
        {
            _aim = null;

            int result = PlayAll(SongKeys[1]);

            Assert.That(result, Is.EqualTo(1));
            Assert.That(_cast, Is.EqualTo(new[] { 1 }));
            Assert.That(Showing(1), Is.EqualTo(SwarmForm.Sting), "it wraps the air ahead");
        }

        [Test]
        public void Press_SongThatCannotBeCast_UsesUpTheNotes()
        {
            PlayAll(SongKeys[5]);

            Assert.That(_cast, Is.Empty, "the Colossus needs full Fury");
            Assert.That(_caster.RecentNoteCount, Is.EqualTo(0));
        }

        [Test]
        public void Press_WhileCollapsed_CastsNothingAndRemembersNothing()
        {
            _body.Lose(_body.Count);

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
            _body.Lose(_body.Count);
            PlayAll(SongKeys[0]);
            _body.Tick(3f, _hives);

            int result = PlayAll(SongKeys[0]);

            Assert.That(result, Is.EqualTo(0));
        }

        // SongNotReady

        [Test]
        public void SongNotReady_NotesCompletedWhileCoolingDown_IsRaisedWithThatSongAndCoolingDown()
        {
            PlayAll(SongKeys[0]);
            _notReady.Clear();

            PlayAll(SongKeys[0]);

            Assert.That(_notReady, Is.EqualTo(new[] { 0 }));
            Assert.That(_reasons, Is.EqualTo(new[] { SongRefusal.CoolingDown }));
            Assert.That(_cast, Is.EqualTo(new[] { 0 }), "only the first cast");
        }

        // Changed with the cost rule: the caster owns the cooldowns now, so the song book (given no cooldowns) matches the
        // cooling song and uses up its notes, as every other refusal does. It still proves the refused song casts nothing.
        [Test]
        public void SongNotReady_NotesCompletedWhileCoolingDown_AreUsedUpAndCastNothing()
        {
            PlayAll(SongKeys[0]);
            _cast.Clear();

            PlayAll(SongKeys[0]);

            Assert.That(_caster.RecentNoteCount, Is.EqualTo(0));
            Assert.That(_cast, Is.Empty);
        }

        [Test]
        public void SongNotReady_RefusedWhileCoolingDown_DoesNotRestartTheCooldown()
        {
            PlayAll(SongKeys[0]);
            _caster.Tick(4f);

            PlayAll(SongKeys[0]);

            Assert.That(_caster.CooldownFraction(0), Is.EqualTo(0.5f).Within(1e-4f), "4 s of the Dome's 8 s are left, as before the refusal");
        }

        [Test]
        public void SongNotReady_NotesCompletedWhileCollapsed_IsRaisedAndNothingIsRemembered()
        {
            _body.Lose(_body.Count);

            PlayAll(SongKeys[0]);

            Assert.That(_notReady, Is.EqualTo(new[] { 0 }));
            Assert.That(_reasons, Is.EqualTo(new[] { SongRefusal.Collapsed }));
            Assert.That(_caster.RecentNoteCount, Is.EqualTo(0));
            Assert.That(_played, Is.Empty);
        }

        [Test]
        public void SongNotReady_CollapsedAndOnlyPartOfASong_IsNotRaised()
        {
            _body.Lose(_body.Count);

            Press(SongKeys[0][0]);
            Press(SongKeys[0][1]);

            Assert.That(_notReady, Is.Empty);
        }

        [Test]
        public void SongNotReady_CollapsedNotesTooOld_AreNotCounted()
        {
            _body.Lose(_body.Count);
            Press(SongKeys[0][0]);
            Press(SongKeys[0][1]);
            _caster.Tick(3f);

            Press(SongKeys[0][2]);

            Assert.That(_notReady, Is.Empty);
        }

        [Test]
        public void SongNotReady_NotesCompletedWhileTheColossusStands_IsRaisedAndTheNotesAreUsedUp()
        {
            _fury.AddDamage(1000f);
            _forms.StartColossus(12f);

            PlayAll(SongKeys[0]);

            Assert.That(_notReady, Is.EqualTo(new[] { 0 }));
            Assert.That(_reasons, Is.EqualTo(new[] { SongRefusal.GiantStanding }));
            Assert.That(_caster.RecentNoteCount, Is.EqualTo(0));
        }

        [Test]
        public void SongNotReady_CanCastIsFalse_IsRaisedAndTheNotesAreUsedUp()
        {
            PlayAll(SongKeys[5]);

            Assert.That(_notReady, Is.EqualTo(new[] { 5 }));
            Assert.That(_caster.RecentNoteCount, Is.EqualTo(0));
        }

        [Test]
        public void SongNotReady_ColossusWithoutFullFury_IsRaisedWithTheColossusAndNeedsFury()
        {
            _fury.AddDamage(FillingDamage - 0.2f);

            PlayAll(SongKeys[5]);

            Assert.That(_notReady, Is.EqualTo(new[] { 5 }));
            Assert.That(_reasons, Is.EqualTo(new[] { SongRefusal.NeedsFury }));
        }

        // ---- Costs (plan section M) ----

        // The song at each index costs the plan's price: Dome 40, Sting 5 for each sting, Tornado 60, Gust 20, Hive Call 0,
        // Colossus 0 (Fury is its price), Meteor 90, Hydra 60, Scythe 30, Lance 70, Nest 300 and Honeycomb 150. The playtest bot
        // set the Sting's (was 10) and the Nest's (was 100, then 200) after plan sections O and I.
        [TestCase(0, 40)]
        [TestCase(1, 5)]
        [TestCase(2, 60)]
        [TestCase(3, 20)]
        [TestCase(4, 0)]
        [TestCase(5, 0)]
        [TestCase(6, 90)]
        [TestCase(7, 60)]
        [TestCase(8, 30)]
        [TestCase(9, 70)]
        [TestCase(10, 300)]
        [TestCase(11, 150)]
        public void WaspCost_IsThePlansPrice(int song, int cost)
        {
            Assert.That(_songs[song].WaspCost, Is.EqualTo(cost));
        }

        // Changed with the first-cast rule: a song's first cast in a match is free, so finding a song is never punished. The
        // price is paid from the second cast on. Changed with the many stings (plan section O): the swarm's 800 wasps send two
        // stings, and the Sting's price is paid for each sting sent. The prices are read from the songs, which
        // WaspCost_IsThePlansPrice holds to the plan.
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        public void Press_ASecondCastInASurvivePhase_LowersTheCountByTheSongsCostAtTheCast(int song)
        {
            UseUpTheFreeCast(song);
            int before = _body.Count;
            int cost = _songs[song].WaspCostFor(before);
            int countAtTheCast = -1;
            _caster.SongCast += _ => countAtTheCast = _body.Count;

            PlayAll(SongKeys[song]);

            Assert.That(_cast, Is.EqualTo(new[] { song }));
            Assert.That(countAtTheCast, Is.EqualTo(before - cost), "paid by the time the cast is reported");
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        public void Press_TheFirstCastOfEachSongInASurvivePhase_IsFree(int song)
        {
            _fury.AddDamage(1000f);
            int countAtTheCast = -1;
            _caster.SongCast += _ => countAtTheCast = _body.Count;

            PlayAll(SongKeys[song]);

            Assert.That(_cast, Is.EqualTo(new[] { song }));
            Assert.That(countAtTheCast, Is.EqualTo(_swarmConfig.StartingWasps), "finding a song is never punished");
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(6)]
        [TestCase(NestIndex)]
        [TestCase(HoneycombIndex)]
        public void Press_SongsAreFree_TheFirstCostsNothing(int song)
        {
            _areSongsFree = true;

            PlayAll(SongKeys[song]);

            Assert.That(_cast, Is.EqualTo(new[] { song }));
            Assert.That(_body.Count, Is.EqualTo(_swarmConfig.StartingWasps));
        }

        // Plan section M: a tower's wasps stay on the map, so from its second cast on it costs its price in a farm phase too.
        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(6, false)]
        [TestCase(7, false)]
        [TestCase(NestIndex, true)]
        [TestCase(HoneycombIndex, true)]
        public void Press_ASecondCastInAFarmPhase_CostsNothing_UnlessItIsATower(int song, bool paysItsPrice)
        {
            _areSongsFree = true;
            UseUpTheFreeCast(song);
            int before = _body.Count;
            int cost = paysItsPrice ? _songs[song].WaspCost : 0;

            PlayAll(SongKeys[song]);

            Assert.That(_cast, Is.EqualTo(new[] { song }));
            Assert.That(_body.Count, Is.EqualTo(before - cost));
        }

        [Test]
        public void Press_ATowerInAFarmPhaseWithTooFewWasps_IsRefusedWithNotEnoughWasps()
        {
            _areSongsFree = true;
            UseUpTheFreeCast(NestIndex);
            LeaveAboveTheCollapseLine(99);

            int result = PlayAll(SongKeys[NestIndex]);

            Assert.That(result, Is.EqualTo(SongBook.NoSong));
            Assert.That(_reasons, Is.EqualTo(new[] { SongRefusal.NotEnoughWasps }));
            Assert.That(_body.Count, Is.EqualTo(_body.CollapseLine + 99));
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(3, false)]
        [TestCase(4, false)]
        [TestCase(5, false)]
        [TestCase(7, false)]
        [TestCase(8, false)]
        [TestCase(9, false)]
        [TestCase(NestIndex, true)]
        [TestCase(HoneycombIndex, true)]
        public void CostsInFarmPhase_IsTrueForTheTowersInThePostSlotOnly(int song, bool isTower)
        {
            Assert.That(_songs[song].CostsInFarmPhase, Is.EqualTo(isTower));
        }

        [Test]
        public void Press_ACastRefusedWhileTheGiantStands_DoesNotUseUpTheFreeCast()
        {
            _forms.StartColossus(1f);
            PlayAll(SongKeys[0]);
            Assert.That(_reasons, Is.EqualTo(new[] { SongRefusal.GiantStanding }));
            _forms.Tick(1.1f, _hives);
            LeaveAboveTheCollapseLine(0);

            int result = PlayAll(SongKeys[0]);

            Assert.That(result, Is.EqualTo(0), "the Dome's first cast is still free");
            Assert.That(_body.Count, Is.EqualTo(_body.CollapseLine));
        }

        [Test]
        public void Press_ACastRefusedWhileCollapsed_DoesNotUseUpTheFreeCast()
        {
            _body.Lose(_body.Count);
            PlayAll(SongKeys[0]);
            Assert.That(_reasons, Is.EqualTo(new[] { SongRefusal.Collapsed }));
            _body.Tick(3f, _hives);
            LeaveAboveTheCollapseLine(0);

            int result = PlayAll(SongKeys[0]);

            Assert.That(result, Is.EqualTo(0));
            Assert.That(_body.Count, Is.EqualTo(_body.CollapseLine));
        }

        [Test]
        public void Press_ACastRefusedWhileCooling_DoesNotChangeThePriceOfTheNext()
        {
            PlayAll(SongKeys[1]);
            PlayAll(SongKeys[1]);
            _caster.Tick(1.6f);
            int before = _body.Count;

            PlayAll(SongKeys[1]);

            Assert.That(_body.Count, Is.EqualTo(before - (2 * _songs[1].WaspCost)), "the second cast that happened pays the Sting's price for each of its two stings");
        }

        [Test]
        public void Price_IsNothingBeforeTheFirstCast_ThenTheSongsCostInASurvivePhase()
        {
            Assert.That(_caster.Price(0, false, _body.Count), Is.EqualTo(0));
            Assert.That(_caster.Price(0, true, _body.Count), Is.EqualTo(0));

            PlayAll(SongKeys[0]);

            Assert.That(_caster.Price(0, false, _body.Count), Is.EqualTo(40));
            Assert.That(_caster.Price(0, true, _body.Count), Is.EqualTo(0), "free in a farm phase");
            Assert.That(_caster.Price(1, false, _body.Count), Is.EqualTo(0), "other songs keep their free cast");
        }

        [Test]
        public void Price_OfATowerAfterItsFirstCast_IsItsCostInEitherPhase()
        {
            _areSongsFree = true;
            PlayAll(SongKeys[HoneycombIndex]);

            Assert.That(_caster.Price(HoneycombIndex, true, _body.Count), Is.EqualTo(_songs[HoneycombIndex].WaspCost));
            Assert.That(_caster.Price(HoneycombIndex, false, _body.Count), Is.EqualTo(_songs[HoneycombIndex].WaspCost));
        }

        // Plan section O: one sting for every 400 living wasps, and the Sting's price for each sting sent.
        [TestCase(400, 1)]
        [TestCase(800, 2)]
        [TestCase(1999, 4)]
        [TestCase(4000, 10)]
        public void Price_OfTheSting_IsItsCostForEachStingTheLivingWaspsSend(int livingWasps, int stings)
        {
            UseUpTheFreeCast(1);

            Assert.That(_caster.Price(1, false, livingWasps), Is.EqualTo(stings * _songs[1].WaspCost));
            Assert.That(_caster.Price(1, true, livingWasps), Is.EqualTo(0), "free in a farm phase");
            Assert.That(_caster.Price(0, false, livingWasps), Is.EqualTo(0), "no other song reads the count");
        }

        // The count is taken as the notes complete, before the price is paid, so paying for the second sting cannot take it away.
        [Test]
        public void Press_TheSting_SendsAndPaysForTheStingsTheCountSentBeforePaying()
        {
            UseUpTheFreeCast(1);
            Assert.That(_body.Count, Is.EqualTo(800));

            PlayAll(SongKeys[1]);

            Assert.That(_body.Count, Is.EqualTo(800 - (2 * _songs[1].WaspCost)));
            Assert.That(_forms.StingCount, Is.EqualTo(2), "what is left would send one; the 800 at the cast send two");
        }

        [Test]
        public void Press_TheStingWhoseStingsWouldTakeTheSwarmBelowItsCollapseLine_IsRefused()
        {
            UseUpTheFreeCast(1);
            JsonUtility.FromJsonOverwrite("{\"<WaspsPerSting>k__BackingField\":10}", _songs[1]);
            int spendable = (StingSong.MostStings * _songs[1].WaspCost) - 1;
            LeaveAboveTheCollapseLine(spendable);

            int result = PlayAll(SongKeys[1]);

            Assert.That(result, Is.EqualTo(SongBook.NoSong), "the swarm sends ten stings, and one wasp too few can be spent for them");
            Assert.That(_reasons, Is.EqualTo(new[] { SongRefusal.NotEnoughWasps }));
            Assert.That(_body.Spendable, Is.EqualTo(spendable), "nothing is paid");
        }

        // Changed with the first-cast rule: the Dome's first cast is free, so it is cast once and rested first.
        [Test]
        public void Press_CostWouldLeaveFewerThanTheCollapseLine_IsRefusedWithNotEnoughWasps()
        {
            UseUpTheFreeCast(0);
            LeaveAboveTheCollapseLine(39);

            int result = PlayAll(SongKeys[0]);

            Assert.That(result, Is.EqualTo(SongBook.NoSong));
            Assert.That(_cast, Is.Empty);
            Assert.That(_notReady, Is.EqualTo(new[] { 0 }));
            Assert.That(_reasons, Is.EqualTo(new[] { SongRefusal.NotEnoughWasps }));
            Assert.That(_body.Count, Is.EqualTo(_body.CollapseLine + 39), "nothing is paid");
            Assert.That(_body.IsCollapsed, Is.False);
            Assert.That(_caster.CooldownFraction(0), Is.EqualTo(0f), "a refused cast starts no cooldown");
            Assert.That(_forms.IsDomed, Is.False);
            Assert.That(_caster.RecentNoteCount, Is.EqualTo(0), "the notes are used up, as for every refusal");
        }

        // Changed with the first-cast rule, as above.
        [Test]
        public void Press_CostLeavesExactlyTheCollapseLine_CastsWithoutCollapsing()
        {
            UseUpTheFreeCast(0);
            LeaveAboveTheCollapseLine(40);

            int result = PlayAll(SongKeys[0]);

            Assert.That(result, Is.EqualTo(0));
            Assert.That(_body.Count, Is.EqualTo(_body.CollapseLine));
            Assert.That(_body.IsCollapsed, Is.False);
        }

        // Changed with the first-cast rule, as above.
        [Test]
        public void Press_RefusedForWasps_StartsNoCooldown_SoItCastsOnceTheWaspsAreBack()
        {
            UseUpTheFreeCast(0);
            LeaveAboveTheCollapseLine(39);
            PlayAll(SongKeys[0]);

            _body.Gain(1f);
            int result = PlayAll(SongKeys[0]);

            Assert.That(result, Is.EqualTo(0), "no cooldown to wait for");
            Assert.That(_body.Count, Is.EqualTo(_body.CollapseLine));
        }

        [Test]
        public void Press_TooFewWaspsInAFarmPhase_StillCasts()
        {
            LeaveAboveTheCollapseLine(0);
            _areSongsFree = true;

            Assert.That(PlayAll(SongKeys[6]), Is.EqualTo(6));
            Assert.That(_body.Count, Is.EqualTo(_body.CollapseLine));
        }

        [Test]
        public void Press_ASongThatCostsNothing_CastsAtTheCollapseLine()
        {
            LeaveAboveTheCollapseLine(0);

            Assert.That(PlayAll(SongKeys[4]), Is.EqualTo(4), "Hive Call is free");
        }

        [Test]
        public void Press_CoolingAndTooFewWasps_SaysCoolingDown()
        {
            PlayAll(SongKeys[0]);
            LeaveAboveTheCollapseLine(1);
            _reasons.Clear();

            PlayAll(SongKeys[0]);

            Assert.That(_reasons, Is.EqualTo(new[] { SongRefusal.CoolingDown }));
        }

        [Test]
        public void SongNotReady_SuccessfulCast_IsNotRaised()
        {
            PlayAll(SongKeys[0]);

            Assert.That(_notReady, Is.Empty);
            Assert.That(_cast, Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void SongNotReady_NotesThatMatchNothing_IsNotRaised()
        {
            Press(L);
            Press(L);
            Press(L);
            PlayAll(SongKeys[0]);
            _notReady.Clear();
            Press(L);
            Press(L);
            Press(L);

            Assert.That(_notReady, Is.Empty);
        }

        [Test]
        public void Press_WhileCollapsedWithNotesPlayed_AllocatesNothing()
        {
            _body.Lose(_body.Count);
            SwarmSongContext context = Context();

            Assert.That(() => { _caster.Press(L, in context); }, Is.Not.AllocatingGCMemory());
        }

        // Notes for the HUD

        [Test]
        public void Press_AnyNote_RaisesNotePlayedWithTheHole()
        {
            Press(L);
            Press(K);

            Assert.That(_played, Is.EqualTo(new[] { L, K }));
        }

        [Test]
        public void RecentNotes_Played_AreRememberedOldestFirst()
        {
            Press(L);
            Press(K);

            Assert.That(_caster.RecentNoteCount, Is.EqualTo(2));
            Assert.That(_caster.RecentNoteHole(0), Is.EqualTo(L));
            Assert.That(_caster.RecentNoteHole(1), Is.EqualTo(K));
        }

        [Test]
        public void RecentNoteAge01_AsTimePasses_RunsFromZeroToOneOverTheNoteWindow()
        {
            Press(L);
            Assert.That(_caster.RecentNoteAge01(0), Is.EqualTo(0f));

            _caster.Tick(1.5f);
            Press(K);

            Assert.That(_caster.RecentNoteAge01(0), Is.EqualTo(0.5f).Within(1e-4f), "1.5 s of a 3 s window");
            Assert.That(_caster.RecentNoteAge01(1), Is.EqualTo(0f));
        }

        [Test]
        public void RecentNotes_OlderThanTheWindow_AreForgotten()
        {
            Press(L);
            _caster.Tick(3f);

            Press(K);

            Assert.That(_caster.RecentNoteCount, Is.EqualTo(1));
            Assert.That(_caster.RecentNoteHole(0), Is.EqualTo(K));
        }

        [Test]
        public void RecentNotes_JustInsideTheWindow_AreKept()
        {
            Press(L);
            _caster.Tick(2.5f);

            Press(K);

            Assert.That(_caster.RecentNoteCount, Is.EqualTo(2));
        }

        [Test]
        public void RecentNotes_MoreThanTheFluteRemembers_DropTheOldest()
        {
            int[] notes = { L, L, K, L, K, L };

            foreach (int note in notes)
            {
                Press(note);
            }

            Assert.That(_caster.RecentNoteCount, Is.EqualTo(5));
            Assert.That(_caster.RecentNoteHole(0), Is.EqualTo(L));
            Assert.That(_caster.RecentNoteHole(1), Is.EqualTo(K));
            Assert.That(_caster.RecentNoteHole(4), Is.EqualTo(L));
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

            Assert.That(_played, Is.EqualTo(new[] { H, K, J }));
        }

        [Test]
        public void Tick_AndPressWithoutACast_AllocateNothing()
        {
            Press(L);
            SwarmSongContext context = Context();

            Assert.That(() => { _caster.Tick(0.016f); }, Is.Not.AllocatingGCMemory());
            Assert.That(() => { _caster.Press(L, in context); }, Is.Not.AllocatingGCMemory());
        }

        // What the swarm shows for a song: a slot song's form while its slot is active, the Gust while its pulse runs, otherwise the whole swarm's form.
        private SwarmForm Showing(int song)
        {
            if (_songs[song] is SlotSongEffect slotSong)
            {
                SlotState state = _forms.Slot(slotSong.Slot);
                return state.IsActive ? state.Form : SwarmForm.River;
            }

            if (song == GustIndex)
            {
                return _forms.GustSeconds >= 0f ? SwarmForm.Gust : SwarmForm.River;
            }

            return _forms.Current;
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
            return new SwarmSongContext(
                Vector3.zero, _aim, Vector3.zero, _body, _forms, _fury, _hives, _enemies, new FakeSmokeClearer(), null, _areSongsFree);
        }

        // Casts the song once, which is free, then lets its cooldown and its rule run out, and forgets what the test recorded.
        private void UseUpTheFreeCast(int song)
        {
            Assert.That(PlayAll(SongKeys[song]), Is.EqualTo(song), "the free cast");
            _caster.Tick(_songs[song].Song.CooldownSeconds + 0.01f);
            _forms.Tick(10f, _hives);
            _cast.Clear();
            _notReady.Clear();
            _reasons.Clear();
        }

        // Loses wasps until only the given number stand above the collapse line.
        private void LeaveAboveTheCollapseLine(int wasps)
        {
            _body.Lose(_body.Count - _body.CollapseLine - wasps);
            Assert.That(_body.Spendable, Is.EqualTo(wasps));
        }
    }
}
