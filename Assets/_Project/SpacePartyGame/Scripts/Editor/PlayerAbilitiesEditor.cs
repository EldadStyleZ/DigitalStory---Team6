using UnityEditor;
using UnityEngine;

namespace SpacePartyGame.Editor
{
    /// <summary>
    /// Lets the blow's area be tuned where it is seen: its numbers sit in the player's Inspector, and with a player
    /// selected the Scene view has handles to drag its reach, inner gap and width. Works in Play Mode too, and the
    /// changes are kept afterwards because they are saved on the shared Blow Settings asset.
    /// </summary>
    [CustomEditor(typeof(PlayerAbilities))]
    public sealed class PlayerAbilitiesEditor : UnityEditor.Editor
    {
        private static readonly string[] AreaFields =
        {
            "_coneAngle", "_reach", "_innerRadius", "_hitLeeway", "_showAreaWhileCharging", "_alwaysShowArea", "_previewAlpha",
        };

        private static bool _showArea = true;
        private SerializedObject _settings;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            BlowSettings settings = Settings();

            if (settings == null)
            {
                return;
            }

            if (_settings == null || _settings.targetObject != settings)
            {
                _settings = new SerializedObject(settings);
            }

            EditorGUILayout.Space();
            _showArea = EditorGUILayout.Foldout(_showArea, "Blow area (shared Blow Settings)", true);

            if (!_showArea)
            {
                return;
            }

            EditorGUILayout.HelpBox("Changes both players. Drag the magenta handles in the Scene view, or tick Always Show Area to tune it in the Game view.", MessageType.None);
            _settings.Update();

            foreach (string field in AreaFields)
            {
                EditorGUILayout.PropertyField(_settings.FindProperty(field));
            }

            if (_settings.ApplyModifiedProperties())
            {
                SceneView.RepaintAll();
            }
        }

        private void OnSceneGUI()
        {
            var abilities = (PlayerAbilities)target;
            BlowSettings settings = Settings();

            if (settings == null)
            {
                return;
            }

            var serialized = new SerializedObject(settings);
            SerializedProperty angle = serialized.FindProperty("_coneAngle");
            SerializedProperty reach = serialized.FindProperty("_reach");
            SerializedProperty inner = serialized.FindProperty("_innerRadius");

            Vector2 origin = abilities.BlowOrigin;
            Vector2 aim = abilities.AimDirection.sqrMagnitude > 1e-6f ? abilities.AimDirection.normalized : Vector2.right;
            Vector3 upperEdge = Quaternion.Euler(0f, 0f, angle.floatValue * 0.5f) * aim;
            float size = HandleUtility.GetHandleSize(origin) * 0.08f;

            Handles.color = Color.magenta;
            Handles.DrawWireArc(origin, Vector3.forward, Quaternion.Euler(0f, 0f, -angle.floatValue * 0.5f) * aim, angle.floatValue, reach.floatValue);
            Handles.DrawWireArc(origin, Vector3.forward, Quaternion.Euler(0f, 0f, -angle.floatValue * 0.5f) * aim, angle.floatValue, inner.floatValue);

            EditorGUI.BeginChangeCheck();

            // Reach and inner gap: dots on the aim line. Width: a dot on the cone's upper corner.
            Vector3 reachPoint = Handles.Slider((Vector3)origin + (Vector3)aim * reach.floatValue, aim, size, Handles.DotHandleCap, 0f);
            Vector3 innerPoint = Handles.Slider((Vector3)origin + (Vector3)aim * inner.floatValue, aim, size, Handles.DotHandleCap, 0f);
            Vector3 cornerPoint = Handles.FreeMoveHandle((Vector3)origin + upperEdge * reach.floatValue, size, Vector3.zero, Handles.DotHandleCap);

            Handles.Label((Vector3)origin + (Vector3)aim * reach.floatValue, $"  reach {reach.floatValue:0.00}");
            Handles.Label((Vector3)origin + upperEdge * reach.floatValue, $"  {angle.floatValue:0}°");

            if (EditorGUI.EndChangeCheck())
            {
                reach.floatValue = Mathf.Max(0.1f, Vector2.Dot((Vector2)reachPoint - origin, aim));
                inner.floatValue = Mathf.Clamp(Vector2.Dot((Vector2)innerPoint - origin, aim), 0f, reach.floatValue - 0.05f);
                angle.floatValue = Mathf.Clamp(Vector2.Angle(aim, (Vector2)cornerPoint - origin) * 2f, 10f, 180f);
                serialized.ApplyModifiedProperties();
            }
        }

        private BlowSettings Settings() => serializedObject.FindProperty("_blowSettings").objectReferenceValue as BlowSettings;
    }
}
