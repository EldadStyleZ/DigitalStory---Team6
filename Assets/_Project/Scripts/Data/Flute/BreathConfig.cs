using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>How playing uses breath and how it comes back.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Breath Config", fileName = "BreathConfig")]
    public sealed class BreathConfig : ScriptableObject
    {
        [field: SerializeField, Min(1f), Tooltip("Full breath.")]
        public float Max { get; private set; } = 100f;

        [field: SerializeField, Min(0f), Tooltip("Breath used per second of playing.")]
        public float DrainPerSecond { get; private set; } = 11f;

        [field: SerializeField, Min(0f), Tooltip("Breath regained per second at rest.")]
        public float RecoveryPerSecond { get; private set; } = 22f;

        [field: SerializeField, Min(0f), Tooltip("Breath regained per second while a hole is held but silent.")]
        public float RecoveryWhileHeldPerSecond { get; private set; } = 9f;

        [field: SerializeField, Min(0f), Tooltip("Breath needed before an exhausted Piper can play again.")]
        public float RecoveredAt { get; private set; } = 35f;
    }
}
