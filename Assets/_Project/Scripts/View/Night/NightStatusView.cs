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
        private const float EndingFontSize = 34f;
        private const float EndingPadding = 18f;
        private const float EndingTopPercent = 36f;
        private const float BreathBarHeight = 6f;
        private const float BreathBarGap = 6f;

        private const string DrownedCaption = "drowned";
        private const string UntilDawnCaption = "Until dawn";

        private static readonly Color TextColor = new Color32(0xF3, 0xEA, 0xD6, 0xFF);
        private static readonly Color CaptionColor = new Color32(0xAA, 0xA7, 0xBB, 0xFF);
        private static readonly Color SignColor = new Color32(0x12, 0x14, 0x22, 0xD6);
        private static readonly Color BreathColor = new Color32(0xF2, 0xB5, 0x44, 0xFF);
        private static readonly Color ExhaustedColor = new Color32(0x8A, 0x5A, 0x5A, 0xFF);
        private static readonly Color BarBackground = new Color(1f, 1f, 1f, 0.12f);

        private NightSession _session;
        private EndingTextConfig _texts;
        private Label _goal;
        private Label _time;
        private Label _rats;
        private Label _ending;
        private VisualElement _breathFill;
        private int _shownDrowned = -1;
        private int _shownSeconds = -1;
        private int _shownRats = -1;

        /// <summary>The goal on screen, for tests and debugging.</summary>
        public string GoalText => _goal?.text;

        /// <summary>The time until dawn on screen, for tests and debugging.</summary>
        public string TimeText => _time?.text;

        /// <summary>The rats still alive on screen, for tests and debugging.</summary>
        public string RatsText => _rats?.text;

        /// <summary>Supplies what to show. Call once, before this object is enabled.</summary>
        public void Initialize(NightSession session, EndingTextConfig texts)
        {
            _session = session;
            _texts = texts;
        }

        private void OnEnable()
        {
            if (_session != null)
            {
                _session.Ended += HandleEnded;
            }
        }

        private void Start()
        {
            // GameRoot has already reported the problem if nothing was supplied.
            if (_session != null)
            {
                BuildPanel(GetComponent<UIDocument>().rootVisualElement);
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

        private void OnDisable()
        {
            // Unity also calls this when the scene unloads before Initialize ran, so _session can be null.
            if (_session != null)
            {
                _session.Ended -= HandleEnded;
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
            root.Add(BuildEnding());
        }

        private VisualElement BuildGoalSign()
        {
            _goal = NewLabel(BigFontSize, TextColor);

            var breathBar = new VisualElement();
            breathBar.style.height = BreathBarHeight;
            breathBar.style.marginTop = BreathBarGap;
            breathBar.style.backgroundColor = BarBackground;

            _breathFill = new VisualElement();
            _breathFill.style.height = BreathBarHeight;
            breathBar.Add(_breathFill);

            VisualElement sign = NewSign();
            sign.Add(_goal);
            sign.Add(NewCaption(DrownedCaption));
            sign.Add(breathBar);
            return sign;
        }

        private VisualElement BuildTimeSign()
        {
            _time = NewLabel(BigFontSize, TextColor);
            _rats = NewLabel(SmallFontSize, CaptionColor);

            VisualElement sign = NewSign();
            sign.style.marginLeft = SignGap;
            sign.Add(NewCaption(UntilDawnCaption));
            sign.Add(_time);
            sign.Add(_rats);
            return sign;
        }

        private Label BuildEnding()
        {
            _ending = NewLabel(EndingFontSize, TextColor);
            _ending.style.position = Position.Absolute;
            _ending.style.left = 0f;
            _ending.style.right = 0f;
            _ending.style.top = Length.Percent(EndingTopPercent);
            _ending.style.unityTextAlign = TextAnchor.MiddleCenter;
            _ending.style.whiteSpace = WhiteSpace.Normal;
            return _ending;
        }

        private static VisualElement NewSign()
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
            sign.style.backgroundColor = SignColor;
            return sign;
        }

        private static Label NewCaption(string text)
        {
            Label caption = NewLabel(SmallFontSize, CaptionColor);
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
            int seconds = Mathf.Max(0, Mathf.CeilToInt(_session.TimeLeft));

            if (drowned == _shownDrowned && rats == _shownRats && seconds == _shownSeconds)
            {
                return;
            }

            _shownDrowned = drowned;
            _shownRats = rats;
            _shownSeconds = seconds;
            _goal.text = $"{drowned}/{_session.Quota}";
            _time.text = $"{seconds}s";
            _rats.text = $"{rats} rats left";
        }

        private void ShowBreath()
        {
            Breath breath = _session.Breath;
            _breathFill.style.width = Length.Percent(breath.Current / breath.Max * 100f);
            _breathFill.style.backgroundColor = breath.IsExhausted ? ExhaustedColor : BreathColor;
        }

        private void HandleEnded(NightOutcome outcome)
        {
            _ending.style.backgroundColor = SignColor;
            _ending.style.paddingTop = EndingPadding;
            _ending.style.paddingBottom = EndingPadding;
            _ending.text = outcome switch
            {
                NightOutcome.Won => $"{_texts.WonTitle}\n{_texts.WonMessage}",
                NightOutcome.QuotaOutOfReach => $"{_texts.QuotaOutOfReachTitle}\n{_texts.QuotaOutOfReachMessage}",
                _ => $"{_texts.DawnTitle}\n{_texts.DawnMessage}",
            };
        }
    }
}
