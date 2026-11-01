using System;
using System.Collections.Generic;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Arrows on the floor of some junctions, each following the walking route to a goal. They use the
    /// route, not the straight line, so they never point at a wall or down a dead end.
    /// </summary>
    public sealed class Signposts
    {
        /// <summary>The direction of a cell with no arrow to show.</summary>
        public const int NoDirection = -1;

        private const int PassagesInAJunction = 3;

        private readonly Maze _maze;
        private readonly StepField _toGoal;
        private readonly List<int> _cells = new List<int>();
        private readonly int[] _directions;

        public Signposts(Maze maze, float chance, IRandomSource random)
        {
            _maze = maze ?? throw new ArgumentNullException(nameof(maze));
            _ = random ?? throw new ArgumentNullException(nameof(random));
            _toGoal = new StepField(maze);
            _directions = new int[maze.CellCount];
            Array.Fill(_directions, NoDirection);

            for (int cell = 0; cell < maze.CellCount; cell++)
            {
                if (IsJunction(cell) && random.NextDouble() < chance)
                {
                    _cells.Add(cell);
                }
            }
        }

        /// <summary>Raised when any arrow changes.</summary>
        public event Action Changed;

        /// <summary>The cells with a place for an arrow.</summary>
        public IReadOnlyList<int> Cells => _cells;

        /// <summary>Which way a cell's arrow points, as a direction of <see cref="Maze"/>, or <see cref="NoDirection"/>.</summary>
        public int DirectionAt(int cell) => _directions[cell];

        public void PointTo(int goal)
        {
            _toGoal.Fill(goal);

            foreach (int cell in _cells)
            {
                _directions[cell] = DirectionToGoal(cell);
            }

            Changed?.Invoke();
        }

        /// <summary>Takes every arrow away.</summary>
        public void Clear()
        {
            Array.Fill(_directions, NoDirection);
            Changed?.Invoke();
        }

        private bool IsJunction(int cell)
        {
            int passages = 0;

            for (int direction = 0; direction < Maze.DirectionCount; direction++)
            {
                if (_maze.NeighbourThrough(cell, direction) != Maze.NoCell)
                {
                    passages++;
                }
            }

            return passages >= PassagesInAJunction;
        }

        private int DirectionToGoal(int cell)
        {
            int next = _toGoal.StepToward(cell);

            for (int direction = 0; next != cell && direction < Maze.DirectionCount; direction++)
            {
                if (_maze.NeighbourThrough(cell, direction) == next)
                {
                    return direction;
                }
            }

            return NoDirection;
        }
    }
}
