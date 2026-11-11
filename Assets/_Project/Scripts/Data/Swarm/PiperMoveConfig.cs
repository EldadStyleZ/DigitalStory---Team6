using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>How fast the Piper moves and turns, and what slows it down.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Piper Move Config", fileName = "PiperMoveConfig")]
    public sealed class PiperMoveConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f), Tooltip("Walking speed in metres per second.")]
        public float Speed { get; private set; } = 4.5f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Share of speed kept while the dome is up.")]
        public float DomeSpeedMultiplier { get; private set; } = 0.5f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Share of speed kept while standing in smoke.")]
        public float SmokeSpeedMultiplier { get; private set; } = 0.6f;

        [field: SerializeField, Min(0f), Tooltip("Seconds after a note that the Piper counts as playing.")]
        public float PlayingHoldSeconds { get; private set; } = 0.6f;

        [field: SerializeField, Min(0f), Tooltip("Shortest time a flute note sounds, in seconds, even for a tap. A held key sounds longer.")]
        public float NoteSeconds { get; private set; } = 0.3f;

        [field: SerializeField, Min(0f), Tooltip("How fast the Piper turns, in degrees per second.")]
        public float TurnDegreesPerSecond { get; private set; } = 720f;

        [field: SerializeField, Min(0.1f), Tooltip("Metres per second the Run clip was authored for. Used to scale its playback.")]
        public float RunClipSpeed { get; private set; } = 4.5f;
    }
}
