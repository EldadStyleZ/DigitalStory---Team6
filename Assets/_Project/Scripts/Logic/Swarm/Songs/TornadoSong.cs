using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// A spinning column of wasps hurts and throws every beekeeper near the core once it stands at full height; a thrown
    /// beekeeper cannot act for a moment (plan section O).
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Tornado", fileName = "TornadoSong")]
    public sealed class TornadoSong : SlotSongEffect
    {
        // J K K H. 60 wasps.
        public TornadoSong()
            : base(new SongDefinition("Tornado", "A column of wasps throws beekeepers back", 10f, 1, 2, 2, 0), SongCategory.Attack, 0.37f, 60)
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Seconds the column spins.")]
        public float Seconds { get; private set; } = 1.5f;

        [field: SerializeField, Min(0f), Tooltip("Metres around the core that the column reaches.")]
        public float Radius { get; private set; } = 5f;

        [field: SerializeField, Min(0f), Tooltip("Metres the column stands tall.")]
        public float Height { get; private set; } = 3f;

        [field: SerializeField, Min(0f), Tooltip("Radians per second the column spins.")]
        public float SpinRadiansPerSecond { get; private set; } = 6f;

        [field: SerializeField, Min(0f), Tooltip("Seconds after the cast when the column stands at full height and hits.")]
        public float HitSeconds { get; private set; } = 0.3f;

        [field: SerializeField, Min(0f), Tooltip("Metres from the core to the column's wall.")]
        public float WallRadius { get; private set; } = 1.5f;

        [field: SerializeField, Min(0f), Tooltip("Damage to every beekeeper in reach.")]
        public float Damage { get; private set; } = 30f;

        // Plan section O: "the tornado feels like nothing happens", so it throws: 12 m/s (was 6) and 1 s that the beekeeper cannot act.
        [field: SerializeField, Min(0f), Tooltip("Metres per second a beekeeper is thrown away from the core.")]
        public float KnockbackSpeed { get; private set; } = 12f;

        [field: SerializeField, Min(0f), Tooltip("Seconds every thrown beekeeper cannot act.")]
        public float StunSeconds { get; private set; } = 1f;

        /// <summary>The Strike slot.</summary>
        public override SongSlot Slot => SongSlot.Strike;

        /// <summary>The tornado's picture.</summary>
        public override SwarmForm Form => SwarmForm.Tornado;

        /// <summary>One of the big hits (plan section O): the swarm's motion freezes for a moment on each.</summary>
        public override bool HitsHard => true;

        protected internal override SongRule CreateRule()
        {
            return new TornadoRule(this);
        }
    }
}
