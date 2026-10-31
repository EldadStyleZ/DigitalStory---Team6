using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>How the game sounds. Everything is generated in code, so nothing here is an audio file.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Sound Config", fileName = "SoundConfig")]
    public sealed class SoundConfig : ScriptableObject
    {
        [field: Header("Overall")]
        [field: SerializeField, Range(0f, 1f), Tooltip("The volume of everything.")]
        public float MasterVolume { get; private set; } = .9f;

        [field: Header("Flute pitch")]
        [field: SerializeField, Min(1f), Tooltip("The pitch of hole 1, in hertz.")]
        public float FluteBaseHertz { get; private set; } = 587.33f;

        [SerializeField, Tooltip("How many semitones above hole 1 each hole sounds.")]
        private int[] _fluteSemitones = { 0, 2, 5, 7, 9, 12 };

        [field: SerializeField, Min(0f), Tooltip("How fast the vibrato wobbles, in hertz.")]
        public float VibratoHertz { get; private set; } = 5.3f;

        [field: SerializeField, Min(0f), Tooltip("How far the vibrato bends the pitch, in hertz.")]
        public float VibratoDepthHertz { get; private set; } = 6f;

        [field: SerializeField, Min(0.0001f), Tooltip("Seconds to slide to a new note.")]
        public float GlideSeconds { get; private set; } = .012f;

        [field: Header("Flute tone")]
        [field: SerializeField, Range(0f, 1f), Tooltip("How loud the octave above the note is.")]
        public float OvertoneShare { get; private set; } = .25f;

        [field: SerializeField, Min(1f), Tooltip("The brightest the tone gets, in hertz.")]
        public float BrightnessHertz { get; private set; } = 2800f;

        [field: SerializeField, Min(1f), Tooltip("The pitch of the breath noise, in hertz.")]
        public float BreathHertz { get; private set; } = 1900f;

        [field: SerializeField, Range(0f, 1f), Tooltip("How loud the breath noise is.")]
        public float BreathShare { get; private set; } = .12f;

        [field: Header("Flute volume")]
        [field: SerializeField, Range(0f, 1f), Tooltip("The flute's volume while a note holds.")]
        public float FluteVolume { get; private set; } = .13f;

        [field: SerializeField, Range(0f, 1f), Tooltip("The volume a new note starts from, which gives it a soft attack.")]
        public float NoteStartVolume { get; private set; } = .06f;

        [field: SerializeField, Min(0f), Tooltip("Seconds a new note stays at its start volume.")]
        public float NoteStartSeconds { get; private set; } = .035f;

        [field: SerializeField, Min(0.0001f), Tooltip("Seconds for the flute to swell in.")]
        public float SwellSeconds { get; private set; } = .03f;

        [field: SerializeField, Min(0.0001f), Tooltip("Seconds for the flute to fade out.")]
        public float FadeSeconds { get; private set; } = .05f;

        public int HoleCount => _fluteSemitones.Length;

        /// <summary>The pitch of a hole, in hertz.</summary>
        public float HertzOfHole(int hole) => FluteBaseHertz * Mathf.Pow(2f, _fluteSemitones[hole] / 12f);
    }
}
