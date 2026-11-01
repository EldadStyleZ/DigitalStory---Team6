using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class MazeTests
    {
        private const float Tolerance = 0.001f;
        private const float Radius = 0.4f;

        private static readonly MazeShape Small = new MazeShape(8, 6, 3, 1, 0f);

        private static Maze Build(MazeShape shape, uint seed = 7)
        {
            return MazeGenerator.Generate(shape, new Mulberry32Random(seed));
        }

        // Cell 0 spans tiles 1 to 3, the wall between is tile 4, and cell 1 spans tiles 5 to 7.
        private static Maze TwoCells(bool isOpen)
        {
            return new Maze(new MazeShape(2, 1, 3, 1, 0f), new[] { isOpen, false }, new[] { false, false });
        }

        [TestCase(1u)]
        [TestCase(2u)]
        [TestCase(99u)]
        public void Generate_AnySeed_EveryCellCanBeReached(uint seed)
        {
            Maze maze = Build(Small, seed);
            var field = new StepField(maze);
            field.Fill(maze.Entrance);

            for (int cell = 0; cell < maze.CellCount; cell++)
            {
                Assert.That(field.StepsAt(cell), Is.Not.EqualTo(StepField.Unreached), $"Cell {cell} is cut off.");
            }
        }

        [Test]
        public void Generate_NoLoops_HasExactlyOneRouteBetweenAnyTwoCells()
        {
            Maze maze = Build(Small);

            Assert.That(maze.PassageCount, Is.EqualTo(maze.CellCount - 1));
        }

        [Test]
        public void Generate_WithLoops_OpensMorePassages()
        {
            Maze plain = Build(Small);
            Maze looped = Build(new MazeShape(8, 6, 3, 1, 0.5f));

            Assert.That(looped.PassageCount, Is.GreaterThan(plain.PassageCount));
        }

        [Test]
        public void Generate_SameSeed_BuildsTheSameMaze()
        {
            Assert.That(Layout(Build(Small, 5)), Is.EqualTo(Layout(Build(Small, 5))));
        }

        [Test]
        public void Generate_OtherSeed_BuildsAnotherMaze()
        {
            Assert.That(Layout(Build(Small, 5)), Is.Not.EqualTo(Layout(Build(Small, 6))));
        }

        [Test]
        public void IsFloor_InsideACell_IsTrue()
        {
            Assert.That(TwoCells(false).IsFloor(2, 2), Is.True);
        }

        [TestCase(0, 2)]
        [TestCase(2, 0)]
        [TestCase(2, 4)]
        [TestCase(8, 2)]
        [TestCase(-1, 2)]
        [TestCase(9, 2)]
        public void IsFloor_OnOrPastTheOuterWall_IsFalse(int tileX, int tileY)
        {
            Assert.That(TwoCells(true).IsFloor(tileX, tileY), Is.False);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void IsFloor_BetweenCells_DependsOnThePassage(bool isOpen)
        {
            Assert.That(TwoCells(isOpen).IsFloor(4, 2), Is.EqualTo(isOpen));
        }

        [Test]
        public void IsFloor_WhereFourWallsMeet_IsAlwaysFalse()
        {
            var open = new Maze(new MazeShape(2, 2, 3, 1, 0f), new[] { true, false, true, false }, new[] { true, true, false, false });

            Assert.That(open.IsFloor(4, 4), Is.False);
        }

        [Test]
        public void CenterOf_FirstCell_IsTheMiddleOfItsTiles()
        {
            Assert.That(TwoCells(false).CenterOf(0), Is.EqualTo(new Vector2(2.5f, 2.5f)));
        }

        [Test]
        public void CellAt_TheCenterOfACell_FindsThatCell()
        {
            Maze maze = Build(Small);

            for (int cell = 0; cell < maze.CellCount; cell++)
            {
                Assert.That(maze.CellAt(maze.CenterOf(cell)), Is.EqualTo(cell));
            }
        }

        [Test]
        public void Move_IntoAWall_StopsAtIt()
        {
            Maze maze = TwoCells(false);
            Vector2 position = maze.CenterOf(0);

            for (int step = 0; step < 40; step++)
            {
                position = maze.Move(position, new Vector2(0.1f, 0f), Radius);
            }

            Assert.That(position.x, Is.LessThanOrEqualTo(4f - Radius + Tolerance));
            Assert.That(position.x, Is.GreaterThan(3f), "It should reach the wall, not stop short.");
        }

        [Test]
        public void Move_ThroughAPassage_ReachesTheNextCell()
        {
            Maze maze = TwoCells(true);
            Vector2 position = maze.CenterOf(0);

            for (int step = 0; step < 40; step++)
            {
                position = maze.Move(position, new Vector2(0.1f, 0f), Radius);
            }

            Assert.That(maze.CellAt(position), Is.EqualTo(1));
        }

        [Test]
        public void Move_DiagonallyIntoAWall_SlidesAlongIt()
        {
            Maze maze = TwoCells(false);
            var nearTheWall = new Vector2(3.5f, 2.5f);

            Vector2 position = maze.Move(nearTheWall, new Vector2(0.2f, 0.2f), Radius);

            Assert.That(position.x, Is.EqualTo(nearTheWall.x).Within(Tolerance), "The wall stops the east step.");
            Assert.That(position.y, Is.EqualTo(2.7f).Within(Tolerance), "The north step still happens.");
        }

        private static string Layout(Maze maze)
        {
            var tiles = new char[maze.Shape.TileColumns * maze.Shape.TileRows];

            for (int y = 0; y < maze.Shape.TileRows; y++)
            {
                for (int x = 0; x < maze.Shape.TileColumns; x++)
                {
                    tiles[x + (y * maze.Shape.TileColumns)] = maze.IsFloor(x, y) ? '.' : '#';
                }
            }

            return new string(tiles);
        }
    }
}
