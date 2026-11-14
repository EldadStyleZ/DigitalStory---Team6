using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// Where a match's map puts things (plan sections S and T): the two hives, the beekeepers' four spawn points, the nest
    /// trees and the scenery. Each match draws its own map from a seed, random within these rules. The pair of hives may lie
    /// along any line, anywhere in a wide band, and each hive has two spawn points of its own, as far from it as the other
    /// hive's are from that one, so the map's shape never decides how the waves split (section T). Where the nests may stand
    /// is also ruled by <see cref="NestConfig"/>; these numbers only add to it.
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Layout Config", fileName = "LayoutConfig")]
    public sealed class LayoutConfig : ScriptableObject
    {
        [field: Header("Arena")]
        [field: SerializeField, Min(1f), Tooltip("Metres from the middle of the map to the arena's edge along either ground axis: the outer edge of the scenery and the farthest a spawn point may be. Keep it at most CoreMoveConfig.PlayAreaHalfSize.")]
        public float ArenaHalfSize { get; private set; } = 42f;

        [field: Header("Hives")]
        [field: SerializeField, Min(0f), Tooltip("Fewest metres from the middle of the map to a hive. The first hive's bearing is any; the second stands across the middle from it.")]
        public float HiveNearest { get; private set; } = 8f;

        [field: SerializeField, Min(0f), Tooltip("Most metres from the middle of the map to a hive.")]
        public float HiveFarthest { get; private set; } = 20f;

        [field: SerializeField, Range(0f, 90f), Tooltip("Most degrees the second hive's bearing from the middle turns off straight across from the first, so the two never stand on the same side.")]
        public float HiveAcrossSpreadDegrees { get; private set; } = 60f;

        [field: SerializeField, Min(0f), Tooltip("Fewest metres between the two hives, so defending both is always a trip.")]
        public float HiveLeastSeparation { get; private set; } = 18f;

        [field: Header("Spawn points")]
        [field: SerializeField, Min(0f), Tooltip("Fewest metres from a spawn point to its own hive: the walk each of its beekeepers makes. Spawn points 0 and 2 belong to the first hive, 1 and 3 to the second, so the waves, which take them in turn, alternate between the hives.")]
        public float SpawnFromHiveNearest { get; private set; } = 18f;

        [field: SerializeField, Min(0f), Tooltip("Most metres from a spawn point to its own hive.")]
        public float SpawnFromHiveFarthest { get; private set; } = 22f;

        [field: SerializeField, Range(0f, 90f), Tooltip("Fewest degrees a spawn point turns off straight out from the middle through its hive. A hive's two spawn points turn to either side, so they come from two directions.")]
        public float SpawnSideNarrowestDegrees { get; private set; } = 20f;

        [field: SerializeField, Range(0f, 120f), Tooltip("Most degrees a spawn point turns off straight out through its hive.")]
        public float SpawnSideWidestDegrees { get; private set; } = 70f;

        [field: SerializeField, Min(0f), Tooltip("Metres a spawn point stands farther from the other hive than from its own, so its beekeepers never walk past the other hive.")]
        public float SpawnOwnHiveLeadMetres { get; private set; } = 4f;

        [field: SerializeField, Min(0f), Tooltip("Fewest metres from a spawn point to the swarm's start, so no beekeeper enters on the swarm.")]
        public float SpawnStartClearance { get; private set; } = 20f;

        [field: SerializeField, Min(0f), Tooltip("Fewest metres between two spawn points.")]
        public float SpawnLeastSpacing { get; private set; } = 10f;

        [field: Header("Nest trees")]
        [field: SerializeField, Min(0f), Tooltip("Most metres a nest tree stands from the middle along either ground axis, so it stays inside the clear middle, short of the scenery.")]
        public float NestInsideHalfSize { get; private set; } = 34f;

        [field: SerializeField, Min(0f), Tooltip("Fewest metres between two nest trees, so no two collect circles touch.")]
        public float NestLeastSpacing { get; private set; } = 8f;

        [field: SerializeField, Min(0f), Tooltip("Fewest metres from a nest tree to a spawn point, so no beekeeper enters under a nest.")]
        public float NestSpawnClearance { get; private set; } = 8f;

        [field: SerializeField, Min(0f), Tooltip("Metres a nest tree keeps beyond NestConfig.HiveClearance from every hive, so rounding never breaks that rule.")]
        public float NestHiveMargin { get; private set; } = 0.5f;

        [field: SerializeField, Min(0f), Tooltip("Fewest metres from a nest tree to the swarm's start.")]
        public float NestStartClearance { get; private set; } = 8f;

        [field: SerializeField, Range(0f, 90f), Tooltip("Fewest degrees off ahead that a nest open at the start stands, one to each side. Ahead is square to the hives' line, on the side the opening camera looks (north), so neither tree stands in the way of a core walking ahead.")]
        public float FirstNestsNarrowestBearing { get; private set; } = 15f;

        [field: SerializeField, Range(0f, 90f), Tooltip("Most degrees off ahead that a nest open at the start stands, so both are in the opening view.")]
        public float FirstNestsWidestBearing { get; private set; } = 40f;

        [field: SerializeField, Range(0f, 30f), Tooltip("The nests that open after a wave stand one in each equal slice of the circle, turned with the hives' line; each turns at most this many degrees off its slice's middle.")]
        public float LaterNestJitterDegrees { get; private set; } = 10f;

        [field: Header("Scenery")]
        [field: SerializeField, Min(0f), Tooltip("The scenery stands between this many metres from the middle along either ground axis and ArenaHalfSize, around the edge, never in the clear middle.")]
        public float SceneryInnerHalfSize { get; private set; } = 22f;

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

        [SerializeField, Tooltip("The fallback map's spawn points, 20 m from their own hive: the west hive's to the south-west, the east hive's to the north-east, the west hive's to the north-west and the east hive's to the south-east.")]
        private Vector3[] _fallbackSpawns =
        {
            new Vector3(-29.82f, 0f, -10f), new Vector3(29.82f, 0f, 10f), new Vector3(-29.82f, 0f, 10f), new Vector3(22.5f, 0f, -17.32f),
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

        /// <summary>The fallback map's spawn points: the first hive's, the second's, the first's, the second's.</summary>
        public IReadOnlyList<Vector3> FallbackSpawns => _fallbackSpawns;

        /// <summary>The fallback map's nest trees, in the order they open.</summary>
        public IReadOnlyList<Vector3> FallbackNests => _fallbackNests;
    }
}
