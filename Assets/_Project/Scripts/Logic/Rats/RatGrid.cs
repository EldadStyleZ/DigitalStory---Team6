using System;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Sorts rats into cells, so a rat only checks those beside it.</summary>
    /// <remarks>
    /// Each cell is a linked list stored in two arrays, so rebuilding it every step allocates nothing.
    /// </remarks>
    public sealed class RatGrid
    {
        /// <summary>Marks the end of a cell's list.</summary>
        public const int End = -1;

        private readonly float _cellSize;
        private readonly int[] _firstInCell;
        private readonly int[] _nextInCell;

        public RatGrid(float townWidth, float townHeight, float cellSize, int ratCount)
        {
            _cellSize = cellSize;
            Columns = Mathf.CeilToInt(townWidth / cellSize);
            Rows = Mathf.CeilToInt(townHeight / cellSize);
            _firstInCell = new int[Columns * Rows];
            _nextInCell = new int[ratCount];
        }

        public int Columns { get; }
        public int Rows { get; }

        public void Clear()
        {
            Array.Fill(_firstInCell, End);
        }

        public void Add(int ratIndex, Vector2 position)
        {
            int cell = CellIndex(ColumnOf(position.x), RowOf(position.y));
            _nextInCell[ratIndex] = _firstInCell[cell];
            _firstInCell[cell] = ratIndex;
        }

        public int ColumnOf(float x) => Mathf.Clamp(Mathf.FloorToInt(x / _cellSize), 0, Columns - 1);

        public int RowOf(float y) => Mathf.Clamp(Mathf.FloorToInt(y / _cellSize), 0, Rows - 1);

        public int FirstIn(int column, int row) => _firstInCell[CellIndex(column, row)];

        public int NextAfter(int ratIndex) => _nextInCell[ratIndex];

        private int CellIndex(int column, int row) => column + (row * Columns);
    }
}
