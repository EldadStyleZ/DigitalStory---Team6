using TMPro;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// Shows the player's name, "P1" to "P4", above its head in the player's colour. Its dark outline and shadow,
    /// which keep it readable on the light floor, come from the shared NameTag material. It sits on its own child,
    /// so it never flips with the body.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshPro))]
    public sealed class NameTag : MonoBehaviour
    {
        [SerializeField, Tooltip("The player whose name and colour this shows.")]
        private PlayerIdentity _identity;

        private TextMeshPro _text;

        private void Awake()
        {
            _text = GetComponent<TextMeshPro>();

            if (_identity == null)
            {
                Debug.LogError($"{name}: NameTag has no PlayerIdentity.", this);
            }
        }

        private void OnEnable()
        {
            if (_identity != null)
            {
                _identity.Changed += Show;
            }

            Show();
        }

        private void OnDisable()
        {
            if (_identity != null)
            {
                _identity.Changed -= Show;
            }
        }

        private void Show()
        {
            if (_identity == null || _text == null)
            {
                return;
            }

            _text.text = _identity.DisplayName;
            _text.color = _identity.Colour;
        }
    }
}
