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
    /// Plan sections A, B and H: the Hydra's heads each bite one of the beekeepers nearest the core, chosen at the cast by
    /// the ThreeNearest aim rule: 3 heads, 4 from level 4 and 5 from level 7. A spare head strikes the ground.
    /// </summary>
    public sealed class HydraSongTests
    {
        private const int HoleCount = 4;
        private const int HydraIndex = 7;
        private const int StingIndex = 1;
        private const float GridTolerance = 0.04f;
        private const int MostHeads = 5;

        // J J L.
        private static readonly int[] Keys = { 1, 1, 3 };

        private readonly List<Object> _assets = new List<Object>();
        private readonly List<ISwarmTarget> _enemies = new List<ISwarmTarget>();
        private readonly List<SwarmSongEffect> _songs = new List<SwarmSongEffect>();
        private readonly List<Vector3> _landings = new List<Vector3>();

        private SwarmConfig _swarmConfig;
        private FuryConfig _furyConfig;
        private SongConfig _songConfig;
        private SquadConfig _squads;
        private SwarmLevelConfig _levels;
        private SwarmBody _body;
        private FuryMeter _fury;
        private SwarmFormMachine _forms;
        private HiveSet _hives;
        private HydraSong _song;
        private SwarmSongCaster _caster;
        private FakeSongAim _chooser;
        private ISwarmTarget _aim;
        private Vector3 _aimPoint;
        private Vector3 _core;
        private int _level;

        [SetUp]
        public void SetUp()
        {
            _swarmConfig = Make<SwarmConfig>();
            _furyConfig = Make<FuryConfig>();
            _songConfig = Make<SongConfig>();
            _squads = Make<SquadConfig>();
            _levels = Make<SwarmLevelConfig>();
            HiveConfig hiveConfig = Make<HiveConfig>();
            _body = new SwarmBody(_swarmConfig);
            _fury = new FuryMeter(_furyConfig);
            _forms = new SwarmFormMachine(_body, _fury, _squads);
            _hives = new HiveSet(new List<Hive> { new Hive(hiveConfig, new Vector3(-10f, 0f, 0f)), new Hive(hiveConfig, new Vector3(10f, 0f, 0f)) });
            _song = Make<HydraSong>();
            _songs.Clear();
            _songs.Add(Make<DomeSong>());
            _songs.Add(Make<StingSong>());
            _songs.Add(Make<TornadoSong>());
            _songs.Add(Make<GustSong>());
            _songs.Add(Make<HiveCallSong>());
            _songs.Add(Make<ColossusSong>());
            _songs.Add(Make<MeteorSong>());
            _songs.Add(_song);
            _songs.Add(Make<ScytheSong>());
            _songs.Add(Make<LanceSong>());
            _caster = new SwarmSongCaster(_songs, _songConfig, HoleCount);
            _enemies.Clear();
            _landings.Clear();
            _chooser = null;
            _aim = null;
            _aimPoint = new Vector3(0f, 0f, 6f);
            _core = Vector3.zero;
            _level = 1;
            _forms.Landed += (form, point) =>
            {
                if (form == SwarmForm.Hydra)
                {
                    _landings.Add(point);
                }
            };
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

        // ---- The song's row ----

        [Test]
        public void Defaults_MatchThePlan()
        {
            Assert.That(_song.Song.Title, Is.EqualTo("Hydra"));
            Assert.That(_song.Song.CooldownSeconds, Is.EqualTo(6f));
            Assert.That(_song.Song.Keys, Is.EqualTo(Keys));
            Assert.That(_song.Category, Is.EqualTo(SongCategory.Attack));
            Assert.That(_song.WaspCost, Is.EqualTo(60));
            Assert.That(_song.Slot, Is.EqualTo(SongSlot.Strike));
            Assert.That(_song.Form, Is.EqualTo(SwarmForm.Hydra));
            Assert.That(_song.Share, Is.EqualTo(0.37f));
            Assert.That(_song.Reach, Is.EqualTo(9f));
            Assert.That(_song.BiteSeconds, Is.EqualTo(0.5f), "plan section O: was 0.7");
            Assert.That(_song.Damage, Is.EqualTo(40f));
            Assert.That(_song.Seconds, Is.EqualTo(1.4f), "the 0.9 s after the bite kept, so it was 1.6");
            Assert.That(_song.Heads, Is.EqualTo(3));
            Assert.That(_song.ExtraHeadFromLevel, Is.EqualTo(4));
            Assert.That(_song.SecondExtraHeadFromLevel, Is.EqualTo(7));
            Assert.That(_song.SpareFanDegrees, Is.EqualTo(30f));
            Assert.That(_song.IsAimed, Is.True);
            Assert.That(_song.AimRule, Is.EqualTo(AimRule.ThreeNearest));
            Assert.That(_song.AimRange, Is.EqualTo(9f));
            Assert.That(_song.CostsInFarmPhase, Is.False, "an attack is no tower");
        }

        [TestCase(1, 3)]
        [TestCase(3, 3)]
        [TestCase(4, 4)]
        [TestCase(6, 4)]
        [TestCase(7, 5)]
        [TestCase(9, 5)]
        public void HeadCount_FollowsTheLevel(int level, int heads)
        {
            Assert.That(_song.HeadCount(level), Is.EqualTo(heads));
        }

        [Test]
        public void Keys_PlayedFromSilence_CastExactlyTheHydra()
        {
            var cast = new List<int>();
            _caster.SongCast += cast.Add;

            int result = PlayAll();

            Assert.That(result, Is.EqualTo(HydraIndex));
            Assert.That(cast, Is.EqualTo(new[] { HydraIndex }));
            Assert.That(Strike().Form, Is.EqualTo(SwarmForm.Hydra));
        }

        [Test]
        public void Cast_RunsInTheStrikeSlotWithThirtySevenPercentOfTheSwarm()
        {
            Cast();

            SlotState strike = Strike();
            Assert.That(strike.IsActive, Is.True);
            Assert.That(strike.Form, Is.EqualTo(SwarmForm.Hydra));
            Assert.That(strike.Category, Is.EqualTo(SongCategory.Attack));
            Assert.That(strike.Low, Is.EqualTo(0.55f).Within(1e-5f));
            Assert.That(strike.High, Is.EqualTo(0.92f).Within(1e-5f), "37% of the swarm from the start of the Strike band");
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River), "the rest of the swarm stays the river");
        }

        // ---- Choosing the targets ----

        [TestCase(1, 3)]
        [TestCase(4, 4)]
        [TestCase(7, 5)]
        public void Cast_AsksTheAimChooserForOneBeekeeperAHeadWithinItsGrownReach(int level, int heads)
        {
            _chooser = new FakeSongAim();
            _level = level;

            Cast();

            Assert.That(_chooser.NearestCalls, Is.EqualTo(1));
            Assert.That(_chooser.LastNearestCount, Is.EqualTo(heads));
            Assert.That(_chooser.LastRange, Is.EqualTo(9f * _levels.SizeScale(level)).Within(1e-4f));
            Assert.That(_chooser.Calls, Is.EqualTo(0), "the Hydra asks for several beekeepers, never for one");
        }

        [TestCase(1, 3)]
        [TestCase(4, 4)]
        [TestCase(7, 5)]
        public void Bite_EachHeadBitesOneOfTheChosenBeekeepers_AsManyAsItHasHeads(int level, int heads)
        {
            FakeSwarmTarget[] keepers = ChooserWith(
                new Vector3(2f, 0f, 0f), new Vector3(0f, 0f, 3f), new Vector3(-4f, 0f, 0f), new Vector3(0f, 0f, -5f), new Vector3(6f, 0f, 1f));
            _level = level;
            Cast();

            _forms.Tick(0.71f, _hives);

            float damage = 40f * _levels.DamageScale(level);
            for (int keeper = 0; keeper < keepers.Length; keeper++)
            {
                float expected = keeper < heads ? damage : 0f;
                Assert.That(keepers[keeper].DamageTaken, Is.EqualTo(expected).Within(1e-3f), "beekeeper " + keeper);
            }

            Assert.That(PictureHeadCount(), Is.EqualTo(heads), "the picture counts the heads that are not 0");
        }

        [Test]
        public void Bite_WithoutAnAimChooser_TheFirstHeadBitesTheAimedBeekeeperInReach()
        {
            var aimed = new FakeSwarmTarget { Position = new Vector3(5f, 0f, 5f) };
            _aim = aimed;
            Cast();

            _forms.Tick(0.71f, _hives);

            Assert.That(aimed.DamageTaken, Is.EqualTo(40f));
            Assert.That(Bites(Head(0)), Is.True);
            Assert.That(Bites(Head(1)), Is.False);
            Assert.That(Bites(Head(2)), Is.False);
        }

        [Test]
        public void Bite_WithoutAnAimChooser_AnAimedBeekeeperBeyondItsReachIsNotBitten()
        {
            var far = new FakeSwarmTarget { Position = new Vector3(0f, 0f, 9.1f) };
            _aim = far;
            Cast();

            _forms.Tick(0.71f, _hives);

            Assert.That(far.DamageTaken, Is.EqualTo(0f), "9.1 m is beyond 9 m");
            Assert.That(Bites(Head(0)), Is.False);
        }

        // ---- The bite ----

        [Test]
        public void Tick_BeforeTheBite_HurtsNobodyAndRaisesNothing()
        {
            FakeSwarmTarget[] keepers = ChooserWith(new Vector3(2f, 0f, 0f), new Vector3(0f, 0f, 3f), new Vector3(-4f, 0f, 0f));
            Cast();

            _forms.Tick(_song.BiteSeconds - 0.01f, _hives);

            foreach (FakeSwarmTarget keeper in keepers)
            {
                Assert.That(keeper.DamageTaken, Is.EqualTo(0f));
            }

            Assert.That(_fury.Value, Is.EqualTo(0f));
            Assert.That(_landings, Is.Empty);
            Assert.That(Strike().ReleasedSeconds, Is.EqualTo(-1f));
        }

        [Test]
        public void Tick_AtTheBite_EachHeadBitesOnceAndFillsFury()
        {
            FakeSwarmTarget[] keepers = ChooserWith(new Vector3(2f, 0f, 0f), new Vector3(0f, 0f, 3f), new Vector3(-4f, 0f, 0f));
            Cast();

            _forms.Tick(_song.BiteSeconds - 0.01f, _hives);
            _forms.Tick(0.02f, _hives);
            _forms.Tick(0.5f, _hives);

            foreach (FakeSwarmTarget keeper in keepers)
            {
                Assert.That(keeper.DamageTaken, Is.EqualTo(40f), "one bite each");
            }

            Assert.That(_fury.Value, Is.EqualTo(120f / _furyConfig.DamagePerFuryPoint).Within(1e-3f));
        }

        [Test]
        public void Tick_OneBigStepOverTheWholeSong_BitesOnceAndLetsGo()
        {
            FakeSwarmTarget[] keepers = ChooserWith(new Vector3(2f, 0f, 0f));
            Cast();

            _forms.Tick(5f, _hives);

            Assert.That(keepers[0].DamageTaken, Is.EqualTo(40f));
            Assert.That(_landings.Count, Is.EqualTo(3), "every head lands once");
            Assert.That(Strike().IsActive, Is.False);
        }

        [Test]
        public void Tick_ATargetThatDiedBeforeTheBite_ItsHeadHurtsNobody()
        {
            FakeSwarmTarget[] keepers = ChooserWith(new Vector3(2f, 0f, 0f), new Vector3(0f, 0f, 3f), new Vector3(-4f, 0f, 0f));
            Cast();
            _forms.Tick(0.4f, _hives);

            keepers[1].IsAlive = false;
            _forms.Tick(0.31f, _hives);

            Assert.That(keepers[0].DamageTaken, Is.EqualTo(40f));
            Assert.That(keepers[1].DamageTaken, Is.EqualTo(0f));
            Assert.That(keepers[2].DamageTaken, Is.EqualTo(40f));
            Assert.That(_fury.Value, Is.EqualTo(80f / _furyConfig.DamagePerFuryPoint).Within(1e-3f));
        }

        [Test]
        public void Tick_ASpareHead_StrikesTheGroundAndHurtsNobody()
        {
            FakeSwarmTarget[] keepers = ChooserWith(new Vector3(0f, 0f, 6f));
            _aimPoint = new Vector3(0f, 0f, 6f);
            Cast();
            Vector2 spare = HeadPoint(Head(1));
            var bystander = new FakeSwarmTarget { Position = new Vector3(spare.x, 0f, spare.y) };
            _enemies.Add(bystander);

            _forms.Tick(0.71f, _hives);

            Assert.That(keepers[0].DamageTaken, Is.EqualTo(40f));
            Assert.That(bystander.DamageTaken, Is.EqualTo(0f), "a spare head bites nobody, even where it strikes");
            Assert.That(_landings.Count, Is.EqualTo(3), "the spare heads land too");
        }

        [Test]
        public void Tick_AtTheBite_RaisesHydraLandedForEachHeadAtItsPoint()
        {
            ChooserWith(new Vector3(2f, 0f, 0f), new Vector3(0f, 0f, 3f), new Vector3(-4f, 0f, 0f));
            Cast();

            _forms.Tick(0.71f, _hives);

            Assert.That(_landings.Count, Is.EqualTo(3));
            Assert.That(_landings[0].x, Is.EqualTo(2f).Within(1e-3f));
            Assert.That(_landings[1].z, Is.EqualTo(3f).Within(1e-3f));
            Assert.That(_landings[2].x, Is.EqualTo(-4f).Within(1e-3f));
        }

        [Test]
        public void Bite_AHigherLevel_HitsHarder_AndAThinSwarmHitsAsHard()
        {
            FakeSwarmTarget[] keepers = ChooserWith(new Vector3(2f, 0f, 0f));
            _level = 3;
            _body.Lose(_body.Count - _body.CollapseLine - 1);
            Cast();

            _forms.Tick(0.71f, _hives);

            Assert.That(keepers[0].DamageTaken, Is.EqualTo(40f * 1.24f).Within(1e-3f));
        }

        // ---- The heads' points, for the picture ----

        [Test]
        public void Heads_FollowTheirBeekeepersUntilTheBite_ThenStayWhereTheyBit()
        {
            FakeSwarmTarget[] keepers = ChooserWith(new Vector3(3f, 0f, 0f));
            Cast();
            Assert.That(HeadPoint(Head(0)).x, Is.EqualTo(3f).Within(GridTolerance));

            keepers[0].Position = new Vector3(4f, 0f, 1f);
            _forms.Tick(0.4f, _hives);
            Assert.That(HeadPoint(Head(0)).x, Is.EqualTo(4f).Within(GridTolerance));
            Assert.That(HeadPoint(Head(0)).y, Is.EqualTo(1f).Within(GridTolerance));

            _forms.Tick(0.31f, _hives);
            keepers[0].Position = new Vector3(7f, 0f, 2f);
            _forms.Tick(0.2f, _hives);

            Assert.That(HeadPoint(Head(0)).x, Is.EqualTo(4f).Within(GridTolerance), "it bit at 4 m and stays there");
            Assert.That(HeadPoint(Head(0)).y, Is.EqualTo(1f).Within(GridTolerance));
        }

        [Test]
        public void Heads_ABeekeeperThatWalksAwayBeforeTheBite_IsStillBitten()
        {
            FakeSwarmTarget[] keepers = ChooserWith(new Vector3(8f, 0f, 0f));
            Cast();
            _forms.Tick(0.5f, _hives);

            keepers[0].Position = new Vector3(9.5f, 0f, 0f);
            _forms.Tick(0.21f, _hives);

            Assert.That(keepers[0].DamageTaken, Is.EqualTo(40f), "the head follows the beekeeper it chose at the cast");
        }

        [Test]
        public void Params_HoldEveryHeadsPointAndWhetherItBites_TheBiteAndTheSize()
        {
            _core = new Vector3(1f, 0.25f, 0f);
            ChooserWith(new Vector3(2.5f, 0f, -3f), new Vector3(-6f, 0f, 4.25f), new Vector3(30f, 0f, -40f));

            Cast();

            Assert.That(HeadPoint(Head(0)), Is.EqualTo(new Vector2(2.5f, -3f)));
            Assert.That(HeadPoint(Head(1)), Is.EqualTo(new Vector2(-6f, 4.25f)));
            Assert.That(HeadPoint(Head(2)), Is.EqualTo(new Vector2(30f, -40f)));
            Assert.That(Bites(Head(0)) && Bites(Head(1)) && Bites(Head(2)), Is.True);
            Assert.That(Head(3), Is.EqualTo(0f), "a fourth head's place is empty with three heads");
            Assert.That(Head(4), Is.EqualTo(0f));
            Assert.That(PictureHeadCount(), Is.EqualTo(3));
            Assert.That(ParamB().y, Is.EqualTo(0.5f), "the bite's seconds");
            Assert.That(ParamB().z, Is.EqualTo(1f), "the picture's size at the first level");
            Assert.That(ParamB().w, Is.EqualTo(0f), "w is the view's");
        }

        // The picture snaps the heads onto their targets at the bite it is sent, so a tuned bite must move the hurt with
        // it: an impact hurts on the frame it visibly lands.
        [Test]
        public void Params_HoldTheTunedBite_AndTheHeadsHurtAtIt()
        {
            JsonUtility.FromJsonOverwrite("{\"<BiteSeconds>k__BackingField\":0.9}", _song);
            FakeSwarmTarget[] keepers = ChooserWith(new Vector3(2f, 0f, 0f));
            Cast();

            Assert.That(ParamB().y, Is.EqualTo(0.9f), "the picture's heads land at the tuned bite");

            _forms.Tick(0.89f, _hives);
            Assert.That(keepers[0].DamageTaken, Is.EqualTo(0f));

            _forms.Tick(0.02f, _hives);
            Assert.That(keepers[0].DamageTaken, Is.EqualTo(40f));
        }

        // A spare head pulled onto the grid's very corner would pack to 0, which the picture reads as no head.
        [Test]
        public void Params_ASpareHeadOnTheGridsCorner_IsNotZero_SoThePictureStillCountsIt()
        {
            _core = new Vector3(-60f, 0f, -60f);
            _chooser = new FakeSongAim { Point = new Vector3(-80f, 0f, -80f) };

            Cast();

            Assert.That(Head(0), Is.GreaterThan(0f));
            Assert.That(HeadPoint(Head(0)).x, Is.EqualTo(-64f).Within(0.07f), "moved at most one step of the grid");
            Assert.That(HeadPoint(Head(0)).y, Is.EqualTo(-64f).Within(0.07f));
            Assert.That(PictureHeadCount(), Is.EqualTo(3));
        }

        // Like the Meteor's height, the picture grows with the level the swarm had at the cast: the necks rear higher and
        // the splashes spread wider, as the reach does.
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(9)]
        public void Params_HoldTheSizeScaleOfTheLevelAtTheCast(int level)
        {
            _level = level;

            Cast();

            Assert.That(ParamB().z, Is.EqualTo(_levels.SizeScale(level)).Within(1e-5f));
        }

        [Test]
        public void Params_EachHeadIsAWholeNumberAFloatHoldsExactly()
        {
            ChooserWith(new Vector3(63.9f, 0f, 63.9f), new Vector3(-64f, 0f, -64f), new Vector3(0.03f, 0f, -0.03f));

            Cast();

            for (int head = 0; head < 3; head++)
            {
                float packed = Head(head);
                Assert.That(packed, Is.EqualTo(Mathf.Round(packed)), "head " + head);
                Assert.That(packed, Is.LessThan(1 << 23), "head " + head);
                Assert.That(packed, Is.GreaterThanOrEqualTo(0f), "head " + head);
            }

            Assert.That(HeadPoint(Head(0)).x, Is.EqualTo(63.9f).Within(GridTolerance));
            Assert.That(HeadPoint(Head(1)), Is.EqualTo(new Vector2(-64f, -64f)));
            Assert.That(HeadPoint(Head(2)).x, Is.EqualTo(0f).Within(GridTolerance));
        }

        [Test]
        public void SpareHeads_WithNobodyChosen_FanOutAroundTheAimAtThirtyDegrees()
        {
            _chooser = new FakeSongAim { Point = new Vector3(0f, 0f, 6f) };

            Cast();

            AssertHead(0, new Vector2(0f, 6f), false);
            AssertHead(1, new Vector2(-3f, 5.196f), false);
            AssertHead(2, new Vector2(3f, 5.196f), false);
        }

        [Test]
        public void SpareHeads_BesideAChosenBeekeeper_TakeTheFanEitherSideOfTheAim()
        {
            ChooserWith(new Vector3(0f, 0f, 6f));
            _chooser.Point = new Vector3(0f, 0f, 6f);

            Cast();

            AssertHead(0, new Vector2(0f, 6f), true);
            AssertHead(1, new Vector2(-3f, 5.196f), false);
            AssertHead(2, new Vector2(3f, 5.196f), false);
        }

        [Test]
        public void SpareHeads_AnAimBeyondTheReach_StrikeAtTheReach()
        {
            _chooser = new FakeSongAim { Point = new Vector3(2f, 0f, 20f) };
            _core = new Vector3(2f, 0f, 0f);

            Cast();

            AssertHead(0, new Vector2(2f, 9f), false);
        }

        // ---- How long it lasts, and what ends it ----

        // Changed with plan section O: it bites at 0.5 s and lasts 1.4 s (was 0.7 s and 1.6 s); what it proves stays.
        [Test]
        public void Tick_ItLastsOnePointFourSeconds_ThenItsReturnBeat()
        {
            Cast();

            _forms.Tick(1.39f, _hives);
            Assert.That(Strike().ReleasedSeconds, Is.EqualTo(-1f), "still running");

            _forms.Tick(0.02f, _hives);
            Assert.That(Strike().ReleasedSeconds, Is.GreaterThanOrEqualTo(0f), "let go at 1.4 s");
            Assert.That(Strike().IsActive, Is.True, "the return beat");

            _forms.Tick(_squads.ReturnSeconds, _hives);
            Assert.That(Strike().IsActive, Is.False);
        }

        [Test]
        public void Cast_ReplacesTheRunningStrikeSong_AndKeepsTheGuardAndPostSongs()
        {
            var stung = new FakeSwarmTarget { Position = new Vector3(2f, 0f, 0f) };
            var post = Make<FakeSlotSong>();
            SwarmSongContext plain = Context();
            Make<DomeSong>().Cast(in plain);
            post.Cast(in plain);
            SwarmSongContext atStung = Context().AimedAt(stung, stung.Position);
            _songs[StingIndex].Cast(in atStung);

            Cast();
            _forms.Tick(1f, _hives);

            Assert.That(Strike().Form, Is.EqualTo(SwarmForm.Hydra));
            Assert.That(stung.DamageTaken, Is.EqualTo(0f), "the Sting ended before it arrived");
            Assert.That(_forms.Slot(SongSlot.Guard).IsActive, Is.True, "the Dome stands");
            Assert.That(_forms.Slot(SongSlot.Post).IsActive, Is.True, "the Post song stays");
        }

        [Test]
        public void Collapse_BeforeTheBite_EndsItWithNoDamage()
        {
            FakeSwarmTarget[] keepers = ChooserWith(new Vector3(2f, 0f, 0f));
            Cast();
            _forms.Tick(_song.BiteSeconds - 0.1f, _hives);

            _body.Lose(_body.Count);
            _forms.Tick(1f, _hives);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
            Assert.That(keepers[0].DamageTaken, Is.EqualTo(0f));
            Assert.That(_landings, Is.Empty);
            Assert.That(Strike().IsActive, Is.False);
        }

        [Test]
        public void Tick_ThroughTheWholeSong_AllocatesNothing()
        {
            ChooserWith(new Vector3(2f, 0f, 0f), new Vector3(0f, 0f, 3f));
            _landings.Capacity = 8;
            Cast();

            Assert.That(
                () =>
                {
                    _forms.Tick(0.4f, _hives);
                    _forms.Tick(0.4f, _hives);
                    _forms.Tick(1.2f, _hives);
                },
                Is.Not.AllocatingGCMemory());
            Assert.That(_landings.Count, Is.EqualTo(3), "it bit while measured");
        }

        // ---- Through the caster ----

        [Test]
        public void Press_WithNothingToHit_StillCasts()
        {
            var cast = new List<int>();
            _caster.SongCast += cast.Add;

            PlayAll();
            _forms.Tick(2f, _hives);

            Assert.That(cast, Is.EqualTo(new[] { HydraIndex }));
            Assert.That(_landings.Count, Is.EqualTo(3), "three heads strike the ground");
        }

        [Test]
        public void Press_WhileOnCooldown_CannotBeRecastUntilSixSecondsPass()
        {
            PlayAll();
            _caster.Tick(5.9f);

            Assert.That(PlayAll(), Is.EqualTo(SongBook.NoSong));

            _caster.Tick(0.11f);

            Assert.That(PlayAll(), Is.EqualTo(HydraIndex));
        }

        [Test]
        public void Press_InASurvivePhase_TheFirstCastIsFreeAndTheSecondCostsSixty()
        {
            int start = _body.Count;

            PlayAll();
            Assert.That(_body.Count, Is.EqualTo(start), "finding a song is never punished");

            _caster.Tick(6.01f);
            PlayAll();
            Assert.That(_body.Count, Is.EqualTo(start - 60));
        }

        // ---- The level the context carries ----

        [Test]
        public void Context_WithoutALevel_IsTheFirstLevel()
        {
            var context = new SwarmSongContext(default, null, default, _body, _forms, _fury, _hives, _enemies, null);
            var scaled = new SwarmSongContext(default, null, default, _body, _forms, _fury, _hives, _enemies, null, null, false, 1.5f, 1.5f);

            Assert.That(context.Level, Is.EqualTo(1));
            Assert.That(scaled.Level, Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(-2)]
        public void Context_ALevelBelowOne_Throws(int level)
        {
            Assert.That(
                () => new SwarmSongContext(default, null, default, _body, _forms, _fury, _hives, _enemies, null, null, false, 1f, 1f, level),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void Context_AimedAt_KeepsTheLevel()
        {
            _level = 7;

            SwarmSongContext aimed = Context().AimedAt(null, Vector3.one);

            Assert.That(aimed.Level, Is.EqualTo(7));
        }

        // ---- Helpers ----

        // HydraRule packs each head into one whole number: 2^22 when the head bites a beekeeper, plus its point on the
        // ground on a 1/16 m grid that starts 64 m below the origin on each axis, x times 2048 plus z.
        private static Vector2 HeadPoint(float packed)
        {
            int value = Mathf.RoundToInt(packed);
            int x = (value >> 11) & 2047;
            int z = value & 2047;
            return new Vector2((x / 16f) - 64f, (z / 16f) - 64f);
        }

        private static bool Bites(float packed)
        {
            return Mathf.RoundToInt(packed) >= (1 << 22);
        }

        private void AssertHead(int head, Vector2 point, bool bites)
        {
            Vector2 actual = HeadPoint(Head(head));
            Assert.That(actual.x, Is.EqualTo(point.x).Within(GridTolerance), "head " + head + " x");
            Assert.That(actual.y, Is.EqualTo(point.y).Within(GridTolerance), "head " + head + " z");
            Assert.That(Bites(Head(head)), Is.EqualTo(bites), "head " + head + " bites");
        }

        // Heads 1 to 4 are ParamA's x, y, z and w, and head 5 is ParamB's x.
        private float Head(int head)
        {
            Vector4 a = Strike().ParamA;
            switch (head)
            {
                case 0:
                    return a.x;
                case 1:
                    return a.y;
                case 2:
                    return a.z;
                case 3:
                    return a.w;
                default:
                    return ParamB().x;
            }
        }

        private Vector4 ParamB() => Strike().ParamB;

        // WaspSwarm.compute counts the heads by the packed ones that are not 0.
        private int PictureHeadCount()
        {
            int count = 0;
            for (int head = 0; head < MostHeads; head++)
            {
                if (Head(head) != 0f)
                {
                    count++;
                }
            }

            return count;
        }

        private FakeSwarmTarget[] ChooserWith(params Vector3[] positions)
        {
            var keepers = new FakeSwarmTarget[positions.Length];
            for (int index = 0; index < positions.Length; index++)
            {
                keepers[index] = new FakeSwarmTarget { Position = positions[index] };
                _enemies.Add(keepers[index]);
            }

            _chooser = new FakeSongAim { Nearest = keepers, Point = positions.Length > 0 ? positions[0] : _aimPoint };
            return keepers;
        }

        private SlotState Strike() => _forms.Slot(SongSlot.Strike);

        private void Cast()
        {
            SwarmSongContext context = Context();
            _song.Cast(in context);
        }

        private int PlayAll()
        {
            int result = SongBook.NoSong;
            SwarmSongContext context = Context();

            foreach (int key in Keys)
            {
                result = _caster.Press(key, in context);
            }

            return result;
        }

        private SwarmSongContext Context()
        {
            return new SwarmSongContext(
                _core, _aim, _aimPoint, _body, _forms, _fury, _hives, _enemies, null, _chooser, false,
                _levels.SizeScale(_level), _levels.DamageScale(_level), _level);
        }

        private T Make<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }
    }
}
