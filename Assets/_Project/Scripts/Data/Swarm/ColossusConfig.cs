using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>The Colossus, the human form of the swarm: its body, its attacks and how long it lasts.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Colossus Config", fileName = "ColossusConfig")]
    public sealed class ColossusConfig : ScriptableObject
    {
        [field: Header("Body")]
        [field: SerializeField, Min(0.1f), Tooltip("Height of the Colossus in metres.")]
        public float Height { get; private set; } = 4f;

        [field: SerializeField, Min(0f), Tooltip("Walking speed in metres per second.")]
        public float Speed { get; private set; } = 3.5f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the Colossus lasts.")]
        public float DurationSeconds { get; private set; } = 12f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the wasps take to build the Colossus.")]
        public float BuildSeconds { get; private set; } = 0.8f;

        [field: SerializeField, Min(0f), Tooltip("Seconds a beekeeper stays stunned after being hit by the Colossus.")]
        public float StunSeconds { get; private set; } = 0.5f;

        [field: SerializeField, Min(1f), Tooltip("Camera distance while the Colossus stands.")]
        public float CameraDistance { get; private set; } = 20f;

        [field: Header("Punch")]
        [field: SerializeField, Min(0f), Tooltip("Damage of a punch.")]
        public float PunchDamage { get; private set; } = 45f;

        [field: SerializeField, Min(0f), Tooltip("Radius of the punch's hit area.")]
        public float PunchRadius { get; private set; } = 1.5f;

        [field: SerializeField, Min(0f), Tooltip("How far in front of the Colossus the punch lands.")]
        public float PunchReach { get; private set; } = 2.5f;

        [field: SerializeField, Min(0f), Tooltip("Seconds from the start of the punch until it lands.")]
        public float PunchHitDelay { get; private set; } = 0.2f;

        [field: SerializeField, Min(0f), Tooltip("Seconds after a punch to press again and continue the chain.")]
        public float ChainWindowSeconds { get; private set; } = 0.6f;

        [field: Header("Heavy punch")]
        [field: SerializeField, Min(0f), Tooltip("Damage of the third punch in a chain.")]
        public float HeavyPunchDamage { get; private set; } = 70f;

        [field: SerializeField, Min(0f), Tooltip("Seconds from the start of the heavy punch until it lands.")]
        public float HeavyPunchHitDelay { get; private set; } = 0.25f;

        [field: SerializeField, Min(0f), Tooltip("Speed, in metres per second, that the heavy punch throws a target.")]
        public float HeavyPunchKnockback { get; private set; } = 8f;

        [field: Header("Kick")]
        [field: SerializeField, Min(0f), Tooltip("Damage of a kick.")]
        public float KickDamage { get; private set; } = 60f;

        [field: SerializeField, Min(0f), Tooltip("Radius of the kick's hit area.")]
        public float KickRadius { get; private set; } = 1.8f;

        [field: SerializeField, Min(0f), Tooltip("How far in front of the Colossus the kick lands.")]
        public float KickReach { get; private set; } = 2.8f;

        [field: SerializeField, Min(0f), Tooltip("Seconds from the start of the kick until it lands.")]
        public float KickHitDelay { get; private set; } = 0.3f;

        [field: SerializeField, Min(0f), Tooltip("Seconds before the Colossus can kick again.")]
        public float KickCooldownSeconds { get; private set; } = 0.8f;
    }
}
