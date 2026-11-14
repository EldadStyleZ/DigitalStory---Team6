using System.Collections.Generic;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// A low poly bridge over a river. It knows the height of its deck, so walkers that follow the ground climb over it
    /// instead of wading through the water. Built by Tools/DigitalStory/Build Bridges Over Rivers.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/River Bridge")]
    public sealed class RiverBridge : MonoBehaviour
    {
        private static readonly List<RiverBridge> Bridges = new List<RiverBridge>();

        [SerializeField] private Vector3 _from;
        [SerializeField] private Vector3 _to;
        [SerializeField, Min(0f)] private float _rise = 1f;
        [SerializeField, Min(0.1f)] private float _halfWidth = 1.5f;

        public void Set(Vector3 from, Vector3 to, float rise, float halfWidth)
        {
            _from = from;
            _to = to;
            _rise = rise;
            _halfWidth = halfWidth;
        }

        /// <summary>The deck's top at t (0 at one end, 1 at the other), in world space.</summary>
        public static float DeckHeight(Vector3 from, Vector3 to, float rise, float t)
        {
            return Mathf.Lerp(from.y, to.y, t) + (rise * Mathf.Sin(Mathf.PI * t));
        }

        /// <summary>
        /// The ground under a point: the terrain, or a bridge's deck where one is higher. Falls back to the point's own
        /// height when there is no terrain.
        /// </summary>
        public static float GroundAt(Vector3 point)
        {
            float ground = point.y;
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                ground = terrain.SampleHeight(point) + terrain.transform.position.y;
            }

            for (int index = 0; index < Bridges.Count; index++)
            {
                if (Bridges[index].TryDeck(point, out float deck) && deck > ground)
                {
                    ground = deck;
                }
            }

            return ground;
        }

        // ---- Routing: beekeepers cross the rivers only on the bridges. ----

        private const string WaterObjectName = "Water (Crest)";
        private const float WetAbove = 0.05f;      // ground this close above the water counts as river
        private const float Cell = 1f;
        private const float DeckMargin = 0.6f;     // metres past each end that still count as on the bridge
        private const float ExitReach = 0.5f;      // this close to the exit, the walker heads for its target again
        private const float WetCost = 1000f;
        private static bool[] _wet;
        private static Vector2 _wetOrigin;
        private static int _wetSize;
        private static bool _wetBuilt;

        /// <summary>The next point to walk to: the target, or a bridge's end when a river lies between.</summary>
        public static Vector3 Route(Vector3 position, Vector3 target)
        {
            BuildWet();
            if (_wet == null || Bridges.Count == 0)
            {
                return target;
            }

            // On a bridge: walk to the end that leads on to the target, then straight on.
            for (int index = 0; index < Bridges.Count; index++)
            {
                RiverBridge bridge = Bridges[index];
                if (!bridge.OnDeck(position))
                {
                    continue;
                }

                Vector3 exit = Cost(bridge._from, target) < Cost(bridge._to, target) ? bridge._from : bridge._to;
                // Measured along the deck, so a walker that already stepped past the exit end never turns back to it.
                return bridge.LeftTo(position, exit) <= ExitReach ? target : exit;
            }

            if (IsDry(position, target))
            {
                return target;
            }

            // A river is in the way: the bridge whose near end is reachable on dry ground and that leads closest on.
            float best = float.MaxValue;
            Vector3 waypoint = target;
            for (int index = 0; index < Bridges.Count; index++)
            {
                RiverBridge bridge = Bridges[index];
                for (int flip = 0; flip < 2; flip++)
                {
                    Vector3 entry = flip == 0 ? bridge._from : bridge._to;
                    Vector3 exit = flip == 0 ? bridge._to : bridge._from;
                    if (!IsDry(position, entry))
                    {
                        continue;
                    }

                    float cost = FlatDistance(position, entry) + Cost(exit, target);
                    if (cost < best)
                    {
                        best = cost;
                        waypoint = entry;
                    }
                }
            }

            return waypoint;
        }

        /// <summary>How close a point is to a river: 1 at the water, fading to 0 at reach metres away.</summary>
        public static float Nearness(Vector3 point, float reach)
        {
            BuildWet();
            if (_wet == null)
            {
                return 0f;
            }

            int cx = Mathf.FloorToInt((point.x - _wetOrigin.x) / Cell);
            int cz = Mathf.FloorToInt((point.z - _wetOrigin.y) / Cell);
            int cells = Mathf.CeilToInt(reach / Cell);
            float nearestSqr = float.MaxValue;
            for (int z = Mathf.Max(0, cz - cells); z <= Mathf.Min(_wetSize - 1, cz + cells); z++)
            {
                for (int x = Mathf.Max(0, cx - cells); x <= Mathf.Min(_wetSize - 1, cx + cells); x++)
                {
                    if (_wet[(z * _wetSize) + x])
                    {
                        float dx = (x - cx) * Cell;
                        float dz = (z - cz) * Cell;
                        nearestSqr = Mathf.Min(nearestSqr, (dx * dx) + (dz * dz));
                    }
                }
            }

            return nearestSqr == float.MaxValue ? 0f : 1f - Mathf.Clamp01(Mathf.Sqrt(nearestSqr) / reach);
        }

        private static float Cost(Vector3 from, Vector3 target)
        {
            return FlatDistance(from, target) + (IsDry(from, target) ? 0f : WetCost);
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            return new Vector2(a.x - b.x, a.z - b.z).magnitude;
        }

        // Whether the straight way between two points stays off the rivers, apart from where it runs on a bridge.
        private static bool IsDry(Vector3 from, Vector3 to)
        {
            float length = FlatDistance(from, to);
            int steps = Mathf.Max(1, Mathf.CeilToInt(length / (Cell * 0.75f)));
            for (int step = 0; step <= steps; step++)
            {
                Vector3 point = Vector3.Lerp(from, to, (float)step / steps);
                if (IsWet(point) && !OnAnyDeck(point))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool OnAnyDeck(Vector3 point)
        {
            for (int index = 0; index < Bridges.Count; index++)
            {
                if (Bridges[index].OnDeck(point))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsWet(Vector3 point)
        {
            int x = Mathf.FloorToInt((point.x - _wetOrigin.x) / Cell);
            int z = Mathf.FloorToInt((point.z - _wetOrigin.y) / Cell);
            if (x < 0 || z < 0 || x >= _wetSize || z >= _wetSize)
            {
                return false;
            }

            return _wet[(z * _wetSize) + x];
        }

        // A coarse map of where the rivers are, made once from the terrain and the water's height.
        private static void BuildWet()
        {
            if (_wetBuilt)
            {
                return;
            }

            _wetBuilt = true;
            _wet = null;
            Terrain terrain = Terrain.activeTerrain;
            GameObject water = GameObject.Find(WaterObjectName);
            if (terrain == null || water == null)
            {
                return;
            }

            float wetBelow = water.transform.position.y + WetAbove;
            Vector3 corner = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            _wetOrigin = new Vector2(corner.x, corner.z);
            _wetSize = Mathf.CeilToInt(Mathf.Max(size.x, size.z) / Cell);
            _wet = new bool[_wetSize * _wetSize];
            for (int z = 0; z < _wetSize; z++)
            {
                for (int x = 0; x < _wetSize; x++)
                {
                    var point = new Vector3(corner.x + ((x + 0.5f) * Cell), 0f, corner.z + ((z + 0.5f) * Cell));
                    _wet[(z * _wetSize) + x] = terrain.SampleHeight(point) + corner.y < wetBelow;
                }
            }
        }

        // Metres still to walk along the deck to this end; below zero once past it.
        private float LeftTo(Vector3 point, Vector3 end)
        {
            Vector3 start = end == _to ? _from : _to;
            var along = new Vector2(end.x - start.x, end.z - start.z);
            float length = along.magnitude;
            if (length < 0.01f)
            {
                return 0f;
            }

            return length - Vector2.Dot(new Vector2(point.x - start.x, point.z - start.z), along / length);
        }

        private bool OnDeck(Vector3 point)
        {
            var along = new Vector2(_to.x - _from.x, _to.z - _from.z);
            float length = along.magnitude;
            if (length < 0.01f)
            {
                return false;
            }

            along /= length;
            var offset = new Vector2(point.x - _from.x, point.z - _from.z);
            float distance = Vector2.Dot(offset, along);
            float aside = Mathf.Abs((offset.x * along.y) - (offset.y * along.x));
            return distance >= -DeckMargin && distance <= length + DeckMargin && aside <= _halfWidth + 0.3f;
        }

        private bool TryDeck(Vector3 point, out float height)
        {
            height = 0f;
            var along = new Vector2(_to.x - _from.x, _to.z - _from.z);
            float length = along.magnitude;
            if (length < 0.01f)
            {
                return false;
            }

            along /= length;
            var offset = new Vector2(point.x - _from.x, point.z - _from.z);
            float distance = Vector2.Dot(offset, along);
            float aside = Mathf.Abs((offset.x * along.y) - (offset.y * along.x));
            if (distance < 0f || distance > length || aside > _halfWidth)
            {
                return false;
            }

            height = DeckHeight(_from, _to, _rise, distance / length);
            return true;
        }

        private void OnEnable()
        {
            Bridges.Add(this);
            _wetBuilt = false;
            BeekeeperBrain.Route = Route;
        }

        private void OnDisable()
        {
            Bridges.Remove(this);
            if (Bridges.Count == 0)
            {
                BeekeeperBrain.Route = null;
            }
        }
    }
}
