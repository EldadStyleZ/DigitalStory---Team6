using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>How dark the night is and how lights cut through it.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Night Look Config", fileName = "NightLookConfig")]
    public sealed class NightLookConfig : ScriptableObject
    {
        [field: Header("Darkness")]
        [field: SerializeField, Tooltip("The darkness for most of the night.")]
        public Color NightColor { get; private set; } = new Color(6f / 255f, 8f / 255f, 24f / 255f, .64f);

        [field: SerializeField, Tooltip("The darkness at sunrise.")]
        public Color DawnColor { get; private set; } = new Color(36f / 255f, 14f / 255f, 18f / 255f, .34f);

        [field: SerializeField, Tooltip("The warm wash over everything at sunrise.")]
        public Color DawnWash { get; private set; } = new Color(1f, 130f / 255f, 70f / 255f, .2f);

        [field: Header("Lamps")]
        [field: SerializeField, Min(1f), Tooltip("How far a lamp clears the darkness, in town units.")]
        public float LampReach { get; private set; } = 150f;

        [field: SerializeField, Range(0f, 1f), Tooltip("How much a lamp clears the darkness at its centre.")]
        public float LampClearing { get; private set; } = .85f;

        [field: SerializeField, Tooltip("The lamp's glow, where alpha is its strength.")]
        public Color LampGlow { get; private set; } = new Color(242f / 255f, 170f / 255f, 70f / 255f, .22f);

        [field: SerializeField, Min(1f), Tooltip("How far a lamp's glow reaches, in town units.")]
        public float LampGlowReach { get; private set; } = 120f;

        [field: Header("The Piper's light")]
        [field: SerializeField, Min(1f), Tooltip("How far the Piper clears the darkness while silent, in town units.")]
        public float PiperReach { get; private set; } = 170f;

        [field: SerializeField, Min(1f), Tooltip("How far the Piper clears the darkness while playing, in town units.")]
        public float PlayingReach { get; private set; } = 260f;

        [field: SerializeField, Min(0f), Tooltip("How much the playing light swells and shrinks, in town units.")]
        public float PlayingPulse { get; private set; } = 6f;

        [field: SerializeField, Min(0f), Tooltip("How quickly the playing light pulses.")]
        public float PlayingPulseRate { get; private set; } = 6f;

        [field: SerializeField, Range(0f, 1f), Tooltip("How much the Piper clears the darkness at the centre.")]
        public float PiperClearing { get; private set; } = .92f;

        [field: Header("Disco")]
        [field: SerializeField, Min(1f), Tooltip("How far the Piper clears the darkness during Disco, in town units.")]
        public float DiscoReach { get; private set; } = 320f;

        [field: SerializeField, Min(0f), Tooltip("How far the colored lights circle from the Piper, in town units.")]
        public float DiscoCircle { get; private set; } = 150f;

        [field: SerializeField, Min(1f), Tooltip("How far each colored light reaches, in town units.")]
        public float DiscoLightReach { get; private set; } = 120f;

        [field: SerializeField, Range(0f, 1f), Tooltip("How bright each colored light is.")]
        public float DiscoLightStrength { get; private set; } = .4f;

        [field: SerializeField, Min(0f), Tooltip("How quickly the colored lights circle.")]
        public float DiscoSpinRate { get; private set; } = 1.7f;

        [field: SerializeField, Min(0f), Tooltip("How quickly the colored lights change color, in hue turns per second.")]
        public float DiscoHueRate { get; private set; } = .39f;
    }
}
