using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>Shows the choice between two creatures and the pause, death and victory screens.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("DigitalStory/Lab/Lab Screens View")]
    public sealed class LabScreensView : MonoBehaviour
    {
        private const float NameFontSize = 20f;
        private const float BodyFontSize = 13f;
        private const float OptionGap = 8f;
        private const float OptionPadding = 12f;
        private const float OptionRadius = 8f;
        private const float LineGap = 6f;
        private const float ButtonFontSize = 14f;
        private const float ButtonPadding = 8f;
        private const float PictureSize = 96f;

        private static readonly string[] OptionButtons = { "chooseFirst", "chooseSecond" };

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
                case LabPhase.Choosing:
                    ShowChoice(_session.WaitingChoice);
                    break;
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

        private void ShowChoice(CreatureChoice choice)
        {
            _card.AddHeading(_texts.ChoiceTitle);
            _card.AddParagraph($"{choice.Problem}. {_texts.ChoiceWarning}");

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = LineGap;

            for (int option = 0; option < choice.Options.Count; option++)
            {
                row.Add(NewOption(choice.Options[option], option));
            }

            _card.Root.Add(row);
        }

        // Neither button takes focus, so Space or Enter can never make a pick that lasts the whole run.
        private VisualElement NewOption(CreatureOffer offer, int option)
        {
            var panel = new VisualElement();
            panel.style.flexGrow = 1f;
            panel.style.flexBasis = 0f;
            panel.style.marginRight = option == 0 ? OptionGap : 0f;
            panel.style.paddingLeft = OptionPadding;
            panel.style.paddingRight = OptionPadding;
            panel.style.paddingTop = OptionPadding;
            panel.style.paddingBottom = OptionPadding;
            panel.style.borderTopLeftRadius = OptionRadius;
            panel.style.borderTopRightRadius = OptionRadius;
            panel.style.borderBottomLeftRadius = OptionRadius;
            panel.style.borderBottomRightRadius = OptionRadius;
            panel.style.backgroundColor = _hud.BreathTrackColor;

            panel.Add(NewPicture(offer.Creature));
            panel.Add(NewLine(offer.Creature.Name, NameFontSize, _hud.GlowColor));
            panel.Add(NewLine(offer.Creature.Role, BodyFontSize, _hud.TextColor));
            panel.Add(NewLine(offer.Song.Name, BodyFontSize, _hud.GlowColor));
            panel.Add(NewLine(offer.Song.Role, BodyFontSize, _hud.CaptionColor));
            panel.Add(NewOptionButton(offer, option));
            return panel;
        }

        private static Image NewPicture(CreatureType creature)
        {
            var picture = new Image { sprite = creature.Look.Sprite };
            picture.style.width = PictureSize;
            picture.style.height = PictureSize;
            picture.style.alignSelf = Align.Center;
            return picture;
        }

        private Button NewOptionButton(CreatureOffer offer, int option)
        {
            var button = new Button(() => _session.Choose(option))
            {
                name = OptionButtons[option],
                text = $"{option + 1}: {_texts.ChooseButton} {offer.Creature.Name}",
            };

            button.style.fontSize = ButtonFontSize;
            button.style.color = _hud.ButtonTextColor;
            button.style.backgroundColor = _hud.ButtonColor;
            button.style.marginTop = LineGap * 2f;
            button.style.marginLeft = 0f;
            button.style.marginRight = 0f;
            button.style.paddingTop = ButtonPadding;
            button.style.paddingBottom = ButtonPadding;
            button.style.borderTopWidth = 0f;
            button.style.borderBottomWidth = 0f;
            button.style.borderLeftWidth = 0f;
            button.style.borderRightWidth = 0f;
            return button;
        }

        private static Label NewLine(string text, float fontSize, Color color)
        {
            var label = new Label(text);
            label.style.fontSize = fontSize;
            label.style.color = color;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginTop = LineGap;
            label.style.marginLeft = 0f;
            label.style.marginRight = 0f;
            label.style.paddingLeft = 0f;
            label.style.paddingRight = 0f;
            return label;
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
