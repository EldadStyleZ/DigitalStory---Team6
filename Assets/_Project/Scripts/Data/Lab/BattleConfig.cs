using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>How creatures and minions move and fight.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Lab/Battle Config", fileName = "BattleConfig")]
    public sealed class BattleConfig : ScriptableObject
    {
        [field: SerializeField, Min(1), Tooltip("The most creatures alive at once.")]
        public int MaxCreatures { get; private set; } = 6;

        [field: SerializeField, Min(0f), Tooltip("How far fighters stay from walls, in tiles.")]
        public float UnitRadius { get; private set; } = 0.35f;

        [field: SerializeField, Min(0f), Tooltip("How far apart fighters try to stand, in tiles.")]
        public float PersonalSpace { get; private set; } = 0.9f;

        [field: SerializeField, Min(0f), Tooltip("How fast crowded fighters drift apart, in tiles per second.")]
        public float SpreadSpeed { get; private set; } = 2f;

        [field: SerializeField, Min(0f), Tooltip("How close a creature stays to the Piper when nothing needs fighting, in tiles.")]
        public float FollowTiles { get; private set; } = 2.5f;

        [field: SerializeField, Min(0f), Tooltip("How far a creature will go from the Piper to look for a fight, in tiles of walking.")]
        public float LeashTiles { get; private set; } = 24f;

        [field: SerializeField, Min(1f), Tooltip("A minion gives up past this many times its detection range.")]
        public float GiveUpMultiplier { get; private set; } = 1.6f;

        [field: SerializeField, Min(0f), Tooltip("Seconds without a fight before a creature goes home, so nobody walks the maze with a full army.")]
        public float QuietSecondsBeforeLeaving { get; private set; } = 20f;

        [field: SerializeField, Min(0f), Tooltip("How close fighters must stand to count as a pack, in tiles.")]
        public float PackTiles { get; private set; } = 3f;

        [field: SerializeField, Range(0f, 1f), Tooltip("The share of health under which a target counts as nearly finished.")]
        public float FinisherHealthShare { get; private set; } = 0.5f;

        [field: SerializeField, Min(0.02f), Tooltip("Seconds between route updates.")]
        public float RouteSeconds { get; private set; } = 0.2f;
    }
}
