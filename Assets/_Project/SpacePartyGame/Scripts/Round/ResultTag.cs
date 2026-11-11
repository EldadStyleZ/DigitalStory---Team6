using TMPro;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// The big word over a player's head at the round's end, such as "WINNER". It sits on its own child above the
    /// name tag, hidden until the round ends, and pops in with a short grow.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshPro))]
    public sealed class ResultTag : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("How long the tag takes to grow in, in seconds.")]
        private float _popSeconds = 0.35f;

        [SerializeField, Tooltip("How the tag grows in: size over time, 0 to 1. Going past 1 overshoots.")]
        private AnimationCurve _pop = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.7f, 1.15f), new Keyframe(1f, 1f));

        private TextMeshPro _text;
        private Vector3 _scale;
        private float _shownAt;

        /// <summary>Shows a word in a colour.</summary>
        public void Show(string text, Color colour)
        {
            Init();
            _text.text = text;
            _text.color = colour;
            _shownAt = Time.time;
            transform.localScale = Vector3.zero;
            gameObject.SetActive(true);
        }

        /// <summary>Hides the tag.</summary>
        public void Hide()
        {
            Init();
            transform.localScale = _scale;
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            if (_text != null)
            {
                return;
            }

            _text = GetComponent<TextMeshPro>();
            _scale = transform.localScale;
        }

        private void Update()
        {
            float t = _popSeconds > 0f ? Mathf.Clamp01((Time.time - _shownAt) / _popSeconds) : 1f;
            transform.localScale = _scale * _pop.Evaluate(t);
        }
    }
}
