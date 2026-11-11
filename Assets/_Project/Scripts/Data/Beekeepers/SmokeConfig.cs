using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>The smoke grenade and the cloud it leaves, which hurts hives and the swarm.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Smoke Config", fileName = "SmokeConfig")]
    public sealed class SmokeConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f), Tooltip("Seconds a grenade flies before it lands.")]
        public float FlightSeconds { get; private set; } = 1.2f;

        [field: SerializeField, Min(0.1f), Tooltip("Radius of the smoke cloud in metres.")]
        public float Radius { get; private set; } = 4f;

        [field: SerializeField, Min(0f), Tooltip("Seconds a cloud lasts.")]
        public float LifeSeconds { get; private set; } = 5f;

        [field: SerializeField, Min(0f), Tooltip("Damage per second to a hive inside the cloud.")]
        public float HiveDamagePerSecond { get; private set; } = 4f;

        [field: SerializeField, Min(0f), Tooltip("Wasps lost per second while the core is inside the cloud.")]
        public float WaspLossPerSecond { get; private set; } = 48f;
    }
}
