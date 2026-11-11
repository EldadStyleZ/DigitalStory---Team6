using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>A Strike squad forms a blade off the core and sweeps it through the half-disc facing the aim, cutting everyone there.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Scythe", fileName = "ScytheSong")]
    public sealed class ScytheSong : SlotSongEffect
    {
        // K L H: the Gust played backwards. 30 wasps.
        public ScytheSong()
            : base(new SongDefinition("Scythe", "A blade of wasps cuts the beekeepers in front", 4f, 2, 3, 0), SongCategory.Attack, 0.37f, 30)
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Seconds the Scythe lasts. The blade crumbles and flies home after it, in the slot's return beat.")]
        public float Seconds { get; private set; } = 1f;

        [field: SerializeField, Min(0f), Tooltip("Metres around the core that the blade reaches.")]
        public float Radius { get; private set; } = 4.5f;

        [field: SerializeField, Min(0f), Tooltip("Seconds after the cast at which the blade cuts, as its edge crosses the aim.")]
        public float HitSeconds { get; private set; } = 0.3f;

        [field: SerializeField, Min(0f), Tooltip("Damage to every beekeeper in the half-disc facing the aim.")]
        public float Damage { get; private set; } = 45f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the blade takes to form off the core before it sweeps. The picture's number only.")]
        public float WindUpSeconds { get; private set; } = 0.25f;

        [field: SerializeField, Range(0f, 360f), Tooltip("Degrees the blade sweeps, centred on the aim. The picture's number only: the cut is the half-disc facing the aim.")]
        public float SweepDegrees { get; private set; } = 200f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the sweep takes on an ease-out cubic after the wind-up. The picture winds the blade 30 degrees back before it sweeps, so its edge reaches the aim, the sweep's middle, 24% of the way through: at 0.2 s that is 0.298 s after the cast, on the frame the blade cuts. The picture's number only.")]
        public float SweepSeconds { get; private set; } = 0.2f;

        [field: SerializeField, Min(0f), Tooltip("Furthest metres from the core at which auto-aim picks the beekeeper the blade faces.")]
        public float FacingRange { get; private set; } = 6f;

        /// <summary>The Strike slot.</summary>
        public override SongSlot Slot => SongSlot.Strike;

        /// <summary>Auto-aim chooses which way the blade faces.</summary>
        public override bool IsAimed => true;

        /// <summary>The most threatening beekeeper within reach (plan section E, Threat): the blade faces it.</summary>
        public override AimRule AimRule => AimRule.Threat;

        /// <summary>As far as it looks for a beekeeper to face: <see cref="FacingRange"/>.</summary>
        public override float AimRange => FacingRange;

        /// <summary>The Scythe's picture.</summary>
        public override SwarmForm Form => SwarmForm.Scythe;

        protected internal override SongRule CreateRule()
        {
            return new ScytheRule(this);
        }
    }
}
