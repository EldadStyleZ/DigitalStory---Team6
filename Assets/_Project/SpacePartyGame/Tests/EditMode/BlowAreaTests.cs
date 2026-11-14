using NUnit.Framework;
using UnityEngine;

namespace SpacePartyGame.Tests
{
    public sealed class BlowAreaTests
    {
        private static readonly Vector2 Origin = Vector2.zero;
        private static readonly Vector2 Aim = Vector2.right;

        [Test]
        public void Overlaps_PointInside_IsHit()
        {
            Assert.That(BlowCone.Overlaps(Origin, Aim, new Vector2(2f, 0f), 0f, 90f, 0.5f, 4f), Is.True);
        }

        [Test]
        public void Overlaps_JustPastTheReach_HitOnlyWithLeeway()
        {
            var point = new Vector2(4.3f, 0f);

            Assert.That(BlowCone.Overlaps(Origin, Aim, point, 0f, 90f, 0.5f, 4f), Is.False);
            Assert.That(BlowCone.Overlaps(Origin, Aim, point, 0.4f, 90f, 0.5f, 4f), Is.True);
        }

        [Test]
        public void Overlaps_JustOutsideTheConeSide_HitOnlyWithLeeway()
        {
            // 0.3 units beyond the cone's upper edge (the edge runs at 45°).
            Vector2 onEdge = new Vector2(1f, 1f).normalized * 2f;
            Vector2 point = onEdge + new Vector2(-1f, 1f).normalized * 0.3f;

            Assert.That(BlowCone.Overlaps(Origin, Aim, point, 0f, 90f, 0.5f, 4f), Is.False);
            Assert.That(BlowCone.Overlaps(Origin, Aim, point, 0.35f, 90f, 0.5f, 4f), Is.True);
            Assert.That(BlowCone.Overlaps(Origin, Aim, point, 0.25f, 90f, 0.5f, 4f), Is.False);
        }

        [Test]
        public void Overlaps_InsideTheInnerGap_HitOnlyWithLeeway()
        {
            var point = new Vector2(0.2f, 0f);

            Assert.That(BlowCone.Overlaps(Origin, Aim, point, 0f, 90f, 0.5f, 4f), Is.False);
            Assert.That(BlowCone.Overlaps(Origin, Aim, point, 0.35f, 90f, 0.5f, 4f), Is.True);
        }

        [Test]
        public void Overlaps_BehindThePlayer_IsMissed()
        {
            Assert.That(BlowCone.Overlaps(Origin, Aim, new Vector2(-2f, 0f), 0.5f, 90f, 0.5f, 4f), Is.False);
        }

        [Test]
        public void Overlaps_FollowsTheAim()
        {
            Assert.That(BlowCone.Overlaps(Origin, Vector2.up, new Vector2(0f, 3f), 0f, 60f, 0.5f, 4f), Is.True);
            Assert.That(BlowCone.Overlaps(Origin, Vector2.up, new Vector2(3f, 0f), 0f, 60f, 0.5f, 4f), Is.False);
        }
    }
}
