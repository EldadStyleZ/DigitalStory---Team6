namespace DigitalStory.Data
{
    /// <summary>The synthesised voices the swarm's notes can sound in (plan section AC). F6 cycles them in this order.</summary>
    public enum NoteVoiceKind
    {
        /// <summary>The breathy flute the game began with.</summary>
        Flute = 0,

        /// <summary>A kalimba-like pluck: a fast attack, a ringing decay and a little inharmonic shimmer.</summary>
        Pluck = 1,

        /// <summary>A soft bell: a gentle strike and a long, warm ring.</summary>
        Bell = 2,

        /// <summary>A buzzing reed with a slight vibrato, as if the wasps hummed the tune. It sustains while a key is held.</summary>
        Reed = 3,
    }
}
