using System;
using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>Every config the lab's rules read.</summary>
    public sealed class LabRules
    {
        public LabRules(MazeConfig maze, LabPlayerConfig player)
        {
            Maze = maze != null ? maze : throw new ArgumentNullException(nameof(maze));
            Player = player != null ? player : throw new ArgumentNullException(nameof(player));
        }

        public MazeConfig Maze { get; }
        public LabPlayerConfig Player { get; }
    }
}
