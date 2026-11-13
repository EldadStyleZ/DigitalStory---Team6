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
            if (!_isShown || phase != _shownPhase || round != _shownRound || roundCount != _shownRoundCount || seconds != _shownSeconds)
            {
                bool phaseChanged = !_isShown || phase != _shownPhase;
                _isShown = true;
                _shownPhase = phase;
                _shownRound = round;
                _shownRoundCount = roundCount;
                _shownSeconds = seconds;
                _phaseLabel.text = phase == MatchPhase.Farm
                    ? "FARM  round " + Number(round) + " of " + Number(roundCount) + "  " + Number(seconds) + "s"
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
