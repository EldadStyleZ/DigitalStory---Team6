using DigitalStory.Data;
using UnityEditor;
using UnityEngine;

namespace DigitalStory.Editor
{
    /// <summary>
    /// Opens the random map up to 100 by 100 metres from the middle (200 m across): the core may roam that far, and the
    /// programmer's map rules spread the hives, spawn points, nest trees and scenery over the wider ground. The first two
    /// nests stay close, for the first lesson. Through the Editor, so the assets stay valid; one Ctrl/Cmd + Z undoes it.
    /// </summary>
    public static class OpenMapHundredMenu
    {
        [MenuItem("Tools/DigitalStory/Open The Random Map To 100")]
        private static void Open()
        {
            Set<CoreMoveConfig>(("PlayAreaHalfSize", 100f));
            Set<LayoutConfig>(
                ("ArenaHalfSize", 100f),
                ("HiveNearest", 15f),
                ("HiveFarthest", 40f),
                ("HiveLeastSeparation", 30f),
                ("SpawnFromHiveNearest", 35f),
                ("SpawnFromHiveFarthest", 50f),
                ("SpawnStartClearance", 40f),
                ("SpawnLeastSpacing", 20f),
                ("NestInsideHalfSize", 90f),
                ("NestLeastSpacing", 12f),
                ("SceneryInnerHalfSize", 12f),
                ("SceneryFootprintShare", 1f),
                ("SceneryLeastSpacing", 3f));
            Set<NestConfig>(("LaterNestsNearest", 20f), ("LaterNestsFarthest", 85f));

            // Raz's swarm: the looser, organic springs and the longer body of his own commit.
            Set<SwarmConfig>(
                ("RiverSpring", 40f),
                ("RiverDamping", 0.6f),
                ("WobbleDamping", 0.3f),
                ("RiverLengthMeters", 11f),
                ("StandBlendSeconds", 0.75f));

            // Grass well past the arena's edge.
            foreach (MonoBehaviour field in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (field == null || field.GetType().Name != "GrassField")
                {
                    continue;
                }

                var grass = new SerializedObject(field);
                grass.FindProperty("_coverHalfSize").floatValue = 200f;
                grass.ApplyModifiedProperties();
                field.SendMessage("MarkDirty", SendMessageOptions.DontRequireReceiver);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(field.gameObject.scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("The random map now opens to 100 m from the middle.");
        }

        private static void Set<T>(params (string Name, float Value)[] values) where T : ScriptableObject
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                var serialized = new SerializedObject(asset);
                foreach ((string name, float value) in values)
                {
                    SerializedProperty property = serialized.FindProperty($"<{name}>k__BackingField");
                    if (property != null)
                    {
                        property.floatValue = value;
                    }
                    else
                    {
                        Debug.LogWarning($"{typeof(T).Name} has no {name}.");
                    }
                }

                Undo.RecordObject(asset, "Open The Random Map To 100");
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
            }
        }
    }
}
