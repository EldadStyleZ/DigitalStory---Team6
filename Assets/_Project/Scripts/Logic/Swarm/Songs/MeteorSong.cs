using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>A Strike squad climbs into the sky and falls on the target like meteors.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Meteor", fileName = "MeteorSong")]
    public sealed class MeteorSong : SlotSongEffect
    {
        // J K L K.
        public MeteorSong()
            : base(new SongDefinition("Meteor", "Wasps fall from the sky on the target", 9f, 1, 2, 3, 2), SongCategory.Attack, 0.35f)
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Metres above the Piper the meteors climb to.")]
        public float Height { get; private set; } = 12f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the meteors climb. The impact point follows the aimed beekeeper until the climb ends.")]
        public float RiseSeconds { get; private set; } = 0.8f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the meteors dive. They land when the dive ends; the burst after it is the slot's return beat.")]
        public float DiveSeconds { get; private set; } = 0.5f;

        [field: SerializeField, Min(0f), Tooltip("Furthest metres from the Piper that the impact can be.")]
        public float MaxRange { get; private set; } = 18f;

        [field: SerializeField, Min(0f), Tooltip("Metres around the impact point that the landing reaches.")]
        public float Radius { get; private set; } = 3f;

        [field: SerializeField, Min(0f), Tooltip("Damage to every beekeeper in reach.")]
        public float Damage { get; private set; } = 60f;

        [field: SerializeField, Min(0f), Tooltip("Seconds every beekeeper in reach is stunned.")]
        public float StunSeconds { get; private set; } = 0.5f;

        /// <summary>The Strike slot.</summary>
        public override SongSlot Slot => SongSlot.Strike;

        /// <summary>Auto-aim chooses what it strikes.</summary>
        public override bool IsAimed => true;

        /// <summary>The beekeeper with the most others close by, within reach (plan section E, Crowd).</summary>
        public override AimRule AimRule => AimRule.Crowd;

        /// <summary>As far as it flies: <see cref="MaxRange"/>.</summary>
        public override float AimRange => MaxRange;

        /// <summary>The meteor's picture.</summary>
        public override SwarmForm Form => SwarmForm.Meteor;

        protected internal override SongRule CreateRule()
        {
            return new MeteorRule(this);
        }
    }
}
