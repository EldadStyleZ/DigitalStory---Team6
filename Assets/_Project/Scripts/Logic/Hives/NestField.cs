using System;
using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The wild nests on the map (plan section H): which are open, and the core collecting them. The first
    /// <see cref="NestConfig.OpenAtStart"/> are open when the match starts, and each cleared wave opens the next ones, in
    /// the order the positions were given. The core collects an open nest by staying within
    /// <see cref="NestConfig.CollectRadius"/> of it, on the ground, for <see cref="NestConfig.CollectSeconds"/> without a
    /// break; leaving the circle or a hit on the core starts the stay again. Each open nest keeps its own clock, so a core in
    /// reach of two collects both. What a collected nest gives is the session's to hand out, on <see cref="NestCollected"/>.
    /// </summary>
    public sealed class NestField
    {
        private readonly NestConfig _config;
        private readonly Nest[] _nests;

        private int _openedCount;
        private int _openCount;
        private int _collectedCount;

        /// <summary>
        /// Places the nests at <paramref name="positions"/>, in the order they open, and opens the first ones. There must be
        /// exactly as many as the config opens in all (<see cref="NestConfig.NestCount"/>).
        /// </summary>
        public NestField(NestConfig config, IReadOnlyList<Vector3> positions)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            if (positions.Count != config.NestCount)
            {
                throw new ArgumentException(
                    $"NestConfig opens {config.NestCount} nests in all, but {positions.Count} nest positions were given.", nameof(positions));
            }

            _nests = new Nest[positions.Count];
            for (int index = 0; index < positions.Count; index++)
            {
                _nests[index] = new Nest(positions[index], config.CollectSeconds);
            }

            OpenUpTo(config.OpenAtStart, false);
        }

        /// <summary>How many nests the map holds.</summary>
        public int Count => _nests.Length;

        /// <summary>The nest at <paramref name="index"/>, in the order they open.</summary>
        public Nest this[int index] => _nests[index];

        /// <summary>How many nests the core has collected. The swarm's level is one more than this.</summary>
        public int CollectedCount => _collectedCount;

        /// <summary>How many nests can be collected now: opened and not yet collected.</summary>
        public int OpenCount => _openCount;

        /// <summary>Raised with the nest's index when a cleared wave opens it. The nests open at the start raise nothing.</summary>
        public event Action<int> NestOpened;

        /// <summary>Raised with the nest's index when the core collects it, after the counts have changed.</summary>
        public event Action<int> NestCollected;

        /// <summary>
        /// Wave number <paramref name="wave"/> (from 1) was cleared: the nests it opens open now, and any an earlier wave
        /// should have opened. Calling it again for the same wave, or after the last wave that opens any, opens nothing more.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The wave is below 1.</exception>
        public void OpenAfterWave(int wave)
        {
            if (wave < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(wave), wave, "Waves are numbered from 1.");
            }

            IReadOnlyList<int> afterWave = _config.OpenAfterWave;
            int open = _config.OpenAtStart;
            for (int index = 0; index < wave && index < afterWave.Count; index++)
            {
                open += afterWave[index];
            }

            OpenUpTo(open, true);
        }

        /// <summary>
        /// Counts the core's stay beside every open nest for <paramref name="deltaTime"/>, and collects each whose stay is
        /// complete. A nest out of reach starts again; so does every nest while <paramref name="canCollect"/> is false, as
        /// while the swarm is collapsed and nothing is there to take the nest. It never allocates.
        /// </summary>
        public void Tick(float deltaTime, Vector3 core, bool canCollect)
        {
            float reachSquared = _config.CollectRadius * _config.CollectRadius;
            for (int index = 0; index < _nests.Length; index++)
            {
                Nest nest = _nests[index];
                if (!nest.IsOpen)
                {
                    continue;
                }

                if (!canCollect || GroundMath.DistanceSquared(nest.Position, core) > reachSquared)
                {
                    nest.Restart();
                    continue;
                }

                if (deltaTime > 0f && nest.Stay(deltaTime))
                {
                    _openCount--;
                    _collectedCount++;
                    NestCollected?.Invoke(index);
                }
            }
        }

        /// <summary>The core was hit: every stay starts again.</summary>
        public void NotifyCoreHit()
        {
            for (int index = 0; index < _nests.Length; index++)
            {
                _nests[index].Restart();
            }
        }

        // The nests open in their given order, so the first count of them are the ones that may be open. The constructor's
        // nests raise nothing: no one can listen yet.
        private void OpenUpTo(int count, bool raise)
        {
            int target = Math.Min(count, _nests.Length);
            while (_openedCount < target)
            {
                int index = _openedCount;
                _nests[index].Open();
                _openedCount++;
                _openCount++;
                if (raise)
                {
                    NestOpened?.Invoke(index);
                }
            }
        }
    }
}
