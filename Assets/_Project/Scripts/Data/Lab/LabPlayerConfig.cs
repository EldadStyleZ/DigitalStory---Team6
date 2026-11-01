using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>How the Piper moves and how much the Piper can take in the lab.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Lab/Player Config", fileName = "LabPlayerConfig")]
    public sealed class LabPlayerConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f), Tooltip("Walking speed, in tiles per second.")]
        public float MoveSpeed { get; private set; } = 6f;

        [field: SerializeField, Min(0f), Tooltip("How far the Piper stays from walls, in tiles.")]
        public float Radius { get; private set; } = 0.4f;

        [field: SerializeField, Min(1f), Tooltip("Health at the start, and after each revival.")]
        public float MaxHealth { get; private set; } = 100f;
    }
}
