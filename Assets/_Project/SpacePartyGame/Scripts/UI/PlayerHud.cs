using TMPro;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// One player's on-screen corner: it sits over that player's viewport and shows their score in their colour.
    /// The score is a placeholder until the scoring step drives it through <see cref="SetScore"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class PlayerHud : MonoBehaviour
    {
        [SerializeField, Tooltip("The player's name and score.")]
        private TMP_Text _score;

        private PlayerIdentity _player;
        private int _points;

        /// <summary>Places the HUD over a viewport and shows a player's name and score.</summary>
        public void Show(PlayerIdentity player, Rect viewport)
        {
            var area = (RectTransform)transform;
            area.anchorMin = viewport.min;
            area.anchorMax = viewport.max;
            area.offsetMin = Vector2.zero;
            area.offsetMax = Vector2.zero;
            _player = player;
            _points = 0;
            Refresh();
            gameObject.SetActive(true);
        }

        /// <summary>Moves the HUD over another viewport, keeping its player and score.</summary>
        public void Place(Rect viewport)
        {
            var area = (RectTransform)transform;
            area.anchorMin = viewport.min;
            area.anchorMax = viewport.max;
        }

        /// <summary>Hides the HUD.</summary>
        public void Hide()
        {
            _player = null;
            gameObject.SetActive(false);
        }

        /// <summary>Shows a new score.</summary>
        public void SetScore(int points)
        {
            _points = points;
            Refresh();
        }

        private void Awake()
        {
            if (_score == null)
            {
                Debug.LogError($"{name}: PlayerHud has no score text.", this);
            }
        }

        private void Refresh()
        {
            if (_player == null)
            {
                return;
            }

            _score.text = $"{_player.DisplayName}  {_points}";
            _score.color = _player.Colour;
        }
    }
}
