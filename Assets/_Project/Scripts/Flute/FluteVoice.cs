using System.Threading;
using UnityEngine;

namespace PiperToy
{
    /// <summary>
    /// The flute's voice: it plays whichever hole the Flute is sounding, with the old game's synthesizer, so a note
    /// glides in and fades out instead of stopping dead.
    /// </summary>
    /// <remarks>
    /// It sits on its own child object, because its sound replaces everything its AudioSource would play, and the
    /// song tunes need an AudioSource of their own. Unity calls OnAudioFilterRead on the audio thread, many times a
    /// second, to ask for the next samples.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class FluteVoice : MonoBehaviour
    {
        [SerializeField, Tooltip("The flute whose held note this voice plays.")]
        private Flute _flute;

        [SerializeField, Tooltip("How the flute sounds.")]
        private FluteSound _sound = new FluteSound();

        private AudioSource _source;
        private FluteSynth _synth;

        /// <summary>The key, from 1 to 6, being played now, or 0 for silence. For tests.</summary>
        public int KeyPlaying { get; private set; }

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
        }

        // Plugging in headphones can stop every sound and change the sample rate, so the voice starts again.
        private void OnEnable()
        {
            AudioSettings.OnAudioConfigurationChanged += HandleAudioChanged;
        }

        private void OnDisable()
        {
            AudioSettings.OnAudioConfigurationChanged -= HandleAudioChanged;
        }

        private void Start()
        {
            StartTheVoice();
        }

        private void Update()
        {
            KeyPlaying = _flute.SoundingKey;
            _synth?.Play(KeyPlaying);
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            Volatile.Read(ref _synth)?.Fill(data, channels);
        }

        private void StartTheVoice()
        {
            int sampleRate = AudioSettings.outputSampleRate;

            // A machine with no sound device has no sample rate, and the toy simply plays silently.
            if (sampleRate <= 0)
            {
                return;
            }

            Volatile.Write(ref _synth, new FluteSynth(_sound, sampleRate, Flute.HoleCount));
            _source.Play();
        }

        private void HandleAudioChanged(bool deviceWasChanged)
        {
            StartTheVoice();
        }
    }
}
