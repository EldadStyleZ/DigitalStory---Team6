using System;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// The synthesised note voices of plan section AC, rendered into buffers: the pluck, the bell and the reed sound the holes'
    /// pitches, start from silence without a step, ring down or hold as they should, never click on a strike, a run, a lift
    /// or a release, and allocate nothing on the audio thread. Their level against the flute is checked in PlayMode
    /// (SwarmFluteVoiceTests), where the flute's synth lives.
    /// </summary>
    public sealed class NoteSynthTests
    {
        private const int Rate = 48000;
        private const int Block = 512;

        // D5 E5 G5 A5 and up, the flute's pitches.
        private static readonly float[] HoleHertz = { 587.33f, 659.26f, 783.99f, 880f, 987.77f, 1174.66f };

        // A smooth signal's second difference stays small next to its peak; a step makes it about as big as the step. The
        // flute itself reaches about 0.1 of its peak from its breath noise.
        private const double MostJerkShare = 0.15;

        public enum Voice
        {
            Pluck,
            Bell,
            Reed,
        }

        private static INoteSynth Make(Voice voice)
        {
            switch (voice)
            {
                case Voice.Pluck:
                    return new StruckSynth(StruckTone.Kalimba, HoleHertz, 0.012f, Rate);
                case Voice.Bell:
                    return new StruckSynth(StruckTone.SoftBell, HoleHertz, 0.012f, Rate);
                default:
                    return new ReedSynth(ReedTone.WaspHum, HoleHertz, 0.012f, Rate);
            }
        }

        [TestCase(Voice.Pluck)]
        [TestCase(Voice.Bell)]
        [TestCase(Voice.Reed)]
        public void Mix_BeforeAnyNote_AddsNothing(Voice voice)
        {
            float[] samples = Render(Make(voice), 0.1f);

            Assert.That(Peak(samples, 0f, 0.1f), Is.EqualTo(0d));
        }

        [TestCase(Voice.Pluck, 0)]
        [TestCase(Voice.Pluck, 3)]
        [TestCase(Voice.Bell, 1)]
        [TestCase(Voice.Bell, 2)]
        [TestCase(Voice.Reed, 0)]
        [TestCase(Voice.Reed, 3)]
        public void Play_AHole_SoundsItsPitch(Voice voice, int hole)
        {
            float[] samples = Render(Make(voice), 0.4f, (0f, synth => synth.Play(hole)));

            double hertz = Pitch(samples, 0.05f, 0.3f, HoleHertz[hole]);

            Assert.That(hertz, Is.EqualTo(HoleHertz[hole]).Within(HoleHertz[hole] * 0.01));
        }

        [TestCase(Voice.Pluck)]
        [TestCase(Voice.Bell)]
        [TestCase(Voice.Reed)]
        public void Play_FromSilence_SwellsInWithoutAStep(Voice voice)
        {
            float[] samples = Render(Make(voice), 0.3f, (0f, synth => synth.Play(1)));
            double peak = Peak(samples, 0f, 0.3f);

            Assert.That(Math.Abs(samples[0]), Is.LessThan(peak * 0.01), "the first sample");
            Assert.That(Peak(samples, 0f, 0.0005f), Is.LessThan(peak * 0.3), "the first half millisecond");
            Assert.That(Jerk(samples, 0f, 0.3f), Is.LessThan(peak * MostJerkShare));
        }

        [TestCase(Voice.Pluck)]
        [TestCase(Voice.Bell)]
        public void StruckNote_WhileHeld_RingsDown(Voice voice)
        {
            float[] samples = Render(Make(voice), 1.2f, (0f, synth => synth.Play(0)));

            Assert.That(Rms(samples, 1f, 1.1f), Is.LessThan(Rms(samples, 0.02f, 0.12f) * 0.6));
            Assert.That(Rms(samples, 1f, 1.1f), Is.GreaterThan(0d), "a held note still rings after a second");
        }

        [TestCase(Voice.Pluck)]
        [TestCase(Voice.Bell)]
        public void StruckNote_LetGo_FadesFasterThanHeld_AndFallsSilent(Voice voice)
        {
            float[] held = Render(Make(voice), 0.8f, (0f, synth => synth.Play(0)));
            float[] letGo = Render(Make(voice), 6f, (0f, synth => synth.Play(0)), (0.3f, synth => synth.Play(Flute.NoHole)));

            Assert.That(Rms(letGo, 0.7f, 0.8f), Is.LessThan(Rms(held, 0.7f, 0.8f) * 0.7));
            Assert.That(Jerk(letGo, 0.25f, 0.6f), Is.LessThan(Peak(letGo, 0f, 0.3f) * MostJerkShare), "no click on letting go");
            Assert.That(Peak(letGo, 5.5f, 6f), Is.EqualTo(0d), "silent and idle at last");
        }

        [TestCase(Voice.Pluck)]
        [TestCase(Voice.Bell)]
        public void StruckNote_StruckAgainOnTheSameHole_RingsAnew(Voice voice)
        {
            float[] samples = Render(Make(voice), 1.4f, (0f, synth => synth.Play(2)), (1.2f, synth => synth.Play(2)));

            Assert.That(Rms(samples, 1.22f, 1.28f), Is.GreaterThan(Rms(samples, 1.14f, 1.2f) * 1.5));
        }

        [Test]
        public void Reed_WhileHeld_Holds_AndLetGo_FadesToSilence()
        {
            float[] samples = Render(Make(Voice.Reed), 1.2f, (0f, synth => synth.Play(1)), (0.7f, synth => synth.Play(Flute.NoHole)));
            double early = Rms(samples, 0.2f, 0.3f);
            double late = Rms(samples, 0.55f, 0.65f);

            Assert.That(20d * Math.Log10(late / early), Is.EqualTo(0d).Within(1d), "a held note holds within 1 dB");
            Assert.That(Rms(samples, 0.95f, 1.05f), Is.LessThan(early * 0.01), "faded a quarter second after it was let go");
            Assert.That(Jerk(samples, 0.65f, 1.2f), Is.LessThan(Peak(samples, 0f, 0.7f) * MostJerkShare), "no click on letting go");
        }

        [TestCase(Voice.Pluck)]
        [TestCase(Voice.Bell)]
        [TestCase(Voice.Reed)]
        public void AFastRun_ALiftAndALetGo_NeverClick(Voice voice)
        {
            // Seven notes in 0.35 s: more than the struck voices ring at once, so the quietest is taken over.
            float[] samples = Render(
                Make(voice),
                1f,
                (0f, synth => synth.Play(1)),
                (0.1f, synth => synth.Play(2)),
                (0.15f, synth => synth.Play(3)),
                (0.2f, synth => synth.Play(1)),
                (0.25f, synth => synth.Play(2)),
                (0.3f, synth => synth.Play(3)),
                (0.35f, synth => synth.Play(0)),
                (0.4f, synth => synth.Lift(2f)),
                (0.5f, synth => synth.Play(Flute.NoHole)));

            Assert.That(Jerk(samples, 0f, 1f), Is.LessThan(Peak(samples, 0f, 1f) * MostJerkShare));
        }

        [TestCase(Voice.Pluck)]
        [TestCase(Voice.Reed)]
        public void Lift_RaisesTheSoundingNote(Voice voice)
        {
            float[] samples = Render(Make(voice), 0.5f, (0f, synth => synth.Play(0)), (0.2f, synth => synth.Lift(2f)));

            double lifted = Pitch(samples, 0.3f, 0.45f, HoleHertz[1]);

            Assert.That(lifted, Is.EqualTo(HoleHertz[0] * Math.Pow(2d, 2d / 12d)).Within(HoleHertz[1] * 0.01));
        }

        [TestCase(Voice.Pluck)]
        [TestCase(Voice.Bell)]
        [TestCase(Voice.Reed)]
        public void Mix_WhileSounding_AllocatesNothing(Voice voice)
        {
            INoteSynth synth = Make(voice);
            var buffer = new float[Block * 2];
            synth.Play(2);
            synth.Mix(buffer, 2);

            Assert.That(() => { synth.Mix(buffer, 2); }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Constructors_WithoutPitchesOrRate_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new StruckSynth(StruckTone.Kalimba, null, 0.01f, Rate));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StruckSynth(StruckTone.Kalimba, HoleHertz, 0.01f, 0));
            Assert.Throws<ArgumentNullException>(() => new ReedSynth(ReedTone.WaspHum, null, 0.01f, Rate));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReedSynth(ReedTone.WaspHum, HoleHertz, 0.01f, -1));
        }

        // Renders mono samples in audio-sized blocks; each step of the script runs before the block that holds its time.
        internal static float[] Render(INoteSynth synth, float seconds, params (float At, Action<INoteSynth> Act)[] script)
        {
            int count = (int)(seconds * Rate);
            var samples = new float[count];
            var buffer = new float[Block];
            int next = 0;
            for (int start = 0; start < count; start += Block)
            {
                while (next < script.Length && script[next].At * Rate <= start)
                {
                    script[next].Act(synth);
                    next++;
                }

                Array.Clear(buffer, 0, Block);
                synth.Mix(buffer, 1);
                Array.Copy(buffer, 0, samples, start, Math.Min(Block, count - start));
            }

            return samples;
        }

        internal static double Rms(float[] samples, float from, float to)
        {
            int first = (int)(from * Rate);
            int last = Math.Min(samples.Length, (int)(to * Rate));
            double sum = 0d;
            for (int index = first; index < last; index++)
            {
                sum += samples[index] * (double)samples[index];
            }

            return Math.Sqrt(sum / Math.Max(1, last - first));
        }

        private static double Peak(float[] samples, float from, float to)
        {
            int first = (int)(from * Rate);
            int last = Math.Min(samples.Length, (int)(to * Rate));
            double peak = 0d;
            for (int index = first; index < last; index++)
            {
                peak = Math.Max(peak, Math.Abs(samples[index]));
            }

            return peak;
        }

        // The largest second difference: small for a smooth wave, about the size of the step for a click.
        private static double Jerk(float[] samples, float from, float to)
        {
            int first = Math.Max(2, (int)(from * Rate));
            int last = Math.Min(samples.Length, (int)(to * Rate));
            double most = 0d;
            for (int index = first; index < last; index++)
            {
                most = Math.Max(most, Math.Abs(samples[index] - (2d * samples[index - 1]) + samples[index - 2]));
            }

            return most;
        }

        // The pitch by autocorrelation near the guess, refined between lags by a parabola through the best three.
        private static double Pitch(float[] samples, float from, float to, double guess)
        {
            int first = (int)(from * Rate);
            int last = (int)(to * Rate);
            int shortest = (int)(Rate / (guess * 1.3));
            int longest = (int)(Rate / (guess / 1.3));
            var scores = new double[longest + 2];
            int best = shortest;
            for (int lag = shortest - 1; lag <= longest + 1; lag++)
            {
                double sum = 0d;
                for (int index = first; index < last - lag; index++)
                {
                    sum += samples[index] * (double)samples[index + lag];
                }

                scores[lag] = sum;
                if (lag >= shortest && lag <= longest && sum > scores[best])
                {
                    best = lag;
                }
            }

            double left = scores[best - 1];
            double middle = scores[best];
            double right = scores[best + 1];
            double bend = left - (2d * middle) + right;
            double shift = bend < 0d ? 0.5d * (left - right) / bend : 0d;
            return Rate / (best + shift);
        }
    }
}
