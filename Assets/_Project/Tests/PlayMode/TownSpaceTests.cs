using DigitalStory.View;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.PlayMode
{
    public sealed class TownSpaceTests
    {
        private const float Tolerance = 0.001f;

        [Test]
        public void ToScene_TownPosition_FlipsTheVerticalAxis()
        {
            Vector3 scene = TownSpace.ToScene(new Vector2(500f, 300f));

            Assert.That(scene.x, Is.EqualTo(5f).Within(Tolerance));
            Assert.That(scene.y, Is.EqualTo(-3f).Within(Tolerance));
        }

        [Test]
        public void ToTown_ScenePosition_UndoesToScene()
        {
            var start = new Vector2(1234f, 567f);

            Vector2 result = TownSpace.ToTown(TownSpace.ToScene(start));

            Assert.That(Vector2.Distance(result, start), Is.LessThan(Tolerance));
        }

        [Test]
        public void ToSceneRotation_FacingDownTheTown_PointsDownTheScene()
        {
            Quaternion rotation = TownSpace.ToSceneRotation(Mathf.PI / 2f);

            Vector3 facing = rotation * Vector3.right;

            Assert.That(facing.y, Is.EqualTo(-1f).Within(Tolerance));
        }
    }
}
