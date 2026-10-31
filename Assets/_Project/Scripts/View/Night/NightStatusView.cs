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
        private const float SkyBarHeight = 14f;
        private const float SkyBarGap = 3f;
        private const float SkyBarBorder = 1f;
        private const float SkyOrbSize = 10f;
        private const float SkyStepsShown = 300f;
        private const int SkyTextureWidth = 64;
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
        private VisualElement _skyBar;
        private VisualElement _skyOrb;
        private Texture2D _skyTexture;
        private int _shownDrowned = -1;
        private int _shownSeconds = -1;
        private int _shownRats = -1;
        private int _shownEaten = -1;

        /// <summary>The goal on screen, for tests and debugging.</summary>
        public string GoalText => _goal?.text;

        /// <summary>The time until dawn on screen, for tests and debugging.</summary>
        public string TimeText => _time?.text;

        /// <summary>How far along the sky bar the moon or sun is, from 0 to 1, for tests and debugging.</summary>
        public float SkyShown { get; private set; }

        /// <summary>True while the sky bar shows the sun, for tests and debugging.</summary>
        public bool IsSunShown { get; private set; }

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
                ShowSky();
            }
        }

        private void OnDestroy()
        {
            // Textures made in code are not freed with the scene.
            if (_skyTexture != null)
            {
                Destroy(_skyTexture);
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
            sign.Add(BuildSkyBar());
            sign.Add(_time);
            sign.Add(_rats);
            return sign;
        }

        private VisualElement BuildSkyBar()
        {
            _skyTexture = NewSkyTexture();

            _skyBar = new VisualElement();
            _skyBar.style.height = SkyBarHeight;
            _skyBar.style.marginTop = SkyBarGap;
            _skyBar.style.backgroundImage = new StyleBackground(_skyTexture);
            _skyBar.style.borderTopWidth = SkyBarBorder;
            _skyBar.style.borderBottomWidth = SkyBarBorder;
            _skyBar.style.borderLeftWidth = SkyBarBorder;
            _skyBar.style.borderRightWidth = SkyBarBorder;
            SetSkyBorder(Color.clear);

            _skyOrb = new VisualElement();
            _skyOrb.style.position = Position.Absolute;
            _skyOrb.style.top = (SkyBarHeight - (2f * SkyBarBorder) - SkyOrbSize) / 2f;
            _skyOrb.style.width = SkyOrbSize;
            _skyOrb.style.height = SkyOrbSize;
            _skyOrb.style.borderTopLeftRadius = SkyOrbSize / 2f;
            _skyOrb.style.borderTopRightRadius = SkyOrbSize / 2f;
            _skyOrb.style.borderBottomLeftRadius = SkyOrbSize / 2f;
            _skyOrb.style.borderBottomRightRadius = SkyOrbSize / 2f;
            _skyOrb.style.backgroundColor = _hud.MoonColor;
            _skyBar.Add(_skyOrb);
            return _skyBar;
        }

        private Texture2D NewSkyTexture()
        {
            var texture = new Texture2D(SkyTextureWidth, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
            };

            for (int x = 0; x < SkyTextureWidth; x++)
            {
                texture.SetPixel(x, 0, _hud.SkyGradient.Evaluate(x / (SkyTextureWidth - 1f)));
            }

            texture.Apply(false);
            return texture;
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

        private void ShowSky()
        {
            float progress = Mathf.Round(_session.NightProgress * SkyStepsShown) / SkyStepsShown;

            if (!Mathf.Approximately(progress, SkyShown))
            {
                SkyShown = progress;
                _skyOrb.style.left = Length.Percent(progress * 100f);
                _skyOrb.style.marginLeft = -progress * SkyOrbSize;
            }

            if (_session.IsNearDawn != IsSunShown)
            {
                IsSunShown = _session.IsNearDawn;
                _skyOrb.style.backgroundColor = IsSunShown ? _hud.SunColor : _hud.MoonColor;
            }

            if (IsSunShown)
            {
                Color pulse = _hud.DawnPulseColor;
                pulse.a = (Mathf.Sin(Time.time * _hud.DawnPulseRate) + 1f) / 2f;
                SetSkyBorder(pulse);
            }
        }

        private void SetSkyBorder(Color color)
        {
            _skyBar.style.borderTopColor = color;
            _skyBar.style.borderBottomColor = color;
            _skyBar.style.borderLeftColor = color;
            _skyBar.style.borderRightColor = color;
        }

        private void ShowBreath()
        {
            Breath breath = _session.Breath;
            _breathFill.style.width = Length.Percent(breath.Current / breath.Max * 100f);
            _breathFill.style.backgroundColor = breath.IsExhausted ? _hud.ExhaustedColor : _hud.BreathColor;
        }
    }
}
