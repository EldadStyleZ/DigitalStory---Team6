using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// The core's recent past at a fixed step, which each wasp of the free swarm reads with its own delay (plan section Q):
    /// a start, a stop or a turn reaches the head of the swarm at once and its tail later.
    /// </summary>
    public sealed class CoreHistoryTests
    {
        private const float Tolerance = 0.001f;
        private const int Samples = 11;
        private const float Step = 0.1f;
        private const float Speed = 4.5f;

        private readonly Vector4[] _samples = new Vector4[Samples];

        [TestCase(1)]
        [TestCase(0)]
        [TestCase(-2)]
        public void Constructor_FewerThanTwoSamples_Throws(int count)
        {
            Assert.That(() => new CoreHistory(count, Step), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [TestCase(0f)]
        [TestCase(-0.1f)]
        public void Constructor_StepNotAboveZero_Throws(float step)
        {
            Assert.That(() => new CoreHistory(Samples, step), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void SpanSeconds_IsFromNowToTheOldestSample()
        {
            Assert.That(new CoreHistory(Samples, Step).SpanSeconds, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void CopyTo_AfterAReset_ReadsAsACoreThatHasStoodStill()
        {
            var history = new CoreHistory(Samples, Step);

            history.Reset(0.7f, 2f);
            history.CopyTo(_samples);

            for (int back = 0; back < Samples; back++)
            {
                Assert.That(_samples[back].x, Is.EqualTo(0f).Within(Tolerance), $"travelled {back} steps back");
                Assert.That(_samples[back].y, Is.EqualTo(0.7f).Within(Tolerance), $"wound {back} steps back");
                Assert.That(_samples[back].z, Is.EqualTo(0f).Within(Tolerance), $"swing {back} steps back");
            }
        }

        [Test]
        public void Record_ASteadyWalk_EachSampleHoldsTheMetresWalkedSinceThen()
        {
            var history = new CoreHistory(Samples, Step);
            Walk(history, 0f, 2f, Speed, 1f / 60f);

            history.CopyTo(_samples);

            for (int back = 0; back < Samples; back++)
            {
                Assert.That(_samples[back].x, Is.EqualTo(Speed * Step * back).Within(Tolerance), $"{back} steps back");
            }
        }

        [Test]
        public void Record_TheSameMotionAt60And120FramesASecond_GivesTheSameSamples()
        {
            var slow = new CoreHistory(Samples, Step);
            var fast = new CoreHistory(Samples, Step);
            var fastSamples = new Vector4[Samples];

            // The core speeds up, so its odometer is a curve, and the wound and the swing ramp.
            Accelerate(slow, 1.37f, 1f / 60f);
            Accelerate(fast, 1.37f, 1f / 120f);
            slow.CopyTo(_samples);
            fast.CopyTo(fastSamples);

            for (int back = 0; back < Samples; back++)
            {
                Assert.That(fastSamples[back].x, Is.EqualTo(_samples[back].x).Within(Tolerance), $"travelled {back} steps back");
                Assert.That(fastSamples[back].y, Is.EqualTo(_samples[back].y).Within(Tolerance), $"wound {back} steps back");
                Assert.That(fastSamples[back].z, Is.EqualTo(_samples[back].z).Within(Tolerance), $"swing {back} steps back");
            }
        }

        [Test]
        public void Record_AStopAfterAWalk_TheOlderSamplesStillHoldTheWalk()
        {
            var history = new CoreHistory(Samples, Step);
            double odometer = Walk(history, 0f, 1f, 4f, 1f / 60f);
            Walk(history, (float)odometer, 0.3f, 0f, 1f / 60f);

            history.CopyTo(_samples);

            Assert.That(_samples[3].x, Is.EqualTo(0f).Within(Tolerance), "the core has not moved for the last 0.3 s");
            Assert.That(_samples[5].x, Is.EqualTo(0.8f).Within(Tolerance), "0.5 s ago it had 0.2 s of walking still to do");
            Assert.That(_samples[10].x, Is.EqualTo(2.8f).Within(Tolerance), "1 s ago it had 0.7 s of walking still to do");
        }

        [Test]
        public void Record_ARampingWoundAndSwing_TheOlderSamplesHoldTheOlderValues()
        {
            var history = new CoreHistory(Samples, Step);
            const float frame = 1f / 60f;
            float time = 0f;
            for (int index = 0; index < 90; index++)
            {
                time += frame;
                history.Record(frame, 0.0, time * 0.5f, time * 2f);
            }

            history.CopyTo(_samples);

            for (int back = 0; back < Samples; back++)
            {
                float then = time - (back * Step);
                Assert.That(_samples[back].y, Is.EqualTo(then * 0.5f).Within(Tolerance), $"wound {back} steps back");
                Assert.That(_samples[back].z, Is.EqualTo(-back * Step * 2f).Within(Tolerance), $"swing {back} steps back, relative to now");
            }
        }

        [Test]
        public void Record_LessTimeThanTheSpan_OlderMomentsReadAsTheReset()
        {
            var history = new CoreHistory(Samples, Step);
            history.Reset(0.3f, 0f);
            Walk(history, 0f, 0.2f, Speed, 1f / 60f);

            history.CopyTo(_samples);

            Assert.That(_samples[6].x, Is.EqualTo(Speed * 0.2f).Within(Tolerance));
            Assert.That(_samples[10].x, Is.EqualTo(Speed * 0.2f).Within(Tolerance));
            Assert.That(_samples[10].y, Is.EqualTo(0.3f).Within(Tolerance));
        }

        [Test]
        public void Record_AFrameLongerThanTheSpan_KeepsTheNewestSamplesOnTheLine()
        {
            var history = new CoreHistory(Samples, Step);

            history.Record(5f, 5.0, 0f, 0f);
            history.CopyTo(_samples);

            for (int back = 0; back < Samples; back++)
            {
                Assert.That(_samples[back].x, Is.EqualTo(back * Step).Within(Tolerance), $"{back} steps back");
            }
        }

        [Test]
        public void Record_AFrameOfNoTime_ChangesOnlyTheStateNow()
        {
            var history = new CoreHistory(Samples, Step);

            history.Record(0f, 1.0, 0.5f, 0f);
            history.CopyTo(_samples);

            Assert.That(_samples[0].x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_samples[0].y, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(_samples[1].x, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(_samples[1].y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Record_NegativeTime_Throws()
        {
            var history = new CoreHistory(Samples, Step);

            Assert.That(() => history.Record(-0.01f, 0.0, 0f, 0f), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void CopyTo_Missing_Throws()
        {
            Assert.That(() => new CoreHistory(Samples, Step).CopyTo(null), Throws.ArgumentNullException);
        }

        [Test]
        public void CopyTo_TooShort_Throws()
        {
            Assert.That(() => new CoreHistory(Samples, Step).CopyTo(new Vector4[Samples - 1]), Throws.ArgumentException);
        }

        // Walks at a steady speed from an odometer reading, frame by frame, and returns the reading at the end.
        private static double Walk(CoreHistory history, float from, float seconds, float speed, float frame)
        {
            double odometer = from;
            int frames = Mathf.RoundToInt(seconds / frame);
            for (int index = 0; index < frames; index++)
            {
                odometer += speed * frame;
                history.Record(frame, odometer, 0f, 0f);
            }

            return odometer;
        }

        // The odometer runs as t squared, the wound as t / 2 and the swing as 3t.
        private static void Accelerate(CoreHistory history, float seconds, float frame)
        {
            int frames = Mathf.RoundToInt(seconds / frame);
            for (int index = 1; index <= frames; index++)
            {
                float time = index * frame;
                history.Record(frame, time * time, time * 0.5f, time * 3f);
            }
        }
    }
}
