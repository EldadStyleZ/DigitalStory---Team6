using System;

namespace SpacePartyGame
{
    /// <summary>
    /// Makes the cello's sound, one buffer at a time, in the same way as the toy's FluteSynth: a bowed string's buzz
    /// (a sawtooth) softened by two filters into a warm tone, with a slow vibrato and a little bow scratch. A note
    /// swells in, slides from the last one, and fades out when released.
    /// </summary>
    /// <remarks>
    /// Unity calls <see cref="Fill"/> on the audio thread, so nothing here touches Unity objects; tests listen to it
    /// without a scene.
    /// </remarks>
    public sealed class CelloSynth : IInstrumentSynth
    {
        /// <summary>The key that means no note.</summary>
        public const int Silent = 0;

        private const float FullTurn = (float)(Math.PI * 2d);
        private const float QuietEnough = 1e-4f;

        private readonly float[] _keyHertz;
        private readonly float _secondsPerSample;
        private readonly float _vibratoHertz;
        private readonly float _vibratoDepth;
        private readonly float _noiseShare;
        private readonly float _volume;
        private readonly float _attack;
        private readonly float _glide;
        private readonly float _fade;
        private readonly float _brightness;

        // Written by the main thread and read by the audio thread.
        private volatile int _requestedKey = Silent;

        // Everything below belongs to the audio thread.
        private int _key = Silent;
        private float _targetHertz;
        private float _hertz;
        private float _gain;
        private float _phase;
        private float _vibratoPhase;
        private float _toneOuter;
        private float _toneInner;
        private uint _noise = 2463534242u;

        public CelloSynth(CelloSound sound, int sampleRate)
        {
            _secondsPerSample = 1f / sampleRate;
            _keyHertz = new float[sound.KeyCount + 1];

            for (int key = 1; key <= sound.KeyCount; key++)
            {
                _keyHertz[key] = sound.HertzOf(key);
            }

            _vibratoHertz = sound.VibratoHertz;
            _vibratoDepth = sound.VibratoDepthHertz;
            _noiseShare = sound.BowNoiseShare;
            _volume = sound.Volume;
            _attack = StepTowards(sound.AttackSeconds, sampleRate);
            _glide = StepTowards(sound.GlideSeconds, sampleRate);
            _fade = StepTowards(sound.FadeSeconds, sampleRate);
            _brightness = 1f - (float)Math.Exp(-FullTurn * sound.BrightnessHertz / sampleRate);
        }

        /// <summary>Sounds a key from 1 to 6, or fades out with <see cref="Silent"/>.</summary>
        public void Play(int key) => _requestedKey = key;

        /// <summary>Writes the next samples over a buffer, with each channel next to the others.</summary>
        public void Fill(float[] buffer, int channels)
        {
            FollowTheRequest();

            for (int index = 0; index < buffer.Length; index += channels)
            {
                float sample = NextSample();

                for (int channel = 0; channel < channels; channel++)
                {
                    buffer[index + channel] = sample;
                }
            }
        }

        private static float StepTowards(float seconds, int sampleRate) => 1f - (float)Math.Exp(-1d / (seconds * sampleRate));

        private void FollowTheRequest()
        {
            int requested = _requestedKey;

            if (requested == _key)
            {
                return;
            }

            _key = requested;

            if (requested == Silent || requested >= _keyHertz.Length)
            {
                _key = Silent;
                return;
            }

            _targetHertz = _keyHertz[requested];

            // From silence the note starts on its pitch; a slide up from nothing would sound like a mistake.
            if (_gain <= QuietEnough)
            {
                _hertz = _targetHertz;
            }
        }

        private float NextSample()
        {
            float target = _key == Silent ? 0f : _volume;
            _gain += (target - _gain) * (_key == Silent ? _fade : _attack);

            if (_key == Silent && _gain <= QuietEnough)
            {
                _gain = 0f;
                return 0f;
            }

            _hertz += (_targetHertz - _hertz) * _glide;
            _vibratoPhase = Wrap(_vibratoPhase + (_vibratoHertz * _secondsPerSample));
            float vibrato = (float)Math.Sin(_vibratoPhase * FullTurn) * _vibratoDepth;
            _phase = Wrap(_phase + ((_hertz + vibrato) * _secondsPerSample));

            float buzz = (2f * _phase) - 1f + (NextNoise() * _noiseShare);
            _toneOuter += (buzz - _toneOuter) * _brightness;
            _toneInner += (_toneOuter - _toneInner) * _brightness;
            return _toneInner * _gain;
        }

        // A fast random number from -1 to 1, safe on the audio thread.
        private float NextNoise()
        {
            _noise ^= _noise << 13;
            _noise ^= _noise >> 17;
            _noise ^= _noise << 5;
            return (_noise / 2147483648f) - 1f;
        }

        private static float Wrap(float phase) => phase >= 1f ? phase - 1f : phase;
    }
}
