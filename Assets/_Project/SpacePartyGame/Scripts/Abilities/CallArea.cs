using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// The ring that shows a player's call. It fades in when shown and out when hidden, never popping, turns slowly
    /// while visible, and takes the calling player's colour.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CallArea : MonoBehaviour
    {
        [SerializeField, Tooltip("The white ring sprite, tinted with the player's colour.")]
        private SpriteRenderer _ring;

        [SerializeField, Min(0.01f), Tooltip("How long the ring takes to fade in or out, in seconds.")]
        private float _fadeSeconds = 0.25f;

        [SerializeField, Range(0f, 1f), Tooltip("How see-through the ring is when fully shown (1 is solid).")]
        private float _shownAlpha = 0.85f;

        [SerializeField, Tooltip("How fast the ring turns, in degrees per second.")]
        private float _degreesPerSecond = 20f;

        private float _alpha;
        private bool _isShown;

        /// <summary>Whether the ring is fading in or fully shown.</summary>
        public bool IsShown => _isShown;

        /// <summary>Sizes the ring so its edge sits at this distance from its centre.</summary>
        public void SetRadius(float radius)
        {
            float width = _ring.sprite.bounds.size.x;
            float scale = width > 0f ? (radius * 2f) / width : 1f;
            _ring.transform.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>Starts fading the ring in, in a player's colour.</summary>
        public void Show(Color colour)
        {
            _isShown = true;
            Color tint = colour;
            tint.a = _ring.color.a;
            _ring.color = tint;
            gameObject.SetActive(true);
        }

        /// <summary>Starts fading the ring out; it switches itself off once invisible.</summary>
        public void Hide() => _isShown = false;

        /// <summary>Hides the ring at once, with no fade, such as when it is first made.</summary>
        public void HideNow()
        {
            _isShown = false;
            _alpha = 0f;
            ApplyAlpha();
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            if (_ring == null)
            {
                Debug.LogError($"{name}: CallArea is missing its ring sprite.", this);
            }
        }

        private void Update()
        {
            float target = _isShown ? _shownAlpha : 0f;
            _alpha = Mathf.MoveTowards(_alpha, target, Time.deltaTime * _shownAlpha / _fadeSeconds);
            ApplyAlpha();
            _ring.transform.Rotate(0f, 0f, _degreesPerSecond * Time.deltaTime);

            if (!_isShown && _alpha <= 0f)
            {
                gameObject.SetActive(false);
            }
        }

        private void ApplyAlpha()
        {
            Color colour = _ring.color;
            colour.a = _alpha;
            _ring.color = colour;
        }
    }
}
