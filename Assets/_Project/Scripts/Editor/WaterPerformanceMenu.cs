using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DigitalStory.Editor
{
    /// <summary>
    /// Plan section AD: sets two of the Crest water's own settings on the forest-clearing scene's "Water (Crest)" and saves
    /// the scene, touching nothing else. Crest is reached by its serialized field names, so this assembly needs no reference
    /// to it. Each value is one field on the WaterRenderer, so the Inspector puts it back (see Revert).
    /// </summary>
    /// <remarks>
    /// Run with -batchmode -executeMethod DigitalStory.Editor.WaterPerformanceMenu.Apply. Never run the scene builder on this
    /// scene: it would rebuild the hand-made map.
    /// </remarks>
    public static class WaterPerformanceMenu
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const string WaterObjectName = "Water (Crest)";
        private const string WaterRendererType = "WaterRenderer";

        // The water renders its surface a second time each frame to merge its depth into the camera's depth texture; only the
        // ground mist's soft edge over the rivers reads it.
        private const string DepthCopyPath = "_WriteToDepthTexture";

        // The planar reflection serves the one game camera, so it may skip frames; at 2 it renders every second frame.
        private const string SingleCameraPath = "_Reflections._RenderOnlySingleCamera";
        private const string RefreshPath = "_Reflections._RefreshPerFrames";
        private const int ReflectionRefreshFrames = 2;

        /// <summary>Applies the lighter water settings and saves the scene.</summary>
        [MenuItem("DigitalStory/Performance/Apply Lighter Water Settings")]
        public static void Apply() => Write(false, true, ReflectionRefreshFrames);

        /// <summary>Puts Raz's original water settings back and saves the scene.</summary>
        [MenuItem("DigitalStory/Performance/Revert Water Settings")]
        public static void Revert() => Write(true, false, 1);

        private static void Write(bool writesDepth, bool isSingleCamera, int refreshFrames)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject water = GameObject.Find(WaterObjectName)
                ?? throw new InvalidOperationException($"The scene has no '{WaterObjectName}'.");
            Component renderer = water.GetComponent(WaterRendererType)
                ?? throw new InvalidOperationException($"'{WaterObjectName}' has no {WaterRendererType}.");
            var serialized = new SerializedObject(renderer);
            Property(serialized, DepthCopyPath).boolValue = writesDepth;
            Property(serialized, SingleCameraPath).boolValue = isSingleCamera;
            Property(serialized, RefreshPath).intValue = refreshFrames;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Could not save {ScenePath}.");
            }

            Debug.Log($"[F010] Water settings written: depth copy {writesDepth}, single camera {isSingleCamera}, reflection every {refreshFrames} frame(s).");
        }

        private static SerializedProperty Property(SerializedObject serialized, string path)
        {
            return serialized.FindProperty(path)
                ?? throw new InvalidOperationException($"Crest's WaterRenderer has no serialized '{path}' any more. Update WaterPerformanceMenu.");
        }
    }
}
