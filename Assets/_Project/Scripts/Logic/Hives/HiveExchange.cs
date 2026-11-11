using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// What passes between the core and the hive it stands at (plan section M). Within
    /// <see cref="HiveConfig.CollectRadius"/> of a standing hive, the hive's store flows into the swarm at
    /// <see cref="HiveConfig.CollectPerSecond"/> until the swarm is full. While Give is held there the hive hands out
    /// nothing; instead, if it is damaged, <see cref="HiveConfig.HealWaspsPerSecond"/> wasps a second flow from the swarm
    /// into its health at <see cref="HiveConfig.WaspsPerHealthPoint"/> wasps a point, stopping at full health and never
    /// taking the swarm below its collapse line.
    /// </summary>
    /// <remarks>
    /// During Hive Call the swarm's form calls every store itself (<see cref="SwarmFormMachine"/>), so the nearby store is
    /// not collected here a second time. Give still works then: it only stops the nearby hive's own hand-out.
    /// </remarks>
    public sealed class HiveExchange
    {
        private const int NoHive = -1;

        // Float error must not hide a wasp that is due, as in SwarmBody.
        private const float WholeEpsilon = 1e-4f;

        private readonly HiveConfig _config;
        private readonly HiveSet _hives;
        private readonly SwarmBody _swarm;

        private int _nearHive = NoHive;
        private bool _isCollecting;
        private bool _isGiving;
        private float _giveCarry;

        public HiveExchange(HiveConfig config, HiveSet hives, SwarmBody swarm)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _hives = hives ?? throw new ArgumentNullException(nameof(hives));
            _swarm = swarm ?? throw new ArgumentNullException(nameof(swarm));
        }

        /// <summary>The standing hive within CollectRadius of the core, the nearest when several are, or -1 when none is.</summary>
        public int NearHive => _nearHive;

        /// <summary>True while the nearby hive's store flows into the swarm.</summary>
        public bool IsCollecting => _isCollecting;

        /// <summary>True while Give is held beside a damaged hive and the swarm has wasps above its collapse line to give.</summary>
        public bool IsGiving => _isGiving;

        /// <summary>Raised with the hive's index when whole wasps arrive from its store.</summary>
        public event Action<int> Collected;

        /// <summary>Raised with the hive's index when wasps are paid into its health.</summary>
        public event Action<int> Gave;

        /// <summary>
        /// Finds the hive the core stands at and moves wasps for <paramref name="deltaTime"/>: from the swarm into its health
        /// while <paramref name="give"/> is held, otherwise from its store into the swarm, unless
        /// <paramref name="isHiveCalling"/>, when Hive Call already calls that store.
        /// </summary>
        public void Tick(float deltaTime, Vector3 core, bool give, bool isHiveCalling)
        {
            _isCollecting = false;
            _isGiving = false;
            _nearHive = FindNearHive(core);

            if (_nearHive == NoHive || !give)
            {
                _giveCarry = 0f;
            }

            if (_nearHive == NoHive || !(deltaTime > 0f))
            {
                return;
            }

            if (give)
            {
                Give(deltaTime);
                return;
            }

            if (!isHiveCalling)
            {
                Collect(deltaTime);
            }
        }

        private int FindNearHive(Vector3 core)
        {
            if (!_hives.TryGetNearestStanding(core, out int index))
            {
                return NoHive;
            }

            float radius = _config.CollectRadius;
            return GroundMath.DistanceSquared(_hives[index].Position, core) <= radius * radius ? index : NoHive;
        }

        private void Collect(float deltaTime)
        {
            Hive hive = _hives[_nearHive];
            float room = _swarm.Room;
            if (!(room > 0f) || !(hive.Store > 0f))
            {
                return;
            }

            _isCollecting = true;
            int arrived = _swarm.Gain(hive.HandOut(deltaTime, room));
            if (arrived > 0)
            {
                Collected?.Invoke(_nearHive);
            }
        }

        // Whole wasps only: the parts of a wasp due at the give rate are carried while Give stays held.
        private void Give(float deltaTime)
        {
            Hive hive = _hives[_nearHive];
            int needed = Mathf.CeilToInt(((hive.MaxHealth - hive.Health) * _config.WaspsPerHealthPoint) - WholeEpsilon);
            int spare = _swarm.Spendable;
            if (needed <= 0 || spare <= 0)
            {
                _giveCarry = 0f;
                return;
            }

            _isGiving = true;
            _giveCarry += _config.HealWaspsPerSecond * deltaTime;
            int due = (int)(_giveCarry + WholeEpsilon);
            if (due <= 0)
            {
                return;
            }

            int given = Math.Min(due, Math.Min(spare, needed));
            _giveCarry = given < due ? 0f : Math.Max(0f, _giveCarry - given);
            if (!_swarm.TrySpend(given))
            {
                return;
            }

            hive.Heal(given / _config.WaspsPerHealthPoint);
            Gave?.Invoke(_nearHive);
        }
    }
}
