using System;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// The sound of a hit, made once at start from a few numbers: a low thump for a hit, a brighter pop for a kill, a dull knock for a hit a
    /// shield blocks, and a soft rising tone the moment a song is cast. All play through one AudioSource with a small pitch variation.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("DigitalStory/Hit Sound View")]
    public sealed class HitSoundView : MonoBehaviour
    {
        private const int SampleRate = 44100;
        private const float FullTurn = Mathf.PI * 2f;

        [Header("Volume")]
        [SerializeField, Range(0f, 1f), Tooltip("Volume of the thump on a hit.")]
        private float _hitVolume = 0.5f;

        [SerializeField, Range(0f, 1f), Tooltip("Volume of the pop on a kill.")]
        private float _killVolume = 0.55f;

        [SerializeField, Range(0f, 1f), Tooltip("Volume of the knock on a blocked hit.")]
        private float _blockedVolume = 0.35f;

        [SerializeField, Range(0f, 1f), Tooltip("Volume of the cast tone.")]
        private float _castVolume = 0.3f;

        [SerializeField, Range(0f, 0.3f), Tooltip("How far the pitch varies either way, as a share.")]
        private float _pitchVariation = 0.06f;

        [SerializeField, Min(0f), Tooltip("Seconds a kind of sound is held back after it played, so a burst of hits in one frame is one sound, not a wall of them.")]
        private float _minGapSeconds = 0.04f;

        private const int KindHit = 0;
        private const int KindKill = 1;
        private const int KindBlocked = 2;
        private const int KindCast = 3;
        private const int Kinds = 4;

        private HiveDefenseSession _session;
        private AudioSource _source;
        private AudioClip[] _clips;
        private float[] _lastPlayed;
        private bool _isReady;
        private HitFeedbackView _chain;

        /// <summary>The pitch the last kill pop played at, before its small random variation. For tests.</summary>
        public float LastKillPitch { get; private set; } = 1f;

        /// <summary>How many sounds were played since Initialize. For tests.</summary>
        public int PlayedCount { get; private set; }

        /// <summary>Bakes the clips once and starts listening. Call once; calling again keeps the clips.</summary>
        /// <exception cref="ArgumentNullException">The session is missing.</exception>
        public void Initialize(HiveDefenseSession session)
        {
            Release();
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;

            if (_clips == null)
            {
                _clips = new AudioClip[Kinds];
                _clips[KindHit] = Bake("HitThump", 170f, 55f, 0.16f, 0.35f, 0.25f, 5f);
                _clips[KindKill] = Bake("KillPop", 1100f, 320f, 0.13f, 0.2f, 0.4f, 7f);
                _clips[KindBlocked] = Bake("BlockKnock", 230f, 120f, 0.1f, 0.5f, 0.1f, 8f);
                _clips[KindCast] = Bake("CastRise", 280f, 760f, 0.2f, 0f, 0.15f, 2.5f);
                _lastPlayed = new float[Kinds];
            }

            for (int kind = 0; kind < Kinds; kind++)
            {
                _lastPlayed[kind] = float.NegativeInfinity;
            }

            PlayedCount = 0;
            _session.SongLanded += OnSongLanded;
            _session.SongCastNow += OnSongCastNow;
            _isReady = true;
        }

        private void OnDestroy()
        {
            Release();
            if (_clips != null)
            {
                for (int kind = 0; kind < _clips.Length; kind++)
                {
                    if (_clips[kind] != null)
                    {
                        Destroy(_clips[kind]);
                    }
                }
            }
        }

        private void Release()
        {
            _isReady = false;
            if (_session != null)
            {
                _session.SongLanded -= OnSongLanded;
                _session.SongCastNow -= OnSongCastNow;
            }

            if (_chain != null)
            {
                _chain.KillChained -= OnKillChained;
                _chain = null;
            }
        }

        /// <summary>
        /// Plays each kill's pop from the feel director's kill chain from now on (plan section Y): each kill of a chain one
        /// semitone higher, up to the top, so a run of kills climbs. Call after Initialize.
        /// </summary>
        /// <exception cref="ArgumentNullException">The feedback view is missing.</exception>
        public void ListenToKillChain(HitFeedbackView feedback)
        {
            if (feedback == null)
            {
                throw new ArgumentNullException(nameof(feedback));
            }

            if (_chain != null)
            {
                _chain.KillChained -= OnKillChained;
            }

            _chain = feedback;
            _chain.KillChained += OnKillChained;
        }

        private void OnKillChained(KillChainStep step)
        {
            LastKillPitch = Mathf.Pow(2f, step.Semitones / 12f);
            if (IsRecorded(GameSoundEvent.BeekeeperDeath))
            {
                return;
            }

            Play(KindKill, _killVolume, LastKillPitch);
        }

        private void OnSongLanded(SongHit hit)
        {
            if (hit.IsBlocked)
            {
                Play(KindBlocked, _blockedVolume);
            }
            else if (hit.IsKill)
            {
                if (_chain == null)
                {
                    Play(KindKill, _killVolume);
                }
            }
            else
            {
                Play(KindHit, _hitVolume);
            }
        }

        private void OnSongCastNow(int song)
        {
            if (!IsRecorded(GameSoundEvent.SongCast))
            {
                Play(KindCast, _castVolume);
            }
        }

        // A recorded sound (plan section AC) replaces the made one for its moment, so the two never double up.
        private bool IsRecorded(GameSoundEvent kind)
        {
            return _chain != null && _chain.RecordedSound != null && _chain.RecordedSound.Has(kind);
        }

        private void Play(int kind, float volume)
        {
            Play(kind, volume, 1f);
        }

        private void Play(int kind, float volume, float pitch)
        {
            if (!_isReady)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now - _lastPlayed[kind] < _minGapSeconds)
            {
                return;
            }

            _lastPlayed[kind] = now;
            _source.pitch = pitch * (1f + UnityEngine.Random.Range(-_pitchVariation, _pitchVariation));
            _source.PlayOneShot(_clips[kind], volume);
            PlayedCount++;
        }

        // A sine that glides from one pitch to another, with a share of noise and of its second harmonic, and a fast fall.
        // Short fade in and out so it never clicks.
        private static AudioClip Bake(string name, float startHertz, float endHertz, float seconds, float noiseShare, float harmonicShare, float decay)
        {
            int count = Mathf.CeilToInt(seconds * SampleRate);
            var samples = new float[count];
            float phase = 0f;
            uint noiseState = 22695477u;
            float lowpass = 0f;
            for (int index = 0; index < count; index++)
            {
                float t = index / (float)count;
                float hertz = startHertz * Mathf.Pow(endHertz / startHertz, t);
                phase += hertz / SampleRate;
                phase -= Mathf.Floor(phase);

                noiseState = (noiseState * 1664525u) + 1013904223u;
                float white = ((noiseState >> 8) / 8388608f) - 1f;
                lowpass += (white - lowpass) * 0.25f;

                float tone = Mathf.Sin(phase * FullTurn) + (harmonicShare * Mathf.Sin(phase * FullTurn * 2f));
                float wave = (tone * (1f - noiseShare)) + (lowpass * noiseShare * 2f);
                float attack = Mathf.Clamp01(index / (SampleRate * 0.003f));
                float release = Mathf.Clamp01((count - index) / (SampleRate * 0.01f));
                float envelope = Mathf.Exp(-decay * t) * attack * release;
                samples[index] = wave * envelope * 0.8f;
            }

            AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
