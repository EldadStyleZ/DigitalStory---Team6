using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>When hidden enemies appear, and how much warning they give.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Lab/Ambush Config", fileName = "AmbushConfig")]
    public sealed class AmbushConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f), Tooltip("How close the Piper must walk to set one off, in tiles of walking.")]
        public float TriggerTiles { get; private set; } = 10f;

        [field: SerializeField, Min(0f), Tooltip("Seconds between setting one off and its warning.")]
        public float DelaySeconds { get; private set; } = 1.5f;

        [field: SerializeField, Min(0f), Tooltip("Seconds of warning before the enemies appear, so the surprise is fair.")]
        public float WarningSeconds { get; private set; } = 0.8f;

        [field: SerializeField, Min(1), Tooltip("How many cells from the Piper they appear.")]
        public int EmergeSteps { get; private set; } = 2;

        [field: SerializeField, Min(0f), Tooltip("Seconds after one before another can start, so the tension can drop.")]
        public float RestSeconds { get; private set; } = 25f;
    }
}
