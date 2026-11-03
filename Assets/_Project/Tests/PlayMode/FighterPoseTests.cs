using DigitalStory.View;
using NUnit.Framework;

namespace DigitalStory.Tests.PlayMode
{
    // Plain sums, so they need no scene.
    public sealed class FighterPoseTests
    {
        private const float Tolerance = 0.001f;
        private const float Stride = 2f;
        private const int Frames = 4;

        [TestCase(0f, 0)]
        [TestCase(0.49f, 0)]
        [TestCase(0.5f, 1)]
        [TestCase(1f, 2)]
        [TestCase(1.99f, 3)]
        [TestCase(2f, 0)]
        [TestCase(1000.5f, 1)]
        public void WalkFrame_FollowsTheDistanceWalked(float distance, int expected)
        {
            Assert.That(FighterPose.WalkFrame(distance, Stride, Frames), Is.EqualTo(expected));
        }

        [Test]
        public void WalkFrame_WithNoFrames_IsNone()
        {
            Assert.That(FighterPose.WalkFrame(1f, Stride, 0), Is.EqualTo(0));
        }

        [TestCase(0f, 0f)]
        [TestCase(0.1f, 1f)]
        [TestCase(0.05f, 0.7071f)]
        [TestCase(0.2f, 0f)]
        [TestCase(5f, 0f)]
        [TestCase(float.PositiveInfinity, 0f)]
        public void Lunge_GoesOutAndBack(float seconds, float expected)
        {
            Assert.That(FighterPose.Lunge(seconds, 0.2f), Is.EqualTo(expected).Within(Tolerance));
        }

        [TestCase(0f, 1f)]
        [TestCase(0.1f, 0.5f)]
        [TestCase(0.2f, 0f)]
        [TestCase(float.PositiveInfinity, 0f)]
        public void Flash_FadesOut(float seconds, float expected)
        {
            Assert.That(FighterPose.Flash(seconds, 0.2f), Is.EqualTo(expected).Within(Tolerance));
        }

        [TestCase(0f, 0f, 0f)]
        [TestCase(0.5f, 0f, 1f)]
        [TestCase(1.5f, 0f, -1f)]
        [TestCase(0f, 0.25f, 1f)]
        public void Breath_RisesAndFalls(float seconds, float phase, float expected)
        {
            Assert.That(FighterPose.Breath(seconds, phase, 2f), Is.EqualTo(expected).Within(Tolerance));
        }
    }
}
