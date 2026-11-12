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
    /// Plan sections A, B and E: the Lance draws back for 0.4 s, aimed by the Line rule, then at 0.5 s a line 14 m long and
    /// 1.2 m wide from the core hurts every beekeeper on it and pushes each one out to the side it is on.
    /// </summary>
    public sealed class LanceSongTests
    {
        private const int HoleCount = 4;
        private const int LanceIndex = 9;
        private const int StingIndex = 1;

        // K L L H.
        private static readonly int[] Keys = { 2, 3, 3, 0 };

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
        private LanceSong _song;
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
            _song = Make<LanceSong>();
            _songs.Clear();
            _songs.Add(Make<DomeSong>());
            _songs.Add(Make<StingSong>());
            _songs.Add(Make<TornadoSong>());
            _songs.Add(Make<GustSong>());
            _songs.Add(Make<HiveCallSong>());
            _songs.Add(Make<ColossusSong>());
            _songs.Add(Make<MeteorSong>());
            _songs.Add(Make<HydraSong>());
            _songs.Add(Make<ScytheSong>());
            _songs.Add(_song);
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
                if (form == SwarmForm.Lance)
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
            Assert.That(_song.Song.Title, Is.EqualTo("Lance"));
            Assert.That(_song.Song.CooldownSeconds, Is.EqualTo(8f));
            Assert.That(_song.Song.Keys, Is.EqualTo(Keys));
            Assert.That(_song.Category, Is.EqualTo(SongCategory.Attack));
            Assert.That(_song.WaspCost, Is.EqualTo(70));
            Assert.That(_song.Slot, Is.EqualTo(SongSlot.Strike));
            Assert.That(_song.Form, Is.EqualTo(SwarmForm.Lance));
            Assert.That(_song.Share, Is.EqualTo(0.37f));
            Assert.That(_song.Length, Is.EqualTo(14f));
            Assert.That(_song.Width, Is.EqualTo(1.2f));
            Assert.That(_song.DrawSeconds, Is.EqualTo(0.25f), "plan section O: it fires at 0.35 s, was 0.5 s");
            Assert.That(_song.HitSeconds, Is.EqualTo(0.35f).Within(1e-5f));
            Assert.That(_song.Damage, Is.EqualTo(60f));
            Assert.That(_song.PushSpeed, Is.EqualTo(5f));
            Assert.That(_song.Seconds, Is.EqualTo(1.15f), "the 0.8 s after the hit kept, so it was 1.3");
            Assert.That(_song.TailLagSeconds, Is.EqualTo(0.08f));
            Assert.That(_song.IsAimed, Is.True);
            Assert.That(_song.AimRule, Is.EqualTo(AimRule.Line));
            Assert.That(_song.AimRange, Is.EqualTo(14f));
            Assert.That(_song.CostsInFarmPhase, Is.False, "an attack is no tower");
        }

        [Test]
        public void Keys_PlayedFromSilence_CastExactlyTheLance()
        {
            var cast = new List<int>();
            _caster.SongCast += cast.Add;

            int result = PlayAll();

            Assert.That(result, Is.EqualTo(LanceIndex));
            Assert.That(cast, Is.EqualTo(new[] { LanceIndex }));
            Assert.That(Strike().Form, Is.EqualTo(SwarmForm.Lance));
        }

        [Test]
        public void Cast_RunsInTheStrikeSlotWithThirtySevenPercentOfTheSwarm()
        {
            Cast();

            SlotState strike = Strike();
            Assert.That(strike.IsActive, Is.True);
            Assert.That(strike.Form, Is.EqualTo(SwarmForm.Lance));
            Assert.That(strike.Category, Is.EqualTo(SongCategory.Attack));
            Assert.That(strike.Low, Is.EqualTo(0.55f).Within(1e-5f));
            Assert.That(strike.High, Is.EqualTo(0.92f).Within(1e-5f));
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        // ---- The thrust ----

        [Test]
        public void Tick_BeforeTheThrust_HurtsNobodyAndRaisesNothing()
        {
            FakeSwarmTarget onIt = Add(0f, 5f);
            Cast();

            _forms.Tick(_song.HitSeconds - 0.01f, _hives);

            Assert.That(onIt.DamageTaken, Is.EqualTo(0f));
            Assert.That(onIt.KnockbackCount, Is.EqualTo(0));
            Assert.That(_fury.Value, Is.EqualTo(0f));
            Assert.That(_landings, Is.Empty);
        }

        [Test]
        public void Tick_AtTheThrust_HurtsEveryLivingBeekeeperOnTheLine()
        {
            FakeSwarmTarget onIt = Add(0f, 5f);
            FakeSwarmTarget atTheEnd = Add(0f, 14f);
            FakeSwarmTarget justBeyond = Add(0f, 14.05f);
            FakeSwarmTarget onTheEdge = Add(0.6f, 7f);
            FakeSwarmTarget justOutside = Add(0.65f, 7f);
            FakeSwarmTarget behind = Add(0f, -1f);
            var high = new FakeSwarmTarget { Position = new Vector3(0.2f, 4f, 9f) };
            var dead = new FakeSwarmTarget { Position = new Vector3(0f, 0f, 3f), IsAlive = false };
            _enemies.Add(high);
            _enemies.Add(dead);
            Cast();

            _forms.Tick(0.51f, _hives);

            Assert.That(onIt.DamageTaken, Is.EqualTo(60f));
            Assert.That(atTheEnd.DamageTaken, Is.EqualTo(60f), "14 m counts");
            Assert.That(justBeyond.DamageTaken, Is.EqualTo(0f));
            Assert.That(onTheEdge.DamageTaken, Is.EqualTo(60f), "0.6 m to the side is the strip's edge");
            Assert.That(justOutside.DamageTaken, Is.EqualTo(0f));
            Assert.That(behind.DamageTaken, Is.EqualTo(0f), "the line starts at the core");
            Assert.That(high.DamageTaken, Is.EqualTo(60f), "height is ignored");
            Assert.That(dead.DamageTaken, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_AtTheThrust_PushesEachOneOutToTheSideItIsOnAtFiveMetresASecond()
        {
            FakeSwarmTarget right = Add(0.3f, 5f);
            FakeSwarmTarget left = Add(-0.3f, 8f);
            FakeSwarmTarget onTheMiddle = Add(0f, 10f);
            FakeSwarmTarget offTheLine = Add(3f, 5f);
            Cast();

            _forms.Tick(0.51f, _hives);

            AssertPush(right, new Vector3(5f, 0f, 0f));
            AssertPush(left, new Vector3(-5f, 0f, 0f));
            AssertPush(onTheMiddle, new Vector3(-5f, 0f, 0f), "one on the middle of the line goes to its left");
            Assert.That(offTheLine.KnockbackCount, Is.EqualTo(0));
        }

        [Test]
        public void Tick_AtTheThrust_FillsFuryFromTheTotalDamage()
        {
            Add(0f, 5f);
            Add(0.2f, 9f);
            Add(4f, 4f);
            Cast();

            _forms.Tick(0.51f, _hives);

            Assert.That(_fury.Value, Is.EqualTo(120f / _furyConfig.DamagePerFuryPoint).Within(1e-3f));
        }

        [Test]
        public void Tick_ThrustsOnlyOnce()
        {
            FakeSwarmTarget onIt = Add(0f, 5f);
            Cast();

            _forms.Tick(0.51f, _hives);
            _forms.Tick(0.3f, _hives);
            _forms.Tick(0.3f, _hives);

            Assert.That(onIt.DamageTaken, Is.EqualTo(60f));
            Assert.That(onIt.KnockbackCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_AtTheThrust_RaisesLanceLandedOnceAtTheSpearsTip()
        {
            Cast();

            _forms.Tick(0.51f, _hives);
            _forms.Tick(0.5f, _hives);

            Assert.That(_landings.Count, Is.EqualTo(1));
            Assert.That(_landings[0].x, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(_landings[0].z, Is.EqualTo(14f).Within(1e-4f));
        }

        // ---- The draw: it follows, then locks ----

        // Changed with plan section O: the draw ends at 0.25 s and the hit lands at 0.35 s (was 0.4 s and 0.5 s), so the steps
        // follow the song's own times; what it proves stays.
        [Test]
        public void Draw_FollowsTheAimedBeekeeperUntilTheRelease_ThenTheLineIsLocked()
        {
            FakeSwarmTarget aimed = Add(0f, 8f);
            _aim = aimed;
            Cast();
            Assert.That(Strike().ParamA, Is.EqualTo(new Vector4(0f, 0f, 0f, 1f)));

            aimed.Position = new Vector3(2f, 0f, 8f);
            _forms.Tick(0.2f, _hives);
            Vector4 drawing = Strike().ParamA;
            Assert.That(drawing.z, Is.EqualTo(2f / Mathf.Sqrt(68f)).Within(1e-4f));
            Assert.That(drawing.w, Is.EqualTo(8f / Mathf.Sqrt(68f)).Within(1e-4f));

            _forms.Tick(_song.DrawSeconds - 0.2f + 0.01f, _hives);
            FakeSwarmTarget leftBehind = Add(2f, 8f);
            aimed.Position = new Vector3(-3f, 0f, 8f);
            _forms.Tick(0.04f, _hives);
            Assert.That(Strike().ParamA, Is.EqualTo(drawing), "locked at the release");
            Assert.That(leftBehind.DamageTaken, Is.EqualTo(0f), "not yet thrown home");

            _forms.Tick(0.06f, _hives);

            Assert.That(leftBehind.DamageTaken, Is.EqualTo(60f), "on the locked line");
            Assert.That(aimed.DamageTaken, Is.EqualTo(0f), "it stepped off the line after the release");
        }

        [Test]
        public void Draw_TheLineStartsWhereTheCoreStandsAtTheRelease()
        {
            Cast();

            _forms.SetCore(new Vector3(1f, 0f, 0f));
            _forms.Tick(0.2f, _hives);
            Assert.That(Strike().ParamA.x, Is.EqualTo(1f));
            Assert.That(Strike().ParamA.y, Is.EqualTo(0f));

            _forms.SetCore(new Vector3(2f, 0f, 0f));
            _forms.Tick(0.21f, _hives);
            _forms.SetCore(new Vector3(4f, 0f, 0f));
            _forms.Tick(0.05f, _hives);

            Vector4 a = Strike().ParamA;
            Assert.That(a.x, Is.EqualTo(2f), "locked where the core stood at the release");
            Assert.That(a.z, Is.EqualTo(-2f / Mathf.Sqrt(40f)).Within(1e-4f), "toward the aim point from there");
            Assert.That(a.w, Is.EqualTo(6f / Mathf.Sqrt(40f)).Within(1e-4f));
        }

        [Test]
        public void Draw_AnAimedBeekeeperThatDies_TheSpearKeepsPointingWhereItWasLastSeen()
        {
            FakeSwarmTarget aimed = Add(4f, 0f);
            _aim = aimed;
            Cast();

            aimed.IsAlive = false;
            aimed.Position = new Vector3(0f, 0f, -4f);
            _forms.Tick(0.45f, _hives);

            Assert.That(Strike().ParamA, Is.EqualTo(new Vector4(0f, 0f, 1f, 0f)));
        }

        // ---- The picture ----

        [Test]
        public void Params_HoldTheOriginAndDirectionOnTheGround_ThenTheLengthTheDrawAndTheTailLag()
        {
            _core = new Vector3(1f, 0.5f, -2f);
            _aimPoint = new Vector3(4f, 0f, 2f);

            Cast();

            Assert.That(Strike().ParamA.x, Is.EqualTo(1f));
            Assert.That(Strike().ParamA.y, Is.EqualTo(-2f));
            Assert.That(Strike().ParamA.z, Is.EqualTo(0.6f).Within(1e-5f));
            Assert.That(Strike().ParamA.w, Is.EqualTo(0.8f).Within(1e-5f));
            Assert.That(Strike().ParamB, Is.EqualTo(new Vector4(14f, 0.25f, 0.08f, 0f)));
        }

        // The picture throws the spear at the draw it is sent and flies its tip for LanceSong.FlightSeconds, so a tuned
        // draw must move the hurt with it: an impact hurts on the frame it visibly lands.
        [Test]
        public void Tick_WithATunedDraw_HitsTheSpearsFlightAfterTheDraw_AndThePictureGetsThatDraw()
        {
            JsonUtility.FromJsonOverwrite("{\"<DrawSeconds>k__BackingField\":0.6}", _song);
            FakeSwarmTarget onIt = Add(0f, 5f);
            Cast();

            Assert.That(Strike().ParamB.y, Is.EqualTo(0.6f), "the picture throws the spear at the draw");
            Assert.That(_song.HitSeconds, Is.EqualTo(0.6f + LanceSong.FlightSeconds));

            _forms.Tick(0.69f, _hives);
            Assert.That(onIt.DamageTaken, Is.EqualTo(0f), "the tip is still flying");

            _forms.Tick(0.02f, _hives);
            Assert.That(onIt.DamageTaken, Is.EqualTo(60f), "hurt as the tip lands");
        }

        // ---- Aiming and the level ----

        [TestCase(1)]
        [TestCase(3)]
        public void Cast_WithAnAimChooser_AsksForTheLineWithinFourteenGrownMetresAndThrustsAtTheAnswer(int level)
        {
            FakeSwarmTarget chosen = Add(-6f, 0f);
            FakeSwarmTarget beyondIt = Add(-10f, 0.1f);
            FakeSwarmTarget elsewhere = Add(0f, 5f);
            _chooser = new FakeSongAim { Target = chosen, Point = chosen.Position };
            _level = level;

            Cast();
            _forms.Tick(0.51f, _hives);

            Assert.That(_chooser.Calls, Is.EqualTo(1));
            Assert.That(_chooser.LastRule, Is.EqualTo(AimRule.Line));
            Assert.That(_chooser.LastRange, Is.EqualTo(14f * _levels.SizeScale(level)).Within(1e-4f));
            Assert.That(chosen.DamageTaken, Is.GreaterThan(0f));
            Assert.That(beyondIt.DamageTaken, Is.GreaterThan(0f), "the line runs on past the chosen one");
            Assert.That(elsewhere.DamageTaken, Is.EqualTo(0f));
        }

        [TestCase(1)]
        [TestCase(3)]
        public void Thrust_GrowsWithTheLevel_InLengthWidthAndDamage(int level)
        {
            FakeSwarmTarget far = Add(0f, 15f);
            FakeSwarmTarget wide = Add(0.7f, 5f);
            FakeSwarmTarget near = Add(0f, 5f);
            _level = level;
            Cast();

            _forms.Tick(0.51f, _hives);

            float damage = 60f * _levels.DamageScale(level);
            float grown = level == 1 ? 0f : damage;
            Assert.That(near.DamageTaken, Is.EqualTo(damage).Within(1e-3f));
            Assert.That(far.DamageTaken, Is.EqualTo(grown).Within(1e-3f), "15 m is past 14 m, within 17.36 m");
            Assert.That(wide.DamageTaken, Is.EqualTo(grown).Within(1e-3f), "0.7 m is past 0.6 m, within 0.744 m");
            Assert.That(near.LastKnockback.magnitude, Is.EqualTo(5f).Within(1e-4f), "the push does not grow");
            Assert.That(Strike().ParamB.x, Is.EqualTo(14f * _levels.SizeScale(level)).Within(1e-4f), "the picture gets the grown line");
        }

        // ---- How long it lasts, and what ends it ----

        // Changed with plan section O: it lasts 1.15 s (was 1.3 s), the 0.8 s after the hit kept; what it proves stays.
        [Test]
        public void Tick_ItLastsOnePointOneFiveSeconds_ThenItsReturnBeat()
        {
            Cast();

            _forms.Tick(1.14f, _hives);
            Assert.That(Strike().ReleasedSeconds, Is.EqualTo(-1f));

            _forms.Tick(0.02f, _hives);
            Assert.That(Strike().ReleasedSeconds, Is.GreaterThanOrEqualTo(0f));

            _forms.Tick(_squads.ReturnSeconds, _hives);
            Assert.That(Strike().IsActive, Is.False);
        }

        [Test]
        public void Cast_ReplacesTheRunningStrikeSong_AndKeepsTheGuardAndPostSongs()
        {
            var stung = new FakeSwarmTarget { Position = new Vector3(5f, 0f, 0f) };
            var post = Make<FakeSlotSong>();
            SwarmSongContext plain = Context();
            Make<DomeSong>().Cast(in plain);
            post.Cast(in plain);
            SwarmSongContext atStung = Context().AimedAt(stung, stung.Position);
            _songs[StingIndex].Cast(in atStung);

            Cast();
            _forms.Tick(2f, _hives);

            Assert.That(stung.DamageTaken, Is.EqualTo(0f), "the Sting ended before it arrived, and the line runs along +z");
            Assert.That(_forms.Slot(SongSlot.Guard).IsActive, Is.True);
            Assert.That(_forms.Slot(SongSlot.Post).IsActive, Is.True);
        }

        [Test]
        public void Collapse_BeforeTheThrust_EndsItWithNoDamage()
        {
            FakeSwarmTarget onIt = Add(0f, 5f);
            Cast();
            _forms.Tick(0.3f, _hives);

            _body.Lose(_body.Count);
            _forms.Tick(1f, _hives);

            Assert.That(onIt.DamageTaken, Is.EqualTo(0f));
            Assert.That(onIt.KnockbackCount, Is.EqualTo(0));
            Assert.That(_landings, Is.Empty);
            Assert.That(Strike().IsActive, Is.False);
        }

        [Test]
        public void Tick_ThroughTheWholeSong_AllocatesNothing()
        {
            _aim = Add(0f, 5f);
            _landings.Capacity = 8;
            Cast();

            Assert.That(
                () =>
                {
                    _forms.Tick(0.2f, _hives);
                    _forms.Tick(0.4f, _hives);
                    _forms.Tick(1.2f, _hives);
                },
                Is.Not.AllocatingGCMemory());
            Assert.That(_landings.Count, Is.EqualTo(1), "it thrust while measured");
        }

        // ---- Through the caster ----

        [Test]
        public void Press_WithNothingToHit_StillCasts()
        {
            var cast = new List<int>();
            _caster.SongCast += cast.Add;

            PlayAll();
            _forms.Tick(0.51f, _hives);

            Assert.That(cast, Is.EqualTo(new[] { LanceIndex }));
            Assert.That(_landings.Count, Is.EqualTo(1), "the spear still flies");
        }

        [Test]
        public void Press_WhileOnCooldown_CannotBeRecastUntilEightSecondsPass()
        {
            PlayAll();
            _caster.Tick(7.9f);

            Assert.That(PlayAll(), Is.EqualTo(SongBook.NoSong));

            _caster.Tick(0.11f);

            Assert.That(PlayAll(), Is.EqualTo(LanceIndex));
        }

        [Test]
        public void Press_InASurvivePhase_TheFirstCastIsFreeAndTheSecondCostsSeventy()
        {
            int start = _body.Count;

            PlayAll();
            Assert.That(_body.Count, Is.EqualTo(start));

            _caster.Tick(8.01f);
            PlayAll();
            Assert.That(_body.Count, Is.EqualTo(start - 70));
        }

        // ---- Helpers ----

        private static void AssertPush(FakeSwarmTarget keeper, Vector3 expected, string message = null)
        {
            Assert.That(keeper.KnockbackCount, Is.EqualTo(1), message);
            Assert.That(keeper.LastKnockback.x, Is.EqualTo(expected.x).Within(1e-4f), message);
            Assert.That(keeper.LastKnockback.y, Is.EqualTo(expected.y).Within(1e-4f), message);
            Assert.That(keeper.LastKnockback.z, Is.EqualTo(expected.z).Within(1e-4f), message);
        }

        private FakeSwarmTarget Add(float x, float z)
        {
            var keeper = new FakeSwarmTarget { Position = new Vector3(x, 0f, z) };
            _enemies.Add(keeper);
            return keeper;
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
