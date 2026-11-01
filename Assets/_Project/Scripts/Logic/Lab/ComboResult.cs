using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>What happened to a combo, and which creature it finished, if any.</summary>
    public readonly struct ComboResult
    {
        public ComboResult(ComboOutcome outcome, CreatureType creature = null)
        {
            Outcome = outcome;
            Creature = creature;
        }

        public ComboOutcome Outcome { get; }
        public CreatureType Creature { get; }
    }
}
