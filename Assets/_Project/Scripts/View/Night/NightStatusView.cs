using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>Shows the goal, the time until dawn, the Piper's breath and how the night ended.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("DigitalStory/Night Status View")]
    public sealed class NightStatusView : MonoBehaviour
    {
        private const float PanelMargin = 12f;
        private const float SignGap = 10f;
        private const float SignPadding = 10f;
        private const float SignRadius = 8f;
        private const float SignMinWidth = 130f;
        private const float BigFontSize = 34f;
        private const float SmallFontSize = 15f;
        private const float BreathBarHeight = 6f;
        private const float BreathBarGap = 6f;

        private NightSession _session;
        private MenuTextConfig _texts;
        private HudConfig _hud;
        private HudLayout _layout;
        private Label _goal;
        private Label _time;
        private Label _rats;
        private VisualElement _breathFill;
        private int _shownDrowned = -1;
        private int _shownSeconds = -1;
        private int _shownRats = -1;
        private int _shownEaten = -1;

        /// <summary>The goal on screen, for tests and debugging.</summary>
        public string GoalText => _goal?.text;

        /// <summary>The time until dawn on screen, for tests and debugging.</summary>
        public string TimeText => _time?.text;

        /// <summary>The rats still alive on screen, for tests and debugging.</summary>
        public string RatsText => _rats?.text;

        /// <summary>Supplies what to show. Call once, before this object is enabled.</summary>
        public void Initialize(NightSession session, MenuTextConfig texts, HudConfig hud, HudLayout layout)
        {
            _session = session;
            _texts = texts;
            _hud = hud;
            _layout = layout;
        }

        private void Start()
        {
            // GameRoot has already reported the problem if nothing was supplied.
            if (_session != null)
            {
                BuildPanel(_layout.Layer);
            }
        }

        private void Update()
        {
            if (_goal != null)
            {
                ShowCounts();
                ShowBreath();
            }
        }

        private void BuildPanel(VisualElement root)
        {
            var signs = new VisualElement();
            signs.style.position = Position.Absolute;
            signs.style.left = PanelMargin;
            signs.style.top = PanelMargin;
            signs.style.flexDirection = FlexDirection.Row;

            signs.Add(BuildGoalSign());
            signs.Add(BuildTimeSign());
            root.Add(signs);
        }

        private VisualElement BuildGoalSign()
        {
            _goal = NewLabel(BigFontSize, _hud.TextColor);

            var breathBar = new VisualElement();
            breathBar.style.height = BreathBarHeight;
            breathBar.style.marginTop = BreathBarGap;
            breathBar.style.backgroundColor = _hud.BreathTrackColor;

            _breathFill = new VisualElement();
            _breathFill.style.height = BreathBarHeight;
            breathBar.Add(_breathFill);

            VisualElement sign = NewSign();
            sign.Add(_goal);
            sign.Add(NewCaption(_texts.DrownedCaption));
            sign.Add(breathBar);
            return sign;
        }

        private VisualElement BuildTimeSign()
        {
            _time = NewLabel(BigFontSize, _hud.TextColor);
            _rats = NewLabel(SmallFontSize, _hud.CaptionColor);

            VisualElement sign = NewSign();
            sign.style.marginLeft = SignGap;
            sign.Add(NewCaption(_texts.UntilDawnCaption));
            sign.Add(_time);
            sign.Add(_rats);
            return sign;
        }

        private VisualElement NewSign()
        {
            var sign = new VisualElement();
            sign.style.minWidth = SignMinWidth;
            sign.style.paddingLeft = SignPadding;
            sign.style.paddingRight = SignPadding;
            sign.style.paddingTop = SignPadding;
            sign.style.paddingBottom = SignPadding;
            sign.style.borderTopLeftRadius = SignRadius;
            sign.style.borderTopRightRadius = SignRadius;
            sign.style.borderBottomLeftRadius = SignRadius;
            sign.style.borderBottomRightRadius = SignRadius;
            sign.style.backgroundColor = _hud.PanelColor;
            return sign;
        }

        private Label NewCaption(string text)
        {
            Label caption = NewLabel(SmallFontSize, _hud.CaptionColor);
            caption.text = text;
            return caption;
        }

        private static Label NewLabel(float fontSize, Color color)
        {
            var label = new Label();
            label.style.color = color;
            label.style.fontSize = fontSize;
            return label;
        }

        // Rebuilds the text only when a number changes, so most frames allocate nothing.
        private void ShowCounts()
        {
            int drowned = _session.Swarm.DrownedCount;
            int rats = _session.Swarm.AliveCount;
            int eaten = _session.Swarm.LostCount;
            int seconds = Mathf.Max(0, Mathf.CeilToInt(_session.TimeLeft));

            if (drowned == _shownDrowned && rats == _shownRats && eaten == _shownEaten && seconds == _shownSeconds)
            {
                return;
            }

            _shownDrowned = drowned;
            _shownRats = rats;
            _shownEaten = eaten;
            _shownSeconds = seconds;
            _goal.text = string.Format(_texts.GoalCountFormat, drowned, _session.Quota);
            _time.text = string.Format(_texts.SecondsFormat, seconds);
            _rats.text = eaten == 0
                ? string.Format(_texts.RatsLeftFormat, rats)
                : string.Format(_texts.RatsLeftAndEatenFormat, rats, eaten);
        }

        private void ShowBreath()
        {
            Breath breath = _session.Breath;
            _breathFill.style.width = Length.Percent(breath.Current / breath.Max * 100f);
            _breathFill.style.backgroundColor = breath.IsExhausted ? _hud.ExhaustedColor : _hud.BreathColor;
        }
    }
}
