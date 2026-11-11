using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>A hive: the Piper's life. Its health and how long it counts as under attack.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Hive Config", fileName = "HiveConfig")]
    public sealed class HiveConfig : ScriptableObject
    {
        [field: SerializeField, Min(1f), Tooltip("Health of one hive.")]
        public float Health { get; private set; } = 300f;

        [field: SerializeField, Min(0f), Tooltip("Seconds after a hit that the hive still counts as under attack.")]
        public float UnderAttackSeconds { get; private set; } = 2f;
    }
}
