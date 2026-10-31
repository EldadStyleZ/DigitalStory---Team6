using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class HouseColliderTests
    {
        private const float Radius = 5f;
        private const float Tolerance = 0.001f;

        // A house spanning x 100 to 180 and y 100 to 160.
        private static readonly HouseCollider Collider = new HouseCollider(new[]
        {
            new House(100f, 100f, 80f, 60f, 0, false, Vector2.zero),
        });

        [Test]
        public void PushOut_ClearOfHouse_DoesNotMove()
        {
            var position = new Vector2(50f, 50f);

            Collider.PushOut(ref position, Radius);

            Assert.That(position, Is.EqualTo(new Vector2(50f, 50f)));
        }

        [Test]
        public void PushOut_TouchingWall_MovesOutToRadius()
        {
            var position = new Vector2(97f, 130f);

            Collider.PushOut(ref position, Radius);

            Assert.That(position.x, Is.EqualTo(95f).Within(Tolerance));
            Assert.That(position.y, Is.EqualTo(130f).Within(Tolerance));
        }

        [TestCase(102f, 130f, 95f, 130f)]
        [TestCase(178f, 130f, 185f, 130f)]
        [TestCase(140f, 102f, 140f, 95f)]
        [TestCase(140f, 158f, 140f, 165f)]
        public void PushOut_CentreInsideHouse_LeavesByNearestSide(float x, float y, float expectedX, float expectedY)
        {
            var position = new Vector2(x, y);

            Collider.PushOut(ref position, Radius);

            Assert.That(position.x, Is.EqualTo(expectedX).Within(Tolerance));
            Assert.That(position.y, Is.EqualTo(expectedY).Within(Tolerance));
        }

        [Test]
        public void PushOut_MovingIntoWall_KeepsOnlyTheSlideAlongIt()
        {
            var position = new Vector2(97f, 130f);
            var velocity = new Vector2(40f, 20f);

            Collider.PushOut(ref position, ref velocity, Radius);

            Assert.That(velocity.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(velocity.y, Is.EqualTo(20f).Within(Tolerance));
        }

        [Test]
        public void PushOut_MovingAwayFromWall_KeepsVelocity()
        {
            var position = new Vector2(97f, 130f);
            var velocity = new Vector2(-40f, 20f);

            Collider.PushOut(ref position, ref velocity, Radius);

            Assert.That(velocity, Is.EqualTo(new Vector2(-40f, 20f)));
        }
    }
}
