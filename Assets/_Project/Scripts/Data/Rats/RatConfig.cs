using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>How rats spawn, flock, get charmed and drown.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Rat Config", fileName = "RatConfig")]
    public sealed class RatConfig : ScriptableObject
    {
        [field: Header("Body")]
        [field: SerializeField, Min(0f), Tooltip("A rat's collision radius.")]
        public float Radius { get; private set; } = 5f;

        [field: Header("Spawning")]
        [field: SerializeField, Min(0f), Tooltip("How far rats spawn from their nest.")]
        public float NestScatter { get; private set; } = 55f;

        [field: SerializeField, Min(0f), Tooltip("Closest a nest can be to the Piper at the start.")]
        public float MinimumNestDistance { get; private set; } = 380f;

        [field: SerializeField, Min(0f), Tooltip("Closest a rat can spawn to the town edge.")]
        public float SpawnMargin { get; private set; } = 6f;

        [field: Header("Flocking")]
        [field: SerializeField, Min(1f), Tooltip("How far a rat notices its neighbours. Also the size of a grid cell.")]
        public float NeighbourRadius { get; private set; } = 36f;

        [field: SerializeField, Min(0f), Tooltip("Rats closer than this push apart.")]
        public float SeparationDistance { get; private set; } = 13f;

        [field: SerializeField, Min(0f), Tooltip("Strength of the push apart.")]
        public float SeparationStrength { get; private set; } = 30f;

        [field: SerializeField, Min(0f), Tooltip("Strength of matching neighbours' velocity.")]
        public float AlignmentStrength { get; private set; } = .8f;

        [field: SerializeField, Min(0f), Tooltip("Strength of moving to the middle of the neighbours.")]
        public float CohesionStrength { get; private set; } = .9f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Charm above which a rat flocks less.")]
        public float LooseFlockCharm { get; private set; } = .3f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Flocking strength left on a charmed rat.")]
        public float LooseFlockShare { get; private set; } = .4f;

        [field: Header("Wandering")]
        [field: SerializeField, Min(0f), Tooltip("How sharply a rat wanders off its heading.")]
        public float WanderTurnRate { get; private set; } = 5f;

        [field: SerializeField, Min(0f), Tooltip("Strength of wandering.")]
        public float WanderStrength { get; private set; } = 45f;

        [field: SerializeField, Min(0f), Tooltip("Distance from the nest where a rat starts drifting home.")]
        public float HomeRange { get; private set; } = 90f;

        [field: SerializeField, Min(0f), Tooltip("Strength of drifting home.")]
        public float HomeStrength { get; private set; } = 35f;

        [field: Header("Charm")]
        [field: SerializeField, Min(0f), Tooltip("Extra charm per second right beside the Piper, fading to none at the edge of the music.")]
        public float CharmRiseNearPerSecond { get; private set; } = 2.2f;

        [field: SerializeField, Min(0f), Tooltip("Charm per second anywhere inside the music.")]
        public float CharmRiseBasePerSecond { get; private set; } = .6f;

        [field: SerializeField, Min(0f), Tooltip("Charm per second caught from a charmed neighbour.")]
        public float CharmSpreadPerSecond { get; private set; } = .55f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Charm a neighbour needs before it spreads.")]
        public float CharmSpreadThreshold { get; private set; } = .6f;

        [field: SerializeField, Min(0f), Tooltip("Charm lost per second without music.")]
        public float CharmFadePerSecond { get; private set; } = .22f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Charm at which a rat counts as charmed.")]
        public float CharmedAt { get; private set; } = .5f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Charm below which a rat ignores the Piper.")]
        public float MinimumFollowCharm { get; private set; } = .02f;

        [field: Header("Following")]
        [field: SerializeField, Min(0f), Tooltip("Top speed of an uncharmed rat.")]
        public float BaseSpeed { get; private set; } = 80f;

        [field: SerializeField, Min(0f), Tooltip("Extra top speed at full charm.")]
        public float CharmSpeedBonus { get; private set; } = 110f;

        [field: SerializeField, Min(0f), Tooltip("Distance at which a following rat slows down.")]
        public float FollowStopDistance { get; private set; } = 48f;

        [field: SerializeField, Min(0f), Tooltip("How firmly a charmed rat steers.")]
        public float SteeringStrength { get; private set; } = 3f;

        [field: Header("River")]
        [field: SerializeField, Range(0f, 1f), Tooltip("Charm below which a rat avoids the bank.")]
        public float BankFearCharm { get; private set; } = .4f;

        [field: SerializeField, Min(0f), Tooltip("Distance from the bank where an uncharmed rat turns back.")]
        public float BankFearDistance { get; private set; } = 90f;

        [field: SerializeField, Min(0f), Tooltip("Strength of turning back from the bank.")]
        public float BankFearStrength { get; private set; } = 7f;

        [field: SerializeField, Min(0f), Tooltip("How far past the bank a rat drowns.")]
        public float DrownMargin { get; private set; } = 4f;
    }
}
