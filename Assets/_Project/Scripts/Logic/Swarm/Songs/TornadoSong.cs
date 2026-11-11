using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>A spinning column of wasps hurts and throws back every beekeeper near the Piper once it stands at full height.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Tornado", fileName = "TornadoSong")]
    public sealed class TornadoSong : SlotSongEffect
    {
        // J K K H.
        public TornadoSong()
            : base(new SongDefinition("Tornado", "A column of wasps throws beekeepers back", 10f, 1, 2, 2, 0), SongCategory.Attack, 0.37f)
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Seconds the column spins.")]
        public float Seconds { get; private set; } = 1.5f;

        [field: SerializeField, Min(0f), Tooltip("Metres around the Piper that the column reaches.")]
        public float Radius { get; private set; } = 5f;

        [field: SerializeField, Min(0f), Tooltip("Metres the column stands tall.")]
        public float Height { get; private set; } = 3f;

        [field: SerializeField, Min(0f), Tooltip("Radians per second the column spins.")]
        public float SpinRadiansPerSecond { get; private set; } = 6f;

        [field: SerializeField, Min(0f), Tooltip("Seconds after the cast when the column stands at full height and hits.")]
        public float HitSeconds { get; private set; } = 0.3f;

        [field: SerializeField, Min(0f), Tooltip("Metres from the Piper to the column's wall.")]
        public float WallRadius { get; private set; } = 1.5f;

        [field: SerializeField, Min(0f), Tooltip("Damage to every beekeeper in reach.")]
        public float Damage { get; private set; } = 30f;

        [field: SerializeField, Min(0f), Tooltip("Metres per second a beekeeper is thrown away from the Piper.")]
        public float KnockbackSpeed { get; private set; } = 6f;

        /// <summary>The Strike slot.</summary>
        public override SongSlot Slot => SongSlot.Strike;

        /// <summary>The tornado's picture.</summary>
        public override SwarmForm Form => SwarmForm.Tornado;

        protected internal override SongRule CreateRule()
        {
            return new TornadoRule(this);
        }
    }
}
