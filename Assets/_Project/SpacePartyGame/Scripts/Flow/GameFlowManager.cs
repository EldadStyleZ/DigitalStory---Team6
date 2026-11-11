using System;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// Runs the game's stages in the scene: it holds the <see cref="GameFlow"/>, runs the countdown, and tells
    /// everyone else when the stage changes. Screens, players, music and vermin listen to its events.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameFlowManager : MonoBehaviour
    {
        [SerializeField, Tooltip("Debug only: lets one player start a match.")]
        private bool _allowSinglePlayer;

        [SerializeField, Tooltip("Off: Start goes straight to the countdown with two fixed players (P1 keyboard, P2 gamepad).")]
        private bool _useJoinScreen;

        [SerializeField, Tooltip("Off: a round's end goes back to the start menu instead of name entry and the leaderboard.")]
        private bool _useLeaderboard;

        [SerializeField, Min(1), Tooltip("How many seconds the countdown before a match lasts.")]
        private int _countdownSeconds = 3;

        private readonly GameFlow _flow = new GameFlow();
        private CountdownTimer _countdown;

        /// <summary>Raised after every stage change, with the stage left and the stage entered.</summary>
        public event Action<GameState, GameState> StateChanged;

        /// <summary>Raised when the countdown before a match begins.</summary>
        public event Action OnMatchStart;

        /// <summary>Raised when the countdown ends and play begins.</summary>
        public event Action OnCountdownFinished;

        /// <summary>Raised when the countdown shows a new number.</summary>
        public event Action<int> CountdownChanged;

        /// <summary>The current stage.</summary>
        public GameState State => _flow.State;

        /// <summary>Whether one player may start a match, for testing.</summary>
        public bool AllowSinglePlayer => _allowSinglePlayer;

        /// <summary>Whether players join on the join screen; when off, two fixed players are spawned instead.</summary>
        public bool UseJoinScreen => _useJoinScreen;

        /// <summary>Whether a round's end leads to name entry and the leaderboard.</summary>
        public bool UseLeaderboard => _useLeaderboard;

        /// <summary>The number the countdown shows now, or 0 outside it.</summary>
        public int CountdownShown => State == GameState.Countdown ? _countdown.Shown : 0;

        /// <summary>Opens the join screen from the start menu.</summary>
        public void OpenJoin() => _flow.TryMoveTo(GameState.Join);

        /// <summary>
        /// What the start menu's Start button does: opens the join screen, or, with it off, starts the countdown
        /// for the fixed players.
        /// </summary>
        public void StartFromMenu()
        {
            if (State == GameState.StartMenu)
            {
                _flow.TryMoveTo(GameFlow.AfterStartMenu(_useJoinScreen));
            }
        }

        /// <summary>Leaves a round's end: to the leaderboard, or, with it off, back to the start menu.</summary>
        public void LeaveRoundEnd()
        {
            if (State == GameState.RoundEnd)
            {
                _flow.TryMoveTo(GameFlow.AfterRoundEnd(_useLeaderboard));
            }
        }

        /// <summary>Starts the countdown if the join screen is open and enough players have joined.</summary>
        public bool RequestMatchStart(int joinedPlayers)
        {
            if (State != GameState.Join || !GameFlow.CanStartMatch(joinedPlayers, _allowSinglePlayer))
            {
                return false;
            }

            return _flow.TryMoveTo(GameState.Countdown);
        }

        /// <summary>Moves to any stage the rules allow, for later steps such as pausing and ending a round.</summary>
        public bool TryMoveTo(GameState state) => _flow.TryMoveTo(state);

        private void Awake()
        {
            _flow.Changed += HandleChanged;
        }

        private void OnDestroy()
        {
            _flow.Changed -= HandleChanged;
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        private void Advance(float deltaTime)
        {
            if (State != GameState.Countdown)
            {
                return;
            }

            int before = _countdown.Shown;

            if (_countdown.Tick(deltaTime))
            {
                _flow.TryMoveTo(GameState.Playing);
            }
            else if (_countdown.Shown != before)
            {
                CountdownChanged?.Invoke(_countdown.Shown);
            }
        }

        private void HandleChanged(GameState from, GameState to)
        {
            if (to == GameState.Countdown)
            {
                _countdown = new CountdownTimer(_countdownSeconds);
            }

            StateChanged?.Invoke(from, to);

            if (to == GameState.Countdown)
            {
                OnMatchStart?.Invoke();
                CountdownChanged?.Invoke(_countdown.Shown);
            }
            else if (from == GameState.Countdown && to == GameState.Playing)
            {
                OnCountdownFinished?.Invoke();
            }
        }
    }
}
