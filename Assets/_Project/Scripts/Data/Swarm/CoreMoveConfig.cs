using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// How fast the swarm's core moves, what slows it down and how far it may go, and how long a flute note sounds. A song
    /// that changes the core's speed holds its own number.
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Core Move Config", fileName = "CoreMoveConfig")]
    public sealed class CoreMoveConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f), Tooltip("Speed of the core in metres per second.")]
        public float Speed { get; private set; } = 4.5f;

        [field: SerializeField, Min(1f), Tooltip(
            "How far the core may go from the middle of the map (the world origin) along either ground axis, in metres. "
            + "The giant keeps to it too. 42 m reaches the outer edge of the scenery (LayoutConfig.ArenaHalfSize) and stays well inside the 80 m ground.")]
        public float PlayAreaHalfSize { get; private set; } = 42f;

        [field: SerializeField, Min(1f), Tooltip("How much faster the core moves while Shift is held. 1 turns the sprint off.")]
        public float SprintSpeedMultiplier { get; private set; } = 1.8f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Share of speed kept while the core is in smoke.")]
        public float SmokeSpeedMultiplier { get; private set; } = 0.6f;

        [field: SerializeField, Min(0f), Tooltip("Shortest time a flute note sounds, in seconds, even for a tap. A held key sounds longer.")]
        public float NoteSeconds { get; private set; } = 0.3f;
    }
}
