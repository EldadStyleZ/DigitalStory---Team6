using System.Collections.Generic;

namespace DigitalStory.Data
{
    /// <summary>Anything the Piper can play on the flute: a creature to summon or a song that helps them.</summary>
    public interface IComboSong
    {
        string Name { get; }

        /// <summary>What it is for, in a few words, for the screen.</summary>
        string Role { get; }

        int UnlockLevel { get; }

        /// <summary>The notes that play it: 0 is H, 1 is J, 2 is K and 3 is L.</summary>
        IReadOnlyList<int> Combo { get; }

        float StaminaCost { get; }
        float CooldownSeconds { get; }

        /// <summary>How its tune is played once the combo is finished.</summary>
        SongStyle Style { get; }
    }
}
