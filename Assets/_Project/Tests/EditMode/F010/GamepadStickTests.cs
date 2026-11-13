using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class GamepadStickTests
    {
        private const float Tolerance = 0.001f;
        private const float DeadZone = 0.2f;

        [TestCase(0f, 0f)]
        [TestCase(0.1f, 0.1f)]
        [TestCase(0.2f, 0f)]
        [TestCase(0f, -0.19f)]
        public void DeadZone_InsideTheZone_IsZero(float x, float y)
        {
            Vector2 stick = GamepadStick.DeadZone(new Vector2(x, y), DeadZone);

            Assert.That(stick.sqrMagnitude, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void DeadZone_LeanedAllTheWay_IsFull()
        {
            Vector2 stick = GamepadStick.DeadZone(new Vector2(0f, 1f), DeadZone);

            Assert.That(stick.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(stick.y, Is.EqualTo(1f).Within(Tolerance));
        }

        // Past the zone the reading grows again from 0, so a small lean is a small move rather than a jump to 20 %.
        [Test]
        public void DeadZone_HalfwayPastTheZone_IsHalf()
        {
            Vector2 stick = GamepadStick.DeadZone(new Vector2(0.6f, 0f), DeadZone);

            Assert.That(stick.x, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(stick.y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void DeadZone_KeepsTheStickWay()
        {
            Vector2 raw = new Vector2(-0.6f, 0.6f);

            Vector2 stick = GamepadStick.DeadZone(raw, DeadZone);

            Assert.That(Vector2.Dot(stick.normalized, raw.normalized), Is.EqualTo(1f).Within(Tolerance));
        }

        // A square-gated pad reads past 1 in the corners; the reading never does.
        [Test]
        public void DeadZone_PastOne_IsCappedAtOne()
        {
            Vector2 stick = GamepadStick.DeadZone(new Vector2(1f, 1f), DeadZone);

            Assert.That(stick.magnitude, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void DeadZone_ZoneOfZero_PassesTheStickThrough()
        {
            Vector2 stick = GamepadStick.DeadZone(new Vector2(0.3f, -0.4f), 0f);

            Assert.That(stick.x, Is.EqualTo(0.3f).Within(Tolerance));
            Assert.That(stick.y, Is.EqualTo(-0.4f).Within(Tolerance));
        }

        [Test]
        public void AimPoint_StickUp_AimsTheReachAheadOfTheCoreTheCamerasWay()
        {
            var core = new Vector3(2f, 0f, 3f);

            Vector3 aim = GamepadStick.AimPoint(core, new Vector2(0f, 1f), Vector3.forward, 8f);

            Assert.That(aim.x, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(aim.z, Is.EqualTo(11f).Within(Tolerance));
        }

        [Test]
        public void AimPoint_StickRight_AimsToTheCamerasRight()
        {
            Vector3 aim = GamepadStick.AimPoint(Vector3.zero, new Vector2(1f, 0f), Vector3.forward, 5f);

            Assert.That(aim.x, Is.EqualTo(5f).Within(Tolerance));
            Assert.That(aim.z, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void AimPoint_HalfLean_AimsHalfTheReach()
        {
            Vector3 aim = GamepadStick.AimPoint(Vector3.zero, new Vector2(0f, 0.5f), Vector3.forward, 8f);

            Assert.That(aim.z, Is.EqualTo(4f).Within(Tolerance));
        }

        // The camera's tilt never lifts the aim off the ground the core stands on.
        [Test]
        public void AimPoint_TiltedCamera_StaysAtTheCoresHeight()
        {
            var core = new Vector3(0f, 0.5f, 0f);
            Vector3 tilted = new Vector3(0f, -0.8f, 0.6f);

            Vector3 aim = GamepadStick.AimPoint(core, new Vector2(0f, 1f), tilted, 8f);

            Assert.That(aim.y, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(aim.z, Is.EqualTo(8f).Within(Tolerance));
        }

        [Test]
        public void AimPoint_TurnedCamera_TurnsTheAim()
        {
            Vector3 aim = GamepadStick.AimPoint(Vector3.zero, new Vector2(0f, 1f), Vector3.right, 8f);

            Assert.That(aim.x, Is.EqualTo(8f).Within(Tolerance));
            Assert.That(aim.z, Is.EqualTo(0f).Within(Tolerance));
        }
    }
}
