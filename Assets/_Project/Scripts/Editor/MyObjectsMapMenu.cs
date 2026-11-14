using System.Collections.Generic;
using DigitalStory.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DigitalStory.Editor
{
    /// <summary>
    /// Fills the programmer's random-map scenery (SceneryView) with Raz's objects: the pines, dead trees, bushes and ruins
    /// together with the low poly kit (rocks, boulders, stumps, logs, ferns, mushrooms, standing stones, the well, broken
    /// pillars, rubble, barrels, crates and straw hives). Each match then scatters these, off the rivers. The fixed props
    /// inside the clearing and the old camps are removed, since the random map now places the objects there.
    /// </summary>
    public static class MyObjectsMapMenu
    {
        private const string ModelFolder = "Assets/_Project/Art/prefabs/";
        private const string KitFolder = "Assets/_Project/Art/Generated/Prefabs/";
        private const int Count = 90;
        private const float BlockerLeastHeight = 2f;

        // Name, height in metres, and how often it is picked.
        private static readonly (string Name, float Height, int Weight)[] Objects =
        {
            ("Tree_Pine", 9f, 10), ("Tree_Dead", 6.5f, 4), ("Bush_Cone", 1.4f, 5), ("Bush_Round", 1.2f, 5),
            ("Bush_Flower", 1f, 4), ("Bush_Lavender", 0.9f, 4), ("Ruin_Arcade", 4.5f, 1), ("Ruin_Arch_Gate", 5f, 1),
            ("Ruin_House_B", 4.5f, 1), ("Ruin_Wall_Window", 3.5f, 2), ("Shack_Abandoned_A", 3f, 1), ("Shack_Abandoned_B", 3f, 1),
            ("Kit_Rock_A", 0.7f, 4), ("Kit_Rock_B", 1f, 3), ("Kit_Rock_C", 0.6f, 3), ("Kit_Rock_Flat", 0.3f, 2),
            ("Kit_Boulder", 2.2f, 2), ("Kit_Pebbles", 0.2f, 2), ("Kit_Stump", 0.6f, 3), ("Kit_Stump_Tall", 1.4f, 2),
            ("Kit_Log", 0.6f, 3), ("Kit_Log_Pile", 0.9f, 1), ("Kit_Branch", 0.3f, 2), ("Kit_Fern", 0.8f, 5),
            ("Kit_Mushrooms", 0.3f, 3), ("Kit_Mushroom_Red", 0.35f, 3), ("Kit_Mushroom_Big", 1f, 2), ("Kit_StandingStone", 2.4f, 2),
            ("Kit_Well", 2.4f, 1), ("Kit_Pillar_Broken", 2f, 2), ("Kit_Rubble", 0.5f, 2), ("Kit_Barrel", 0.8f, 1),
            ("Kit_Crate", 0.65f, 1), ("Kit_Skep", 0.8f, 1), ("Kit_Grave", 0.9f, 1),
        };

        [MenuItem("Tools/DigitalStory/Use My Objects In The Random Map")]
        private static void Build()
        {
            SceneryView scenery = Object.FindAnyObjectByType<SceneryView>(FindObjectsInactive.Include);
            if (scenery == null)
            {
                EditorUtility.DisplayDialog("My Objects", "The scene has no SceneryView (the random map's scenery).", "OK");
                return;
            }

            Undo.SetCurrentGroupName("Use My Objects In The Random Map");
            int group = Undo.GetCurrentGroup();
            Transform parent = scenery.transform;
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                Undo.DestroyObjectImmediate(parent.GetChild(index).gameObject);
            }

            var pool = new List<int>();
            for (int index = 0; index < Objects.Length; index++)
            {
                for (int weight = 0; weight < Objects[index].Weight; weight++)
                {
                    pool.Add(index);
                }
            }

            var random = new System.Random(7);
            var props = new List<Transform>();
            var footprints = new List<float>();
            for (int count = 0; count < Count; count++)
            {
                (string name, float height, int _) = Objects[pool[random.Next(pool.Count)]];
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + name + ".prefab")
                    ?? AssetDatabase.LoadAssetAtPath<GameObject>(KitFolder + name + ".prefab");
                if (prefab == null)
                {
                    continue;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                Undo.RegisterCreatedObjectUndo(instance, "Use My Objects In The Random Map");
                Quaternion turn = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                instance.transform.SetPositionAndRotation(Vector3.zero, turn * prefab.transform.localRotation);
                float target = height * (0.85f + ((float)random.NextDouble() * 0.3f));
                float current = Measure(instance.transform).size.y;
                if (current > 1e-4f)
                {
                    instance.transform.localScale *= target / current;
                }

                Bounds bounds = Measure(instance.transform);
                instance.transform.position += new Vector3(0f, -bounds.min.y, 0f);
                foreach (Transform part in instance.GetComponentsInChildren<Transform>())
                {
                    part.gameObject.isStatic = false;
                }

                props.Add(instance.transform);
                float x = Mathf.Max(Mathf.Abs(bounds.min.x), Mathf.Abs(bounds.max.x));
                float z = Mathf.Max(Mathf.Abs(bounds.min.z), Mathf.Abs(bounds.max.z));
                footprints.Add(Mathf.Sqrt((x * x) + (z * z)));
            }

            var serialized = new SerializedObject(scenery);
            SerializedProperty propList = serialized.FindProperty("_props");
            propList.arraySize = props.Count;
            SerializedProperty footList = serialized.FindProperty("_footprints");
            footList.arraySize = footprints.Count;
            for (int index = 0; index < props.Count; index++)
            {
                propList.GetArrayElementAtIndex(index).objectReferenceValue = props[index];
                footList.GetArrayElementAtIndex(index).floatValue = footprints[index];
            }

            serialized.FindProperty("_blockerLeastHeight").floatValue = BlockerLeastHeight;
            serialized.ApplyModifiedProperties();

            // The fixed props inside the clearing and the old camps would stand in the random map's way.
            GameObject dressing = GameObject.Find("Set Dressing");
            if (dressing != null)
            {
                foreach (string fixedGroup in new[] { "Clearing", "Beekeeper Camps" })
                {
                    Transform found = dressing.transform.Find(fixedGroup);
                    if (found != null)
                    {
                        Undo.DestroyObjectImmediate(found.gameObject);
                    }
                }
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scenery.gameObject.scene);
            Debug.Log($"The random map now scatters {props.Count} of Raz's objects. Save the scene to keep it.");
        }

        private static Bounds Measure(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(root.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }
    }
}
