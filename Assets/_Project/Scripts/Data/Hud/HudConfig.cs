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

        [field: Header("Breath bar")]
        [field: SerializeField, Tooltip("Breath left.")]
        public Color BreathColor { get; private set; } = new Color32(0xF2, 0xB5, 0x44, 0xFF);

        [field: SerializeField, Tooltip("Breath left while exhausted.")]
        public Color ExhaustedColor { get; private set; } = new Color32(0x8A, 0x5A, 0x5A, 0xFF);

        [field: SerializeField, Tooltip("Behind the breath left.")]
        public Color BreathTrackColor { get; private set; } = new Color(1f, 1f, 1f, 0.12f);

        [field: Header("Combos row")]
        [field: SerializeField, Tooltip("The hole numbers on a combo.")]
        public Color KeyTextColor { get; private set; } = new Color32(0x1A, 0x12, 0x09, 0xFF);

        [field: SerializeField, Tooltip("The border of a song that was just cast.")]
        public Color GlowColor { get; private set; } = new Color32(0xE8, 0xC5, 0x47, 0xFF);

        [field: SerializeField, Min(0f), Tooltip("Seconds a song stays lit after it is cast.")]
        public float GlowSeconds { get; private set; } = .9f;

        [field: SerializeField, Range(0f, 1f), Tooltip("How visible a song is while it rests.")]
        public float RestingOpacity { get; private set; } = .4f;
    }
}
