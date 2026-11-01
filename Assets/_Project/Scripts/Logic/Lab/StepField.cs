using System;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Measures how many cells every cell is from its sources by walking, so anything can find its way
    /// through the maze by stepping downhill. Walls count, which a straight-line distance would miss.
    /// </summary>
    public sealed class StepField
    {
        public const int Unreached = int.MaxValue;

        private readonly Maze _maze;
        private readonly int[] _steps;
        private readonly int[] _queue;

        private int _queueStart;
        private int _queueEnd;

        public StepField(Maze maze)
        {
            _maze = maze ?? throw new ArgumentNullException(nameof(maze));
            _steps = new int[maze.CellCount];
            _queue = new int[maze.CellCount];
            Clear();
        }

        /// <summary>The longest walk from a source to any cell reached.</summary>
        public int Furthest { get; private set; }

        /// <summary>Measures from one cell.</summary>
        public void Fill(int source)
        {
            Clear();
            AddSource(source);
            Spread();
        }

        public void Clear()
        {
            Array.Fill(_steps, Unreached);
            _queueStart = 0;
            _queueEnd = 0;
            Furthest = 0;
        }

        public void AddSource(int cell)
        {
            if (_steps[cell] == Unreached)
            {
                _steps[cell] = 0;
                _queue[_queueEnd++] = cell;
            }
        }

        /// <summary>Measures every cell from the sources added since the last clear.</summary>
        public void Spread()
        {
            while (_queueStart < _queueEnd)
            {
                int cell = _queue[_queueStart++];
                Furthest = _steps[cell];

                for (int direction = 0; direction < Maze.DirectionCount; direction++)
                {
                    int next = _maze.NeighbourThrough(cell, direction);

                    if (next != Maze.NoCell && _steps[next] == Unreached)
                    {
                        _steps[next] = _steps[cell] + 1;
                        _queue[_queueEnd++] = next;
                    }
                }
            }
        }

        public int StepsAt(int cell) => _steps[cell];

        /// <summary>Returns the next cell on the shortest walk to a source, or the same cell if already there.</summary>
        public int StepToward(int cell)
        {
            int best = cell;

            for (int direction = 0; direction < Maze.DirectionCount; direction++)
            {
                int next = _maze.NeighbourThrough(cell, direction);

                if (next != Maze.NoCell && _steps[next] < _steps[best])
                {
                    best = next;
                }
            }

            return best;
        }
    }
}
