using DigitalStory.Data;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.PlayMode
{
    public sealed class ToneBakerTests
    {
        private const int SampleRate = 48000;

        [Test]
        public void Bake_NoTones_GivesNoSound()
        {
            Assert.That(ToneBaker.Bake(new SoundCue(), SampleRate), Is.Empty);
        }

        [Test]
        public void Bake_OneTone_LastsItsLength()
        {
            float[] sound = ToneBaker.Bake(new SoundCue(new Tone(ToneShape.Sine, 440f, 440f, 0.5f, 0.1f)), SampleRate);

            Assert.That(sound.Length, Is.EqualTo(SampleRate / 2).Within(2));
        }

        [Test]
        public void Bake_DelayedTone_LastsToTheEndOfTheLastTone()
        {
            var cue = new SoundCue(
                new Tone(ToneShape.Sine, 440f, 440f, 0.5f, 0.1f),
                new Tone(ToneShape.Sine, 440f, 440f, 0.25f, 0.1f, 1f));

            Assert.That(ToneBaker.Bake(cue, SampleRate).Length, Is.EqualTo(SampleRate * 5 / 4).Within(2));
        }

        [Test]
        public void Bake_DelayedTone_StaysSilentUntilItsStart()
        {
            float[] sound = ToneBaker.Bake(new SoundCue(new Tone(ToneShape.Sine, 440f, 440f, 0.1f, 0.1f, 0.5f)), SampleRate);

            Assert.That(Peak(sound, 0, SampleRate / 2), Is.EqualTo(0f));
            Assert.That(Peak(sound, SampleRate / 2, sound.Length), Is.GreaterThan(0.01f));
        }

        [TestCase(ToneShape.Sine)]
        [TestCase(ToneShape.Triangle)]
        [TestCase(ToneShape.Square)]
        [TestCase(ToneShape.Sawtooth)]
        [TestCase(ToneShape.Noise)]
        public void Bake_EveryShape_MakesSoundNoLouderThanItsVolume(ToneShape shape)
        {
            float[] sound = ToneBaker.Bake(new SoundCue(new Tone(shape, 440f, 440f, 0.2f, 0.1f)), SampleRate);

            Assert.That(Peak(sound, 0, sound.Length), Is.GreaterThan(0.01f));
            Assert.That(Peak(sound, 0, sound.Length), Is.LessThanOrEqualTo(0.1001f));
        }

        [TestCase(ToneShape.Sine)]
        [TestCase(ToneShape.Noise)]
        public void Bake_AnyTone_DiesAwayToNothing(ToneShape shape)
        {
            float[] sound = ToneBaker.Bake(new SoundCue(new Tone(shape, 440f, 440f, 0.5f, 0.1f)), SampleRate);

            Assert.That(Peak(sound, sound.Length - 480, sound.Length), Is.LessThan(0.0005f));
        }

        [Test]
        public void Bake_AnyTone_StartsWithoutAClick()
        {
            float[] sound = ToneBaker.Bake(new SoundCue(new Tone(ToneShape.Square, 440f, 440f, 0.5f, 0.1f)), SampleRate);

            Assert.That(Mathf.Abs(sound[0]), Is.LessThan(0.001f));
        }

        [Test]
        public void Bake_SlidingTone_MovesFromItsStartPitchToItsEndPitch()
        {
            float[] sound = ToneBaker.Bake(new SoundCue(new Tone(ToneShape.Sine, 400f, 1600f, 1f, 0.5f)), SampleRate);

            int window = SampleRate / 10;
            Assert.That(CrossingsPerSecond(sound, 0, window), Is.LessThan(600f));
            Assert.That(CrossingsPerSecond(sound, sound.Length / 2, window), Is.InRange(600f, 1200f));
        }

        [Test]
        public void Bake_EveryCueInTheGame_NeverClips()
        {
            var config = ScriptableObject.CreateInstance<SoundConfig>();
            SoundCue[] cues =
            {
                config.Squeak, config.Splash, config.Eaten, config.Pop, config.SongChime, config.Won, config.Lost,
                config.DiscoKick, config.DiscoTick, config.CongaBeat, config.CongaAccent,
                ScriptableObject.CreateInstance<BalloonsEffect>().Jingle,
                ScriptableObject.CreateInstance<FaintEffect>().Jingle,
                ScriptableObject.CreateInstance<CrowdSurfEffect>().Jingle,
                ScriptableObject.CreateInstance<ChoirEffect>().Jingle,
            };

            foreach (SoundCue cue in cues)
            {
                float[] sound = ToneBaker.Bake(cue, SampleRate);
                Assert.That(sound, Is.Not.Empty);
                Assert.That(Peak(sound, 0, sound.Length), Is.LessThanOrEqualTo(1f));
            }
        }

        private static float Peak(float[] sound, int from, int to)
        {
            float peak = 0f;

            for (int index = from; index < to; index++)
            {
                peak = Mathf.Max(peak, Mathf.Abs(sound[index]));
            }

            return peak;
        }

        private static float CrossingsPerSecond(float[] sound, int from, int samples)
        {
            var crossings = 0;

            for (int index = from + 1; index < from + samples; index++)
            {
                crossings += sound[index - 1] < 0f && sound[index] >= 0f ? 1 : 0;
            }

            return crossings * (float)SampleRate / samples;
        }
    }
}
