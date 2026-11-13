using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// The Colossus, the human form of the swarm: its body and its moves (plan section I, version 2). Every damage here is
    /// multiplied by the swarm level's damage scale, and every radius, reach and range by its size scale, as they were when
    /// the giant stood up. How long it lasts, and the time its kills add, are on the Colossus song.
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Colossus Config", fileName = "ColossusConfig")]
    public sealed class ColossusConfig : ScriptableObject
    {
        [field: Header("Body")]
        [field: SerializeField, Min(0.1f), Tooltip("Height of the Colossus in metres.")]
        public float Height { get; private set; } = 4f;

        [field: SerializeField, Min(0f), Tooltip("Walking speed in metres per second.")]
        public float Speed { get; private set; } = 3.5f;

        [field: SerializeField, Min(0f), Tooltip("Seconds a beekeeper stays stunned after being hit by the Colossus.")]
        public float StunSeconds { get; private set; } = 0.5f;

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

        [field: Header("Phrases")]
        [field: SerializeField, Min(0f), Tooltip("Seconds after a kick or a slam to press it again for its phrase: K K a spin kick, L L a leap slam.")]
        public float PhraseWindowSeconds { get; private set; } = 0.6f;

        [field: Header("Ground slam")]
        [field: SerializeField, Min(0f), Tooltip("Damage of the ground slam to every beekeeper within its radius.")]
        public float SlamDamage { get; private set; } = 40f;

        [field: SerializeField, Min(0f), Tooltip("Radius, in metres around the Colossus, of the ground slam.")]
        public float SlamRadius { get; private set; } = 5f;

        [field: SerializeField, Min(0f), Tooltip("Speed, in metres per second, that the ground slam throws a beekeeper away from the Colossus.")]
        public float SlamKnockback { get; private set; } = 4f;

        [field: SerializeField, Min(0f), Tooltip("Seconds from the start of the ground slam until it lands.")]
        public float SlamHitDelay { get; private set; } = 0.3f;

        [field: SerializeField, Min(0f), Tooltip("Seconds before the Colossus can slam again. The leap slam does not wait for it.")]
        public float SlamCooldownSeconds { get; private set; } = 3f;

        [field: Header("Roar")]
        [field: SerializeField, Min(0f), Tooltip("Radius, in metres around the Colossus, of the roar. It does no damage.")]
        public float RoarRadius { get; private set; } = 6f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the roar stuns every beekeeper within its radius.")]
        public float RoarStunSeconds { get; private set; } = 1.2f;

        [field: SerializeField, Min(0f), Tooltip("Seconds from the start of the roar until it lands.")]
        public float RoarHitDelay { get; private set; } = 0.2f;

        [field: SerializeField, Min(0f), Tooltip("Seconds before the Colossus can roar again.")]
        public float RoarCooldownSeconds { get; private set; } = 6f;

        [field: Header("Spin kick (K K)")]
        [field: SerializeField, Min(0f), Tooltip("Damage of the spin kick to every beekeeper within its radius.")]
        public float SpinKickDamage { get; private set; } = 55f;

        [field: SerializeField, Min(0f), Tooltip("Radius, in metres around the Colossus, of the spin kick.")]
        public float SpinKickRadius { get; private set; } = 3.5f;

        [field: SerializeField, Min(0f), Tooltip("Seconds from the start of the spin kick until it lands.")]
        public float SpinKickHitDelay { get; private set; } = 0.3f;

        [field: Header("Leap slam (L L)")]
        [field: SerializeField, Min(0f), Tooltip("The most metres the Colossus leaps toward the aimed spot.")]
        public float LeapRange { get; private set; } = 10f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the Colossus is in the air, from take-off until it lands and hits.")]
        public float LeapSeconds { get; private set; } = 0.6f;

        [field: SerializeField, Min(0f), Tooltip("Damage of the landing to every beekeeper within its radius.")]
        public float LeapDamage { get; private set; } = 70f;

        [field: SerializeField, Min(0f), Tooltip("Radius, in metres around the landing, of the leap slam.")]
        public float LeapRadius { get; private set; } = 4f;

        [field: SerializeField, Min(0f), Tooltip("Speed, in metres per second, that the landing throws a beekeeper away from it.")]
        public float LeapKnockback { get; private set; } = 6f;

        [field: Header("Hand launch (right: R2, right mouse or Space; left: L2, left mouse or Left Shift)")]
        [field: SerializeField, Min(0.01f), Tooltip("Seconds the arm thrusts forward before the wasps of its hand and forearm leave as a fist.")]
        public float FistThrustSeconds { get; private set; } = 0.25f;

        [field: SerializeField, Min(0f), Tooltip("Damage of a fist to every beekeeper within its radius of where it hits. Grows with the level, as every blow of the giant.")]
        public float FistDamage { get; private set; } = 20f;

        [field: SerializeField, Min(0f), Tooltip("Radius, in metres around where the fist hits, of its blow. Grows with the level.")]
        public float FistRadius { get; private set; } = 1.2f;

        [field: SerializeField, Min(0f), Tooltip("Speed, in metres per second, that a fist throws a beekeeper away from where it hits. None at first, so the hand launch adds damage only, and the giant's balance holds until a later tuning.")]
        public float FistKnockback { get; private set; }

        [field: SerializeField, Min(0f), Tooltip("Seconds a beekeeper the fist hits, and does not kill, stays stunned. None at first, as for the throw.")]
        public float FistStunSeconds { get; private set; }

        [field: SerializeField, Min(0.1f), Tooltip("Metres per second a fist flies.")]
        public float FistSpeed { get; private set; } = 24f;

        [field: SerializeField, Min(0.01f), Tooltip("The fewest seconds a fist flies, so even a target at the giant's feet sees the fist leave the arm.")]
        public float FistLeastFlightSeconds { get; private set; } = 0.2f;

        [field: SerializeField, Min(0f), Tooltip("The most metres from the giant a fist flies: auto-aim picks a beekeeper within it, and an aimed point beyond it is pulled in to it. Grows with the level.")]
        public float FistRange { get; private set; } = 14f;

        [field: SerializeField, Min(0.1f), Tooltip("Seconds from a fist's hit until its arm has grown back and can launch again. The arm is missing until then.")]
        public float FistRegrowSeconds { get; private set; } = 2f;
    }
}
