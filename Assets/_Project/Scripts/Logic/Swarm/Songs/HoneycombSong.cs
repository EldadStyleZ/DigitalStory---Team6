using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>A Post squad winds around one hive as a shell: the hive takes far less damage, and sticks that hit it are stung.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Honeycomb", fileName = "HoneycombSong")]
    public sealed class HoneycombSong : SlotSongEffect
    {
        // H J L K: the Nest played backwards. 150 wasps, which stay on the hive while it stands.
        public HoneycombSong()
            : base(new SongDefinition("Honeycomb", "A shell of wasps guards one hive", 15f, 0, 1, 3, 2), SongCategory.Defense, 0.2f, 150)
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Seconds a shell cast in a farm phase lasts, if no wave starts. A wave that starts while it stands makes it last until that wave ends.")]
        public float FarmSeconds { get; private set; } = 60f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Share of the damage from sticks and smoke that still reaches the hive inside the shell.")]
        public float DamageFraction { get; private set; } = 0.25f;

        [field: SerializeField, Min(0f), Tooltip("Damage a stick takes back for each hit on the shielded hive.")]
        public float ThornDamage { get; private set; } = 10f;

        [field: SerializeField, Min(0f), Tooltip("Metres from the hive's middle to the shell's wall.")]
        public float ShellRadius { get; private set; } = 1.6f;

        [field: SerializeField, Min(0f), Tooltip("Metres the shell's cells burst out when it ends. The picture's number only.")]
        public float BurstRadius { get; private set; } = 2.2f;

        /// <summary>The Post slot: a new tower replaces the old one.</summary>
        public override SongSlot Slot => SongSlot.Post;

        /// <summary>Auto-aim chooses the hive.</summary>
        public override bool IsAimed => true;

        /// <summary>The hive hit most recently in the last 3 seconds, else the nearest standing hive (plan section E, Hive).</summary>
        public override AimRule AimRule => AimRule.Hive;

        /// <summary>The Honeycomb's picture.</summary>
        public override SwarmForm Form => SwarmForm.Honeycomb;

        protected internal override SongRule CreateRule()
        {
            return new HoneycombRule(this);
        }
    }
}
