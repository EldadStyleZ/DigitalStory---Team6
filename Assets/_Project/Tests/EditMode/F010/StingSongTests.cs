using System.Collections.Generic;
using System.Globalization;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// The owner's many stings (plan section O): one Sting cast sends one sting for every 400 living wasps at the cast, from
    /// 1 to 10. Each takes its own target among the most threatening beekeepers in reach, the spare ones double up, most
    /// threatening first, and a sting whose target dies takes the next threat, or ends. This one rule reads the live count,
    /// by the owner's decision. The single sting's own rows are in <see cref="SwarmSongTests"/>.
    /// </summary>
    public sealed class StingSongTests
    {
        private readonly List<Object> _assets = new List<Object>();

        private SwarmConfig _swarmConfig;
        private FuryConfig _furyConfig;
        private HiveConfig _hiveConfig;
        private SquadConfig _squads;
        private SwarmBody _body;
        private FuryMeter _fury;
        private SwarmFormMachine _forms;
        private HiveSet _hives;
        private List<ISwarmTarget> _enemies;
        private FakeSongAim _aim;
        private StingSong _song;
        private List<SongHit> _hits;

        private SlotState Strike => _forms.Slot(SongSlot.Strike);

        [SetUp]
        public void SetUp()
        {
            _swarmConfig = Make<SwarmConfig>();
            _furyConfig = Make<FuryConfig>();
            _hiveConfig = Make<HiveConfig>();
            _squads = Make<SquadConfig>();
            _hives = new HiveSet(new List<Hive> { new Hive(_hiveConfig, new Vector3(-30f, 0f, 0f)), new Hive(_hiveConfig, new Vector3(30f, 0f, 0f)) });
            _enemies = new List<ISwarmTarget>();
            _aim = new FakeSongAim { Point = new Vector3(0f, 0f, 6f) };
            _song = Make<StingSong>();
            Build(800);
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

        // ---- How many ----

        [TestCase(0, 1)]
        [TestCase(399, 1)]
        [TestCase(400, 1)]
        [TestCase(799, 1)]
        [TestCase(800, 2)]
        [TestCase(1199, 2)]
        [TestCase(1200, 3)]
        [TestCase(3999, 9)]
        [TestCase(4000, 10)]
        [TestCase(100000, 10)]
        public void StingCount_IsOnePerFourHundredLivingWasps_AtLeastOneAndAtMostTen(int livingWasps, int stings)
        {
            Assert.That(_song.StingCount(livingWasps), Is.EqualTo(stings));
        }

        [Test]
        public void Defaults_HoldTheOwnersNumbers()
        {
            Assert.That(_song.WaspsPerSting, Is.EqualTo(400));
            Assert.That(_song.MaxStings, Is.EqualTo(10));
            Assert.That(_song.ArriveSeconds, Is.EqualTo(0.35f), "was 0.6");
            Assert.That(_song.ReaimSeconds, Is.EqualTo(0.2f));
            Assert.That(_song.SqueezeSeconds, Is.EqualTo(0.25f));
        }

        // The owner: every 400 wasps one sting, and the price is the Sting's for each sting sent.
        [TestCase(0, 1)]
        [TestCase(799, 1)]
        [TestCase(800, 2)]
        [TestCase(1200, 3)]
        [TestCase(4000, 10)]
        [TestCase(9000, 10)]
        public void WaspCostFor_IsTheStingsPriceForEachStingSent(int livingWasps, int stings)
        {
            Assert.That(_song.WaspCostFor(livingWasps), Is.EqualTo(_song.WaspCost * stings));
        }

        [Test]
        public void Cast_SendsAStingForEveryFourHundredLivingWasps_EachAtItsOwnThreat()
        {
            Build(1200);
            FakeSwarmTarget first = Threat(2f, 0f);
            FakeSwarmTarget second = Threat(0f, 3f);
            FakeSwarmTarget third = Threat(-4f, 0f);
            Threat(0f, -5f);

            Cast();

            Assert.That(_aim.ThreatCalls, Is.EqualTo(1));
            Assert.That(_aim.LastThreatCount, Is.EqualTo(3));
            Assert.That(_aim.LastRange, Is.EqualTo(_song.MaxRange));
            Assert.That(_forms.StingCount, Is.EqualTo(3));
            Assert.That(_forms.StingTarget(0), Is.EqualTo(first.Position));
            Assert.That(_forms.StingTarget(1), Is.EqualTo(second.Position));
            Assert.That(_forms.StingTarget(2), Is.EqualTo(third.Position));
            Assert.That((Vector3)Strike.ParamA, Is.EqualTo(first.Position), "ParamA stays the first target");
            Assert.That(Strike.ParamA.w, Is.EqualTo(_song.ArriveSeconds));
            Assert.That(Strike.ParamB, Is.EqualTo(new Vector4(3f, 0f, 0f, 0f)), "ParamB.x carries the count, so the squad is shared evenly");
        }

        [Test]
        public void Cast_FewerTargetsThanStings_TheSpareStingsDoubleUpMostThreateningFirst()
        {
            Build(2000);
            FakeSwarmTarget first = Threat(2f, 0f);
            FakeSwarmTarget second = Threat(0f, 3f);

            Cast();

            Assert.That(_forms.StingCount, Is.EqualTo(5));
            Assert.That(_forms.StingTarget(0), Is.EqualTo(first.Position));
            Assert.That(_forms.StingTarget(1), Is.EqualTo(second.Position));
            Assert.That(_forms.StingTarget(2), Is.EqualTo(first.Position));
            Assert.That(_forms.StingTarget(3), Is.EqualTo(second.Position));
            Assert.That(_forms.StingTarget(4), Is.EqualTo(first.Position));
        }

        // Changed with the felt hit (plan section O): the damage comes in pulses on the picture's squeezes, the first on the
        // arrival, each the damage of the time to the next squeeze, so the impact is felt on the frame it lands.
        [Test]
        public void Tick_EachSting_HurtsItsOwnTargetOnItsArrivalAndOnEverySqueeze()
        {
            Build(1200);
            FakeSwarmTarget first = Threat(2f, 0f);
            FakeSwarmTarget second = Threat(0f, 3f);
            FakeSwarmTarget third = Threat(-4f, 0f);
            Cast();

            _forms.Tick(_song.ArriveSeconds - 0.01f, _hives);
            Assert.That(first.DamageTaken + second.DamageTaken + third.DamageTaken, Is.EqualTo(0f));
            _forms.Tick(0.02f, _hives);

            float pulse = Pulse();
            Assert.That(first.DamageTaken, Is.EqualTo(pulse).Within(1e-3f), "the arrival deals the time to the next squeeze at once");
            Assert.That(second.DamageTaken, Is.EqualTo(pulse).Within(1e-3f));
            Assert.That(third.DamageTaken, Is.EqualTo(pulse).Within(1e-3f));

            _forms.Tick(_song.SqueezeSeconds, _hives);

            Assert.That(first.DamageTaken, Is.EqualTo(2f * pulse).Within(1e-3f), "and the next squeeze the quarter second after it");
            Assert.That(second.DamageTaken, Is.EqualTo(2f * pulse).Within(1e-3f));
            Assert.That(third.DamageTaken, Is.EqualTo(2f * pulse).Within(1e-3f));
            Assert.That(_fury.Value, Is.EqualTo(3f * 2f * pulse / _furyConfig.DamagePerFuryPoint).Within(1e-3f));
        }

        [Test]
        public void Tick_StingsDoubledUpOnOneTarget_AddTheirDamage()
        {
            Build(1200);
            FakeSwarmTarget only = Threat(2f, 0f);
            Cast();

            _forms.Tick(_song.ArriveSeconds, _hives);
            _forms.Tick(_song.SqueezeSeconds * 1.5f, _hives);

            Assert.That(only.DamageTaken, Is.EqualTo(3f * 2f * Pulse()).Within(1e-3f), "three stings, each on the arrival and the next squeeze");
        }

        [Test]
        public void Tick_DamagePerStingGrowsWithTheLevel_NotWithTheCount()
        {
            Build(800);
            FakeSwarmTarget first = Threat(2f, 0f);
            FakeSwarmTarget second = Threat(0f, 3f);
            Cast(damageScale: 1.24f);

            _forms.Tick(_song.ArriveSeconds, _hives);
            _forms.Tick(_song.SqueezeSeconds * 1.5f, _hives);

            Assert.That(first.DamageTaken, Is.EqualTo(2f * Pulse() * 1.24f).Within(1e-3f));
            Assert.That(second.DamageTaken, Is.EqualTo(2f * Pulse() * 1.24f).Within(1e-3f));
        }

        // ---- When a target dies ----

        [Test]
        public void Tick_AStingWhoseTargetDies_TakesTheNextThreatNotYetStung()
        {
            FakeSwarmTarget first = Threat(2f, 0f);
            FakeSwarmTarget second = Threat(0f, 3f);
            FakeSwarmTarget third = Threat(-4f, 0f);
            Cast();
            _forms.Tick(_song.ArriveSeconds + 0.1f, _hives);

            first.IsAlive = false;
            _forms.Tick(0.1f, _hives);

            Assert.That(_forms.StingCount, Is.EqualTo(2));
            Assert.That(_forms.StingTarget(0), Is.EqualTo(third.Position), "the second sting already wraps the second threat");
            Assert.That(_forms.StingTarget(1), Is.EqualTo(second.Position));
            Assert.That(Strike.ReleasedSeconds, Is.EqualTo(-1f));
        }

        [Test]
        public void Tick_AStingThatTookANewTarget_HurtsItOnlyOnceItHasFlownThere()
        {
            FakeSwarmTarget first = Threat(2f, 0f);
            Threat(0f, 3f);
            FakeSwarmTarget third = Threat(-4f, 0f);
            Cast();
            _forms.Tick(_song.ArriveSeconds + 0.1f, _hives);
            first.IsAlive = false;

            _forms.Tick(_song.ReaimSeconds - 0.01f, _hives);
            Assert.That(third.DamageTaken, Is.EqualTo(0f), "the squeeze while it flies there is not its");

            _forms.Tick(0.02f, _hives);

            // It hurts on its arrival, for the time from there to the next squeeze, and then on the squeezes.
            float arrival = _song.ArriveSeconds + 0.1f + _song.ReaimSeconds;
            float nextSqueeze = _song.ArriveSeconds + (2f * _song.SqueezeSeconds);
            Assert.That(third.DamageTaken, Is.EqualTo(_song.DamagePerSecond * (nextSqueeze - arrival)).Within(1e-3f));
        }

        [Test]
        public void Tick_AStingWhoseTargetDiesWithEveryThreatStung_DoublesUpOnTheMostThreatening()
        {
            Build(1200);
            FakeSwarmTarget first = Threat(2f, 0f);
            FakeSwarmTarget second = Threat(0f, 3f);
            FakeSwarmTarget third = Threat(-4f, 0f);
            Cast();
            _forms.Tick(_song.ArriveSeconds + 0.1f, _hives);

            third.IsAlive = false;
            _forms.Tick(0.1f, _hives);

            Assert.That(_forms.StingTarget(2), Is.EqualTo(first.Position));
            Assert.That(_forms.StingTarget(1), Is.EqualTo(second.Position));
        }

        [Test]
        public void Tick_AStingWhoseOwnDamageKills_TakesTheNextThreatInTheSameStep()
        {
            FakeSwarmTarget weak = Threat(2f, 0f);
            weak.DiesAfter = 5f;
            Threat(0f, 3f);
            FakeSwarmTarget third = Threat(-4f, 0f);
            Cast();

            _forms.Tick(_song.ArriveSeconds + 0.3f, _hives);

            Assert.That(weak.IsAlive, Is.False);
            Assert.That(_forms.StingTarget(0), Is.EqualTo(third.Position));
            Assert.That(Strike.ReleasedSeconds, Is.EqualTo(-1f));
        }

        [Test]
        public void Tick_NoThreatLeftForAnySting_TheStingsEndAndTheSongLetsGo()
        {
            FakeSwarmTarget first = Threat(2f, 0f);
            FakeSwarmTarget second = Threat(0f, 3f);
            Cast();
            _forms.Tick(_song.ArriveSeconds + 0.1f, _hives);

            first.IsAlive = false;
            second.IsAlive = false;
            _forms.Tick(0.1f, _hives);

            Assert.That(Strike.ReleasedSeconds, Is.GreaterThanOrEqualTo(0f));
            float before = first.DamageTaken + second.DamageTaken;
            _forms.Tick(0.3f, _hives);
            Assert.That(first.DamageTaken + second.DamageTaken, Is.EqualTo(before));
        }

        [Test]
        public void Tick_OneTargetDiesWithNoNewThreat_ItsStingDoublesUpOnAnotherStingsTarget()
        {
            var first = new FakeSwarmTarget { Position = new Vector3(2f, 0f, 0f) };
            var second = new FakeSwarmTarget { Position = new Vector3(0f, 0f, 3f) };
            _aim.Threats = new ISwarmTarget[] { first, second };
            Cast();
            _forms.Tick(_song.ArriveSeconds + 0.1f, _hives);
            _aim.Threats = new ISwarmTarget[0];

            first.IsAlive = false;
            _forms.Tick(0.1f, _hives);

            Assert.That(_forms.StingCount, Is.EqualTo(2), "no sting ends while another still wraps a living beekeeper");
            Assert.That(_forms.StingTarget(0), Is.EqualTo(second.Position));
        }

        // ---- Nothing in reach ----

        [Test]
        public void Cast_NothingInReach_EveryStingWrapsTheAirAtTheAimForItsSeconds()
        {
            Build(1200);

            Cast();

            Assert.That(_forms.StingCount, Is.EqualTo(3));
            for (int sting = 0; sting < 3; sting++)
            {
                Assert.That(_forms.StingTarget(sting), Is.EqualTo(new Vector3(0f, 0f, 6f)));
            }

            _forms.Tick(_song.AirSeconds - 0.01f, _hives);
            Assert.That(Strike.ReleasedSeconds, Is.EqualTo(-1f));
            _forms.Tick(0.02f, _hives);
            Assert.That(Strike.ReleasedSeconds, Is.GreaterThanOrEqualTo(0f));
        }

        // ---- What the view reads ----

        [Test]
        public void StingCount_WithNoStingInTheStrikeSlot_IsZero_AndEveryTargetIsZero()
        {
            Assert.That(_forms.StingCount, Is.EqualTo(0));
            Assert.That(_forms.StingTarget(0), Is.EqualTo(Vector3.zero));

            SwarmSongContext context = Context();
            Make<TornadoSong>().Cast(in context);
            Assert.That(_forms.StingCount, Is.EqualTo(0));

            Threat(2f, 0f);
            Cast();
            Assert.That(_forms.StingCount, Is.EqualTo(2));
            Assert.That(_forms.StingTarget(2), Is.EqualTo(Vector3.zero), "past the count");
            _forms.Tick(_song.Seconds + _squads.ReturnSeconds + 0.1f, _hives);
            Assert.That(_forms.StingCount, Is.EqualTo(0), "the slot has emptied");
        }

        [Test]
        public void StingTarget_FollowsItsTargetAsItWalks()
        {
            FakeSwarmTarget walker = Threat(2f, 0f);
            Cast();

            walker.Position = new Vector3(3f, 0f, 1f);
            _forms.Tick(0.1f, _hives);

            Assert.That(_forms.StingTarget(0), Is.EqualTo(new Vector3(3f, 0f, 1f)));
            Assert.That((Vector3)Strike.ParamA, Is.EqualTo(new Vector3(3f, 0f, 1f)));
        }

        [TestCase(-1)]
        [TestCase(10)]
        public void StingTarget_PastTheMostStings_Throws(int index)
        {
            Assert.That(() => _forms.StingTarget(index), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        // ---- The hits it reports (plan section O, point 7) ----

        // Changed with the felt hit: the picture's rings squeeze on the arrival itself (WaspSwarm.compute), so the first hit is
        // felt there, not a squeeze later.
        [Test]
        public void SongLanded_EachSting_ReportsItsPulseOnTheArrivalAndOnEverySqueeze()
        {
            FakeSwarmTarget first = Threat(2f, 0f);
            FakeSwarmTarget second = Threat(0f, 3f);
            Cast();

            _forms.Tick(_song.ArriveSeconds - 0.01f, _hives);
            Assert.That(_hits, Is.Empty, "nothing before the arrival");
            _forms.Tick(0.02f, _hives);

            Assert.That(_hits.Count, Is.EqualTo(2), "the first hits land as the rings first squeeze, on the arrival");
            AssertHit(_hits[0], first.Position, Pulse(), false);
            AssertHit(_hits[1], second.Position, Pulse(), false);
            _forms.Tick(_song.SqueezeSeconds * 0.5f, _hives);
            Assert.That(_hits.Count, Is.EqualTo(2), "the next squeeze is a quarter second on");
            _forms.Tick(_song.SqueezeSeconds * 0.5f, _hives);

            Assert.That(_hits.Count, Is.EqualTo(4));
            AssertHit(_hits[2], first.Position, Pulse(), false);
            AssertHit(_hits[3], second.Position, Pulse(), false);
        }

        [Test]
        public void SongLanded_OnTheTickOfTheArrival_ReportsTheFirstHitAsTheStrikeLands()
        {
            FakeSwarmTarget only = Threat(2f, 0f);
            Build(400);
            int landings = 0;
            _forms.Landed += (form, point) => landings++;
            Cast();

            _forms.Tick(_song.ArriveSeconds, _hives);

            Assert.That(landings, Is.EqualTo(1));
            Assert.That(_hits.Count, Is.EqualTo(1), "the impact is felt on the frame it lands");
            AssertHit(_hits[0], only.Position, Pulse(), false);
            Assert.That(only.DamageTaken, Is.EqualTo(Pulse()).Within(1e-3f));
        }

        // At 48 frames a second every squeeze falls 0.8 of the way through a frame, so the frame that holds it is plain.
        [Test]
        public void SongLanded_TickedFrameByFrame_LandsOneHitOnTheFrameOfEachSqueezeAndNoneBetween()
        {
            const float frame = 1f / 48f;
            FakeSwarmTarget only = Threat(2f, 0f);
            Build(400);
            Cast();

            float seconds = 0f;
            while (Strike.ReleasedSeconds < 0f && seconds < _song.Seconds + 1f)
            {
                int before = _hits.Count;
                float from = seconds;
                _forms.Tick(frame, _hives);
                seconds += frame;
                Assert.That(_hits.Count - before, Is.EqualTo(SqueezesBetween(from, seconds)), "the frame ending at " + seconds + " s");
            }

            Assert.That(Strike.ReleasedSeconds, Is.GreaterThanOrEqualTo(0f), "the Sting never let go");
            float reported = 0f;
            foreach (SongHit hit in _hits)
            {
                reported += hit.Damage;
            }

            Assert.That(reported, Is.EqualTo(only.DamageTaken).Within(1e-3f));
            Assert.That(only.DamageTaken, Is.EqualTo(_song.DamagePerSecond * (_song.Seconds - _song.ArriveSeconds)).Within(1e-2f), "its damage a second from its arrival to its end");
        }

        // Changed with the felt hit: the kill now comes on the arrival's pulse.
        [Test]
        public void SongLanded_AStingThatKills_ReportsTheKillAtOnce()
        {
            FakeSwarmTarget weak = Threat(2f, 0f);
            weak.DiesAfter = Pulse() * 0.5f;
            Threat(0f, 3f);
            Cast();

            _forms.Tick(_song.ArriveSeconds, _hives);

            Assert.That(weak.IsAlive, Is.False);
            Assert.That(_hits.Count, Is.EqualTo(2));
            AssertHit(_hits[0], weak.Position, Pulse(), true);
            Assert.That(_hits[1].IsKill, Is.False, "the other sting's beekeeper lives");
        }

        [Test]
        public void SongLanded_OverTheWholeSongInOneStep_ReportsEveryPointOfDamageOnce()
        {
            FakeSwarmTarget only = Threat(2f, 0f);
            Build(400);
            Cast();

            _forms.Tick(_song.Seconds + 0.5f, _hives);

            float reported = 0f;
            foreach (SongHit hit in _hits)
            {
                reported += hit.Damage;
            }

            Assert.That(reported, Is.EqualTo(only.DamageTaken).Within(1e-3f), "every point of damage is reported once");
        }

        // ---- Helpers ----

        private void Build(int wasps)
        {
            JsonUtility.FromJsonOverwrite(
                "{\"<StartingCapacity>k__BackingField\":" + Mathf.Max(wasps, 1).ToString(CultureInfo.InvariantCulture)
                + ",\"<StartingWasps>k__BackingField\":" + wasps.ToString(CultureInfo.InvariantCulture) + "}",
                _swarmConfig);
            _body = new SwarmBody(_swarmConfig);
            _fury = new FuryMeter(_furyConfig);
            _forms = new SwarmFormMachine(_body, _fury, _squads);
            _hits = new List<SongHit>();
            _forms.SongLanded += _hits.Add;
        }

        private FakeSwarmTarget Threat(float x, float z)
        {
            var target = new FakeSwarmTarget { Position = new Vector3(x, 0f, z) };
            _enemies.Add(target);
            var threats = new List<ISwarmTarget>(_aim.Threats) { target };
            _aim.Threats = threats.ToArray();
            return target;
        }

        private void Cast(float damageScale = 1f)
        {
            SwarmSongContext context = Context(damageScale);
            _song.Cast(in context);
        }

        private SwarmSongContext Context(float damageScale = 1f)
        {
            return new SwarmSongContext(
                Vector3.zero, null, _aim.Point, _body, _forms, _fury, _hives, _enemies, null, _aim, false, 1f, damageScale);
        }

        // A whole pulse: one squeeze's damage at the level of the cast.
        private float Pulse()
        {
            return _song.DamagePerSecond * _song.SqueezeSeconds;
        }

        // How many of the picture's squeezes (every SqueezeSeconds from the arrival, before the end) fall after `from` and
        // by `to`.
        private int SqueezesBetween(float from, float to)
        {
            int count = 0;
            for (int squeeze = 0; ; squeeze++)
            {
                float at = _song.ArriveSeconds + (squeeze * _song.SqueezeSeconds);
                if (at >= _song.Seconds)
                {
                    return count;
                }

                if (at > from && at <= to)
                {
                    count++;
                }
            }
        }

        private static void AssertHit(SongHit hit, Vector3 position, float damage, bool isKill)
        {
            Assert.That(hit.Position, Is.EqualTo(position));
            Assert.That(hit.Damage, Is.EqualTo(damage).Within(1e-3f));
            Assert.That(hit.IsKill, Is.EqualTo(isKill));
            Assert.That(hit.IsBlocked, Is.False);
            Assert.That(hit.Form, Is.EqualTo(SwarmForm.Sting));
            Assert.That(hit.IsHeavy, Is.False, "the Sting is not one of the big hits");
        }

        private T Make<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }
    }
}
