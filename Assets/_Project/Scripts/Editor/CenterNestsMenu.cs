using DigitalStory.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DigitalStory.Editor
{
    /// <summary>
    /// Moves each nest's own point (where its rings glow and where the swarm collects it) onto its tree's trunk, and leaves
    /// the tree where it stands, so the rings sit round the tree. One Ctrl/Cmd + Z undoes it.
    /// </summary>
    public static class CenterNestsMenu
    {
        [MenuItem("Tools/DigitalStory/Center Nests On Their Trees")]
        private static void Center()
        {
            Undo.SetCurrentGroupName("Center Nests On Their Trees");
            int group = Undo.GetCurrentGroup();
            int moved = 0;
            foreach (NestView nest in Object.FindObjectsByType<NestView>(FindObjectsInactive.Include))
            {
                Transform tree = null;
                foreach (Transform child in nest.transform)
                {
                    if (child.GetComponentInChildren<Renderer>(true) != null)
                    {
                        tree = child;
                        break;
                    }
                }

                if (tree == null)
                {
                    continue;
                }

                Vector3 trunk = tree.position;
                Vector3 offset = new Vector3(trunk.x - nest.transform.position.x, 0f, trunk.z - nest.transform.position.z);
                if (offset.sqrMagnitude < 0.01f)
                {
                    continue;
                }

                Undo.RecordObject(nest.transform, "Center Nests On Their Trees");
                Undo.RecordObject(tree, "Center Nests On Their Trees");
                Vector3 treeWorld = tree.position;
                Vector3 position = nest.transform.position + offset;
                Terrain terrain = Terrain.activeTerrain;
                if (terrain != null)
                {
                    position.y = terrain.SampleHeight(position) + terrain.GetPosition().y;
                }

                nest.transform.position = position;
                tree.position = treeWorld;
                moved++;
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"Centred {moved} nests on their trees. Save the scene to keep it.");
        }
    }
}
