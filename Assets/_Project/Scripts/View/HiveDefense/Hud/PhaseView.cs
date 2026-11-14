using System;
using System.Globalization;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// The phase at the top centre of the HUD: "FARM round 2 of 5" with the seconds left, or "SURVIVE wave 2 of 5", a hint in a
    /// farm phase, a short banner when the phase changes and "+N wasps" when a wave is called early. It listens to the match
    /// and unsubscribes in <see cref="Dispose"/>.
    /// </summary>
    /// <remarks>Timers are floats advanced by <see cref="Tick"/>. Text and classes change only when a value changes.</remarks>
    public sealed class PhaseView : IDisposable
    {
        private const string FarmClass = "phase-label--farm";
        private const string SurviveClass = "phase-label--survive";
        private const string ShownClass = "line--shown";

        private readonly HiveDefenseSession _session;
        private readonly Label _phaseLabel;
        private readonly Label _hint;
        private readonly Label _banner;
        private readonly Label _granted;
        private readonly float _bannerSeconds;
        private readonly float _grantedSeconds;
        private readonly string _hintText;
        private float _bannerLeft;
        private float _grantedLeft;
        private MatchPhase _shownPhase;
        private int _shownRound = -1;
        private int _shownRoundCount = -1;
        private int _shownSeconds = -1;
        private bool _isShown;
        private ControlDevice _device;

        // The farm's time left as a glowing bar that shortens, with a little clock riding its end. It turns amber and then
        // red as time runs out, and the clock's hand sweeps round once a second.
        private const float TimerWidth = 300f;
        private const float TimerHeight = 30f;
        private const float BarThickness = 6f;
        private const float ClockRadius = 9f;
        private static readonly Color TimerCalm = new Color(0.5f, 0.95f, 0.85f, 1f);
        private static readonly Color TimerLate = new Color(1f, 0.7f, 0.25f, 1f);
        private static readonly Color TimerEnd = new Color(1f, 0.28f, 0.18f, 1f);
        private readonly VisualElement _timer;
        private float _farmTotal;
        private float _timeLeft01 = 1f;
        private float _secondsLeft;

        /// <summary>Finds "phase-label", "phase-hint", "banner-line" and "granted-line" in the root and starts listening.</summary>
        /// <exception cref="ArgumentNullException">An argument is missing.</exception>
        /// <exception cref="InvalidOperationException">The UXML lacks an element.</exception>
        public PhaseView(HiveDefenseSession session, VisualElement root, string hintText, float bannerSeconds, float grantedSeconds)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            _phaseLabel = Require(root, "phase-label");
            _hint = Require(root, "phase-hint");
            _banner = Require(root, "banner-line");
            _granted = Require(root, "granted-line");
            _bannerSeconds = bannerSeconds;
            _grantedSeconds = grantedSeconds;
            _hintText = hintText ?? string.Empty;
            ControlSymbols.ShowPrompt(_hint, _hintText, _device);
            _banner.RemoveFromClassList(ShownClass);
            _granted.RemoveFromClassList(ShownClass);

            _timer = new VisualElement { pickingMode = PickingMode.Ignore, name = "phase-timer" };
            _timer.style.width = TimerWidth;
            _timer.style.height = TimerHeight;
            _timer.style.alignSelf = Align.Center;
            _timer.generateVisualContent += DrawTimer;
            VisualElement parent = _phaseLabel.parent;
            parent.Insert(parent.IndexOf(_phaseLabel) + 1, _timer);

            session.Waves.PhaseChanged += OnPhaseChanged;
            session.WaspsGranted += OnWaspsGranted;
        }

        /// <summary>Stops listening to the match.</summary>
        public void Dispose()
        {
            _session.Waves.PhaseChanged -= OnPhaseChanged;
            _session.WaspsGranted -= OnWaspsGranted;
        }

        /// <summary>Names the key of the farm phase's hint for the device in use.</summary>
        public void ShowControls(ControlDevice device)
        {
            if (device == _device)
            {
                return;
            }

            _device = device;
            ControlSymbols.ShowPrompt(_hint, _hintText, device);
        }

        /// <summary>Updates the phase line and the timers. Call once per frame.</summary>
        public void Tick(float deltaTime)
        {
            MatchPhase phase = _session.Phase;
            int round = _session.Round;
            int roundCount = _session.RoundCount;
            int seconds = phase == MatchPhase.Farm ? Mathf.CeilToInt(_session.FarmSecondsLeft) : 0;
            ShowTimer(phase);
            if (!_isShown || phase != _shownPhase || round != _shownRound || roundCount != _shownRoundCount || seconds != _shownSeconds)
            {
                bool phaseChanged = !_isShown || phase != _shownPhase;
                _isShown = true;
                _shownPhase = phase;
                _shownRound = round;
                _shownRoundCount = roundCount;
                _shownSeconds = seconds;
                _phaseLabel.text = phase == MatchPhase.Farm
                    ? "FARM  round " + Number(round) + " of " + Number(roundCount)
                    : "SURVIVE  wave " + Number(round) + " of " + Number(roundCount);
                if (phaseChanged)
                {
                    _phaseLabel.EnableInClassList(FarmClass, phase == MatchPhase.Farm);
                    _phaseLabel.EnableInClassList(SurviveClass, phase == MatchPhase.Survive);
                    _hint.EnableInClassList(ShownClass, phase == MatchPhase.Farm);
                }
            }

            _bannerLeft = Countdown(_bannerLeft, deltaTime, _banner);
            _grantedLeft = Countdown(_grantedLeft, deltaTime, _granted);
        }

        private void ShowTimer(MatchPhase phase)
        {
            bool farming = phase == MatchPhase.Farm;
            _timer.style.display = farming ? DisplayStyle.Flex : DisplayStyle.None;
            if (!farming)
            {
                _farmTotal = 0f;
                return;
            }

            // The farm's full length is the most time seen left since it began.
            _secondsLeft = Mathf.Max(0f, _session.FarmSecondsLeft);
            _farmTotal = Mathf.Max(_farmTotal, _secondsLeft);
            _timeLeft01 = _farmTotal > 0f ? Mathf.Clamp01(_secondsLeft / _farmTotal) : 0f;
            _timer.MarkDirtyRepaint();
        }

        private void DrawTimer(MeshGenerationContext context)
        {
            Painter2D paint = context.painter2D;
            float left = ClockRadius + 4f;
            float right = TimerWidth - ClockRadius - 4f;
            float y = TimerHeight * 0.5f;
            float end = Mathf.Lerp(left, right, _timeLeft01);
            Color color = _timeLeft01 > 0.35f ? TimerCalm
                : Color.Lerp(TimerEnd, TimerLate, Mathf.InverseLerp(0.1f, 0.35f, _timeLeft01));

            // In the last seconds the whole bar breathes faster.
            float urgency = _secondsLeft <= 5f ? 0.5f + (0.5f * Mathf.Sin(Time.unscaledTime * 10f)) : 0f;

            // The empty track.
            paint.lineCap = LineCap.Round;
            paint.BeginPath();
            paint.MoveTo(new Vector2(left, y));
            paint.LineTo(new Vector2(right, y));
            paint.strokeColor = new Color(color.r, color.g, color.b, 0.12f);
            paint.lineWidth = BarThickness;
            paint.Stroke();

            // The time left, with a soft glow under it.
            if (end > left + 0.5f)
            {
                for (int ring = 3; ring >= 1; ring--)
                {
                    paint.BeginPath();
                    paint.MoveTo(new Vector2(left, y));
                    paint.LineTo(new Vector2(end, y));
                    paint.strokeColor = new Color(color.r, color.g, color.b, (0.08f + (urgency * 0.06f)) * (4 - ring));
                    paint.lineWidth = BarThickness + (ring * 5f);
                    paint.Stroke();
                }

                paint.BeginPath();
                paint.MoveTo(new Vector2(left, y));
                paint.LineTo(new Vector2(end, y));
                paint.strokeColor = Color.Lerp(color, Color.white, 0.25f + (urgency * 0.3f));
                paint.lineWidth = BarThickness;
                paint.Stroke();
            }

            // The clock at the bar's end: a glowing face, its rim, twelve o'clock and a hand that sweeps every second.
            var centre = new Vector2(end, y);
            paint.BeginPath();
            paint.Arc(centre, ClockRadius + 5f, Angle.Degrees(0f), Angle.Degrees(360f));
            paint.fillColor = new Color(color.r, color.g, color.b, 0.18f + (urgency * 0.15f));
            paint.Fill();

            paint.BeginPath();
            paint.Arc(centre, ClockRadius, Angle.Degrees(0f), Angle.Degrees(360f));
            paint.fillColor = new Color(0.04f, 0.06f, 0.06f, 0.95f);
            paint.Fill();
            paint.strokeColor = color;
            paint.lineWidth = 2f;
            paint.Stroke();

            paint.BeginPath();
            paint.MoveTo(centre + new Vector2(0f, -ClockRadius + 1.5f));
            paint.LineTo(centre + new Vector2(0f, -ClockRadius + 4f));
            paint.lineWidth = 1.5f;
            paint.Stroke();

            float sweep = (1f - Mathf.Repeat(_secondsLeft, 1f)) * Mathf.PI * 2f;
            paint.BeginPath();
            paint.MoveTo(centre);
            paint.LineTo(centre + (new Vector2(Mathf.Sin(sweep), -Mathf.Cos(sweep)) * (ClockRadius - 3f)));
            paint.strokeColor = Color.white;
            paint.lineWidth = 2f;
            paint.Stroke();

            paint.BeginPath();
            paint.Arc(centre, 1.5f, Angle.Degrees(0f), Angle.Degrees(360f));
            paint.fillColor = Color.white;
            paint.Fill();
        }

        private static float Countdown(float left, float deltaTime, VisualElement line)
        {
            if (left <= 0f)
            {
                return 0f;
            }

            left -= deltaTime;
            if (left <= 0f)
            {
                line.RemoveFromClassList(ShownClass);
                return 0f;
            }

            return left;
        }

        private void OnPhaseChanged(MatchPhase phase)
        {
            _banner.text = phase == MatchPhase.Survive ? "Wave " + Number(_session.Round) : "Wave cleared";
            _bannerLeft = _bannerSeconds;
            _banner.AddToClassList(ShownClass);
        }

        private void OnWaspsGranted(int wasps)
        {
            _granted.text = "+" + Number(wasps) + " wasps";
            _grantedLeft = _grantedSeconds;
            _granted.AddToClassList(ShownClass);
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static Label Require(VisualElement root, string name)
        {
            return root.Q<Label>(name) ?? throw new InvalidOperationException("The HUD UXML has no element named " + name + ".");
        }
    }
}
