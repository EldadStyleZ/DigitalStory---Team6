using System;
using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DigitalStory.Editor
{
    /// <summary>
    /// Opens up F-010's map in one step, through the Editor so the assets stay valid: the core may roam farther, the nests
    /// that open after a wave move out to the new range so each one is a trip, and the beekeepers' spawn points move out
    /// to the new edge. The nests open at the start stay close for the first lesson. Running it again changes nothing.
    /// The new layout is checked with NestLayout, the same check the game runs, and one Ctrl/Cmd + Z undoes it all.
    /// </summary>
    public static class OpenUpMapMenu
    {
        private const float PlayAreaHalfSize = 100f;
        private const float LaterNestsNearest = 45f;
        private const float LaterNestsFarthest = 90f;

        [MenuItem("Tools/DigitalStory/Open Up The Map (F-010)")]
        private static void OpenUp()
        {
            CoreMoveConfig coreMove = LoadOne<CoreMoveConfig>();
            NestConfig nestConfig = LoadOne<NestConfig>();
            if (coreMove == null || nestConfig == null)
            {
                EditorUtility.DisplayDialog("Open Up The Map", "CoreMoveConfig or NestConfig was not found.", "OK");
                return;
            }

            GameObject nestsRoot = GameObject.Find("Nests");
            GameObject spawnRoot = GameObject.Find("Spawn Points");
            if (nestsRoot == null)
            {
                EditorUtility.DisplayDialog("Open Up The Map", "Open the F-010 scene first: it has no Nests object.", "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();

            float oldHalf = coreMove.PlayAreaHalfSize;
            float oldNearest = nestConfig.LaterNestsNearest;
            float oldFarthest = nestConfig.LaterNestsFarthest;

            var nests = new List<Transform>();
            foreach (Transform nest in nestsRoot.transform)
            {
                nests.Add(nest);
            }

            Terrain terrain = Terrain.activeTerrain;
            Undo.RecordObjects(nests.ToArray(), "Open Up The Map");
            for (int index = nestConfig.OpenAtStart; index < nests.Count; index++)
            {
                Vector3 position = nests[index].position;
                var flat = new Vector2(position.x, position.z);
                float distance = flat.magnitude;
                if (distance < 0.01f)
                {
                    continue;
                }

                float share = Mathf.InverseLerp(oldNearest, oldFarthest, distance);
                float moved = Mathf.Lerp(LaterNestsNearest, LaterNestsFarthest, share);
                flat *= moved / distance;
                position.x = flat.x;
                position.z = flat.y;
                if (terrain != null)
                {
                    position.y = terrain.SampleHeight(position) + terrain.GetPosition().y;
                }

                nests[index].position = position;
            }

            if (spawnRoot != null && oldHalf > 0.01f)
            {
                var spawns = new List<Transform>();
                foreach (Transform spawn in spawnRoot.transform)
                {
                    spawns.Add(spawn);
                }

                Undo.RecordObjects(spawns.ToArray(), "Open Up The Map");
                float factor = PlayAreaHalfSize / oldHalf;
                foreach (Transform spawn in spawns)
                {
                    Vector3 position = spawn.position;
                    position.x *= factor;
                    position.z *= factor;
                    spawn.position = position;
                }
            }

            SetFloat(coreMove, "<PlayAreaHalfSize>k__BackingField", PlayAreaHalfSize);
            SetFloat(nestConfig, "<LaterNestsNearest>k__BackingField", LaterNestsNearest);
            SetFloat(nestConfig, "<LaterNestsFarthest>k__BackingField", LaterNestsFarthest);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(nestsRoot.scene);
            Undo.CollapseUndoOperations(group);

            Check(nests, nestConfig);
        }

        private static void Check(List<Transform> nests, NestConfig config)
        {
            var nestPositions = new Vector3[nests.Count];
            for (int index = 0; index < nests.Count; index++)
            {
                nestPositions[index] = nests[index].position;
            }

            var hives = new List<Vector3>();
            foreach (string name in new[] { "Hive A", "Hive B" })
            {
                GameObject hive = GameObject.Find(name);
                if (hive != null)
                {
                    hives.Add(hive.transform.position);
                }
            }

            try
            {
                NestLayout.Check(nestPositions, hives, config, PlayAreaHalfSize);
                Debug.Log($"The map is open: the core roams {PlayAreaHalfSize} m from the middle, and the later nests stand {LaterNestsNearest}-{LaterNestsFarthest} m out. Save the scene to keep it.");
            }
            catch (ArgumentException problem)
            {
                Debug.LogError("The opened map breaks a nest rule, so the game will not start with it: " + problem.Message +
                    " Move that nest by hand, or undo with Ctrl/Cmd + Z.");
            }
        }

        private static void SetFloat(UnityEngine.Object asset, string property, float value)
        {
            var serialized = new SerializedObject(asset);
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null)
            {
                Debug.LogError($"{asset.name} has no field {property}.");
                return;
            }

            field.floatValue = value;
            serialized.ApplyModifiedProperties();
        }

        private static T LoadOne<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
        }
    }
}
