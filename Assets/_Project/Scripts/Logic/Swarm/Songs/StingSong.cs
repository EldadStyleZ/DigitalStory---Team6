using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>A Strike squad arcs to the aimed beekeeper and stings it, or wraps the air ahead when there is nothing in reach.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Sting", fileName = "StingSong")]
    public sealed class StingSong : SlotSongEffect
    {
        // J K H.
        public StingSong()
            : base(new SongDefinition("Sting", "Wasps wrap around the aimed beekeeper", 1.5f, 1, 2, 0), SongCategory.Attack, 0.3f, knownFromStart: true)
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Seconds the sting lasts, or until the target dies.")]
        public float Seconds { get; private set; } = 4f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the wasps fly before they reach the target. The damage starts when they arrive.")]
        public float ArriveSeconds { get; private set; } = 0.6f;

        [field: SerializeField, Min(0f), Tooltip("Damage the target takes each second once the wasps arrive.")]
        public float DamagePerSecond { get; private set; } = 25f;

        [field: SerializeField, Min(0f), Tooltip("Furthest metres from the core the wasps fly. A target further away is out of reach, and the wasps wrap the air this far toward it.")]
        public float MaxRange { get; private set; } = 12f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the wasps wrap the empty air when there is no living target in reach.")]
        public float AirSeconds { get; private set; } = 1f;

        /// <summary>The Strike slot.</summary>
        public override SongSlot Slot => SongSlot.Strike;

        /// <summary>Auto-aim chooses what it strikes.</summary>
        public override bool IsAimed => true;

        /// <summary>The most threatening beekeeper within reach (plan section E, Threat).</summary>
        public override AimRule AimRule => AimRule.Threat;

        /// <summary>As far as it flies: <see cref="MaxRange"/>.</summary>
        public override float AimRange => MaxRange;

        /// <summary>The sting's picture.</summary>
        public override SwarmForm Form => SwarmForm.Sting;

        protected internal override SongRule CreateRule()
        {
            return new StingRule(this);
        }
    }
}
