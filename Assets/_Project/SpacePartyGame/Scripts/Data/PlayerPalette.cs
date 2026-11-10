using System;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// Each player slot's cage and colour, in one place so they are easy to change. The colours were measured from
    /// the frame of each cage sprite by Tools/space-party-art (see NOTES.md).
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerPalette", menuName = "Space Party Game/Player Palette")]
    public sealed class PlayerPalette : ScriptableObject
    {
        [Serializable]
        private struct Slot
        {
            [Tooltip("The cage that belongs to this player.")]
            public Sprite Cage;

            [Tooltip("The player's colour, taken from the cage's frame.")]
            public Color Colour;
        }

        [SerializeField, Tooltip("P1 to P4, in order.")]
        private Slot[] _slots = new Slot[GameFlow.MaxPlayers];

        /// <summary>The colour of a slot, from 0 to 3.</summary>
        public Color ColourOf(int slot) => IsValid(slot) ? _slots[slot].Colour : Color.white;

        /// <summary>The cage sprite of a slot, from 0 to 3.</summary>
        public Sprite CageOf(int slot) => IsValid(slot) ? _slots[slot].Cage : null;

        private bool IsValid(int slot)
        {
            if (slot >= 0 && slot < _slots.Length)
            {
                return true;
            }

            Debug.LogError($"{name}: there is no player slot {slot}.", this);
            return false;
        }

        private void OnValidate()
        {
            if (_slots.Length != GameFlow.MaxPlayers)
            {
                Array.Resize(ref _slots, GameFlow.MaxPlayers);
            }
        }
    }
}
