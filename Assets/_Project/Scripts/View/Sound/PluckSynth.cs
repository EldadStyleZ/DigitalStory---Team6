using System;
using System.Threading;
using DigitalStory.Data;

namespace DigitalStory.View
{
    /// <summary>A kalimba-like pluck for the swarm's notes, one buffer at a time (plan section AC).</summary>
    /// <remarks>
    /// Unity calls <see cref="Fill"/> on the audio thread, so it stays free of Unity calls and allocation. A tine: a sine at
    /// the note with a soft octave, and an inharmonic partial a little above five times the note that dies fast, all under a
    /// 3 ms attack and an exponential ring. Each pluck takes the other of two voices, and the one it replaces is damped
    /// over a few milliseconds, so neither a new note nor a released one clicks. A held key lets the ring run its course.
    /// </remarks>
    public sealed class PluckSynth : INoteSynth
    {
        private const float FullTurn = (float)(Math.PI * 2d);
        private const float OctaveShare = 0.12f;
        private const float PartialRatio = 5.43f;
        private const float PartialShare = 0.2f;
        private const float PartialDecaySeconds = 0.06f;
        private const float AttackSeconds = 0.003f;
        private const float StealSeconds = 0.006f;
        private const float GlideSeconds = 0.012f;
        private const float QuietEnough = 1e-5f;
        private const int VoiceCount = 2;

        private readonly float[] _holeHertz;
        private readonly float _secondsPerSample;
        private readonly float _volume;
        private readonly float _attackStep;
        private readonly float _ring;
        private readonly float _partialRing;
        private readonly float _damp;
        private readonly float _steal;
        private readonly float _glide;

        // Written by the main thread and read by the audio thread.
        private volatile int _requestedHole = FluteSynth.Silent;
        private volatile float _requestedLift;
        private int _requestCount;

        // Everything below belongs to the audio thread.
        private readonly float[] _hertz = new float[VoiceCount];
        private readonly float[] _targetHertz = new float[VoiceCount];
        private readonly float[] _phase = new float[VoiceCount];
        private readonly float[] _partialPhase = new float[VoiceCount];
        private readonly float[] _attack = new float[VoiceCount];
        private readonly float[] _body = new float[VoiceCount];
        private readonly float[] _partial = new float[VoiceCount];
        private readonly float[] _release = new float[VoiceCount];
        private readonly float[] _releaseStep = new float[VoiceCount];
        private int _seenCount;
        private int _voice;
        private int _hole = FluteSynth.Silent;
        private float _lift;
        private float _liftRatio = 1f;

        /// <param name="config">How the pluck sounds and the holes' pitches.</param>
        /// <param name="sampleRate">How many samples play per second.</param>
        /// <param name="holeCount">How many holes the flute has, which the sound must cover.</param>
        public PluckSynth(SoundConfig config, int sampleRate, int holeCount)
        {
            if (holeCount > config.HoleCount)
            {
                throw new ArgumentException(
                    $"The flute has {holeCount} holes, but SoundConfig only has pitches for {config.HoleCount}.",
                    nameof(holeCount));
            }

            _secondsPerSample = 1f / sampleRate;
            _holeHertz = new float[config.HoleCount];
            for (int hole = 0; hole < _holeHertz.Length; hole++)
            {
                _holeHertz[hole] = config.HertzOfHole(hole);
            }

            _volume = config.PluckVolume;
            _attackStep = 1f / Math.Max(1f, AttackSeconds * sampleRate);
            _ring = Fall(config.PluckDecaySeconds, sampleRate);
            _partialRing = Fall(PartialDecaySeconds, sampleRate);
            _damp = Fall(config.PluckDampSeconds, sampleRate);
            _steal = Fall(StealSeconds, sampleRate);
            _glide = 1f - (float)Math.Exp(-1d / (GlideSeconds * sampleRate));
            for (int voice = 0; voice < VoiceCount; voice++)
            {
                _releaseStep[voice] = 1f;
            }
        }

        /// <summary>Plucks a hole, or damps the ring with <see cref="FluteSynth.Silent"/>. Plucking the same hole again plucks it anew.</summary>
        public void Play(int hole)
        {
            _requestedHole = hole;
            Interlocked.Increment(ref _requestCount);
        }

        /// <summary>Raises the note by some semitones until it is set again, gliding there; 0 plays the holes' own pitches.</summary>
        public void Lift(float semitones) => _requestedLift = semitones;

        /// <summary>Writes the next samples over a buffer.</summary>
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

        // The share left after one sample, so a level falls to about a third in the given time.
        private static float Fall(float seconds, int sampleRate) => (float)Math.Exp(-1d / (seconds * sampleRate));

        private static float Wrap(float phase) => phase >= 1f ? phase - 1f : phase;

        private void FollowTheRequest()
        {
            float lift = _requestedLift;
            if (lift != _lift)
            {
                _lift = lift;
                _liftRatio = (float)Math.Pow(2d, lift / 12d);
                if (_hole != FluteSynth.Silent)
                {
                    _targetHertz[_voice] = _holeHertz[_hole] * _liftRatio;
                }
            }

            int count = Volatile.Read(ref _requestCount);
            if (count == _seenCount)
            {
                return;
            }

            _seenCount = count;
            int requested = _requestedHole;
            _hole = requested;
            if (requested == FluteSynth.Silent)
            {
                _releaseStep[_voice] = Math.Min(_releaseStep[_voice], _damp);
                return;
            }

            _releaseStep[_voice] = _steal;
            _voice = (_voice + 1) % VoiceCount;
            StartVoice(_voice, _holeHertz[requested] * _liftRatio);
        }

        private void StartVoice(int voice, float hertz)
        {
            _hertz[voice] = hertz;
            _targetHertz[voice] = hertz;
            _phase[voice] = 0f;
            _partialPhase[voice] = 0f;
            _attack[voice] = 0f;
            _body[voice] = 1f;
            _partial[voice] = 1f;
            _release[voice] = 1f;
            _releaseStep[voice] = 1f;
        }

        private float NextSample()
        {
            float sum = 0f;
            for (int voice = 0; voice < VoiceCount; voice++)
            {
                if (_release[voice] <= QuietEnough || _body[voice] <= QuietEnough)
                {
                    continue;
                }

                sum += NextOfVoice(voice);
            }

            return sum * _volume;
        }

        private float NextOfVoice(int voice)
        {
            _attack[voice] = Math.Min(1f, _attack[voice] + _attackStep);
            _body[voice] *= _ring;
            _partial[voice] *= _partialRing;
            _release[voice] *= _releaseStep[voice];
            _hertz[voice] += (_targetHertz[voice] - _hertz[voice]) * _glide;

            float step = _hertz[voice] * _secondsPerSample;
            _phase[voice] = Wrap(_phase[voice] + step);
            _partialPhase[voice] = _partialPhase[voice] + (step * PartialRatio);
            _partialPhase[voice] -= (float)Math.Floor(_partialPhase[voice]);

            float turn = _phase[voice] * FullTurn;
            float tone = (float)Math.Sin(turn) + (OctaveShare * (float)Math.Sin(turn * 2f));
            float shimmer = PartialShare * _partial[voice] * (float)Math.Sin(_partialPhase[voice] * FullTurn);
            return ((tone * _body[voice]) + shimmer) * _attack[voice] * _release[voice];
        }
    }
}
