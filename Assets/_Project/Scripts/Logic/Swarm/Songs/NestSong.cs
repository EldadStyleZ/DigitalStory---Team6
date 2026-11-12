using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>A Post squad is lobbed onto a lane and waits there as a trap that a beekeeper walks into.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Nest", fileName = "NestSong")]
    public sealed class NestSong : SlotSongEffect
    {
        // K L J H: the Honeycomb played backwards. 200 wasps, which stay on the lane while it stands. The playtest bot set it
        // (was 100) after plan section O made the stings cheaper, so the towers still take a real share of a win's wasps.
        public NestSong()
            : base(new SongDefinition("Nest", "A trap of wasps waits on the lane", 8f, 2, 3, 1, 0), SongCategory.Attack, 0.15f, 200)
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Furthest metres from the core that the nest can be lobbed.")]
        public float MaxRange { get; private set; } = 10f;

        [field: SerializeField, Min(0f), Tooltip("Seconds after the cast until the nest is armed. A beekeeper already near it sets it off as it arms.")]
        public float ArmSeconds { get; private set; } = 0.6f;

        [field: SerializeField, Min(0f), Tooltip("Metres from the nest within which the first living beekeeper sets it off.")]
        public float TriggerRadius { get; private set; } = 2.5f;

        [field: SerializeField, Min(0f), Tooltip("Seconds between a beekeeper setting the nest off and the burst.")]
        public float FuseSeconds { get; private set; } = 0.3f;

        [field: SerializeField, Min(0f), Tooltip("Metres around the nest that the burst reaches.")]
        public float BlastRadius { get; private set; } = 3.5f;

        [field: SerializeField, Min(0f), Tooltip("Damage to every beekeeper in reach of the burst.")]
        public float Damage { get; private set; } = 70f;

        [field: SerializeField, Min(0f), Tooltip("Seconds every beekeeper in reach of the burst is stunned.")]
        public float StunSeconds { get; private set; } = 1f;

        [field: SerializeField, Min(0f), Tooltip("Seconds after the cast at which an unset nest ends, hurting nobody.")]
        public float MaxSeconds { get; private set; } = 60f;

        /// <summary>The Post slot: a new tower replaces the old one.</summary>
        public override SongSlot Slot => SongSlot.Post;

        /// <summary>Auto-aim chooses where it is lobbed.</summary>
        public override bool IsAimed => true;

        /// <summary>The beekeeper with the most others close by (plan section E, Crowd).</summary>
        public override AimRule AimRule => AimRule.Crowd;

        /// <summary>As far as it is lobbed: <see cref="MaxRange"/>.</summary>
        public override float AimRange => MaxRange;

        /// <summary>The nest's picture.</summary>
        public override SwarmForm Form => SwarmForm.Nest;

        /// <summary>One of the big hits (plan section O): the swarm's motion freezes for a moment on each.</summary>
        public override bool HitsHard => true;

        protected internal override SongRule CreateRule()
        {
            return new NestRule(this);
        }
    }
}
