using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>Builds a maze by carving a long winding walk, backing up at each dead end.</summary>
    public static class MazeGenerator
    {
        private const int NoChoice = -1;

        public static Maze Generate(MazeShape shape, IRandomSource random)
        {
            var openEast = new bool[shape.CellCount];
            var openNorth = new bool[shape.CellCount];

            Carve(shape, random, openEast, openNorth);
            OpenLoops(shape, random, openEast, openNorth);
            return new Maze(shape, openEast, openNorth);
        }

        // Uses its own stack, because a big maze would overflow the call stack if this called itself.
        private static void Carve(MazeShape shape, IRandomSource random, bool[] openEast, bool[] openNorth)
        {
            var visited = new bool[shape.CellCount];
            var trail = new int[shape.CellCount];
            var choices = new int[Maze.DirectionCount];
            int trailLength = 1;
            visited[0] = true;

            while (trailLength > 0)
            {
                int cell = trail[trailLength - 1];
                int next = PickUnvisited(shape, cell, visited, choices, random);

                if (next == NoChoice)
                {
                    trailLength--;
                    continue;
                }

                Open(shape, cell, next, openEast, openNorth);
                visited[next] = true;
                trail[trailLength] = next;
                trailLength++;
            }
        }

        private static int PickUnvisited(MazeShape shape, int cell, bool[] visited, int[] choices, IRandomSource random)
        {
            int count = 0;
            int column = cell % shape.Columns;

            if (column < shape.Columns - 1 && !visited[cell + 1])
            {
                choices[count++] = cell + 1;
            }

            if (cell + shape.Columns < shape.CellCount && !visited[cell + shape.Columns])
            {
                choices[count++] = cell + shape.Columns;
            }

            if (column > 0 && !visited[cell - 1])
            {
                choices[count++] = cell - 1;
            }

            if (cell >= shape.Columns && !visited[cell - shape.Columns])
            {
                choices[count++] = cell - shape.Columns;
            }

            return count == 0 ? NoChoice : choices[(int)(random.NextDouble() * count)];
        }

        private static void Open(MazeShape shape, int from, int to, bool[] openEast, bool[] openNorth)
        {
            int lower = from < to ? from : to;
            bool isSideways = to - from == 1 || from - to == 1;

            if (isSideways)
            {
                openEast[lower] = true;
            }
            else
            {
                openNorth[lower] = true;
            }
        }

        private static void OpenLoops(MazeShape shape, IRandomSource random, bool[] openEast, bool[] openNorth)
        {
            for (int cell = 0; cell < shape.CellCount; cell++)
            {
                if (cell % shape.Columns < shape.Columns - 1 && !openEast[cell] && random.NextDouble() < shape.LoopChance)
                {
                    openEast[cell] = true;
                }

                if (cell + shape.Columns < shape.CellCount && !openNorth[cell] && random.NextDouble() < shape.LoopChance)
                {
                    openNorth[cell] = true;
                }
            }
        }
    }
}
