using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// A maze of cells joined by passages. One tile is one world unit, with tile 0,0 at the south-west corner.
    /// </summary>
    public sealed class Maze
    {
        public const int NoCell = -1;
        public const int DirectionCount = 4;

        private const int East = 0;
        private const int North = 1;
        private const int West = 2;

        private readonly bool[] _openEast;
        private readonly bool[] _openNorth;

        /// <param name="openEast">One entry per cell: true when it opens to the cell east of it.</param>
        /// <param name="openNorth">One entry per cell: true when it opens to the cell north of it.</param>
        public Maze(MazeShape shape, bool[] openEast, bool[] openNorth)
        {
            if (openEast == null || openEast.Length != shape.CellCount)
            {
                throw new ArgumentException($"Needs one entry for each of the {shape.CellCount} cells.", nameof(openEast));
            }

            if (openNorth == null || openNorth.Length != shape.CellCount)
            {
                throw new ArgumentException($"Needs one entry for each of the {shape.CellCount} cells.", nameof(openNorth));
            }

            Shape = shape;
            _openEast = openEast;
            _openNorth = openNorth;
            PassageCount = CountOpen(openEast) + CountOpen(openNorth);
        }

        public MazeShape Shape { get; }
        public int CellCount => Shape.CellCount;
        public int PassageCount { get; }

        /// <summary>Where every walk starts.</summary>
        public int Entrance => 0;

        /// <summary>Returns the cell reached by walking one way from a cell, or <see cref="NoCell"/> if a wall is there.</summary>
        /// <param name="direction">A number from 0 up to, but not including, <see cref="DirectionCount"/>.</param>
        public int NeighbourThrough(int cell, int direction)
        {
            switch (direction)
            {
                case East:
                    return _openEast[cell] ? cell + 1 : NoCell;
                case North:
                    return _openNorth[cell] ? cell + Shape.Columns : NoCell;
                case West:
                    return cell % Shape.Columns > 0 && _openEast[cell - 1] ? cell - 1 : NoCell;
                default:
                    return cell >= Shape.Columns && _openNorth[cell - Shape.Columns] ? cell - Shape.Columns : NoCell;
            }
        }

        /// <summary>True when two cells are the same, or a passage joins them.</summary>
        public bool AreLinked(int first, int second)
        {
            if (first == second)
            {
                return true;
            }

            for (int direction = 0; direction < DirectionCount; direction++)
            {
                if (NeighbourThrough(first, direction) == second)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsFloor(int tileX, int tileY)
        {
            int x = tileX - Shape.WallTiles;
            int y = tileY - Shape.WallTiles;

            if (x < 0 || y < 0)
            {
                return false;
            }

            int column = x / Shape.CellPitch;
            int row = y / Shape.CellPitch;

            if (column >= Shape.Columns || row >= Shape.Rows)
            {
                return false;
            }

            bool isInCorridorX = x % Shape.CellPitch < Shape.CorridorTiles;
            bool isInCorridorY = y % Shape.CellPitch < Shape.CorridorTiles;
            int cell = column + (row * Shape.Columns);

            if (isInCorridorX && isInCorridorY)
            {
                return true;
            }

            if (isInCorridorY)
            {
                return _openEast[cell];
            }

            return isInCorridorX && _openNorth[cell];
        }

        public Vector2 CenterOf(int cell)
        {
            float halfCorridor = Shape.CorridorTiles / 2f;
            int column = cell % Shape.Columns;
            int row = cell / Shape.Columns;

            return new Vector2(
                Shape.WallTiles + (column * Shape.CellPitch) + halfCorridor,
                Shape.WallTiles + (row * Shape.CellPitch) + halfCorridor);
        }

        /// <summary>
        /// One of several spots in a ring around a cell's centre, so a group can share a cell without
        /// standing on each other. The ring is half a corridor wide, which keeps everyone off the walls.
        /// </summary>
        public Vector2 SpotAround(int cell, int index, int count)
        {
            float angle = index * (Mathf.PI * 2f) / count;
            float radius = Shape.CorridorTiles / 4f;
            return CenterOf(cell) + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }

        /// <summary>Returns the cell nearest a position. A position inside a passage belongs to the closer side.</summary>
        public int CellAt(Vector2 position)
        {
            int column = NearestIndex(position.x, Shape.Columns);
            int row = NearestIndex(position.y, Shape.Rows);
            return column + (row * Shape.Columns);
        }

        /// <summary>Moves a round body, stopping at walls and sliding along them.</summary>
        public Vector2 Move(Vector2 position, Vector2 delta, float radius)
        {
            Vector2 next = position;
            next.x += delta.x;

            if (Touches(next, radius))
            {
                next.x = position.x;
            }

            next.y += delta.y;

            if (Touches(next, radius))
            {
                next.y = position.y;
            }

            return next;
        }

        private static int CountOpen(bool[] openings)
        {
            int count = 0;

            foreach (bool isOpen in openings)
            {
                if (isOpen)
                {
                    count++;
                }
            }

            return count;
        }

        private int NearestIndex(float position, int count)
        {
            float fromFirstCell = position - Shape.WallTiles + (Shape.WallTiles / 2f);
            return Mathf.Clamp(Mathf.FloorToInt(fromFirstCell / Shape.CellPitch), 0, count - 1);
        }

        private bool Touches(Vector2 center, float radius)
        {
            int left = Mathf.FloorToInt(center.x - radius);
            int right = Mathf.FloorToInt(center.x + radius);
            int bottom = Mathf.FloorToInt(center.y - radius);
            int top = Mathf.FloorToInt(center.y + radius);

            for (int y = bottom; y <= top; y++)
            {
                for (int x = left; x <= right; x++)
                {
                    if (!IsFloor(x, y))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
