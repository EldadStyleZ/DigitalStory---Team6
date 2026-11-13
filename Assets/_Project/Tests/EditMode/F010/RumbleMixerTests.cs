using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;

namespace DigitalStory.Tests.EditMode
{
    public sealed class RumbleMixerTests
    {
        private const float Tolerance = 0.001f;
        private const float Frame = 0.02f;

        [Test]
        public void Tick_NoPulse_IsStill()
        {
            var mixer = new RumbleMixer();

            MotorSpeeds speeds = mixer.Tick(Frame);

            Assert.That(speeds.Low, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(speeds.High, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(speeds.IsStill, Is.True);
        }

        [Test]
        public void Tick_OnePulse_GivesItsSpeeds()
        {
            var mixer = new RumbleMixer();
            mixer.Add(new RumblePulse(0.4f, 0.7f, 0.1f));

            MotorSpeeds speeds = mixer.Tick(Frame);

            Assert.That(speeds.Low, Is.EqualTo(0.4f).Within(Tolerance));
            Assert.That(speeds.High, Is.EqualTo(0.7f).Within(Tolerance));
        }

        [Test]
        public void Tick_TwoPulses_CombineByTheStrongerOfEachMotor()
        {
            var mixer = new RumbleMixer();
            mixer.Add(new RumblePulse(0.2f, 0.8f, 0.1f));
            mixer.Add(new RumblePulse(0.6f, 0.1f, 0.1f));

            MotorSpeeds speeds = mixer.Tick(Frame);

            Assert.That(speeds.Low, Is.EqualTo(0.6f).Within(Tolerance));
            Assert.That(speeds.High, Is.EqualTo(0.8f).Within(Tolerance));
        }

        [Test]
        public void Tick_PastAPulsesSeconds_IsStill()
        {
            var mixer = new RumbleMixer();
            mixer.Add(new RumblePulse(0.5f, 0.5f, 0.1f));

            mixer.Tick(0.06f);
            mixer.Tick(0.06f);
            MotorSpeeds speeds = mixer.Tick(Frame);

            Assert.That(speeds.IsStill, Is.True);
        }

        [Test]
        public void Tick_WithinAPulsesSeconds_StillGivesIt()
        {
            var mixer = new RumbleMixer();
            mixer.Add(new RumblePulse(0.5f, 0.5f, 0.1f));

            mixer.Tick(0.06f);
            MotorSpeeds speeds = mixer.Tick(Frame);

            Assert.That(speeds.Low, Is.EqualTo(0.5f).Within(Tolerance));
        }

        // A pulse shorter than a frame is still felt for the frame it was added in.
        [Test]
        public void Tick_PulseShorterThanTheFrame_IsFeltOnce()
        {
            var mixer = new RumbleMixer();
            mixer.Add(new RumblePulse(0f, 0.3f, 0.01f));

            MotorSpeeds first = mixer.Tick(0.1f);
            MotorSpeeds second = mixer.Tick(0.1f);

            Assert.That(first.High, Is.EqualTo(0.3f).Within(Tolerance));
            Assert.That(second.IsStill, Is.True);
        }

        [Test]
        public void Tick_ShortPulseEnds_TheLongerOneGoesOn()
        {
            var mixer = new RumbleMixer();
            mixer.Add(new RumblePulse(0.9f, 0f, 0.05f));
            mixer.Add(new RumblePulse(0.3f, 0f, 1f));

            mixer.Tick(0.1f);
            MotorSpeeds speeds = mixer.Tick(Frame);

            Assert.That(speeds.Low, Is.EqualTo(0.3f).Within(Tolerance));
        }

        [Test]
        public void Clear_StopsEveryPulse()
        {
            var mixer = new RumbleMixer();
            mixer.Add(new RumblePulse(0.5f, 0.5f, 1f));
            mixer.Add(new RumblePulse(0.2f, 0.9f, 1f));

            mixer.Clear();

            Assert.That(mixer.Tick(Frame).IsStill, Is.True);
        }

        [Test]
        public void Add_SilentOrEmptyPulse_IsIgnored()
        {
            var mixer = new RumbleMixer();
            mixer.Add(new RumblePulse(0f, 0f, 1f));
            mixer.Add(new RumblePulse(1f, 1f, 0f));

            Assert.That(mixer.Tick(Frame).IsStill, Is.True);
        }

        // More pulses than the mixer holds: the newest replaces the one nearest its end, so a fresh thump is never lost.
        [Test]
        public void Add_MorePulsesThanItHolds_KeepsTheNewest()
        {
            var mixer = new RumbleMixer();
            for (int pulse = 0; pulse < RumbleMixer.Capacity; pulse++)
            {
                mixer.Add(new RumblePulse(0.1f, 0.1f, 1f));
            }

            mixer.Add(new RumblePulse(0.8f, 0.2f, 1f));

            Assert.That(mixer.Tick(Frame).Low, Is.EqualTo(0.8f).Within(Tolerance));
        }

        // Songs-v2 R, small fix 4: the last hive's long rumble starts on the tick the match ends, and plays out.
        [Test]
        public void Close_WithAPulseRunning_LetsItPlayOutThenIsStill()
        {
            var mixer = new RumbleMixer();
            mixer.Add(new RumblePulse(0.7f, 0.1f, 0.9f));

            mixer.Close();
            MotorSpeeds during = mixer.Tick(0.5f);
            MotorSpeeds late = mixer.Tick(0.3f);
            mixer.Tick(0.2f);
            MotorSpeeds after = mixer.Tick(Frame);

            Assert.That(mixer.IsClosed, Is.True);
            Assert.That(during.Low, Is.EqualTo(0.7f).Within(Tolerance));
            Assert.That(late.Low, Is.EqualTo(0.7f).Within(Tolerance), "0.8 s into a 0.9 s pulse");
            Assert.That(after.IsStill, Is.True);
        }

        [Test]
        public void Close_ThenAPulse_IsRefusedUntilOpen()
        {
            var mixer = new RumbleMixer();

            mixer.Close();
            mixer.Add(new RumblePulse(0.4f, 0.4f, 0.5f));
            MotorSpeeds closed = mixer.Tick(Frame);
            mixer.Open();
            mixer.Add(new RumblePulse(0.4f, 0.4f, 0.5f));
            MotorSpeeds open = mixer.Tick(Frame);

            Assert.That(closed.IsStill, Is.True);
            Assert.That(mixer.IsClosed, Is.False);
            Assert.That(open.Low, Is.EqualTo(0.4f).Within(Tolerance));
        }

        [Test]
        public void Clear_WhileClosed_StopsThePulseAtOnce()
        {
            var mixer = new RumbleMixer();
            mixer.Add(new RumblePulse(0.7f, 0.1f, 0.9f));
            mixer.Close();

            mixer.Clear();

            Assert.That(mixer.Tick(Frame).IsStill, Is.True);
        }
    }
}
