using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// Where a match's map puts things (plan section S): the two hives, the beekeepers' four spawn points, the nest trees and
    /// the scenery around the edge. Each match draws its own map from a seed, random within these rules, so every map is
    /// fair. The bands come from the fixed map the scene had before (hives 12.5 m either side of the middle, spawn points
    /// 32 m out, scenery between 25.5 and 34 m), so distances stay in that range. Where the nests may stand is also ruled by
    /// <see cref="NestConfig"/>; these numbers only add to it.
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Layout Config", fileName = "LayoutConfig")]
    public sealed class LayoutConfig : ScriptableObject
    {
        [field: Header("Arena")]
        [field: SerializeField, Min(1f), Tooltip("Metres from the middle of the map to the arena's edge along either ground axis: the outer edge of the scenery and the farthest a spawn point may be. Keep it at most CoreMoveConfig.PlayAreaHalfSize.")]
        public float ArenaHalfSize { get; private set; } = 34f;

        // The hive and spawn bands stay close to the old fixed map (hives 12.5 m out and 25 m apart, walks of 19.5 m and more)
        // because the economy was tuned on it: with hives 11 to 14 m out, 22 m apart and turned up to 20 degrees, the Mixed
        // plan without nests, which lost every match in round 5 on the fixed map, won one in five with 10% of its wasps on
        // healing. The nests and the scenery take the variety.
        [field: Header("Hives")]
        [field: SerializeField, Min(0f), Tooltip("Fewest metres from the middle of the map to a hive. The first hive stands west of the middle, the second east.")]
        public float HiveNearest { get; private set; } = 12f;

        [field: SerializeField, Min(0f), Tooltip("Most metres from the middle of the map to a hive.")]
        public float HiveFarthest { get; private set; } = 13.5f;

        [field: SerializeField, Range(0f, 60f), Tooltip("Most degrees a hive's bearing from the middle turns off due west (the first) or due east (the second), so both stay in the opening view.")]
        public float HiveTiltDegrees { get; private set; } = 15f;

        [field: SerializeField, Min(0f), Tooltip("Fewest metres between the two hives, so defending both is always a trip.")]
        public float HiveLeastSeparation { get; private set; } = 24f;

        [field: Header("Spawn points")]
        [field: SerializeField, Min(0f), Tooltip("Fewest metres from the middle of the map to a spawn point. The four turn with the line between the hives: one ahead, one behind, one beyond each hive.")]
        public float SpawnNearest { get; private set; } = 31f;

        [field: SerializeField, Min(0f), Tooltip("Most metres from the middle of the map to a spawn point.")]
        public float SpawnFarthest { get; private set; } = 33f;

        [field: SerializeField, Range(0f, 45f), Tooltip("Most degrees a spawn point's bearing turns off its side of the hives' line. The ones ahead and behind turn only toward the west hive.")]
        public float SpawnJitterDegrees { get; private set; } = 8f;

        // On the old fixed map the spawn points ahead and behind were as far from both hives, and the tie sent their
        // beekeepers to the west hive, so it took three of every four and the east hive, which starts hurt, one. The economy
        // was tuned on that split; without it the share of wasps spent healing swung from 0% to 30% between maps.
        [field: SerializeField, Min(0f), Tooltip("Metres the spawn points ahead and behind stand nearer the west hive than the east one, so their beekeepers walk to the west hive as on the old fixed map.")]
        public float SpawnWestLeanMetres { get; private set; } = 1f;

        [field: SerializeField, Min(0f), Tooltip("Fewest metres from a spawn point to any hive, so no beekeeper's walk to a hive is much shorter than on the old fixed map (19.5 m).")]
        public float SpawnHiveLeastDistance { get; private set; } = 19f;

        [field: Header("Nest trees")]
        [field: SerializeField, Min(0f), Tooltip("Most metres a nest tree stands from the middle along either ground axis, so it stays inside the clear middle, short of the scenery.")]
        public float NestInsideHalfSize { get; private set; } = 24f;

        [field: SerializeField, Min(0f), Tooltip("Fewest metres between two nest trees, so no two collect circles touch.")]
        public float NestLeastSpacing { get; private set; } = 8f;

        [field: SerializeField, Min(0f), Tooltip("Fewest metres from a nest tree to a spawn point, so no beekeeper enters under a nest.")]
        public float NestSpawnClearance { get; private set; } = 8f;

        [field: SerializeField, Min(0f), Tooltip("Metres a nest tree keeps beyond NestConfig.HiveClearance from every hive, so rounding never breaks that rule.")]
        public float NestHiveMargin { get; private set; } = 0.5f;

        [field: SerializeField, Min(0f), Tooltip("Fewest metres from a nest tree to the swarm's start.")]
        public float NestStartClearance { get; private set; } = 8f;

        [field: SerializeField, Range(0f, 90f), Tooltip("Fewest degrees off ahead (the line from the middle to the spawn point ahead) that a nest open at the start stands, one to each side, so neither tree stands in the way of a core walking ahead.")]
        public float FirstNestsNarrowestBearing { get; private set; } = 15f;

        [field: SerializeField, Range(0f, 90f), Tooltip("Most degrees off ahead that a nest open at the start stands, so both are in the opening view.")]
        public float FirstNestsWidestBearing { get; private set; } = 40f;

        [field: SerializeField, Range(0f, 30f), Tooltip("The nests that open after a wave stand one in each equal slice of the circle, turned with the hives' line; each turns at most this many degrees off its slice's middle.")]
        public float LaterNestJitterDegrees { get; private set; } = 10f;

        [field: Header("Scenery")]
        [field: SerializeField, Min(0f), Tooltip("The scenery stands between this many metres from the middle along either ground axis and ArenaHalfSize, around the edge, never in the clear middle.")]
        public float SceneryInnerHalfSize { get; private set; } = 25.5f;

        [field: SerializeField, Min(0f), Tooltip("Fewest metres between two props' pivots.")]
        public float SceneryLeastSpacing { get; private set; } = 2f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Two props' pivots are also at least this share of their two footprints' radii apart, so a big ruin and a pine do not stand in each other.")]
        public float SceneryFootprintShare { get; private set; } = 0.5f;

        [field: SerializeField, Min(0f), Tooltip("Fewest metres from a hive to the edge of a prop's footprint, so nothing stands in a hive's rings.")]
        public float SceneryHiveClearance { get; private set; } = 6f;

        [field: SerializeField, Min(0f), Tooltip("Fewest metres from a nest tree to the edge of a prop's footprint.")]
        public float SceneryNestClearance { get; private set; } = 3f;

        [field: SerializeField, Min(0f), Tooltip("Fewest metres from a spawn point to the edge of a prop's footprint.")]
        public float ScenerySpawnClearance { get; private set; } = 4f;

        [field: SerializeField, Min(0f), Tooltip("Fewest metres from the swarm's start to the edge of a prop's footprint.")]
        public float SceneryStartClearance { get; private set; } = 10f;

        [field: SerializeField, Min(0f), Tooltip("Half the width of a lane, the straight walk from a spawn point to a hive. No prop's footprint reaches into a lane.")]
        public float LaneHalfWidth { get; private set; } = 2f;

        [field: Header("Search")]
        [field: SerializeField, Min(1), Tooltip("Whole maps tried before the fallback map is used. Each try draws hives, spawn points and nests afresh.")]
        public int LayoutAttempts { get; private set; } = 30;

        [field: SerializeField, Min(1), Tooltip("Tries for each hive pair, set of spawn points or nest within one map before that map is given up.")]
        public int PlaceAttempts { get; private set; } = 200;

        [field: SerializeField, Min(1), Tooltip("Tries for each prop. A prop that finds no place is left out of that match.")]
        public int SceneryAttempts { get; private set; } = 40;

        [Header("Fallback map")]
        [SerializeField, Tooltip("The hives of the map used when no map keeps the rules: the old fixed map's, west then east.")]
        private Vector3[] _fallbackHives = { new Vector3(-12.5f, 0f, 0f), new Vector3(12.5f, 0f, 0f) };

        [SerializeField, Tooltip("The fallback map's spawn points: ahead, behind, beyond the east hive, beyond the west hive. The old fixed map's, with the ones ahead and behind 2 m toward the west hive.")]
        private Vector3[] _fallbackSpawns =
        {
            new Vector3(-2f, 0f, 31.94f), new Vector3(-2f, 0f, -31.94f), new Vector3(32f, 0f, 0f), new Vector3(-32f, 0f, 0f),
        };

        [SerializeField, Tooltip("The fallback map's nest trees, in the order they open: the old fixed map's.")]
        private Vector3[] _fallbackNests =
        {
            new Vector3(-4.2303505f, 0f, 9.91818f),
            new Vector3(6.0840244f, 0f, 12.559615f),
            new Vector3(-1.1644772f, 0f, -17.937988f),
            new Vector3(-0.48996973f, 0f, 18.718933f),
            new Vector3(16.543379f, 0f, 13.702695f),
            new Vector3(-18.956007f, 0f, -12.818432f),
            new Vector3(-19.761074f, 0f, 11.611771f),
            new Vector3(23.381496f, 0f, -8.659761f),
        };

        /// <summary>The fallback map's hives, west then east.</summary>
        public IReadOnlyList<Vector3> FallbackHives => _fallbackHives;

        /// <summary>The fallback map's spawn points: ahead, behind, beyond the east hive, beyond the west hive.</summary>
        public IReadOnlyList<Vector3> FallbackSpawns => _fallbackSpawns;

        /// <summary>The fallback map's nest trees, in the order they open.</summary>
        public IReadOnlyList<Vector3> FallbackNests => _fallbackNests;
    }
}
