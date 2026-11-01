using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>How stamina is spent and recovered.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Lab/Stamina Config", fileName = "StaminaConfig")]
    public sealed class StaminaConfig : ScriptableObject
    {
        [field: SerializeField, Min(1f), Tooltip("Stamina when full.")]
        public float Max { get; private set; } = 100f;

        [field: SerializeField, Min(0f), Tooltip("Stamina recovered per second.")]
        public float RecoveryPerSecond { get; private set; } = 6f;

        [field: SerializeField, Min(0f), Tooltip("Stamina lost when a combo fails.")]
        public float FailedComboCost { get; private set; } = 6f;
    }
}
