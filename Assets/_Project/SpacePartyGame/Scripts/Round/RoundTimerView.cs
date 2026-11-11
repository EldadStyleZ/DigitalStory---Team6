using TMPro;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// The round clock at the top middle of the screen. It shows the seconds left, turns red near the end and ticks
    /// aloud for the last seconds. It is on screen from the countdown until the game goes back to the menu.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoundTimerView : MonoBehaviour
    {
        [SerializeField, Tooltip("The round to show.")]
        private RoundManager _round;

        [SerializeField, Tooltip("The game's stages.")]
        private GameFlowManager _flow;

        [SerializeField, Tooltip("The clock's object, shown only during a game.")]
        private GameObject _clock;

        [SerializeField, Tooltip("The number.")]
        private TMP_Text _text;

        [SerializeField, Tooltip("Plays the ticks.")]
        private TickVoice _tickVoice;

        [SerializeField, Tooltip("The number's normal colour.")]
        private Color _normalColour = new Color(0.91f, 0.93f, 0.95f);

        [SerializeField, Tooltip("The number's colour near the end.")]
        private Color _warningColour = new Color(0.9f, 0.25f, 0.25f);

        [SerializeField, Min(0), Tooltip("From how many seconds left the number turns to the warning colour.")]
        private int _warningFrom = 5;

        [SerializeField, Min(0), Tooltip("From how many seconds left each second ticks aloud.")]
        private int _tickFrom = 10;

        [SerializeField, Min(0), Tooltip("From how many seconds left the tick is the higher one.")]
        private int _highTickFrom = 5;

        [SerializeField, Min(0f), Tooltip("How long the clock stays on screen after reaching 0, in seconds.")]
        private float _hideAfterZeroSeconds = 1f;

        private float _hideAt = float.PositiveInfinity;

        private void Awake()
        {
            if (_round == null || _flow == null || _clock == null || _text == null)
            {
                Debug.LogError($"{name}: RoundTimerView is missing a reference.", this);
            }
        }

        private void OnEnable()
        {
            _round.TimeShownChanged += HandleTimeShownChanged;
            _flow.StateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            _round.TimeShownChanged -= HandleTimeShownChanged;
            _flow.StateChanged -= HandleStateChanged;
        }

        private void Start()
        {
            HandleStateChanged(_flow.State, _flow.State);
        }

        private void HandleStateChanged(GameState from, GameState to)
        {
            bool inGame = to == GameState.Countdown || to == GameState.Playing || to == GameState.Paused
                || to == GameState.RoundEnd;
            _clock.SetActive(inGame);
            _hideAt = to == GameState.RoundEnd ? Time.time + _hideAfterZeroSeconds : float.PositiveInfinity;

            if (inGame)
            {
                Show(_round.TimeShown);
            }
        }

        private void Update()
        {
            if (Time.time >= _hideAt)
            {
                _hideAt = float.PositiveInfinity;
                _clock.SetActive(false);
            }
        }

        private void HandleTimeShownChanged(int shown)
        {
            Show(shown);

            if (_flow.State == GameState.Playing && _tickVoice != null && RoundClock.Ticks(shown, _tickFrom))
            {
                _tickVoice.Tick(RoundClock.IsWarning(shown, _highTickFrom));
            }
        }

        private void Show(int shown)
        {
            _text.text = shown.ToString();
            _text.color = RoundClock.IsWarning(shown, _warningFrom) ? _warningColour : _normalColour;
        }
    }
}
