using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Reads the keyboard and runs the lab every frame.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Lab/Lab Runner")]
    public sealed class LabRunner : MonoBehaviour
    {
        // A long frame would otherwise carry things straight through a wall.
        private const float LongestStepSeconds = 0.05f;

        private LabSession _session;

        /// <summary>Supplies what to run. Call once, before this object updates.</summary>
        public void Initialize(LabSession session) => _session = session;

        private void Update()
        {
            if (_session != null)
            {
                _session.Tick(ReadMove(), Mathf.Min(Time.deltaTime, LongestStepSeconds));
            }
        }

        private static Vector2 ReadMove()
        {
            float x = Axis(KeyCode.RightArrow, KeyCode.D) - Axis(KeyCode.LeftArrow, KeyCode.A);
            float y = Axis(KeyCode.UpArrow, KeyCode.W) - Axis(KeyCode.DownArrow, KeyCode.S);
            return new Vector2(x, y);
        }

        private static float Axis(KeyCode arrow, KeyCode letter)
        {
            return Input.GetKey(arrow) || Input.GetKey(letter) ? 1f : 0f;
        }
    }
}
