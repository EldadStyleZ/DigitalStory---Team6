namespace DigitalStory.Data
{
    /// <summary>
    /// How sharp the 3D view is drawn, as a player chooses in the settings panel (plan section AD), in the order the panel
    /// steps through. The HUD is drawn at the display's own resolution whatever the choice.
    /// </summary>
    public enum RenderScaleChoice
    {
        /// <summary>At most <see cref="DisplayConfig.AutoMostLines"/> lines high, scaled up to the display; native below that.</summary>
        Auto = 0,

        /// <summary>Every pixel of the display.</summary>
        Native = 1,

        /// <summary>Three quarters of the display's width and height.</summary>
        ThreeQuarters = 2,

        /// <summary>Half the display's width and height.</summary>
        Half = 3,
    }
}
