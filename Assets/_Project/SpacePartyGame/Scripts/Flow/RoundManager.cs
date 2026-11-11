using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpacePartyGame
{
    /// <summary>
    /// Runs a round: the clock counts down while playing; at 0 the scores lock, the vermin stop following, the
    /// winner's view widens to the whole screen and the winner gets their tag. Esc or Start, any time during a game,
    /// resets it to the start menu. It also tells every player what they may do in each stage.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoundManager : MonoBehaviour
    {
        [SerializeField, Tooltip("The game's stages.")]
        private GameFlowManager _flow;

        [SerializeField, Tooltip("The players in the match.")]
        private PlayerJoinController _joining;

        [SerializeField, Tooltip("The cages and scores.")]
        private CageManager _cages;

        [SerializeField, Tooltip("Widens the winner's view at the end.")]
        private SplitScreenManager _splitScreen;

        [SerializeField, Tooltip("The pool holding every vermin, so they can all stop following at the end.")]
        private VerminPool _vermin;

        [SerializeField, Min(1f), Tooltip("How long a round lasts, in seconds.")]
        private float _roundSeconds = 50f;

        [SerializeField, Tooltip("What the winner's tag says.")]
        private string _winnerText = "WINNER";

        [SerializeField, Tooltip("What both players' tags say in a draw.")]
        private string _drawText = "DRAW";

        [SerializeField, Tooltip("The colour of the draw tags.")]
        private Color _drawColour = new Color(0.91f, 0.93f, 0.95f);

        private readonly HashSet<PlayerInput> _listening = new HashSet<PlayerInput>();
        private RoundTimer _timer;

        /// <summary>Raised when the clock shows a new number, with that number.</summary>
        public event Action<int> TimeShownChanged;

        /// <summary>Raised when a round ends, with the winning slot or <see cref="RoundResult.Draw"/>.</summary>
        public event Action<int> RoundEnded;

        /// <summary>The number the clock shows: the round's length before play, then the seconds left.</summary>
        public int TimeShown => _timer != null ? _timer.Shown : Mathf.CeilToInt(_roundSeconds);

        /// <summary>The last round's winner, or <see cref="RoundResult.Draw"/>.</summary>
        public int Winner { get; private set; } = RoundResult.Draw;

        private void Awake()
        {
            if (_flow == null || _joining == null || _cages == null || _splitScreen == null || _vermin == null)
            {
                Debug.LogError($"{name}: RoundManager is missing a reference.", this);
            }
        }

        private void OnEnable()
        {
            _flow.StateChanged += HandleStateChanged;
            _joining.PlayerJoined += Listen;
        }

        private void OnDisable()
        {
            _flow.StateChanged -= HandleStateChanged;
            _joining.PlayerJoined -= Listen;

            foreach (PlayerInput player in _listening)
            {
                if (player != null)
                {
                    player.actions.FindAction("Player/Pause", true).performed -= HandleResetPressed;
                }
            }

            _listening.Clear();
        }

        private void Start()
        {
            foreach (PlayerInput player in _joining.Players)
            {
                Listen(player);
            }

            ApplyPermissions(_flow.State);
        }

        private void Update()
        {
            if (_flow.State != GameState.Playing || _timer == null)
            {
                return;
            }

            int before = _timer.Shown;
            bool finished = _timer.Tick(Time.deltaTime);

            if (_timer.Shown != before)
            {
                TimeShownChanged?.Invoke(_timer.Shown);
            }

            if (finished)
            {
                EndRound();
            }
        }

        // Esc (keyboard) and Start (gamepad) are the Player map's Pause action.
        private void Listen(PlayerInput player)
        {
            if (player == null || !_listening.Add(player))
            {
                return;
            }

            player.actions.FindAction("Player/Pause", true).performed += HandleResetPressed;
            ApplyPermissions(player, _flow.State);
        }

        private void HandleResetPressed(InputAction.CallbackContext context)
        {
            if (GameFlow.CanReset(_flow.State))
            {
                _flow.TryMoveTo(GameState.StartMenu);
            }
        }

        private void HandleStateChanged(GameState from, GameState to)
        {
            if (to == GameState.Countdown)
            {
                _timer = new RoundTimer(_roundSeconds);
                Winner = RoundResult.Draw;
                TimeShownChanged?.Invoke(_timer.Shown);
            }
            else if (to == GameState.StartMenu || to == GameState.Join)
            {
                _timer = null;
                Winner = RoundResult.Draw;
                HideTags();
            }

            ApplyPermissions(to);
        }

        private void EndRound()
        {
            _cages.LockScores();
            Winner = RoundResult.WinnerOf(_cages.Scores, _joining.Players.Count);

            foreach (Vermin vermin in _vermin.GetComponentsInChildren<Vermin>(false))
            {
                vermin.StopFollowing();
            }

            if (!_flow.TryMoveTo(GameState.RoundEnd))
            {
                return;
            }

            _splitScreen.ShowWinner(Winner);
            ShowTags();
            RoundEnded?.Invoke(Winner);
        }

        private void ApplyPermissions(GameState state)
        {
            foreach (PlayerInput player in _joining.Players)
            {
                ApplyPermissions(player, state);
            }
        }

        private void ApplyPermissions(PlayerInput player, GameState state)
        {
            if (player.TryGetComponent(out PlayerAbilities abilities) && player.TryGetComponent(out PlayerIdentity identity))
            {
                abilities.Apply(PlayerPermissions.For(state, identity.Slot == Winner));
            }
        }

        private void ShowTags()
        {
            foreach (PlayerInput player in _joining.Players)
            {
                ResultTag tag = player.GetComponentInChildren<ResultTag>(true);
                var identity = player.GetComponent<PlayerIdentity>();

                if (tag == null || identity == null)
                {
                    continue;
                }

                if (Winner == RoundResult.Draw)
                {
                    tag.Show(_drawText, _drawColour);
                }
                else if (identity.Slot == Winner)
                {
                    tag.Show(_winnerText, identity.Colour);
                }
                else
                {
                    tag.Hide();
                }
            }
        }

        private void HideTags()
        {
            foreach (PlayerInput player in _joining.Players)
            {
                ResultTag tag = player.GetComponentInChildren<ResultTag>(true);

                if (tag != null)
                {
                    tag.Hide();
                }
            }
        }
    }
}
