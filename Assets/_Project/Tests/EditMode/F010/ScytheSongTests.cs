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
    /// Plan sections A, B and E: the Scythe's blade sweeps round the core, and at 0.3 s it cuts every beekeeper within 4.5 m
    /// in the half-disc facing the aim, which the Threat rule picks.
    /// </summary>
    public sealed class ScytheSongTests
    {
        private const int HoleCount = 4;
        private const int ScytheIndex = 8;
        private const int StingIndex = 1;

        // K L H.
        private static readonly int[] Keys = { 2, 3, 0 };

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
        private ScytheSong _song;
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
            _song = Make<ScytheSong>();
            _songs.Clear();
            _songs.Add(Make<DomeSong>());
            _songs.Add(Make<StingSong>());
            _songs.Add(Make<TornadoSong>());
            _songs.Add(Make<GustSong>());
            _songs.Add(Make<HiveCallSong>());
            _songs.Add(Make<ColossusSong>());
            _songs.Add(Make<MeteorSong>());
            _songs.Add(Make<HydraSong>());
            _songs.Add(_song);
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
                if (form == SwarmForm.Scythe)
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
            Assert.That(_song.Song.Title, Is.EqualTo("Scythe"));
            Assert.That(_song.Song.CooldownSeconds, Is.EqualTo(4f));
            Assert.That(_song.Song.Keys, Is.EqualTo(Keys));
            Assert.That(_song.Category, Is.EqualTo(SongCategory.Attack));
            Assert.That(_song.WaspCost, Is.EqualTo(30));
            Assert.That(_song.Slot, Is.EqualTo(SongSlot.Strike));
            Assert.That(_song.Form, Is.EqualTo(SwarmForm.Scythe));
            Assert.That(_song.Share, Is.EqualTo(0.37f));
            Assert.That(_song.Radius, Is.EqualTo(4.5f));
            Assert.That(_song.HitSeconds, Is.EqualTo(0.3f));
            Assert.That(_song.Damage, Is.EqualTo(45f));
            Assert.That(_song.Seconds, Is.EqualTo(1f));
            Assert.That(_song.WindUpSeconds, Is.EqualTo(0.25f));
            Assert.That(_song.SweepDegrees, Is.EqualTo(200f));
            Assert.That(_song.SweepSeconds, Is.EqualTo(0.2f), "the picture's edge, wound back 30 degrees, reaches the aim by the cut");
            Assert.That(_song.FacingRange, Is.EqualTo(6f));
            Assert.That(_song.IsAimed, Is.True);
            Assert.That(_song.AimRule, Is.EqualTo(AimRule.Threat));
            Assert.That(_song.AimRange, Is.EqualTo(6f));
            Assert.That(_song.CostsInFarmPhase, Is.False, "an attack is no tower");
        }

        [Test]
        public void Keys_PlayedFromSilence_CastExactlyTheScythe()
        {
            var cast = new List<int>();
            _caster.SongCast += cast.Add;

            int result = PlayAll();

            Assert.That(result, Is.EqualTo(ScytheIndex));
            Assert.That(cast, Is.EqualTo(new[] { ScytheIndex }));
            Assert.That(Strike().Form, Is.EqualTo(SwarmForm.Scythe));
        }

        [Test]
        public void Cast_RunsInTheStrikeSlotWithThirtySevenPercentOfTheSwarm()
        {
            Cast();

            SlotState strike = Strike();
            Assert.That(strike.IsActive, Is.True);
            Assert.That(strike.Form, Is.EqualTo(SwarmForm.Scythe));
            Assert.That(strike.Category, Is.EqualTo(SongCategory.Attack));
            Assert.That(strike.Low, Is.EqualTo(0.55f).Within(1e-5f));
            Assert.That(strike.High, Is.EqualTo(0.92f).Within(1e-5f));
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        // ---- The cut ----

        [Test]
        public void Tick_BeforeTheCut_HurtsNobodyAndRaisesNothing()
        {
            FakeSwarmTarget front = Add(0f, 3f);
            Cast();

            _forms.Tick(0.29f, _hives);

            Assert.That(front.DamageTaken, Is.EqualTo(0f));
            Assert.That(_fury.Value, Is.EqualTo(0f));
            Assert.That(_landings, Is.Empty);
        }

        [Test]
        public void Tick_AtTheCut_HurtsEveryLivingBeekeeperInTheHalfDiscFacingTheAim()
        {
            FakeSwarmTarget front = Add(0f, 3f);
            FakeSwarmTarget onTheLine = Add(4f, 0f);
            FakeSwarmTarget justBehind = Add(2f, -0.05f);
            FakeSwarmTarget onTheEdge = Add(0f, 4.5f);
            FakeSwarmTarget justOutside = Add(0f, 4.55f);
            var high = new FakeSwarmTarget { Position = new Vector3(1f, 5f, 1f) };
            var dead = new FakeSwarmTarget { Position = new Vector3(0f, 0f, 1f), IsAlive = false };
            _enemies.Add(high);
            _enemies.Add(dead);
            Cast();

            _forms.Tick(0.31f, _hives);

            Assert.That(front.DamageTaken, Is.EqualTo(45f));
            Assert.That(onTheLine.DamageTaken, Is.EqualTo(45f), "the line through the core is the half-disc's edge");
            Assert.That(justBehind.DamageTaken, Is.EqualTo(0f), "just behind the line");
            Assert.That(onTheEdge.DamageTaken, Is.EqualTo(45f), "4.5 m counts");
            Assert.That(justOutside.DamageTaken, Is.EqualTo(0f), "just outside the radius");
            Assert.That(high.DamageTaken, Is.EqualTo(45f), "height is ignored");
            Assert.That(dead.DamageTaken, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_AtTheCut_FillsFuryFromTheTotalDamage()
        {
            Add(0f, 3f);
            Add(-2f, 1f);
            Add(0f, -3f);
            Cast();

            _forms.Tick(0.31f, _hives);

            Assert.That(_fury.Value, Is.EqualTo(90f / _furyConfig.DamagePerFuryPoint).Within(1e-3f), "two cuts of 45");
        }

        [Test]
        public void Tick_CutsOnlyOnce()
        {
            FakeSwarmTarget front = Add(0f, 3f);
            Cast();

            _forms.Tick(0.31f, _hives);
            _forms.Tick(0.3f, _hives);
            _forms.Tick(0.3f, _hives);

            Assert.That(front.DamageTaken, Is.EqualTo(45f));
        }

        [Test]
        public void Tick_FacesTheAimedBeekeeper()
        {
            FakeSwarmTarget aimed = Add(3f, 0f);
            FakeSwarmTarget sameSide = Add(2f, 1f);
            FakeSwarmTarget otherSide = Add(-2f, 0f);
            _aim = aimed;
            Cast();

            _forms.Tick(0.31f, _hives);

            Assert.That(aimed.DamageTaken, Is.EqualTo(45f));
            Assert.That(sameSide.DamageTaken, Is.EqualTo(45f));
            Assert.That(otherSide.DamageTaken, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_TheCutIsCentredOnTheCoreWhereItStandsThen()
        {
            FakeSwarmTarget nearTheNewPlace = Add(5f, 3f);
            FakeSwarmTarget nearTheOldPlace = Add(0f, 3f);
            Cast();

            _forms.SetCore(new Vector3(5f, 0f, 0f));
            _forms.Tick(0.31f, _hives);

            Assert.That(nearTheNewPlace.DamageTaken, Is.EqualTo(45f));
            Assert.That(nearTheOldPlace.DamageTaken, Is.EqualTo(0f), "5.8 m from where the core stands at the cut");
            Assert.That(_landings, Is.EqualTo(new[] { new Vector3(5f, 0f, 0f) }));
        }

        [Test]
        public void Tick_AtTheCut_RaisesScytheLandedOnceAtTheCore()
        {
            _core = new Vector3(1f, 0f, 2f);
            _forms.SetCore(_core);
            Cast();

            _forms.Tick(0.31f, _hives);
            _forms.Tick(0.5f, _hives);

            Assert.That(_landings, Is.EqualTo(new[] { new Vector3(1f, 0f, 2f) }));
        }

        [TestCase(1)]
        [TestCase(3)]
        public void Cut_GrowsWithTheLevel_InReachAndDamage(int level)
        {
            FakeSwarmTarget fiveMetresOut = Add(0f, 5f);
            _level = level;
            Cast();

            _forms.Tick(0.31f, _hives);

            float expected = level == 1 ? 0f : 45f * _levels.DamageScale(level);
            Assert.That(fiveMetresOut.DamageTaken, Is.EqualTo(expected).Within(1e-3f), "4.5 m, or 5.58 m at level 3");
            Assert.That(Strike().ParamA.z, Is.EqualTo(4.5f * _levels.SizeScale(level)).Within(1e-4f), "the picture gets the grown blade");
        }

        // ---- The picture ----

        [Test]
        public void Params_HoldTheStartAngleTheSignedSweepTheRadiusAndTheWindUp_ThenTheCutAndTheSweepTimes()
        {
            Cast();

            Vector4 a = Strike().ParamA;
            float sweep = 200f * Mathf.Deg2Rad;
            Assert.That(a.x, Is.EqualTo((Mathf.PI * 0.5f) - (sweep * 0.5f)).Within(1e-4f), "the sweep is centred on the aim, +z");
            Assert.That(a.y, Is.EqualTo(sweep).Within(1e-4f));
            Assert.That(a.z, Is.EqualTo(4.5f).Within(1e-4f));
            Assert.That(a.w, Is.EqualTo(0.25f).Within(1e-4f));
            Assert.That(Strike().ParamB, Is.EqualTo(new Vector4(0.3f, 0.2f, 0f, 0f)));
        }

        [Test]
        public void Params_EachCastSweepsTheOtherWay()
        {
            Cast();
            float first = Strike().ParamA.y;

            Cast();
            float second = Strike().ParamA.y;
            float sweep = 200f * Mathf.Deg2Rad;

            Assert.That(second, Is.EqualTo(-first).Within(1e-5f));
            Assert.That(Strike().ParamA.x, Is.EqualTo((Mathf.PI * 0.5f) + (sweep * 0.5f)).Within(1e-4f), "still centred on the aim");
        }

        [Test]
        public void Params_AimedAtABeekeeper_CentreTheSweepOnIt()
        {
            _aim = Add(-3f, 0f);

            Cast();

            Vector4 a = Strike().ParamA;
            Assert.That(a.x + (a.y * 0.5f), Is.EqualTo(Mathf.PI).Within(1e-4f), "-x lies at pi");
        }

        // ---- Aiming ----

        [TestCase(1)]
        [TestCase(3)]
        public void Cast_WithAnAimChooser_AsksForTheThreatWithinSixGrownMetresAndFacesTheAnswer(int level)
        {
            FakeSwarmTarget chosen = Add(-3f, 0f);
            FakeSwarmTarget otherSide = Add(2f, 0f);
            _chooser = new FakeSongAim { Target = chosen, Point = chosen.Position };
            _level = level;

            Cast();
            _forms.Tick(0.31f, _hives);

            Assert.That(_chooser.Calls, Is.EqualTo(1));
            Assert.That(_chooser.LastRule, Is.EqualTo(AimRule.Threat));
            Assert.That(_chooser.LastRange, Is.EqualTo(6f * _levels.SizeScale(level)).Within(1e-4f));
            Assert.That(chosen.DamageTaken, Is.GreaterThan(0f));
            Assert.That(otherSide.DamageTaken, Is.EqualTo(0f));
        }

        // ---- How long it lasts, and what ends it ----

        [Test]
        public void Tick_ItLastsOneSecond_ThenItsReturnBeat()
        {
            Cast();

            _forms.Tick(0.99f, _hives);
            Assert.That(Strike().ReleasedSeconds, Is.EqualTo(-1f));

            _forms.Tick(0.02f, _hives);
            Assert.That(Strike().ReleasedSeconds, Is.GreaterThanOrEqualTo(0f));

            _forms.Tick(_squads.ReturnSeconds, _hives);
            Assert.That(Strike().IsActive, Is.False);
        }

        [Test]
        public void Cast_ReplacesTheRunningStrikeSong_AndKeepsTheGuardAndPostSongs()
        {
            var stung = new FakeSwarmTarget { Position = new Vector3(0f, 0f, -8f) };
            var post = Make<FakeSlotSong>();
            SwarmSongContext plain = Context();
            Make<DomeSong>().Cast(in plain);
            post.Cast(in plain);
            SwarmSongContext atStung = Context().AimedAt(stung, stung.Position);
            _songs[StingIndex].Cast(in atStung);

            Cast();
            _forms.Tick(2f, _hives);

            Assert.That(stung.DamageTaken, Is.EqualTo(0f), "the Sting ended before it arrived, and the cut does not reach 8 m");
            Assert.That(_forms.Slot(SongSlot.Guard).IsActive, Is.True);
            Assert.That(_forms.Slot(SongSlot.Post).IsActive, Is.True);
        }

        [Test]
        public void Cast_ReplacesTheRunningStrikeSong_WithItsOwnForm()
        {
            SwarmSongContext plain = Context();
            _songs[StingIndex].Cast(in plain);

            Cast();

            Assert.That(Strike().Form, Is.EqualTo(SwarmForm.Scythe));
        }

        [Test]
        public void Collapse_BeforeTheCut_EndsItWithNoDamage()
        {
            FakeSwarmTarget front = Add(0f, 3f);
            Cast();
            _forms.Tick(0.2f, _hives);

            _body.Lose(_body.Count);
            _forms.Tick(1f, _hives);

            Assert.That(front.DamageTaken, Is.EqualTo(0f));
            Assert.That(_landings, Is.Empty);
            Assert.That(Strike().IsActive, Is.False);
        }

        [Test]
        public void Tick_ThroughTheWholeSong_AllocatesNothing()
        {
            Add(0f, 3f);
            _landings.Capacity = 8;
            Cast();

            Assert.That(
                () =>
                {
                    _forms.Tick(0.2f, _hives);
                    _forms.Tick(0.2f, _hives);
                    _forms.Tick(1.2f, _hives);
                },
                Is.Not.AllocatingGCMemory());
            Assert.That(_landings.Count, Is.EqualTo(1), "it cut while measured");
        }

        // ---- Through the caster ----

        [Test]
        public void Press_WithNothingToHit_StillCasts()
        {
            var cast = new List<int>();
            _caster.SongCast += cast.Add;

            PlayAll();
            _forms.Tick(0.31f, _hives);

            Assert.That(cast, Is.EqualTo(new[] { ScytheIndex }));
            Assert.That(_landings.Count, Is.EqualTo(1), "the blade still sweeps");
        }

        [Test]
        public void Press_WhileOnCooldown_CannotBeRecastUntilFourSecondsPass()
        {
            PlayAll();
            _caster.Tick(3.9f);

            Assert.That(PlayAll(), Is.EqualTo(SongBook.NoSong));

            _caster.Tick(0.11f);

            Assert.That(PlayAll(), Is.EqualTo(ScytheIndex));
        }

        [Test]
        public void Press_InASurvivePhase_TheFirstCastIsFreeAndTheSecondCostsThirty()
        {
            int start = _body.Count;

            PlayAll();
            Assert.That(_body.Count, Is.EqualTo(start));

            _caster.Tick(4.01f);
            PlayAll();
            Assert.That(_body.Count, Is.EqualTo(start - 30));
        }

        // ---- Helpers ----

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
