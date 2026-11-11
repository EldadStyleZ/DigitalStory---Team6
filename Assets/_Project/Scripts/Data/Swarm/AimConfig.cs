using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// The numbers behind auto-aim: how long a target is kept and how threats, lines and crowds are judged. Each song's reach
    /// lives on its own song asset (its AimRange), never here, so the marker and the cast cannot disagree.
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Aim Config", fileName = "AimConfig")]
    public sealed class AimConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f), Tooltip("Seconds a rule keeps the target it chose while it lives and stays in range.")]
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

        [field: SerializeField, Min(0f), Tooltip("Metres ahead of the core that an aimed song strikes when nobody is in range.")]
        public float EmptyAimDistance { get; private set; } = 6f;

        [field: SerializeField, Min(0f), Tooltip("Metres from the mouse's ground point within which a beekeeper is picked while the mouse is live.")]
        public float MouseRadius { get; private set; } = 3f;

        [field: SerializeField, Min(1), Tooltip("The most beekeepers ThreeNearest picks, nearest first: one for each of the Hydra's most heads (5 from level 7). A Hydra with fewer heads takes the nearest of them.")]
        public int NearestCount { get; private set; } = 5;
    }
}
