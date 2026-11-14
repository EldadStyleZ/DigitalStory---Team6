using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DigitalStory.GrassBrush.Editor
{
    /// <summary>
    /// Builds a small kit of low poly, flat shaded models in the project's style, as prefabs: rocks, a stump, a fallen
    /// log, glowing mushrooms, standing stones, the beekeepers' hive boxes and crates, and a lantern. Made once; delete
    /// the Generated folder to make them again.
    /// </summary>
    public static class LowPolyKit
    {
        public const string Folder = "Assets/_Project/Art/Generated";
        public const string PrefabFolder = Folder + "/Prefabs/";

        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        public static void Build()
        {
            System.IO.Directory.CreateDirectory(Folder + "/Meshes");
            System.IO.Directory.CreateDirectory(Folder + "/Materials");
            System.IO.Directory.CreateDirectory(Folder + "/Prefabs");
            Materials.Clear();

            var random = new System.Random(5);
            Make("Kit_Rock_A", m => m.Rock(Vector3.up * 0.35f, new Vector3(0.9f, 0.6f, 0.75f), random, 0), "Stone");
            Make("Kit_Rock_B", m => m.Rock(Vector3.up * 0.5f, new Vector3(0.8f, 0.9f, 0.7f), random, 0), "Stone Dark");
            Make("Kit_Rock_C", m =>
            {
                m.Rock(new Vector3(0f, 0.3f, 0f), new Vector3(0.7f, 0.5f, 0.6f), random, 0);
                m.Rock(new Vector3(0.65f, 0.18f, 0.25f), new Vector3(0.4f, 0.3f, 0.35f), random, 0);
                m.Rock(new Vector3(-0.4f, 0.12f, 0.5f), new Vector3(0.25f, 0.2f, 0.25f), random, 0);
            }, "Stone");
            Make("Kit_StandingStone", m => m.Prism(Vector3.zero, 5, 0.5f, 0.32f, 2.4f, Quaternion.Euler(0f, 0f, 3f), 0, 0.12f, random), "Stone Dark");
            Make("Kit_Stump", m =>
            {
                m.Prism(Vector3.zero, 7, 0.45f, 0.4f, 0.55f, Quaternion.identity, 0, 0.05f, random);
                m.Prism(new Vector3(0f, 0.55f, 0f), 7, 0.36f, 0.36f, 0.02f, Quaternion.identity, 1, 0f, random);
                for (int root = 0; root < 4; root++)
                {
                    Quaternion turn = Quaternion.Euler(0f, (root * 90f) + 20f, 0f);
                    m.Prism(turn * new Vector3(0f, 0.05f, 0.35f), 4, 0.12f, 0.04f, 0.45f, turn * Quaternion.Euler(70f, 0f, 0f), 0, 0f, random);
                }
            }, "Bark", "Wood Light");
            Make("Kit_Log", m =>
            {
                Quaternion lie = Quaternion.Euler(0f, 0f, 90f);
                m.Prism(new Vector3(1.4f, 0.3f, 0f), 7, 0.3f, 0.27f, 2.8f, lie, 0, 0.04f, random);
                m.Prism(new Vector3(1.42f, 0.3f, 0f), 7, 0.25f, 0.25f, 0.02f, lie, 1, 0f, random);
                m.Prism(new Vector3(-1.38f, 0.3f, 0f), 7, 0.23f, 0.23f, 0.02f, lie, 1, 0f, random);
                m.Prism(new Vector3(0.3f, 0.45f, 0.2f), 4, 0.07f, 0.03f, 0.5f, Quaternion.Euler(-40f, 0f, 0f), 0, 0f, random);
            }, "Bark", "Wood Light");
            Make("Kit_Mushrooms", m =>
            {
                for (int shroom = 0; shroom < 5; shroom++)
                {
                    float size = Mathf.Lerp(0.5f, 1.1f, (float)random.NextDouble());
                    var at = new Vector3(((float)random.NextDouble() - 0.5f) * 0.6f, 0f, ((float)random.NextDouble() - 0.5f) * 0.6f);
                    m.Prism(at, 5, 0.04f * size, 0.035f * size, 0.22f * size, Quaternion.Euler(((float)random.NextDouble() - 0.5f) * 20f, 0f, 0f), 0, 0f, random);
                    m.Cap(at + (Vector3.up * 0.22f * size), 0.13f * size, 0.08f * size, 1);
                    m.Spots(at + (Vector3.up * 0.22f * size), 0.13f * size, 0.08f * size, 2, random);
                }
            }, "Mushroom Stem", "Red Cap", "Spot White");
            Make("Kit_HiveBoxes", m => HiveStack(m, 2), "Hive White", "Hive Roof", "Wood Light");
            Make("Kit_HiveBoxes_Tall", m => HiveStack(m, 3), "Hive White", "Hive Roof", "Wood Light");
            Make("Kit_Crate", m =>
            {
                m.Box(new Vector3(0f, 0.3f, 0f), new Vector3(0.6f, 0.6f, 0.6f), Quaternion.identity, 0);
                m.Box(new Vector3(0f, 0.3f, 0.305f), new Vector3(0.62f, 0.08f, 0.02f), Quaternion.identity, 1);
                m.Box(new Vector3(0f, 0.3f, -0.305f), new Vector3(0.62f, 0.08f, 0.02f), Quaternion.identity, 1);
                m.Box(new Vector3(0.1f, 0.62f, 0f), new Vector3(0.14f, 0.04f, 0.5f), Quaternion.Euler(0f, 20f, 0f), 1);
            }, "Wood Light", "Bark");
            Make("Kit_Lantern", m =>
            {
                m.Prism(Vector3.zero, 4, 0.07f, 0.06f, 1.7f, Quaternion.identity, 0, 0f, random);
                m.Box(new Vector3(0.18f, 1.62f, 0f), new Vector3(0.4f, 0.05f, 0.05f), Quaternion.identity, 0);
                m.Box(new Vector3(0.33f, 1.4f, 0f), new Vector3(0.16f, 0.22f, 0.16f), Quaternion.identity, 1);
                m.Prism(new Vector3(0.33f, 1.51f, 0f), 4, 0.13f, 0.02f, 0.1f, Quaternion.Euler(0f, 45f, 0f), 2, 0f, random);
            }, "Bark", "Lantern Glow", "Iron");
            BuildMore(random);
            AssetDatabase.SaveAssets();
        }

        private static float R(System.Random random, float from, float to) => Mathf.Lerp(from, to, (float)random.NextDouble());

        // The rest of the kit: more nature, the beekeepers' camp and the ruins.
        private static void BuildMore(System.Random random)
        {
            // ---- Nature ----
            Make("Kit_Rock_Flat", m => m.Rock(Vector3.up * 0.12f, new Vector3(1.1f, 0.22f, 0.8f), random, 0), "Stone");
            Make("Kit_Boulder", m =>
            {
                m.Rock(Vector3.up * 0.9f, new Vector3(1.6f, 1.3f, 1.4f), random, 0);
                m.Rock(new Vector3(1.3f, 0.3f, 0.6f), new Vector3(0.5f, 0.4f, 0.45f), random, 0);
            }, "Stone Dark");
            Make("Kit_Pebbles", m =>
            {
                for (int pebble = 0; pebble < 7; pebble++)
                {
                    var at = new Vector3(R(random, -0.7f, 0.7f), 0.06f, R(random, -0.7f, 0.7f));
                    float size = R(random, 0.1f, 0.22f);
                    m.Rock(at, new Vector3(size, size * 0.6f, size * 0.8f), random, pebble % 2);
                }
            }, "Stone", "Stone Dark");
            Make("Kit_Stump_Tall", m =>
            {
                m.Prism(Vector3.zero, 7, 0.4f, 0.32f, 1.1f, Quaternion.Euler(0f, 0f, 4f), 0, 0.06f, random);
                m.Prism(new Vector3(0.08f, 1.1f, 0f), 5, 0.22f, 0.02f, 0.35f, Quaternion.Euler(0f, 0f, -12f), 1, 0.2f, random);
                m.Prism(new Vector3(0.25f, 0.6f, 0.2f), 4, 0.06f, 0.02f, 0.5f, Quaternion.Euler(30f, 0f, -50f), 0, 0f, random);
            }, "Bark", "Wood Light");
            Make("Kit_Log_Pile", m =>
            {
                Quaternion lie = Quaternion.Euler(0f, 0f, 90f);
                float[] xs = { -0.3f, 0.3f, 0f };
                float[] ys = { 0.22f, 0.22f, 0.6f };
                for (int log = 0; log < 3; log++)
                {
                    m.Prism(new Vector3(0.9f, ys[log], xs[log]), 7, 0.22f, 0.22f, 1.8f, lie, 0, 0.04f, random);
                    m.Prism(new Vector3(0.92f, ys[log], xs[log]), 7, 0.18f, 0.18f, 0.02f, lie, 1, 0f, random);
                    m.Prism(new Vector3(-0.9f, ys[log], xs[log]), 7, 0.18f, 0.18f, 0.02f, lie, 1, 0f, random);
                }
            }, "Bark", "Wood Light");
            Make("Kit_Branch", m =>
            {
                m.Prism(new Vector3(0f, 0.08f, 0f), 5, 0.09f, 0.05f, 2.2f, Quaternion.Euler(0f, 0f, 88f), 0, 0.05f, random);
                m.Prism(new Vector3(-0.8f, 0.1f, 0f), 4, 0.05f, 0.02f, 0.8f, Quaternion.Euler(80f, 30f, 0f), 0, 0f, random);
                m.Prism(new Vector3(-1.4f, 0.1f, 0f), 4, 0.04f, 0.01f, 0.6f, Quaternion.Euler(-75f, -20f, 0f), 0, 0f, random);
            }, "Bark");
            Make("Kit_Mushroom_Big", m =>
            {
                m.Prism(Vector3.zero, 6, 0.12f, 0.09f, 0.7f, Quaternion.Euler(0f, 0f, 5f), 0, 0.05f, random);
                m.Cap(new Vector3(0.05f, 0.7f, 0f), 0.45f, 0.26f, 1);
                m.Spots(new Vector3(0.05f, 0.7f, 0f), 0.45f, 0.26f, 2, random);
                m.Prism(new Vector3(0.35f, 0f, 0.2f), 5, 0.04f, 0.03f, 0.25f, Quaternion.identity, 0, 0f, random);
                m.Cap(new Vector3(0.35f, 0.25f, 0.2f), 0.14f, 0.08f, 1);
                m.Spots(new Vector3(0.35f, 0.25f, 0.2f), 0.14f, 0.08f, 2, random);
            }, "Mushroom Stem", "Red Cap", "Spot White");
            Make("Kit_Mushroom_Red", m =>
            {
                for (int shroom = 0; shroom < 4; shroom++)
                {
                    float size = R(random, 0.6f, 1.2f);
                    var at = new Vector3(R(random, -0.3f, 0.3f), 0f, R(random, -0.3f, 0.3f));
                    m.Prism(at, 5, 0.05f * size, 0.04f * size, 0.25f * size, Quaternion.identity, 0, 0f, random);
                    m.Cap(at + (Vector3.up * 0.25f * size), 0.16f * size, 0.1f * size, 1);
                }
            }, "Mushroom Stem", "Red Cap");
            Make("Kit_Fern", m =>
            {
                for (int frond = 0; frond < 9; frond++)
                {
                    Quaternion turn = Quaternion.Euler(R(random, 40f, 65f), frond * 40f, 0f);
                    m.Prism(Vector3.zero, 3, 0.09f, 0f, R(random, 0.7f, 1.1f), Quaternion.Euler(0f, frond * 40f, 0f) * Quaternion.Euler(R(random, 40f, 65f), 0f, 0f), frond % 2, 0f, random);
                }
            }, "Fern", "Fern Dark");
            Make("Kit_Reeds", m =>
            {
                for (int reed = 0; reed < 14; reed++)
                {
                    var at = new Vector3(R(random, -0.5f, 0.5f), 0f, R(random, -0.5f, 0.5f));
                    float height = R(random, 0.9f, 1.7f);
                    Quaternion lean = Quaternion.Euler(R(random, -10f, 10f), 0f, R(random, -10f, 10f));
                    m.Prism(at, 3, 0.03f, 0.005f, height, lean, 0, 0f, random);
                    if (reed % 3 == 0)
                    {
                        m.Prism(at + (lean * (Vector3.up * height * 0.75f)), 5, 0.045f, 0.045f, 0.22f, lean, 1, 0f, random);
                    }
                }
            }, "Reed Wind", "Cattail Wind");

            // ---- The beekeepers' camp ----
            Make("Kit_Skep", m =>
            {
                // A woven straw hive: rings that narrow toward a rounded top, with a dark doorway.
                float y = 0f;
                for (int ring = 0; ring < 6; ring++)
                {
                    float bottom = Mathf.Lerp(0.42f, 0.12f, Mathf.Pow(ring / 6f, 1.6f));
                    float top = Mathf.Lerp(0.42f, 0.12f, Mathf.Pow((ring + 1) / 6f, 1.6f));
                    m.Prism(new Vector3(0f, y, 0f), 8, bottom, top, 0.13f, Quaternion.identity, 0, 0.02f, random);
                    y += 0.12f;
                }

                m.Box(new Vector3(0f, 0.07f, 0.38f), new Vector3(0.16f, 0.1f, 0.08f), Quaternion.identity, 1);
                m.Box(new Vector3(0f, -0.04f, 0f), new Vector3(0.9f, 0.08f, 0.9f), Quaternion.identity, 2);
            }, "Straw", "Iron", "Wood Light");
            Make("Kit_HoneyJars", m =>
            {
                for (int jar = 0; jar < 5; jar++)
                {
                    var at = new Vector3(R(random, -0.25f, 0.25f), 0f, R(random, -0.25f, 0.25f));
                    float h = R(random, 0.14f, 0.22f);
                    m.Prism(at, 6, 0.07f, 0.07f, h, Quaternion.identity, 0, 0f, random);
                    m.Prism(at + (Vector3.up * h), 6, 0.075f, 0.075f, 0.03f, Quaternion.identity, 1, 0f, random);
                }
            }, "Honey", "Canvas");
            Make("Kit_Campfire", m =>
            {
                for (int stone = 0; stone < 8; stone++)
                {
                    float angle = stone * Mathf.PI / 4f;
                    m.Rock(new Vector3(Mathf.Cos(angle) * 0.55f, 0.08f, Mathf.Sin(angle) * 0.55f), new Vector3(0.15f, 0.12f, 0.13f), random, 0);
                }

                for (int log = 0; log < 4; log++)
                {
                    Quaternion turn = Quaternion.Euler(0f, log * 90f + 20f, 0f);
                    m.Prism(turn * new Vector3(0f, 0f, 0.35f), 5, 0.06f, 0.05f, 0.5f, turn * Quaternion.Euler(-60f, 0f, 0f), 1, 0f, random);
                }

                m.Rock(new Vector3(0f, 0.06f, 0f), new Vector3(0.25f, 0.1f, 0.25f), random, 2);
            }, "Stone Dark", "Bark", "Ember");
            Make("Kit_Tent", m =>
            {
                m.Prism(new Vector3(0f, 0f, -1f), 3, 1.1f, 1.1f, 2f, Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, 90f, 0f), 0, 0f, random);
                m.Prism(new Vector3(0f, 0f, -1.15f), 4, 0.05f, 0.05f, 1.9f, Quaternion.identity, 1, 0f, random);
                m.Prism(new Vector3(0f, 0f, 1.15f), 4, 0.05f, 0.05f, 1.9f, Quaternion.identity, 1, 0f, random);
            }, "Canvas", "Bark");
            Make("Kit_Barrel", m =>
            {
                m.Prism(Vector3.zero, 8, 0.3f, 0.36f, 0.4f, Quaternion.identity, 0, 0f, random);
                m.Prism(new Vector3(0f, 0.4f, 0f), 8, 0.36f, 0.3f, 0.4f, Quaternion.identity, 0, 0f, random);
                m.Prism(new Vector3(0f, 0.12f, 0f), 8, 0.335f, 0.345f, 0.05f, Quaternion.identity, 1, 0f, random);
                m.Prism(new Vector3(0f, 0.63f, 0f), 8, 0.345f, 0.335f, 0.05f, Quaternion.identity, 1, 0f, random);
            }, "Wood Light", "Iron");
            Make("Kit_Cart", m =>
            {
                m.Box(new Vector3(0f, 0.55f, 0f), new Vector3(1f, 0.35f, 0.7f), Quaternion.Euler(0f, 0f, -6f), 0);
                Quaternion wheel = Quaternion.Euler(90f, 0f, 0f);
                m.Prism(new Vector3(0.35f, 0.3f, -0.4f), 8, 0.3f, 0.3f, 0.06f, wheel, 1, 0f, random);
                m.Prism(new Vector3(0.35f, 0.3f, 0.46f), 8, 0.3f, 0.3f, 0.06f, wheel, 1, 0f, random);
                m.Box(new Vector3(-0.85f, 0.5f, 0.2f), new Vector3(0.8f, 0.05f, 0.05f), Quaternion.Euler(0f, 0f, -15f), 1);
                m.Box(new Vector3(-0.85f, 0.5f, -0.2f), new Vector3(0.8f, 0.05f, 0.05f), Quaternion.Euler(0f, 0f, -15f), 1);
            }, "Wood Light", "Bark");
            Make("Kit_Signpost", m =>
            {
                m.Prism(Vector3.zero, 4, 0.06f, 0.05f, 1.8f, Quaternion.identity, 0, 0f, random);
                m.Box(new Vector3(0.3f, 1.55f, 0f), new Vector3(0.7f, 0.18f, 0.04f), Quaternion.Euler(0f, 0f, 5f), 1);
                m.Box(new Vector3(-0.25f, 1.25f, 0f), new Vector3(0.6f, 0.16f, 0.04f), Quaternion.Euler(0f, 25f, -4f), 1);
            }, "Bark", "Wood Light");
            Make("Kit_Fence", m => Fence(m, random, false), "Bark", "Wood Light");
            Make("Kit_Fence_Broken", m => Fence(m, random, true), "Bark", "Wood Light");

            // ---- The ruins ----
            Make("Kit_Well", m =>
            {
                m.Prism(Vector3.zero, 8, 0.75f, 0.72f, 0.7f, Quaternion.identity, 0, 0.03f, random);
                m.Prism(new Vector3(0f, 0.7f, 0f), 8, 0.6f, 0.6f, 0.02f, Quaternion.identity, 3, 0f, random);
                m.Prism(new Vector3(-0.65f, 0.7f, 0f), 4, 0.06f, 0.06f, 1.2f, Quaternion.identity, 1, 0f, random);
                m.Prism(new Vector3(0.65f, 0.7f, 0f), 4, 0.06f, 0.06f, 1.2f, Quaternion.identity, 1, 0f, random);
                m.Prism(new Vector3(0f, 1.85f, -0.6f), 3, 0.62f, 0.62f, 1.2f, Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, 90f, 0f), 2, 0f, random);
            }, "Stone", "Bark", "Hive Roof", "Iron");
            Make("Kit_Pillar_Broken", m =>
            {
                m.Box(new Vector3(0f, 0.15f, 0f), new Vector3(0.8f, 0.3f, 0.8f), Quaternion.identity, 0);
                m.Prism(new Vector3(0f, 0.3f, 0f), 8, 0.28f, 0.26f, 1.4f, Quaternion.identity, 0, 0.03f, random);
                m.Prism(new Vector3(0f, 1.7f, 0f), 8, 0.26f, 0.2f, 0.25f, Quaternion.Euler(8f, 0f, 6f), 0, 0.15f, random);
                m.Prism(new Vector3(0.9f, 0f, 0.3f), 8, 0.26f, 0.26f, 0.9f, Quaternion.Euler(0f, 0f, 92f), 1, 0.03f, random);
            }, "Stone", "Stone Dark");
            Make("Kit_Rubble", m =>
            {
                for (int block = 0; block < 7; block++)
                {
                    var at = new Vector3(R(random, -0.8f, 0.8f), R(random, 0.1f, 0.2f), R(random, -0.6f, 0.6f));
                    m.Box(at, new Vector3(R(random, 0.3f, 0.5f), R(random, 0.2f, 0.3f), R(random, 0.25f, 0.4f)), Quaternion.Euler(R(random, -15f, 15f), R(random, 0f, 90f), R(random, -15f, 15f)), block % 2);
                }
            }, "Stone", "Stone Dark");
            Make("Kit_Grave", m =>
            {
                m.Box(new Vector3(0f, 0.45f, 0f), new Vector3(0.55f, 0.9f, 0.14f), Quaternion.Euler(-6f, 0f, 4f), 0);
                m.Box(new Vector3(0f, 0.05f, 0.5f), new Vector3(0.6f, 0.1f, 1f), Quaternion.identity, 1);
            }, "Stone Dark", "Stone");
        }

        private static void Fence(LowPoly m, System.Random random, bool broken)
        {
            for (int post = 0; post < 3; post++)
            {
                float lean = broken && post == 2 ? 25f : R(random, -4f, 4f);
                m.Prism(new Vector3(post * 1.2f, 0f, 0f), 4, 0.07f, 0.06f, 1f, Quaternion.Euler(0f, 0f, lean), 0, 0f, random);
            }

            for (int rail = 0; rail < 2; rail++)
            {
                float y = 0.4f + (rail * 0.4f);
                float length = broken && rail == 1 ? 1.2f : 2.4f;
                m.Box(new Vector3(length * 0.5f, y, 0.05f), new Vector3(length, 0.08f, 0.04f), Quaternion.Euler(0f, 0f, R(random, -2f, 2f)), 1);
            }

            if (broken)
            {
                m.Box(new Vector3(1.8f, 0.05f, 0.4f), new Vector3(1.2f, 0.08f, 0.04f), Quaternion.Euler(0f, 30f, 0f), 1);
            }
        }

        // A beekeeper's hive: stacked white boxes on a stand, with a dark roof.
        private static void HiveStack(LowPoly m, int boxes)
        {
            m.Box(new Vector3(0f, 0.12f, 0f), new Vector3(0.62f, 0.24f, 0.52f), Quaternion.identity, 2);
            for (int box = 0; box < boxes; box++)
            {
                float y = 0.24f + (box * 0.3f) + 0.15f;
                m.Box(new Vector3(0f, y, 0f), new Vector3(0.55f - (box % 2 * 0.01f), 0.28f, 0.45f), Quaternion.Euler(0f, (box % 2) * 1.5f, 0f), 0);
            }

            float top = 0.24f + (boxes * 0.3f);
            m.Box(new Vector3(0f, top + 0.04f, 0f), new Vector3(0.64f, 0.08f, 0.54f), Quaternion.identity, 1);
            m.Box(new Vector3(0f, 0.3f, 0.235f), new Vector3(0.2f, 0.03f, 0.02f), Quaternion.identity, 1);
        }

        private static void Make(string name, System.Action<LowPoly> build, params string[] materials)
        {
            string prefabPath = PrefabFolder + name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                if (System.Array.IndexOf(Redo, name) >= 0)
                {
                    Remake(name, build, materials, prefabPath);
                }

                return;
            }

            var mesh = new LowPoly(materials.Length);
            build(mesh);
            Mesh built = mesh.Build(name);
            AssetDatabase.CreateAsset(built, $"{Folder}/Meshes/{name}.asset");

            var shared = new Material[materials.Length];
            for (int index = 0; index < materials.Length; index++)
            {
                shared[index] = Material(materials[index]);
            }

            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = built;
            go.AddComponent<MeshRenderer>().sharedMaterials = shared;
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }

        // Models changed since they were first made: their mesh and materials are updated in place, so the scene keeps them.
        private static readonly string[] Redo = { "Kit_Reeds" };

        private static void Remake(string name, System.Action<LowPoly> build, string[] materials, string prefabPath)
        {
            var mesh = new LowPoly(materials.Length);
            build(mesh);
            Mesh fresh = mesh.Build(name);
            var stored = AssetDatabase.LoadAssetAtPath<Mesh>($"{Folder}/Meshes/{name}.asset");
            stored.Clear();
            stored.SetVertices(fresh.vertices);
            stored.SetNormals(fresh.normals);
            stored.subMeshCount = fresh.subMeshCount;
            for (int index = 0; index < fresh.subMeshCount; index++)
            {
                stored.SetTriangles(fresh.GetTriangles(index), index);
            }

            stored.RecalculateBounds();
            Object.DestroyImmediate(fresh);
            EditorUtility.SetDirty(stored);

            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            var shared = new Material[materials.Length];
            for (int index = 0; index < materials.Length; index++)
            {
                shared[index] = Material(materials[index]);
            }

            contents.GetComponent<MeshRenderer>().sharedMaterials = shared;
            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        private static Material Material(string name)
        {
            if (Materials.TryGetValue(name, out Material found))
            {
                return found;
            }

            string path = $"{Folder}/Materials/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null && name.EndsWith(" Wind"))
            {
                // Plants that sway with the grass.
                material = new Material(Shader.Find("DigitalStory/WindReeds")) { name = name };
                material.SetColor("_Color", name.StartsWith("Cattail") ? new Color(0.3f, 0.19f, 0.11f) : new Color(0.38f, 0.47f, 0.2f));
                material.enableInstancing = true;
                AssetDatabase.CreateAsset(material, path);
            }

            if (material == null)
            {
                material = new Material(Shader.Find("Standard")) { name = name };
                material.SetFloat("_Glossiness", 0.05f);
                (Color color, Color glow) = name switch
                {
                    "Stone" => (new Color(0.42f, 0.44f, 0.46f), Color.black),
                    "Stone Dark" => (new Color(0.27f, 0.29f, 0.31f), Color.black),
                    "Bark" => (new Color(0.3f, 0.2f, 0.13f), Color.black),
                    "Wood Light" => (new Color(0.62f, 0.47f, 0.3f), Color.black),
                    "Mushroom Stem" => (new Color(0.85f, 0.82f, 0.72f), Color.black),
                    "Mushroom Glow" => (new Color(0.25f, 0.75f, 0.7f), new Color(0.15f, 0.9f, 0.8f) * 1.6f),
                    "Hive White" => (new Color(0.86f, 0.82f, 0.7f), Color.black),
                    "Hive Roof" => (new Color(0.32f, 0.3f, 0.3f), Color.black),
                    "Lantern Glow" => (new Color(1f, 0.8f, 0.45f), new Color(1f, 0.65f, 0.25f) * 3f),
                    "Iron" => (new Color(0.15f, 0.15f, 0.16f), Color.black),
                    "Red Cap" => (new Color(0.72f, 0.1f, 0.07f), Color.black),
                    "Spot White" => (new Color(0.95f, 0.93f, 0.88f), Color.black),
                    "Fern" => (new Color(0.2f, 0.42f, 0.18f), Color.black),
                    "Fern Dark" => (new Color(0.13f, 0.3f, 0.13f), Color.black),
                    "Reed" => (new Color(0.4f, 0.48f, 0.22f), Color.black),
                    "Straw" => (new Color(0.78f, 0.6f, 0.3f), Color.black),
                    "Honey" => (new Color(0.95f, 0.6f, 0.1f), new Color(0.9f, 0.45f, 0.05f) * 0.8f),
                    "Canvas" => (new Color(0.75f, 0.7f, 0.58f), Color.black),
                    "Ember" => (new Color(1f, 0.4f, 0.1f), new Color(1f, 0.35f, 0.05f) * 3f),
                    _ => (Color.gray, Color.black),
                };
                material.color = color;
                if (glow != Color.black)
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", glow);
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                }

                AssetDatabase.CreateAsset(material, path);
            }

            Materials[name] = material;
            return material;
        }

        /// <summary>A mesh of flat shaded faces, each with its own corners.</summary>
        private sealed class LowPoly
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector3> _normals = new List<Vector3>();
            private readonly List<int>[] _triangles;

            public LowPoly(int submeshes)
            {
                _triangles = new List<int>[submeshes];
                for (int index = 0; index < submeshes; index++)
                {
                    _triangles[index] = new List<int>();
                }
            }

            // The part's inside point, so every face is wound to face out, whatever order its corners came in.
            private Vector3 _inside;

            public void Triangle(int submesh, Vector3 a, Vector3 b, Vector3 c)
            {
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                if (Vector3.Dot(normal, ((a + b + c) / 3f) - _inside) < 0f)
                {
                    (b, c) = (c, b);
                    normal = -normal;
                }

                int start = _vertices.Count;
                _vertices.Add(a);
                _vertices.Add(b);
                _vertices.Add(c);
                _normals.Add(normal);
                _normals.Add(normal);
                _normals.Add(normal);
                _triangles[submesh].Add(start);
                _triangles[submesh].Add(start + 1);
                _triangles[submesh].Add(start + 2);
            }

            /// <summary>An upright prism (a cone when the top radius is 0), its base at the given point.</summary>
            public void Prism(Vector3 basePoint, int sides, float bottom, float top, float height, Quaternion turn, int submesh, float wobble, System.Random random)
            {
                var low = new Vector3[sides];
                var high = new Vector3[sides];
                for (int side = 0; side < sides; side++)
                {
                    float angle = side * Mathf.PI * 2f / sides;
                    var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    float jitter = 1f + (((float)random.NextDouble() - 0.5f) * 2f * wobble);
                    low[side] = basePoint + (turn * (dir * bottom * jitter));
                    high[side] = basePoint + (turn * ((dir * top * jitter) + (Vector3.up * height)));
                }

                _inside = basePoint + (turn * (Vector3.up * height * 0.5f));
                Vector3 bottomCentre = basePoint;
                Vector3 topCentre = basePoint + (turn * (Vector3.up * height));
                for (int side = 0; side < sides; side++)
                {
                    int next = (side + 1) % sides;
                    Triangle(submesh, low[side], high[side], high[next]);
                    Triangle(submesh, low[side], high[next], low[next]);
                    Triangle(submesh, topCentre, high[next], high[side]);
                    Triangle(submesh, bottomCentre, low[side], low[next]);
                }
            }

            /// <summary>A mushroom cap: a low faceted dome.</summary>
            public void Cap(Vector3 basePoint, float radius, float height, int submesh)
            {
                const int Sides = 6;
                Vector3 tip = basePoint + (Vector3.up * height);
                _inside = basePoint + (Vector3.up * height * 0.3f);
                for (int side = 0; side < Sides; side++)
                {
                    float a0 = side * Mathf.PI * 2f / Sides;
                    float a1 = (side + 1) * Mathf.PI * 2f / Sides;
                    Vector3 rim0 = basePoint + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * radius;
                    Vector3 rim1 = basePoint + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radius;
                    Vector3 mid0 = Vector3.Lerp(rim0, tip, 0.5f) + (Vector3.up * height * 0.25f) + ((rim0 - basePoint) * 0.15f);
                    Vector3 mid1 = Vector3.Lerp(rim1, tip, 0.5f) + (Vector3.up * height * 0.25f) + ((rim1 - basePoint) * 0.15f);
                    Triangle(submesh, rim0, mid0, mid1);
                    Triangle(submesh, rim0, mid1, rim1);
                    Triangle(submesh, mid0, tip, mid1);
                    Triangle(submesh, basePoint, rim0, rim1);
                }
            }

            /// <summary>White spots on a cap, like a fly agaric: small flat bumps on its slope and one near the top.</summary>
            public void Spots(Vector3 basePoint, float radius, float height, int submesh, System.Random random)
            {
                int count = 5 + random.Next(3);
                for (int spot = 0; spot < count; spot++)
                {
                    float angle = (spot / (float)count * Mathf.PI * 2f) + (float)random.NextDouble();
                    float along = 0.45f + ((float)random.NextDouble() * 0.25f);
                    var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    Vector3 rim = basePoint + (dir * radius);
                    Vector3 tip = basePoint + (Vector3.up * height);
                    Vector3 at = Vector3.Lerp(rim, tip, along) + (Vector3.up * height * 0.28f) + (dir * radius * 0.08f);
                    float size = radius * 0.16f;
                    Quaternion tilt = Quaternion.FromToRotation(Vector3.up, ((at - basePoint).normalized + Vector3.up).normalized);
                    Box(at, new Vector3(size, size * 0.35f, size), tilt, submesh);
                }

                Box(basePoint + (Vector3.up * height * 1.02f), new Vector3(radius * 0.18f, radius * 0.06f, radius * 0.18f), Quaternion.identity, submesh);
            }

            public void Box(Vector3 centre, Vector3 size, Quaternion turn, int submesh)
            {
                Vector3 h = size * 0.5f;
                _inside = centre;
                Vector3 P(float x, float y, float z) => centre + (turn * new Vector3(x * h.x, y * h.y, z * h.z));
                Quad(submesh, P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1));
                Quad(submesh, P(-1, -1, 1), P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1));
                Quad(submesh, P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1));
                Quad(submesh, P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), P(-1, -1, -1));
                Quad(submesh, P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), P(-1, -1, 1));
                Quad(submesh, P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), P(1, -1, -1));
            }

            private void Quad(int submesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                Triangle(submesh, a, b, c);
                Triangle(submesh, a, c, d);
            }

            /// <summary>A faceted rock: an octahedron split once, its corners pushed about.</summary>
            public void Rock(Vector3 centre, Vector3 size, System.Random random, int submesh)
            {
                var corners = new List<Vector3> { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
                int[] faces = { 0, 4, 3, 0, 3, 5, 0, 5, 2, 0, 2, 4, 1, 3, 4, 1, 5, 3, 1, 2, 5, 1, 4, 2 };
                var cache = new Dictionary<long, int>();
                int Middle(int a, int b)
                {
                    long key = ((long)Mathf.Min(a, b) << 32) + Mathf.Max(a, b);
                    if (!cache.TryGetValue(key, out int index))
                    {
                        index = corners.Count;
                        corners.Add(((corners[a] + corners[b]) * 0.5f).normalized);
                        cache[key] = index;
                    }

                    return index;
                }

                var split = new List<int>();
                for (int face = 0; face < faces.Length; face += 3)
                {
                    int a = faces[face], b = faces[face + 1], c = faces[face + 2];
                    int ab = Middle(a, b), bc = Middle(b, c), ca = Middle(c, a);
                    split.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }

                var shaped = new Vector3[corners.Count];
                for (int index = 0; index < corners.Count; index++)
                {
                    float push = 0.8f + ((float)random.NextDouble() * 0.4f);
                    Vector3 corner = corners[index] * push;
                    corner.y = Mathf.Max(corner.y, -0.35f);
                    shaped[index] = centre + Vector3.Scale(corner, size);
                }

                _inside = centre;
                for (int face = 0; face < split.Count; face += 3)
                {
                    Triangle(submesh, shaped[split[face]], shaped[split[face + 2]], shaped[split[face + 1]]);
                }
            }

            public Mesh Build(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(_vertices);
                mesh.SetNormals(_normals);
                mesh.subMeshCount = _triangles.Length;
                for (int index = 0; index < _triangles.Length; index++)
                {
                    mesh.SetTriangles(_triangles[index], index);
                }

                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
