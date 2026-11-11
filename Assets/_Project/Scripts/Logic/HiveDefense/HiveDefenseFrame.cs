using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>What the View tells the session each tick: where the Piper stands and faces, what the player aims at and presses.</summary>
    public readonly struct HiveDefenseFrame
    {
        /// <summary>
        /// Makes a frame with the mouse idle, so auto-aim chooses what an aimed song strikes.
        /// <paramref name="move"/> is x and z on the ground and steers the giant.
        /// </summary>
        public HiveDefenseFrame(
            Vector3 piperPosition,
            Vector3 piperFacing,
            Vector3 aimPoint,
            Vector2 move,
            int pressedHoleMask,
            bool punch,
            bool kick)
            : this(piperPosition, piperFacing, aimPoint, move, pressedHoleMask, punch, kick, false)
        {
        }

        /// <summary>
        /// Makes a frame. <paramref name="move"/> is x and z on the ground and steers the giant. While
        /// <paramref name="isMouseLive"/> is true the mouse aims: aimed songs strike at <paramref name="aimPoint"/>.
        /// </summary>
        public HiveDefenseFrame(
            Vector3 piperPosition,
            Vector3 piperFacing,
            Vector3 aimPoint,
            Vector2 move,
            int pressedHoleMask,
            bool punch,
            bool kick,
            bool isMouseLive)
        {
            PiperPosition = piperPosition;
            PiperFacing = piperFacing;
            AimPoint = aimPoint;
            Move = move;
            PressedHoleMask = pressedHoleMask;
            Punch = punch;
            Kick = kick;
            IsMouseLive = isMouseLive;
        }

        /// <summary>Where the Piper stands.</summary>
        public Vector3 PiperPosition { get; }

        /// <summary>
        /// Where the Piper faces. Only its direction on the ground counts, and a zero one keeps the last. An aimed song with
        /// nothing to strike strikes the ground ahead of it.
        /// </summary>
        public Vector3 PiperFacing { get; }

        /// <summary>The point on the ground under the cursor. Only read while <see cref="IsMouseLive"/>.</summary>
        public Vector3 AimPoint { get; }

        /// <summary>True while the mouse aims (it moved a moment ago or a button is held); false while auto-aim chooses.</summary>
        public bool IsMouseLive { get; }

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
