using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DigitalStory.GrassBrush.Editor
{
    /// <summary>
    /// Dresses the map with composed groups of the project's low poly models, so it reads as designed rather than
    /// scattered: a dense pine forest framing the edge, ruined hamlets behind broken walls and a gate, tree groves with an
    /// undergrowth of bushes, and patches of flowers. Rivers, the middle, the gameplay spots and the beekeepers' ways in
    /// stay clear. Everything goes under "Set Dressing"; running it again rebuilds it, one Ctrl/Cmd + Z undoes it.
    /// </summary>
    public static class SetDressingMenu
    {
        private const string RootName = "Set Dressing";
        private const string PrefabFolder = "Assets/_Project/Art/prefabs/";
        private const string WaterObjectName = "Water (Crest)";
        private const int Seed = 2026;
        private const float OpenMiddle = 18f;
        private const float KeepClear = 7f;
        private const float PathHalfWidth = 4.5f;
        private const float ForestFrom = 86f;
        private const float ForestSpacing = 7.5f;
        private const float MapEdge = 135f;

        private static readonly List<(Vector2 Point, float Radius)> Placed = new List<(Vector2, float)>();
        private static readonly List<Vector2> Spots = new List<Vector2>();
        private static readonly List<(Vector2 From, Vector2 To)> Paths = new List<(Vector2, Vector2)>();
        private static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, Vector3> Scales = new Dictionary<string, Vector3>();
        private static Terrain _terrain;
        private static float _waterLevel = float.MinValue;
        private static float _edge;
        private static System.Random _random;

        [MenuItem("Tools/DigitalStory/Dress The Map")]
        private static void Dress()
        {
            _terrain = Terrain.activeTerrain;
            if (_terrain == null)
            {
                EditorUtility.DisplayDialog("Dress The Map", "The scene has no terrain.", "OK");
                return;
            }

            LowPolyKit.Build();
            _random = new System.Random(Seed);
            Placed.Clear();
            Prefabs.Clear();
            Scales.Clear();
            GameObject water = GameObject.Find(WaterObjectName);
            _waterLevel = water != null ? water.transform.position.y : float.MinValue;
            Vector3 size = _terrain.terrainData.size;
            Vector3 corner = _terrain.GetPosition();
            // The dressing frames the play area; the terrain itself reaches much farther, out of sight.
            _edge = Mathf.Min(MapEdge, Mathf.Min(Mathf.Min(-corner.x, corner.x + size.x), Mathf.Min(-corner.z, corner.z + size.z)) - 2f);
            CollectGameplay();

            Undo.SetCurrentGroupName("Dress The Map");
            int group = Undo.GetCurrentGroup();
            GameObject old = GameObject.Find(RootName);
            if (old != null)
            {
                Undo.DestroyObjectImmediate(old);
            }

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Dress The Map");
            root.isStatic = true;

            // A clearing in a forest: an open meadow in the middle, an old ruin and the existing scenery gathered at its edge,
            // trails into the trees to every nest and spawn, and a dense wood all round with an undergrowth at its rim.
            _clearingPhase = Chance() * 10f;
            int count = 0;
            // The random map places the objects inside the arena now (the programmer's SceneryView); the dressing only
            // frames it with the wood beyond its edge and the reeds along the rivers.
            count += Treeline(Child(root, "Treeline"));
            count += Woods(Child(root, "Forest"));
            count += Reeds(Child(root, "Riverside"));

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"Dressed the map with {count} models. Save the scene to keep them.");
        }

        // ---- Ground mist ----

        [MenuItem("Tools/DigitalStory/Ground Mist")]
        private static void Mist()
        {
            _terrain = Terrain.activeTerrain;
            if (_terrain == null)
            {
                return;
            }

            // The same clearing the dressing used: its edge comes from the first draw of the same seed.
            _random = new System.Random(Seed);
            _clearingPhase = Chance() * 10f;
            GameObject water = GameObject.Find(WaterObjectName);
            _waterLevel = water != null ? water.transform.position.y : float.MinValue;

            Undo.SetCurrentGroupName("Ground Mist");
            int group = Undo.GetCurrentGroup();
            GameObject old = GameObject.Find("Ground Mist");
            if (old != null)
            {
                Undo.DestroyObjectImmediate(old);
            }

            // The distance fog was already on; a touch thicker and greener, so the wood fades into the night.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.06f, 0.09f, 0.11f);
            RenderSettings.fogDensity = 0.019f;

            var root = new GameObject("Ground Mist");
            Undo.RegisterCreatedObjectUndo(root, "Ground Mist");
            root.AddComponent<GroundMist>();

            var material = AssetDatabase.LoadAssetAtPath<Material>(LowPolyKit.Folder + "/Materials/Ground Mist.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("DigitalStory/GroundMist")) { name = "Ground Mist" };
                AssetDatabase.CreateAsset(material, LowPolyKit.Folder + "/Materials/Ground Mist.mat");
            }

            string meshPath = LowPolyKit.Folder + "/Meshes/Mist Sheet.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null)
            {
                mesh = new Mesh
                {
                    name = "Mist Sheet",
                    vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f) },
                    uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) },
                    triangles = new[] { 0, 1, 2, 0, 2, 3 },
                    normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up },
                };
                mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, meshPath);
            }

            var sheets = new List<Vector2>();
            int count = 0;

            // Along the forest's rim, where the cool air pools.
            for (float angle = 0f; angle < 360f; angle += 14f)
            {
                Vector2 dir = Dir(angle + Jitter(5f));
                Vector2 point = dir * (ClearingAt(dir * 100f) + Mathf.Lerp(-6f, 4f, Chance()));
                count += Sheet(root.transform, mesh, material, point, Mathf.Lerp(16f, 26f, Chance()), sheets);
            }

            // Over the rivers.
            if (_waterLevel > float.MinValue)
            {
                for (int attempt = 0; attempt < 3000; attempt++)
                {
                    var point = new Vector2(Jitter(110f), Jitter(110f));
                    float height = _terrain.SampleHeight(new Vector3(point.x, 0f, point.y)) + _terrain.GetPosition().y;
                    if (height < _waterLevel + 0.2f)
                    {
                        count += Sheet(root.transform, mesh, material, point, Mathf.Lerp(14f, 22f, Chance()), sheets);
                    }
                }
            }

            // A few loose wisps through the wood and over the meadow.
            for (int wisp = 0; wisp < 14; wisp++)
            {
                var point = Dir(Chance() * 360f) * Mathf.Lerp(20f, 105f, Chance());
                count += Sheet(root.transform, mesh, material, point, Mathf.Lerp(12f, 20f, Chance()), sheets);
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"Ground mist: {count} sheets. Tune it on the Ground Mist material. Save the scene to keep it.");
        }

        // One sheet of mist, unless another already covers that spot.
        private static int Sheet(Transform parent, Mesh mesh, Material material, Vector2 point, float size, List<Vector2> sheets)
        {
            foreach (Vector2 other in sheets)
            {
                if (Vector2.Distance(point, other) < size * 0.55f)
                {
                    return 0;
                }
            }

            var position = new Vector3(point.x, 0f, point.y);
            position.y = Mathf.Max(_terrain.SampleHeight(position) + _terrain.GetPosition().y, _waterLevel) + Mathf.Lerp(0.5f, 1.3f, Chance());
            var sheet = new GameObject("Mist");
            sheet.transform.SetParent(parent, false);
            sheet.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, RandomYaw(), 0f));
            sheet.transform.localScale = new Vector3(size, 1f, size * Mathf.Lerp(0.6f, 1f, Chance()));
            sheet.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = sheet.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            sheets.Add(point);
            return 1;
        }

        // ---- The clearing ----

        private const float ClearingRadius = 106f;
        private static float _clearingPhase;

        // The clearing's edge wanders in and out, so it reads as grown rather than drawn.
        private static float ClearingAt(Vector2 point)
        {
            float angle = Mathf.Atan2(point.y, point.x);
            float wander = (Mathf.Sin((angle * 3f) + _clearingPhase) * 5f) + (Mathf.Sin((angle * 7f) + (_clearingPhase * 2.3f)) * 2.5f)
                + (Mathf.Sin((angle * 13f) + (_clearingPhase * 0.7f)) * 1.2f);
            return ClearingRadius + wander;
        }

        // The existing scenery joins the picture: its trees go to the treeline, its bushes to the undergrowth, and its ruins
        // to one old ruin at the clearing's edge, half taken back by the wood.
        private static int GatherScenery()
        {
            GameObject scenery = GameObject.Find("Scenery");
            if (scenery == null)
            {
                return 0;
            }

            var ruins = new List<Transform>();
            var plants = new List<Transform>();
            foreach (Transform item in scenery.transform)
            {
                // The ruins stay exactly where they were set (one holds a nest tree), and so does anything round a nest.
                if (item.name.StartsWith("Ruin") || item.name.StartsWith("Shack") || NearNest(Flat(item.position)))
                {
                    Placed.Add((Flat(item.position), Mathf.Min(Bounds(item.gameObject), 6f)));
                    continue;
                }

                (item.name.StartsWith("Ruin") || item.name.StartsWith("Shack") ? ruins : plants).Add(item);
            }

            Undo.RecordObjects(scenery.GetComponentsInChildren<Transform>(), "Dress The Map");
            int count = 0;
            float ruinAngle = Chance() * 360f;
            Vector2 ruinCentre = default;
            for (int attempt = 0; attempt < 40; attempt++)
            {
                ruinCentre = Dir(ruinAngle + (attempt * 23f)) * 1f;
                ruinCentre *= ClearingAt(ruinCentre * 100f) - 2f;
                if (IsFree(ruinCentre, 10f))
                {
                    break;
                }
            }

            float facing = LookAt(ruinCentre, Vector2.zero);
            for (int index = 0; index < ruins.Count; index++)
            {
                for (int attempt = 0; attempt < 30; attempt++)
                {
                    Vector2 point = ruinCentre + (Dir(facing + 180f + Jitter(110f)) * (3f + (index * 1.6f) + attempt * 0.3f));
                    if (Move(ruins[index], point, facing + Jitter(25f), 0f))
                    {
                        count++;
                        break;
                    }
                }
            }

            var ruinGroup = Child(GameObject.Find(RootName), "Old Ruin");
            count += Around(ruinGroup, ruinCentre, "Kit_Rubble", 4, 2f, 9f);
            count += Around(ruinGroup, ruinCentre, "Kit_Pillar_Broken", 2, 4f, 9f);
            count += Around(ruinGroup, ruinCentre, "Kit_Fern", 6, 3f, 10f);
            count += Around(ruinGroup, ruinCentre, "Bush_Lavender", 3, 5f, 10f);

            foreach (Transform plant in plants)
            {
                for (int attempt = 0; attempt < 40; attempt++)
                {
                    Vector2 dir = Dir(Chance() * 360f);
                    Vector2 point = dir * (ClearingAt(dir * 100f) + Mathf.Lerp(-2f, 6f, Chance()));
                    if (Move(plant, point, RandomYaw(), 0f))
                    {
                        count++;
                        break;
                    }
                }
            }

            return count;
        }

        private const float NestKeepsItsSetting = 12f;
        private static readonly List<Vector2> NestPoints = new List<Vector2>();

        private static bool NearNest(Vector2 point)
        {
            foreach (Vector2 nest in NestPoints)
            {
                if (Vector2.Distance(point, nest) < NestKeepsItsSetting)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Move(Transform item, Vector2 point, float yaw, float unused)
        {
            float radius = Mathf.Min(Bounds(item.gameObject), 6f);
            if (!IsFree(point, radius))
            {
                return false;
            }

            var position = new Vector3(point.x, 0f, point.y);
            position.y = _terrain.SampleHeight(position) + _terrain.GetPosition().y;
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(item.gameObject);
            Quaternion own = source != null ? source.transform.localRotation : Quaternion.Euler(item.eulerAngles.x, 0f, item.eulerAngles.z);
            item.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f) * own);
            Placed.Add((point, radius));
            return true;
        }

        // Inside the clearing: open grass with a few lone trees near the rim, drifts of flowers and a few stones.
        private static int InTheClearing(Transform parent)
        {
            int count = 0;
            for (int tree = 0; tree < 6; tree++)
            {
                Vector2 dir = Dir((tree * 60f) + Jitter(20f));
                Vector2 point = dir * (ClearingAt(dir * 100f) * Mathf.Lerp(0.6f, 0.85f, Chance()));
                string kind = tree % 3 == 2 ? "Tree_Dead" : "Tree_Pine";
                if (Put(parent, kind, point, RandomYaw(), Mathf.Lerp(1f, 1.3f, Chance())))
                {
                    count++;
                    count += Around(parent, point, "Kit_Fern", 2, 1.5f, 3f);
                    count += Around(parent, point, tree % 2 == 0 ? "Kit_Mushroom_Red" : "Kit_Mushrooms", 1, 1.2f, 2.5f);
                }
            }

            for (int drift = 0; drift < 7; drift++)
            {
                if (!FindSpot(drift * 51f, 50f, 22f, ClearingRadius - 8f, 3f, out Vector2 centre))
                {
                    continue;
                }

                string flower = drift % 2 == 0 ? "Bush_Lavender" : "Bush_Flower";
                for (int index = 0; index < 9; index++)
                {
                    Vector2 point = centre + new Vector2(Gaussian().x * 4.5f, Gaussian().y * 2f);
                    if (Put(parent, flower, point, RandomYaw(), Mathf.Lerp(0.7f, 1.1f, Chance())))
                    {
                        count++;
                    }
                }
            }

            for (int stone = 0; stone < 4; stone++)
            {
                if (FindSpot(Chance() * 360f, 360f, 25f, ClearingRadius - 6f, 2f, out Vector2 point))
                {
                    count += Put(parent, stone == 0 ? "Kit_Boulder" : "Kit_Rock_A", point, RandomYaw(), Mathf.Lerp(0.8f, 1.2f, Chance())) ? 1 : 0;
                    count += Around(parent, point, "Kit_Pebbles", 1, 1.5f, 2.5f);
                }
            }

            return count;
        }

        // The rim of the wood: a thick undergrowth of bushes and ferns, with smaller and dead trees mixed in.
        private static int Treeline(Transform parent)
        {
            int count = 0;
            for (float angle = 0f; angle < 360f; angle += 1.6f)
            {
                Vector2 dir = Dir(angle + Jitter(0.8f));
                float rim = ClearingAt(dir * 100f);
                // The play area is a square, so the rim follows the square: a wall of wood on every side.
                Vector2 point = dir * ((rim + Mathf.Lerp(-1.5f, 5f, Chance())) / Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y)));
                float pick = Chance();
                string kind = pick < 0.3f ? "Bush_Round" : pick < 0.5f ? "Bush_Cone" : pick < 0.7f ? "Kit_Fern" : pick < 0.78f ? "Kit_Stump" : pick < 0.86f ? "Kit_Log" : pick < 0.93f ? "Kit_Rock_B" : "Tree_Dead";
                if (Put(parent, kind, point, RandomYaw(), Mathf.Lerp(0.8f, 1.3f, Chance())))
                {
                    count++;
                }
            }

            return count;
        }

        // The wood itself: dense pines from just past the rim to the edge, taller farther in, with ferns and mushrooms below.
        private static int Woods(Transform parent)
        {
            int count = 0;
            const float Spacing = 4.5f;
            for (float x = -_edge; x <= _edge; x += Spacing)
            {
                for (float z = -_edge; z <= _edge; z += Spacing)
                {
                    var point = new Vector2(x, z) + (Jitter2() * (Spacing * 0.45f));
                    float into = Mathf.Max(Mathf.Abs(point.x), Mathf.Abs(point.y)) - ClearingAt(point);
                    if (into < 3f || Chance() > Mathf.Lerp(0.55f, 0.95f, Mathf.InverseLerp(3f, 15f, into)))
                    {
                        continue;
                    }

                    string tree = Chance() < 0.07f ? "Tree_Dead" : "Tree_Pine";
                    float scale = Mathf.Lerp(1f, 1.6f, Mathf.InverseLerp(3f, 40f, into)) * Mathf.Lerp(0.85f, 1.15f, Chance());
                    if (Put(parent, tree, point, RandomYaw(), scale))
                    {
                        count++;
                        if (into < 25f && Chance() < 0.3f)
                        {
                            string below = Chance() < 0.6f ? "Kit_Fern" : Chance() < 0.5f ? "Kit_Mushroom_Red" : "Bush_Round";
                            count += Around(parent, point, below, 1, 1.4f, 2.5f);
                        }
                    }
                }
            }

            return count;
        }

        // ---- The compositions ----

        // A forest wall round the edge, thicker the farther out, mostly pines with a few dead trees and an undergrowth.
        private static int Forest(Transform parent)
        {
            int count = 0;
            for (float x = -_edge; x <= _edge; x += ForestSpacing)
            {
                for (float z = -_edge; z <= _edge; z += ForestSpacing)
                {
                    var point = new Vector2(x, z) + (Jitter2() * (ForestSpacing * 0.45f));
                    float out01 = Mathf.InverseLerp(ForestFrom, _edge, Mathf.Max(Mathf.Abs(point.x), Mathf.Abs(point.y)));
                    if (out01 <= 0f || Chance() > Mathf.Lerp(0.35f, 0.92f, out01))
                    {
                        continue;
                    }

                    string tree = Chance() < 0.09f ? "Tree_Dead" : "Tree_Pine";
                    if (Put(parent, tree, point, RandomYaw(), Mathf.Lerp(0.95f, 1.35f, out01)))
                    {
                        count++;
                        if (Chance() < 0.4f)
                        {
                            count += Around(parent, point, Chance() < 0.5f ? "Bush_Cone" : "Bush_Round", 1, 2.2f, 3.5f);
                        }
                    }
                }
            }

            return count;
        }

        // Three ruined hamlets between the ways in: a house in the middle, broken walls round it, a gate facing the arena.
        private static int Hamlets(Transform parent)
        {
            int count = 0;
            string[] houses = { "Ruin_House_B", "Shack_Abandoned_A", "Shack_Abandoned_B" };
            string[] walls = { "medieval_wall 1", "Ruin_Wall_Window" };
            float start = Chance() * 360f;
            for (int hamlet = 0; hamlet < 3; hamlet++)
            {
                if (!FindSpot(start + (hamlet * 120f), 40f, 52f, 70f, 13f, out Vector2 centre))
                {
                    continue;
                }

                var group = Child(parent.gameObject, $"Hamlet {hamlet + 1}");
                float facing = LookAt(centre, Vector2.zero);
                if (Put(group, houses[hamlet % houses.Length], centre, facing + 180f + Jitter(15f), 1f))
                {
                    count++;
                }

                // Broken walls on a square round the house, one side left open toward the arena for the gate.
                for (int side = 1; side < 4; side++)
                {
                    float angle = facing + (side * 90f);
                    Vector2 wallPoint = centre + (Dir(angle) * 8f) + (Jitter2() * 1.2f);
                    if (Chance() < 0.85f && Put(group, walls[_random.Next(walls.Length)], wallPoint, angle + 90f + Jitter(8f), 1f))
                    {
                        count++;
                    }
                }

                Vector2 gatePoint = centre + (Dir(facing) * 9f);
                if (Put(group, Chance() < 0.6f ? "Ruin_Arch_Gate" : "Ruin_Arcade", gatePoint, facing + 90f, 1f))
                {
                    count++;
                }

                count += Around(group, centre, "Tree_Dead", 1, 10f, 13f);
                count += Around(group, gatePoint, "Kit_Lantern", 2, 2f, 3f);
                count += Around(group, centre, "Kit_Crate", 2, 3f, 6f);
                count += Around(group, centre, "Kit_Well", 1, 4f, 6f);
                count += Around(group, centre, "Kit_Pillar_Broken", 2, 10f, 13f);
                count += Around(group, centre, "Kit_Rubble", 3, 7f, 12f);
                count += Around(group, centre, "Kit_Barrel", 1, 3f, 6f);
                count += Around(group, centre, "Kit_Fence_Broken", 2, 11f, 14f);
                count += Around(group, gatePoint, "Kit_Signpost", 1, 3f, 4f);
                count += Around(group, centre, "Kit_Grave", hamlet == 0 ? 3 : 0, 12f, 15f);
                count += Around(group, centre, "Bush_Lavender", 4, 9f, 12f);
                count += Around(group, centre, "Bush_Flower", 3, 6f, 11f);
                count += Around(group, centre, "Bush_Round", 3, 10f, 14f);
            }

            return count;
        }

        // Groves of one kind of tree, close together, ringed by bushes.
        private static int Groves(Transform parent)
        {
            int count = 0;
            for (int grove = 0; grove < 8; grove++)
            {
                if (!FindSpot(Chance() * 360f, 180f, 28f, 78f, 9f, out Vector2 centre))
                {
                    continue;
                }

                var group = Child(parent.gameObject, $"Grove {grove + 1}");
                string tree = grove % 4 == 3 ? "Tree_Dead" : "Tree_Pine";
                int trees = 3 + _random.Next(4);
                for (int index = 0; index < trees; index++)
                {
                    for (int attempt = 0; attempt < 12; attempt++)
                    {
                        if (Put(group, tree, centre + (Gaussian() * 3.5f), RandomYaw(), Mathf.Lerp(0.85f, 1.25f, Chance())))
                        {
                            count++;
                            break;
                        }
                    }
                }

                count += Around(group, centre, "Bush_Cone", 3, 5f, 8f);
                count += Around(group, centre, "Bush_Round", 3, 5f, 8f);
                count += Around(group, centre, grove % 2 == 0 ? "Bush_Lavender" : "Bush_Flower", 2, 6f, 9f);
                count += Around(group, centre, "Kit_Fern", 4, 2f, 6f);
                count += Around(group, centre, grove % 2 == 0 ? "Kit_Mushroom_Big" : "Kit_Mushroom_Red", 2, 2f, 5f);
                count += Around(group, centre, grove % 3 == 0 ? "Kit_Log_Pile" : "Kit_Stump_Tall", 1, 5f, 8f);
            }

            return count;
        }

        // Patches of flowers and lavender, in drifts.
        private static int Meadows(Transform parent)
        {
            int count = 0;
            for (int patch = 0; patch < 6; patch++)
            {
                if (!FindSpot(Chance() * 360f, 180f, 22f, 65f, 5f, out Vector2 centre))
                {
                    continue;
                }

                var group = Child(parent.gameObject, $"Flowers {patch + 1}");
                string flower = patch % 2 == 0 ? "Bush_Lavender" : "Bush_Flower";
                for (int index = 0; index < 12; index++)
                {
                    for (int attempt = 0; attempt < 10; attempt++)
                    {
                        string kind = Chance() < 0.75f ? flower : (flower == "Bush_Flower" ? "Bush_Lavender" : "Bush_Flower");
                        if (Put(group, kind, centre + (Gaussian() * 3.2f), RandomYaw(), Mathf.Lerp(0.8f, 1.2f, Chance())))
                        {
                            count++;
                            break;
                        }
                    }
                }
            }

            return count;
        }

        // The beekeepers' camps: stacked hive boxes, a lantern and a few rocks, beside each way in, near the edge.
        private static int Camps(Transform parent)
        {
            int count = 0;
            int camp = 0;
            foreach ((Vector2 from, Vector2 to) in Paths)
            {
                Vector2 along = (to - from).normalized;
                Vector2 side = new Vector2(along.y, -along.x) * (camp % 2 == 0 ? 1f : -1f);
                // On the trail's side, a little way into the wood from the clearing's rim.
                float rimDistance = Vector2.Distance(from, to) - ClearingAt(from);
                Vector2 centre = from + (along * Mathf.Max(8f, rimDistance - 14f)) + (side * 9f);
                var group = Child(parent.gameObject, $"Camp {++camp}");
                for (int box = 0; box < 4; box++)
                {
                    for (int attempt = 0; attempt < 10; attempt++)
                    {
                        Vector2 point = centre + (Gaussian() * 2.5f);
                        if (Put(group, box % 2 == 0 ? "Kit_HiveBoxes_Tall" : "Kit_HiveBoxes", point, LookAt(point, to) + Jitter(20f), 1f))
                        {
                            count++;
                            break;
                        }
                    }
                }

                count += Around(group, centre, "Kit_Lantern", 1, 3.5f, 4.5f);
                count += Around(group, centre, "Kit_Tent", 1, 6f, 8f);
                count += Around(group, centre, "Kit_Campfire", 1, 4f, 5f);
                count += Around(group, centre, "Kit_Skep", 2, 3f, 6f);
                count += Around(group, centre, "Kit_HoneyJars", 1, 2f, 4f);
                count += Around(group, centre, "Kit_Barrel", 1, 4f, 6f);
                count += Around(group, centre, camp % 2 == 0 ? "Kit_Cart" : "Kit_Signpost", 1, 5f, 7f);
                count += Around(group, centre, "Kit_Fence", 2, 8f, 10f);
                count += Around(group, centre, "Kit_Crate", 2, 3f, 5f);
                count += Around(group, centre, "Kit_Rock_A", 2, 4f, 6f);
                count += Around(group, centre, "Bush_Flower", 4, 4f, 7f);
            }

            return count;
        }

        // The small things that make a place feel lived in: rocks, stumps, fallen logs, glowing mushrooms, standing stones.
        private static int Details(Transform parent)
        {
            int count = 0;
            var anchors = new List<(Vector2 Point, float Radius)>(Placed);
            string[] smalls =
            {
                "Kit_Rock_A", "Kit_Rock_B", "Kit_Rock_C", "Kit_Rock_Flat", "Kit_Pebbles", "Kit_Stump", "Kit_Log", "Kit_Branch",
                "Kit_Mushrooms", "Kit_Mushrooms", "Kit_Mushroom_Red", "Kit_Fern", "Kit_Fern",
            };
            for (int index = 0; index < 170; index++)
            {
                (Vector2 anchor, float radius) = anchors[_random.Next(anchors.Count)];
                string kind = smalls[_random.Next(smalls.Length)];
                Vector2 point = anchor + (Dir(Chance() * 360f) * (radius + 0.8f + (Chance() * 2.5f)));
                if (Put(parent, kind, point, RandomYaw(), Mathf.Lerp(0.7f, 1.4f, Chance())))
                {
                    count++;
                }
            }

            // A few big boulders out in the open, landmarks to steer by.
            for (int boulder = 0; boulder < 6; boulder++)
            {
                if (FindSpot(Chance() * 360f, 360f, 25f, 80f, 3f, out Vector2 spot) && Put(parent, "Kit_Boulder", spot, RandomYaw(), Mathf.Lerp(0.8f, 1.3f, Chance())))
                {
                    count++;
                }
            }

            count += Reeds(parent);

            // A few standing stones in a broken circle, a landmark out in the open.
            if (FindSpot(Chance() * 360f, 360f, 35f, 70f, 7f, out Vector2 circle))
            {
                var group = Child(parent.gameObject, "Stone Circle");
                for (int stone = 0; stone < 7; stone++)
                {
                    if (Chance() < 0.8f && Put(group, "Kit_StandingStone", circle + (Dir(stone * (360f / 7f)) * 5f), (stone * (360f / 7f)) + Jitter(10f), Mathf.Lerp(0.8f, 1.3f, Chance())))
                    {
                        count++;
                    }
                }

                count += Around(group, circle, "Kit_Mushrooms", 3, 1f, 3f);
            }

            return count;
        }

        // Reeds along the river banks: on dry ground just above the water.
        private static int Reeds(Transform parent)
        {
            if (_waterLevel == float.MinValue || Prefab("Kit_Reeds") == null)
            {
                return 0;
            }

            int count = 0;
            var group = Child(parent.gameObject, "Reeds");
            for (int attempt = 0; attempt < 12000 && count < 320; attempt++)
            {
                var point = new Vector2(Jitter(_edge), Jitter(_edge));
                if (!Bank(point) || !Clear(point, 0.5f))
                {
                    continue;
                }

                // Reeds grow in beds: a few clumps round each one.
                count += Reed(group, point);
                for (int clump = 0; clump < 3; clump++)
                {
                    Vector2 near = point + (Gaussian() * 1.6f);
                    if (Bank(near) && Clear(near, 0.45f))
                    {
                        count += Reed(group, near);
                    }
                }
            }

            return count;
        }

        // Dry ground just above the water: the banks.
        private static bool Bank(Vector2 point)
        {
            float height = _terrain.SampleHeight(new Vector3(point.x, 0f, point.y)) + _terrain.GetPosition().y;
            return height >= _waterLevel + 0.08f && height <= _waterLevel + 0.9f;
        }

        private static int Reed(Transform group, Vector2 point)
        {
            var position = new Vector3(point.x, 0f, point.y);
            position.y = _terrain.SampleHeight(position) + _terrain.GetPosition().y - 0.05f;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(Prefab("Kit_Reeds"), group);
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, RandomYaw(), 0f));
            instance.transform.localScale = Vector3.one * Mathf.Lerp(0.75f, 1.35f, Chance());
            instance.isStatic = false;
            Placed.Add((point, 0.45f));
            return 1;
        }

        // Free of the middle, the gameplay spots, the ways in and what is placed, but allowed next to the water.
        private static bool Clear(Vector2 point, float radius)
        {
            if (point.magnitude < OpenMiddle)
            {
                return false;
            }

            foreach (Vector2 spot in Spots)
            {
                if (Vector2.Distance(point, spot) < KeepClear)
                {
                    return false;
                }
            }

            foreach ((Vector2 from, Vector2 to) in Paths)
            {
                if (DistanceToSegment(point, from, to) < PathHalfWidth)
                {
                    return false;
                }
            }

            foreach ((Vector2 other, float otherRadius) in Placed)
            {
                if (Vector2.Distance(point, other) < (otherRadius + radius) * 0.8f)
                {
                    return false;
                }
            }

            return true;
        }

        // ---- Placing ----

        private static int Around(Transform parent, Vector2 centre, string prefab, int how, float near, float far)
        {
            int count = 0;
            for (int index = 0; index < how; index++)
            {
                for (int attempt = 0; attempt < 12; attempt++)
                {
                    Vector2 point = centre + (Dir(Chance() * 360f) * Mathf.Lerp(near, far, Chance()));
                    if (Put(parent, prefab, point, RandomYaw(), Mathf.Lerp(0.85f, 1.2f, Chance())))
                    {
                        count++;
                        break;
                    }
                }
            }

            return count;
        }

        private static bool FindSpot(float angle, float spread, float near, float far, float radius, out Vector2 point)
        {
            for (int attempt = 0; attempt < 60; attempt++)
            {
                point = Dir(angle + Jitter(spread * 0.5f)) * Mathf.Lerp(near, far, Chance());
                if (IsFree(point, radius))
                {
                    return true;
                }
            }

            point = default;
            return false;
        }

        private static bool Put(Transform parent, string name, Vector2 point, float yaw, float scale)
        {
            GameObject prefab = Prefab(name);
            if (prefab == null)
            {
                return false;
            }

            float radius = Footprint(name) * scale;
            if (!IsFree(point, radius))
            {
                return false;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            var position = new Vector3(point.x, 0f, point.y);
            position.y = _terrain.SampleHeight(position) + _terrain.GetPosition().y;
            // Keep the model's own rotation (some stand up only with it) and turn it about the up axis.
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f) * prefab.transform.localRotation);
            instance.transform.localScale = Scales[name] * scale;
            foreach (Transform part in instance.GetComponentsInChildren<Transform>())
            {
                part.gameObject.isStatic = true;
            }

            Placed.Add((point, radius));
            return true;
        }

        private static bool IsFree(Vector2 point, float radius)
        {
            if (point.magnitude < OpenMiddle + radius || Mathf.Abs(point.x) > _edge || Mathf.Abs(point.y) > _edge)
            {
                return false;
            }

            foreach (Vector2 spot in Spots)
            {
                if (Vector2.Distance(point, spot) < KeepClear + radius)
                {
                    return false;
                }
            }

            foreach ((Vector2 from, Vector2 to) in Paths)
            {
                if (DistanceToSegment(point, from, to) < PathHalfWidth + radius)
                {
                    return false;
                }
            }

            foreach ((Vector2 other, float otherRadius) in Placed)
            {
                if (Vector2.Distance(point, other) < (otherRadius + radius) * 0.85f)
                {
                    return false;
                }
            }

            // Stay out of the rivers and off their banks.
            for (int side = 0; side < 5; side++)
            {
                Vector2 probe = side == 0 ? point : point + (Dir(side * 90f) * (radius + 1.5f));
                var world = new Vector3(probe.x, 0f, probe.y);
                if (_terrain.SampleHeight(world) + _terrain.GetPosition().y < _waterLevel + 0.4f)
                {
                    return false;
                }
            }

            return true;
        }

        // ---- What the scene already has ----

        private static void CollectGameplay()
        {
            Spots.Clear();
            Paths.Clear();
            var spawns = new List<Vector2>();
            var hives = new List<Vector2>();
            var nests = new List<Vector2>();
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                string name = root.name;
                if (name.StartsWith("Hive A") || name.StartsWith("Hive B"))
                {
                    hives.Add(Flat(root.transform.position));
                }
                else if (name == "Nests" || name == "Spawn Points")
                {
                    foreach (Transform child in root.transform)
                    {
                        Spots.Add(Flat(child.position));
                        (name == "Spawn Points" ? spawns : nests).Add(Flat(child.position));
                    }
                }
                else if (name == "Bridges")
                {
                    foreach (Transform child in root.transform)
                    {
                        Spots.Add(Flat(child.position));
                    }
                }
            }

            Spots.AddRange(hives);
            Vector2 target = Vector2.zero;
            foreach (Vector2 hive in hives)
            {
                target += hive / hives.Count;
            }

            // The beekeepers walk in from each spawn point toward the hives, so their way stays open, and a trail runs from
            // the middle to every nest, so the swarm can reach it through the wood.
            foreach (Vector2 spawn in spawns)
            {
                Paths.Add((spawn, target));
            }

            NestPoints.Clear();
            NestPoints.AddRange(nests);
            foreach (Vector2 nest in nests)
            {
                Paths.Add((Vector2.zero, nest));
            }
        }

        // What already stands in the scene keeps its room.
        private static void CollectExisting()
        {
            GameObject scenery = GameObject.Find("Scenery");
            if (scenery == null)
            {
                return;
            }

            foreach (Transform item in scenery.transform)
            {
                Placed.Add((Flat(item.position), Mathf.Min(Bounds(item.gameObject), 6f)));
            }
        }

        private static GameObject Prefab(string name)
        {
            if (!Prefabs.TryGetValue(name, out GameObject prefab))
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + name + ".prefab")
                    ?? AssetDatabase.LoadAssetAtPath<GameObject>(LowPolyKit.PrefabFolder + name + ".prefab");
                Prefabs[name] = prefab;
                Scales[name] = prefab != null ? SceneScale(prefab) : Vector3.one;
            }

            return prefab;
        }

        // The size the scene already uses for this model, so new ones match the old.
        private static Vector3 SceneScale(GameObject prefab)
        {
            GameObject scenery = GameObject.Find("Scenery");
            if (scenery != null)
            {
                foreach (Transform item in scenery.transform)
                {
                    if (PrefabUtility.GetCorrespondingObjectFromSource(item.gameObject) == prefab)
                    {
                        return item.localScale;
                    }
                }
            }

            return prefab.transform.localScale;
        }

        private static readonly Dictionary<string, float> Footprints = new Dictionary<string, float>();

        private static float Footprint(string name)
        {
            if (!Footprints.TryGetValue(name, out float radius))
            {
                var probe = (GameObject)PrefabUtility.InstantiatePrefab(Prefabs[name]);
                probe.transform.localScale = Scales[name];
                probe.transform.rotation = Prefabs[name].transform.localRotation;
                radius = Bounds(probe);
                Object.DestroyImmediate(probe);
                Footprints[name] = radius;
            }

            return radius;
        }

        private static float Bounds(GameObject item)
        {
            Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return 1f;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.8f, 0.4f, 10f);
        }

        // ---- Small helpers ----

        private static Transform Child(GameObject parent, string name)
        {
            var child = new GameObject(name) { isStatic = true };
            child.transform.SetParent(parent.transform, false);
            return child.transform;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 from, Vector2 to)
        {
            Vector2 along = to - from;
            float t = along.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(point - from, along) / along.sqrMagnitude) : 0f;
            return Vector2.Distance(point, from + (along * t));
        }

        private static Vector2 Flat(Vector3 position) => new Vector2(position.x, position.z);

        private static Vector2 Dir(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
        }

        private static float LookAt(Vector2 from, Vector2 to)
        {
            Vector2 direction = to - from;
            return Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        }

        private static float Chance() => (float)_random.NextDouble();

        private static float Jitter(float amount) => ((Chance() * 2f) - 1f) * amount;

        private static Vector2 Jitter2() => new Vector2(Jitter(1f), Jitter(1f));

        private static float RandomYaw() => Chance() * 360f;

        private static Vector2 Gaussian()
        {
            float u = 1f - Chance();
            float v = Chance();
            float length = Mathf.Sqrt(-2f * Mathf.Log(u));
            return new Vector2(Mathf.Cos(v * Mathf.PI * 2f), Mathf.Sin(v * Mathf.PI * 2f)) * length * 0.6f;
        }
    }
}
