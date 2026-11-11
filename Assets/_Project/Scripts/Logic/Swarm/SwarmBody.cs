using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>The swarm's health: how many wasps are alive, the collapse at too few, and the respawn after it.</summary>
    public sealed class SwarmBody
    {
        // Float error must not hide a wasp that is due, for example 0.5 + 0.49999997.
        private const float WholeEpsilon = 1e-4f;

        private readonly SwarmConfig _config;
        private int _count;
        private bool _isCollapsed;
        private float _blockLeft;
        private float _drainCarry;
        private float _gainCarry;

        public SwarmBody(SwarmConfig config)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            if (config.MaxWasps < 1)
            {
                throw new ArgumentException("SwarmConfig.MaxWasps must be at least 1.", nameof(config));
            }

            if (config.StartingWasps < 0 || config.StartingWasps > config.MaxWasps)
            {
                throw new ArgumentException("SwarmConfig.StartingWasps must be between 0 and MaxWasps.", nameof(config));
            }

            if (config.RespawnWasps < 0 || config.RespawnWasps > config.MaxWasps)
            {
                throw new ArgumentException("SwarmConfig.RespawnWasps must be between 0 and MaxWasps.", nameof(config));
            }

            _count = config.StartingWasps;
        }

        /// <summary>How many wasps are alive.</summary>
        public int Count => _count;

        /// <summary>The most wasps there can be.</summary>
        public int Max => _config.MaxWasps;

        /// <summary>True from a collapse until the respawn.</summary>
        public bool IsCollapsed => _isCollapsed;

        /// <summary>Raised when the count changes, with the new count.</summary>
        public event Action<int> CountChanged;

        /// <summary>Raised when the count falls below the collapse limit.</summary>
        public event Action Collapsed;

        /// <summary>Raised when the swarm comes back after a collapse.</summary>
        public event Action Respawned;

        /// <summary>
        /// Removes wasps and returns how many were removed. Only <paramref name="lossFraction"/> of the loss, rounded, gets
        /// through: the share the running songs let through (<see cref="SwarmFormMachine.WaspLossFraction"/>), such as the
        /// dome's.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The fraction is not from 0 to 1.</exception>
        public int Lose(int wasps, float lossFraction = 1f)
        {
            RequireFraction(lossFraction);

            if (_isCollapsed || wasps <= 0)
            {
                return 0;
            }

            return Remove(Mathf.RoundToInt(wasps * lossFraction));
        }

        /// <summary>Removes wasps at a rate (smoke), carrying fractions between calls. Same rules as Lose.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The fraction is not from 0 to 1.</exception>
        public void Drain(float waspsPerSecond, float deltaTime, float lossFraction = 1f)
        {
            RequireFraction(lossFraction);

            if (_isCollapsed || !(waspsPerSecond > 0f) || !(deltaTime > 0f))
            {
                return;
            }

            _drainCarry += waspsPerSecond * deltaTime * lossFraction;
            int whole = (int)(_drainCarry + WholeEpsilon);
            if (whole <= 0)
            {
                return;
            }

            _drainCarry = Math.Max(0f, _drainCarry - whole);
            Remove(whole);
        }

        /// <summary>Hive Call: adds wasps at a rate per standing hive, carrying fractions, capped at Max, nothing while collapsed.</summary>
        public void Gain(float deltaTime, int standingHives)
        {
            if (_isCollapsed || standingHives <= 0 || !(deltaTime > 0f))
            {
                return;
            }

            int room = _config.MaxWasps - _count;
            if (room <= 0)
            {
                _gainCarry = 0f;
                return;
            }

            _gainCarry += _config.HiveCallWaspsPerSecondPerHive * standingHives * deltaTime;
            int whole = (int)(_gainCarry + WholeEpsilon);
            if (whole <= 0)
            {
                return;
            }

            if (whole >= room)
            {
                whole = room;
                _gainCarry = 0f;
            }
            else
            {
                _gainCarry = Math.Max(0f, _gainCarry - whole);
            }

            _count += whole;
            CountChanged?.Invoke(_count);
        }

        /// <summary>Counts the collapse block down, then respawns the swarm if a hive stands.</summary>
        public void Tick(float deltaTime, int standingHives)
        {
            if (!_isCollapsed)
            {
                return;
            }

            if (deltaTime > 0f)
            {
                _blockLeft = Math.Max(0f, _blockLeft - deltaTime);
            }

            if (_blockLeft > 0f || standingHives <= 0)
            {
                return;
            }

            _isCollapsed = false;
            _count = _config.RespawnWasps;
            CountChanged?.Invoke(_count);
            Respawned?.Invoke();
        }

        private static void RequireFraction(float lossFraction)
        {
            if (!(lossFraction >= 0f && lossFraction <= 1f))
            {
                throw new ArgumentOutOfRangeException(nameof(lossFraction), lossFraction, "The share of a loss that gets through must be from 0 to 1.");
            }
        }

        private int Remove(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int removed = Math.Min(amount, _count);
            _count -= removed;
            if (_count < _config.CollapseBelow)
            {
                Collapse();
            }
            else if (removed > 0)
            {
                CountChanged?.Invoke(_count);
            }

            return removed;
        }

        private void Collapse()
        {
            _count = 0;
            _isCollapsed = true;
            _blockLeft = _config.CollapseBlockSeconds;
            _drainCarry = 0f;
            _gainCarry = 0f;
            CountChanged?.Invoke(0);
            Collapsed?.Invoke();
        }
    }
}
