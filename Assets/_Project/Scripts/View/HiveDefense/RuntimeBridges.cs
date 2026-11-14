using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// Builds low poly bridges at the start of a match, wherever a beekeeper's way in (from each spawn point to its own
    /// hive) crosses a river, so the bridges follow the random map. The bridges built before are cleared first.
    /// </summary>
    public static class RuntimeBridges
    {
        private const string ParentName = "Bridges";
        private const string WaterObjectName = "Water (Crest)";
        private const float Step = 0.25f;
        private const float WetAbove = 0.3f;     // ground this close to the water counts as river
        private const float LandOnBank = 2f;     // metres each end reaches onto the bank
        private const float HalfWidth = 1.6f;
        private const float RisePerMetre = 0.12f;
        private const float MaxRise = 1.6f;

        private static readonly Color Wood = new Color(0.55f, 0.36f, 0.2f);
        private static readonly Color DarkWood = new Color(0.32f, 0.2f, 0.11f);

        private static Material[] s_materials;

        /// <summary>Clears the old bridges and builds new ones for these spawn points and hives.</summary>
        public static void Build(IReadOnlyList<Vector3> spawns, IReadOnlyList<Vector3> hives)
        {
            Terrain terrain = Terrain.activeTerrain;
            GameObject water = GameObject.Find(WaterObjectName);
            if (terrain == null || water == null || spawns == null || hives == null || hives.Count == 0)
            {
                return;
            }

            GameObject root = GameObject.Find(ParentName) ?? new GameObject(ParentName);
            for (int index = root.transform.childCount - 1; index >= 0; index--)
            {
                GameObject old = root.transform.GetChild(index).gameObject;
                old.SetActive(false);
                Object.Destroy(old);
            }

            if (s_materials == null)
            {
                s_materials = new[] { Plain(Wood), Plain(DarkWood) };
            }

            float waterLevel = water.transform.position.y;
            int built = 0;
            for (int index = 0; index < spawns.Count; index++)
            {
                Vector3 hive = hives[index % hives.Count];
                foreach ((Vector3 from, Vector3 to) in Crossings(terrain, spawns[index], hive, waterLevel))
                {
                    MakeBridge(root.transform, terrain, from, to, waterLevel, s_materials, built++);
                }
            }
        }

        private static Material Plain(Color color)
        {
            var material = new Material(Shader.Find("Standard")) { color = color, name = "Bridge" };
            material.SetFloat("_Glossiness", 0.08f);
            return material;
        }

        private static float Height(Terrain terrain, Vector3 point)
        {
            return terrain.SampleHeight(point) + terrain.transform.position.y;
        }

        // Walks the straight way in and returns each wet stretch, widened onto both banks.
        private static IEnumerable<(Vector3, Vector3)> Crossings(Terrain terrain, Vector3 spawn, Vector3 target, float waterLevel)
        {
            spawn.y = 0f;
            target.y = 0f;
            Vector3 direction = (target - spawn).normalized;
            float length = Vector3.Distance(spawn, target);
            float wetStart = -1f;
            for (float distance = 0f; distance <= length; distance += Step)
            {
                bool wet = Height(terrain, spawn + (direction * distance)) < waterLevel + WetAbove;
                if (wet && wetStart < 0f)
                {
                    wetStart = distance;
                }
                else if (!wet && wetStart >= 0f)
                {
                    Vector3 from = spawn + (direction * Mathf.Max(0f, wetStart - LandOnBank));
                    Vector3 to = spawn + (direction * Mathf.Min(length, distance + LandOnBank));
                    from.y = Height(terrain, from);
                    to.y = Height(terrain, to);
                    wetStart = -1f;
                    yield return (from, to);
                }
            }
        }

        private static void MakeBridge(Transform parent, Terrain terrain, Vector3 from, Vector3 to, float waterLevel, Material[] materials, int number)
        {
            float length = Vector3.Distance(new Vector3(from.x, 0f, from.z), new Vector3(to.x, 0f, to.z));
            float rise = Mathf.Min(MaxRise, length * RisePerMetre);
            Vector3 forward = new Vector3(to.x - from.x, 0f, to.z - from.z).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            var random = new System.Random(number * 7919 + 13);

            var mesh = new LowPolyMesh();
            Vector3 origin = from;
            Vector3 Local(Vector3 world) => new Vector3(Vector3.Dot(world - origin, right), world.y - origin.y, Vector3.Dot(world - origin, forward));
            Vector3 Deck(float t, float side) => from + (forward * (length * t)) + (right * side) + (Vector3.up * (RiverBridge.DeckHeight(from, to, rise, t) - from.y));

            // Planks, each a little uneven, with small gaps.
            const float PlankLength = 0.45f;
            int planks = Mathf.Max(2, Mathf.RoundToInt(length / PlankLength));
            for (int index = 0; index < planks; index++)
            {
                float t = (index + 0.5f) / planks;
                float jitter = (float)(random.NextDouble() - 0.5);
                Vector3 a = Local(Deck(t - (0.45f / planks), 0f));
                Vector3 b = Local(Deck(t + (0.45f / planks), 0f));
                Vector3 centre = ((a + b) * 0.5f) + (Vector3.down * 0.06f) + (Vector3.right * (jitter * 0.12f));
                mesh.Beam(centre, b - a, HalfWidth + (jitter * 0.1f), 0.06f, 0, jitter * 4f);
            }

            // Two stringers under the deck's edges, and the railings.
            const int Pieces = 8;
            const float PostEvery = 1.6f;
            for (int sideIndex = -1; sideIndex <= 1; sideIndex += 2)
            {
                float side = sideIndex * (HalfWidth - 0.15f);
                for (int piece = 0; piece < Pieces; piece++)
                {
                    Vector3 a = Local(Deck((float)piece / Pieces, side)) + (Vector3.down * 0.2f);
                    Vector3 b = Local(Deck((float)(piece + 1) / Pieces, side)) + (Vector3.down * 0.2f);
                    mesh.Beam((a + b) * 0.5f, b - a, 0.1f, 0.1f, 1, 0f);
                }

                int posts = Mathf.Max(2, Mathf.RoundToInt(length / PostEvery) + 1);
                Vector3 previousTop = Vector3.zero;
                for (int post = 0; post < posts; post++)
                {
                    float t = (float)post / (posts - 1);
                    Vector3 foot = Local(Deck(t, side));
                    float lean = (float)(random.NextDouble() - 0.5) * 0.06f;
                    Vector3 top = foot + new Vector3(lean, 0.95f, 0f);
                    mesh.Beam((foot + top) * 0.5f, top - foot, 0.08f, 0.08f, 1, 0f);
                    if (post > 0)
                    {
                        Vector3 railA = previousTop + (Vector3.down * 0.05f);
                        Vector3 railB = top + (Vector3.down * 0.05f);
                        mesh.Beam((railA + railB) * 0.5f, railB - railA, 0.06f, 0.05f, 0, 0f);
                        Vector3 midA = previousTop + (Vector3.down * 0.5f);
                        Vector3 midB = top + (Vector3.down * 0.5f);
                        mesh.Beam((midA + midB) * 0.5f, midB - midA, 0.04f, 0.04f, 1, 0f);
                    }

                    previousTop = top;
                }

                // Piles down into the river bed, at the quarter points.
                for (int pile = 1; pile <= 3; pile++)
                {
                    float t = pile / 4f;
                    Vector3 deck = Deck(t, side);
                    float bed = Mathf.Min(Height(terrain, deck), waterLevel) - 0.4f;
                    Vector3 top = Local(deck) + (Vector3.down * 0.2f);
                    Vector3 bottom = new Vector3(top.x, bed - origin.y, top.z);
                    mesh.Beam((top + bottom) * 0.5f, top - bottom, 0.12f, 0.12f, 1, 0f);
                }
            }

            Mesh built = mesh.Build($"Bridge {number + 1}");
            var bridge = new GameObject($"Bridge {number + 1}");
            bridge.transform.SetParent(parent, false);
            bridge.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(forward, Vector3.up));
            bridge.AddComponent<MeshFilter>().sharedMesh = built;
            bridge.AddComponent<MeshRenderer>().sharedMaterials = materials;
            bridge.AddComponent<MeshCollider>().sharedMesh = built;
            bridge.AddComponent<RiverBridge>().Set(from, to, rise, HalfWidth);
        }

        /// <summary>Boxes with their own corners, so every face is flat shaded.</summary>
        private sealed class LowPolyMesh
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector3> _normals = new List<Vector3>();
            private readonly List<int>[] _triangles = { new List<int>(), new List<int>() };

            /// <summary>A box along span (its full length), halfWidth across and halfHeight up, twisted a little.</summary>
            public void Beam(Vector3 centre, Vector3 span, float halfWidth, float halfHeight, int submesh, float twistDegrees)
            {
                Vector3 along = span.normalized;
                Vector3 side = Vector3.Cross(Vector3.up, along);
                if (side.sqrMagnitude < 0.001f)
                {
                    side = Vector3.right;
                }

                side.Normalize();
                Vector3 up = Vector3.Cross(along, side).normalized;
                Quaternion twist = Quaternion.AngleAxis(twistDegrees, along);
                side = twist * side * halfWidth;
                up = twist * up * halfHeight;
                Vector3 half = span * 0.5f;

                Vector3 Corner(int x, int y, int z) => centre + (side * x) + (up * y) + (half * z);
                Face(submesh, Corner(-1, 1, -1), Corner(-1, 1, 1), Corner(1, 1, 1), Corner(1, 1, -1));
                Face(submesh, Corner(-1, -1, 1), Corner(-1, -1, -1), Corner(1, -1, -1), Corner(1, -1, 1));
                Face(submesh, Corner(1, -1, -1), Corner(1, 1, -1), Corner(1, 1, 1), Corner(1, -1, 1));
                Face(submesh, Corner(-1, -1, 1), Corner(-1, 1, 1), Corner(-1, 1, -1), Corner(-1, -1, -1));
                Face(submesh, Corner(1, -1, 1), Corner(1, 1, 1), Corner(-1, 1, 1), Corner(-1, -1, 1));
                Face(submesh, Corner(-1, -1, -1), Corner(-1, 1, -1), Corner(1, 1, -1), Corner(1, -1, -1));
            }

            private void Face(int submesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                int start = _vertices.Count;
                _vertices.Add(a);
                _vertices.Add(b);
                _vertices.Add(c);
                _vertices.Add(d);
                for (int index = 0; index < 4; index++)
                {
                    _normals.Add(normal);
                }

                _triangles[submesh].AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }

            public Mesh Build(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(_vertices);
                mesh.SetNormals(_normals);
                mesh.subMeshCount = 2;
                mesh.SetTriangles(_triangles[0], 0);
                mesh.SetTriangles(_triangles[1], 1);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
