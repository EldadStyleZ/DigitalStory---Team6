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
        [field: SerializeField, Min(0f), Tooltip("Spring strength pulling a wasp toward its place in the river and in the figure-eight. Higher follows more tightly.")]
        public float RiverSpring { get; private set; } = 100f;

        [field: SerializeField, Min(0f), Tooltip("Damping on the river spring. 1 brings a wasp to its place as fast as it can without flying past it; lower overshoots and bounces.")]
        public float RiverDamping { get; private set; } = 1f;

        [field: SerializeField, Min(0f), Tooltip("Spring strength pulling a wasp toward its place in a form.")]
        public float ShapeSpring { get; private set; } = 180f;

        [field: SerializeField, Min(0f), Tooltip("Damping on the form spring.")]
        public float ShapeDamping { get; private set; } = 0.75f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Damping while a shape that has just formed wobbles: a song's squad, Hive Call's halo, the Colossus. The river and the eight never wobble.")]
        public float WobbleDamping { get; private set; } = 0.5f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the wobble of a shape that has just formed lasts.")]
        public float WobbleSeconds { get; private set; } = 0.35f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the change of form takes to sweep through the swarm.")]
        public float FormWaveSeconds { get; private set; } = 0.6f;

        [field: SerializeField, Min(0f), Tooltip("Lanes per second the wasps flow along the river.")]
        public float FlowSpeed { get; private set; } = 0.08f;

        [field: SerializeField, Min(0f), Tooltip("Length of the river behind the core, before its tail's delay (FollowDelaySeconds) stretches it: walking, the river is this plus the metres the core covers in that delay.")]
        public float RiverLengthMeters { get; private set; } = 8f;

        [field: SerializeField, Min(0f), Tooltip("Half the width of the river. Wide enough that the big wasps keep apart instead of merging into one band. The river is less than half as tall as it is wide.")]
        public float RiverRadius { get; private set; } = 1.2f;

        [field: SerializeField, Min(0f), Tooltip("Sideways sway of the river, in metres.")]
        public float MeanderAmplitude { get; private set; } = 0.25f;

        [field: SerializeField, Min(0f), Tooltip("Metres of path the river needs before it stretches out behind the core. On a shorter path, at the start of a run, the swarm flies the figure-eight round the core.")]
        public float StandingTrailMeters { get; private set; } = 2f;

        [field: SerializeField, Min(0f), Tooltip("The core counts as standing while it stays within this many metres (on the ground) of where it stopped, so settling never counts as a step.")]
        public float StandStillMeters { get; private set; } = 0.05f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the core stands before the river winds into the figure-eight round it.")]
        public float StandDelaySeconds { get; private set; } = 0.5f;

        [field: SerializeField, Min(0f), Tooltip("Seconds one wasp takes to wind into the figure-eight, and to unwind out of it once the core moves; each starts its own delay (FollowDelaySeconds) after the river's head. 0 switches at once.")]
        public float StandBlendSeconds { get; private set; } = 1f;

        // The core is an invisible point, and nothing marks it. While it stands, the free swarm flows round a figure-eight in
        // the air whose crossing sits over it, so the eye finds the core where the loop crosses itself. The sizes grow with
        // the swarm's level, as the songs' do.
        [field: Header("The figure-eight")]
        [field: SerializeField, Min(0f), Tooltip("Metres on the ground from the crossing, over the core, to the tip of each lobe, at level 1.")]
        public float EightRadius { get; private set; } = 2.6f;

        [field: SerializeField, Min(0f), Tooltip("Metres above the core the eight crosses itself, at level 1. Keep it above EightRadius times the sine of EightTilt, so the low lobe stays off the ground.")]
        public float EightHeight { get; private set; } = 1.4f;

        [field: SerializeField, Range(0f, 1.2f), Tooltip("Radians the eight's plane tilts, so one lobe rises as the other dips. 0 lays it flat.")]
        public float EightTilt { get; private set; } = 0.5f;

        [field: SerializeField, Min(0f), Tooltip("Metres the ribbon of wasps spreads to each side of the eight's line, at level 1. Keep it well under half of EightRadius, so each lobe keeps its hole.")]
        public float EightThickness { get; private set; } = 0.45f;

        [field: SerializeField, Min(0.5f), Tooltip("Seconds a wasp takes to fly once round the whole eight.")]
        public float EightLapSeconds { get; private set; } = 5f;

        // Plan section Q. Each wasp of the free swarm follows the core's recent path with its own delay, so a start, a stop
        // or a turn reaches the head of the swarm at once and travels down to its tail as a wave, and a change of shape
        // sweeps through the swarm instead of moving every wasp at once.
        [field: Header("Motion")]
        [field: SerializeField, Range(0f, 1.8f), Tooltip("Seconds the river's tail follows the core's path behind its head, which follows at once. On a stop the tail keeps flowing in for this long before it curls into the eight; on a start the eight unwinds head first.")]
        public float FollowDelaySeconds { get; private set; } = 0.8f;

        [field: SerializeField, Range(1f, 3f), Tooltip("How the delay grows from the river's head to its tail: 1 evenly; higher keeps more of the head quick and stretches the tail thinner.")]
        public float FollowDelayPower { get; private set; } = 1.5f;

        [field: SerializeField, Range(0f, 0.1f), Tooltip("Share of each wasp's place in a wave taken from its own random number, so the front of a wave is ragged like a flock's instead of a ruled line.")]
        public float WaveJitter { get; private set; } = 0.08f;

        [field: SerializeField, Range(0.1f, 1f), Tooltip("Width of the river's tail as a share of its head's, so the ribbon thins out behind the core.")]
        public float TailWidthShare { get; private set; } = 0.55f;

        [field: SerializeField, Range(0f, 0.3f), Tooltip("Seconds one wasp takes to ease from the old whole-swarm form into the new (Hive Call, the Colossus, the river) once the change's wave reaches it. 0 hands it over at once, as a jump of its goal.")]
        public float FormBlendSeconds { get; private set; } = 0.3f;

        [field: SerializeField, Range(0.001f, 0.0167f), Tooltip("Longest step the wasps' springs take. Each frame is cut into equal steps no longer than this, so the swarm moves the same at 60 and 120 frames a second. 1/240 divides both into whole steps.")]
        public float SpringStepSeconds { get; private set; } = 1f / 240f;

        // Plan section X. The free swarm (the river, the eight and wasps between forms) parts round each tall prop, nest tree
        // and hive below its top, hugs it and closes again behind it, instead of flying through it.
        [field: Header("Flow around obstacles")]
        [field: SerializeField, Range(0f, 1.5f), Tooltip("An obstacle's radius as a share of the mean of its box's half-width and half-depth. A tree's box holds its whole crown, so a little under 1 keeps the stream close to the trunk.")]
        public float ObstacleRadiusShare { get; private set; } = 0.8f;

        [field: SerializeField, Min(0f), Tooltip("Metres outside an obstacle's radius the wasps' goals keep as they go round it. It also covers how far the springs cut the corner.")]
        public float ObstacleMargin { get; private set; } = 0.45f;

        [field: SerializeField, Min(0f), Tooltip("Metres before an obstacle's edge, along the way a wasp flies, where the stream starts to part.")]
        public float ObstacleLeadMeters { get; private set; } = 2.5f;

        [field: SerializeField, Min(0f), Tooltip("Metres past an obstacle's edge, along the way a wasp flies, by which the stream has closed again.")]
        public float ObstacleCloseMeters { get; private set; } = 2f;

        [field: SerializeField, Range(0f, 1f), Tooltip("How much of the bend round obstacles the wasps fly. 1 keeps every goal out of the obstacle; 0 flies straight through.")]
        public float ObstacleStrength { get; private set; } = 1f;

        [field: SerializeField, Range(0f, 0.5f), Tooltip("How much faster a wasp flies on its way round an obstacle, as a share of its speed, easing in before and out after it.")]
        public float ObstacleSpeedUp { get; private set; } = 0.25f;

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
        [field: SerializeField, Tooltip("Colour of the plain swarm: the river, the figure-eight round the core and the collapsed swarm. A warm yellow that glows yellow-white.")]
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

        [field: SerializeField, Range(0f, 1f), Tooltip("How strong the swarm's light is for the smallest swarm, as a share of its full strength. It rises to full as the swarm grows to MaxWasps.")]
        public float LightLeastStrength { get; private set; } = 0.3f;

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
