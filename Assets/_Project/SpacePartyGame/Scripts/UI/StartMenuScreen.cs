using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpacePartyGame
{
    /// <summary>
    /// The title and the Start button. The button is selected when the menu appears, so gamepad A (Submit) presses
    /// it as well as a mouse click; Start and Space are not Submit, so they do not.
    /// </summary>
    public sealed class StartMenuScreen : StageScreen
    {
        [SerializeField, Tooltip("Opens the join screen.")]
        private Button _startButton;

        protected override GameState ShownIn => GameState.StartMenu;

        protected override void OnEnable()
        {
            base.OnEnable();
            _startButton.onClick.AddListener(Flow.OpenJoin);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _startButton.onClick.RemoveListener(Flow.OpenJoin);
        }

        protected override void OnShown()
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_startButton.gameObject);
            }
        }
    }
}
