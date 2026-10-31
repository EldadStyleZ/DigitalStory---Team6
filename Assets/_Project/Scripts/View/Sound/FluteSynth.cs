using System;
using DigitalStory.Data;

namespace DigitalStory.View
{
    /// <summary>Generates the flute's sound, one buffer at a time.</summary>
    /// <remarks>
    /// Unity calls <see cref="Fill"/> on the audio thread, so it must stay free of Unity calls.
    /// The recipe is the prototype's: a triangle wave with a soft octave above it, vibrato,
    /// a filter that takes the edge off and a little breath noise.
    /// </remarks>
    public sealed class FluteSynth
    {
        public const int Silent = -1;

        private const float FullTurn = (float)(Math.PI * 2d);
        private const float OvertoneRatio = 2f;
        private const float QuietEnough = 1e-6f;
        private const uint NoiseSeed = 2463534242u;
        private const float NoiseScale = 2147483648f;

        private readonly float[] _holeHertz;
        private readonly float _secondsPerSample;
        private readonly float _vibratoHertz;
        private readonly float _vibratoDepth;
        private readonly float _overtoneShare;
        private readonly float _breathShare;
        private readonly float _fluteVolume;
        private readonly float _noteStartVolume;
        private readonly int _noteStartSamples;
        private readonly float _glide;
        private readonly float _swell;
        private readonly float _fade;
        private readonly float _brightness;
        private readonly float _breathHigh;
        private readonly float _breathLow;

        // Written by the main thread and read by the audio thread.
        private volatile int _requestedHole = Silent;

        // Everything below belongs to the audio thread.
        private int _hole = Silent;
        private float _targetHertz;
        private float _hertz;
        private float _gain;
        private int _noteStartLeft;
        private float _phase;
        private float _vibratoPhase;
        private float _tone;
        private float _breathOuter;
        private float _breathInner;
        private uint _noise = NoiseSeed;

        /// <param name="config">How the flute sounds.</param>
        /// <param name="sampleRate">How many samples play per second.</param>
        public FluteSynth(SoundConfig config, int sampleRate)
        {
            _secondsPerSample = 1f / sampleRate;
            _holeHertz = new float[config.HoleCount];

            for (int hole = 0; hole < _holeHertz.Length; hole++)
            {
                _holeHertz[hole] = config.HertzOfHole(hole);
            }

            _vibratoHertz = config.VibratoHertz;
            _vibratoDepth = config.VibratoDepthHertz;
            _overtoneShare = config.OvertoneShare;
            _breathShare = config.BreathShare;
            _fluteVolume = config.FluteVolume;
            _noteStartVolume = config.NoteStartVolume;
            _noteStartSamples = (int)(config.NoteStartSeconds * sampleRate);
            _glide = StepTowards(config.GlideSeconds, sampleRate);
            _swell = StepTowards(config.SwellSeconds, sampleRate);
            _fade = StepTowards(config.FadeSeconds, sampleRate);
            _brightness = FilterStep(config.BrightnessHertz, sampleRate);
            _breathHigh = FilterStep(config.BreathHertz * 1.3f, sampleRate);
            _breathLow = FilterStep(config.BreathHertz * 0.7f, sampleRate);
        }

        /// <summary>Sounds a hole, or fades out with <see cref="Silent"/>.</summary>
        public void Play(int hole) => _requestedHole = hole;

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

        // How much of the gap to close each sample, to reach about two thirds of the way in the given time.
        private static float StepTowards(float seconds, int sampleRate) => 1f - (float)Math.Exp(-1d / (seconds * sampleRate));

        private static float FilterStep(float hertz, int sampleRate) => 1f - (float)Math.Exp(-FullTurn * hertz / sampleRate);

        private static float Triangle(float phase) => (4f * Math.Abs(phase - 0.5f)) - 1f;

        private void FollowTheRequest()
        {
            int requested = _requestedHole;

            if (requested == _hole)
            {
                return;
            }

            _hole = requested;

            if (requested == Silent)
            {
                return;
            }

            _targetHertz = _holeHertz[requested];
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
                _gain = _hole == Silent ? 0f : _gain;
                return 0f;
            }

            float tone = NextTone();
            _tone += (tone - _tone) * _brightness;
            return (_tone + (NextBreath() * _breathShare)) * _gain;
        }

        private void UpdateGain()
        {
            if (_hole == Silent)
            {
                _gain += (0f - _gain) * _fade;
                return;
            }

            float target = _noteStartLeft > 0 ? _noteStartVolume : _fluteVolume;
            _noteStartLeft = Math.Max(0, _noteStartLeft - 1);
            _gain += (target - _gain) * _swell;
        }

        private float NextTone()
        {
            _hertz += (_targetHertz - _hertz) * _glide;
            _vibratoPhase = Wrap(_vibratoPhase + (_vibratoHertz * _secondsPerSample));
            float vibrato = (float)Math.Sin(_vibratoPhase * FullTurn) * _vibratoDepth;

            _phase = Wrap(_phase + ((_hertz + vibrato) * _secondsPerSample));
            float overtone = (float)Math.Sin(_phase * OvertoneRatio * FullTurn) * _overtoneShare;
            return Triangle(_phase) + overtone;
        }

        // White noise with its top and bottom removed, which leaves a band around the breath pitch.
        private float NextBreath()
        {
            _noise ^= _noise << 13;
            _noise ^= _noise >> 17;
            _noise ^= _noise << 5;

            float white = (_noise / NoiseScale) - 1f;
            _breathOuter += (white - _breathOuter) * _breathHigh;
            _breathInner += (_breathOuter - _breathInner) * _breathLow;
            return _breathOuter - _breathInner;
        }

        private static float Wrap(float phase) => phase >= 1f ? phase - 1f : phase;
    }
}
