using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// Every number for the blow power, shared by both players: holding the Left note charges it, letting go when
    /// it is full blows everything in front of the player away.
    /// </summary>
    [CreateAssetMenu(fileName = "BlowSettings", menuName = "Space Party Game/Blow Settings")]
    public sealed class BlowSettings : ScriptableObject
    {
        [Header("Charge")]
        [SerializeField, Min(0.1f), Tooltip("How long the Left note must be held to fill the bar, in seconds.")]
        private float _chargeSeconds = 3f;

        [Header("Bar (under the player's feet)")]
        [SerializeField, Min(0.05f), Tooltip("The bar's width, in units.")]
        private float _barWidth = 0.9f;

        [SerializeField, Min(0.02f), Tooltip("The bar's height, in units.")]
        private float _barHeight = 0.13f;

        [SerializeField, Tooltip("Where the bar sits, from the player's feet, in units.")]
        private Vector2 _barOffset = new Vector2(0f, -0.22f);

        [SerializeField, Tooltip("The empty part of the bar.")]
        private Color _barBackColour = new Color(0.231f, 0.129f, 0.114f, 0.75f);

        [SerializeField, Min(0f), Tooltip("How long the bar takes to fade in and out, in seconds.")]
        private float _barFadeSeconds = 0.12f;

        public float ChargeSeconds => _chargeSeconds;
        public float BarWidth => _barWidth;
        public float BarHeight => _barHeight;
        public Vector2 BarOffset => _barOffset;
        public Color BarBackColour => _barBackColour;
        public float BarFadeSeconds => _barFadeSeconds;
    }
}
