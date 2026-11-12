using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// A hive: the swarm's life and its income. Its health, how long it counts as under attack, the store of wasps it makes,
    /// and how wasps pass between it and the core (plan section M).
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Hive Config", fileName = "HiveConfig")]
    public sealed class HiveConfig : ScriptableObject
    {
        [field: SerializeField, Min(1f), Tooltip("Health of one hive.")]
        public float Health { get; private set; } = 450f;

        [field: SerializeField, Min(0f), Tooltip("Seconds after a hit that the hive still counts as under attack.")]
        public float UnderAttackSeconds { get; private set; } = 2f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Share of full health the last hive starts a match with, so the first lesson has a hive to heal.")]
        public float DamagedStartFraction { get; private set; } = 0.8f;

        [field: Header("Store")]
        [field: SerializeField, Min(0f), Tooltip("Wasps a second the hive makes into its own store while its health is at or above ProductionHealthFraction.")]
        public float ProductionPerSecond { get; private set; } = 6f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Share of full health at or above which the hive makes wasps. Below it the hive makes nothing, which is the reason to heal it.")]
        public float ProductionHealthFraction { get; private set; } = 0.5f;

        [field: SerializeField, Min(0f), Tooltip("Most wasps the hive's store holds. A full store takes no more.")]
        public float StoreCapacity { get; private set; } = 300f;

        [field: SerializeField, Min(0f), Tooltip("Wasps in each hive's store when a match starts. Keep it at most StoreCapacity.")]
        public float StartStore { get; private set; } = 100f;

        [field: Header("Collecting and giving")]
        [field: SerializeField, Min(0f), Tooltip("Metres on the ground from the hive within which the core collects its store, and can hold Give to heal it.")]
        public float CollectRadius { get; private set; } = 3f;

        [field: SerializeField, Min(0f), Tooltip("Wasps a second the store flows into the swarm: to the core within CollectRadius, or to the core anywhere during Hive Call. It stops when the swarm is full.")]
        public float CollectPerSecond { get; private set; } = 150f;

        [field: SerializeField, Min(0f), Tooltip("Wasps a second that flow from the swarm into the hive's health while Give is held within CollectRadius.")]
        public float HealWaspsPerSecond { get; private set; } = 60f;

        // 3 (was 2.5), set by the playtest bot once the Sting's damage came in pulses from its arrival: kills came sooner, so
        // the hives took fewer hits and a win healed with less than 15% of its wasps.
        [field: SerializeField, Min(0.01f), Tooltip("Wasps given for each point of health healed.")]
        public float WaspsPerHealthPoint { get; private set; } = 3f;
    }
}
