using System;
using System.Collections;
using DigitalStory.Data;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>
    /// Plan section AC: the pluck, the bell and the reed are as loud as the flute (within 2 dB of RMS over a held note of the
    /// shortest note length, 0.3 s, from silence, on every hole the swarm plays), and the swarm's voice starts in the
    /// SoundConfig's voice and cycles through all four.
    /// </summary>
    public sealed class SwarmFluteVoiceLevelTests
    {
        private const int Rate = 48000;
        private const int Block = 512;
        private const float NoteSeconds = 0.3f;
        private const double MostDecibelsApart = 2d;
        private const int SwarmHoles = 4;

        [TestCase(NoteVoiceKind.Pluck)]
        [TestCase(NoteVoiceKind.Bell)]
        [TestCase(NoteVoiceKind.Reed)]
        public void Voice_OnEveryHole_IsWithinTwoDecibelsOfTheFlute(NoteVoiceKind voice)
        {
            SoundConfig sound = ScriptableObject.CreateInstance<SoundConfig>();
            try
            {
                for (int hole = 0; hole < SwarmHoles; hole++)
                {
                    double flute = HeldRms(new FluteSynth(sound, Rate, sound.HoleCount), hole);
                    double other = HeldRms(Make(voice, sound), hole);
                    double apart = 20d * Math.Log10(other / flute);

                    Assert.That(apart, Is.EqualTo(0d).Within(MostDecibelsApart), $"hole {hole}: {apart:F2} dB from the flute");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sound);
            }
        }

        [UnityTest]
        public IEnumerator SwarmFluteVoice_StartsInTheConfigsVoice_AndCyclesThroughAllFour()
        {
            SoundConfig sound = ScriptableObject.CreateInstance<SoundConfig>();
            CoreMoveConfig move = ScriptableObject.CreateInstance<CoreMoveConfig>();
            var host = new GameObject("SwarmFluteVoice");
            try
            {
                SwarmFluteVoice voice = host.AddComponent<SwarmFluteVoice>();
                voice.Initialize(sound, move);
                yield return null;

                Assert.That(voice.Voice, Is.EqualTo(NoteVoiceKind.Pluck), "the default is the kalimba pluck");
                voice.PlayNote(1);
                Assert.That(voice.NextVoice(), Is.EqualTo(NoteVoiceKind.Bell));
                Assert.That(voice.HolePlaying, Is.EqualTo(FluteSynth.Silent), "the old voice let its note go");
                Assert.That(voice.NextVoice(), Is.EqualTo(NoteVoiceKind.Reed));
                Assert.That(voice.NextVoice(), Is.EqualTo(NoteVoiceKind.Flute));
                Assert.That(voice.NextVoice(), Is.EqualTo(NoteVoiceKind.Pluck));
                Assert.That(SwarmFluteVoice.TitleOf(NoteVoiceKind.Pluck), Is.EqualTo("Kalimba"));

                voice.PlayNote(2);
                Assert.That(voice.HolePlaying, Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(sound);
                UnityEngine.Object.DestroyImmediate(move);
            }
        }

        [UnityTest]
        public IEnumerator CarryOverRestart_TheNextVoiceStartsInTheCarriedVoice_Once()
        {
            SoundConfig sound = ScriptableObject.CreateInstance<SoundConfig>();
            CoreMoveConfig move = ScriptableObject.CreateInstance<CoreMoveConfig>();
            var first = new GameObject("First");
            var second = new GameObject("Second");
            try
            {
                SwarmFluteVoice.CarryOverRestart(NoteVoiceKind.Reed);
                SwarmFluteVoice carried = first.AddComponent<SwarmFluteVoice>();
                carried.Initialize(sound, move);
                SwarmFluteVoice fresh = second.AddComponent<SwarmFluteVoice>();
                fresh.Initialize(sound, move);
                yield return null;

                Assert.That(carried.Voice, Is.EqualTo(NoteVoiceKind.Reed));
                Assert.That(fresh.Voice, Is.EqualTo(sound.NoteVoice));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
                UnityEngine.Object.DestroyImmediate(sound);
                UnityEngine.Object.DestroyImmediate(move);
            }
        }

        private static INoteSynth Make(NoteVoiceKind voice, SoundConfig sound)
        {
            var hertz = new float[sound.HoleCount];
            for (int hole = 0; hole < hertz.Length; hole++)
            {
                hertz[hole] = sound.HertzOfHole(hole);
            }

            switch (voice)
            {
                case NoteVoiceKind.Pluck:
                    return new StruckSynth(sound.Pluck, hertz, sound.GlideSeconds, Rate);
                case NoteVoiceKind.Bell:
                    return new StruckSynth(sound.Bell, hertz, sound.GlideSeconds, Rate);
                default:
                    return new ReedSynth(sound.Reed, hertz, sound.GlideSeconds, Rate);
            }
        }

        // RMS over a held note of NoteSeconds from silence, in audio-sized blocks of stereo, as the AudioSource asks.
        private static double HeldRms(INoteSynth synth, int hole)
        {
            synth.Play(hole);
            var buffer = new float[Block * 2];
            int frames = (int)(NoteSeconds * Rate);
            double sum = 0d;
            int counted = 0;
            while (counted < frames)
            {
                Array.Clear(buffer, 0, buffer.Length);
                synth.Mix(buffer, 2);
                for (int frame = 0; frame < Block && counted < frames; frame++, counted++)
                {
                    sum += buffer[frame * 2] * (double)buffer[frame * 2];
                }
            }

            return Math.Sqrt(sum / frames);
        }
    }
}
