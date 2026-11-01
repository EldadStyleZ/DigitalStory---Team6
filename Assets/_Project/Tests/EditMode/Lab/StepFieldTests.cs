using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;

namespace DigitalStory.Tests.EditMode
{
    public sealed class StepFieldTests
    {
        // Cells 0 and 1 sit side by side with a wall between them, so the only way round is 0, 2, 3, 1.
        private static Maze Horseshoe()
        {
            return new Maze(
                new MazeShape(2, 2, 3, 1, 0f),
                new[] { false, false, true, false },
                new[] { true, true, false, false });
        }

        [Test]
        public void StepsAt_AcrossAWall_CountsTheWalkNotTheGap()
        {
            var field = new StepField(Horseshoe());

            field.Fill(0);

            Assert.That(field.StepsAt(1), Is.EqualTo(3));
        }

        [Test]
        public void StepToward_AcrossAWall_GoesTheLongWayRound()
        {
            var field = new StepField(Horseshoe());

            field.Fill(1);

            Assert.That(field.StepToward(0), Is.EqualTo(2));
        }

        [Test]
        public void StepToward_AtTheSource_StaysThere()
        {
            var field = new StepField(Horseshoe());

            field.Fill(3);

            Assert.That(field.StepToward(3), Is.EqualTo(3));
        }

        [Test]
        public void StepsAt_SeveralSources_MeasuresToTheNearest()
        {
            var field = new StepField(Horseshoe());

            field.Clear();
            field.AddSource(0);
            field.AddSource(1);
            field.Spread();

            Assert.That(field.StepsAt(2), Is.EqualTo(1));
            Assert.That(field.StepsAt(3), Is.EqualTo(1));
        }

        [Test]
        public void StepsAt_NoSources_EverythingIsUnreached()
        {
            var field = new StepField(Horseshoe());

            field.Clear();
            field.Spread();

            Assert.That(field.StepsAt(0), Is.EqualTo(StepField.Unreached));
        }

        [Test]
        public void Furthest_AfterFilling_IsTheLongestWalk()
        {
            var field = new StepField(Horseshoe());

            field.Fill(0);

            Assert.That(field.Furthest, Is.EqualTo(3));
        }
    }
}
