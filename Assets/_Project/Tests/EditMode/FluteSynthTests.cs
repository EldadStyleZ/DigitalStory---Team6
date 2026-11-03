using NUnit.Framework;
using UnityEngine;

namespace PiperToy.Tests
{
    // The flute's sound is plain C#, so the tests can listen to it sample by sample.
    public sealed class FluteSynthTests
    {
        private const int Rate = 48000;

        private FluteSynth _synth;

        [SetUp]
        public void CreateSynth()
        {
            _synth = new FluteSynth(new FluteSound(), Rate, Flute.HoleCount);
        }

        private float[] Listen(float seconds)
        {
            var buffer = new float[(int)(seconds * Rate)];
            _synth.Fill(buffer, 1);
            return buffer;
        }

        private static float Loudest(float[] samples, int from = 0)
        {
            float loudest = 0f;

            for (int index = from; index < samples.Length; index++)
            {
                loudest = Mathf.Max(loudest, Mathf.Abs(samples[index]));
            }

            return loudest;
        }

        [Test]
        public void Fill_WithNoHoleHeld_IsSilent()
        {
            Assert.That(Loudest(Listen(0.1f)), Is.EqualTo(0f));
        }

        [Test]
        public void Fill_WithAHoleHeld_Sounds()
        {
            _synth.Play(1);

            Assert.That(Loudest(Listen(0.2f)), Is.GreaterThan(0.05f));
        }

        // The old game's pitches: D, then up the scale to the D an octave above.
        [TestCase(1, 587.33f)]
        [TestCase(2, 659.26f)]
        [TestCase(3, 783.99f)]
        [TestCase(4, 880f)]
        [TestCase(5, 987.77f)]
        [TestCase(6, 1174.66f)]
        public void Fill_EachHole_PlaysTheOldGamesPitch(int key, float hertz)
        {
            _synth.Play(key);
            Listen(0.2f);

            float[] second = Listen(1f);
            float band = Loudest(second) * 0.3f;
            int cycles = 0;
            bool isHigh = second[0] > 0f;

            // Counting only full swings from below the band to above it ignores the breath noise near zero.
            foreach (float sample in second)
            {
                if (!isHigh && sample > band)
                {
                    cycles++;
                    isHigh = true;
                }
                else if (isHigh && sample < -band)
                {
                    isHigh = false;
                }
            }

            Assert.That(cycles, Is.EqualTo(hertz).Within(hertz * 0.02f));
        }

        // A note that stops dead clicks; the old game's flute fades out instead.
        [Test]
        public void Fill_WhenTheHoleIsReleased_FadesOutInsteadOfStopping()
        {
            _synth.Play(1);
            float playing = Loudest(Listen(0.3f));

            _synth.Play(FluteSynth.Silent);
            float[] release = Listen(0.03f);
            float[] later = Listen(0.5f);

            Assert.That(Loudest(release, release.Length / 2), Is.GreaterThan(playing * 0.2f), "Still fading out.");
            Assert.That(Loudest(later, later.Length / 2), Is.LessThan(0.0005f), "Silent in the end.");
        }

        [Test]
        public void Fill_WhenTheHoleIsReleased_HasNoJump()
        {
            _synth.Play(1);
            float[] before = Listen(0.3f);

            _synth.Play(FluteSynth.Silent);
            float[] after = Listen(0.01f);

            float step = Mathf.Abs(after[0] - before[before.Length - 1]);
            Assert.That(step, Is.LessThan(Loudest(before) * 0.5f));
        }

        [Test]
        public void Fill_StereoBuffer_PutsTheSameSampleInBothChannels()
        {
            _synth.Play(3);
            var buffer = new float[2000];

            _synth.Fill(buffer, 2);

            for (int index = 0; index < buffer.Length; index += 2)
            {
                Assert.That(buffer[index + 1], Is.EqualTo(buffer[index]));
            }
        }
    }
}
