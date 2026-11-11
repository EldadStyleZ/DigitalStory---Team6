using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpacePartyGame
{
    /// <summary>
    /// The title and the Start button. The button is selected when the menu appears, so gamepad A (Submit) presses
    /// it as well as a mouse click; Start and Space are not Submit, so they do not.
    /// </summary>
    public sealed class StartMenuScreen : MenuScreen
    {
        [SerializeField, Tooltip("Opens the join screen, or starts the countdown when the join screen is off.")]
        private Button _startButton;

        public override MenuId Id => MenuId.StartMenu;

        private void OnEnable()
        {
            // The join screen is optional now; StartFromMenu opens it only when it is on.
            // _startButton.onClick.AddListener(Flow.OpenJoin);
            _startButton.onClick.AddListener(Flow.StartFromMenu);
        }

        private void OnDisable()
        {
            // _startButton.onClick.RemoveListener(Flow.OpenJoin);
            _startButton.onClick.RemoveListener(Flow.StartFromMenu);
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
