using System.Collections;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>How a vermin shows up: its sprite fades from invisible to fully shown when it spawns.</summary>
    [DisallowMultipleComponent]
    public sealed class VerminAppearance : MonoBehaviour
    {
        [SerializeField, Tooltip("The sprite that fades in.")]
        private SpriteRenderer _sprite;

        [SerializeField, Min(0f), Tooltip("How long the fade in takes, in seconds.")]
        private float _fadeSeconds = 0.5f;

        private Coroutine _fade;

        /// <summary>Starts the sprite at alpha 0 and lerps it up to 1. The spawner calls it as the vermin appears.</summary>
        public void FadeInOnSpawn()
        {
            if (_fade != null)
            {
                StopCoroutine(_fade);
            }

            SetAlpha(0f);
            _fade = StartCoroutine(FadeRoutine());
        }

        private void Awake()
        {
            if (_sprite == null)
            {
                Debug.LogError($"{name}: VerminAppearance is missing its sprite.", this);
            }
        }

        // A vermin put back in the pool mid-fade comes out fully shown unless it is faded in again.
        private void OnDisable()
        {
            _fade = null;
            SetAlpha(1f);
        }

        private IEnumerator FadeRoutine()
        {
            float elapsed = 0f;

            while (elapsed < _fadeSeconds)
            {
                elapsed += Time.deltaTime;
                SetAlpha(FadeIn.AlphaAt(elapsed, _fadeSeconds));
                yield return null;
            }

            SetAlpha(1f);
            _fade = null;
        }

        private void SetAlpha(float alpha)
        {
            Color color = _sprite.color;
            color.a = alpha;
            _sprite.color = color;
        }
    }
}
