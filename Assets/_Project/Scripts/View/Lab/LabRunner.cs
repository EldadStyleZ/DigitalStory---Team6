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

        private static readonly KeyCode[] NoteKeys = { KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L };

        private LabSession _session;
        private LabFlow _flow;

        /// <summary>Supplies what to run. Call once, before this object updates.</summary>
        public void Initialize(LabSession session, LabFlow flow)
        {
            _session = session;
            _flow = flow;
        }

        private void Update()
        {
            if (_session == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
            {
                _flow.TogglePause();
            }

            if (_flow.IsPlaying)
            {
                PlayPressedNotes();
                _session.Tick(ReadMove(), Mathf.Min(Time.deltaTime, LongestStepSeconds));
            }
        }

        private void PlayPressedNotes()
        {
            for (int note = 0; note < NoteKeys.Length; note++)
            {
                if (Input.GetKeyDown(NoteKeys[note]))
                {
                    _session.Press(note);
                }
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
