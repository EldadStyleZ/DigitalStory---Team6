using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>What the View tells the session each tick: where the Piper stands, what the player aims at and presses.</summary>
    public readonly struct HiveDefenseFrame
    {
        /// <summary>Makes a frame. <paramref name="move"/> is x and z on the ground and steers the giant.</summary>
        public HiveDefenseFrame(Vector3 piperPosition, Vector3 aimPoint, Vector2 move, int pressedHoleMask, bool punch, bool kick)
        {
            PiperPosition = piperPosition;
            AimPoint = aimPoint;
            Move = move;
            PressedHoleMask = pressedHoleMask;
            Punch = punch;
            Kick = kick;
        }

        /// <summary>Where the Piper stands.</summary>
        public Vector3 PiperPosition { get; }

        /// <summary>The point on the ground under the cursor.</summary>
        public Vector3 AimPoint { get; }

        /// <summary>The steering input for the giant: x and z on the ground, length 0 to 1.</summary>
        public Vector2 Move { get; }

        /// <summary>Bit n is set when hole n was pressed this tick. See <see cref="HoleBit"/>.</summary>
        public int PressedHoleMask { get; }

        /// <summary>True when the punch was pressed this tick.</summary>
        public bool Punch { get; }

        /// <summary>True when the kick was pressed this tick.</summary>
        public bool Kick { get; }

        /// <summary>The bit for a hole, counting from 0.</summary>
        public static int HoleBit(int hole) => 1 << hole;
    }
}
