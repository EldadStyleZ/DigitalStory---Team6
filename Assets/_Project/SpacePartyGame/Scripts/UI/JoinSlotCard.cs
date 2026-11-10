using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpacePartyGame
{
    /// <summary>One player slot on the join screen: its cage, its name in its colour, and who has joined it.</summary>
    [DisallowMultipleComponent]
    public sealed class JoinSlotCard : MonoBehaviour
    {
        [SerializeField, Tooltip("The slot's cage picture.")]
        private Image _cage;

        [SerializeField, Tooltip("The slot's name, such as P1.")]
        private TMP_Text _label;

        [SerializeField, Tooltip("Who has joined, or how to join.")]
        private TMP_Text _status;

        [SerializeField, Range(0f, 1f), Tooltip("How see-through the cage is while the slot is free.")]
        private float _freeAlpha = 0.35f;

        /// <summary>Shows the slot as free.</summary>
        public void ShowFree(int slot, Sprite cage, Color colour)
        {
            _cage.sprite = cage;
            _cage.color = new Color(1f, 1f, 1f, _freeAlpha);
            _label.text = $"P{slot + 1}";
            _label.color = colour;
            _status.text = "Space or Start\nto join";
            _status.color = new Color(1f, 1f, 1f, 0.75f);
        }

        /// <summary>Shows the slot as taken, with the kind of device that joined it.</summary>
        public void ShowJoined(string device, Color colour)
        {
            _cage.color = Color.white;
            _status.text = device;
            _status.color = colour;
        }
    }
}
