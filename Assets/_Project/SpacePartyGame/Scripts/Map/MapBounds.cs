using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// Tells cameras and spawners where the playable floor is: the box round it, and whether a point is on it. It
    /// reads the floor from the map's border loop, so the collider and this answer can never disagree.
    /// </summary>
    /// <remarks>The map does not move, so the floor is worked out once, in world space, the first time it is asked
    /// for.</remarks>
    [DisallowMultipleComponent]
    public sealed class MapBounds : MonoBehaviour
    {
        [SerializeField, Tooltip("The closed edge round the playable floor, on the MapBorder layer.")]
        private EdgeCollider2D _border;

        private PolygonArea _area;

        /// <summary>The box round the playable floor, in world space.</summary>
        public Rect Rect => Area.Bounds;

        private PolygonArea Area => _area ??= BuildArea();

        /// <summary>Whether a world point is on the playable floor.</summary>
        public bool Contains(Vector2 worldPoint) => Area.Contains(worldPoint);

        private void Awake()
        {
            if (_border == null)
            {
                Debug.LogError($"{name}: MapBounds has no border edge.", this);
            }
        }

        private PolygonArea BuildArea()
        {
            Vector2[] local = _border.points;
            var world = new Vector2[local.Length];

            for (int i = 0; i < local.Length; i++)
            {
                world[i] = _border.transform.TransformPoint(local[i] + _border.offset);
            }

            return new PolygonArea(world);
        }

        private void OnDrawGizmosSelected()
        {
            if (_border == null || _border.pointCount < 3)
            {
                return;
            }

            Rect rect = BuildArea().Bounds;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(rect.center, rect.size);
        }
    }
}
