using System.Globalization;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// A slot song whose slot, category and share a test chooses, for the Post slot that no real song uses yet.
    /// It is a Defense taking 15% of the Post band unless a test says otherwise, and its rule runs until <see cref="ReleaseAfter"/>.
    /// </summary>
    internal sealed class FakeSlotSong : SlotSongEffect
    {
        public FakeSlotSong()
            : base(new SongDefinition("Fake slot song", string.Empty, 0f, 0), SongCategory.Defense, 0.15f, 0)
        {
        }

        public SongSlot SlotToUse { get; set; } = SongSlot.Post;

        public SwarmForm FormToShow { get; set; } = SwarmForm.River;

        /// <summary>Seconds after its cast at which the rule lets go. Infinity keeps it running.</summary>
        public float ReleaseAfter { get; set; } = float.PositiveInfinity;

        /// <summary>The share of each wasp loss its rule lets through while it runs.</summary>
        public float WaspLossFraction { get; set; } = 1f;

        /// <summary>How much its rule multiplies the core's speed while it runs.</summary>
        public float CoreSpeedMultiplier { get; set; } = 1f;

        /// <summary>False when its rule keeps smoke from slowing the core.</summary>
        public bool SmokeSlowsCore { get; set; } = true;

        /// <summary>The hive its rule shields, or -1 for none.</summary>
        public int ShieldedHive { get; set; } = -1;

        /// <summary>The share of damage its rule lets through to <see cref="ShieldedHive"/>.</summary>
        public float HiveDamageFraction { get; set; } = 1f;

        /// <summary>The rule the slot engine made, once the song has been cast.</summary>
        public FakeSongRule Rule { get; private set; }

        public override SongSlot Slot => SlotToUse;

        public override SwarmForm Form => FormToShow;

        public void SetShare(float share)
        {
            JsonUtility.FromJsonOverwrite("{\"_share\":" + share.ToString("R", CultureInfo.InvariantCulture) + "}", this);
        }

        public void SetCategory(SongCategory category)
        {
            JsonUtility.FromJsonOverwrite("{\"_category\":" + (int)category + "}", this);
        }

        protected override SongRule CreateRule()
        {
            Rule = new FakeSongRule(this);
            return Rule;
        }
    }
}
