using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// A flat area enclosed by a loop of points, such as the playable floor of the map. It answers whether a point
    /// is inside and gives the box round it. It is a plain C# class, so tests can check it without a scene.
    /// </summary>
    public sealed class PolygonArea
    {
        private readonly Vector2[] _points;

        /// <param name="points">The loop, in order. A last point that repeats the first, as a closed edge has, is
        /// ignored.</param>
        public PolygonArea(IReadOnlyList<Vector2> points)
        {
            int count = points?.Count ?? 0;

            if (count > 1 && points[count - 1] == points[0])
            {
                count--;
            }

            if (count < 3)
            {
                throw new ArgumentException("An area needs at least three different points.", nameof(points));
            }

            _points = new Vector2[count];
            Vector2 min = points[0];
            Vector2 max = points[0];

            for (int i = 0; i < count; i++)
            {
                _points[i] = points[i];
                min = Vector2.Min(min, points[i]);
                max = Vector2.Max(max, points[i]);
            }

            Bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        /// <summary>The smallest box that holds every point of the loop.</summary>
        public Rect Bounds { get; }

        /// <summary>Whether a point is inside the loop.</summary>
        public bool Contains(Vector2 point)
        {
            if (!Bounds.Contains(point))
            {
                return false;
            }

            // Count how many edges a line running right from the point crosses: an odd number means inside.
            bool inside = false;

            for (int i = 0, previous = _points.Length - 1; i < _points.Length; previous = i++)
            {
                Vector2 a = _points[i];
                Vector2 b = _points[previous];

                if ((a.y > point.y) != (b.y > point.y))
                {
                    float crossingX = a.x + ((point.y - a.y) / (b.y - a.y) * (b.x - a.x));

                    if (point.x < crossingX)
                    {
                        inside = !inside;
                    }
                }
            }

            return inside;
        }
    }
}
