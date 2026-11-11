using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// The blow's charge bar under the player's feet, filling in the player's colour. It is on screen only while
    /// the blow charges, fading in and out.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BlowBar : MonoBehaviour
    {
        [SerializeField, Tooltip("The empty bar behind the fill.")]
        private SpriteRenderer _back;

        [SerializeField, Tooltip("The filled part, growing from the left.")]
        private SpriteRenderer _fill;

        private BlowSettings _settings;
        private Color _colour = Color.white;
        private float _progress;
        private float _alpha;
        private bool _shown;

        /// <summary>Gives the bar its sizes and colours.</summary>
        public void Initialize(BlowSettings settings)
        {
            _settings = settings;
            transform.localPosition = settings.BarOffset;
            _back.size = new Vector2(settings.BarWidth, settings.BarHeight);
            _back.transform.localPosition = Vector3.zero;
            Draw();
        }

        /// <summary>Shows the bar this full (0 to 1) in a colour.</summary>
        public void Show(float progress, Color colour)
        {
            _progress = Mathf.Clamp01(progress);
            _colour = colour;
            _shown = true;
            gameObject.SetActive(true);
        }

        /// <summary>Fades the bar out.</summary>
        public void Hide()
        {
            _shown = false;
        }

        /// <summary>Hides the bar at once.</summary>
        public void HideNow()
        {
            _shown = false;
            _alpha = 0f;
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            if (_back == null || _fill == null)
            {
                Debug.LogError($"{name}: BlowBar is missing its back or fill.", this);
            }
        }

        private void Update()
        {
            float fade = _settings != null && _settings.BarFadeSeconds > 0f ? Time.deltaTime / _settings.BarFadeSeconds : 1f;
            _alpha = Mathf.MoveTowards(_alpha, _shown ? 1f : 0f, fade);
            Draw();

            if (!_shown && _alpha <= 0f)
            {
                gameObject.SetActive(false);
            }
        }

        private void Draw()
        {
            if (_settings == null || _back == null || _fill == null)
            {
                return;
            }

            Color back = _settings.BarBackColour;
            back.a *= _alpha;
            _back.color = back;

            Color fill = _colour;
            fill.a = _alpha;
            _fill.color = fill;

            float width = _settings.BarWidth * _progress;
            _fill.enabled = width > 0.001f;
            _fill.size = new Vector2(Mathf.Max(width, _settings.BarHeight), _settings.BarHeight);
            _fill.transform.localScale = width < _settings.BarHeight ? new Vector3(width / _settings.BarHeight, 1f, 1f) : Vector3.one;

            // The fill's sprite is centred, so it moves right as it grows to keep its left end still.
            _fill.transform.localPosition = new Vector3((width - _settings.BarWidth) * 0.5f, 0f, 0f);
        }
    }
}
