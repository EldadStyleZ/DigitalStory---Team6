using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;
using static DigitalStory.Tests.EditMode.LabTestData;

namespace DigitalStory.Tests.EditMode
{
    // What a fighter has just done, which the view turns into walking legs, lunges and flashes.
    public sealed class FighterMotionTests
    {
        private const float Tolerance = 0.001f;
        private const float Radius = 0.35f;
        private const float Speed = 2f;
        private const float LongAgo = 1e6f;

        private static readonly FighterStats Walker = new FighterStats(100f, Speed, 1f, 1f, 1f);

        private Maze _maze;
        private Creature _fighter;

        [SetUp]
        public void Build()
        {
            _maze = Corridor(4);
            _fighter = new Creature(Creature(Walker), _maze.CenterOf(0));
        }

        [Test]
        public void NewFighter_HasDoneNothingYet()
        {
            Assert.That(_fighter.DistanceWalked, Is.EqualTo(0f));
            Assert.That(_fighter.SecondsSinceStep, Is.GreaterThan(LongAgo));
            Assert.That(_fighter.SecondsSinceStrike, Is.GreaterThan(LongAgo));
            Assert.That(_fighter.SecondsSinceHurt, Is.GreaterThan(LongAgo));
        }

        [Test]
        public void WalkToward_AddsTheDistanceCovered()
        {
            _fighter.WalkToward(_maze.CenterOf(3), _maze, Radius, 0.5f);

            Assert.That(_fighter.DistanceWalked, Is.EqualTo(Speed * 0.5f).Within(Tolerance));
        }

        // A fighter pressed against a wall would otherwise run on the spot.
        [Test]
        public void WalkToward_IntoAWall_CountsOnlyTheDistanceCovered()
        {
            Vector2 start = _fighter.Position;

            for (int step = 0; step < 30; step++)
            {
                _fighter.WalkToward(start + (Vector2.up * 20f), _maze, Radius, 0.1f);
            }

            Assert.That(_fighter.DistanceWalked, Is.EqualTo(Vector2.Distance(start, _fighter.Position)).Within(Tolerance));
            Assert.That(_fighter.DistanceWalked, Is.LessThan(Speed * 3f));
        }

        [Test]
        public void WalkToward_MarksAStep()
        {
            _fighter.WalkToward(_maze.CenterOf(3), _maze, Radius, 0.1f);
            _fighter.PassTime(0.3f);

            Assert.That(_fighter.SecondsSinceStep, Is.EqualTo(0.3f).Within(Tolerance));
        }

        [Test]
        public void WalkToward_AWallStraightAhead_IsNoStep()
        {
            Vector2 start = _fighter.Position;

            for (int step = 0; step < 30; step++)
            {
                _fighter.WalkToward(start + (Vector2.up * 20f), _maze, Radius, 0.1f);
            }

            _fighter.PassTime(0.3f);
            _fighter.WalkToward(start + (Vector2.up * 20f), _maze, Radius, 0.1f);

            Assert.That(_fighter.SecondsSinceStep, Is.EqualTo(0.3f).Within(Tolerance));
        }

        // Being pushed through a crowd is not walking, so the legs stay still.
        [Test]
        public void Nudge_IsNotWalking()
        {
            _fighter.Nudge(Vector2.right, _maze, Radius);

            Assert.That(_fighter.DistanceWalked, Is.EqualTo(0f));
            Assert.That(_fighter.SecondsSinceStep, Is.GreaterThan(LongAgo));
        }

        [Test]
        public void MarkHit_StartsTheStrikeClock()
        {
            _fighter.MarkHit();
            _fighter.PassTime(0.2f);

            Assert.That(_fighter.SecondsSinceStrike, Is.EqualTo(0.2f).Within(Tolerance));
        }

        [Test]
        public void TakeHit_StartsTheHurtClock()
        {
            _fighter.TakeHit(5f);
            _fighter.PassTime(0.1f);

            Assert.That(_fighter.SecondsSinceHurt, Is.EqualTo(0.1f).Within(Tolerance));
        }

        [Test]
        public void TakeHit_ThatTakesNoHealth_IsNoHurt()
        {
            _fighter.TakeHit(0f);

            Assert.That(_fighter.SecondsSinceHurt, Is.GreaterThan(LongAgo));
        }

        [Test]
        public void PassTime_LeavesTheHitWaitAlone()
        {
            _fighter.MarkHit();
            _fighter.PassTime(5f);

            Assert.That(_fighter.CanHitNow, Is.False);
        }

        [Test]
        public void TurnToward_FacesThePoint()
        {
            _fighter.TurnToward(_fighter.Position + Vector2.up);

            Assert.That(_fighter.Facing, Is.EqualTo(90f).Within(Tolerance));
        }

        [Test]
        public void TurnToward_ItsOwnSpot_KeepsItsFacing()
        {
            _fighter.TurnToward(_fighter.Position + Vector2.down);
            _fighter.TurnToward(_fighter.Position);

            Assert.That(_fighter.Facing, Is.EqualTo(-90f).Within(Tolerance));
        }
    }
}
