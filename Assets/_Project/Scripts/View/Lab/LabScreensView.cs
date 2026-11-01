using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>Shows the pause, death and victory screens, including the choice after dying.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("DigitalStory/Lab/Lab Screens View")]
    public sealed class LabScreensView : MonoBehaviour
    {
        private LabFlow _flow;
        private LabSession _session;
        private LabTextConfig _texts;
        private HudConfig _hud;
        private VisualElement _overlay;
        private MenuCard _card;

        /// <summary>The heading on the shown screen, for tests and debugging.</summary>
        public string ShownTitle => _overlay != null && _overlay.style.display == DisplayStyle.Flex ? _card.Title : null;

        /// <summary>The screen's card, for tests and debugging.</summary>
        public VisualElement Card => _card?.Root;

        /// <summary>Supplies the flow to follow and the words to show. Call once, before this object starts.</summary>
        public void Initialize(LabFlow flow, LabSession session, LabTextConfig texts, HudConfig hud)
        {
            _flow = flow;
            _session = session;
            _texts = texts;
            _hud = hud;
        }

        private void Start()
        {
            // LabRoot has already reported the problem if nothing was supplied.
            if (_flow == null)
            {
                return;
            }

            BuildOverlay(GetComponent<UIDocument>().rootVisualElement);
            _flow.Changed += HandleFlowChanged;
            HandleFlowChanged(_flow.Phase);
        }

        private void OnDestroy()
        {
            // Unity also calls this when the scene unloads before Start ran, so _overlay can be null.
            if (_overlay != null)
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

        private void HandleFlowChanged(LabPhase phase)
        {
            _card.Clear();

            switch (phase)
            {
                case LabPhase.Paused:
                    _card.AddHeading(_texts.PausedTitle);
                    _card.AddMainButton("resumeButton", _texts.ResumeButton, _flow.TogglePause);
                    break;
                case LabPhase.Dead:
                    _card.AddHeading(_texts.DeadTitle);
                    _card.AddParagraph(_texts.DeadMessage);
                    _card.AddStat($"{_texts.LevelLabel} {_session.Levels.Level}");
                    _card.AddMainButton("continueButton", _texts.ContinueButton, ContinueRun);
                    _card.AddSecondButton("newGameButton", _texts.NewGameButton, StartOver);
                    break;
                case LabPhase.Won:
                    _card.AddHeading(_texts.WonTitle);
                    _card.AddParagraph(_texts.WonMessage);
                    _card.AddMainButton("newGameButton", _texts.NewGameButton, StartOver);
                    break;
                default:
                    _overlay.style.display = DisplayStyle.None;
                    return;
            }

            // The HUD builds itself in its own Start, so its place among the children is not fixed.
            _overlay.BringToFront();
            _overlay.style.display = DisplayStyle.Flex;
        }

        private void ContinueRun()
        {
            _session.Continue();
            _flow.Revive();
        }

        // A new scene guarantees a clean run, with nothing left to reset by hand.
        private void StartOver() => SceneManager.LoadScene(gameObject.scene.buildIndex);
    }
}
