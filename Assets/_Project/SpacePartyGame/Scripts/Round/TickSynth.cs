using System;
using System.Threading;

namespace SpacePartyGame
{
    /// <summary>
    /// The clock's tick near the end of a round: a short wood-block "tok", two sine partials with a fast decay and a
    /// tiny noise click on top. <see cref="Tick"/> is called from the game; <see cref="Fill"/> runs on the audio
    /// thread.
    /// </summary>
    public sealed class TickSynth
    {
        private const float TwoPi = (float)(Math.PI * 2.0);

        private readonly float _sampleRate;
        private readonly float _pitch;
        private readonly float _highPitch;
        private readonly float _decaySeconds;
        private readonly float _volume;
        private readonly Random _noise = new Random(7);

        private int _pending;
        private float _frequency;
        private float _phase1;
        private float _phase2;
        private float _envelope;
        private float _clickEnvelope;
        private float _decayPerSample;
        private float _clickDecayPerSample;

        /// <param name="pitch">The tick's pitch in Hz.</param>
        /// <param name="highPitch">The pitch of the last seconds' ticks, in Hz.</param>
        /// <param name="decaySeconds">How long the tok rings, roughly, in seconds.</param>
        /// <param name="volume">Peak loudness, 0 to 1.</param>
        public TickSynth(int sampleRate, float pitch = 900f, float highPitch = 1250f, float decaySeconds = 0.06f, float volume = 0.5f)
        {
            _sampleRate = sampleRate;
            _pitch = pitch;
            _highPitch = highPitch;
            _decaySeconds = Math.Max(0.005f, decaySeconds);
            _volume = Math.Min(1f, Math.Max(0f, volume));
            _decayPerSample = (float)Math.Exp(-1.0 / (_decaySeconds * 0.25f * _sampleRate));
            _clickDecayPerSample = (float)Math.Exp(-1.0 / (0.002f * _sampleRate));
        }

        /// <summary>Plays one tick; <paramref name="high"/> plays the higher one for the last seconds.</summary>
        public void Tick(bool high)
        {
            Volatile.Write(ref _pending, high ? 2 : 1);
        }

        /// <summary>Adds the tick's next samples to the buffer, on every channel.</summary>
        public void Fill(float[] buffer, int channels)
        {
            int pending = Interlocked.Exchange(ref _pending, 0);

            if (pending != 0)
            {
                _frequency = pending == 2 ? _highPitch : _pitch;
                _phase1 = 0f;
                _phase2 = 0f;
                _envelope = 1f;
                _clickEnvelope = 1f;
            }

            if (_envelope < 1e-5f)
            {
                return;
            }

            float step1 = TwoPi * _frequency / _sampleRate;
            float step2 = TwoPi * _frequency * 2.76f / _sampleRate;

            for (int i = 0; i < buffer.Length; i += channels)
            {
                float tone = (float)Math.Sin(_phase1) * 0.75f + (float)Math.Sin(_phase2) * 0.25f * _envelope;
                float click = ((float)_noise.NextDouble() * 2f - 1f) * _clickEnvelope * 0.3f;
                float sample = (tone * _envelope + click) * _volume;
                sample = Math.Max(-1f, Math.Min(1f, sample));

                for (int c = 0; c < channels; c++)
                {
                    buffer[i + c] = Math.Max(-1f, Math.Min(1f, buffer[i + c] + sample));
                }

                _phase1 = (_phase1 + step1) % TwoPi;
                _phase2 = (_phase2 + step2) % TwoPi;
                _envelope *= _decayPerSample;
                _clickEnvelope *= _clickDecayPerSample;
            }

            if (_envelope < 1e-5f)
            {
                _envelope = 0f;
            }
        }
    }
}
