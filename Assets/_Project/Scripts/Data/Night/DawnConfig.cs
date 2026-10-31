using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>When the night starts to turn into dawn.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Dawn Config", fileName = "DawnConfig")]
    public sealed class DawnConfig : ScriptableObject
    {
        [field: SerializeField, Range(0f, .99f), Tooltip("Share of the night gone when the sky starts to warm.")]
        public float StartsAt { get; private set; } = .6f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Share of the night gone when the sun is about to rise.")]
        public float NearAt { get; private set; } = .82f;
    }
}
