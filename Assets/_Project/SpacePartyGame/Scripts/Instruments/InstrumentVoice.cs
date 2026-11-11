using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// Where a player's instrument comes out: an AudioSource whose samples the <see cref="PlayerInstrument"/>
    /// writes, as the toy's FluteVoice does. It sits on its own child, because its sound replaces anything else the
    /// AudioSource would play.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class InstrumentVoice : MonoBehaviour
    {
        [SerializeField, Tooltip("The instrument whose notes this voice plays.")]
        private PlayerInstrument _instrument;

        private AudioSource _source;

        /// <summary>Starts the AudioSource, so Unity begins asking for samples.</summary>
        public void StartPlaying()
        {
            if (!_source.isPlaying)
            {
                _source.Play();
            }
        }

        private void Awake()
        {
            _source = GetComponent<AudioSource>();

            if (_instrument == null)
            {
                Debug.LogError($"{name}: InstrumentVoice is missing its instrument.", this);
            }
        }

        // Unity calls this on the audio thread to ask for the next samples.
        private void OnAudioFilterRead(float[] data, int channels)
        {
            _instrument?.FillAudio(data, channels);
        }
    }
}
