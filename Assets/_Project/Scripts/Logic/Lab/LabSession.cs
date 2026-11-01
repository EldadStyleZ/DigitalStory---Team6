using System;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>One run through the lab's maze, and the only way the view changes it.</summary>
    public sealed class LabSession
    {
        public LabSession(LabRules rules)
        {
            _ = rules ?? throw new ArgumentNullException(nameof(rules));
            Maze = MazeGenerator.Generate(rules.Maze.Shape, new Mulberry32Random((uint)rules.Maze.Seed));
            Piper = new LabPiper(rules.Player, Maze);
        }

        public Maze Maze { get; }
        public LabPiper Piper { get; }

        /// <summary>Advances the run by one step.</summary>
        public void Tick(Vector2 move, float deltaTime) => Piper.Tick(move, deltaTime);
    }
}
