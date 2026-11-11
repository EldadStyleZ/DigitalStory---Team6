using System;
using System.Threading;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Sounds the flute note of the hole the Wasp Piper just played, for as long as its key is held.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("DigitalStory/Swarm Flute Voice")]
    public sealed class SwarmFluteVoice : MonoBehaviour
    {
        private SoundConfig _sound;
        private PiperMoveConfig _move;
        private HiveDefenseInput _input;
        private AudioSource _source;
        private FluteSynth _synth;
        private int _buffersFilled;
        private float _secondsSounding;

        /// <summary>How many sound buffers have been filled, for tests and debugging.</summary>
        public int BuffersFilled => Volatile.Read(ref _buffersFilled);

        /// <summary>The hole being sounded, or <see cref="FluteSynth.Silent"/>. For tests and debugging.</summary>
        public int HolePlaying { get; private set; } = FluteSynth.Silent;

        /// <summary>Supplies what to play. Call once, before this object starts.</summary>
        /// <exception cref="ArgumentNullException">A config is missing.</exception>
        public void Initialize(SoundConfig sound, PiperMoveConfig move)
        {
            _sound = sound != null ? sound : throw new ArgumentNullException(nameof(sound));
            _move = move != null ? move : throw new ArgumentNullException(nameof(move));
        }

        /// <summary>Optionally supplies the input, so a note lasts while its key is held. Call once.</summary>
        public void Connect(HiveDefenseInput input)
        {
            _input = input != null ? input : throw new ArgumentNullException(nameof(input));
        }

        /// <summary>Sounds a hole, replacing any note now sounding. The hole counts from 0.</summary>
        public void PlayNote(int hole)
        {
            HolePlaying = hole;
            _secondsSounding = 0f;
            _synth?.Play(hole);
        }

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
        }

        private void OnEnable() => AudioSettings.OnAudioConfigurationChanged += HandleAudioChanged;

        private void Start()
        {
            if (_sound == null || _move == null)
            {
                Debug.LogError("SwarmFluteVoice needs Initialize before it starts.", this);
                return;
            }

            StartTheFlute();
        }

        private void Update()
        {
            if (HolePlaying == FluteSynth.Silent)
            {
                return;
            }

            // Real time, so a note does not hang when the game is paused or slowed.
            _secondsSounding += Time.unscaledDeltaTime;
            bool isHeld = _input != null && _input.IsHoleHeld(HolePlaying);

            if (_secondsSounding >= _move.NoteSeconds && !isHeld)
            {
                HolePlaying = FluteSynth.Silent;
                _synth?.Play(FluteSynth.Silent);
            }
        }

        private void OnDisable() => AudioSettings.OnAudioConfigurationChanged -= HandleAudioChanged;

        // Unity calls this on the audio thread, so the synth has to arrive complete: see StartTheFlute.
        private void OnAudioFilterRead(float[] data, int channels)
        {
            FluteSynth synth = Volatile.Read(ref _synth);

            if (synth != null)
            {
                synth.Fill(data, channels);
                Interlocked.Increment(ref _buffersFilled);
            }
        }

        private void StartTheFlute()
        {
            int sampleRate = AudioSettings.outputSampleRate;

            // A machine with no audio device reports no rate, and the game simply plays without sound.
            if (sampleRate <= 0)
            {
                return;
            }

            FluteSynth synth = new FluteSynth(_sound, sampleRate, _sound.HoleCount);
            synth.Play(HolePlaying);
            Volatile.Write(ref _synth, synth);
            _source.Play();
        }

        // Changing device, such as plugging in headphones, stops every source and can change the sample rate.
        private void HandleAudioChanged(bool deviceWasChanged)
        {
            if (_sound != null && _move != null)
            {
                StartTheFlute();
            }
        }
    }
}
