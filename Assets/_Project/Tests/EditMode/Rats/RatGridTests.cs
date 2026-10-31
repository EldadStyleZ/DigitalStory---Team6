using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class RatGridTests
    {
        private const float CellSize = 36f;

        [Test]
        public void Constructor_TownSize_RoundsCellsUp()
        {
            RatGrid grid = NewGrid();

            Assert.That(grid.Columns, Is.EqualTo(56));
            Assert.That(grid.Rows, Is.EqualTo(37));
        }

        [TestCase(-10f, 0)]
        [TestCase(0f, 0)]
        [TestCase(35f, 0)]
        [TestCase(36f, 1)]
        [TestCase(5000f, 55)]
        public void ColumnOf_AnyPosition_StaysInsideTheGrid(float x, int expected)
        {
            Assert.That(NewGrid().ColumnOf(x), Is.EqualTo(expected));
        }

        [Test]
        public void Add_TwoRatsInOneCell_ListsBothLatestFirst()
        {
            RatGrid grid = NewGrid();
            grid.Clear();

            grid.Add(0, new Vector2(10f, 10f));
            grid.Add(1, new Vector2(20f, 20f));

            Assert.That(grid.FirstIn(0, 0), Is.EqualTo(1));
            Assert.That(grid.NextAfter(1), Is.EqualTo(0));
            Assert.That(grid.NextAfter(0), Is.EqualTo(RatGrid.End));
        }

        [Test]
        public void Clear_AfterAdding_EmptiesEveryCell()
        {
            RatGrid grid = NewGrid();
            grid.Clear();
            grid.Add(0, new Vector2(10f, 10f));

            grid.Clear();

            Assert.That(grid.FirstIn(0, 0), Is.EqualTo(RatGrid.End));
        }

        private static RatGrid NewGrid() => new RatGrid(2000f, 1300f, CellSize, 8);
    }
}
