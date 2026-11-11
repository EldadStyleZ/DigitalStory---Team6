using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>The numbers behind auto-aim: how far each song looks, how long a target is kept and how crowds are judged.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Aim Config", fileName = "AimConfig")]
    public sealed class AimConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f), Tooltip("Sting's reach from the Piper, in metres.")]
        public float StingRange { get; private set; } = 12f;

        [field: SerializeField, Min(0f), Tooltip("Scythe's reach from the Piper, in metres.")]
        public float ScytheRange { get; private set; } = 6f;

        [field: SerializeField, Min(0f), Tooltip("Hydra's reach from the Piper, in metres.")]
        public float HydraRange { get; private set; } = 9f;

        [field: SerializeField, Min(0f), Tooltip("Lance's reach from the Piper, in metres.")]
        public float LanceRange { get; private set; } = 14f;

        [field: SerializeField, Min(0f), Tooltip("Meteor's reach from the Piper, in metres.")]
        public float MeteorRange { get; private set; } = 18f;

        [field: SerializeField, Min(0f), Tooltip("Nest's reach from the Piper, in metres.")]
        public float NestRange { get; private set; } = 10f;

        [field: SerializeField, Min(0f), Tooltip("Maelstrom's reach from the Piper, in metres.")]
        public float MaelstromRange { get; private set; } = 12f;

        [field: SerializeField, Min(0f), Tooltip("Seconds a chosen target is kept while it lives and stays in range.")]
        public float HoldSeconds { get; private set; } = 1f;

        [field: SerializeField, Min(0f), Tooltip("Metres taken off a beekeeper's threat score when it stands near a standing hive.")]
        public float HiveThreatBonus { get; private set; } = 6f;

        [field: SerializeField, Min(0f), Tooltip("How close to a standing hive, in metres, a beekeeper counts as threatening it.")]
        public float HiveThreatRadius { get; private set; } = 3f;

        [field: SerializeField, Min(0f), Tooltip("Width in metres of the strip Lance counts beekeepers in.")]
        public float LineWidth { get; private set; } = 1.2f;

        [field: SerializeField, Min(0f), Tooltip("Radius in metres within which beekeepers count as a crowd.")]
        public float CrowdRadius { get; private set; } = 3.5f;

        [field: SerializeField, Min(0f), Tooltip("Seconds a hive hit counts as recent for Honeycomb.")]
        public float HiveRecentSeconds { get; private set; } = 3f;

        [field: SerializeField, Min(0f), Tooltip("Metres ahead of the Piper that an aimed song strikes when nobody is in range.")]
        public float EmptyAimDistance { get; private set; } = 6f;

        [field: SerializeField, Min(0f), Tooltip("Metres from the mouse's ground point within which a beekeeper is picked while the mouse is live.")]
        public float MouseRadius { get; private set; } = 3f;

        [field: SerializeField, Min(1), Tooltip("How many beekeepers ThreeNearest picks.")]
        public int NearestCount { get; private set; } = 3;
    }
}
