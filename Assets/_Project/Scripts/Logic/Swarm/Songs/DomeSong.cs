using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>A Guard squad forms a dome around the Piper, so hits cost far fewer wasps.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Dome", fileName = "DomeSong")]
    public sealed class DomeSong : SlotSongEffect
    {
        // H K J: the Sting played backwards.
        public DomeSong()
            : base(new SongDefinition("Dome", "A dome of wasps shields the Piper", 8f, 0, 2, 1), SongCategory.Defense, 0.35f)
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Seconds the dome lasts.")]
        public float Seconds { get; private set; } = 5f;

        [field: SerializeField, Min(0f), Tooltip("Metres from the Piper to the dome.")]
        public float Radius { get; private set; } = 2.2f;

        /// <summary>The Guard slot.</summary>
        public override SongSlot Slot => SongSlot.Guard;

        /// <summary>The dome's picture.</summary>
        public override SwarmForm Form => SwarmForm.Dome;

        protected internal override SongRule CreateRule()
        {
            return new DomeRule(this);
        }
    }
}
