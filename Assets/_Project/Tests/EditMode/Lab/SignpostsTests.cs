using System.Linq;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using static DigitalStory.Tests.EditMode.LabTestData;

namespace DigitalStory.Tests.EditMode
{
    public sealed class SignpostsTests
    {
        private static readonly MazeShape Shape = new MazeShape(20, 20, 3, 1, 0.02f);

        private Maze _maze;

        [SetUp]
        public void BuildMaze() => _maze = MazeGenerator.Generate(Shape, new Mulberry32Random(5u));

        private int PassagesAt(int cell)
        {
            return Enumerable.Range(0, Maze.DirectionCount).Count(direction => _maze.NeighbourThrough(cell, direction) != Maze.NoCell);
        }

        private int FarCell()
        {
            var field = new StepField(_maze);
            field.Fill(_maze.Entrance);
            return Enumerable.Range(0, _maze.CellCount).First(cell => field.StepsAt(cell) == field.Furthest);
        }

        [Test]
        public void Constructor_FullChance_MarksEveryJunction()
        {
            var signposts = new Signposts(_maze, 1f, new Mulberry32Random(1u));

            int[] junctions = Enumerable.Range(0, _maze.CellCount).Where(cell => PassagesAt(cell) >= 3).ToArray();
            Assert.That(signposts.Cells, Is.EquivalentTo(junctions));
            Assert.That(junctions, Is.Not.Empty);
        }

        [Test]
        public void Constructor_NoChance_MarksNothing()
        {
            var signposts = new Signposts(_maze, 0f, new Mulberry32Random(1u));

            Assert.That(signposts.Cells, Is.Empty);
        }

        [Test]
        public void Constructor_HalfChance_MarksOnlySomeJunctions()
        {
            int all = new Signposts(_maze, 1f, new Mulberry32Random(1u)).Cells.Count;

            int some = new Signposts(_maze, 0.5f, new Mulberry32Random(1u)).Cells.Count;

            Assert.That(some, Is.GreaterThan(0).And.LessThan(all));
        }

        [Test]
        public void Constructor_SameSeed_MarksTheSameCells()
        {
            var first = new Signposts(_maze, 0.5f, new Mulberry32Random(7u));
            var second = new Signposts(_maze, 0.5f, new Mulberry32Random(7u));

            Assert.That(first.Cells, Is.EqualTo(second.Cells));
        }

        [Test]
        public void DirectionAt_BeforeAnyGoal_PointsNowhere()
        {
            var signposts = new Signposts(_maze, 1f, new Mulberry32Random(1u));

            Assert.That(signposts.DirectionAt(signposts.Cells[0]), Is.EqualTo(Signposts.NoDirection));
        }

        [Test]
        public void PointTo_EveryArrow_StepsCloserToTheGoal()
        {
            int goal = FarCell();
            var field = new StepField(_maze);
            field.Fill(goal);
            var signposts = new Signposts(_maze, 1f, new Mulberry32Random(1u));

            signposts.PointTo(goal);

            foreach (int cell in signposts.Cells.Where(cell => cell != goal))
            {
                int next = _maze.NeighbourThrough(cell, signposts.DirectionAt(cell));
                Assert.That(next, Is.Not.EqualTo(Maze.NoCell), $"The arrow at {cell} points at a wall.");
                Assert.That(field.StepsAt(next), Is.EqualTo(field.StepsAt(cell) - 1), $"The arrow at {cell} is wrong.");
            }
        }

        [Test]
        public void PointTo_OnTheGoalItself_ShowsNoArrow()
        {
            var signposts = new Signposts(_maze, 1f, new Mulberry32Random(1u));
            int goal = signposts.Cells[0];

            signposts.PointTo(goal);

            Assert.That(signposts.DirectionAt(goal), Is.EqualTo(Signposts.NoDirection));
        }

        [Test]
        public void PointTo_ReportsTheChange()
        {
            var signposts = new Signposts(_maze, 1f, new Mulberry32Random(1u));
            int changes = 0;
            signposts.Changed += () => changes++;

            signposts.PointTo(FarCell());

            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void Clear_TakesEveryArrowAway()
        {
            var signposts = new Signposts(_maze, 1f, new Mulberry32Random(1u));
            signposts.PointTo(FarCell());

            signposts.Clear();

            Assert.That(signposts.Cells.All(cell => signposts.DirectionAt(cell) == Signposts.NoDirection), Is.True);
        }
    }
}
