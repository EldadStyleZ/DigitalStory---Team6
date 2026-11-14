using System;
using System.Threading;
using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>
    /// A struck note voice, the kalimba-like pluck or the soft bell (plan section AC): each note is the hole's pitch and three
    /// partials above it, every one a sine that starts at zero, swells in over the attack and rings down on its own, the higher
    /// ones faster. A held key lets the note ring; letting it go, or striking the next note, damps it, so it fades and is
    /// never cut. A few notes ring at once; when all are busy the quietest fades out over a couple of milliseconds before it
    /// takes the new note, so even a fast run never clicks.
    /// </summary>
    /// <remarks>
    /// <see cref="Play"/> and <see cref="Lift"/> are called on the main thread and <see cref="Mix"/> on the audio thread; they
    /// share only a few volatile fields, and <see cref="Mix"/> allocates nothing.
    /// </remarks>
    public sealed class StruckSynth : INoteSynth
    {
        /// <summary>The voice's notes ring at once: enough for a run of quick notes to overlap.</summary>
        public const int Voices = 4;

        private const int Partials = 4;
        private const double FullTurn = Math.PI * 2d;

        // A voice whose loudest partial is below this share of the note's volume is silent and idle.
        private const float Audible = 1e-4f;

        // A partial below this share is skipped: far under hearing, and it saves the sine.
        private const float Faint = 1e-6f;

        // Seconds for a voice to fade before it takes a new note while every voice is busy.
        private const float StealSeconds = 0.002f;

        private readonly float[] _holeHertz;
        private readonly float _secondsPerSample;
        private readonly float _nyquistHertz;
        private readonly float _volume;
        private readonly float _attackStep;
        private readonly float _stealStep;
        private readonly float _glide;
        private readonly float[] _ratios = new float[Partials];
        private readonly float[] _shares = new float[Partials];
        private readonly float[] _ringStep = new float[Partials];
        private readonly float[] _dampStep = new float[Partials];

        // Written by the main thread and read by the audio thread.
        private volatile int _requestedHole = Flute.NoHole;
        private volatile bool _isRequestedHeld;
        private volatile float _requestedLift;
        private int _requestedStrikes;

        // Everything below belongs to the audio thread. Arrays per voice; the partials of voice v at v * Partials.
        private readonly int[] _hole = new int[Voices];
        private readonly bool[] _isActive = new bool[Voices];
        private readonly bool[] _isHeld = new bool[Voices];
        private readonly bool[] _isStealing = new bool[Voices];
        private readonly int[] _pendingHole = new int[Voices];
        private readonly float[] _hertz = new float[Voices];
        private readonly float[] _targetHertz = new float[Voices];
        private readonly float[] _attack = new float[Voices];
        private readonly float[] _stealGain = new float[Voices];
        private readonly float[] _phase = new float[Voices * Partials];
        private readonly float[] _amplitude = new float[Voices * Partials];
        private int _strikes;
        private int _newest = -1;
        private int _activeCount;
        private float _lift;
        private float _liftRatio = 1f;

        /// <param name="tone">How the voice sounds.</param>
        /// <param name="holeHertz">The pitch of each hole, in hertz. Copied.</param>
        /// <param name="glideSeconds">Seconds a lifted note takes to slide to its new pitch.</param>
        /// <param name="sampleRate">How many samples play per second.</param>
        /// <exception cref="ArgumentNullException">The pitches are missing.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The sample rate is not above 0.</exception>
        public StruckSynth(in StruckTone tone, float[] holeHertz, float glideSeconds, int sampleRate)
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
            _nyquistHertz = sampleRate * 0.5f;
            _volume = tone.Volume;
            _attackStep = 1f / Math.Max(1f, tone.AttackSeconds * sampleRate);
            _stealStep = Decay(StealSeconds, sampleRate);
            _glide = 1f - Decay(Math.Max(glideSeconds, 1e-4f), sampleRate);

            SetPartial(0, 1f, 1f, tone, sampleRate);
            SetPartial(1, tone.SecondRatio, tone.SecondShare, tone, sampleRate);
            SetPartial(2, tone.ThirdRatio, tone.ThirdShare, tone, sampleRate);
            SetPartial(3, tone.FourthRatio, tone.FourthShare, tone, sampleRate);
        }

        /// <summary>True while any note still rings. For tests; read it from the audio thread's side only.</summary>
        public bool IsRinging => _activeCount > 0;

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
            if (_activeCount == 0)
            {
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

        // The share of a value left after one sample of a fall with the given time constant.
        private static float Decay(float seconds, int sampleRate) => (float)Math.Exp(-1d / (seconds * sampleRate));

        private void SetPartial(int partial, float ratio, float share, in StruckTone tone, int sampleRate)
        {
            _ratios[partial] = ratio;
            _shares[partial] = ratio > 0f ? share : 0f;
            float speed = ratio > 0f ? (float)Math.Pow(ratio, tone.DecayPower) : 1f;
            _ringStep[partial] = Decay(tone.RingSeconds / speed, sampleRate);
            _dampStep[partial] = Decay(Math.Min(tone.DampSeconds, tone.RingSeconds) / speed, sampleRate);
        }

        private void FollowTheRequest()
        {
            float lift = _requestedLift;
            if (lift != _lift)
            {
                _lift = lift;
                _liftRatio = (float)Math.Pow(2d, lift / 12d);
                if (_newest >= 0 && _isActive[_newest] && !_isStealing[_newest])
                {
                    _targetHertz[_newest] = _holeHertz[_hole[_newest]] * _liftRatio;
                }
            }

            int strikes = Volatile.Read(ref _requestedStrikes);
            if (strikes != _strikes)
            {
                _strikes = strikes;
                Strike(_requestedHole);
            }

            if (!_isRequestedHeld && _newest >= 0)
            {
                _isHeld[_newest] = false;
            }
        }

        private void Strike(int hole)
        {
            if (hole < 0 || hole >= _holeHertz.Length)
            {
                return;
            }

            // The note before is damped, as a finger lifting off it, and rings out underneath the new one.
            if (_newest >= 0)
            {
                _isHeld[_newest] = false;
            }

            int voice = QuietestVoice();
            _newest = voice;
            if (_isActive[voice] && !_isStealing[voice])
            {
                // Every voice rings: this one fades over a couple of milliseconds first, then takes the note.
                _isStealing[voice] = true;
                _stealGain[voice] = 1f;
            }

            _pendingHole[voice] = hole;
            if (!_isActive[voice])
            {
                StartNote(voice);
            }
        }

        private int QuietestVoice()
        {
            int quietest = 0;
            float least = float.MaxValue;
            for (int voice = 0; voice < Voices; voice++)
            {
                if (!_isActive[voice])
                {
                    return voice;
                }

                float level = Level(voice);
                if (voice != _newest && level < least)
                {
                    least = level;
                    quietest = voice;
                }
            }

            return quietest;
        }

        private float Level(int voice)
        {
            float loudest = 0f;
            int first = voice * Partials;
            for (int partial = 0; partial < Partials; partial++)
            {
                loudest = Math.Max(loudest, _amplitude[first + partial]);
            }

            float steal = _isStealing[voice] ? _stealGain[voice] : 1f;
            return loudest * _attack[voice] * steal;
        }

        private void StartNote(int voice)
        {
            int hole = _pendingHole[voice];
            float hertz = _holeHertz[hole] * _liftRatio;
            _hole[voice] = hole;
            _hertz[voice] = hertz;
            _targetHertz[voice] = hertz;
            _attack[voice] = 0f;
            _isHeld[voice] = voice == _newest && _isRequestedHeld;
            _isStealing[voice] = false;
            int first = voice * Partials;
            for (int partial = 0; partial < Partials; partial++)
            {
                // Every sine starts at zero and swells in, so a note never starts with a step. A partial above the highest
                // pitch the sample rate can carry is left out rather than folded back down as a wrong note.
                _phase[first + partial] = 0f;
                bool isCarried = hertz * _ratios[partial] < _nyquistHertz;
                _amplitude[first + partial] = isCarried ? _shares[partial] : 0f;
            }

            if (!_isActive[voice])
            {
                _isActive[voice] = true;
                _activeCount++;
            }
        }

        private float NextSample()
        {
            float sum = 0f;
            for (int voice = 0; voice < Voices; voice++)
            {
                if (_isActive[voice])
                {
                    sum += NextVoiceSample(voice);
                }
            }

            return sum * _volume;
        }

        private float NextVoiceSample(int voice)
        {
            float gain = 1f;
            if (_isStealing[voice])
            {
                _stealGain[voice] *= _stealStep;
                gain = _stealGain[voice];
                if (gain * Level(voice) < Audible)
                {
                    StartNote(voice);
                    gain = 1f;
                }
            }

            _hertz[voice] += (_targetHertz[voice] - _hertz[voice]) * _glide;
            float attack = _attack[voice];
            if (attack < 1f)
            {
                attack = Math.Min(1f, attack + _attackStep);
                _attack[voice] = attack;
            }

            float[] steps = _isHeld[voice] ? _ringStep : _dampStep;
            float step = _hertz[voice] * _secondsPerSample;
            int first = voice * Partials;
            float sum = 0f;
            float loudest = 0f;
            for (int partial = 0; partial < Partials; partial++)
            {
                int index = first + partial;
                float amplitude = _amplitude[index];
                if (amplitude < Faint)
                {
                    continue;
                }

                float phase = _phase[index] + (step * _ratios[partial]);
                phase -= (float)Math.Floor(phase);
                _phase[index] = phase;
                sum += (float)Math.Sin(phase * FullTurn) * amplitude;
                _amplitude[index] = amplitude * steps[partial];
                loudest = Math.Max(loudest, amplitude);
            }

            if (loudest < Audible && !_isStealing[voice])
            {
                _isActive[voice] = false;
                _activeCount--;
            }

            return sum * attack * gain;
        }
    }
}
