namespace DigitalStory.Logic
{
    /// <summary>The three volumes of the settings panel (plan section AC), in the panel's order.</summary>
    public enum VolumeGroup
    {
        /// <summary>Everything: it multiplies both other groups.</summary>
        Master = 0,

        /// <summary>The note voice, and any music track to come.</summary>
        Music = 1,

        /// <summary>Everything else: the recorded sounds, the swarm's hum and the synthesised hit and cast sounds.</summary>
        Effects = 2,
    }
}
