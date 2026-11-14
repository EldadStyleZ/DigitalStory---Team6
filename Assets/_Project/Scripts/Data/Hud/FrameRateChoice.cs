namespace DigitalStory.Data
{
    /// <summary>The frame rate a player chooses in the settings panel (plan section AD), in the order the panel steps through.</summary>
    public enum FrameRateChoice
    {
        /// <summary>As fast as the display refreshes, in step with it.</summary>
        Display = 0,

        /// <summary>120 frames a second at most.</summary>
        Rate120 = 1,

        /// <summary>60 frames a second at most: half the work of 120, so the machine runs cooler.</summary>
        Rate60 = 2,
    }
}
