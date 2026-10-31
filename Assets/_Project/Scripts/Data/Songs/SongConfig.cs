using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>How the flute remembers notes while the player plays a combo.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Song Config", fileName = "SongConfig")]
    public sealed class SongConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f), Tooltip("Seconds a note counts towards a combo.")]
        public float NoteWindowSeconds { get; private set; } = 3f;

        [field: SerializeField, Min(1), Tooltip("How many recent notes the flute remembers.")]
        public int NotesRemembered { get; private set; } = 5;
    }
}
