using NUnit.Framework;
using UnityEngine;

namespace PiperToy.Tests
{
    // The disco's colour rule is plain C#, so these tests need no scene.
    public sealed class DiscoPaletteTests
    {
        private static readonly Color[] Colors = { Color.red, Color.green, Color.blue };

        private static void AssertNear(Color actual, Color expected)
        {
            Assert.That(Vector4.Distance(actual, expected), Is.LessThan(0.01f), $"{actual} should be {expected}.");
        }

        [Test]
        public void ColorAt_TheStartOfABeat_IsThatBeatsColour()
        {
            AssertNear(DiscoPalette.ColorAt(Colors, 0f), Color.red);
            AssertNear(DiscoPalette.ColorAt(Colors, 1f), Color.green);
            AssertNear(DiscoPalette.ColorAt(Colors, 2f), Color.blue);
        }

        // Each colour holds for most of its beat, so the lights read as distinct colours, not a smear.
        [Test]
        public void ColorAt_TheMiddleOfABeat_StillHoldsThatBeatsColour()
        {
            AssertNear(DiscoPalette.ColorAt(Colors, 1.5f), Color.green);
        }

        // At the end of a beat it fades into the next colour instead of jumping.
        [Test]
        public void ColorAt_TheEndOfABeat_IsOnItsWayToTheNextColour()
        {
            Color fading = DiscoPalette.ColorAt(Colors, 0.9f);

            Assert.That(fading.r, Is.GreaterThan(0.05f).And.LessThan(0.95f));
            Assert.That(fading.g, Is.GreaterThan(0.05f).And.LessThan(0.95f));
        }

        [Test]
        public void ColorAt_AfterTheLastColour_StartsAgainFromTheFirst()
        {
            AssertNear(DiscoPalette.ColorAt(Colors, 3f), Color.red);
            AssertNear(DiscoPalette.ColorAt(Colors, 7f), Color.green);
        }
    }
}
