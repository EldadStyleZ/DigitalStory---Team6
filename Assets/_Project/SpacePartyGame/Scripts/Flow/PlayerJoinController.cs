using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpacePartyGame
{
    /// <summary>
    /// Lets players join on the join screen and keeps every player's input in step with the game's stage.
    /// </summary>
    /// <remarks>
    /// PlayerInputManager spawns a player when an unused device presses Join, so each device joins once. Its join
    /// action is built from the Join map's bindings, rather than pointing into the shared actions asset, because the
    /// first player's PlayerInput uses that asset itself and limits it to that player's devices. A player who has
    /// joined presses Join again, on their own Join map, to start the match.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputManager))]
    public sealed class PlayerJoinController : MonoBehaviour
    {
        [SerializeField, Tooltip("The game's stages.")]
        private GameFlowManager _flow;

        [SerializeField, Tooltip("The controls; joining uses the bindings of its Join/Join action.")]
        private InputActionAsset _controls;

        [SerializeField, Tooltip("Each slot's colour and cage.")]
        private PlayerPalette _palette;

        [SerializeField, Tooltip("The playable floor, to check the spawn points.")]
        private MapBounds _map;

        [SerializeField, Tooltip("The parent for spawned players.")]
        private Transform _playersParent;

        [SerializeField, Tooltip("Where P1 to P4 appear, in world space.")]
        private Vector2[] _spawnPoints =
        {
            new Vector2(-2f, 1f),
            new Vector2(2f, 1f),
            new Vector2(-2f, -1.5f),
            new Vector2(2f, -1.5f),
        };

        private readonly List<PlayerInput> _players = new List<PlayerInput>();
        private PlayerInputManager _manager;

        /// <summary>Raised when a player joins, after its slot, colour and place are set.</summary>
        public event Action<PlayerInput> PlayerJoined;

        /// <summary>The players who have joined, in the order they joined.</summary>
        public IReadOnlyList<PlayerInput> Players => _players;

        /// <summary>Where P1 to P4 appear, so cages can keep clear of them.</summary>
        public IReadOnlyList<Vector2> SpawnPoints => _spawnPoints;

        /// <summary>A short name for the kind of device a player uses, such as "Keyboard" or "Gamepad".</summary>
        public static string DeviceNameOf(PlayerInput player) =>
            player.currentControlScheme == "Gamepad" ? "Gamepad" : "Keyboard";

        private void Awake()
        {
            _manager = GetComponent<PlayerInputManager>();

            if (_flow == null || _controls == null || _palette == null || _map == null)
            {
                Debug.LogError($"{name}: PlayerJoinController is missing its flow, controls, palette or map.", this);
                return;
            }

            _manager.DisableJoining();
            _manager.joinAction = new InputActionProperty(CopyOfJoinAction());
        }

        private InputAction CopyOfJoinAction()
        {
            InputAction source = _controls.FindAction("Join/Join", true);
            var copy = new InputAction("Join", InputActionType.Button);

            foreach (InputBinding binding in source.bindings)
            {
                copy.AddBinding(binding.path, binding.interactions, binding.processors, binding.groups);
            }

            return copy;
        }

        private void OnEnable()
        {
            _manager.onPlayerJoined += HandlePlayerJoined;
            _flow.StateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            _manager.onPlayerJoined -= HandlePlayerJoined;
            _flow.StateChanged -= HandleStateChanged;

            foreach (PlayerInput player in _players)
            {
                player.actions.FindAction("Join/Join", true).performed -= HandleJoinPressed;
            }
        }

        private void Start()
        {
            SetJoining(_flow.State == GameState.Join);
        }

        private void HandleStateChanged(GameState from, GameState to)
        {
            SetJoining(to == GameState.Join);

            foreach (PlayerInput player in _players)
            {
                ApplyInputMode(player, to);
            }
        }

        // The four-player limit is kept here rather than by PlayerInputManager's max player count, which logs a
        // warning every time the last slot fills.
        private void SetJoining(bool on)
        {
            if (on && _players.Count < GameFlow.MaxPlayers)
            {
                _manager.EnableJoining();
            }
            else
            {
                _manager.DisableJoining();
            }
        }

        private void HandlePlayerJoined(PlayerInput player)
        {
            int slot = player.playerIndex;
            player.name = $"Player {slot + 1}";
            player.transform.SetParent(_playersParent, true);
            player.transform.position = SpawnPointFor(slot);
            player.GetComponent<PlayerIdentity>().Initialize(slot, _palette.ColourOf(slot));
            player.actions.FindAction("Join/Join", true).performed += HandleJoinPressed;
            _players.Add(player);
            ApplyInputMode(player, _flow.State);
            SetJoining(_flow.State == GameState.Join);
            PlayerJoined?.Invoke(player);
        }

        private void HandleJoinPressed(InputAction.CallbackContext context)
        {
            _flow.RequestMatchStart(_players.Count);
        }

        private Vector2 SpawnPointFor(int slot)
        {
            Vector2 point = slot < _spawnPoints.Length ? _spawnPoints[slot] : Vector2.zero;

            if (_map.Contains(point))
            {
                return point;
            }

            Debug.LogError($"{name}: spawn point {slot + 1} at {point} is not on the floor, so the player starts in the middle.", this);
            return _map.Rect.center;
        }

        private static void ApplyInputMode(PlayerInput player, GameState state)
        {
            string map = PlayerInputModes.MapFor(state);

            if (map == null)
            {
                player.DeactivateInput();
                return;
            }

            player.ActivateInput();
            player.SwitchCurrentActionMap(map);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;

            foreach (Vector2 point in _spawnPoints)
            {
                Gizmos.DrawWireSphere(point, 0.3f);
            }
        }
    }
}
