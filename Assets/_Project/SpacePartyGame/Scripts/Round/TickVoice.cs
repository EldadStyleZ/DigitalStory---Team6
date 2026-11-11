using System.Threading;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// Plays the clock's tick through its AudioSource, made by <see cref="TickSynth"/> on the audio thread, so there
    /// is no sound file.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class TickVoice : MonoBehaviour
    {
        [SerializeField, Min(50f), Tooltip("The tick's pitch, in Hz.")]
        private float _pitch = 900f;

        [SerializeField, Min(50f), Tooltip("The pitch of the last seconds' ticks, in Hz.")]
        private float _highPitch = 1250f;

        [SerializeField, Range(0.01f, 0.5f), Tooltip("Roughly how long each tick rings, in seconds.")]
        private float _ringSeconds = 0.06f;

        [SerializeField, Range(0f, 1f), Tooltip("How loud the tick is.")]
        private float _volume = 0.5f;

        private AudioSource _source;
        private TickSynth _synth;

        /// <summary>Plays one tick; <paramref name="high"/> plays the higher one.</summary>
        public void Tick(bool high)
        {
            Volatile.Read(ref _synth)?.Tick(high);
        }

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
        }

        private void Start()
        {
            StartTheSynth();
            AudioSettings.OnAudioConfigurationChanged += HandleAudioChanged;
        }

        private void OnDestroy()
        {
            AudioSettings.OnAudioConfigurationChanged -= HandleAudioChanged;
        }

        private void StartTheSynth()
        {
            int rate = AudioSettings.outputSampleRate;

            if (rate <= 0)
            {
                return;
            }

            Volatile.Write(ref _synth, new TickSynth(rate, _pitch, _highPitch, _ringSeconds, _volume));

            if (!_source.isPlaying)
            {
                _source.Play();
            }
        }

        private void HandleAudioChanged(bool deviceWasChanged)
        {
            StartTheSynth();
        }

        // Unity calls this on the audio thread to ask for the next samples.
        private void OnAudioFilterRead(float[] data, int channels)
        {
            Volatile.Read(ref _synth)?.Fill(data, channels);
        }
    }
}
