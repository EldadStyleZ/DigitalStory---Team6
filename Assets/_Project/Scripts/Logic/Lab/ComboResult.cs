using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>What happened to a combo, and which one it finished, if any.</summary>
    public readonly struct ComboResult
    {
        public ComboResult(ComboOutcome outcome, IComboSong song = null)
        {
            Outcome = outcome;
            Song = song;
        }

        public ComboOutcome Outcome { get; }
        public IComboSong Song { get; }
    }
}
