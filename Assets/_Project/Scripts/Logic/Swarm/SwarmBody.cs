using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The swarm's health and its one currency (plan section M): how many wasps are alive, how many it may hold, the collapse
    /// at too few, and the respawn after it. Wasps never come from nothing: they arrive from the hives' stores or a nest, and
    /// a price is only paid while it leaves the swarm at or above its collapse line.
    /// </summary>
    /// <remarks>
    /// The swarm grows during a match (plan section H): <see cref="GrowCapacity"/> raises how many wasps it may hold, up to
    /// <see cref="Max"/>. The collapse line, the low warning and the respawn are shares of the capacity, so they grow with it.
    /// Nothing here makes a song stronger: strength comes from the swarm's level, never from the count, which is also the
    /// player's health.
    /// </remarks>
    public sealed class SwarmBody
    {
        // Float error must not hide a wasp that is due, for example 0.5 + 0.49999997.
        private const float WholeEpsilon = 1e-4f;

        private readonly SwarmConfig _config;
        private int _count;
        private int _capacity;
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

            if (config.StartingCapacity < 1 || config.StartingCapacity > config.MaxWasps)
            {
                throw new ArgumentException("SwarmConfig.StartingCapacity must be between 1 and MaxWasps.", nameof(config));
            }

            if (config.StartingWasps < 0 || config.StartingWasps > config.StartingCapacity)
            {
                throw new ArgumentException("SwarmConfig.StartingWasps must be between 0 and StartingCapacity.", nameof(config));
            }

            RequireShare(config.CollapseShare, nameof(SwarmConfig.CollapseShare));
            RequireShare(config.RespawnShare, nameof(SwarmConfig.RespawnShare));
            RequireShare(config.LowShare, nameof(SwarmConfig.LowShare));
            RequireShare(config.MinRespawnShare, nameof(SwarmConfig.MinRespawnShare));

            // A respawn below the collapse line would leave nothing to spend, and the next loss would collapse it again.
            if (config.MinRespawnShare < config.CollapseShare)
            {
                throw new ArgumentException("SwarmConfig.MinRespawnShare must be at least CollapseShare.", nameof(config));
            }

            _capacity = config.StartingCapacity;
            _count = config.StartingWasps;
        }

        /// <summary>How many wasps are alive.</summary>
        public int Count => _count;

        /// <summary>How many wasps the swarm may hold now. It starts at SwarmConfig.StartingCapacity and grows with each nest, up to <see cref="Max"/>.</summary>
        public int Capacity => _capacity;

        /// <summary>The most wasps the capacity can ever reach: SwarmConfig.MaxWasps, the size of the GPU buffers.</summary>
        public int Max => _config.MaxWasps;

        /// <summary>True from a collapse until the respawn.</summary>
        public bool IsCollapsed => _isCollapsed;

        /// <summary>
        /// Seconds until the collapsed swarm comes back, for the HUD's count (plan section O): the collapse block left, which
        /// ends in the respawn while a hive stands. 0 while the swarm is not collapsed, and once the block is over.
        /// </summary>
        public float SecondsToRespawn => _isCollapsed ? _blockLeft : 0f;

        /// <summary>The swarm collapses when a loss leaves fewer wasps than this: SwarmConfig.CollapseShare of the capacity.</summary>
        public int CollapseLine => ShareOfCapacity(_config.CollapseShare);

        /// <summary>The HUD warns that the swarm is low below this: SwarmConfig.LowShare of the capacity.</summary>
        public int LowLine => ShareOfCapacity(_config.LowShare);

        /// <summary>True while fewer wasps are alive than <see cref="LowLine"/>, collapsed included.</summary>
        public bool IsLow => _count < LowLine;

        /// <summary>The most wasps a respawn takes from the hives' stores: SwarmConfig.RespawnShare of the capacity.</summary>
        public int RespawnWasps => ShareOfCapacity(_config.RespawnShare);

        /// <summary>The fewest wasps a respawn comes back with, however little the stores hold: SwarmConfig.MinRespawnShare of the capacity.</summary>
        public int MinRespawnWasps => ShareOfCapacity(_config.MinRespawnShare);

        /// <summary>
        /// How many more wasps can join before the swarm is full, less the part of a wasp already on its way. 0 while
        /// collapsed: a collapsed swarm takes nothing in.
        /// </summary>
        public float Room => _isCollapsed ? 0f : Math.Max(0f, _capacity - _count - _gainCarry);

        /// <summary>How many wasps can be paid without taking the swarm below its collapse line. 0 while collapsed.</summary>
        public int Spendable => _isCollapsed ? 0 : Math.Max(0, _count - CollapseLine);

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

        /// <summary>
        /// Raises how many wasps the swarm may hold by <paramref name="wasps"/> (a nest), up to <see cref="Max"/>, and returns
        /// how much it rose. It adds no wasp: those come through <see cref="Gain"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The amount is negative.</exception>
        public int GrowCapacity(int wasps)
        {
            if (wasps < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(wasps), wasps, "The capacity only grows.");
            }

            int grown = Math.Min(wasps, _config.MaxWasps - _capacity);
            _capacity += grown;
            return grown;
        }

        /// <summary>
        /// Adds wasps that arrived (a hive's store flowing in, a nest, a bonus), capped at the capacity, and returns how many
        /// whole wasps joined. Parts of a wasp, as a store flows in a little each tick, are carried to the next call. Nothing
        /// while collapsed, and nothing for 0 or less.
        /// </summary>
        public int Gain(float wasps)
        {
            if (_isCollapsed || !(wasps > 0f))
            {
                return 0;
            }

            int room = _capacity - _count;
            if (room <= 0)
            {
                _gainCarry = 0f;
                return 0;
            }

            _gainCarry += wasps;
            int whole = (int)(_gainCarry + WholeEpsilon);
            if (whole <= 0)
            {
                return 0;
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
            return whole;
        }

        /// <summary>
        /// Pays <paramref name="wasps"/> (a song's price, a gift to a hive) and returns true, but only while the swarm keeps at
        /// least its collapse line: paying never collapses it. Refused, changing nothing, when it would leave fewer, or while
        /// collapsed.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The price is negative.</exception>
        public bool TrySpend(int wasps)
        {
            if (wasps < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(wasps), wasps, "A price cannot be negative.");
            }

            if (_isCollapsed || wasps > Spendable)
            {
                return false;
            }

            if (wasps > 0)
            {
                _count -= wasps;
                CountChanged?.Invoke(_count);
            }

            return true;
        }

        /// <summary>
        /// Counts the collapse block down, then respawns the swarm once a hive stands. It takes up to
        /// <see cref="RespawnWasps"/> from the standing hives' stores, and never comes back with fewer than
        /// <see cref="MinRespawnWasps"/>, however little they hold.
        /// </summary>
        public void Tick(float deltaTime, HiveSet hives)
        {
            if (hives == null)
            {
                throw new ArgumentNullException(nameof(hives));
            }

            if (!_isCollapsed)
            {
                return;
            }

            if (deltaTime > 0f)
            {
                _blockLeft = Math.Max(0f, _blockLeft - deltaTime);
            }

            if (_blockLeft > 0f || hives.StandingCount <= 0)
            {
                return;
            }

            int fromStores = hives.TakeFromStores(RespawnWasps);
            _isCollapsed = false;
            _count = Math.Min(_capacity, Math.Max(fromStores, MinRespawnWasps));
            CountChanged?.Invoke(_count);
            Respawned?.Invoke();
        }

        private static void RequireShare(float share, string name)
        {
            if (!(share >= 0f && share <= 1f))
            {
                throw new ArgumentException($"SwarmConfig.{name} must be from 0 to 1, but is {share}.", "config");
            }
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
            if (_count < CollapseLine)
            {
                Collapse();
            }
            else if (removed > 0)
            {
                CountChanged?.Invoke(_count);
            }

            return removed;
        }

        // Rounded, so float error in a share such as 0.12 cannot move a line by a wasp: 12% of 800 is 96.
        private int ShareOfCapacity(float share)
        {
            return Mathf.RoundToInt(share * _capacity);
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
