using System;
using System.Threading;
using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The buzzing reed voice (plan section AC), as if the wasps hummed the tune: a saw and a narrow pulse, both band-limited
    /// (polyBLEP, so no harsh aliasing), softened by a gentle low-pass, with a slight vibrato. It swells in, holds while the
    /// key is held and fades once the note is let go. A note that follows a sounding one slides to its pitch with a short dip,
    /// so each note is heard as new, the way the flute does.
    /// </summary>
    /// <remarks>
    /// <see cref="Play"/> and <see cref="Lift"/> are called on the main thread and <see cref="Mix"/> on the audio thread; they
    /// share only a few volatile fields, and <see cref="Mix"/> allocates nothing.
    /// </remarks>
    public sealed class ReedSynth : INoteSynth
    {
        private const double FullTurn = Math.PI * 2d;
        private const float QuietEnough = 1e-6f;

        // Seconds a following note dips for, and how fast it dips.
        private const float ArticulationSeconds = 0.03f;
        private const float DipSeconds = 0.005f;

        private readonly float[] _holeHertz;
        private readonly float _secondsPerSample;
        private readonly float _volume;
        private readonly float _attack;
        private readonly float _release;
        private readonly float _dip;
        private readonly float _articulationShare;
        private readonly int _articulationSamples;
        private readonly float _pulseWidth;
        private readonly float _pulseShare;
        private readonly float _lowPass;
        private readonly float _vibratoHertz;
        private readonly float _vibratoDepth;
        private readonly float _glide;

        // Written by the main thread and read by the audio thread.
        private volatile int _requestedHole = Flute.NoHole;
        private volatile bool _isRequestedHeld;
        private volatile float _requestedLift;
        private int _requestedStrikes;

        // Everything below belongs to the audio thread.
        private int _strikes;
        private int _hole = Flute.NoHole;
        private bool _isHeld;
        private float _lift;
        private float _liftRatio = 1f;
        private float _hertz;
        private float _targetHertz;
        private float _gain;
        private int _articulationLeft;
        private float _phase;
        private float _vibratoPhase;
        private float _lowOuter;
        private float _lowInner;

        /// <param name="tone">How the voice sounds.</param>
        /// <param name="holeHertz">The pitch of each hole, in hertz. Copied.</param>
        /// <param name="glideSeconds">Seconds a note takes to slide to a new pitch.</param>
        /// <param name="sampleRate">How many samples play per second.</param>
        /// <exception cref="ArgumentNullException">The pitches are missing.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The sample rate is not above 0.</exception>
        public ReedSynth(in ReedTone tone, float[] holeHertz, float glideSeconds, int sampleRate)
        {
            if (holeHertz == null)
            {
                throw new ArgumentNullException(nameof(holeHertz));
            }

            if (sampleRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate), sampleRate, "The sample rate must be above 0.");
            }

            _holeHertz = (float[])holeHertz.Clone();
            _secondsPerSample = 1f / sampleRate;
            _volume = tone.Volume;
            _attack = StepTowards(tone.AttackSeconds, sampleRate);
            _release = StepTowards(tone.ReleaseSeconds, sampleRate);
            _dip = StepTowards(DipSeconds, sampleRate);
            _articulationShare = tone.ArticulationShare;
            _articulationSamples = (int)(ArticulationSeconds * sampleRate);
            _pulseWidth = tone.PulseWidth;
            _pulseShare = tone.PulseShare;
            _lowPass = 1f - (float)Math.Exp(-FullTurn * tone.BrightnessHertz / sampleRate);
            _vibratoHertz = tone.VibratoHertz;
            _vibratoDepth = (float)(Math.Pow(2d, tone.VibratoCents / 1200d) - 1d);
            _glide = StepTowards(Math.Max(glideSeconds, 1e-4f), sampleRate);
        }

        /// <inheritdoc/>
        public void Play(int hole)
        {
            if (hole == Flute.NoHole)
            {
                _isRequestedHeld = false;
                return;
            }

            _requestedHole = hole;
            _isRequestedHeld = true;
            Interlocked.Increment(ref _requestedStrikes);
        }

        /// <inheritdoc/>
        public void Lift(float semitones) => _requestedLift = semitones;

        /// <inheritdoc/>
        public void Mix(float[] buffer, int channels)
        {
            FollowTheRequest();
            if (!_isHeld && _gain <= QuietEnough)
            {
                _gain = 0f;
                return;
            }

            for (int index = 0; index < buffer.Length; index += channels)
            {
                float sample = NextSample();
                for (int channel = 0; channel < channels; channel++)
                {
                    buffer[index + channel] += sample;
                }
            }
        }

        // How much of the gap to close each sample, to reach about two thirds of the way in the given time.
        private static float StepTowards(float seconds, int sampleRate) => 1f - (float)Math.Exp(-1d / (seconds * sampleRate));

        // The polyBLEP correction for a step of 2 at phase 0 of a wave whose phase moves by step each sample.
        private static float Blep(float phase, float step)
        {
            if (phase < step)
            {
                float t = phase / step;
                return t + t - (t * t) - 1f;
            }

            if (phase > 1f - step)
            {
                float t = (phase - 1f) / step;
                return (t * t) + t + t + 1f;
            }

            return 0f;
        }

        private void FollowTheRequest()
        {
            float lift = _requestedLift;
            if (lift != _lift)
            {
                _lift = lift;
                _liftRatio = (float)Math.Pow(2d, lift / 12d);
                if (_hole >= 0)
                {
                    _targetHertz = _holeHertz[_hole] * _liftRatio;
                }
            }

            int strikes = Volatile.Read(ref _requestedStrikes);
            if (strikes != _strikes)
            {
                _strikes = strikes;
                Start(_requestedHole);
            }

            if (!_isRequestedHeld)
            {
                _isHeld = false;
            }
        }

        private void Start(int hole)
        {
            if (hole < 0 || hole >= _holeHertz.Length)
            {
                return;
            }

            bool isSounding = _gain > QuietEnough;
            _hole = hole;
            _isHeld = _isRequestedHeld;
            _targetHertz = _holeHertz[hole] * _liftRatio;
            if (isSounding)
            {
                _articulationLeft = _articulationSamples;
                return;
            }

            // From silence the note starts on its pitch: a slide up from the last note would sound like a mistake.
            _hertz = _targetHertz;
            _articulationLeft = 0;
        }

        private float NextSample()
        {
            UpdateGain();
            _hertz += (_targetHertz - _hertz) * _glide;
            _vibratoPhase += _vibratoHertz * _secondsPerSample;
            _vibratoPhase -= (float)Math.Floor(_vibratoPhase);
            float vibrato = 1f + ((float)Math.Sin(_vibratoPhase * FullTurn) * _vibratoDepth);
            float step = _hertz * vibrato * _secondsPerSample;
            _phase += step;
            _phase -= (float)Math.Floor(_phase);

            float saw = (2f * _phase) - 1f - Blep(_phase, step);
            float fall = _phase - _pulseWidth;
            fall -= (float)Math.Floor(fall);
            float pulse = (_phase < _pulseWidth ? 1f : -1f) + Blep(_phase, step) - Blep(fall, step) - ((2f * _pulseWidth) - 1f);
            float tone = saw + ((pulse - saw) * _pulseShare);

            _lowOuter += (tone - _lowOuter) * _lowPass;
            _lowInner += (_lowOuter - _lowInner) * _lowPass;
            return _lowInner * _gain;
        }

        private void UpdateGain()
        {
            if (!_isHeld)
            {
                _gain += (0f - _gain) * _release;
                return;
            }

            if (_articulationLeft > 0)
            {
                _articulationLeft--;
                _gain += ((_volume * _articulationShare) - _gain) * _dip;
                return;
            }

            _gain += (_volume - _gain) * _attack;
        }
    }
}
