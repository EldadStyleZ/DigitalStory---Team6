namespace DigitalStory.Logic
{
    /// <summary>
    /// Decides which device the player is using, from what they touched this frame, so the HUD names the controls in their hands.
    /// </summary>
    public static class LastDeviceRule
    {
        /// <summary>
        /// The device in use after this frame: the one used, when only one was. When neither or both were, the frame says nothing
        /// about which the player picked up, so the current one stays and the HUD does not flicker between them.
        /// </summary>
        public static ControlDevice Next(ControlDevice current, bool isKeyboardUsed, bool isGamepadUsed)
        {
            if (isKeyboardUsed == isGamepadUsed)
            {
                return current;
            }

            return isGamepadUsed ? ControlDevice.Gamepad : ControlDevice.KeyboardMouse;
        }
    }
}
