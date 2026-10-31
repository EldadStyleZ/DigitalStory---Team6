using System;
using DigitalStory.Data;

namespace DigitalStory.View
{
    /// <summary>Turns a sound cue into samples, ready to become an audio clip.</summary>
    public static class ToneBaker
    {
        private const double FullTurn = Math.PI * 2d;
        private const double AttackSeconds = .008d;

        // The level a tone fades to, as in the prototype.
        private const double QuietLevel = .0001d;

        public static float[] Bake(SoundCue cue, int sampleRate)
        {
            double seconds = 0d;

            foreach (Tone tone in cue.Tones)
            {
                seconds = Math.Max(seconds, tone.EndsAt);
            }

            var sound = new float[(int)Math.Ceiling(seconds * sampleRate)];

            foreach (Tone tone in cue.Tones)
            {
                AddTone(sound, in tone, sampleRate);
            }

            return sound;
        }

        private static void AddTone(float[] sound, in Tone tone, int sampleRate)
        {
            int first = (int)(tone.Delay * sampleRate);
            int count = Math.Min((int)(tone.Seconds * sampleRate), sound.Length - first);
            double slide = tone.EndHertz / (double)tone.StartHertz;
            double phase = 0d;
            var noise = new FilteredNoise(tone.StartHertz, sampleRate);

            for (int index = 0; index < count; index++)
            {
                double time = index / (double)sampleRate;
                double hertz = tone.StartHertz * Math.Pow(slide, time / tone.Seconds);
                phase = (phase + (hertz / sampleRate)) % 1d;

                double wave = tone.Shape == ToneShape.Noise ? noise.Next() : Wave(tone.Shape, phase);
                sound[first + index] += (float)(wave * Envelope(time, in tone));
            }
        }

        private static double Wave(ToneShape shape, double phase) => shape switch
        {
            ToneShape.Triangle => Waveforms.Triangle(phase),
            ToneShape.Square => phase < .5d ? 1d : -1d,
            ToneShape.Sawtooth => (2d * phase) - 1d,
            _ => Math.Sin(phase * FullTurn),
        };

        // A quick rise to avoid a click, then a fade that sounds even to the ear.
        private static double Envelope(double time, in Tone tone)
        {
            if (time < AttackSeconds)
            {
                return tone.Volume * time / AttackSeconds;
            }

            double fade = (time - AttackSeconds) / (tone.Seconds - AttackSeconds);
            return tone.Volume * Math.Pow(QuietLevel / tone.Volume, fade);
        }

        /// <summary>White noise with its highs removed.</summary>
        private struct FilteredNoise
        {
            private readonly double _step;

            private WhiteNoise _white;
            private double _level;

            public FilteredNoise(float hertz, int sampleRate)
            {
                _step = 1d - Math.Exp(-FullTurn * hertz / sampleRate);
                _white = WhiteNoise.Start();
                _level = 0d;
            }

            public double Next()
            {
                _level += (_white.Next() - _level) * _step;
                return _level;
            }
        }
    }
}
