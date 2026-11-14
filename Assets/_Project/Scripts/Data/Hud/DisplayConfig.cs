using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// The display settings a player starts with (plan section AD), before they change them in the settings panel (Esc).
    /// While no asset is given to the hive defense root, it uses these code defaults.
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/Display Config", fileName = "DisplayConfig")]
    public sealed class DisplayConfig : ScriptableObject
    {
        [field: SerializeField, Tooltip("The frame rate a player starts with.")]
        public FrameRateChoice DefaultFrameRate { get; private set; } = FrameRateChoice.Display;

        [field: SerializeField, Tooltip("The render scale a player starts with. Auto keeps a laptop's display native and draws a very large one at fewer lines.")]
        public RenderScaleChoice DefaultRenderScale { get; private set; } = RenderScaleChoice.Auto;

        [field: SerializeField, Range(720, 4320), Tooltip("The most lines Auto draws the 3D view at; a taller display gets it scaled up.")]
        public int AutoMostLines { get; private set; } = 1600;
    }
}
