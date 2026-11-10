// Lives in Assets/_Project/Scripts/Editor (editor-only assembly DigitalStory.Editor).
// Use: select one texture in the Project window, then Tools > Set Skybox From Selected Texture.
//
// We only have one face of the space skybox, so the same image goes on all six sides of a
// Skybox/6 Sided material. Done through the Editor API so no scene or asset file is hand-edited.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DigitalStory.Editor
{
    public static class SkyboxFromTexture
    {
        private const string MenuPath = "Tools/Set Skybox From Selected Texture";

        private static readonly string[] FaceProperties =
        {
            "_FrontTex", "_BackTex", "_LeftTex", "_RightTex", "_UpTex", "_DownTex"
        };

        [MenuItem(MenuPath, true)]
        private static bool CanApply()
        {
            return Selection.activeObject is Texture2D;
        }

        [MenuItem(MenuPath)]
        private static void Apply()
        {
            var texture = (Texture2D)Selection.activeObject;
            var texturePath = AssetDatabase.GetAssetPath(texture);

            var shader = Shader.Find("Skybox/6 Sided");
            if (shader == null)
            {
                Debug.LogError("SkyboxFromTexture: shader 'Skybox/6 Sided' was not found.");
                return;
            }

            // Clamp stops the opposite edge of the image bleeding in along the cube edges.
            if (AssetImporter.GetAtPath(texturePath) is TextureImporter importer && importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }

            var folder = System.IO.Path.GetDirectoryName(texturePath).Replace('\\', '/');
            var materialPath = folder + "/" + texture.name + "_Skybox.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.shader = shader;
            foreach (var property in FaceProperties)
            {
                material.SetTexture(property, texture);
            }

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            RenderSettings.skybox = material;
            DynamicGI.UpdateEnvironment();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("SkyboxFromTexture: skybox set to " + materialPath);
        }
    }
}
