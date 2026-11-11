using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>One kind of beekeeper: how tough it is, how it moves and how it attacks. The defaults are the stick.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Beekeeper Config", fileName = "BeekeeperConfig")]
    public sealed class BeekeeperConfig : ScriptableObject
    {
        [field: Header("Kind")]
        [field: SerializeField, Tooltip("Stick hits in melee. Smoker throws smoke grenades.")]
        public BeekeeperKind Kind { get; private set; } = BeekeeperKind.Stick;

        [field: Header("Body")]
        [field: SerializeField, Min(1f), Tooltip("Health of the beekeeper.")]
        public float Health { get; private set; } = 60f;

        [field: SerializeField, Min(0f), Tooltip("Walking speed in metres per second.")]
        public float Speed { get; private set; } = 2.6f;

        [field: SerializeField, Min(0f), Tooltip("Distance at which beekeepers push each other apart.")]
        public float SeparationRadius { get; private set; } = 1.2f;

        [field: Header("Senses")]
        [field: SerializeField, Min(0f), Tooltip("Distance at which the beekeeper notices the core and chases.")]
        public float NoticeRange { get; private set; } = 7f;

        [field: SerializeField, Min(0f), Tooltip("Distance at which the beekeeper stops chasing and returns to the hive.")]
        public float GiveUpRange { get; private set; } = 14f;

        [field: Header("Attack")]
        [field: SerializeField, Min(0f), Tooltip("Distance from which the beekeeper attacks. For a smoker, the throw range.")]
        public float AttackRange { get; private set; } = 1.8f;

        [field: SerializeField, Min(0f), Tooltip("Seconds of warning before the attack lands.")]
        public float WindUpSeconds { get; private set; } = 0.6f;

        [field: SerializeField, Min(0f), Tooltip("Seconds of recovery after an attack.")]
        public float CooldownSeconds { get; private set; } = 1.4f;

        [field: SerializeField, Min(0f), Tooltip("Damage a stick hit does to a hive. A smoker does none.")]
        public float HiveDamage { get; private set; } = 6f;

        [field: SerializeField, Min(0), Tooltip("Wasps a stick knocks off the swarm when it hits the core. A smoker does none.")]
        public int WaspsKnockedOff { get; private set; } = 72;
    }
}
