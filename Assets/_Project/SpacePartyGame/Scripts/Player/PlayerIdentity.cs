using System;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// Which player this is: its slot, from 0 to 3, and its colour. The join step sets them through
    /// <see cref="Initialize"/>; the name tag and later the cages and notes read them.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerIdentity : MonoBehaviour
    {
        /// <summary>How many players can play at once.</summary>
        public const int MaxPlayers = 4;

        [SerializeField, Range(0, MaxPlayers - 1), Tooltip("The player's slot: 0 is P1, 3 is P4.")]
        private int _slot;

        [SerializeField, Tooltip("The player's colour, used for the name tag and everything that is theirs.")]
        private Color _colour = Color.white;

        /// <summary>Raised whenever the slot or colour changes.</summary>
        public event Action Changed;

        /// <summary>The player's slot, from 0 to 3.</summary>
        public int Slot => _slot;

        /// <summary>The player's colour.</summary>
        public Color Colour => _colour;

        /// <summary>The name shown above the player, from "P1" to "P4".</summary>
        public string DisplayName => $"P{_slot + 1}";

        /// <summary>Sets the player's slot and colour.</summary>
        public void Initialize(int slot, Color colour)
        {
            if (slot < 0 || slot >= MaxPlayers)
            {
                Debug.LogError($"{name}: player slot {slot} is outside 0 to {MaxPlayers - 1}.", this);
                return;
            }

            _slot = slot;
            _colour = colour;
            Changed?.Invoke();
        }

        private void OnValidate()
        {
            Changed?.Invoke();
        }
    }
}
