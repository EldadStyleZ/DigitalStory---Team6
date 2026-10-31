using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Pauses on P or Esc, and when the game loses focus.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Game Flow Input")]
    public sealed class GameFlowInput : MonoBehaviour
    {
        private GameFlow _flow;

        /// <summary>Supplies the flow to control. Call once, before this object updates.</summary>
        public void Initialize(GameFlow flow) => _flow = flow;

        private void Update()
        {
            if (_flow != null && (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape)))
            {
                _flow.TogglePause();
            }
        }

        // The game stops without focus anyway, so show why instead of looking frozen.
        private void OnApplicationFocus(bool hasFocus)
        {
            if (_flow != null && !hasFocus)
            {
                _flow.Pause();
            }
        }
    }
}
