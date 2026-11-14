using NUnit.Framework;

namespace SpacePartyGame.Tests
{
    public sealed class FollowPaceTests
    {
        [Test]
        public void Speed_LeaderWalking_IsTheNormalFollowSpeed()
        {
            Assert.That(FollowPace.Speed(3.6f, 4f, 4f, 1f), Is.EqualTo(3.6f).Within(0.0001f));
        }

        [Test]
        public void Speed_LeaderRunningTwiceAsFast_FullMatch_DoublesIt()
        {
            Assert.That(FollowPace.Speed(3.6f, 4f, 8f, 1f), Is.EqualTo(7.2f).Within(0.0001f));
        }

        [Test]
        public void Speed_HalfMatch_GetsHalfTheBoost()
        {
            Assert.That(FollowPace.Speed(3.6f, 4f, 8f, 0.5f), Is.EqualTo(5.4f).Within(0.0001f));
        }

        [Test]
        public void Speed_NoMatch_IgnoresTheRun()
        {
            Assert.That(FollowPace.Speed(3.6f, 4f, 8f, 0f), Is.EqualTo(3.6f).Within(0.0001f));
        }

        [Test]
        public void Speed_LeaderSlowerThanWalking_NeverSlowsFollowers()
        {
            Assert.That(FollowPace.Speed(3.6f, 4f, 0f, 1f), Is.EqualTo(3.6f).Within(0.0001f));
        }

        [Test]
        public void Speed_NoWalkSpeed_IsTheNormalFollowSpeed()
        {
            Assert.That(FollowPace.Speed(3.6f, 0f, 8f, 1f), Is.EqualTo(3.6f).Within(0.0001f));
        }
    }
}
