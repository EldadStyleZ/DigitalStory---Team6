using System;
using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Everything fighting in the maze. Minions wake when the Piper or a creature comes close, and they
    /// hit the Piper only once no creature is left. Creatures follow the Piper and fight whatever is awake.
    /// </summary>
    public sealed class Battle
    {
        private const float SmallestGapSquared = 0.0001f;

        private readonly Maze _maze;
        private readonly BattleConfig _config;
        private readonly LabPiper _piper;
        private readonly Health _piperHealth;
        private readonly List<Creature> _creatures = new List<Creature>();
        private readonly List<Minion> _minions = new List<Minion>();
        private readonly List<Minion> _awake = new List<Minion>();
        private readonly List<Fighter> _crowd = new List<Fighter>();
        private readonly StepField _towardPiper;
        private readonly StepField _towardCreatures;
        private readonly StepField _towardMinions;

        private float _secondsToNewRoutes;
        private bool _routesAreStale = true;

        public Battle(Maze maze, BattleConfig config, LabPiper piper, Health piperHealth)
        {
            _maze = maze ?? throw new ArgumentNullException(nameof(maze));
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _piper = piper ?? throw new ArgumentNullException(nameof(piper));
            _piperHealth = piperHealth ?? throw new ArgumentNullException(nameof(piperHealth));
            _towardPiper = new StepField(maze);
            _towardCreatures = new StepField(maze);
            _towardMinions = new StepField(maze);
        }

        /// <summary>Raised once for each minion that falls, after it leaves the field.</summary>
        public event Action<Minion> MinionKilled;

        /// <summary>Raised for every hit.</summary>
        public event Action<StrikeReport> Struck;

        public IReadOnlyList<Creature> Creatures => _creatures;
        public IReadOnlyList<Minion> Minions => _minions;
        public IReadOnlyList<Minion> AwakeMinions => _awake;

        /// <summary>True while any minion has noticed the Piper or a creature.</summary>
        public bool IsInBattle => _awake.Count > 0;

        public bool HasRoomForACreature => _creatures.Count < _config.MaxCreatures;

        /// <summary>Summons a creature beside the Piper.</summary>
        public Creature AddCreature(CreatureType type)
        {
            var creature = new Creature(type, _piper.Position);
            _creatures.Add(creature);
            _routesAreStale = true;
            return creature;
        }

        public Minion AddMinion(MinionType type, Vector2 position, MinionRank rank = MinionRank.Ordinary)
        {
            var minion = new Minion(type, position, rank);
            _minions.Add(minion);
            return minion;
        }

        public void Tick(float deltaTime)
        {
            UpdateRoutes(deltaTime);
            UpdateAlertness();

            foreach (Creature creature in _creatures)
            {
                TickCreature(creature, deltaTime);
            }

            foreach (Minion minion in _awake)
            {
                TickMinion(minion, deltaTime);
            }

            SpreadOut(deltaTime);
            RemoveFallen();
        }

        /// <summary>Ends every fight: creatures vanish and minions go back to sleep where they stand.</summary>
        public void Retreat()
        {
            _creatures.Clear();
            _awake.Clear();

            foreach (Minion minion in _minions)
            {
                minion.Calm();
            }

            _routesAreStale = true;
        }

        private void UpdateRoutes(float deltaTime)
        {
            _secondsToNewRoutes -= deltaTime;

            if (!_routesAreStale && _secondsToNewRoutes > 0f)
            {
                return;
            }

            _secondsToNewRoutes = _config.RouteSeconds;
            _routesAreStale = false;
            _towardPiper.Fill(_maze.CellAt(_piper.Position));
            FillFrom(_towardCreatures, _creatures);
            FillFrom(_towardMinions, _awake);
        }

        private void FillFrom<T>(StepField field, List<T> fighters) where T : Fighter
        {
            field.Clear();

            foreach (T fighter in fighters)
            {
                field.AddSource(_maze.CellAt(fighter.Position));
            }

            field.Spread();
        }

        private void UpdateAlertness()
        {
            _awake.Clear();

            foreach (Minion minion in _minions)
            {
                UpdateAlertness(minion);

                if (minion.IsAwake)
                {
                    _awake.Add(minion);
                }
            }
        }

        private void UpdateAlertness(Minion minion)
        {
            int cell = _maze.CellAt(minion.Position);
            int steps = Mathf.Min(_towardPiper.StepsAt(cell), _towardCreatures.StepsAt(cell));
            float walkingTiles = steps * (float)_maze.Shape.CellPitch;

            if (minion.IsAwake && walkingTiles > minion.Type.DetectTiles * _config.GiveUpMultiplier)
            {
                minion.Calm();
            }
            else if (!minion.IsAwake && walkingTiles <= minion.Type.DetectTiles)
            {
                minion.Wake();
                _routesAreStale = true;
            }
        }

        // The leash only stops a creature going to look for a fight. It always takes on a minion beside
        // it, or something that hits from a distance could wear creatures down without ever being reached.
        private void TickCreature(Creature creature, float deltaTime)
        {
            creature.Rest(deltaTime);
            int cell = _maze.CellAt(creature.Position);
            Minion target = NearestLinked(_awake, creature.Position, cell);

            if (target != null)
            {
                FightMinion(creature, cell, target, deltaTime);
            }
            else if (CanGoLooking(cell))
            {
                Chase(creature, cell, _towardMinions, creature.Position, deltaTime);
            }
            else
            {
                FollowPiper(creature, cell, deltaTime);
            }
        }

        private void FightMinion(Creature creature, int cell, Minion target, float deltaTime)
        {
            if (IsInReach(creature, target.Position))
            {
                HitMinions(creature, target);
            }
            else
            {
                Chase(creature, cell, _towardMinions, target.Position, deltaTime);
            }
        }

        private bool CanGoLooking(int cell)
        {
            bool isOnLeash = _towardPiper.StepsAt(cell) * (float)_maze.Shape.CellPitch <= _config.LeashTiles;
            return isOnLeash && _towardMinions.StepsAt(cell) != StepField.Unreached;
        }

        private void FollowPiper(Creature creature, int cell, float deltaTime)
        {
            bool isCloseEnough = _maze.AreLinked(cell, _maze.CellAt(_piper.Position))
                && Vector2.Distance(creature.Position, _piper.Position) <= _config.FollowTiles;

            if (!isCloseEnough)
            {
                Chase(creature, cell, _towardPiper, _piper.Position, deltaTime);
            }
        }

        private void TickMinion(Minion minion, float deltaTime)
        {
            minion.Rest(deltaTime);
            int cell = _maze.CellAt(minion.Position);

            if (_creatures.Count > 0)
            {
                FightCreatures(minion, cell, deltaTime);
            }
            else
            {
                FightPiper(minion, cell, deltaTime);
            }
        }

        private void FightCreatures(Minion minion, int cell, float deltaTime)
        {
            Creature target = NearestLinked(_creatures, minion.Position, cell);

            if (target != null && IsInReach(minion, target.Position))
            {
                HitCreatures(minion, target);
                return;
            }

            Chase(minion, cell, _towardCreatures, target != null ? target.Position : minion.Position, deltaTime);
        }

        private void FightPiper(Minion minion, int cell, float deltaTime)
        {
            Vector2 piper = _piper.Position;

            if (!_maze.AreLinked(cell, _maze.CellAt(piper)) || !IsInReach(minion, piper))
            {
                Chase(minion, cell, _towardPiper, piper, deltaTime);
                return;
            }

            if (minion.CanHitNow)
            {
                minion.MarkHit();
                _piperHealth.Damage(minion.Stats.Damage);
                Struck?.Invoke(new StrikeReport(piper, 0f, false));
            }
        }

        private void Chase(Fighter fighter, int cell, StepField route, Vector2 target, float deltaTime)
        {
            int next = route.StepToward(cell);
            Vector2 goal = next == cell ? target : _maze.CenterOf(next);
            fighter.WalkToward(goal, _maze, _config.UnitRadius, deltaTime);
        }

        private bool IsInReach(Fighter attacker, Vector2 target)
        {
            float reach = attacker.Stats.Reach + (_config.UnitRadius * 2f);
            return (target - attacker.Position).sqrMagnitude <= reach * reach;
        }

        // Only cells joined by a passage count, so nothing hits through a wall.
        private T NearestLinked<T>(List<T> fighters, Vector2 position, int cell) where T : Fighter
        {
            T nearest = null;
            float nearestSquared = float.MaxValue;

            foreach (T fighter in fighters)
            {
                float squared = (fighter.Position - position).sqrMagnitude;

                if (fighter.IsAlive && squared < nearestSquared && _maze.AreLinked(cell, _maze.CellAt(fighter.Position)))
                {
                    nearest = fighter;
                    nearestSquared = squared;
                }
            }

            return nearest;
        }

        private void HitMinions(Creature attacker, Minion target)
        {
            if (attacker.CanHitNow)
            {
                attacker.MarkHit();
                Land(attacker.Stats, target, _minions, true);
            }
        }

        private void HitCreatures(Minion attacker, Creature target)
        {
            if (attacker.CanHitNow)
            {
                attacker.MarkHit();
                Land(attacker.Stats, target, _creatures, false);
            }
        }

        private void Land<T>(FighterStats stats, T target, List<T> side, bool isByCreature) where T : Fighter
        {
            Vector2 center = target.Position;
            Struck?.Invoke(new StrikeReport(center, stats.AreaRadius, isByCreature));

            if (!stats.HitsAnArea)
            {
                target.Health.Damage(stats.Damage);
                return;
            }

            int cell = _maze.CellAt(center);

            foreach (T fighter in side)
            {
                bool isCovered = Vector2.Distance(fighter.Position, center) <= stats.AreaRadius
                    && _maze.AreLinked(cell, _maze.CellAt(fighter.Position));

                if (isCovered)
                {
                    fighter.Health.Damage(stats.Damage);
                }
            }
        }

        private void SpreadOut(float deltaTime)
        {
            _crowd.Clear();
            _crowd.AddRange(_creatures);
            _crowd.AddRange(_awake);
            float halfStep = _config.SpreadSpeed * deltaTime / 2f;

            for (int first = 0; first < _crowd.Count; first++)
            {
                for (int second = first + 1; second < _crowd.Count; second++)
                {
                    PushApart(_crowd[first], _crowd[second], second, halfStep);
                }
            }
        }

        private void PushApart(Fighter first, Fighter second, int seed, float halfStep)
        {
            Vector2 gap = second.Position - first.Position;
            float squared = gap.sqrMagnitude;

            if (squared >= _config.PersonalSpace * _config.PersonalSpace)
            {
                return;
            }

            // Two fighters on the same spot have no direction between them, so one is made up.
            Vector2 heading = squared > SmallestGapSquared ? gap / Mathf.Sqrt(squared) : new Vector2(Mathf.Cos(seed), Mathf.Sin(seed));
            first.Nudge(heading * -halfStep, _maze, _config.UnitRadius);
            second.Nudge(heading * halfStep, _maze, _config.UnitRadius);
        }

        private void RemoveFallen()
        {
            for (int index = _creatures.Count - 1; index >= 0; index--)
            {
                if (!_creatures[index].IsAlive)
                {
                    _creatures.RemoveAt(index);
                    _routesAreStale = true;
                }
            }

            for (int index = _minions.Count - 1; index >= 0; index--)
            {
                Minion minion = _minions[index];

                if (!minion.IsAlive)
                {
                    _minions.RemoveAt(index);
                    _awake.Remove(minion);
                    _routesAreStale = true;
                    MinionKilled?.Invoke(minion);
                }
            }
        }
    }
}
