using System.Threading;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Plays whichever hole the flute is sounding.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("DigitalStory/Flute Voice")]
    public sealed class FluteVoice : MonoBehaviour
    {
        private NightSession _session;
        private GameFlow _flow;
        private SoundConfig _sound;
        private int _holeCount;
        private AudioSource _source;
        private FluteSynth _synth;
        private int _buffersFilled;

        /// <summary>How many sound buffers have been filled, for tests and debugging.</summary>
        public int BuffersFilled => Volatile.Read(ref _buffersFilled);

        /// <summary>The hole sent to the sound, for tests and debugging.</summary>
        public int HolePlaying { get; private set; } = FluteSynth.Silent;

        /// <summary>Supplies what to play. Call once, before this object starts.</summary>
        public void Initialize(NightSession session, GameFlow flow, SoundConfig sound, int holeCount)
        {
            _session = session;
            _flow = flow;
            _sound = sound;
            _holeCount = holeCount;
        }

        private void Awake() => _source = GetComponent<AudioSource>();

        private void OnEnable() => AudioSettings.OnAudioConfigurationChanged += HandleAudioChanged;

        private void Start()
        {
            // GameRoot has already reported the problem if nothing was supplied.
            if (_session != null)
            {
                StartTheFlute();
            }
        }

        private void Update()
        {
            if (_synth != null)
            {
                HolePlaying = _flow.IsPlaying ? _session.Flute.CurrentHole : FluteSynth.Silent;
                _synth.Play(HolePlaying);
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

            Volatile.Write(ref _synth, new FluteSynth(_sound, sampleRate, _holeCount));
            _source.Play();
        }

        // Changing device, such as plugging in headphones, stops every source and can change the sample rate.
        private void HandleAudioChanged(bool deviceWasChanged)
        {
            if (_session != null)
            {
                StartTheFlute();
            }
        }
    }
}
