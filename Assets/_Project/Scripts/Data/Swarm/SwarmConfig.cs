using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>The wasp swarm: how many wasps there are, what makes it collapse, and how it looks and flows.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Swarm Config", fileName = "SwarmConfig")]
    public sealed class SwarmConfig : ScriptableObject
    {
        [field: Header("Rules")]
        [field: SerializeField, Min(1), Tooltip("Wasps at the start of a run.")]
        public int StartingWasps { get; private set; } = 5000;

        [field: SerializeField, Min(1), Tooltip("Most wasps the swarm can hold. Also the size of the GPU buffers.")]
        public int MaxWasps { get; private set; } = 10000;

        [field: SerializeField, Min(0), Tooltip("The swarm collapses when a loss leaves fewer wasps than this.")]
        public int CollapseBelow { get; private set; } = 600;

        [field: SerializeField, Min(0f), Tooltip("Seconds the swarm stays collapsed before it can respawn.")]
        public float CollapseBlockSeconds { get; private set; } = 3f;

        [field: SerializeField, Min(0), Tooltip("Wasps the swarm comes back with when a hive still stands.")]
        public int RespawnWasps { get; private set; } = 2500;

        [field: SerializeField, Min(0f), Tooltip("Wasps gained per second from each standing hive while Hive Call is held.")]
        public float HiveCallWaspsPerSecondPerHive { get; private set; } = 250f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Share of a loss that still gets through the dome.")]
        public float DomeLossFraction { get; private set; } = 0.15f;

        [field: Header("River and shapes")]
        [field: SerializeField, Min(0f), Tooltip("Spring strength pulling a wasp toward its place in the river.")]
        public float RiverSpring { get; private set; } = 40f;

        [field: SerializeField, Min(0f), Tooltip("Damping on the river spring. Higher means less overshoot.")]
        public float RiverDamping { get; private set; } = 0.6f;

        [field: SerializeField, Min(0f), Tooltip("Spring strength pulling a wasp toward its place in a form.")]
        public float ShapeSpring { get; private set; } = 180f;

        [field: SerializeField, Min(0f), Tooltip("Damping on the form spring.")]
        public float ShapeDamping { get; private set; } = 0.75f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Damping while the swarm wobbles after a hit.")]
        public float WobbleDamping { get; private set; } = 0.3f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the wobble after a hit lasts.")]
        public float WobbleSeconds { get; private set; } = 0.35f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the change of form takes to sweep through the swarm.")]
        public float FormWaveSeconds { get; private set; } = 0.6f;

        [field: SerializeField, Min(0f), Tooltip("Lanes per second the wasps flow along the river.")]
        public float FlowSpeed { get; private set; } = 0.08f;

        [field: SerializeField, Min(0f), Tooltip("Length of the river when the Piper moves.")]
        public float RiverLengthMeters { get; private set; } = 8f;

        [field: SerializeField, Min(0f), Tooltip("Half the width of the river.")]
        public float RiverRadius { get; private set; } = 0.55f;

        [field: SerializeField, Min(0f), Tooltip("Sideways sway of the river, in metres.")]
        public float MeanderAmplitude { get; private set; } = 0.25f;

        [field: SerializeField, Min(0f), Tooltip("Length of the river when the Piper stands still.")]
        public float StandingTrailMeters { get; private set; } = 2f;

        [field: SerializeField, Min(0f), Tooltip("Radius of the ring the swarm forms around the Piper.")]
        public float TorusRadius { get; private set; } = 1.5f;

        [field: Header("Wasps")]
        [field: SerializeField, Min(0f), Tooltip("Size of the small shake on each wasp, in metres.")]
        public float BuzzAmplitude { get; private set; } = 0.04f;

        [field: SerializeField, Min(0f), Tooltip("Speed of the small shake on each wasp.")]
        public float BuzzFrequency { get; private set; } = 9f;

        [field: SerializeField, Min(0f), Tooltip("Seconds a wasp takes to fade when the swarm collapses.")]
        public float DyingSeconds { get; private set; } = 1.5f;

        [field: SerializeField, Min(0.001f), Tooltip("Drawn length of one wasp, in metres.")]
        public float WaspLength { get; private set; } = 0.12f;

        [field: Header("Trail and limits")]
        [field: SerializeField, Min(2), Tooltip("Points the Piper's path trail keeps.")]
        public int TrailPoints { get; private set; } = 128;

        [field: SerializeField, Min(0.01f), Tooltip("Metres of travel between two trail points.")]
        public float TrailSpacing { get; private set; } = 0.15f;

        [field: SerializeField, Min(0.001f), Tooltip("Longest time step the swarm simulates. Longer frames are clamped.")]
        public float MaxDeltaTime { get; private set; } = 1f / 30f;

        [field: SerializeField, Min(1f), Tooltip("Half size of the box that wasps may fill. Used for the draw bounds.")]
        public float BoundsMeters { get; private set; } = 60f;

        [field: Header("Colours")]
        [field: SerializeField, Tooltip("Colour of wasps in the river.")]
        public Color RiverColor { get; private set; } = new Color(1f, 0.85f, 0.1f, 1f);

        [field: SerializeField, Tooltip("Colour of the dome.")]
        public Color DomeColor { get; private set; } = new Color(0.2f, 0.55f, 1f, 1f);

        [field: SerializeField, Tooltip("Colour of the sting.")]
        public Color StingColor { get; private set; } = new Color(1f, 0.5f, 0.1f, 1f);

        [field: SerializeField, Tooltip("Colour of the tornado.")]
        public Color TornadoColor { get; private set; } = new Color(0.5f, 0.55f, 0.65f, 1f);

        [field: SerializeField, Tooltip("Colour of the gust.")]
        public Color GustColor { get; private set; } = new Color(0.7f, 0.9f, 1f, 1f);

        [field: SerializeField, Tooltip("Colour of the Hive Call (gold).")]
        public Color HiveCallColor { get; private set; } = new Color(1f, 0.84f, 0.1f, 1f);

        [field: SerializeField, Tooltip("Colour of the Colossus.")]
        public Color ColossusColor { get; private set; } = new Color(0.9f, 0.6f, 0.15f, 1f);
    }
}
