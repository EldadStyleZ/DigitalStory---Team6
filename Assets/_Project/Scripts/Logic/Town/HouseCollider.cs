using System;
using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Keeps circles out of houses.</summary>
    public sealed class HouseCollider
    {
        // Below this squared distance the circle's centre counts as inside the house.
        private const float InsideDistanceSquared = 1e-6f;

        private readonly House[] _houses;

        public HouseCollider(IReadOnlyList<House> houses)
        {
            if (houses == null)
            {
                throw new ArgumentNullException(nameof(houses));
            }

            _houses = new House[houses.Count];

            for (int index = 0; index < houses.Count; index++)
            {
                _houses[index] = houses[index];
            }
        }

        /// <summary>Moves a circle out of any house it overlaps.</summary>
        public void PushOut(ref Vector2 position, float radius)
        {
            Vector2 noVelocity = Vector2.zero;
            PushOut(ref position, ref noVelocity, radius);
        }

        /// <summary>Moves a circle out of any house it overlaps. A circle touching a wall also stops moving into it.</summary>
        public void PushOut(ref Vector2 position, ref Vector2 velocity, float radius)
        {
            for (int index = 0; index < _houses.Length; index++)
            {
                ref readonly House house = ref _houses[index];

                if (IsClear(in house, position, radius))
                {
                    continue;
                }

                var nearestPoint = new Vector2(
                    Mathf.Clamp(position.x, house.X, house.Right),
                    Mathf.Clamp(position.y, house.Y, house.Bottom));
                Vector2 fromWall = position - nearestPoint;

                if (fromWall.sqrMagnitude > InsideDistanceSquared)
                {
                    SlideOffWall(ref position, ref velocity, fromWall, radius);
                }
                else
                {
                    // Like the prototype, this keeps the velocity, so tuning matches the original.
                    LeaveByNearestSide(ref position, in house, radius);
                }
            }
        }

        private static bool IsClear(in House house, Vector2 position, float radius)
        {
            return position.x + radius < house.X
                || position.x - radius > house.Right
                || position.y + radius < house.Y
                || position.y - radius > house.Bottom;
        }

        private static void SlideOffWall(ref Vector2 position, ref Vector2 velocity, Vector2 fromWall, float radius)
        {
            float distance = fromWall.magnitude;

            if (distance >= radius)
            {
                return;
            }

            Vector2 outward = fromWall / distance;
            position += outward * (radius - distance);

            float speedOutward = Vector2.Dot(velocity, outward);

            if (speedOutward < 0f)
            {
                velocity -= outward * speedOutward;
            }
        }

        private static void LeaveByNearestSide(ref Vector2 position, in House house, float radius)
        {
            float toLeft = position.x - house.X;
            float toRight = house.Right - position.x;
            float toTop = position.y - house.Y;
            float toBottom = house.Bottom - position.y;

            // Two-argument Min, because the four-argument version allocates an array.
            float nearest = Mathf.Min(Mathf.Min(toLeft, toRight), Mathf.Min(toTop, toBottom));

            if (nearest == toLeft)
            {
                position.x = house.X - radius;
            }
            else if (nearest == toRight)
            {
                position.x = house.Right + radius;
            }
            else if (nearest == toTop)
            {
                position.y = house.Y - radius;
            }
            else
            {
                position.y = house.Bottom + radius;
            }
        }
    }
}
