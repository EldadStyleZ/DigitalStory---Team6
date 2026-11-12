using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// All the hives of a level: counts the standing ones, finds the nearest, and draws on the standing hives' stores
    /// (plan section M). Where several stores are drawn on, the lower index gives first.
    /// </summary>
    public sealed class HiveSet
    {
        // Not game tuning: a store with less than one whole wasp in it counts as empty.
        private const float OneWasp = 1f;

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

        /// <summary>True while a standing hive holds at least one whole wasp in its store.</summary>
        public bool HasStoredWasps
        {
            get
            {
                for (int i = 0; i < _hives.Length; i++)
                {
                    if (!_hives[i].IsDestroyed && _hives[i].Store >= OneWasp)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>True while a standing hive is below full health, so Give has something to heal.</summary>
        public bool HasDamagedHive
        {
            get
            {
                for (int i = 0; i < _hives.Length; i++)
                {
                    if (_hives[i].IsDamaged)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>True while a standing hive is under attack: it was hurt within HiveConfig.UnderAttackSeconds (<see cref="Hive.IsUnderAttack"/>).</summary>
        public bool HasHiveUnderAttack
        {
            get
            {
                for (int i = 0; i < _hives.Length; i++)
                {
                    if (!_hives[i].IsDestroyed && _hives[i].IsUnderAttack)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

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

        /// <summary>
        /// Every standing hive's store flows out at its collect rate for <paramref name="deltaTime"/>, as Hive Call calls
        /// them, but no more than <paramref name="most"/> wasps in all. Returns how many left the stores, parts of a wasp included.
        /// </summary>
        public float HandOutAll(float deltaTime, float most)
        {
            float handed = 0f;
            for (int i = 0; i < _hives.Length && handed < most; i++)
            {
                if (!_hives[i].IsDestroyed)
                {
                    handed += _hives[i].HandOut(deltaTime, most - handed);
                }
            }

            return handed;
        }

        /// <summary>
        /// Takes up to <paramref name="wasps"/> whole wasps from the standing hives' stores, as the respawn after a collapse
        /// does, and returns how many it took. Parts of a wasp stay in the stores.
        /// </summary>
        public int TakeFromStores(int wasps)
        {
            int taken = 0;
            for (int i = 0; i < _hives.Length && taken < wasps; i++)
            {
                Hive hive = _hives[i];
                if (hive.IsDestroyed)
                {
                    continue;
                }

                int whole = Math.Min(wasps - taken, (int)hive.Store);
                hive.TakeFromStore(whole);
                taken += whole;
            }

            return taken;
        }

        /// <summary>Ticks every hive: their under-attack timers and their stores.</summary>
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
