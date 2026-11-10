using System;
using NUnit.Framework;
using UnityEngine;

namespace SpacePartyGame.Tests
{
    public sealed class PolygonAreaTests
    {
        // A 4 by 4 square with its bottom-right quarter cut out, so it is concave.
        private static readonly Vector2[] LShape =
        {
            new Vector2(0f, 0f),
            new Vector2(2f, 0f),
            new Vector2(2f, 2f),
            new Vector2(4f, 2f),
            new Vector2(4f, 4f),
            new Vector2(0f, 4f),
        };

        [TestCase(1f, 1f)]
        [TestCase(1f, 3f)]
        [TestCase(3f, 3f)]
        public void Contains_PointOnTheFloor_IsTrue(float x, float y)
        {
            var area = new PolygonArea(LShape);

            Assert.That(area.Contains(new Vector2(x, y)), Is.True);
        }

        [TestCase(3f, 1f)]
        [TestCase(-1f, 2f)]
        [TestCase(2f, 5f)]
        [TestCase(5f, 3f)]
        public void Contains_PointOffTheFloor_IsFalse(float x, float y)
        {
            var area = new PolygonArea(LShape);

            Assert.That(area.Contains(new Vector2(x, y)), Is.False);
        }

        [Test]
        public void Contains_ClosedLoopWithTheFirstPointRepeated_GivesTheSameAnswer()
        {
            var closed = new Vector2[LShape.Length + 1];
            LShape.CopyTo(closed, 0);
            closed[LShape.Length] = LShape[0];
            var area = new PolygonArea(closed);

            Assert.That(area.Contains(new Vector2(1f, 1f)), Is.True);
            Assert.That(area.Contains(new Vector2(3f, 1f)), Is.False);
        }

        [Test]
        public void Bounds_LShape_IsTheBoxRoundEveryPoint()
        {
            var area = new PolygonArea(LShape);

            Assert.That(area.Bounds, Is.EqualTo(new Rect(0f, 0f, 4f, 4f)));
        }

        [Test]
        public void Constructor_FewerThanThreePoints_Throws()
        {
            Assert.Throws<ArgumentException>(() => new PolygonArea(new[] { Vector2.zero, Vector2.one }));
        }
    }
}
