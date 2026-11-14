using System;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>The core's recent path, resampled to one point every fixed distance, so the river can flow along it.</summary>
    public sealed class PathTrail
    {
        private readonly int _capacity;
        private readonly float _spacing;
        private readonly Vector3[] _points;

        private int _newest;
        private int _count;
        private Vector3 _cursor;
        private float _carry;
        private double _travelled;

        public PathTrail(int capacity, float spacing)
        {
            if (capacity < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "A trail needs room for at least two points.");
            }

            if (!(spacing > 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(spacing), spacing, "The spacing must be above zero.");
            }

            _capacity = capacity;
            _spacing = spacing;
            _points = new Vector3[capacity];
        }

        /// <summary>How many points are stored.</summary>
        public int Count => _count;

        /// <summary>Metres along the path from the newest point to the oldest.</summary>
        public float Length => _count > 0 ? (_count - 1) * _spacing : 0f;

        /// <summary>Metres along the path from the newest point to the last position pushed: the core runs ahead of it.</summary>
        public float Lead => _carry;

        /// <summary>Metres travelled since the last <see cref="Reset"/>, for every push: the core's odometer.</summary>
        public double Travelled => _travelled;

        /// <summary>Forgets the path and starts it at <paramref name="position"/>.</summary>
        public void Reset(Vector3 position)
        {
            _newest = 0;
            _count = 1;
            _points[0] = position;
            _cursor = position;
            _carry = 0f;
            _travelled = 0.0;
        }

        /// <summary>Stores a point each spacing metres of travel (several if the step was long) and drops the oldest when full.</summary>
        public void Push(Vector3 position)
        {
            if (_count == 0)
            {
                Reset(position);
                return;
            }

            Vector3 delta = position - _cursor;
            float distance = delta.magnitude;
            if (distance <= 0f)
            {
                return;
            }

            _travelled += distance;
            Vector3 direction = delta / distance;
            float along = _spacing - _carry;
            while (along <= distance)
            {
                Store(_cursor + direction * along);
                along += _spacing;
            }

            _carry = distance + _spacing - along;
            if (_carry < 0f)
            {
                _carry = 0f;
            }

            _cursor = position;
        }

        /// <summary>The point at <paramref name="lane"/>: 0 is the newest, 1 the oldest. Lanes outside 0..1 wrap.</summary>
        public Vector3 PointAt(float lane)
        {
            if (_count == 0)
            {
                return Vector3.zero;
            }

            if (lane < 0f || lane > 1f)
            {
                lane -= Mathf.Floor(lane);
            }

            float index = lane * (_count - 1);
            int lower = (int)index;
            if (lower >= _count - 1)
            {
                return _points[IndexFromNewest(_count - 1)];
            }

            return Vector3.Lerp(_points[IndexFromNewest(lower)], _points[IndexFromNewest(lower + 1)], index - lower);
        }

        /// <summary>Writes the points newest first (xyz is the position, w the metres from the newest point) and returns how many. Does not allocate.</summary>
        public int CopyTo(Vector4[] target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            int written = Mathf.Min(_count, target.Length);
            for (int i = 0; i < written; i++)
            {
                Vector3 point = _points[IndexFromNewest(i)];
                target[i] = new Vector4(point.x, point.y, point.z, i * _spacing);
            }

            return written;
        }

        private void Store(Vector3 point)
        {
            _newest = (_newest + 1) % _capacity;
            _points[_newest] = point;
            if (_count < _capacity)
            {
                _count++;
            }
        }

        private int IndexFromNewest(int steps)
        {
            return (_newest - steps + _capacity) % _capacity;
        }
    }
}
