using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.GrassBrush
{
    /// <summary>
    /// The grass clumps painted with the Grass Brush, kept in an asset so painting does not bloat the scene file.
    /// Each clump has a world position, a turn around the up axis and a size.
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/Grass Data", fileName = "GrassData")]
    public sealed class GrassData : ScriptableObject
    {
        [SerializeField] private List<Vector3> _positions = new List<Vector3>();
        [SerializeField] private List<float> _turns = new List<float>();
        [SerializeField] private List<float> _sizes = new List<float>();

        // Where the brush erased grass from a field that covers the whole terrain: one byte per square, 1 for erased.
        [SerializeField, HideInInspector] private byte[] _coverMask = new byte[0];
        [SerializeField, HideInInspector] private int _maskSize;
        [SerializeField, HideInInspector] private float _maskCell;
        [SerializeField, HideInInspector] private Vector2 _maskOrigin;

        public int Count => _positions.Count;

        /// <summary>Changes every time the clumps change, so GrassField knows to rebuild its draw data.</summary>
        public int Version { get; private set; }

        /// <summary>Changes when the erase mask is replaced as a whole (undo, redo, clearing), so the covering grass regrows.</summary>
        public int MaskVersion { get; private set; }

        public Vector3 GetPosition(int index) => _positions[index];

        public float GetTurn(int index) => _turns[index];

        public float GetSize(int index) => _sizes[index];

        /// <summary>Moves a clump without telling GrassField. Call <see cref="MarkChanged"/> after a batch of moves.</summary>
        public void SetPositionSilently(int index, Vector3 position)
        {
            _positions[index] = position;
        }

        public void MarkChanged()
        {
            Version++;
        }

        public void Add(Vector3 position, float turn, float size)
        {
            _positions.Add(position);
            _turns.Add(turn);
            _sizes.Add(size);
            Version++;
        }

        /// <summary>Removes every clump within the radius of the centre, measured flat on the ground.</summary>
        public int RemoveInRadius(Vector3 centre, float radius)
        {
            var radiusSquared = radius * radius;
            var removed = 0;
            for (var i = _positions.Count - 1; i >= 0; i--)
            {
                var offset = _positions[i] - centre;
                offset.y = 0f;
                if (offset.sqrMagnitude > radiusSquared)
                {
                    continue;
                }

                var last = _positions.Count - 1;
                _positions[i] = _positions[last];
                _turns[i] = _turns[last];
                _sizes[i] = _sizes[last];
                _positions.RemoveAt(last);
                _turns.RemoveAt(last);
                _sizes.RemoveAt(last);
                removed++;
            }

            if (removed > 0)
            {
                Version++;
            }

            return removed;
        }

        /// <summary>Removes every clump the test picks. Returns how many went.</summary>
        public int RemoveWhere(System.Func<Vector3, bool> shouldRemove)
        {
            var removed = 0;
            for (var i = _positions.Count - 1; i >= 0; i--)
            {
                if (!shouldRemove(_positions[i]))
                {
                    continue;
                }

                var last = _positions.Count - 1;
                _positions[i] = _positions[last];
                _turns[i] = _turns[last];
                _sizes[i] = _sizes[last];
                _positions.RemoveAt(last);
                _turns.RemoveAt(last);
                _sizes.RemoveAt(last);
                removed++;
            }

            if (removed > 0)
            {
                Version++;
            }

            return removed;
        }

        /// <summary>Counts the clumps within the radius of the centre, measured flat on the ground.</summary>
        public int CountInRadius(Vector3 centre, float radius)
        {
            var radiusSquared = radius * radius;
            var count = 0;
            foreach (var position in _positions)
            {
                var offset = position - centre;
                offset.y = 0f;
                if (offset.sqrMagnitude <= radiusSquared)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Makes sure the erase mask covers the square of <paramref name="halfSize"/> round <paramref name="centre"/> in
        /// squares of <paramref name="cell"/> metres. A mask of another layout starts again, with nothing erased.
        /// </summary>
        public void EnsureMask(Vector2 centre, float halfSize, float cell)
        {
            var size = Mathf.Max(1, Mathf.CeilToInt(halfSize * 2f / cell));
            var origin = centre - (Vector2.one * halfSize);
            if (_maskSize == size && Mathf.Approximately(_maskCell, cell) && _maskOrigin == origin && _coverMask.Length == size * size)
            {
                return;
            }

            _maskSize = size;
            _maskCell = cell;
            _maskOrigin = origin;
            _coverMask = new byte[size * size];
        }

        /// <summary>True where the brush erased the covering grass.</summary>
        public bool IsErased(float x, float z)
        {
            if (_maskSize == 0 || _coverMask.Length == 0)
            {
                return false;
            }

            var ix = Mathf.FloorToInt((x - _maskOrigin.x) / _maskCell);
            var iz = Mathf.FloorToInt((z - _maskOrigin.y) / _maskCell);
            if (ix < 0 || iz < 0 || ix >= _maskSize || iz >= _maskSize)
            {
                return false;
            }

            return _coverMask[(iz * _maskSize) + ix] != 0;
        }

        /// <summary>
        /// Erases or brings back the covering grass in a circle. <paramref name="weight"/> gives, for a distance from 0 at
        /// the centre to 1 at the rim, the chance a square changes, so a soft brush leaves a soft edge.
        /// </summary>
        public void SetErased(Vector2 centre, float radius, bool erased, System.Func<float, float> weight)
        {
            if (_maskSize == 0)
            {
                return;
            }

            var minX = Mathf.Max(0, Mathf.FloorToInt((centre.x - radius - _maskOrigin.x) / _maskCell));
            var maxX = Mathf.Min(_maskSize - 1, Mathf.FloorToInt((centre.x + radius - _maskOrigin.x) / _maskCell));
            var minZ = Mathf.Max(0, Mathf.FloorToInt((centre.y - radius - _maskOrigin.y) / _maskCell));
            var maxZ = Mathf.Min(_maskSize - 1, Mathf.FloorToInt((centre.y + radius - _maskOrigin.y) / _maskCell));
            var value = erased ? (byte)1 : (byte)0;
            for (var iz = minZ; iz <= maxZ; iz++)
            {
                for (var ix = minX; ix <= maxX; ix++)
                {
                    var middle = _maskOrigin + new Vector2((ix + 0.5f) * _maskCell, (iz + 0.5f) * _maskCell);
                    var distance01 = Vector2.Distance(middle, centre) / Mathf.Max(0.0001f, radius);
                    if (distance01 > 1f || Random.value > weight(distance01))
                    {
                        continue;
                    }

                    _coverMask[(iz * _maskSize) + ix] = value;
                }
            }
        }

        public void ClearMask()
        {
            if (_coverMask.Length > 0)
            {
                System.Array.Clear(_coverMask, 0, _coverMask.Length);
                MaskVersion++;
            }
        }

        public void Clear()
        {
            _positions.Clear();
            _turns.Clear();
            _sizes.Clear();
            Version++;
        }

        private void OnValidate()
        {
            // Undo and redo replace the lists and the mask, so the draw data must be rebuilt.
            Version++;
            MaskVersion++;
        }
    }
}
