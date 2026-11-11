using System;
using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Picks what an aimed song strikes, so the player never has to aim and no song fires at a surprise.</summary>
    /// <remarks>
    /// Call <see cref="Tick"/> once per frame, then <see cref="Select"/> with the rule of the song about to cast
    /// (or a default rule to draw the marker). Read the result from <see cref="Target"/>, <see cref="HiveIndex"/>
    /// and <see cref="AimPoint"/>. All distances ignore height. Nothing is allocated after construction.
    /// </remarks>
    public sealed class AutoAim
    {
        private const float Epsilon = 1e-6f;

        private readonly AimConfig _config;
        private readonly IReadOnlyList<ISwarmTarget> _targets;
        private readonly HiveSet _hives;
        private readonly float[] _hiveHealth;
        private readonly float[] _hiveSinceHit;
        private readonly int[] _nearest;
        private float _sinceChosen;

        /// <summary>Starts with no target. The target list may change in size and content between calls.</summary>
        public AutoAim(AimConfig config, IReadOnlyList<ISwarmTarget> targets, HiveSet hives)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _targets = targets ?? throw new ArgumentNullException(nameof(targets));
            _hives = hives ?? throw new ArgumentNullException(nameof(hives));

            _hiveHealth = new float[hives.Count];
            _hiveSinceHit = new float[hives.Count];
            for (int i = 0; i < _hiveHealth.Length; i++)
            {
                _hiveHealth[i] = hives[i].Health;
                _hiveSinceHit[i] = float.MaxValue;
            }

            _nearest = new int[Mathf.Max(1, config.NearestCount)];
            TargetIndex = -1;
            HiveIndex = -1;
        }

        /// <summary>The chosen beekeeper, or null when the target is a hive, a point or nothing.</summary>
        public ISwarmTarget Target => TargetIndex >= 0 && TargetIndex < _targets.Count ? _targets[TargetIndex] : null;

        /// <summary>Index of the chosen beekeeper in the target list, or -1.</summary>
        public int TargetIndex { get; private set; }

        /// <summary>Index of the chosen hive, or -1. Only the <see cref="AimRule.Hive"/> rule sets it.</summary>
        public int HiveIndex { get; private set; }

        /// <summary>True when a beekeeper or a hive is chosen. False when the song strikes the empty point ahead.</summary>
        public bool HasTarget => TargetIndex >= 0 || HiveIndex >= 0;

        /// <summary>Where the song strikes: the target, the mouse's ground point, or the point ahead of the Piper.</summary>
        public Vector3 AimPoint { get; private set; }

        /// <summary>How many beekeepers <see cref="GetNearest"/> holds. Only <see cref="AimRule.ThreeNearest"/> fills it.</summary>
        public int NearestCount { get; private set; }

        /// <summary>The index in the target list of the nearest beekeeper number <paramref name="slot"/>, nearest first.</summary>
        public int GetNearest(int slot)
        {
            if (slot < 0 || slot >= NearestCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slot));
            }

            return _nearest[slot];
        }

        /// <summary>Ages the hold timer and notes which hives were hit.</summary>
        public void Tick(float deltaTime)
        {
            _sinceChosen += deltaTime;
            for (int i = 0; i < _hiveHealth.Length; i++)
            {
                float health = _hives[i].Health;
                if (health < _hiveHealth[i])
                {
                    _hiveSinceHit[i] = 0f;
                }
                else if (_hiveSinceHit[i] < float.MaxValue)
                {
                    _hiveSinceHit[i] += deltaTime;
                }

                _hiveHealth[i] = health;
            }
        }

        /// <summary>The range, in metres, for a rule that has several songs. Pass the song's own range to <see cref="Select"/>.</summary>
        public float RangeFor(AimRule rule)
        {
            switch (rule)
            {
                case AimRule.Threat: return _config.StingRange;
                case AimRule.ThreeNearest: return _config.HydraRange;
                case AimRule.Line: return _config.LanceRange;
                case AimRule.Crowd: return _config.MeteorRange;
                default: return float.MaxValue;
            }
        }

        /// <summary>Chooses the target for a rule. Returns true when a beekeeper or hive was chosen.</summary>
        /// <param name="rule">How to choose.</param>
        /// <param name="range">Metres from the Piper within which a beekeeper may be chosen. Ignored for Hive.</param>
        /// <param name="piperPosition">Where the Piper stands.</param>
        /// <param name="piperForward">Where the Piper faces. Height is ignored.</param>
        /// <param name="mouseLive">True while the mouse wins over auto-aim.</param>
        /// <param name="mousePoint">The mouse's point on the ground. Only read while the mouse is live.</param>
        public bool Select(AimRule rule, float range, Vector3 piperPosition, Vector3 piperForward, bool mouseLive, Vector3 mousePoint)
        {
            NearestCount = 0;
            HiveIndex = -1;

            if (rule == AimRule.ThreeNearest)
            {
                FillNearest(mouseLive ? mousePoint : piperPosition, piperPosition, range);
            }

            if (mouseLive)
            {
                SetTarget(NearestTo(mousePoint, _config.MouseRadius));
                AimPoint = mousePoint;
                return TargetIndex >= 0;
            }

            if (rule == AimRule.Hive)
            {
                SetTarget(-1);
                HiveIndex = PickHive(piperPosition);
                if (HiveIndex >= 0)
                {
                    AimPoint = _hives[HiveIndex].Position;
                    return true;
                }

                AimPoint = Ahead(piperPosition, piperForward);
                return false;
            }

            if (!IsHeld(piperPosition, range))
            {
                SetTarget(PickBeekeeper(rule, piperPosition, range));
            }

            if (TargetIndex < 0)
            {
                AimPoint = Ahead(piperPosition, piperForward);
                return false;
            }

            AimPoint = _targets[TargetIndex].Position;
            return true;
        }

        private bool IsHeld(Vector3 piper, float range)
        {
            return TargetIndex >= 0
                && TargetIndex < _targets.Count
                && _sinceChosen < _config.HoldSeconds
                && _targets[TargetIndex].IsAlive
                && DistanceSquared(_targets[TargetIndex].Position, piper) <= range * range;
        }

        private void SetTarget(int index)
        {
            if (index != TargetIndex)
            {
                _sinceChosen = 0f;
            }

            TargetIndex = index;
        }

        private int PickBeekeeper(AimRule rule, Vector3 piper, float range)
        {
            float rangeSquared = range * range;
            int best = -1;
            int bestCount = -1;
            float bestScore = float.MaxValue;

            for (int i = 0; i < _targets.Count; i++)
            {
                ISwarmTarget candidate = _targets[i];
                if (!candidate.IsAlive || DistanceSquared(candidate.Position, piper) > rangeSquared)
                {
                    continue;
                }

                float score = Threat(candidate.Position, piper);
                int count = rule == AimRule.Line ? LineCount(candidate.Position, piper, rangeSquared)
                    : rule == AimRule.Crowd ? CrowdCount(i)
                    : 0;

                if (count > bestCount || (count == bestCount && score < bestScore))
                {
                    best = i;
                    bestCount = count;
                    bestScore = score;
                }
            }

            return best;
        }

        // Distance to the Piper, less a bonus while the beekeeper stands near a standing hive. Lower is more urgent.
        private float Threat(Vector3 position, Vector3 piper)
        {
            float score = Mathf.Sqrt(DistanceSquared(position, piper));
            float radiusSquared = _config.HiveThreatRadius * _config.HiveThreatRadius;
            for (int h = 0; h < _hives.Count; h++)
            {
                if (!_hives[h].IsDestroyed && DistanceSquared(position, _hives[h].Position) <= radiusSquared)
                {
                    return score - _config.HiveThreatBonus;
                }
            }

            return score;
        }

        private int CrowdCount(int index)
        {
            float radiusSquared = _config.CrowdRadius * _config.CrowdRadius;
            Vector3 centre = _targets[index].Position;
            int count = 0;
            for (int j = 0; j < _targets.Count; j++)
            {
                if (j != index && _targets[j].IsAlive && DistanceSquared(_targets[j].Position, centre) <= radiusSquared)
                {
                    count++;
                }
            }

            return count;
        }

        // Beekeepers in range, ahead of the Piper, inside the strip along the line from the Piper through the candidate.
        private int LineCount(Vector3 through, Vector3 piper, float rangeSquared)
        {
            float dx = through.x - piper.x;
            float dz = through.z - piper.z;
            float lengthSquared = dx * dx + dz * dz;
            if (lengthSquared < Epsilon)
            {
                return 1;
            }

            float length = Mathf.Sqrt(lengthSquared);
            float ux = dx / length;
            float uz = dz / length;
            float half = _config.LineWidth * 0.5f;
            int count = 0;
            for (int j = 0; j < _targets.Count; j++)
            {
                if (!_targets[j].IsAlive)
                {
                    continue;
                }

                Vector3 p = _targets[j].Position;
                float rx = p.x - piper.x;
                float rz = p.z - piper.z;
                if (rx * rx + rz * rz > rangeSquared)
                {
                    continue;
                }

                float along = rx * ux + rz * uz;
                float across = Mathf.Abs(rx * uz - rz * ux);
                if (along >= 0f && across <= half)
                {
                    count++;
                }
            }

            return count;
        }

        private int PickHive(Vector3 piper)
        {
            int recent = -1;
            float recentAge = _config.HiveRecentSeconds;
            for (int i = 0; i < _hives.Count; i++)
            {
                if (!_hives[i].IsDestroyed && _hiveSinceHit[i] <= recentAge && (recent < 0 || _hiveSinceHit[i] < _hiveSinceHit[recent]))
                {
                    recent = i;
                }
            }

            if (recent >= 0)
            {
                return recent;
            }

            return _hives.TryGetNearestStanding(piper, out int nearest) ? nearest : -1;
        }

        private int NearestTo(Vector3 point, float radius)
        {
            int best = -1;
            float bestSquared = radius * radius;
            for (int i = 0; i < _targets.Count; i++)
            {
                if (!_targets[i].IsAlive)
                {
                    continue;
                }

                float squared = DistanceSquared(_targets[i].Position, point);
                if (squared <= bestSquared && (best < 0 || squared < DistanceSquared(_targets[best].Position, point)))
                {
                    best = i;
                    bestSquared = squared;
                }
            }

            return best;
        }

        // Fills the nearest beekeepers to the reference point that are within range of the Piper. Insertion keeps it sorted.
        private void FillNearest(Vector3 reference, Vector3 piper, float range)
        {
            float rangeSquared = range * range;
            int capacity = _nearest.Length;
            for (int i = 0; i < _targets.Count; i++)
            {
                if (!_targets[i].IsAlive || DistanceSquared(_targets[i].Position, piper) > rangeSquared)
                {
                    continue;
                }

                float squared = DistanceSquared(_targets[i].Position, reference);
                int at = NearestCount;
                while (at > 0 && squared < DistanceSquared(_targets[_nearest[at - 1]].Position, reference))
                {
                    at--;
                }

                if (at >= capacity)
                {
                    continue;
                }

                int last = Mathf.Min(NearestCount, capacity - 1);
                for (int k = last; k > at; k--)
                {
                    _nearest[k] = _nearest[k - 1];
                }

                _nearest[at] = i;
                NearestCount = Mathf.Min(NearestCount + 1, capacity);
            }
        }

        private Vector3 Ahead(Vector3 piper, Vector3 forward)
        {
            forward.y = 0f;
            forward = forward.sqrMagnitude < Epsilon ? Vector3.forward : forward.normalized;
            Vector3 point = piper + (forward * _config.EmptyAimDistance);
            point.y = piper.y;
            return point;
        }

        private static float DistanceSquared(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
