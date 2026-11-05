using NUnit.Framework;
using UnityEngine;

namespace PiperToy.Tests
{
    // The conga line's path is plain C#, so these tests need no scene and run in a moment.
    public sealed class CongaLineTests
    {
        // The path keeps a footstep every tenth of a unit, so a corner can be cut by a few hundredths.
        private const float Tolerance = 0.05f;

        private CongaLine _line;

        [SetUp]
        public void StartALine()
        {
            _line = new CongaLine();
            _line.Begin(Vector2.zero, Vector2.left, 5f);
        }

        // Walks the Piper in small steps, as the physics steps do.
        private void Walk(Vector2 from, Vector2 to)
        {
            const int Steps = 100;

            for (int step = 1; step <= Steps; step++)
            {
                _line.Record(Vector2.Lerp(from, to, step / (float)Steps));
            }
        }

        private static void AssertNear(Vector2 actual, Vector2 expected)
        {
            Assert.That(Vector2.Distance(actual, expected), Is.LessThan(Tolerance), $"{actual} should be {expected}.");
        }

        [Test]
        public void Begin_BeforeThePiperWalks_TheLineIsStraightBehindIt()
        {
            AssertNear(_line.SpotBehind(2f), new Vector2(-2f, 0f));
        }

        [Test]
        public void Record_WhenThePiperWalksStraight_TheLineFollowsDirectlyBehind()
        {
            Walk(Vector2.zero, new Vector2(3f, 0f));

            AssertNear(_line.SpotBehind(1f), new Vector2(2f, 0f));
        }

        // The point of a conga: the rats go round the corner where the Piper went, not straight behind its back.
        [Test]
        public void Record_AfterThePiperTurnsACorner_TheLineFollowsItsFootsteps()
        {
            Walk(Vector2.zero, new Vector2(3f, 0f));
            Walk(new Vector2(3f, 0f), new Vector2(3f, 3f));

            AssertNear(_line.SpotBehind(1f), new Vector2(3f, 2f));
            AssertNear(_line.SpotBehind(4f), new Vector2(2f, 0f));
        }

        // The line forgets old footsteps, but never the ones its last rat still stands on.
        [Test]
        public void Record_AfterALongWalk_TheLastPlaceIsStillOnThePath()
        {
            Walk(Vector2.zero, new Vector2(20f, 0f));
            Walk(new Vector2(20f, 0f), new Vector2(20f, 20f));

            AssertNear(_line.SpotBehind(5f), new Vector2(20f, 15f));
        }

        [Test]
        public void SpotBehind_FurtherThanTheLine_IsItsLastPoint()
        {
            AssertNear(_line.SpotBehind(50f), new Vector2(-5f, 0f));
        }
    }
}
