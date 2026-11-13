using DigitalStory.Logic;
using NUnit.Framework;

namespace DigitalStory.Tests.EditMode
{
    public sealed class LastDeviceRuleTests
    {
        [TestCase(ControlDevice.KeyboardMouse)]
        [TestCase(ControlDevice.Gamepad)]
        public void Next_OnlyTheGamepadUsed_IsTheGamepad(ControlDevice current)
        {
            Assert.That(LastDeviceRule.Next(current, false, true), Is.EqualTo(ControlDevice.Gamepad));
        }

        [TestCase(ControlDevice.KeyboardMouse)]
        [TestCase(ControlDevice.Gamepad)]
        public void Next_OnlyTheKeyboardUsed_IsTheKeyboard(ControlDevice current)
        {
            Assert.That(LastDeviceRule.Next(current, true, false), Is.EqualTo(ControlDevice.KeyboardMouse));
        }

        [TestCase(ControlDevice.KeyboardMouse)]
        [TestCase(ControlDevice.Gamepad)]
        public void Next_NothingUsed_KeepsTheCurrent(ControlDevice current)
        {
            Assert.That(LastDeviceRule.Next(current, false, false), Is.EqualTo(current));
        }

        // Both in one frame says nothing about which the player picked up, so the HUD does not flicker.
        [TestCase(ControlDevice.KeyboardMouse)]
        [TestCase(ControlDevice.Gamepad)]
        public void Next_BothUsed_KeepsTheCurrent(ControlDevice current)
        {
            Assert.That(LastDeviceRule.Next(current, true, true), Is.EqualTo(current));
        }
    }
}
