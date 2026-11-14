using System;
using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The wild nests on the map (plan sections H and U): which are open, and the core collecting them. The first
    /// <see cref="NestConfig.OpenAtStart"/> are open when the match starts, and each cleared wave opens the next ones, in
    /// the order the positions were given. Standing within <see cref="NestConfig.CollectRadius"/> of an open nest, on the
    /// ground, makes it the active nest, whose combo the HUD shows; the nearest wins when two rings overlap. Playing the
    /// combo's notes in order (<see cref="PlayNote"/>) collects it; a note that does not continue the combo starts it again,
    /// and leaving the ring empties it. Standing still collects nothing, and there is no timer. What a collected nest gives
    /// is the session's to hand out, on <see cref="NestCollected"/>.
    /// </summary>
    public sealed class NestField
    {
        /// <summary>The value of <see cref="ActiveNest"/> while the core stands in no open nest's ring.</summary>
        public const int NoNest = -1;

        private readonly NestConfig _config;
        private readonly Nest[] _nests;

        private int _openedCount;
        private int _openCount;
        private int _collectedCount;
        private int _active = NoNest;

        /// <summary>
        /// Places the nests at <paramref name="positions"/>, in the order they open, each with its combo from
        /// <paramref name="combos"/>, and opens the first ones. There must be exactly as many of each as the config opens in
        /// all (<see cref="NestConfig.NestCount"/>).
        /// </summary>
        public NestField(NestConfig config, IReadOnlyList<Vector3> positions, IReadOnlyList<int[]> combos)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            if (combos == null)
            {
                throw new ArgumentNullException(nameof(combos));
            }

            if (positions.Count != config.NestCount)
            {
                throw new ArgumentException(
                    $"NestConfig opens {config.NestCount} nests in all, but {positions.Count} nest positions were given.", nameof(positions));
            }

            if (combos.Count != positions.Count)
            {
                throw new ArgumentException($"{positions.Count} nests need as many combos, but {combos.Count} were given.", nameof(combos));
            }

            _nests = new Nest[positions.Count];
            for (int index = 0; index < positions.Count; index++)
            {
                _nests[index] = new Nest(positions[index], combos[index]);
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

        /// <summary>The open nest whose ring the core stands in, whose combo is shown and hears the notes, or <see cref="NoNest"/>.</summary>
        public int ActiveNest => _active;

        /// <summary>Raised with the nest's index when a cleared wave opens it. The nests open at the start raise nothing.</summary>
        public event Action<int> NestOpened;

        /// <summary>Raised with the nest's index when the core collects it, after the counts have changed.</summary>
        public event Action<int> NestCollected;

        /// <summary>Raised with the nest's index when the core steps into its ring and its combo is shown, empty.</summary>
        public event Action<int> ComboShown;

        /// <summary>
        /// Raised with the nest's index when its combo stops being shown without being completed: the core left the ring, a
        /// nearer ring took over, or the swarm can no longer collect.
        /// </summary>
        public event Action<int> ComboHidden;

        /// <summary>
        /// Raised with the active nest's index and what a note did to its combo, whenever it changed something: never for
        /// <see cref="NestComboStep.Ignored"/>. A completing note raises it before <see cref="NestCollected"/>.
        /// </summary>
        public event Action<int, NestComboStep> ComboNote;

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
        /// Finds the ring the core stands in: the nearest open nest within reach, or none while <paramref name="canCollect"/>
        /// is false, as while the swarm is collapsed or the giant stands. Stepping out of a ring, or into a nearer one, empties
        /// the old combo. It never allocates.
        /// </summary>
        public void Tick(Vector3 core, bool canCollect)
        {
            int nearest = NoNest;
            if (canCollect)
            {
                float best = _config.CollectRadius * _config.CollectRadius;
                for (int index = 0; index < _nests.Length; index++)
                {
                    if (!_nests[index].IsOpen)
                    {
                        continue;
                    }

                    float distance = GroundMath.DistanceSquared(_nests[index].Position, core);
                    if (distance <= best && (nearest == NoNest || distance < best))
                    {
                        nearest = index;
                        best = distance;
                    }
                }
            }

            if (nearest == _active)
            {
                return;
            }

            int left = _active;
            _active = nearest;
            if (left != NoNest)
            {
                _nests[left].Restart();
                ComboHidden?.Invoke(left);
            }

            if (nearest != NoNest)
            {
                _nests[nearest].Restart();
                ComboShown?.Invoke(nearest);
            }
        }

        /// <summary>
        /// The flute played <paramref name="hole"/>: the active nest's combo hears it, and its last note collects the nest.
        /// With no active nest it does nothing. It never allocates.
        /// </summary>
        public NestComboStep PlayNote(int hole)
        {
            if (_active == NoNest)
            {
                return NestComboStep.Ignored;
            }

            int index = _active;
            NestComboStep step = _nests[index].Play(hole);
            if (step == NestComboStep.Ignored)
            {
                return step;
            }

            if (step == NestComboStep.Completed)
            {
                _active = NoNest;
                _openCount--;
                _collectedCount++;
            }

            ComboNote?.Invoke(index, step);
            if (step == NestComboStep.Completed)
            {
                NestCollected?.Invoke(index);
            }

            return step;
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
