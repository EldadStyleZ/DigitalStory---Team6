using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>How the lab sounds.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Lab/Sound Config", fileName = "LabSoundConfig")]
    public sealed class LabSoundConfig : ScriptableObject
    {
        [SerializeField, Tooltip("One sound for each flute note, in order. Keep their pitches the same as the band's.")]
        private SoundCue[] _notes =
        {
            new SoundCue(new Tone(ToneShape.Triangle, 440f, 440f, 0.24f, 0.26f), new Tone(ToneShape.Sine, 880f, 880f, 0.18f, 0.05f)),
            new SoundCue(new Tone(ToneShape.Triangle, 523.25f, 523.25f, 0.24f, 0.26f), new Tone(ToneShape.Sine, 1046.5f, 1046.5f, 0.18f, 0.05f)),
            new SoundCue(new Tone(ToneShape.Triangle, 659.25f, 659.25f, 0.24f, 0.26f), new Tone(ToneShape.Sine, 1318.5f, 1318.5f, 0.18f, 0.05f)),
            new SoundCue(new Tone(ToneShape.Triangle, 784f, 784f, 0.24f, 0.26f), new Tone(ToneShape.Sine, 1568f, 1568f, 0.18f, 0.05f)),
        };

        public IReadOnlyList<SoundCue> Notes => _notes;

        [field: SerializeField, Tooltip("The band behind every combo's tune.")]
        public SongBand Band { get; private set; } = new SongBand();

        [field: SerializeField, Min(0f), Tooltip("The shortest gap between hit sounds, so a big fight does not become a roar.")]
        public float HitGapSeconds { get; private set; } = 0.09f;

        [field: SerializeField, Tooltip("When a combo fails.")]
        public SoundCue Failed { get; private set; } = new SoundCue(new Tone(ToneShape.Sawtooth, 180f, 110f, 0.22f, 0.25f));

        [field: SerializeField, Tooltip("When a note or combo is refused.")]
        public SoundCue Refused { get; private set; } = new SoundCue(new Tone(ToneShape.Square, 160f, 160f, 0.07f, 0.1f));

        [field: SerializeField, Tooltip("When something is hit.")]
        public SoundCue Hit { get; private set; } = new SoundCue(new Tone(ToneShape.Noise, 900f, 400f, 0.07f, 0.1f));

        [field: SerializeField, Tooltip("When an area hit lands.")]
        public SoundCue AreaHit { get; private set; } = new SoundCue(new Tone(ToneShape.Noise, 300f, 120f, 0.3f, 0.2f));

        [field: SerializeField, Tooltip("The warning that hidden enemies are coming.")]
        public SoundCue AmbushWarning { get; private set; } = new SoundCue(
            new Tone(ToneShape.Sawtooth, 220f, 233f, 0.5f, 0.2f),
            new Tone(ToneShape.Sawtooth, 233f, 247f, 0.4f, 0.2f, 0.25f));

        [field: SerializeField, Tooltip("When hidden enemies appear.")]
        public SoundCue AmbushSprung { get; private set; } = new SoundCue(
            new Tone(ToneShape.Noise, 1400f, 200f, 0.25f, 0.3f),
            new Tone(ToneShape.Square, 110f, 82f, 0.3f, 0.16f));

        [field: SerializeField, Tooltip("When the Piper reaches a new level.")]
        public SoundCue LevelUp { get; private set; } = new SoundCue(
            new Tone(ToneShape.Triangle, 523f, 523f, 0.14f, 0.3f),
            new Tone(ToneShape.Triangle, 659f, 659f, 0.14f, 0.3f, 0.12f),
            new Tone(ToneShape.Triangle, 1047f, 1047f, 0.3f, 0.3f, 0.24f));

        [field: SerializeField, Tooltip("When the Piper falls.")]
        public SoundCue Died { get; private set; } = new SoundCue(new Tone(ToneShape.Sawtooth, 300f, 70f, 0.8f, 0.3f));

        [field: SerializeField, Tooltip("When the last boss falls.")]
        public SoundCue Won { get; private set; } = new SoundCue(
            new Tone(ToneShape.Triangle, 523f, 523f, 0.2f, 0.3f),
            new Tone(ToneShape.Triangle, 784f, 784f, 0.2f, 0.3f, 0.18f),
            new Tone(ToneShape.Triangle, 1047f, 1047f, 0.5f, 0.3f, 0.36f));

        /// <summary>Every sound here, so all of them can be prepared at once.</summary>
        public IEnumerable<SoundCue> EveryCue
        {
            get
            {
                foreach (SoundCue note in _notes)
                {
                    yield return note;
                }

                yield return Failed;
                yield return Refused;
                yield return Hit;
                yield return AreaHit;
                yield return AmbushWarning;
                yield return AmbushSprung;
                yield return LevelUp;
                yield return Died;
                yield return Won;
            }
        }
    }
}
