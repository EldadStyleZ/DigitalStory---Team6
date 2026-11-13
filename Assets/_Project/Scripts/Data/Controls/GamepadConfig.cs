using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// The controller's numbers for F-010: the sticks' dead zone, how far the right stick aims and how fast it turns the giant's
    /// chase view, and the rumble the controller gives for each moment of play.
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Gamepad Config", fileName = "GamepadConfig")]
    public sealed class GamepadConfig : ScriptableObject
    {
        [field: Header("Sticks")]
        [field: SerializeField, Range(0f, 0.9f), Tooltip("How far a stick must lean, 0 to 1, before it counts. Past it the stick's reading grows again from 0, so a small lean is fine control rather than a jump.")]
        public float StickDeadZone { get; private set; } = 0.2f;

        [field: SerializeField, Min(0f), Tooltip("Metres from the swarm's core that the right stick aims at, leaned all the way, in the top view.")]
        public float AimReachMetres { get; private set; } = 8f;

        [field: SerializeField, Min(0f), Tooltip("Degrees per second the right stick, leaned all the way, turns the giant's chase view.")]
        public float OrbitDegreesPerSecond { get; private set; } = 150f;

        [field: Header("Rumble")]
        [field: SerializeField, Tooltip("A light tick for each note played.")]
        public RumblePulse NotePulse { get; private set; } = new RumblePulse(0f, 0.25f, 0.05f);

        [field: SerializeField, Tooltip("A medium thump when a song is cast.")]
        public RumblePulse CastPulse { get; private set; } = new RumblePulse(0.35f, 0.45f, 0.14f);

        [field: SerializeField, Tooltip("A thump when the swarm's attack, or the giant's, hits beekeepers.")]
        public RumblePulse HitPulse { get; private set; } = new RumblePulse(0.3f, 0.2f, 0.08f);

        [field: SerializeField, Min(0f), Tooltip("The fewest seconds between two hit thumps, so a stream of hits does not buzz the whole time.")]
        public float HitGapSeconds { get; private set; } = 0.15f;

        [field: SerializeField, Tooltip("A heavy one when the giant's slam or its leap lands.")]
        public RumblePulse HeavyPulse { get; private set; } = new RumblePulse(0.9f, 0.6f, 0.3f);

        [field: SerializeField, Tooltip("A long, low one when a hive is destroyed or the swarm collapses.")]
        public RumblePulse LossPulse { get; private set; } = new RumblePulse(0.7f, 0.1f, 0.9f);
    }
}
