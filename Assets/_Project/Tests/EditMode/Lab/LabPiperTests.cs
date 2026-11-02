using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class LabPiperTests
    {
        private const float Tolerance = 0.001f;

        private LabPlayerConfig _config;
        private Maze _maze;
        private LabPiper _piper;

        [SetUp]
        public void CreatePiper()
        {
            _config = ScriptableObject.CreateInstance<LabPlayerConfig>();
            _maze = new Maze(new MazeShape(2, 1, 3, 1, 0f), new[] { true, false }, new[] { false, false });
            _piper = new LabPiper(_config, _maze);
        }

        [Test]
        public void Piper_AtTheStart_StandsAtTheEntrance()
        {
            Assert.That(_piper.Position, Is.EqualTo(_maze.CenterOf(_maze.Entrance)));
        }

        [Test]
        public void Tick_WalkingEast_MovesAtTheConfiguredSpeed()
        {
            Vector2 start = _piper.Position;

            _piper.Tick(Vector2.right, 0.1f);

            Assert.That(_piper.Position.x - start.x, Is.EqualTo(_config.MoveSpeed * 0.1f).Within(Tolerance));
        }

        [Test]
        public void Tick_SpeedChangedDuringARun_UsesTheNewSpeed()
        {
            Vector2 start = _piper.Position;

            JsonUtility.FromJsonOverwrite("{\"<MoveSpeed>k__BackingField\": 9}", _config);
            _piper.Tick(Vector2.right, 0.1f);

            Assert.That(_piper.Position.x - start.x, Is.EqualTo(0.9f).Within(Tolerance));
        }

        [Test]
        public void Tick_WalkingDiagonally_IsNoFasterThanStraight()
        {
            var piper = new LabPiper(_config, new Maze(new MazeShape(1, 1, 9, 1, 0f), new[] { false }, new[] { false }));
            Vector2 start = piper.Position;

            piper.Tick(new Vector2(1f, 1f), 0.1f);

            Assert.That((piper.Position - start).magnitude, Is.EqualTo(_config.MoveSpeed * 0.1f).Within(Tolerance));
        }

        [Test]
        public void Tick_WalkingIntoAWall_DoesNotCrossIt()
        {
            for (int step = 0; step < 60; step++)
            {
                _piper.Tick(Vector2.down, 1f / 60f);
            }

            Assert.That(_piper.Position.y, Is.GreaterThanOrEqualTo(1f + _config.Radius - Tolerance));
        }

        [Test]
        public void Tick_Walking_FacesTheWayItWalks()
        {
            _piper.Tick(Vector2.up, 0.01f);

            Assert.That(_piper.Facing, Is.EqualTo(90f).Within(Tolerance));
        }

        [Test]
        public void Tick_StandingStill_KeepsFacingTheSameWay()
        {
            _piper.Tick(Vector2.up, 0.01f);
            _piper.Tick(Vector2.zero, 0.01f);

            Assert.That(_piper.Facing, Is.EqualTo(90f).Within(Tolerance));
        }

        [Test]
        public void MoveTo_ACell_StandsAtItsCenter()
        {
            _piper.MoveTo(1);

            Assert.That(_piper.Position, Is.EqualTo(_maze.CenterOf(1)));
        }
    }
}
