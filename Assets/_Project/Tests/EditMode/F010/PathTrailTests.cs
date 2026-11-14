using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class PathTrailTests
    {
        private const float Tolerance = 0.001f;

        private static PathTrail Walked(int capacity, float spacing, float toX)
        {
            var trail = new PathTrail(capacity, spacing);
            trail.Reset(Vector3.zero);
            trail.Push(new Vector3(toX, 0f, 0f));
            return trail;
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-3)]
        public void Constructor_CapacityBelowTwo_Throws(int capacity)
        {
            Assert.That(() => new PathTrail(capacity, 1f), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void Constructor_SpacingNotPositive_Throws(float spacing)
        {
            Assert.That(() => new PathTrail(8, spacing), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void New_HasNoPoints()
        {
            var trail = new PathTrail(8, 1f);

            Assert.That(trail.Count, Is.EqualTo(0));
            Assert.That(trail.Length, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Reset_StoresOnePointAtThePosition()
        {
            var trail = new PathTrail(8, 1f);

            trail.Reset(new Vector3(3f, 0f, 4f));

            Assert.That(trail.Count, Is.EqualTo(1));
            Assert.That(trail.Length, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(Vector3.Distance(trail.PointAt(0.7f), new Vector3(3f, 0f, 4f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void Reset_AfterWalking_ForgetsTheOldPath()
        {
            var trail = Walked(8, 1f, 5.5f);

            trail.Reset(new Vector3(0f, 0f, 9f));

            Assert.That(trail.Count, Is.EqualTo(1));
            Assert.That(trail.Length, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Push_ShorterThanSpacing_StoresNothing()
        {
            var trail = Walked(8, 1f, 0.9f);

            Assert.That(trail.Count, Is.EqualTo(1));
        }

        [Test]
        public void Push_OneSpacingOfTravel_StoresOnePoint()
        {
            var trail = Walked(8, 1f, 1.2f);

            Assert.That(trail.Count, Is.EqualTo(2));
            Assert.That(trail.Length, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(trail.PointAt(0f).x, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Push_LongStep_StoresSeveralPointsOnTheStraightLine()
        {
            var trail = Walked(16, 1f, 3.5f);

            Assert.That(trail.Count, Is.EqualTo(4));
            Assert.That(trail.Length, Is.EqualTo(3f).Within(Tolerance));
            Assert.That(trail.PointAt(0f).x, Is.EqualTo(3f).Within(Tolerance));
            Assert.That(trail.PointAt(1f).x, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Push_SmallStepsAddUp_StoreAPointOnceTheDistanceIsReached()
        {
            var trail = new PathTrail(8, 1f);
            trail.Reset(Vector3.zero);

            trail.Push(new Vector3(0.4f, 0f, 0f));
            trail.Push(new Vector3(0.8f, 0f, 0f));
            Assert.That(trail.Count, Is.EqualTo(1));

            trail.Push(new Vector3(1.2f, 0f, 0f));
            Assert.That(trail.Count, Is.EqualTo(2));
            Assert.That(trail.PointAt(0f).x, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Push_RemainderCarriesToTheNextPoint()
        {
            var trail = new PathTrail(8, 1f);
            trail.Reset(Vector3.zero);
            trail.Push(new Vector3(1.4f, 0f, 0f));

            trail.Push(new Vector3(2.1f, 0f, 0f));

            Assert.That(trail.Count, Is.EqualTo(3));
            Assert.That(trail.PointAt(0f).x, Is.EqualTo(2f).Within(Tolerance));
        }

        [Test]
        public void Push_SameSpot_StoresNothing()
        {
            var trail = Walked(8, 1f, 2.5f);
            int before = trail.Count;

            trail.Push(new Vector3(2.5f, 0f, 0f));
            trail.Push(new Vector3(2.5f, 0f, 0f));

            Assert.That(trail.Count, Is.EqualTo(before));
        }

        [Test]
        public void Push_DiagonalStep_PlacesPointsAlongTheLine()
        {
            var trail = new PathTrail(8, 2f);
            trail.Reset(Vector3.zero);

            trail.Push(new Vector3(3f, 0f, 4f));

            Assert.That(trail.Count, Is.EqualTo(3));
            Assert.That(Vector3.Distance(trail.PointAt(0f), new Vector3(2.4f, 0f, 3.2f)), Is.LessThan(Tolerance));
            Assert.That(Vector3.Distance(trail.PointAt(0.5f), new Vector3(1.2f, 0f, 1.6f)), Is.LessThan(Tolerance));
            Assert.That(trail.Length, Is.EqualTo(4f).Within(Tolerance));
        }

        [Test]
        public void Push_Full_DropsTheOldestPoints()
        {
            var trail = Walked(3, 1f, 5.2f);

            Assert.That(trail.Count, Is.EqualTo(3));
            Assert.That(trail.Length, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(trail.PointAt(0f).x, Is.EqualTo(5f).Within(Tolerance));
            Assert.That(trail.PointAt(1f).x, Is.EqualTo(3f).Within(Tolerance));
        }

        [Test]
        public void Push_ExactlyCapacityPoints_KeepsAll()
        {
            var trail = Walked(3, 1f, 2.2f);

            Assert.That(trail.Count, Is.EqualTo(3));
            Assert.That(trail.PointAt(1f).x, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void PointAt_BetweenPoints_Interpolates()
        {
            var trail = Walked(16, 1f, 4.2f);

            Assert.That(trail.PointAt(0.5f).x, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(trail.PointAt(0.125f).x, Is.EqualTo(3.5f).Within(Tolerance));
            Assert.That(trail.PointAt(0.875f).x, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void PointAt_LaneOneIsTheOldestPoint()
        {
            var trail = Walked(16, 1f, 4.2f);

            Assert.That(trail.PointAt(1f).x, Is.EqualTo(0f).Within(Tolerance));
        }

        [TestCase(1.25f, 0.25f)]
        [TestCase(-0.25f, 0.75f)]
        [TestCase(2.5f, 0.5f)]
        public void PointAt_OutsideZeroToOne_Wraps(float lane, float wrapped)
        {
            var trail = Walked(16, 1f, 4.2f);

            Assert.That(trail.PointAt(lane).x, Is.EqualTo(trail.PointAt(wrapped).x).Within(Tolerance));
        }

        [Test]
        public void PointAt_FollowsACorner()
        {
            var trail = new PathTrail(16, 1f);
            trail.Reset(Vector3.zero);
            trail.Push(new Vector3(2f, 0f, 0f));
            trail.Push(new Vector3(2f, 0f, 2f));

            Assert.That(trail.Length, Is.EqualTo(4f).Within(Tolerance));
            Assert.That(Vector3.Distance(trail.PointAt(0.5f), new Vector3(2f, 0f, 0f)), Is.LessThan(Tolerance));
            Assert.That(Vector3.Distance(trail.PointAt(0.25f), new Vector3(2f, 0f, 1f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void CopyTo_WritesNewestFirstWithDistanceFromTheNewest()
        {
            var trail = Walked(16, 1f, 3.2f);
            var target = new Vector4[8];

            int written = trail.CopyTo(target);

            Assert.That(written, Is.EqualTo(4));
            Assert.That(target[0].x, Is.EqualTo(3f).Within(Tolerance));
            Assert.That(target[0].w, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(target[1].x, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(target[1].w, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(target[3].x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(target[3].w, Is.EqualTo(3f).Within(Tolerance));
        }

        [Test]
        public void CopyTo_LeavesTheRestOfALargerTargetAlone()
        {
            var trail = Walked(16, 1f, 1.2f);
            var target = new Vector4[4];
            target[3] = new Vector4(9f, 9f, 9f, 9f);

            trail.CopyTo(target);

            Assert.That(target[3], Is.EqualTo(new Vector4(9f, 9f, 9f, 9f)));
        }

        [Test]
        public void CopyTo_TargetSmallerThanCount_WritesOnlyWhatFits()
        {
            var trail = Walked(16, 1f, 5.2f);
            var target = new Vector4[2];

            int written = trail.CopyTo(target);

            Assert.That(written, Is.EqualTo(2));
            Assert.That(target[0].x, Is.EqualTo(5f).Within(Tolerance));
            Assert.That(target[1].x, Is.EqualTo(4f).Within(Tolerance));
        }

        [Test]
        public void CopyTo_AfterOverflow_StillNewestFirst()
        {
            var trail = Walked(3, 1f, 5.2f);
            var target = new Vector4[3];

            int written = trail.CopyTo(target);

            Assert.That(written, Is.EqualTo(3));
            Assert.That(target[0].x, Is.EqualTo(5f).Within(Tolerance));
            Assert.That(target[2].x, Is.EqualTo(3f).Within(Tolerance));
            Assert.That(target[2].w, Is.EqualTo(2f).Within(Tolerance));
        }

        [Test]
        public void CopyTo_NullTarget_Throws()
        {
            var trail = Walked(8, 1f, 2.2f);

            Assert.That(() => trail.CopyTo(null), Throws.TypeOf<System.ArgumentNullException>());
        }

        // The river is measured from the core, which runs ahead of the newest point by up to one spacing. Without the lead
        // the river's places would jump a spacing forward each time a point is stored, instead of gliding with the core.
        [Test]
        public void Lead_AfterSteps_IsTheMetresFromTheNewestPointToTheCore()
        {
            var trail = new PathTrail(8, 1f);
            trail.Reset(Vector3.zero);
            Assert.That(trail.Lead, Is.EqualTo(0f).Within(Tolerance));

            trail.Push(new Vector3(0.4f, 0f, 0f));
            Assert.That(trail.Lead, Is.EqualTo(0.4f).Within(Tolerance));

            trail.Push(new Vector3(2.3f, 0f, 0f));
            Assert.That(trail.Lead, Is.EqualTo(0.3f).Within(Tolerance));
        }

        [Test]
        public void Travelled_AddsUpEveryStepAndStartsAgainOnReset()
        {
            var trail = new PathTrail(4, 1f);
            trail.Reset(Vector3.zero);

            trail.Push(new Vector3(3f, 0f, 0f));
            trail.Push(new Vector3(3f, 0f, 4f));
            trail.Push(new Vector3(3f, 0f, 4f));
            Assert.That(trail.Travelled, Is.EqualTo(7.0).Within(Tolerance));

            trail.Reset(new Vector3(1f, 0f, 1f));
            Assert.That(trail.Travelled, Is.EqualTo(0.0).Within(Tolerance));
        }
    }
}
