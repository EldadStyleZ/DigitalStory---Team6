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
    /// The slot engine of plan section C, through <see cref="SwarmFormMachine"/>: three slots with fixed seed bands, one song in
    /// each, a return beat after a song lets go, and how the Gust pulse, Hive Call, the Colossus and the collapse treat the slots.
    /// The Sting's target stands 3 m from the Piper, who stands at the origin, unless a test moves them.
    /// </summary>
    public sealed class SongSlotsTests
    {
        private const float Tolerance = 1e-4f;
        private const int StandingHives = 2;

        private readonly List<ISwarmTarget> _enemies = new List<ISwarmTarget>();
        private readonly List<Object> _assets = new List<Object>();
        private readonly List<SwarmForm> _changes = new List<SwarmForm>();
        private readonly List<SwarmForm> _landed = new List<SwarmForm>();

        private SwarmConfig _swarmConfig;
        private FuryConfig _furyConfig;
        private SquadConfig _squads;
        private HiveSet _hives;
        private SwarmBody _body;
        private FuryMeter _fury;
        private SwarmFormMachine _forms;
        private FakeSwarmTarget _aim;
        private Vector3 _aimPoint;
        private Vector3 _piper;
        private StingSong _sting;
        private TornadoSong _tornado;
        private MeteorSong _meteor;
        private DomeSong _dome;
        private GustSong _gust;
        private HiveCallSong _hiveCall;
        private ColossusSong _colossus;
        private FakeSlotSong _post;

        private SlotState Post => _forms.Slot(SongSlot.Post);

        private SlotState Guard => _forms.Slot(SongSlot.Guard);

        private SlotState Strike => _forms.Slot(SongSlot.Strike);

        [SetUp]
        public void SetUp()
        {
            _swarmConfig = Make<SwarmConfig>();
            _furyConfig = Make<FuryConfig>();
            _squads = Make<SquadConfig>();
            HiveConfig hiveConfig = Make<HiveConfig>();
            _hives = new HiveSet(new List<Hive> { new Hive(hiveConfig, new Vector3(-10f, 0f, 0f)), new Hive(hiveConfig, new Vector3(10f, 0f, 0f)) });
            _sting = Make<StingSong>();
            _tornado = Make<TornadoSong>();
            _meteor = Make<MeteorSong>();
            _dome = Make<DomeSong>();
            _gust = Make<GustSong>();
            _hiveCall = Make<HiveCallSong>();
            _colossus = Make<ColossusSong>();
            _post = Make<FakeSlotSong>();
            _aim = new FakeSwarmTarget { Position = new Vector3(3f, 0f, 0f) };
            _aimPoint = Vector3.zero;
            _piper = Vector3.zero;
            _enemies.Clear();
            Build();
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

        // Empty slots

        [TestCase(SongSlot.Post, 0f)]
        [TestCase(SongSlot.Guard, 0.2f)]
        [TestCase(SongSlot.Strike, 0.55f)]
        public void Slot_New_IsEmptyAndHoldsNoWasps(SongSlot slot, float bandLow)
        {
            SlotState state = _forms.Slot(slot);

            Assert.That(state.IsActive, Is.False);
            Assert.That(state.Seconds, Is.EqualTo(0f));
            Assert.That(state.ReleasedSeconds, Is.EqualTo(-1f));
            Assert.That(state.Low, Is.EqualTo(bandLow).Within(Tolerance));
            Assert.That(state.High, Is.EqualTo(state.Low), "an empty seed range");
        }

        [Test]
        public void Slot_NoSuchSlot_Throws()
        {
            Assert.That(() => _forms.Slot((SongSlot)3), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        // A Defense and an Attack together

        [Test]
        public void Cast_ADefenseThenAnAttack_RunSideBySideBesideTheRiver()
        {
            Cast(_dome);
            Cast(_sting);

            Assert.That(Guard.IsActive, Is.True);
            Assert.That(Guard.Form, Is.EqualTo(SwarmForm.Dome));
            Assert.That(Guard.Category, Is.EqualTo(SongCategory.Defense));
            Assert.That(Strike.IsActive, Is.True);
            Assert.That(Strike.Form, Is.EqualTo(SwarmForm.Sting));
            Assert.That(Strike.Category, Is.EqualTo(SongCategory.Attack));
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River), "the rest of the swarm stays the river");
            Assert.That(_forms.IsDomed, Is.True);
            Assert.That(_changes, Is.Empty, "a slot song does not change the whole swarm's form");
        }

        [Test]
        public void Tick_ADefenseAndAnAttackRunning_NeitherEndsTheOther()
        {
            Cast(_dome);
            Cast(_sting);

            Tick(1f);

            Assert.That(Guard.ReleasedSeconds, Is.EqualTo(-1f));
            Assert.That(Guard.Seconds, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(Strike.ReleasedSeconds, Is.EqualTo(-1f));
            Assert.That(Strike.Seconds, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(_forms.IsDomed, Is.True);
            Assert.That(_aim.DamageTaken, Is.EqualTo(10f).Within(1e-3f), "25 a second from its arrival at 0.6 s");
        }

        [Test]
        public void Cast_ADefenseWhileAnAttackRuns_KeepsTheAttackGoing()
        {
            Cast(_sting);
            Tick(0.5f);

            Cast(_dome);

            Assert.That(Strike.Form, Is.EqualTo(SwarmForm.Sting));
            Assert.That(Strike.Seconds, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(Strike.ReleasedSeconds, Is.EqualTo(-1f));
            Assert.That(Guard.Seconds, Is.EqualTo(0f));
        }

        // Replacing

        [Test]
        public void Cast_ReplacesOnlyTheSongInItsOwnSlot()
        {
            Cast(_dome);
            Cast(_sting);
            Tick(1f);

            Cast(_tornado);

            Assert.That(Strike.Form, Is.EqualTo(SwarmForm.Tornado));
            Assert.That(Strike.Seconds, Is.EqualTo(0f));
            Assert.That(Strike.ReleasedSeconds, Is.EqualTo(-1f));
            Assert.That(Guard.Form, Is.EqualTo(SwarmForm.Dome));
            Assert.That(Guard.Seconds, Is.EqualTo(1f).Within(Tolerance), "the Dome runs on untouched");
        }

        [Test]
        public void Cast_OverAMeteorBeforeItLands_LosesTheMeteorsImpact()
        {
            var enemy = new FakeSwarmTarget { Position = new Vector3(6f, 0f, 0f) };
            _enemies.Add(enemy);
            _aim = null;
            _aimPoint = new Vector3(6f, 0f, 0f);
            Cast(_meteor);
            Tick(1.2f);

            _aimPoint = new Vector3(-6f, 0f, 0f);
            Cast(_sting);
            Tick(2f);

            Assert.That(enemy.DamageTaken, Is.EqualTo(0f));
            Assert.That(enemy.LastStunSeconds, Is.EqualTo(0f));
            Assert.That(_fury.Value, Is.EqualTo(0f));
            Assert.That(_landed, Has.No.Member(SwarmForm.Meteor));
        }

        [Test]
        public void Cast_OverATornadoBeforeItsHit_LosesTheHit()
        {
            var enemy = new FakeSwarmTarget { Position = new Vector3(2f, 0f, 0f) };
            _enemies.Add(enemy);
            Cast(_tornado);
            Tick(0.2f);

            _aim = null;
            Cast(_sting);
            Tick(1f);

            Assert.That(enemy.DamageTaken, Is.EqualTo(0f));
            Assert.That(enemy.LastKnockback, Is.EqualTo(Vector3.zero));
            Assert.That(_landed, Has.No.Member(SwarmForm.Tornado));
        }

        // Release and the return beat

        [Test]
        public void Release_TimeUp_StartsTheReturnBeat()
        {
            Cast(_dome);

            Tick(5f);

            Assert.That(Guard.IsActive, Is.True, "the wasps still fly home");
            Assert.That(Guard.Seconds, Is.EqualTo(5f).Within(Tolerance));
            Assert.That(Guard.ReleasedSeconds, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_forms.IsDomed, Is.False, "the return beat shields nothing");
        }

        [Test]
        public void ReturnBeat_AfterItsSeconds_EmptiesTheSlot()
        {
            Cast(_dome);
            Tick(5f);

            Tick(0.5f);
            Assert.That(Guard.IsActive, Is.True);
            Assert.That(Guard.ReleasedSeconds, Is.EqualTo(0.5f).Within(Tolerance));

            Tick(0.2f);
            Assert.That(Guard.IsActive, Is.False);
            Assert.That(Guard.ReleasedSeconds, Is.EqualTo(-1f));
            Assert.That(Guard.High, Is.EqualTo(Guard.Low));
        }

        [Test]
        public void ReturnBeat_HurtsNothing()
        {
            Cast(_sting);

            Tick(4f);
            Assert.That(_aim.DamageTaken, Is.EqualTo(85f).Within(1e-3f), "25 a second from 0.6 s to 4 s");
            Assert.That(Strike.ReleasedSeconds, Is.EqualTo(0f).Within(Tolerance));

            Tick(0.5f);
            Assert.That(Strike.IsActive, Is.True);
            Assert.That(_aim.DamageTaken, Is.EqualTo(85f).Within(1e-3f));
            Assert.That(_fury.Value, Is.EqualTo(42.5f).Within(1e-3f));
        }

        [Test]
        public void Release_TargetDies_StartsTheReturnBeatAndStopsTheDamage()
        {
            Cast(_sting);
            Tick(1f);
            _aim.IsAlive = false;

            Tick(0.1f);
            Assert.That(_aim.DamageTaken, Is.EqualTo(10f).Within(1e-3f));
            Assert.That(Strike.IsActive, Is.True);
            Assert.That(Strike.ReleasedSeconds, Is.EqualTo(0.1f).Within(1e-3f), "it let go when the target was found dead");

            Tick(0.6f);
            Assert.That(Strike.IsActive, Is.False);
        }

        [Test]
        public void Cast_DuringTheReturnBeat_ReplacesIt()
        {
            Cast(_sting);
            Tick(4.2f);
            Assert.That(Strike.ReleasedSeconds, Is.GreaterThan(0f));

            Cast(_tornado);

            Assert.That(Strike.IsActive, Is.True);
            Assert.That(Strike.Form, Is.EqualTo(SwarmForm.Tornado));
            Assert.That(Strike.Seconds, Is.EqualTo(0f));
            Assert.That(Strike.ReleasedSeconds, Is.EqualTo(-1f));
        }

        [Test]
        public void Cast_TheSameSongDuringItsReturnBeat_StartsItAgain()
        {
            Cast(_dome);
            Tick(5.2f);

            Cast(_dome);

            Assert.That(Guard.ReleasedSeconds, Is.EqualTo(-1f));
            Assert.That(Guard.Seconds, Is.EqualTo(0f));
            Assert.That(_forms.IsDomed, Is.True);
            Tick(4.9f);
            Assert.That(_forms.IsDomed, Is.True, "a whole new five seconds");
            Tick(0.2f);
            Assert.That(_forms.IsDomed, Is.False);
        }

        [Test]
        public void Tick_OneStepPastTheSongAndItsReturnBeat_EmptiesTheSlotInThatStep()
        {
            var enemy = new FakeSwarmTarget { Position = new Vector3(2f, 0f, 0f) };
            _enemies.Add(enemy);
            Cast(_dome);
            Cast(_tornado);

            Tick(30f);

            Assert.That(Guard.IsActive, Is.False);
            Assert.That(Strike.IsActive, Is.False);
            Assert.That(enemy.DamageTaken, Is.EqualTo(30f), "the tornado still hit, once");
        }

        // The Gust pulse

        [Test]
        public void Gust_CastWhileTheDomeStands_IsAPulseThatKeepsTheDome()
        {
            Cast(_dome);
            Tick(1f);
            _piper = new Vector3(4f, 0f, 7f);

            Cast(_gust);

            Assert.That(Guard.Form, Is.EqualTo(SwarmForm.Dome));
            Assert.That(Guard.Seconds, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(Guard.ReleasedSeconds, Is.EqualTo(-1f));
            Assert.That(_forms.IsDomed, Is.True);
            Assert.That(Strike.IsActive, Is.False);
            Assert.That(Post.IsActive, Is.False);
            Assert.That(_forms.GustSeconds, Is.EqualTo(0f));
            Assert.That(_forms.GustCenter, Is.EqualTo(new Vector3(4f, 0f, 7f)));
            Assert.That(_forms.GustRadius, Is.EqualTo(6f));
        }

        [Test]
        public void GustSeconds_CountsThePulseThenIsMinusOne()
        {
            Assert.That(_forms.GustSeconds, Is.EqualTo(-1f), "no gust yet");
            Cast(_gust);

            Tick(0.5f);
            Assert.That(_forms.GustSeconds, Is.EqualTo(0.5f).Within(Tolerance));

            Tick(0.4f);
            Assert.That(_forms.GustSeconds, Is.EqualTo(-1f), "the pulse lasts 0.8 s");
        }

        [Test]
        public void Gust_DuringHiveCall_KeepsHiveCall()
        {
            Cast(_hiveCall);

            Cast(_gust);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.HiveCall));
            Assert.That(_forms.GustSeconds, Is.EqualTo(0f));
        }

        // Hive Call

        [Test]
        public void HiveCall_EndsTheStrikeSongAtOnceAndKeepsGuardAndPost()
        {
            Cast(_dome);
            Cast(_post);
            Cast(_sting);
            Tick(1f);

            Cast(_hiveCall);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.HiveCall));
            Assert.That(Strike.IsActive, Is.False, "ended, with no return beat");
            Assert.That(Guard.Form, Is.EqualTo(SwarmForm.Dome));
            Assert.That(Guard.Seconds, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(Post.IsActive, Is.True);
        }

        [Test]
        public void HiveCall_EndingAMeteor_LosesItsImpact()
        {
            var enemy = new FakeSwarmTarget { Position = new Vector3(6f, 0f, 0f) };
            _enemies.Add(enemy);
            _aim = null;
            _aimPoint = new Vector3(6f, 0f, 0f);
            Cast(_meteor);
            Tick(1f);

            Cast(_hiveCall);
            Tick(1f);

            Assert.That(enemy.DamageTaken, Is.EqualTo(0f));
            Assert.That(_landed, Is.Empty);
        }

        [Test]
        public void Defense_CastDuringHiveCall_KeepsHiveCall()
        {
            Cast(_hiveCall);

            Cast(_dome);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.HiveCall));
            Assert.That(_forms.IsPiperRooted, Is.True);
            Assert.That(Guard.Form, Is.EqualTo(SwarmForm.Dome));
        }

        [Test]
        public void Attack_CastDuringHiveCall_EndsHiveCall()
        {
            Cast(_hiveCall);

            Cast(_sting);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(Strike.Form, Is.EqualTo(SwarmForm.Sting));
            Assert.That(_changes, Is.EqualTo(new[] { SwarmForm.HiveCall, SwarmForm.River }));
        }

        [Test]
        public void Hit_DuringHiveCallWhileAGuardSongRuns_KeepsHiveCall()
        {
            Cast(_dome);
            Cast(_hiveCall);

            _forms.NotifyHit(new Vector3(1f, 0f, 0f));

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.HiveCall));
        }

        [Test]
        public void Hit_DuringHiveCallInTheGuardSongsReturnBeat_EndsHiveCall()
        {
            Cast(_dome);
            Cast(_hiveCall);
            Tick(5.2f);
            Assert.That(Guard.ReleasedSeconds, Is.GreaterThan(0f));
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.HiveCall));

            _forms.NotifyHit(new Vector3(1f, 0f, 0f));

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        [Test]
        public void PiperMoves_DuringHiveCallUnderTheDome_EndsHiveCallButKeepsTheDome()
        {
            Cast(_dome);
            Cast(_hiveCall);

            _forms.NotifyPiperMoved();

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(Guard.ReleasedSeconds, Is.EqualTo(-1f));
            Assert.That(_forms.IsDomed, Is.True);
        }

        // The Colossus

        [Test]
        public void Colossus_EndsStrikeAndGuardAtOnceAndKeepsPost()
        {
            Cast(_dome);
            Cast(_post);
            Cast(_sting);
            Tick(1f);
            FillFury();

            Cast(_colossus);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Colossus));
            Assert.That(Strike.IsActive, Is.False, "its wasps fly straight into the build");
            Assert.That(Guard.IsActive, Is.False);
            Assert.That(_forms.IsDomed, Is.False);
            Assert.That(Post.IsActive, Is.True);
            Assert.That(Post.Seconds, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Colossus_EndsHiveCall()
        {
            Cast(_hiveCall);
            FillFury();

            Cast(_colossus);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Colossus));
            Assert.That(_changes, Is.EqualTo(new[] { SwarmForm.HiveCall, SwarmForm.Colossus }));
        }

        [Test]
        public void Colossus_EndingAMeteor_LosesItsImpact()
        {
            var enemy = new FakeSwarmTarget { Position = new Vector3(6f, 0f, 0f) };
            _enemies.Add(enemy);
            _aim = null;
            _aimPoint = new Vector3(6f, 0f, 0f);
            Cast(_meteor);
            Tick(1f);
            FillFury();

            Cast(_colossus);
            Tick(1f);

            Assert.That(enemy.DamageTaken, Is.EqualTo(0f));
            Assert.That(_landed, Is.Empty);
        }

        [Test]
        public void Colossus_WhileItStands_NothingCasts()
        {
            FillFury();
            Cast(_colossus);
            _changes.Clear();
            SwarmSongContext context = Context();

            Assert.That(_forms.StartSong(_dome, in context), Is.False);
            Cast(_sting);
            Cast(_gust);
            Assert.That(_forms.StartHiveCall(), Is.False);

            Assert.That(Guard.IsActive, Is.False);
            Assert.That(Strike.IsActive, Is.False);
            Assert.That(_forms.GustSeconds, Is.EqualTo(-1f));
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Colossus));
            Assert.That(_changes, Is.Empty);
        }

        [Test]
        public void Colossus_WhileItStands_ThePostSongRunsOn()
        {
            Cast(_post);
            FillFury();
            Cast(_colossus);

            Tick(2f);

            Assert.That(Post.IsActive, Is.True);
            Assert.That(Post.Seconds, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(_post.Rule.TickCount, Is.EqualTo(1));
        }

        // The collapse

        [Test]
        public void Collapse_EndsEverySlotAndTheGustAtOnceWithNoReturnBeat()
        {
            Cast(_dome);
            Cast(_post);
            Cast(_sting);
            Cast(_gust);

            Collapse();

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Collapsed));
            Assert.That(Post.IsActive, Is.False);
            Assert.That(Guard.IsActive, Is.False);
            Assert.That(Strike.IsActive, Is.False);
            Assert.That(_forms.GustSeconds, Is.EqualTo(-1f));
            Assert.That(_forms.IsDomed, Is.False);
        }

        [Test]
        public void Collapse_AStingItEnded_HurtsNoMore()
        {
            Cast(_sting);
            Tick(1f);

            Collapse();
            Tick(1f);

            Assert.That(_aim.DamageTaken, Is.EqualTo(10f).Within(1e-3f));
        }

        [Test]
        public void StartSongAndGust_WhileCollapsed_AreRefused()
        {
            Collapse();
            SwarmSongContext context = Context();

            Assert.That(_forms.StartSong(_dome, in context), Is.False);
            Assert.That(_forms.StartGust(Vector3.zero, 6f, 0.8f), Is.False);
            Assert.That(Guard.IsActive, Is.False);
            Assert.That(_forms.GustSeconds, Is.EqualTo(-1f));
        }

        [Test]
        public void Respawn_AfterACollapse_SongsCastAgain()
        {
            Collapse();
            _body.Tick(_swarmConfig.CollapseBlockSeconds, StandingHives);
            SwarmSongContext context = Context();

            Assert.That(_forms.StartSong(_dome, in context), Is.True);
            Assert.That(Guard.Form, Is.EqualTo(SwarmForm.Dome));
        }

        // Squads

        [Test]
        public void Squad_EachSong_TakesItsShareFromTheStartOfItsBand()
        {
            Cast(_sting);
            AssertSquad(Strike, 0.55f, 0.85f);

            Cast(_tornado);
            AssertSquad(Strike, 0.55f, 0.92f);

            Cast(_meteor);
            AssertSquad(Strike, 0.55f, 0.9f);

            Cast(_dome);
            AssertSquad(Guard, 0.2f, 0.55f);

            Cast(_post);
            AssertSquad(Post, 0f, 0.15f);
        }

        [Test]
        public void Squad_AShareWiderThanItsBand_StopsAtTheBandsEnd()
        {
            _post.SlotToUse = SongSlot.Guard;
            _post.SetShare(0.5f);

            Cast(_post);

            AssertSquad(Guard, 0.2f, 0.55f);
        }

        [Test]
        public void Squad_BandsAndTheReturnBeat_ComeFromTheSquadConfig()
        {
            JsonUtility.FromJsonOverwrite(
                "{\"<StrikeLow>k__BackingField\":0.6,\"<StrikeHigh>k__BackingField\":0.95,\"<ReturnSeconds>k__BackingField\":1}", _squads);
            Build();

            Cast(_sting);
            AssertSquad(Strike, 0.6f, 0.9f);

            Tick(4f);
            Tick(0.9f);
            Assert.That(Strike.IsActive, Is.True, "a one second return beat");
            Tick(0.2f);
            Assert.That(Strike.IsActive, Is.False);
        }

        [Test]
        public void SquadConfig_Defaults_AreThePlansBandsAndReturnBeat()
        {
            Assert.That(_squads.PostLow, Is.EqualTo(0f));
            Assert.That(_squads.PostHigh, Is.EqualTo(0.2f));
            Assert.That(_squads.GuardLow, Is.EqualTo(0.2f));
            Assert.That(_squads.GuardHigh, Is.EqualTo(0.55f));
            Assert.That(_squads.StrikeLow, Is.EqualTo(0.55f));
            Assert.That(_squads.StrikeHigh, Is.EqualTo(0.92f));
            Assert.That(_squads.ReturnSeconds, Is.EqualTo(0.6f));
        }

        [TestCase("{\"<GuardLow>k__BackingField\":0.1}")]
        [TestCase("{\"<StrikeLow>k__BackingField\":0.5}")]
        [TestCase("{\"<PostLow>k__BackingField\":0.3}")]
        [TestCase("{\"<StrikeHigh>k__BackingField\":1.2}")]
        [TestCase("{\"<GuardLow>k__BackingField\":-0.1}")]
        [TestCase("{\"<ReturnSeconds>k__BackingField\":-1}")]
        public void New_BandsThatOverlapOrLeaveZeroToOneOrANegativeReturn_Throws(string json)
        {
            JsonUtility.FromJsonOverwrite(json, _squads);

            Assert.That(() => new SwarmFormMachine(_body, _fury, _squads), Throws.ArgumentException);
        }

        [Test]
        public void New_NoSquadConfig_Throws()
        {
            Assert.That(() => new SwarmFormMachine(_body, _fury, null), Throws.ArgumentNullException);
        }

        // Rules never read the wasp count

        [TestCase(300)]
        [TestCase(2000)]
        public void Songs_AtAnyWaspCount_HitAsHardAndTakeTheSameSquad(int wasps)
        {
            _body.Lose(_swarmConfig.StartingWasps - wasps);
            Assert.That(_body.Count, Is.EqualTo(wasps));
            var enemy = new FakeSwarmTarget { Position = new Vector3(2f, 0f, 0f) };
            _enemies.Add(enemy);
            _aim = enemy;

            Cast(_tornado);
            Tick(0.3f);
            Assert.That(enemy.DamageTaken, Is.EqualTo(30f).Within(1e-3f));

            Cast(_sting);
            Tick(4f);
            Assert.That(enemy.DamageTaken, Is.EqualTo(115f).Within(1e-3f));

            Cast(_meteor);
            AssertSquad(Strike, 0.55f, 0.9f);
            Tick(1.32f);
            Assert.That(enemy.DamageTaken, Is.EqualTo(175f).Within(1e-3f));
        }

        // The numbers each song gives its picture (plan section B, the "CPU:" lines)

        [Test]
        public void Sting_Params_AreTheLiveTargetAndTheSecondTarget()
        {
            Cast(_sting);
            AssertVector(Strike.ParamA, 3f, 0f, 0f, 0.6f);

            _aim.Position = new Vector3(4f, 0f, 1f);
            Tick(0.1f);

            AssertVector(Strike.ParamA, 4f, 0f, 1f, 0.6f);
            AssertVector(Strike.ParamB, 4f, 0f, 1f, 0f);
        }

        [Test]
        public void Sting_ParamAW_IsItsArrivalTime_SoThePictureLandsWithTheFirstDamage()
        {
            JsonUtility.FromJsonOverwrite("{\"<ArriveSeconds>k__BackingField\":0.45}", _sting);

            Cast(_sting);

            Assert.That(Strike.ParamA.w, Is.EqualTo(_sting.ArriveSeconds));
            Assert.That(Strike.ParamA.w, Is.EqualTo(0.45f));
        }

        [Test]
        public void Tornado_Params_AreItsReachHeightSpinAndHitTimeThenItsWall()
        {
            Cast(_tornado);

            AssertVector(Strike.ParamA, 5f, 3f, 6f, 0.3f);
            AssertVector(Strike.ParamB, 1.5f, -1f, 0f, 0f);
        }

        [Test]
        public void Meteor_Params_AreTheImpactAndHeightThenRiseDiveAndRadius()
        {
            _aim = null;
            _aimPoint = new Vector3(6f, 0f, 2f);

            Cast(_meteor);

            AssertVector(Strike.ParamA, 6f, 0f, 2f, 12f);
            AssertVector(Strike.ParamB, 0.8f, 0.5f, 3f, 0f);
        }

        [Test]
        public void Dome_Params_BeforeAnyHit_AreItsRadiusAndNoHit()
        {
            Cast(_dome);

            AssertVector(Guard.ParamA, 2.2f, 0f, 0f, -1f);
            AssertVector(Guard.ParamB, 0f, 0f, 0f, 0f);
        }

        [Test]
        public void Dome_ABlockedHit_GivesTheHitsSideAndTime()
        {
            _piper = new Vector3(1f, 0f, 1f);
            Cast(_dome);
            Tick(1.5f);

            _forms.NotifyHit(new Vector3(1f, 5f, 4f));

            AssertVector(Guard.ParamA, 2.2f, 0f, 1f, 1.5f);
        }

        [Test]
        public void Dome_AHitInItsReturnBeat_IsNotBlocked()
        {
            Cast(_dome);
            Tick(5.2f);

            _forms.NotifyHit(new Vector3(3f, 0f, 0f));

            Assert.That(Guard.ParamA.w, Is.EqualTo(-1f));
        }

        [Test]
        public void NotifyHit_ReachesOnlyRunningSongs()
        {
            _post.ReleaseAfter = 1f;
            Cast(_post);
            _forms.NotifyHit(Vector3.one);
            Tick(1.2f);

            _forms.NotifyHit(Vector3.one);

            Assert.That(_post.Rule.HitCount, Is.EqualTo(1));
        }

        // Landing

        [Test]
        public void Landed_StingArrives_IsRaisedOnceWhereTheTargetStands()
        {
            var points = new List<Vector3>();
            _forms.Landed += (form, point) => points.Add(point);
            Cast(_sting);

            Tick(0.59f);
            Assert.That(points, Is.Empty);
            Assert.That(_aim.DamageTaken, Is.EqualTo(0f));

            Tick(0.02f);
            Assert.That(_landed, Is.EqualTo(new[] { SwarmForm.Sting }));
            Assert.That(points, Is.EqualTo(new[] { new Vector3(3f, 0f, 0f) }));

            Tick(1f);
            Assert.That(_landed.Count, Is.EqualTo(1));
        }

        // What a running song changes in the match: a new song needs only its rule for these

        [Test]
        public void Modifiers_NoSongRunning_ChangeNothing()
        {
            Assert.That(_forms.WaspLossFraction, Is.EqualTo(1f));
            Assert.That(_forms.PiperSpeedMultiplier, Is.EqualTo(1f));
            Assert.That(_forms.SmokeSlowsPiper, Is.True);
            Assert.That(_forms.HiveDamageFraction(0), Is.EqualTo(1f));
        }

        [Test]
        public void Modifiers_TwoRunningSongs_Multiply()
        {
            _post.WaspLossFraction = 0.5f;
            _post.PiperSpeedMultiplier = 1.6f;
            _post.SmokeSlowsPiper = false;
            _post.ShieldedHive = 1;
            _post.HiveDamageFraction = 0.25f;

            Cast(_dome);
            Cast(_post);

            Assert.That(_forms.WaspLossFraction, Is.EqualTo(0.5f * _dome.LossFraction).Within(Tolerance));
            Assert.That(_forms.PiperSpeedMultiplier, Is.EqualTo(1.6f * _dome.SpeedMultiplier).Within(Tolerance));
            Assert.That(_forms.SmokeSlowsPiper, Is.False);
            Assert.That(_forms.HiveDamageFraction(1), Is.EqualTo(0.25f).Within(Tolerance));
            Assert.That(_forms.HiveDamageFraction(0), Is.EqualTo(1f), "only the shielded hive");
        }

        [Test]
        public void Modifiers_ASongThatLetGo_ChangesNothingDuringItsReturnBeat()
        {
            _post.WaspLossFraction = 0.5f;
            _post.SmokeSlowsPiper = false;
            _post.ShieldedHive = 0;
            _post.HiveDamageFraction = 0.25f;
            _post.ReleaseAfter = 1f;
            Cast(_post);

            Tick(1f);

            Assert.That(Post.ReleasedSeconds, Is.GreaterThanOrEqualTo(0f), "the return beat plays");
            Assert.That(_forms.WaspLossFraction, Is.EqualTo(1f));
            Assert.That(_forms.SmokeSlowsPiper, Is.True);
            Assert.That(_forms.HiveDamageFraction(0), Is.EqualTo(1f));
        }

        [Test]
        public void NotifyHiveHit_ReachesOnlyRunningSongs()
        {
            var attacker = new FakeSwarmTarget();
            Cast(_post);

            _forms.NotifyHiveHit(1, attacker);

            Assert.That(_post.Rule.HiveHitCount, Is.EqualTo(1));
            Assert.That(_post.Rule.LastHiveHit, Is.EqualTo(1));
            Assert.That(_post.Rule.LastHiveAttacker, Is.SameAs(attacker));

            _post.ReleaseAfter = 0f;
            Tick(0.1f);
            _forms.NotifyHiveHit(0, attacker);
            Assert.That(_post.Rule.HiveHitCount, Is.EqualTo(1), "it let go");
        }

        // Allocation

        [Test]
        public void Tick_EverySlotAndAGustRunning_AllocatesNothing()
        {
            Cast(_dome);
            Cast(_post);
            Cast(_sting);
            Cast(_gust);

            Assert.That(() => { _forms.Tick(0.016f, StandingHives); }, Is.Not.AllocatingGCMemory());
            Assert.That(() => { _forms.Slot(SongSlot.Strike); }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Modifiers_WithEverySlotRunning_AllocateNothing()
        {
            Cast(_dome);
            Cast(_post);
            Cast(_sting);
            var attacker = new FakeSwarmTarget();

            Assert.That(() => { _ = _forms.WaspLossFraction * _forms.PiperSpeedMultiplier * _forms.HiveDamageFraction(0); }, Is.Not.AllocatingGCMemory());
            Assert.That(() => { _forms.NotifyHiveHit(0, attacker); }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Cast_ASongCastBefore_AllocatesNothing()
        {
            Cast(_sting);
            SwarmSongContext context = Context();

            Assert.That(() => { _sting.Cast(in context); }, Is.Not.AllocatingGCMemory());
        }

        private static void AssertSquad(SlotState state, float low, float high)
        {
            Assert.That(state.Low, Is.EqualTo(low).Within(Tolerance), "low");
            Assert.That(state.High, Is.EqualTo(high).Within(Tolerance), "high");
        }

        private static void AssertVector(Vector4 actual, float x, float y, float z, float w)
        {
            Assert.That(actual.x, Is.EqualTo(x).Within(Tolerance), "x");
            Assert.That(actual.y, Is.EqualTo(y).Within(Tolerance), "y");
            Assert.That(actual.z, Is.EqualTo(z).Within(Tolerance), "z");
            Assert.That(actual.w, Is.EqualTo(w).Within(Tolerance), "w");
        }

        private void Build()
        {
            _body = new SwarmBody(_swarmConfig);
            _fury = new FuryMeter(_furyConfig);
            _forms = new SwarmFormMachine(_body, _fury, _squads);
            _changes.Clear();
            _landed.Clear();
            _forms.FormChanged += _changes.Add;
            _forms.Landed += (form, point) => _landed.Add(form);
        }

        private T Make<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }

        private void Cast(SwarmSongEffect song)
        {
            SwarmSongContext context = Context();
            song.Cast(in context);
        }

        private SwarmSongContext Context()
        {
            return new SwarmSongContext(_piper, _aim, _aimPoint, _body, _forms, _fury, _hives, _enemies, null);
        }

        private void Tick(float seconds)
        {
            _forms.Tick(seconds, StandingHives);
        }

        private void Collapse()
        {
            _body.Lose(_swarmConfig.StartingWasps - 100);
        }

        private void FillFury()
        {
            _fury.AddDamage(_furyConfig.Max * _furyConfig.DamagePerFuryPoint);
        }
    }
}
