using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>What the player wants the Piper to do this step.</summary>
    public readonly struct PiperIntent
    {
        /// <param name="move">The direction to walk, in town space.</param>
        /// <param name="heldHoles">One bit per flute hole that is held down.</param>
        /// <param name="pressedHoles">One bit per flute hole pressed this step.</param>
        public PiperIntent(Vector2 move, int heldHoles, int pressedHoles)
        {
            Move = move;
            HeldHoles = heldHoles;
            PressedHoles = pressedHoles;
        }

        public Vector2 Move { get; }
        public int HeldHoles { get; }
        public int PressedHoles { get; }
    }
}
