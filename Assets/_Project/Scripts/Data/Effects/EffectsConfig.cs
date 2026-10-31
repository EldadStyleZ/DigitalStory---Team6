using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>The look and timing of notes, rings, ripples and puffs.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Effects Config", fileName = "EffectsConfig")]
    public sealed class EffectsConfig : ScriptableObject
    {
        [field: Header("Notes from the flute")]
        [field: SerializeField, Min(0.01f), Tooltip("Seconds between notes while playing.")]
        public float NoteInterval { get; private set; } = .2f;

        [field: SerializeField, Min(0f), Tooltip("How far above the Piper a note starts, in town units.")]
        public float NoteStartHeight { get; private set; } = 14f;

        [field: SerializeField, Min(0f), Tooltip("How far to either side a note starts, in town units.")]
        public float NoteStartSpread { get; private set; } = 10f;

        [field: SerializeField, Min(0f), Tooltip("How fast a note rises.")]
        public float NoteRiseSpeed { get; private set; } = 34f;

        [field: SerializeField, Min(0f), Tooltip("The fastest a note drifts sideways.")]
        public float NoteDriftSpeed { get; private set; } = 15f;

        [field: SerializeField, Min(0f), Tooltip("Seconds a note lasts.")]
        public float NoteSeconds { get; private set; } = 1.6f;

        [field: SerializeField, Min(0f), Tooltip("How big a note is, in town units.")]
        public float NoteSize { get; private set; } = 18f;

        [field: Header("Notes from the choir")]
        [field: SerializeField, Min(0f), Tooltip("How many notes the rats sing per second.")]
        public float ChoirNotesPerSecond { get; private set; } = 10f;

        [field: SerializeField, Tooltip("The color of the rats' notes.")]
        public Color ChoirNoteColor { get; private set; } = new Color32(0xFF, 0xAA, 0xE6, 0xFF);

        [field: SerializeField, Min(0f), Tooltip("How fast the rats' notes rise.")]
        public float ChoirRiseSpeed { get; private set; } = 40f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the rats' notes last.")]
        public float ChoirNoteSeconds { get; private set; } = 1.2f;

        [field: Header("Rings from the flute")]
        [field: SerializeField, Min(0.01f), Tooltip("Seconds between rings while calling or marching.")]
        public float RingInterval { get; private set; } = .7f;

        [field: SerializeField, Min(0.01f), Tooltip("Seconds between rings while scaring.")]
        public float ScareRingInterval { get; private set; } = .35f;

        [field: SerializeField, Min(1f), Tooltip("How fast a ring grows while calling or marching.")]
        public float RingSpeed { get; private set; } = 240f;

        [field: SerializeField, Min(1f), Tooltip("How fast a ring grows while scaring.")]
        public float ScareRingSpeed { get; private set; } = 420f;

        [field: SerializeField, Min(0f), Tooltip("How big a ring starts, in town units.")]
        public float RingStartRadius { get; private set; } = 16f;

        [field: SerializeField, Range(0f, 1f), Tooltip("How visible a ring is at its start.")]
        public float RingOpacity { get; private set; } = .5f;

        [field: SerializeField, Range(0f, 1f), Tooltip("How visible the circle around the Piper is while playing.")]
        public float MusicCircleOpacity { get; private set; } = .32f;

        [field: Header("Ripples")]
        [field: SerializeField, Tooltip("The color of a ripple.")]
        public Color RippleColor { get; private set; } = new Color(190f / 255f, 230f / 255f, 245f / 255f, .6f);

        [field: SerializeField, Min(0f), Tooltip("How big a ripple starts, in town units.")]
        public float RippleStartRadius { get; private set; } = 3f;

        [field: SerializeField, Min(0f), Tooltip("How fast a ripple grows.")]
        public float RippleGrowth { get; private set; } = 26f;

        [field: SerializeField, Min(0.01f), Tooltip("Seconds a ripple lasts.")]
        public float RippleSeconds { get; private set; } = 1.1f;

        [field: SerializeField, Range(0f, 1f), Tooltip("How flat a ripple looks, where 1 is a circle.")]
        public float RippleFlatness { get; private set; } = .6f;

        [field: Header("Puffs")]
        [field: SerializeField, Min(0f), Tooltip("How big a puff dot is, in town units.")]
        public float PuffSize { get; private set; } = 4.4f;

        [field: SerializeField, Tooltip("The dots when a rat falls in the river.")]
        public PuffStyle SplashPuff { get; private set; } = new PuffStyle(new Color32(0xBE, 0xE6, 0xF0, 0xFF), 3, 22f, .9f);

        [field: SerializeField, Tooltip("The dots when a cat catches a rat.")]
        public PuffStyle EatenPuff { get; private set; } = new PuffStyle(new Color32(0x96, 0x82, 0x78, 0xFF), 5, 30f, .6f);

        [field: SerializeField, Tooltip("The dots when a balloon pops.")]
        public PuffStyle PopPuff { get; private set; } = new PuffStyle(new Color32(0xFF, 0x96, 0xC8, 0xFF), 4, 40f, .5f);
    }
}
