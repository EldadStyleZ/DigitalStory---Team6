using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// A menu or overlay screen. <see cref="MenuManager"/> decides when it shows: only one menu is on screen at a
    /// time, always over the dark backdrop.
    /// </summary>
    public abstract class MenuScreen : MonoBehaviour
    {
        [SerializeField, Tooltip("The game's stages.")]
        private GameFlowManager _flow;

        [SerializeField, Tooltip("The panel to show and hide; a child, so this script keeps running.")]
        private GameObject _panel;

        /// <summary>Which menu this is.</summary>
        public abstract MenuId Id { get; }

        /// <summary>The game's stages.</summary>
        protected GameFlowManager Flow => _flow;

        /// <summary>Shows or hides the menu.</summary>
        public void SetShown(bool shown)
        {
            bool wasShown = _panel.activeSelf;
            _panel.SetActive(shown);

            if (shown && !wasShown)
            {
                OnShown();
            }
        }

        /// <summary>Called each time the menu appears.</summary>
        protected virtual void OnShown()
        {
        }

        protected virtual void Awake()
        {
            if (_flow == null || _panel == null)
            {
                Debug.LogError($"{name}: {GetType().Name} is missing its flow or panel.", this);
            }
        }
    }
}
