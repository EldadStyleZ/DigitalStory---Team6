using System;
using UnityEditor;
using UnityEngine;

namespace DigitalStory.Editor
{
    /// <summary>
    /// Sets the Player's Active Input Handling to Both, so the Input Manager (the keyboard and mouse code) and the Input System
    /// package (the controller and its rumble) both work. The setting is changed through Unity, never by editing the file.
    /// </summary>
    public static class InputHandlingSetting
    {
        // PlayerSettings' activeInputHandler: 0 the Input Manager, 1 the Input System package, 2 both.
        private const int Both = 2;
        private const string PropertyName = "activeInputHandler";
        private const string SettingsPath = "ProjectSettings/ProjectSettings.asset";

        /// <summary>Sets Active Input Handling to Both and saves the project settings. Restart the Editor afterwards.</summary>
        /// <exception cref="InvalidOperationException">The Player settings or the property cannot be found.</exception>
        [MenuItem("DigitalStory/Set Active Input Handling to Both")]
        public static void SetBoth()
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(SettingsPath);
            if (assets == null || assets.Length == 0 || assets[0] == null)
            {
                throw new InvalidOperationException($"Could not load the Player settings from {SettingsPath}.");
            }

            var serialized = new SerializedObject(assets[0]);
            SerializedProperty handler = serialized.FindProperty(PropertyName)
                ?? throw new InvalidOperationException($"The Player settings have no property named {PropertyName}.");

            if (handler.intValue == Both)
            {
                Debug.Log("[F010] Active Input Handling is already Both.");
                return;
            }

            handler.intValue = Both;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("[F010] Active Input Handling set to Both. Restart the Editor so it takes effect.");
        }
    }
}
