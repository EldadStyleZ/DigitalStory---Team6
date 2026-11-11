using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>All the hives of a level: counts the standing ones and finds the nearest.</summary>
    public sealed class HiveSet
    {
        private readonly Hive[] _hives;

        public HiveSet(IReadOnlyList<Hive> hives)
        {
            if (hives == null)
            {
                throw new ArgumentNullException(nameof(hives));
            }

            _hives = new Hive[hives.Count];
            for (int i = 0; i < _hives.Length; i++)
            {
                Hive hive = hives[i] ?? throw new ArgumentException("A hive in the list is null.", nameof(hives));
                _hives[i] = hive;
                hive.Destroyed += OnHiveDestroyed;
            }
        }

        /// <summary>How many hives the level started with.</summary>
        public int Count => _hives.Length;

        /// <summary>The hive at an index.</summary>
        public Hive this[int index] => _hives[index];

        /// <summary>How many hives are not destroyed.</summary>
        public int StandingCount
        {
            get
            {
                int standing = 0;
                for (int i = 0; i < _hives.Length; i++)
                {
                    if (!_hives[i].IsDestroyed)
                    {
                        standing++;
                    }
                }

                return standing;
            }
        }

        /// <summary>True when no hive stands. The game is lost.</summary>
        public bool AllDestroyed => StandingCount == 0;

        /// <summary>Raised when a hive falls, with that hive's index.</summary>
        public event Action<int> HiveDestroyed;

        /// <summary>Finds the standing hive nearest to a point, ignoring height. False, with index -1, when none stands.</summary>
        public bool TryGetNearestStanding(Vector3 from, out int index)
        {
            index = -1;
            float best = float.MaxValue;
            for (int i = 0; i < _hives.Length; i++)
            {
                if (_hives[i].IsDestroyed)
                {
                    continue;
                }

                Vector3 position = _hives[i].Position;
                float dx = position.x - from.x;
                float dz = position.z - from.z;
                float squared = dx * dx + dz * dz;
                if (squared < best)
                {
                    best = squared;
                    index = i;
                }
            }

            return index >= 0;
        }

        /// <summary>Ticks every hive.</summary>
        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _hives.Length; i++)
            {
                _hives[i].Tick(deltaTime);
            }
        }

        private void OnHiveDestroyed(Hive hive)
        {
            for (int i = 0; i < _hives.Length; i++)
            {
                if (ReferenceEquals(_hives[i], hive))
                {
                    HiveDestroyed?.Invoke(i);
                    return;
                }
            }
        }
    }
}
