using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DigitalStory.Tests.EditMode
{
    public sealed class NightSessionTests
    {
        private const float Step = 1f / 60f;
        private const float PiperTolerance = 0.05f;
        private const float MeanRatTolerance = 0.5f;
        private const float CharmTolerance = 0.01f;
        private const float AngleToleranceDegrees = 1f;

        // A flock magnifies rounding. Past this phase one run cannot be compared exactly: the prototype
        // drifted 2.7 units from itself by phase 4, and drowned 12 rats instead of 15, just from rounding to float.
        private const int ExactRatPhases = 3;

        // How many standard errors an average may differ by, plus a floor for phases where nothing varies.
        private const double AllowedErrors = 4d;
        private const double SmallestAllowance = 0.05d;

        private static readonly PiperIntent Idle = new PiperIntent(Vector2.zero, 0, 0);

        [Test]
        public void Constructor_NewNight_StartsFromItsRules()
        {
            NightRules night = TestRules.PrototypeNight(0);

            NightSession session = NewSession(night);

            Assert.That(session.Swarm.AliveCount, Is.EqualTo(night.RatCount));
            Assert.That(session.Quota, Is.EqualTo(night.Quota));
            Assert.That(session.TimeLeft, Is.EqualTo(night.TimeLimit));
            Assert.That(session.Outcome, Is.EqualTo(NightOutcome.InProgress));
        }

        [TestCase(0f, 0f, 0f)]
        [TestCase(30f, 0.3f, 0f)]
        [TestCase(60f, 0.6f, 0f)]
        [TestCase(70f, 0.7f, 0.25f)]
        [TestCase(80f, 0.8f, 0.5f)]
        [TestCase(100f, 1f, 1f)]
        [TestCase(120f, 1f, 1f)]
        public void Dawn_AsTheNightPasses_FollowsThePrototypesCurve(float secondsGone, float progress, float dawn)
        {
            NightSession session = NewSession(new NightRules(1234, 70, 45, 100f, 3));

            if (secondsGone > 0f)
            {
                session.Tick(Idle, secondsGone);
            }

            Assert.That(session.NightProgress, Is.EqualTo(progress).Within(CharmTolerance));
            Assert.That(session.Dawn, Is.EqualTo(dawn).Within(CharmTolerance));
        }

        [TestCase(81f, false)]
        [TestCase(83f, true)]
        public void IsNearDawn_LateInTheNight_TurnsOn(float secondsGone, bool expected)
        {
            NightSession session = NewSession(new NightRules(1234, 70, 45, 100f, 3));

            session.Tick(Idle, secondsGone);

            Assert.That(session.IsNearDawn, Is.EqualTo(expected));
        }

        [Test]
        public void Constructor_QuotaAboveRatCount_FailsWithAClearError()
        {
            var night = new NightRules(1234, 70, 71, 130f, 3);

            Assert.That(() => NewSession(night), Throws.ArgumentException.With.Message.Contains(nameof(NightRules.Quota)));
        }

        [Test]
        public void Constructor_QuotaEqualToRatCount_IsAllowed()
        {
            var night = new NightRules(1234, 70, 70, 130f, 3);

            NightSession session = NewSession(night);

            Assert.That(session.Outcome, Is.EqualTo(NightOutcome.InProgress));
        }

        [Test]
        public void Tick_PressedHole_IsRememberedAsLastPressed()
        {
            NightSession session = NewSession(TestRules.PrototypeNight(0));

            session.Tick(new PiperIntent(Vector2.zero, HolesHeld(1, 4), 1 << 1), Step);

            Assert.That(session.Flute.CurrentHole, Is.EqualTo(1));
        }

        [Test]
        public void Tick_OutOfTime_EndsAtDawnOnce()
        {
            NightRules night = new NightRules(1234, 70, 45, 0.5f, 3);
            NightSession session = NewSession(night);
            var endings = 0;
            session.Ended += _ => endings++;

            for (int tick = 0; tick < 60; tick++)
            {
                session.Tick(Idle, Step);
            }

            Assert.That(session.Outcome, Is.EqualTo(NightOutcome.DawnBroke));
            Assert.That(endings, Is.EqualTo(1));
        }

        [Test]
        public void Tick_AfterTheNightEnds_ChangesNothing()
        {
            NightRules night = new NightRules(1234, 70, 45, 0.01f, 3);
            NightSession session = NewSession(night);
            session.Tick(Idle, Step);
            float timeAtEnd = session.TimeLeft;

            session.Tick(new PiperIntent(Vector2.left, 0, 0), Step);

            Assert.That(session.TimeLeft, Is.EqualTo(timeAtEnd));
        }

        [Test]
        public void Tick_ScriptedRun_MovesThePiperLikeThePrototype()
        {
            NightSession session = NewSession(TestRules.PrototypeNight(0));

            foreach (PrototypeValues.Snapshot expected in PrototypeValues.ScriptedRun)
            {
                RunPhase(session, expected);

                Assert.That(session.Piper.Position.x, Is.EqualTo((float)expected.PiperX).Within(PiperTolerance));
                Assert.That(session.Piper.Position.y, Is.EqualTo((float)expected.PiperY).Within(PiperTolerance));
                Assert.That(AngleBetween(session.Piper.Facing, (float)expected.PiperFacing), Is.LessThan(AngleToleranceDegrees));
                Assert.That(session.Breath.Current, Is.EqualTo((float)expected.Breath).Within(PiperTolerance));
                Assert.That(session.TimeLeft, Is.EqualTo((float)expected.TimeLeft).Within(PiperTolerance));
            }
        }

        [Test]
        public void Tick_ScriptedRunEarlyPhases_MovesRatsLikeThePrototype()
        {
            NightSession session = NewSession(TestRules.PrototypeNight(0));

            for (int phase = 0; phase < ExactRatPhases; phase++)
            {
                PrototypeValues.Snapshot expected = PrototypeValues.ScriptedRun[phase];
                RunPhase(session, expected);

                Assert.That(MeanRatX(session), Is.EqualTo((float)expected.MeanRatX).Within(MeanRatTolerance));
                Assert.That(MeanCharm(session), Is.EqualTo((float)expected.MeanCharm).Within(CharmTolerance));
            }
        }

        [Test]
        public void Tick_ManyScriptedRuns_AveragesMatchThePrototype()
        {
            int phases = PrototypeValues.ScriptedRun.Length;
            var ratX = new double[phases];
            var charm = new double[phases];
            var drowned = new double[phases];

            for (int run = 0; run < PrototypeValues.AverageRunCount; run++)
            {
                NightSession session = NewSession(TestRules.PrototypeNight(0), (uint)(PrototypeValues.AverageFirstSeed + run));

                for (int phase = 0; phase < phases; phase++)
                {
                    RunPhase(session, PrototypeValues.ScriptedRun[phase]);
                    ratX[phase] += MeanRatX(session) / PrototypeValues.AverageRunCount;
                    charm[phase] += MeanCharm(session) / PrototypeValues.AverageRunCount;
                    drowned[phase] += (double)session.Swarm.DrownedCount / PrototypeValues.AverageRunCount;
                }
            }

            for (int phase = 0; phase < phases; phase++)
            {
                AssertAverage(ratX[phase], PrototypeValues.AverageMeanRatX[phase], PrototypeValues.AverageMeanRatXError[phase], phase);
                AssertAverage(charm[phase], PrototypeValues.AverageMeanCharm[phase], PrototypeValues.AverageMeanCharmError[phase], phase);
                AssertAverage(drowned[phase], PrototypeValues.AverageDrowned[phase], PrototypeValues.AverageDrownedError[phase], phase);
            }
        }

        [Test]
        public void Tick_LeadingRatsToTheRiver_WinsTheNight()
        {
            var night = new NightRules(1234, 70, 10, 130f, 3);
            NightSession session = NewSession(night);
            NightOutcome? ending = null;
            session.Ended += outcome => ending = outcome;

            foreach (PrototypeValues.Snapshot phase in PrototypeValues.ScriptedRun)
            {
                RunPhase(session, phase);
            }

            Assert.That(ending, Is.EqualTo(NightOutcome.Won));
        }

        [Test]
        public void Tick_ManyScriptedRunsWithACat_AveragesMatchThePrototype()
        {
            int phases = PrototypeValues.ScriptedRun.Length;
            var lost = new double[phases];
            var charm = new double[phases];

            for (int run = 0; run < PrototypeValues.AverageRunCount; run++)
            {
                NightSession session = NewSession(TestRules.PrototypeNight(1), (uint)(PrototypeValues.AverageFirstSeed + run));

                for (int phase = 0; phase < phases; phase++)
                {
                    RunPhase(session, PrototypeValues.ScriptedRun[phase]);
                    lost[phase] += session.Swarm.LostCount;
                    charm[phase] += MeanCharm(session);
                }
            }

            for (int phase = 0; phase < phases; phase++)
            {
                double averageLost = lost[phase] / PrototypeValues.AverageRunCount;
                double averageCharm = charm[phase] / PrototypeValues.AverageRunCount;
                AssertAverage(averageLost, PrototypeValues.CatNightAverageLost[phase], PrototypeValues.CatNightAverageLostError[phase], phase);
                AssertAverage(averageCharm, PrototypeValues.CatNightAverageMeanCharm[phase], PrototypeValues.CatNightAverageMeanCharmError[phase], phase);
            }
        }

        [Test]
        public void Tick_PlayingScareWithinReachOfACat_SendsItRunning()
        {
            NightSession session = SessionWhereScaresReachEverywhere();
            int scareHole = 1 << 4;

            session.Tick(new PiperIntent(Vector2.zero, scareHole, scareHole), Step);

            Assert.That(session.Cats[0].FleeLeft, Is.GreaterThan(0f));
        }

        [Test]
        public void Tick_CallingWithinReachOfACat_DoesNotScareIt()
        {
            NightSession session = SessionWhereScaresReachEverywhere();
            int callHole = 1 << 0;

            session.Tick(new PiperIntent(Vector2.zero, callHole, callHole), Step);

            Assert.That(session.Cats[0].FleeLeft, Is.EqualTo(0f));
        }

        [Test]
        public void Constructor_NightWithCats_SpawnsThem()
        {
            NightSession session = NewSession(TestRules.PrototypeNight(2));

            Assert.That(session.Cats.Count, Is.EqualTo(TestRules.PrototypeNight(2).CatCount));
        }

        [Test]
        public void Tick_FullNight_AllocatesNothing()
        {
            NightSession session = NewSession(TestRules.PrototypeNight(3));
            var intent = new PiperIntent(Vector2.left, 1, 1);

            Assert.That(() => session.Tick(intent, Step), Is.Not.AllocatingGCMemory());
        }

        private static NightSession SessionWhereScaresReachEverywhere()
        {
            var catConfig = ScriptableObject.CreateInstance<CatConfig>();
            JsonUtility.FromJsonOverwrite("{\"<ScareExtraRange>k__BackingField\":100000}", catConfig);
            GameRules rules = TestRules.With(cats: catConfig);
            NightRules night = TestRules.PrototypeNight(1);
            return new NightSession(rules, night, new Mulberry32Random((uint)night.Seed), new FixedRandomSource());
        }

        private static NightSession NewSession(NightRules night, uint simulationSeed = PrototypeValues.ScriptedRandomSeed)
        {
            var townRandom = new Mulberry32Random((uint)night.Seed);
            return new NightSession(TestRules.Default, night, townRandom, new Mulberry32Random(simulationSeed));
        }

        private static void AssertAverage(double actual, double expected, double standardError, int phase)
        {
            double allowance = (standardError * AllowedErrors) + SmallestAllowance;
            Assert.That(actual, Is.EqualTo(expected).Within(allowance), $"Phase {phase + 1}");
        }

        private static void RunPhase(NightSession session, PrototypeValues.Snapshot phase)
        {
            int heldHoles = phase.Hole >= 0 ? 1 << phase.Hole : 0;
            var intent = new PiperIntent(new Vector2(phase.MoveX, phase.MoveY), heldHoles, 0);

            for (int tick = 0; tick < phase.Ticks; tick++)
            {
                session.Tick(intent, PrototypeValues.ScriptedStep);
            }
        }

        private static float MeanRatX(NightSession session)
        {
            float sum = 0f;

            for (int index = 0; index < session.Swarm.Count; index++)
            {
                sum += session.Swarm[index].IsAlive ? session.Swarm[index].Position.x : 0f;
            }

            return sum / session.Swarm.AliveCount;
        }

        private static float MeanCharm(NightSession session)
        {
            float sum = 0f;

            for (int index = 0; index < session.Swarm.Count; index++)
            {
                sum += session.Swarm[index].IsAlive ? session.Swarm[index].Charm : 0f;
            }

            return sum / session.Swarm.AliveCount;
        }

        // A half turn can wrap either way, so +pi and -pi are the same facing.
        private static float AngleBetween(float first, float second)
        {
            return Mathf.Abs(Mathf.DeltaAngle(first * Mathf.Rad2Deg, second * Mathf.Rad2Deg));
        }

        private static int HolesHeld(int first, int second) => (1 << first) | (1 << second);
    }
}
