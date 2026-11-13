namespace DigitalStory.Logic
{
    /// <summary>Which hands the player last used: the keyboard and mouse, or a controller. The HUD names the controls of that one.</summary>
    public enum ControlDevice
    {
        /// <summary>The keyboard and mouse. The default, until a controller is used.</summary>
        KeyboardMouse,

        /// <summary>A gamepad.</summary>
        Gamepad,
    }
}
