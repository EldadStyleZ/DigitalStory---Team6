using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>The colors and timing shared by everything drawn over the game.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Hud Config", fileName = "HudConfig")]
    public sealed class HudConfig : ScriptableObject
    {
        [field: Header("Colors")]
        [field: SerializeField, Tooltip("Main text.")]
        public Color TextColor { get; private set; } = new Color32(0xF3, 0xEA, 0xD6, 0xFF);

        [field: SerializeField, Tooltip("Small captions.")]
        public Color CaptionColor { get; private set; } = new Color32(0xAA, 0xA7, 0xBB, 0xFF);

        [field: SerializeField, Tooltip("Behind signs, chips and banners.")]
        public Color PanelColor { get; private set; } = new Color32(0x12, 0x14, 0x22, 0xD6);

        [field: SerializeField, Tooltip("Darkens the game behind a menu.")]
        public Color OverlayColor { get; private set; } = new Color32(0x09, 0x0A, 0x14, 0xBD);

        [field: Header("Buttons")]
        [field: SerializeField, Tooltip("The main button on a menu.")]
        public Color ButtonColor { get; private set; } = new Color32(0xE8, 0xC5, 0x47, 0xFF);

        [field: SerializeField, Tooltip("The words on the main button.")]
        public Color ButtonTextColor { get; private set; } = new Color32(0x23, 0x1C, 0x0C, 0xFF);

        [field: SerializeField, Tooltip("The other button on a menu.")]
        public Color SecondButtonColor { get; private set; } = new Color(1f, 1f, 1f, 0.1f);

        [field: Header("Breath bar")]
        [field: SerializeField, Tooltip("Breath left.")]
        public Color BreathColor { get; private set; } = new Color32(0xF2, 0xB5, 0x44, 0xFF);

        [field: SerializeField, Tooltip("Breath left while exhausted.")]
        public Color ExhaustedColor { get; private set; } = new Color32(0x8A, 0x5A, 0x5A, 0xFF);

        [field: SerializeField, Tooltip("Behind the breath left.")]
        public Color BreathTrackColor { get; private set; } = new Color(1f, 1f, 1f, 0.12f);

        [field: Header("Sky bar")]
        [field: SerializeField, Tooltip("The sky from dusk on the left to sunrise on the right.")]
        public Gradient SkyGradient { get; private set; } = NewSkyGradient();

        [field: SerializeField, Tooltip("The moon on the sky bar.")]
        public Color MoonColor { get; private set; } = new Color32(0xFF, 0xF3, 0xC8, 0xFF);

        [field: SerializeField, Tooltip("The sun on the sky bar.")]
        public Color SunColor { get; private set; } = new Color32(0xFF, 0x9F, 0x3A, 0xFF);

        [field: SerializeField, Tooltip("The pulse around the sky bar when dawn is close.")]
        public Color DawnPulseColor { get; private set; } = new Color32(0xFF, 0x7A, 0x4A, 0xFF);

        [field: Header("Flute")]
        [field: SerializeField, Tooltip("The body of the flute.")]
        public Color FluteColor { get; private set; } = new Color32(0xBD, 0x84, 0x43, 0xFF);

        [field: SerializeField, Tooltip("A hole that is not sounding.")]
        public Color HoleColor { get; private set; } = new Color32(0x1D, 0x14, 0x0A, 0xFF);

        [field: SerializeField, Tooltip("The number on a hole that is not sounding.")]
        public Color HoleNumberColor { get; private set; } = new Color32(0xFF, 0xEC, 0xC8, 0x80);

        [field: SerializeField, Range(0f, 1f), Tooltip("How visible the flute is while out of breath.")]
        public float ExhaustedFluteOpacity { get; private set; } = .5f;

        [field: Header("Minimap")]
        [field: SerializeField, Tooltip("A cat on the map.")]
        public Color MinimapCatColor { get; private set; } = new Color32(0xFF, 0x5A, 0x4A, 0xFF);

        [field: SerializeField, Tooltip("The Piper on the map.")]
        public Color MinimapPiperColor { get; private set; } = new Color32(0xFF, 0xF0, 0xA0, 0xFF);

        [field: SerializeField, Tooltip("The outline of what the camera sees.")]
        public Color MinimapWindowColor { get; private set; } = new Color32(0xB4, 0xB4, 0xBC, 0xFF);

        [field: Header("Combos row")]
        [field: SerializeField, Tooltip("The hole numbers on a combo.")]
        public Color KeyTextColor { get; private set; } = new Color32(0x1A, 0x12, 0x09, 0xFF);

        [field: SerializeField, Tooltip("The border of a song that was just cast.")]
        public Color GlowColor { get; private set; } = new Color32(0xE8, 0xC5, 0x47, 0xFF);

        [field: SerializeField, Min(0f), Tooltip("Seconds a song stays lit after it is cast.")]
        public float GlowSeconds { get; private set; } = .9f;

        [field: SerializeField, Range(0f, 1f), Tooltip("How visible a song is while it rests.")]
        public float RestingOpacity { get; private set; } = .4f;

        private static Gradient NewSkyGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color32(0x1C, 0x21, 0x40, 0xFF), 0f),
                    new GradientColorKey(new Color32(0x2F, 0x2A, 0x55, 0xFF), .5f),
                    new GradientColorKey(new Color32(0xB8, 0x60, 0x4A, 0xFF), .82f),
                    new GradientColorKey(new Color32(0xF6, 0xC2, 0x5A, 0xFF), 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }
    }
}
