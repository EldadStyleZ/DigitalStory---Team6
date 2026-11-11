using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// Finds a place for each cage: as near as possible to its preferred spot (the painters' spots), but fully on
    /// the floor with a margin, away from the centre, clear of the players' spawn points and of the other cages.
    /// </summary>
    public static class CagePlacement
    {
        /// <summary>What a cage's place must satisfy.</summary>
        public struct Rules
        {
            /// <summary>Whether a point is on the playable floor.</summary>
            public Func<Vector2, bool> IsOnFloor;

            /// <summary>Half the size of a cage's outer edge.</summary>
            public Vector2 CageHalfSize;

            /// <summary>How far every edge of a cage must stay from the floor's edge.</summary>
            public float BorderMargin;

            /// <summary>The space left between two cages.</summary>
            public float Gap;

            /// <summary>Where players appear; a cage must stay clear of them.</summary>
            public IReadOnlyList<Vector2> SpawnPoints;

            /// <summary>How far a cage's edge must stay from a spawn point.</summary>
            public float SpawnClearance;

            /// <summary>The middle of the map.</summary>
            public Vector2 Centre;

            /// <summary>How far a cage's centre must stay from the middle of the map.</summary>
            public float MinCentreDistance;

            /// <summary>How far from its preferred spot a cage may be moved.</summary>
            public float SearchRadius;

            /// <summary>The spacing of the spots tried while searching.</summary>
            public float SearchStep;
        }

        /// <summary>A place for each preferred spot, in order, each the nearest valid one to its spot.</summary>
        /// <exception cref="InvalidOperationException">No valid place is within the search radius.</exception>
        public static Vector2[] Place(IReadOnlyList<Vector2> preferred, Rules rules)
        {
            List<Vector2> offsets = SearchOffsets(rules.SearchRadius, rules.SearchStep);
            var placed = new List<Vector2>(preferred.Count);

            foreach (Vector2 spot in preferred)
            {
                bool found = false;

                foreach (Vector2 offset in offsets)
                {
                    Vector2 candidate = spot + offset;

                    if (IsValid(candidate, placed, rules))
                    {
                        placed.Add(candidate);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    throw new InvalidOperationException($"No room for a cage within {rules.SearchRadius} of {spot}.");
                }
            }

            return placed.ToArray();
        }

        /// <summary>Whether a cage centred here satisfies every rule, given the cages already placed.</summary>
        public static bool IsValid(Vector2 centre, IReadOnlyList<Vector2> placed, Rules rules)
        {
            Vector2 half = rules.CageHalfSize;
            Vector2 reach = half + (Vector2.one * rules.BorderMargin);

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if ((x != 0 || y != 0) && !rules.IsOnFloor(centre + new Vector2(x * reach.x, y * reach.y)))
                    {
                        return false;
                    }
                }
            }

            if ((centre - rules.Centre).magnitude < rules.MinCentreDistance)
            {
                return false;
            }

            if (rules.SpawnPoints != null)
            {
                Vector2 clear = half + (Vector2.one * rules.SpawnClearance);

                foreach (Vector2 spawn in rules.SpawnPoints)
                {
                    if (Mathf.Abs(spawn.x - centre.x) < clear.x && Mathf.Abs(spawn.y - centre.y) < clear.y)
                    {
                        return false;
                    }
                }
            }

            foreach (Vector2 other in placed)
            {
                if (Mathf.Abs(other.x - centre.x) < (half.x * 2f) + rules.Gap
                    && Mathf.Abs(other.y - centre.y) < (half.y * 2f) + rules.Gap)
                {
                    return false;
                }
            }

            return true;
        }

        // Every grid offset within the radius, nearest first, so the search keeps a cage close to its spot.
        private static List<Vector2> SearchOffsets(float radius, float step)
        {
            var offsets = new List<Vector2>();
            int steps = Mathf.FloorToInt(radius / step);

            for (int x = -steps; x <= steps; x++)
            {
                for (int y = -steps; y <= steps; y++)
                {
                    var offset = new Vector2(x * step, y * step);

                    if (offset.magnitude <= radius + 0.0001f)
                    {
                        offsets.Add(offset);
                    }
                }
            }

            offsets.Sort((a, b) =>
            {
                int byDistance = a.sqrMagnitude.CompareTo(b.sqrMagnitude);
                return byDistance != 0 ? byDistance : a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x);
            });
            return offsets;
        }
    }

    /// <summary>Works out which vermin are inside a cage.</summary>
    public static class CageCounting
    {
        /// <summary>The ids of the active candidates whose centre is inside the interior.</summary>
        public static ISet<int> Inside(IEnumerable<(int Id, Vector2 Centre, bool Active)> candidates, Rect interior)
        {
            var inside = new HashSet<int>();

            foreach ((int id, Vector2 centre, bool active) in candidates)
            {
                if (active && interior.Contains(centre))
                {
                    inside.Add(id);
                }
            }

            return inside;
        }
    }

    /// <summary>
    /// Each player's live score: a number of points for every vermin in their cage, so it goes up and down as
    /// vermin come and go, until it is locked at the end of a round.
    /// </summary>
    public sealed class Scoreboard
    {
        private readonly int[] _scores;
        private readonly int _pointsPerVermin;

        public Scoreboard(int players, int pointsPerVermin)
        {
            _scores = new int[players];
            _pointsPerVermin = pointsPerVermin;
        }

        /// <summary>Raised with the slot and its new score whenever a score changes.</summary>
        public event Action<int, int> Changed;

        /// <summary>Every slot's score, P1 first.</summary>
        public IReadOnlyList<int> Scores => _scores;

        /// <summary>Whether the scores are frozen.</summary>
        public bool IsLocked { get; private set; }

        public int ScoreOf(int slot) => _scores[slot];

        /// <summary>Sets how many vermin are in a slot's cage, unless the scores are locked.</summary>
        public void SetCount(int slot, int vermin)
        {
            if (IsLocked)
            {
                return;
            }

            Set(slot, vermin * _pointsPerVermin);
        }

        /// <summary>Freezes every score, such as when a round ends.</summary>
        public void Lock()
        {
            IsLocked = true;
        }

        /// <summary>Sets every score back to 0 and unlocks them, for a new match.</summary>
        public void Reset()
        {
            IsLocked = false;

            for (int slot = 0; slot < _scores.Length; slot++)
            {
                Set(slot, 0);
            }
        }

        private void Set(int slot, int score)
        {
            if (_scores[slot] == score)
            {
                return;
            }

            _scores[slot] = score;
            Changed?.Invoke(slot, score);
        }
    }
}
