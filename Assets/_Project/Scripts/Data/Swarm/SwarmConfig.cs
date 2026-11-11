using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// The wasp swarm: how many wasps there are, how many it may hold, what makes it collapse, and how it looks, flows and
    /// glows. The swarm grows during a match (plan section H): each nest collected raises its capacity, up to MaxWasps, so
    /// the collapse line, the low warning and the respawn are shares of the capacity, not fixed counts.
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Swarm Config", fileName = "SwarmConfig")]
    public sealed class SwarmConfig : ScriptableObject
    {
        // Each wasp is drawn twice as long as in the first tuning, so the swarm has fewer, bigger wasps that read one by one.
        // The shares are the old fixed counts over the old 2,000 start (240, 600, 1,000 and 300), so the balance between
        // losses, gains and the collapse stays the same as the swarm grows.
        [field: Header("Rules")]
        [field: SerializeField, Min(0), Tooltip("Wasps at the start of a run. Keep it at most StartingCapacity.")]
        public int StartingWasps { get; private set; } = 800;

        [field: SerializeField, Min(1), Tooltip("How many wasps the swarm may hold at the start of a run. Each nest collected raises it, up to MaxWasps.")]
        public int StartingCapacity { get; private set; } = 800;

        [field: SerializeField, Min(1), Tooltip("Most wasps the swarm's capacity can ever reach. Also the size of the GPU buffers.")]
        public int MaxWasps { get; private set; } = 4000;

        [field: SerializeField, Range(0f, 1f), Tooltip("The swarm collapses when a loss leaves fewer wasps than this share of its capacity. A song or a gift to a hive that would leave fewer is refused, so paying never collapses it.")]
        public float CollapseShare { get; private set; } = 0.12f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the swarm stays collapsed before it can respawn.")]
        public float CollapseBlockSeconds { get; private set; } = 3f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Share of its capacity that the swarm takes, at most, from the hives' stores when it comes back after a collapse, while a hive still stands.")]
        public float RespawnShare { get; private set; } = 0.5f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Share of its capacity that the swarm comes back with at least after a collapse, however little the stores hold, so the player can always act. Keep it from CollapseShare to 1.")]
        public float MinRespawnShare { get; private set; } = 0.15f;

        [field: SerializeField, Range(0f, 1f), Tooltip("The HUD warns that the swarm is low while it has fewer wasps than this share of its capacity.")]
        public float LowShare { get; private set; } = 0.3f;

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

        [field: SerializeField, Min(0f), Tooltip("Length of the river when the core moves.")]
        public float RiverLengthMeters { get; private set; } = 11f;

        [field: SerializeField, Min(0f), Tooltip("Half the width of the river, and of the ring around the core. Wide enough that the big wasps keep apart instead of merging into one band. The river is less than half as tall as it is wide.")]
        public float RiverRadius { get; private set; } = 1.2f;

        [field: SerializeField, Min(0f), Tooltip("Sideways sway of the river, in metres.")]
        public float MeanderAmplitude { get; private set; } = 0.25f;

        [field: SerializeField, Min(0f), Tooltip("Metres of path the river needs before it stretches out behind the core. On a shorter path, at the start of a run, it curls into the ring around it.")]
        public float StandingTrailMeters { get; private set; } = 2f;

        [field: SerializeField, Min(0f), Tooltip("Radius of the ring the swarm forms around the core. Keep it more than RiverRadius plus CoreRingRadius, so the ring never covers the inner ring at its centre.")]
        public float TorusRadius { get; private set; } = 2.4f;

        [field: SerializeField, Min(0f), Tooltip("The core counts as standing while it stays within this many metres (on the ground) of where it stopped, so settling never counts as a step.")]
        public float StandStillMeters { get; private set; } = 0.05f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the core stands before the river curls into the ring around it.")]
        public float StandDelaySeconds { get; private set; } = 0.5f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the river takes to ease into the ring, and back out once the core moves. 0 switches at once.")]
        public float StandBlendSeconds { get; private set; } = 0.75f;

        // The core is an invisible point, so a few wasps mark it: a tight ring that stays on it, still or moving.
        [field: Header("The core")]
        [field: SerializeField, Range(0f, 0.3f), Tooltip("Share of the wasps that circle the core in a tight inner ring, so the centre of the swarm, which is the player, reads from the camera. 0 hides the inner ring.")]
        public float CoreRingShare { get; private set; } = 0.05f;

        [field: SerializeField, Min(0f), Tooltip("Radius of the inner ring around the core, in metres. Keep it below TorusRadius minus RiverRadius, so the two rings stay apart.")]
        public float CoreRingRadius { get; private set; } = 0.6f;

        [field: SerializeField, Range(0f, 1f), Tooltip("How hot the inner ring's wasps glow, from 0 to 1, while no song runs; half that while one does, as the river quiets. The calm river glows at 0.1, the Dome at 0.4, and 0.95 and up flash white.")]
        public float CoreRingGlow { get; private set; } = 0.75f;

        [field: Header("Wasps")]
        [field: SerializeField, Min(0f), Tooltip("Size of the small shake on each wasp, in metres.")]
        public float BuzzAmplitude { get; private set; } = 0.04f;

        [field: SerializeField, Min(0f), Tooltip("Speed of the small shake on each wasp.")]
        public float BuzzFrequency { get; private set; } = 9f;

        [field: SerializeField, Min(0f), Tooltip("Seconds a wasp takes to fade when the swarm collapses.")]
        public float DyingSeconds { get; private set; } = 1.5f;

        [field: SerializeField, Min(0.001f), Tooltip("Drawn length of one wasp, in metres.")]
        public float WaspLength { get; private set; } = 0.24f;

        [field: Header("Trail and limits")]
        [field: SerializeField, Min(2), Tooltip("Points the core's path trail keeps.")]
        public int TrailPoints { get; private set; } = 128;

        [field: SerializeField, Min(0.01f), Tooltip("Metres of travel between two trail points.")]
        public float TrailSpacing { get; private set; } = 0.15f;

        [field: SerializeField, Min(0.001f), Tooltip("Longest time step the swarm simulates. Longer frames are clamped.")]
        public float MaxDeltaTime { get; private set; } = 1f / 30f;

        [field: SerializeField, Min(1f), Tooltip("Half size of the box that wasps may fill. Used for the draw bounds.")]
        public float BoundsMeters { get; private set; } = 60f;

        // The wasps glow by these colours, so they are light and saturated: the shader whitens the middle of each wasp itself.
        [field: Header("Colours")]
        [field: SerializeField, Tooltip("Colour of the plain swarm: the river, the ring around the core and the collapsed swarm. A warm yellow that glows yellow-white.")]
        public Color RiverColor { get; private set; } = new Color(1f, 0.82f, 0.42f, 1f);

        [field: SerializeField, Tooltip("Colour of an attack song's form (Sting, Tornado, Meteor, Colossus). A hot red. Red means attack and nothing else.")]
        public Color AttackColor { get; private set; } = new Color(1f, 0.22f, 0.1f, 1f);

        [field: SerializeField, Tooltip("Colour of a defense song's form (Dome, Gust).")]
        public Color DefenseColor { get; private set; } = new Color(0.25f, 0.6f, 1f, 1f);

        [field: SerializeField, Tooltip("Colour of a power song's form (Hive Call). A deep gold, more saturated than the river's yellow.")]
        public Color PowerColor { get; private set; } = new Color(1f, 0.7f, 0.08f, 1f);

        // A soft glow is drawn around every wasp in place of bloom, which the project cannot add. In a dense swarm many
        // glows overlap and add up, so each one is faint; they blend softly toward white instead of clipping.
        [field: Header("Glow")]
        [field: SerializeField, Min(0f), Tooltip("Metres across the soft glow drawn around each wasp. 0 hides the glow.")]
        public float GlowSize { get; private set; } = 0.5f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Brightness of one wasp's glow at its middle. Keep it low: in the river dozens of glows overlap.")]
        public float GlowStrength { get; private set; } = 0.18f;

        [field: SerializeField, Min(0f), Tooltip("Metres the swarm's light reaches over the ground and the trees, at full strength.")]
        public float LightRange { get; private set; } = 14f;

        [field: SerializeField, Min(0f), Tooltip("Brightness of the swarm's light at full strength.")]
        public float LightIntensity { get; private set; } = 5.5f;

        [field: SerializeField, Min(0f), Tooltip("Metres above the ground the swarm's light hangs. Lower lights a smaller, brighter pool.")]
        public float LightHeight { get; private set; } = 3f;

        [field: SerializeField, Min(0f), Tooltip("Metres the swarm's light sits toward the camera from the middle of the swarm, so the side of the swarm the camera sees is lit too.")]
        public float LightTowardCamera { get; private set; } = 1.5f;

        /// <summary>The colour the wasps take while a song of <paramref name="category"/> holds their form.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The category has no colour.</exception>
        public Color ColorOf(SongCategory category)
        {
            switch (category)
            {
                case SongCategory.Attack:
                    return AttackColor;
                case SongCategory.Defense:
                    return DefenseColor;
                case SongCategory.Power:
                    return PowerColor;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, "This song category has no colour in SwarmConfig.");
            }
        }
    }
}
