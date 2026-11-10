using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpacePartyGame
{
    /// <summary>The join screen: four slot cards, filled in as players join, and how to start.</summary>
    public sealed class JoinScreen : StageScreen
    {
        [SerializeField, Tooltip("Reports each player who joins.")]
        private PlayerJoinController _joining;

        [SerializeField, Tooltip("Each slot's colour and cage.")]
        private PlayerPalette _palette;

        [SerializeField, Tooltip("P1 to P4, in order.")]
        private JoinSlotCard[] _cards = new JoinSlotCard[GameFlow.MaxPlayers];

        [SerializeField, Tooltip("Tells the players how to start.")]
        private TMP_Text _hint;

        protected override GameState ShownIn => GameState.Join;

        protected override void OnEnable()
        {
            base.OnEnable();
            _joining.PlayerJoined += HandlePlayerJoined;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _joining.PlayerJoined -= HandlePlayerJoined;
        }

        protected override void OnShown()
        {
            for (int slot = 0; slot < _cards.Length; slot++)
            {
                _cards[slot].ShowFree(slot, _palette.CageOf(slot), _palette.ColourOf(slot));
            }

            foreach (PlayerInput player in _joining.Players)
            {
                HandlePlayerJoined(player);
            }

            UpdateHint();
        }

        private void HandlePlayerJoined(PlayerInput player)
        {
            int slot = player.playerIndex;

            if (slot >= 0 && slot < _cards.Length)
            {
                _cards[slot].ShowJoined(PlayerJoinController.DeviceNameOf(player), _palette.ColourOf(slot));
            }

            UpdateHint();
        }

        private void UpdateHint()
        {
            int needed = Flow.AllowSinglePlayer ? 1 : 2;
            int joined = _joining.Players.Count;
            _hint.text = joined >= needed
                ? "Press join again to start"
                : $"Waiting for players: {joined} of {needed} needed";
        }
    }
}
