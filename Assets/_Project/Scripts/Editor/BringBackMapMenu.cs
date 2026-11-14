using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DigitalStory.Editor
{
    /// <summary>
    /// Brings Raz's map (terrain with rivers, grass, water, the forest and props, mist and bridges) from
    /// raz-map-source.unity into the open scene, which keeps the programmer's random placement of hives, nests and spawns.
    /// The plain ground it replaces is switched off. One Ctrl/Cmd + Z does not undo the move; reload the scene instead.
    /// </summary>
    public static class BringBackMapMenu
    {
        private const string SourcePath = "Assets/_Project/Scenes/raz-map-source.unity";
        private static readonly string[] Roots = { "Terrain", "Grass Field", "Water (Crest)", "Set Dressing", "Ground Mist", "Bridges" };

        [MenuItem("Tools/DigitalStory/Bring Back My Map")]
        private static void Bring()
        {
            // Load the programmer's scene fresh from disk, now that all its scripts are compiled, so none of its links drop.
            Scene target = EditorSceneManager.OpenScene("Assets/_Project/Scenes/eldad-lab-2-scene.unity", OpenSceneMode.Single);
            Scene source = EditorSceneManager.OpenScene(SourcePath, OpenSceneMode.Additive);
            int moved = 0;
            foreach (GameObject root in source.GetRootGameObjects())
            {
                if (System.Array.IndexOf(Roots, root.name) < 0)
                {
                    continue;
                }

                foreach (GameObject old in target.GetRootGameObjects())
                {
                    if (old.name == root.name)
                    {
                        Object.DestroyImmediate(old);
                    }
                }

                SceneManager.MoveGameObjectToScene(root, target);
                moved++;
            }

            EditorSceneManager.CloseScene(source, true);
            SceneManager.SetActiveScene(target);

            foreach (GameObject root in target.GetRootGameObjects())
            {
                if (root.name == "Ground")
                {
                    root.SetActive(false);
                }
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.06f, 0.09f, 0.11f);
            RenderSettings.fogDensity = 0.019f;

            EditorSceneManager.MarkSceneDirty(target);
            Debug.Log($"Brought back {moved} parts of the map. Save the scene to keep them.");
        }
    }
}
