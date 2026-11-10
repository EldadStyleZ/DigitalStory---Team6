using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// A screen that is shown during one stage of the game and hidden otherwise, without the menu backdrop, such as
    /// the countdown. Menus use <see cref="MenuScreen"/> instead. Subclasses fill in what the screen does while
    /// shown.
    /// </summary>
    public abstract class StageScreen : MonoBehaviour
    {
        [SerializeField, Tooltip("The game's stages.")]
        private GameFlowManager _flow;

        [SerializeField, Tooltip("The panel to show and hide; a child, so this script keeps running.")]
        private GameObject _panel;

        /// <summary>The game's stages.</summary>
        protected GameFlowManager Flow => _flow;

        /// <summary>The stage this screen belongs to.</summary>
        protected abstract GameState ShownIn { get; }

        protected virtual void Awake()
        {
            if (_flow == null || _panel == null)
            {
                Debug.LogError($"{name}: {GetType().Name} is missing its flow or panel.", this);
            }
        }

        protected virtual void OnEnable()
        {
            _flow.StateChanged += HandleStateChanged;
        }

        protected virtual void OnDisable()
        {
            _flow.StateChanged -= HandleStateChanged;
        }

        protected virtual void Start()
        {
            SetShown(_flow.State == ShownIn);
        }

        /// <summary>Called each time the screen appears.</summary>
        protected virtual void OnShown()
        {
        }

        private void HandleStateChanged(GameState from, GameState to)
        {
            SetShown(to == ShownIn);
        }

        private void SetShown(bool shown)
        {
            _panel.SetActive(shown);

            if (shown)
            {
                OnShown();
            }
        }
    }
}
