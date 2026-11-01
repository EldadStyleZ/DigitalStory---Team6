using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>How long a combo can take.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Lab/Combo Config", fileName = "ComboConfig")]
    public sealed class ComboConfig : ScriptableObject
    {
        [field: SerializeField, Min(0.05f), Tooltip("Seconds allowed before the next note, or the combo fails.")]
        public float SecondsToNextNote { get; private set; } = 1.4f;
    }
}
