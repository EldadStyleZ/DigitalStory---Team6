using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>One cat's state.</summary>
    /// <remarks>A mutable struct with public fields, for the same reason as <see cref="Rat"/>.</remarks>
    public struct Cat
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 Target;
        public int Column;
        public int Row;
        public float FleeLeft;
        public float EatLeft;
        public float SleepLeft;
        public float Phase;
        public bool IsHunting;

        /// <param name="start">The street junction the cat starts on.</param>
        /// <param name="phase">An offset that stops cats animating in step.</param>
        public Cat(Junction start, float phase)
        {
            Position = start.Position;
            Velocity = Vector2.zero;
            Target = start.Position;
            Column = start.Column;
            Row = start.Row;
            FleeLeft = 0f;
            EatLeft = 0f;
            SleepLeft = 0f;
            Phase = phase;
            IsHunting = false;
        }

        public readonly bool IsAsleep => SleepLeft > 0f;
    }
}
