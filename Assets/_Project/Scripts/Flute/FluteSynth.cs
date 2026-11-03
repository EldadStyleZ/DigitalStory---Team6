using System;

namespace PiperToy
{
    /// <summary>
    /// Makes the flute's sound, one buffer of samples at a time. It is the old game's synthesizer: a triangle wave
    /// with a soft note an octave above it, vibrato, a filter that takes the edge off and a little breath. A new
    /// note dips and swells like a tongued note, slides from the last one, and a released note fades out.
    /// </summary>
    /// <remarks>
    /// Unity calls <see cref="Fill"/> on the audio thread, so nothing here may touch Unity objects. It is a plain
    /// C# class, so tests can listen to it without a scene.
    /// </remarks>
    public sealed class FluteSynth
    {
        /// <summary>The key that means no hole is held.</summary>
        public const int Silent = 0;

        private const float FullTurn = (float)(Math.PI * 2d);
        private const float OvertoneRatio = 2f;
        private const float QuietEnough = 1e-6f;

        private readonly float[] _keyHertz;
        private readonly float _secondsPerSample;
        private readonly float _vibratoHertz;
        private readonly float _vibratoDepth;
        private readonly float _overtoneShare;
        private readonly float _breathShare;
        private readonly float _volume;
        private readonly float _noteStartVolume;
        private readonly int _noteStartSamples;
        private readonly float _glide;
        private readonly float _dip;
        private readonly float _recover;
        private readonly float _fade;
        private readonly float _brightness;
        private readonly float _breathHigh;
        private readonly float _breathLow;

        // Written by the main thread and read by the audio thread.
        private volatile int _requestedKey = Silent;

        // Everything below belongs to the audio thread.
        private int _key = Silent;
        private float _targetHertz;
        private float _hertz;
        private float _gain;
        private int _noteStartLeft;
        private float _phase;
        private float _vibratoPhase;
        private float _tone;
        private float _breathOuter;
        private float _breathInner;
        private uint _noise = 2463534242u;

        public FluteSynth(FluteSound sound, int sampleRate, int holeCount)
        {
            if (holeCount > sound.HoleCount)
            {
                throw new ArgumentException($"The flute has {holeCount} holes, but its sound only has pitches for {sound.HoleCount}.", nameof(holeCount));
            }

            _secondsPerSample = 1f / sampleRate;
            _keyHertz = new float[holeCount + 1];

            for (int key = 1; key <= holeCount; key++)
            {
                _keyHertz[key] = sound.HertzOf(key);
            }

            _vibratoHertz = sound.VibratoHertz;
            _vibratoDepth = sound.VibratoDepthHertz;
            _overtoneShare = sound.OvertoneShare;
            _breathShare = sound.BreathShare;
            _volume = sound.Volume;
            _noteStartVolume = sound.NoteStartVolume;
            _noteStartSamples = (int)(sound.NoteStartSeconds * sampleRate);
            _glide = StepTowards(sound.GlideSeconds, sampleRate);
            _dip = StepTowards(sound.NoteDipSeconds, sampleRate);
            _recover = StepTowards(sound.NoteRecoverSeconds, sampleRate);
            _fade = StepTowards(sound.FadeSeconds, sampleRate);
            _brightness = FilterStep(sound.BrightnessHertz, sampleRate);
            _breathHigh = FilterStep(sound.BreathHertz * (1f + sound.BreathWidth), sampleRate);
            _breathLow = FilterStep(sound.BreathHertz * (1f - sound.BreathWidth), sampleRate);
        }

        /// <summary>Sounds a key from 1 to 6, or fades out with <see cref="Silent"/>.</summary>
        public void Play(int key) => _requestedKey = key;

        /// <summary>Writes the next samples over a buffer.</summary>
        /// <param name="buffer">Samples, with each channel next to the others.</param>
        /// <param name="channels">How many channels the buffer holds.</param>
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

        // How much of the gap to close each sample, to get about two thirds of the way there in the given time.
        private static float StepTowards(float seconds, int sampleRate) => 1f - (float)Math.Exp(-1d / (seconds * sampleRate));

        private static float FilterStep(float hertz, int sampleRate) => 1f - (float)Math.Exp(-FullTurn * hertz / sampleRate);

        private void FollowTheRequest()
        {
            int requested = _requestedKey;

            if (requested == _key)
            {
                return;
            }

            _key = requested;

            if (requested == Silent)
            {
                return;
            }

            _targetHertz = _keyHertz[requested];
            _noteStartLeft = _noteStartSamples;

            // Starting from silence, a slide up from the last note would sound like a mistake.
            if (_gain <= QuietEnough)
            {
                _hertz = _targetHertz;
            }
        }

        private float NextSample()
        {
            UpdateGain();

            if (_gain <= QuietEnough)
            {
                _gain = _key == Silent ? 0f : _gain;
                return 0f;
            }

            float tone = NextTone();
            _tone += (tone - _tone) * _brightness;
            return (_tone + (NextBreath() * _breathShare)) * _gain;
        }

        private void UpdateGain()
        {
            if (_key == Silent)
            {
                _gain += (0f - _gain) * _fade;
                return;
            }

            if (_noteStartLeft > 0)
            {
                _noteStartLeft--;
                _gain += (_noteStartVolume - _gain) * _dip;
            }
            else
            {
                _gain += (_volume - _gain) * _recover;
            }
        }

        private float NextTone()
        {
            _hertz += (_targetHertz - _hertz) * _glide;
            _vibratoPhase = Wrap(_vibratoPhase + (_vibratoHertz * _secondsPerSample));
            float vibrato = (float)Math.Sin(_vibratoPhase * FullTurn) * _vibratoDepth;
            _phase = Wrap(_phase + ((_hertz + vibrato) * _secondsPerSample));
            float overtone = (float)Math.Sin(_phase * OvertoneRatio * FullTurn) * _overtoneShare;
            float triangle = (4f * Math.Abs(_phase - 0.5f)) - 1f;
            return triangle + overtone;
        }

        // White noise with its top and bottom removed, which leaves a band around the breath's pitch.
        private float NextBreath()
        {
            _breathOuter += (NextNoise() - _breathOuter) * _breathHigh;
            _breathInner += (_breathOuter - _breathInner) * _breathLow;
            return _breathOuter - _breathInner;
        }

        // A fast random number from -1 to 1, safe on the audio thread, the same generator as the old game's.
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
