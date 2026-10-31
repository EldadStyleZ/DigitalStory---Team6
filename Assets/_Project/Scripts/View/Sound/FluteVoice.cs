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
        private FluteSynth _synth;
        private int _buffersFilled;

        /// <summary>How many sound buffers have been filled, for tests and debugging.</summary>
        public int BuffersFilled => Volatile.Read(ref _buffersFilled);

        /// <summary>The hole sent to the sound, for tests and debugging.</summary>
        public int HolePlaying { get; private set; } = FluteSynth.Silent;

        /// <summary>Supplies what to play. Call once, before this object starts.</summary>
        public void Initialize(NightSession session, GameFlow flow, SoundConfig sound)
        {
            _session = session;
            _flow = flow;
            _sound = sound;
        }

        private void Start()
        {
            // GameRoot has already reported the problem if nothing was supplied.
            if (_session == null)
            {
                return;
            }

            _synth = new FluteSynth(_sound, AudioSettings.outputSampleRate);
            GetComponent<AudioSource>().Play();
        }

        private void Update()
        {
            if (_synth != null)
            {
                HolePlaying = _flow.IsPlaying ? _session.Flute.CurrentHole : FluteSynth.Silent;
                _synth.Play(HolePlaying);
            }
        }

        // Unity calls this on the audio thread.
        private void OnAudioFilterRead(float[] data, int channels)
        {
            FluteSynth synth = _synth;

            if (synth != null)
            {
                synth.Fill(data, channels);
                Interlocked.Increment(ref _buffersFilled);
            }
        }
    }
}
