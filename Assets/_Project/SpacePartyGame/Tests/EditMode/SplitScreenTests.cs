using NUnit.Framework;
using UnityEngine;

namespace SpacePartyGame.Tests
{
    public sealed class SplitLayoutTests
    {
        [Test]
        public void For_OnePlayer_FillsTheScreenWithNoOverview()
        {
            SplitLayout layout = SplitLayout.For(1);

            Assert.That(layout.Players, Is.EqualTo(new[] { new Rect(0f, 0f, 1f, 1f) }));
            Assert.That(layout.HasOverview, Is.False);
        }

        [Test]
        public void For_TwoPlayers_GivesP1TheLeftHalfAndP2TheRight()
        {
            SplitLayout layout = SplitLayout.For(2);

            Assert.That(layout.Players, Is.EqualTo(new[] { new Rect(0f, 0f, 0.5f, 1f), new Rect(0.5f, 0f, 0.5f, 1f) }));
            Assert.That(layout.HasOverview, Is.False);
            Assert.That(layout.HasVerticalDivider, Is.True);
            Assert.That(layout.HasHorizontalDivider, Is.False);
        }

        [Test]
        public void For_ThreePlayers_GivesQuadrantsAndTheOverviewBottomRight()
        {
            SplitLayout layout = SplitLayout.For(3);

            Assert.That(layout.Players, Is.EqualTo(new[]
            {
                new Rect(0f, 0.5f, 0.5f, 0.5f),
                new Rect(0.5f, 0.5f, 0.5f, 0.5f),
                new Rect(0f, 0f, 0.5f, 0.5f),
            }));
            Assert.That(layout.HasOverview, Is.True);
            Assert.That(layout.Overview, Is.EqualTo(new Rect(0.5f, 0f, 0.5f, 0.5f)));
            Assert.That(layout.HasVerticalDivider, Is.True);
            Assert.That(layout.HasHorizontalDivider, Is.True);
        }

        [Test]
        public void For_FourPlayers_GivesQuadrantsInReadingOrder()
        {
            SplitLayout layout = SplitLayout.For(4);

            Assert.That(layout.Players, Is.EqualTo(new[]
            {
                new Rect(0f, 0.5f, 0.5f, 0.5f),
                new Rect(0.5f, 0.5f, 0.5f, 0.5f),
                new Rect(0f, 0f, 0.5f, 0.5f),
                new Rect(0.5f, 0f, 0.5f, 0.5f),
            }));
            Assert.That(layout.HasOverview, Is.False);
        }

        [TestCase(0)]
        [TestCase(5)]
        public void For_OutsideOneToFour_Throws(int players)
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => SplitLayout.For(players));
        }
    }

    public sealed class CameraFramingTests
    {
        private static readonly Rect Map = new Rect(-8f, -4f, 16f, 9f);

        [Test]
        public void Clamp_ViewInsideTheMap_StaysPut()
        {
            Vector2 centre = CameraFraming.Clamp(new Vector2(1f, 1f), new Vector2(3f, 2f), Map);

            Assert.That(centre, Is.EqualTo(new Vector2(1f, 1f)));
        }

        [Test]
        public void Clamp_PastTheLeftAndTopEdges_StopsWithTheViewOnTheEdges()
        {
            Vector2 centre = CameraFraming.Clamp(new Vector2(-7.5f, 4.8f), new Vector2(3f, 2f), Map);

            Assert.That(centre, Is.EqualTo(new Vector2(-5f, 3f)));
        }

        [Test]
        public void Clamp_PastTheRightAndBottomEdges_StopsWithTheViewOnTheEdges()
        {
            Vector2 centre = CameraFraming.Clamp(new Vector2(10f, -6f), new Vector2(3f, 2f), Map);

            Assert.That(centre, Is.EqualTo(new Vector2(5f, -2f)));
        }

        [Test]
        public void Clamp_ViewWiderThanTheMap_CentresThatAxis()
        {
            Vector2 centre = CameraFraming.Clamp(new Vector2(6f, 2f), new Vector2(9f, 2f), Map);

            Assert.That(centre, Is.EqualTo(new Vector2(0f, 2f)));
        }

        [Test]
        public void HalfExtents_OrthoSizeAndAspect_GiveTheHalfWidthAndHeight()
        {
            Assert.That(CameraFraming.HalfExtents(3f, 2f), Is.EqualTo(new Vector2(6f, 3f)));
        }

        [TestCase(16f / 9f, 4.5f)]
        [TestCase(1f, 8f)]
        [TestCase(3f, 4.5f)]
        public void SizeToFit_ShowsTheWholeMap(float aspect, float expected)
        {
            Assert.That(CameraFraming.SizeToFit(Map, aspect), Is.EqualTo(expected).Within(0.0001f));
        }
    }
}
