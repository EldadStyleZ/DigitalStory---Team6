using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Plan section X: the trees, ruins, nest trees and hives the free swarm flies round, as the shader reads them, and the
    /// numbers of the bend round them.
    /// </summary>
    public sealed class SwarmObstaclesTests
    {
        private const string ShaderPath = "_Project/Art/F010/Shaders/WaspSwarm.compute";
        private const float Tolerance = 0.0001f;

        [Test]
        public void FromBox_TakesTheMiddleOnTheGround_AShareOfTheMeanHalfWidth_AndTheTop()
        {
            var box = new Bounds(new Vector3(3f, 2.5f, -4f), new Vector3(4f, 5f, 2f));

            Vector4 obstacle = SwarmObstacles.FromBox(box, 0.8f);

            Assert.AreEqual(3f, obstacle.x, Tolerance, "x of the middle");
            Assert.AreEqual(-4f, obstacle.y, Tolerance, "z of the middle");
            Assert.AreEqual(0.8f * 1.5f, obstacle.z, Tolerance, "radius: the share of the mean of 2 and 1");
            Assert.AreEqual(5f, obstacle.w, Tolerance, "top");
        }

        [Test]
        public void Fill_SkipsABoxWithNoWidth()
        {
            var boxes = new List<Bounds>
            {
                new Bounds(Vector3.zero, new Vector3(0f, 3f, 0f)),
                new Bounds(new Vector3(5f, 1f, 5f), new Vector3(2f, 2f, 2f)),
            };
            var obstacles = new Vector4[4];

            int count = SwarmObstacles.Fill(boxes, 1f, obstacles);

            Assert.AreEqual(1, count);
            Assert.AreEqual(5f, obstacles[0].x, Tolerance);
        }

        [Test]
        public void Fill_StopsWhenTheArrayIsFull()
        {
            var boxes = new List<Bounds>();
            for (int index = 0; index < SwarmObstacles.MostObstacles + 5; index++)
            {
                boxes.Add(new Bounds(new Vector3(index, 1f, 0f), Vector3.one * 2f));
            }

            Assert.AreEqual(3, SwarmObstacles.Fill(boxes, 1f, new Vector4[3]));
            Assert.AreEqual(SwarmObstacles.MostObstacles, SwarmObstacles.Fill(boxes, 1f, new Vector4[SwarmObstacles.MostObstacles + 5]));
        }

        [Test]
        public void TheShader_HoldsAsManyObstaclesAsTheRule()
        {
            string text = File.ReadAllText(Path.Combine(Application.dataPath, ShaderPath));
            Match match = Regex.Match(text, @"#define\s+OBSTACLE_MOST\s+(\d+)");

            Assert.That(match.Success, Is.True, "WaspSwarm.compute defines no OBSTACLE_MOST.");
            Assert.AreEqual(SwarmObstacles.MostObstacles, int.Parse(match.Groups[1].Value));
        }

        // The stream hugs each obstacle a little outside its radius, parts before it, closes again behind it and speeds up a
        // little on the way round; the full bend keeps every goal out of the obstacle.
        [Test]
        public void FlowDefaults_PartBeforeTheObstacle_HugIt_AndCloseBehindIt()
        {
            var config = ScriptableObject.CreateInstance<SwarmConfig>();

            Assert.That(config.ObstacleRadiusShare, Is.InRange(0.5f, 1f));
            Assert.That(config.ObstacleMargin, Is.GreaterThan(0f));
            Assert.That(config.ObstacleLeadMeters, Is.GreaterThan(config.ObstacleMargin));
            Assert.That(config.ObstacleCloseMeters, Is.GreaterThan(0f));
            Assert.AreEqual(1f, config.ObstacleStrength, Tolerance);
            Assert.That(config.ObstacleSpeedUp, Is.InRange(0.05f, 0.5f));

            Object.DestroyImmediate(config);
        }
    }
}
