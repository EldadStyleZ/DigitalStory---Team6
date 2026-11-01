using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>The Piper in the lab: walks the maze and stops at its walls.</summary>
    public sealed class LabPiper
    {
        private readonly LabPlayerConfig _config;
        private readonly Maze _maze;

        public LabPiper(LabPlayerConfig config, Maze maze)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _maze = maze ?? throw new ArgumentNullException(nameof(maze));
            Position = maze.CenterOf(maze.Entrance);
        }

        public Vector2 Position { get; private set; }

        /// <summary>The way the Piper faces, in degrees, with east as zero.</summary>
        public float Facing { get; private set; }

        public void Tick(Vector2 move, float deltaTime)
        {
            if (move == Vector2.zero)
            {
                return;
            }

            Vector2 heading = move.normalized;
            Facing = Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg;
            Position = _maze.Move(Position, heading * (_config.MoveSpeed * deltaTime), _config.Radius);
        }

        /// <summary>Puts the Piper at the middle of a cell.</summary>
        public void MoveTo(int cell) => Position = _maze.CenterOf(cell);
    }
}
