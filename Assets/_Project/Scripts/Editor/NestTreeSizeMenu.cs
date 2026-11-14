using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DigitalStory.Editor
{
    public static class NestTreeSizeMenu
    {
        private const float PineHeight = 9f;

        [MenuItem("Tools/DigitalStory/Make Nest Trees Pine Size")]
        public static void Grow()
        {
            var nests = GameObject.Find("Nests");
            if (nests == null) { Debug.LogWarning("No Nests object in the scene."); return; }
            var count = 0;
            foreach (Transform nest in nests.transform)
            foreach (Transform tree in nest)
            {
                if (!tree.name.StartsWith("WaspNestTree")) continue;
                var renderers = tree.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                if (bounds.size.y < 0.01f) continue;
                Undo.RecordObject(tree, "Grow nest tree");
                tree.localScale *= PineHeight / bounds.size.y;
                count++;
            }
            EditorSceneManager.MarkSceneDirty(nests.scene);
            Debug.Log($"{count} nest trees now stand {PineHeight} m tall, like the pines. Save the scene to keep it.");
        }
    }
}
