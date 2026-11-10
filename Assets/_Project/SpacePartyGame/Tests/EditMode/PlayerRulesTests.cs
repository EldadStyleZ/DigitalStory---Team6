using NUnit.Framework;
using UnityEngine;

namespace SpacePartyGame.Tests
{
    public sealed class FacingRuleTests
    {
        [TestCase(1, 0.8f, 1)]
        [TestCase(1, -0.8f, -1)]
        [TestCase(-1, 0.8f, 1)]
        [TestCase(-1, -0.8f, -1)]
        public void Next_ClearSidewaysInput_FacesThatWay(int current, float moveX, int expected)
        {
            Assert.That(FacingRule.Next(current, moveX), Is.EqualTo(expected));
        }

        [TestCase(1, 0f)]
        [TestCase(-1, 0f)]
        [TestCase(-1, 0.05f)]
        [TestCase(1, -0.05f)]
        public void Next_NoClearSidewaysInput_KeepsTheLastFacing(int current, float moveX)
        {
            Assert.That(FacingRule.Next(current, moveX), Is.EqualTo(current));
        }

        [Test]
        public void Next_UnsetFacing_StartsFacingRight()
        {
            Assert.That(FacingRule.Next(0, 0f), Is.EqualTo(1));
        }
    }

    public sealed class PlayerMotionTests
    {
        [Test]
        public void VelocityFor_FullStickRight_IsSpeedRight()
        {
            Assert.That(PlayerMotion.VelocityFor(Vector2.right, 4f), Is.EqualTo(new Vector2(4f, 0f)));
        }

        [Test]
        public void VelocityFor_DiagonalKeys_IsNoFasterThanStraight()
        {
            Vector2 velocity = PlayerMotion.VelocityFor(new Vector2(1f, 1f), 4f);

            Assert.That(velocity.magnitude, Is.EqualTo(4f).Within(0.001f));
        }

        [Test]
        public void VelocityFor_HalfStick_IsHalfSpeed()
        {
            Vector2 velocity = PlayerMotion.VelocityFor(new Vector2(0f, 0.5f), 4f);

            Assert.That(velocity, Is.EqualTo(new Vector2(0f, 2f)));
        }

        [Test]
        public void VelocityFor_NoInput_IsStill()
        {
            Assert.That(PlayerMotion.VelocityFor(Vector2.zero, 4f), Is.EqualTo(Vector2.zero));
        }
    }
}
