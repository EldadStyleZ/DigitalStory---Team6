using DigitalStory.Data;
using DigitalStory.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DigitalStory.Tests.PlayMode
{
    public sealed class FluteSynthTests
    {
        private const int SampleRate = 48000;
        private const int HalfASecond = SampleRate / 2;
        private const int Mono = 1;
        private const int Stereo = 2;
        private const int HoleCount = 6;

        private static SoundConfig Config => ScriptableObject.CreateInstance<SoundConfig>();

        [Test]
        public void Fill_NothingPlaying_IsSilent()
        {
            var synth = new FluteSynth(Config, SampleRate, HoleCount);

            float[] sound = Record(synth, HalfASecond);

            Assert.That(Loudness(sound), Is.EqualTo(0f).Within(1e-6f));
        }

        [Test]
        public void Fill_PlayingAHole_MakesSound()
        {
            var synth = new FluteSynth(Config, SampleRate, HoleCount);
            synth.Play(0);

            float[] sound = Record(synth, HalfASecond);

            Assert.That(Loudness(sound), Is.GreaterThan(0.01f));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void Fill_EachHole_SoundsAtItsPitch(int hole)
        {
            SoundConfig config = Config;
            var synth = new FluteSynth(config, SampleRate, HoleCount);
            synth.Play(hole);
            Record(synth, SampleRate / 10);

            float[] sound = Record(synth, HalfASecond);

            float pitch = config.HertzOfHole(hole);
            Assert.That(StrengthAt(sound, pitch), Is.GreaterThan(StrengthAt(sound, pitch * 1.12f) * 4f));
            Assert.That(StrengthAt(sound, pitch), Is.GreaterThan(StrengthAt(sound, pitch / 1.12f) * 4f));
        }

        [Test]
        public void Fill_Always_StaysInRange()
        {
            var synth = new FluteSynth(Config, SampleRate, HoleCount);
            synth.Play(5);

            foreach (float sample in Record(synth, SampleRate))
            {
                Assert.That(sample, Is.InRange(-1f, 1f));
            }
        }

        [Test]
        public void Fill_AfterStopping_FadesToSilence()
        {
            var synth = new FluteSynth(Config, SampleRate, HoleCount);
            synth.Play(2);
            Record(synth, HalfASecond);

            synth.Play(FluteSynth.Silent);
            Record(synth, HalfASecond);
            float[] sound = Record(synth, SampleRate / 10);

            Assert.That(Loudness(sound), Is.LessThan(0.0001f));
        }

        [Test]
        public void Fill_StartingANote_DoesNotClick()
        {
            var synth = new FluteSynth(Config, SampleRate, HoleCount);
            synth.Play(0);

            float[] sound = Record(synth, 64);

            Assert.That(Mathf.Abs(sound[0]), Is.LessThan(0.01f));
        }

        [Test]
        public void Fill_ChangingHole_DipsThenRecoversLikeThePrototype()
        {
            SoundConfig config = Config;
            var synth = new FluteSynth(config, SampleRate, HoleCount);
            synth.Play(0);
            Record(synth, HalfASecond);
            float held = Loudness(Record(synth, SampleRate / 10));

            synth.Play(3);
            Record(synth, SampleRate * 25 / 1000);
            float dipped = Loudness(Record(synth, SampleRate * 10 / 1000));
            Record(synth, SampleRate * 100 / 1000);
            float recovered = Loudness(Record(synth, SampleRate / 10));

            Assert.That(dipped / held, Is.EqualTo(config.NoteStartVolume / config.FluteVolume).Within(0.1f));
            Assert.That(recovered / held, Is.EqualTo(1f).Within(0.1f));
        }

        [Test]
        public void Constructor_SoundCoversTooFewHoles_FailsWithAClearError()
        {
            SoundConfig config = Config;

            Assert.That(
                () => new FluteSynth(config, SampleRate, config.HoleCount + 1),
                Throws.ArgumentException.With.Message.Contains("hole"));
        }

        [Test]
        public void Silent_MeansTheSameAsTheFlutesNoHole()
        {
            Assert.That(FluteSynth.Silent, Is.EqualTo(DigitalStory.Logic.Flute.NoHole));
        }

        [Test]
        public void Fill_ManyChannels_WritesTheSameToEach()
        {
            var synth = new FluteSynth(Config, SampleRate, HoleCount);
            synth.Play(1);
            var buffer = new float[2048];

            synth.Fill(buffer, Stereo);

            for (int sample = 0; sample < buffer.Length; sample += Stereo)
            {
                Assert.That(buffer[sample + 1], Is.EqualTo(buffer[sample]));
            }
        }

        [Test]
        public void Fill_AnyBuffer_AllocatesNothing()
        {
            var synth = new FluteSynth(Config, SampleRate, HoleCount);
            synth.Play(1);
            var buffer = new float[2048];

            Assert.That(() => synth.Fill(buffer, Stereo), Is.Not.AllocatingGCMemory());
        }

        private static float[] Record(FluteSynth synth, int samples)
        {
            var sound = new float[samples];
            synth.Fill(sound, Mono);
            return sound;
        }

        private static float Loudness(float[] sound)
        {
            float total = 0f;

            foreach (float sample in sound)
            {
                total += sample * sample;
            }

            return Mathf.Sqrt(total / sound.Length);
        }

        // The Goertzel method: measures how much of one frequency a sound holds.
        private static float StrengthAt(float[] sound, float hertz)
        {
            float coefficient = 2f * Mathf.Cos(2f * Mathf.PI * hertz / SampleRate);
            float previous = 0f;
            float beforeThat = 0f;

            foreach (float sample in sound)
            {
                float current = sample + (coefficient * previous) - beforeThat;
                beforeThat = previous;
                previous = current;
            }

            return (previous * previous) + (beforeThat * beforeThat) - (coefficient * previous * beforeThat);
        }
    }
}
