using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>Shows the menu screen that belongs to the current phase of the game.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("DigitalStory/Menu Screens View")]
    public sealed class MenuScreensView : MonoBehaviour
    {
        private GameFlow _flow;
        private HudConfig _hud;
        private Dictionary<GamePhase, IMenuScreen> _screens;
        private VisualElement _overlay;
        private MenuCard _card;

        /// <summary>True while a menu covers the game, for tests and debugging.</summary>
        public bool IsShowing => _overlay != null && _overlay.style.display == DisplayStyle.Flex;

        /// <summary>The heading on the shown screen, for tests and debugging.</summary>
        public string ShownTitle => IsShowing ? _card.Title : null;

        /// <summary>The menu card, for tests and debugging.</summary>
        public VisualElement Card => _card?.Root;

        /// <summary>Supplies the flow to follow and the words to show. Call once, before this object is enabled.</summary>
        public void Initialize(
            GameFlow flow,
            NightSession session,
            MenuTextConfig texts,
            TownArtConfig art,
            HudConfig hud)
        {
            _flow = flow;
            _hud = hud;
            _screens = new Dictionary<GamePhase, IMenuScreen>
            {
                [GamePhase.Title] = new TitleScreen(texts, art, hud, session, flow.Begin),
            };
        }

        private void OnEnable()
        {
            if (_flow != null)
            {
                _flow.Changed += HandleFlowChanged;
            }
        }

        private void Start()
        {
            // GameRoot has already reported the problem if nothing was supplied.
            if (_flow != null)
            {
                BuildOverlay(GetComponent<UIDocument>().rootVisualElement);
                HandleFlowChanged(_flow.Phase);
            }
        }

        private void OnDisable()
        {
            // Unity also calls this when the scene unloads before Initialize ran, so _flow can be null.
            if (_flow != null)
            {
                _flow.Changed -= HandleFlowChanged;
            }
        }

        private void BuildOverlay(VisualElement root)
        {
            _overlay = new VisualElement();
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0f;
            _overlay.style.right = 0f;
            _overlay.style.top = 0f;
            _overlay.style.bottom = 0f;
            _overlay.style.alignItems = Align.Center;
            _overlay.style.justifyContent = Justify.Center;
            _overlay.style.backgroundColor = _hud.OverlayColor;
            _card = new MenuCard(_overlay, _hud);
            root.Add(_overlay);
        }

        private void HandleFlowChanged(GamePhase phase)
        {
            _card.Clear();

            if (!_screens.TryGetValue(phase, out IMenuScreen screen))
            {
                _overlay.style.display = DisplayStyle.None;
                return;
            }

            _overlay.style.display = DisplayStyle.Flex;
            screen.Fill(_card);
        }
    }
}
