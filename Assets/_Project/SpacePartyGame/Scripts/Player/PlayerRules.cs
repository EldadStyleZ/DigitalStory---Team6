using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>Decides which way a player faces: right is 1, left is -1.</summary>
    public static class FacingRule
    {
        /// <summary>Sideways input smaller than this keeps the current facing, so a slightly tilted stick does not
        /// flip the player.</summary>
        public const float Deadzone = 0.1f;

        /// <summary>The facing after this frame's sideways input. With no clear sideways input the player keeps
        /// facing the way it last moved, and a player that has never moved faces right, as the art is drawn.</summary>
        public static int Next(int current, float moveX)
        {
            if (moveX >= Deadzone)
            {
                return 1;
            }

            if (moveX <= -Deadzone)
            {
                return -1;
            }

            return current < 0 ? -1 : 1;
        }
    }

    /// <summary>Turns move input into a velocity.</summary>
    public static class PlayerMotion
    {
        /// <summary>The velocity for a move input. Keys held diagonally are no faster than straight, and a half-tilted
        /// stick gives half speed.</summary>
        public static Vector2 VelocityFor(Vector2 move, float speed) => Vector2.ClampMagnitude(move, 1f) * speed;
    }
}
