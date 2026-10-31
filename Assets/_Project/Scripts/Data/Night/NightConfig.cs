using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>The numbers for the game's one night.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Night Config", fileName = "NightConfig")]
    public sealed class NightConfig : ScriptableObject
    {
        [field: SerializeField, Tooltip("The rats, the goal and the time until dawn.")]
        public NightRules Rules { get; private set; } = new NightRules(2211, 170, 115, 150f, 5, 1);
    }
}
