using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>How cats move, hunt, react to the flute and frighten rats.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Cat Config", fileName = "CatConfig")]
    public sealed class CatConfig : ScriptableObject
    {
        [field: Header("Body")]
        [field: SerializeField, Min(0f), Tooltip("A cat's collision radius.")]
        public float Radius { get; private set; } = 9f;

        [field: Header("Spawning")]
        [field: SerializeField, Min(0f), Tooltip("Closest a cat can start to the Piper.")]
        public float MinimumStartDistance { get; private set; } = 450f;

        [field: Header("Moving")]
        [field: SerializeField, Min(0f), Tooltip("Speed while walking the streets.")]
        public float PatrolSpeed { get; private set; } = 70f;

        [field: SerializeField, Min(0f), Tooltip("Speed while chasing a rat.")]
        public float HuntSpeed { get; private set; } = 140f;

        [field: SerializeField, Min(0f), Tooltip("Speed while running from the Piper.")]
        public float FleeSpeed { get; private set; } = 175f;

        [field: SerializeField, Min(0f), Tooltip("How quickly a cat reaches the speed it wants.")]
        public float SteeringRate { get; private set; } = 4f;

        [field: SerializeField, Min(0f), Tooltip("Distance at which a cat has reached a junction.")]
        public float ArrivalDistance { get; private set; } = 16f;

        [field: SerializeField, Min(0f), Tooltip("How far from the bank a cat stays.")]
        public float RiverSetback { get; private set; } = 40f;

        [field: SerializeField, Min(0f), Tooltip("Closest a cat can get to the town edge.")]
        public float TownMargin { get; private set; } = 8f;

        [field: Header("Hunting")]
        [field: SerializeField, Min(0f), Tooltip("How far a cat notices rats, measured along the streets.")]
        public float HuntRange { get; private set; } = 220f;

        [field: SerializeField, Min(0f), Tooltip("Distance at which a cat catches a rat.")]
        public float EatDistance { get; private set; } = 11f;

        [field: SerializeField, Min(0f), Tooltip("Seconds a cat ignores rats after eating.")]
        public float EatPauseSeconds { get; private set; } = 1.6f;

        [field: Header("The flute")]
        [field: SerializeField, Min(0f), Tooltip("How far past the music a scare still reaches a cat.")]
        public float ScareExtraRange { get; private set; } = 60f;

        [field: SerializeField, Min(0f), Tooltip("Seconds a cat keeps running after a scare.")]
        public float ScareSeconds { get; private set; } = 1.5f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Share of its speed a sleeping cat keeps each sixtieth of a second.")]
        public float SleepSlowdown { get; private set; } = .9f;

        [field: Header("Effect on rats")]
        [field: SerializeField, Min(1f), Tooltip("How far rats sense an awake cat.")]
        public float FearRange { get; private set; } = 130f;

        [field: SerializeField, Min(0f), Tooltip("Strength of the push right beside a cat.")]
        public float FearStrength { get; private set; } = 520f;

        [field: SerializeField, Min(0f), Tooltip("Charm a rat loses per second near a cat.")]
        public float CharmLossPerSecond { get; private set; } = 1.3f;
    }
}
