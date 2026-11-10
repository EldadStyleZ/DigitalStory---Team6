using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// Shows one menu at a time over a dark backdrop that covers the whole game, every split viewport included.
    /// Each stage opens its own menu (see <see cref="MenuRules"/>); a menu opened from another, such as Settings
    /// from Pause, goes on top with <see cref="Push"/> and closes with <see cref="Pop"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuManager : MonoBehaviour
    {
        [SerializeField, Tooltip("The game's stages.")]
        private GameFlowManager _flow;

        [SerializeField, Tooltip("The full-screen dark layer behind every menu.")]
        private GameObject _backdrop;

        [SerializeField, Tooltip("Every menu screen.")]
        private MenuScreen[] _menus;

        private readonly MenuStack _stack = new MenuStack();

        /// <summary>The menu on show, or None.</summary>
        public MenuId Top => _stack.Top;

        /// <summary>Opens a menu over the current one, hiding it.</summary>
        public void Push(MenuId menu) => _stack.Push(menu);

        /// <summary>Closes the top menu, showing the one underneath again.</summary>
        public void Pop() => _stack.Pop();

        private void Awake()
        {
            if (_flow == null || _backdrop == null)
            {
                Debug.LogError($"{name}: MenuManager is missing its flow or backdrop.", this);
            }
        }

        private void OnEnable()
        {
            _stack.Changed += Apply;
            _flow.StateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            _stack.Changed -= Apply;
            _flow.StateChanged -= HandleStateChanged;
        }

        private void Start()
        {
            _stack.ReplaceWith(MenuRules.MenuFor(_flow.State));
        }

        private void HandleStateChanged(GameState from, GameState to)
        {
            _stack.ReplaceWith(MenuRules.MenuFor(to));
        }

        private void Apply()
        {
            _backdrop.SetActive(_stack.IsAnyOpen);

            // Hide first, so two menus are never on screen together, even for a moment.
            foreach (MenuScreen menu in _menus)
            {
                if (!_stack.IsShown(menu.Id))
                {
                    menu.SetShown(false);
                }
            }

            foreach (MenuScreen menu in _menus)
            {
                if (_stack.IsShown(menu.Id))
                {
                    menu.SetShown(true);
                }
            }
        }
    }
}
