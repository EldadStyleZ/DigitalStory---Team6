using System;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// The HUD's giant strip (plan section I). While the Colossus stands it shows a ring of the seconds left, which glows for a
    /// moment when a kill adds time, and small marks for the slam's, the roar's and each hand's cooldown (songs-v2 R). Each time
    /// the giant forms, a line names its moves and a second its two hand launches for a few seconds and then fade, with a smaller
    /// line of its two phrases the first times only. The hands are named by the device in use: RT and LT on a controller, the
    /// mouse buttons and keys otherwise. Hidden otherwise. No song can be cast while the giant stands, so the songbook steps
    /// aside then and the strip takes its corner, and the lines at the bottom move to the left corner, clear of the giant.
    /// </summary>
    /// <remarks>Lives on the same GameObject as the HUD's UIDocument. The HUD view adds it and calls Initialize.</remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Giant Hud View")]
    public sealed class GiantHudView : MonoBehaviour
    {
        private const string ShownClass = "giant-strip--shown";
        private const string LineShownClass = "giant-line--shown";
        private const string ReadyClass = "giant-mark--ready";
        private const string SongbookHiddenClass = "songbook--hidden";
        private const string BottomGiantClass = "bottom--giant";
        private const float PercentScale = 100f;
        private const float FullTurnDegrees = 360f;
        private const float TopDegrees = -90f;
        private const int MostShownSeconds = 99;

        // The seconds the ring's label can show, made once, so a second ticking by never allocates.
        private static readonly string[] SecondsTexts = MakeSecondsTexts();

        [SerializeField, Tooltip("The line that shows each time the giant forms: its moves.")]
        private string _movesText = "J punch · K kick · L slam · H roar";

        [SerializeField, Tooltip("The second, smaller line: the two phrases. Shown only the first times the giant forms in a match.")]
        private string _phrasesText = "K K spin kick · L L leap";

        [SerializeField, Tooltip("The line of the two hand launches, as the keyboard and mouse play them. A controller player reads RT and LT for the keys.")]
        private string _handsText = "Right click or Space: right fist · Left click or Shift: left fist";

        [SerializeField, Tooltip("The right hand's cooldown mark, as the mouse plays it. A controller player reads RT.")]
        private string _rightMarkText = "RMB fist";

        [SerializeField, Tooltip("The left hand's cooldown mark, as the mouse plays it. A controller player reads LT.")]
        private string _leftMarkText = "LMB fist";

        [SerializeField, Min(0.5f), Tooltip("Seconds the lines stay before they fade.")]
        private float _lineSeconds = 6f;

        [SerializeField, Min(0), Tooltip("How many times the second line shows in a match.")]
        private int _phraseFormations = 2;

        [SerializeField, Min(0.05f), Tooltip("Seconds the ring glows after a kill adds time.")]
        private float _growSeconds = 0.5f;

        [SerializeField, Min(1f), Tooltip("The ring's width, in panel pixels.")]
        private float _ringWidth = 6f;

        [SerializeField, Tooltip("The ring's colour for the time left.")]
        private Color _ringColor = new Color(0.69f, 0.36f, 0.9f, 1f);

        [SerializeField, Tooltip("The ring's colour for a moment after a kill adds time.")]
        private Color _ringGrewColor = new Color(1f, 0.84f, 0.35f, 1f);

        [SerializeField, Tooltip("The colour of the ring's track, the time already gone.")]
        private Color _ringTrackColor = new Color(1f, 1f, 1f, 0.14f);

        private HiveDefenseSession _session;
        private bool _isReady;
        private VisualElement _strip;
        private VisualElement _ring;
        private Label _secondsLabel;
        private VisualElement _slamFill;
        private VisualElement _roarFill;
        private VisualElement _rightFill;
        private VisualElement _leftFill;
        private Label _rightLabel;
        private Label _leftLabel;
        private Label _handsLine;
        private VisualElement _bottom;
        private Label _movesLine;
        private Label _phrasesLine;
        private VisualElement _songbook;
        private bool _isShowing;
        private int _formations;
        private float _lineLeft;
        private float _growLeft;
        private float _lastTotal;
        private float _ringShare;
        private bool _isRingGlowing;
        private int _shownSeconds = -1;
        private int _shownRing = -1;
        private int _shownSlam = -1;
        private int _shownRoar = -1;
        private int _shownRight = -1;
        private int _shownLeft = -1;
        private ControlDevice _device;

        /// <summary>True while the strip shows, for tests and debugging.</summary>
        public bool IsShowing => _isShowing;

        /// <summary>True while the moves' line shows, from the giant's forming until it fades, for tests and debugging.</summary>
        public bool IsHintShowing => _movesLine != null && _movesLine.ClassListContains(LineShownClass);

        /// <summary>True while the phrases' line shows with the moves' line, for tests and debugging.</summary>
        public bool IsPhrasesHintShowing => _phrasesLine != null && _phrasesLine.ClassListContains(LineShownClass);

        /// <summary>True while the line of the hand launches shows with the moves' line, for tests and debugging.</summary>
        public bool IsHandsHintShowing => _handsLine != null && _handsLine.ClassListContains(LineShownClass);

        /// <summary>The line of the hand launches as it reads now, for tests.</summary>
        public string HandsText => _handsLine != null ? _handsLine.text : string.Empty;

        /// <summary>The right hand's mark as it reads now, for tests.</summary>
        public string RightMarkText => _rightLabel != null ? _rightLabel.text : string.Empty;

        /// <summary>True while the lines at the bottom stand in their corner because the giant stands, for tests.</summary>
        public bool IsBottomMoved => _bottom != null && _bottom.ClassListContains(BottomGiantClass);

        /// <summary>True while the songbook is hidden because the giant stands, for tests and debugging.</summary>
        public bool IsSongbookHidden => _songbook != null && _songbook.ClassListContains(SongbookHiddenClass);

        /// <summary>The share of the giant's time the ring shows as left, 0 to 1, for tests and debugging.</summary>
        public float RingShare => _ringShare;

        /// <summary>Finds the strip's elements in the HUD's UIDocument. Call once, before the first Update.</summary>
        /// <exception cref="ArgumentNullException">The session is missing.</exception>
        /// <exception cref="InvalidOperationException">The UXML lacks an element, or a text is empty.</exception>
        public void Initialize(HiveDefenseSession session)
        {
            _isReady = false;
            _session = session ?? throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(_movesText) || string.IsNullOrWhiteSpace(_phrasesText) || string.IsNullOrWhiteSpace(_handsText)
                || string.IsNullOrWhiteSpace(_rightMarkText) || string.IsNullOrWhiteSpace(_leftMarkText))
            {
                throw new InvalidOperationException($"{name}: the giant's lines are empty. Set them on the GiantHudView.");
            }

            VisualElement root = GetComponent<UIDocument>().rootVisualElement
                ?? throw new InvalidOperationException("The UIDocument has no Visual Tree Asset. Set HiveDefenseHud.uxml.");

            if (_ring != null)
            {
                _ring.generateVisualContent -= DrawRing;
            }

            _strip = Require<VisualElement>(root, "giant-strip");
            _ring = Require<VisualElement>(root, "giant-ring");
            _secondsLabel = Require<Label>(root, "giant-seconds");
            _slamFill = Require<VisualElement>(root, "giant-slam-fill");
            _roarFill = Require<VisualElement>(root, "giant-roar-fill");
            _rightFill = Require<VisualElement>(root, "giant-right-fill");
            _leftFill = Require<VisualElement>(root, "giant-left-fill");
            _rightLabel = Require<Label>(root, "giant-right-label");
            _leftLabel = Require<Label>(root, "giant-left-label");
            _movesLine = Require<Label>(root, "giant-moves-line");
            _handsLine = Require<Label>(root, "giant-hands-line");
            _phrasesLine = Require<Label>(root, "giant-phrases-line");
            _songbook = Require<VisualElement>(root, "songbook");
            _bottom = Require<VisualElement>(root, "bottom");
            NameControls();
            _ring.generateVisualContent += DrawRing;

            _isShowing = false;
            _formations = 0;
            _lineLeft = 0f;
            _growLeft = 0f;
            _lastTotal = 0f;
            _ringShare = 0f;
            _isRingGlowing = false;
            _shownSeconds = -1;
            _shownRing = -1;
            _shownSlam = -1;
            _shownRoar = -1;
            _shownRight = -1;
            _shownLeft = -1;
            _strip.RemoveFromClassList(ShownClass);
            _movesLine.RemoveFromClassList(LineShownClass);
            _handsLine.RemoveFromClassList(LineShownClass);
            _phrasesLine.RemoveFromClassList(LineShownClass);
            _songbook.RemoveFromClassList(SongbookHiddenClass);
            _bottom.RemoveFromClassList(BottomGiantClass);
            _isReady = true;
        }

        /// <summary>Names the giant's moves for the device in use: the note keys' letters, or a controller's button symbols.</summary>
        public void ShowControls(ControlDevice device)
        {
            _device = device;
            if (_movesLine != null)
            {
                NameControls();
            }
        }

        private void NameControls()
        {
            ControlSymbols.ShowPrompt(_movesLine, _movesText, _device);
            ControlSymbols.ShowPrompt(_handsLine, _handsText, _device);
            ControlSymbols.ShowPrompt(_phrasesLine, _phrasesText, _device);
            ControlSymbols.ShowPrompt(_rightLabel, _rightMarkText, _device);
            ControlSymbols.ShowPrompt(_leftLabel, _leftMarkText, _device);
        }

        private void Update()
        {
            if (!_isReady)
            {
                return;
            }

            bool active = _session.IsColossusActive && _session.State == HiveDefenseState.Playing;
            if (active != _isShowing)
            {
                if (active)
                {
                    Show();
                }
                else
                {
                    Hide();
                }
            }

            if (!_isShowing)
            {
                return;
            }

            // Game seconds, as the rest of the HUD counts them.
            float deltaTime = Time.deltaTime;
            ShowTime(deltaTime);
            ShowCooldowns();
            ShowLines(deltaTime);
        }

        private void OnDestroy()
        {
            _isReady = false;
            if (_ring != null)
            {
                _ring.generateVisualContent -= DrawRing;
            }
        }

        private void Show()
        {
            _isShowing = true;
            _formations++;
            _lineLeft = _lineSeconds;
            _growLeft = 0f;
            _isRingGlowing = false;
            _lastTotal = _session.ColossusSecondsTotal;
            _shownSeconds = -1;
            _shownRing = -1;
            _shownSlam = -1;
            _shownRoar = -1;
            _shownRight = -1;
            _shownLeft = -1;
            _strip.AddToClassList(ShownClass);
            _movesLine.AddToClassList(LineShownClass);
            _handsLine.AddToClassList(LineShownClass);
            _phrasesLine.EnableInClassList(LineShownClass, _formations <= _phraseFormations);
            _songbook.AddToClassList(SongbookHiddenClass);
            _bottom.AddToClassList(BottomGiantClass);
        }

        private void Hide()
        {
            _isShowing = false;
            _lineLeft = 0f;
            _strip.RemoveFromClassList(ShownClass);
            _movesLine.RemoveFromClassList(LineShownClass);
            _handsLine.RemoveFromClassList(LineShownClass);
            _phrasesLine.RemoveFromClassList(LineShownClass);
            _songbook.RemoveFromClassList(SongbookHiddenClass);
            _bottom.RemoveFromClassList(BottomGiantClass);
        }

        // The ring is the share of the giant's whole time still left. A kill that adds seconds makes it glow for a moment.
        private void ShowTime(float deltaTime)
        {
            float total = _session.ColossusSecondsTotal;
            float left = Mathf.Clamp(_session.ColossusSecondsLeft, 0f, total);
            bool isGlowing = _isRingGlowing;
            if (total > _lastTotal + 0.001f)
            {
                _growLeft = _growSeconds;
                isGlowing = true;
            }

            _lastTotal = total;
            if (_growLeft > 0f)
            {
                _growLeft -= deltaTime;
                if (_growLeft <= 0f)
                {
                    isGlowing = false;
                }
            }

            _ringShare = total > 0f ? left / total : 0f;
            int ring = Percent(_ringShare);
            if (ring != _shownRing || isGlowing != _isRingGlowing)
            {
                _shownRing = ring;
                _isRingGlowing = isGlowing;
                _ring.MarkDirtyRepaint();
            }

            int seconds = Mathf.Min(Mathf.CeilToInt(left), MostShownSeconds);
            if (seconds != _shownSeconds)
            {
                _shownSeconds = seconds;
                _secondsLabel.text = SecondsTexts[seconds];
            }
        }

        private void DrawRing(MeshGenerationContext context)
        {
            Rect rect = _ring.contentRect;
            float radius = (Mathf.Min(rect.width, rect.height) - _ringWidth) * 0.5f;
            if (!(radius > 0f))
            {
                return;
            }

            Painter2D painter = context.painter2D;
            painter.lineWidth = _ringWidth;
            painter.lineCap = LineCap.Butt;
            painter.strokeColor = _ringTrackColor;
            painter.BeginPath();
            painter.Arc(rect.center, radius, Angle.Degrees(0f), Angle.Degrees(FullTurnDegrees));
            painter.Stroke();

            float share = _shownRing / PercentScale;
            if (!(share > 0f))
            {
                return;
            }

            painter.strokeColor = _isRingGlowing ? _ringGrewColor : _ringColor;
            painter.BeginPath();
            painter.Arc(rect.center, radius, Angle.Degrees(TopDegrees), Angle.Degrees(TopDegrees + (FullTurnDegrees * share)));
            painter.Stroke();
        }

        // The mark fills while the move recharges and lights when it is ready.
        private void ShowCooldowns()
        {
            ShowMark(_slamFill, _session.SlamCooldown01, ref _shownSlam);
            ShowMark(_roarFill, _session.RoarCooldown01, ref _shownRoar);
            ShowMark(_rightFill, _session.FistCooldown01(ColossusHand.Right), ref _shownRight);
            ShowMark(_leftFill, _session.FistCooldown01(ColossusHand.Left), ref _shownLeft);
        }

        private static void ShowMark(VisualElement fill, float cooldown01, ref int shown)
        {
            int percent = Percent(1f - cooldown01);
            if (percent == shown)
            {
                return;
            }

            shown = percent;
            fill.style.width = new Length(percent, LengthUnit.Percent);
            fill.EnableInClassList(ReadyClass, percent >= (int)PercentScale);
        }

        private void ShowLines(float deltaTime)
        {
            if (_lineLeft <= 0f)
            {
                return;
            }

            _lineLeft -= deltaTime;
            if (_lineLeft <= 0f)
            {
                // The style sheet's transition fades them out.
                _movesLine.RemoveFromClassList(LineShownClass);
                _handsLine.RemoveFromClassList(LineShownClass);
                _phrasesLine.RemoveFromClassList(LineShownClass);
            }
        }

        private static int Percent(float share01) => Mathf.RoundToInt(Mathf.Clamp01(share01) * PercentScale);

        private static string[] MakeSecondsTexts()
        {
            var texts = new string[MostShownSeconds + 1];
            for (int seconds = 0; seconds < texts.Length; seconds++)
            {
                texts[seconds] = seconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            return texts;
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            return root.Q<T>(name) ?? throw new InvalidOperationException("The HUD UXML has no element named " + name + ".");
        }
    }
}
