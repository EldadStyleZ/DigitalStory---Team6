using System;
using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Everything fighting in the maze. Minions wake when the Piper or a creature comes close, and they
    /// hit the Piper only once no creature is left. Creatures follow the Piper and fight whatever is awake,
    /// and each side's traits shape how: both sides follow the same rules.
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
        private readonly List<Minion> _standingBack = new List<Minion>();
        private readonly List<Fighter> _crowd = new List<Fighter>();
        private readonly StepField _towardPiper;
        private readonly StepField _towardCreatures;
        private readonly StepField _towardMinions;
        private readonly StepField _towardStandingBack;

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
            _towardStandingBack = new StepField(maze);
        }

        /// <summary>Raised once for each minion that falls, after it leaves the field.</summary>
        public event Action<Minion> MinionKilled;

        /// <summary>Raised for every hit.</summary>
        public event Action<StrikeReport> Struck;

        /// <summary>Raised with the place health came back.</summary>
        public event Action<Vector2> Healed;

        /// <summary>Raised for each creature that goes home after a quiet spell.</summary>
        public event Action<Creature> CreatureLeft;

        public IReadOnlyList<Creature> Creatures => _creatures;
        public IReadOnlyList<Minion> Minions => _minions;
        public IReadOnlyList<Minion> AwakeMinions => _awake;

        /// <summary>True while any minion has noticed the Piper or a creature.</summary>
        public bool IsInBattle => _awake.Count > 0;

        public bool HasRoomForACreature => _creatures.Count < _config.MaxCreatures;

        /// <summary>Seconds of game time the battle has run. It stops while the game is paused.</summary>
        public float ElapsedSeconds { get; private set; }

        /// <summary>The battle songs still working.</summary>
        public SongEffects Songs { get; } = new SongEffects();

        public bool IsBoosted => Songs.IsAnyRunning;
        public float BoostSecondsLeft => Songs.LongestSecondsLeft;

        /// <summary>Plays a battle song. Different songs work together.</summary>
        public void Play(BattleSong song)
        {
            SongBuff buff = song.Buff;

            if (buff.Healing > 0f)
            {
                HealEveryone(buff.Healing);
            }

            if (buff.EnemySlowShare > 0f || buff.BurstDamage > 0f)
            {
                StrikeEveryEnemy(buff);
            }

            if (buff.Lasts)
            {
                Songs.Add(song);
            }
        }

        /// <summary>How many cells of walking a cell is from the Piper, as of the last route update.</summary>
        public int StepsToPiper(int cell) => _towardPiper.StepsAt(cell);

        /// <summary>Summons a creature beside the Piper.</summary>
        public Creature AddCreature(CreatureType type)
        {
            var creature = new Creature(type, _piper.Position);
            _creatures.Add(creature);
            _routesAreStale = true;
            return creature;
        }

        public Minion AddMinion(MinionType type, Vector2 position, MinionRank rank = MinionRank.Ordinary, MinionGang gang = null)
        {
            var minion = new Minion(type, position, rank, gang);
            _minions.Add(minion);
            gang?.Add(minion);
            _routesAreStale = true;
            return minion;
        }

        public void Tick(float deltaTime)
        {
            ElapsedSeconds += deltaTime;
            PassTime(_creatures, deltaTime);
            PassTime(_minions, deltaTime);
            Songs.Tick(deltaTime);
            UpdateRoutes(deltaTime);
            UpdateAlertness();

            // A faster creature simply lives through more time, so it walks and hits faster alike.
            foreach (Creature creature in _creatures)
            {
                TickCreature(creature, deltaTime * Songs.PaceMultiplier);
            }

            foreach (Minion minion in _awake)
            {
                TickMinion(minion, deltaTime);
            }

            SpreadOut(deltaTime);
            RemoveFallen();
            SendQuietCreaturesHome(deltaTime);
        }

        /// <summary>Ends every fight: creatures vanish and minions go back to sleep where they stand.</summary>
        public void Retreat()
        {
            _creatures.Clear();
            _awake.Clear();
            _standingBack.Clear();
            Songs.Clear();

            foreach (Minion minion in _minions)
            {
                minion.Calm();
            }

            _routesAreStale = true;
        }

        private static void PassTime<T>(List<T> fighters, float deltaTime) where T : Fighter
        {
            foreach (T fighter in fighters)
            {
                fighter.PassTime(deltaTime);
            }
        }

        private void HealEveryone(float amount)
        {
            foreach (Creature creature in _creatures)
            {
                creature.Health.Heal(amount);
                Healed?.Invoke(creature.Position);
            }

            _piperHealth.Heal(amount);
            Healed?.Invoke(_piper.Position);
        }

        private void StrikeEveryEnemy(SongBuff buff)
        {
            foreach (Minion minion in _awake)
            {
                if (buff.EnemySlowShare > 0f)
                {
                    minion.Slow(buff.EnemySlowShare, buff.EnemySlowSeconds);
                }

                if (buff.BurstDamage > 0f)
                {
                    minion.TakeHit(buff.BurstDamage);
                    Struck?.Invoke(new StrikeReport(minion.Position, 0f, true));
                }
            }
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
            FillFrom(_towardStandingBack, _standingBack);
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
            _standingBack.Clear();

            foreach (Minion minion in _minions)
            {
                UpdateAlertness(minion);

                if (minion.IsAwake)
                {
                    _awake.Add(minion);
                }

                if (minion.IsAwake && minion.Stats.Traits.StandsBack)
                {
                    _standingBack.Add(minion);
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
                Rouse(minion);
            }
        }

        private void Rouse(Minion minion)
        {
            minion.Wake();
            _routesAreStale = true;

            if (minion.Gang == null)
            {
                return;
            }

            foreach (Minion member in minion.Gang.Members)
            {
                member.Wake();
            }
        }

        // The leash only stops a creature going to look for a fight. It always takes on a minion beside
        // it, or something that hits from a distance could wear creatures down without ever being reached.
        private void TickCreature(Creature creature, float deltaTime)
        {
            creature.Rest(deltaTime);
            int cell = _maze.CellAt(creature.Position);
            MendCreatures(creature, cell);

            if (creature.Stats.Traits.HuntsBackline && Hunt(creature, cell, deltaTime))
            {
                return;
            }

            Minion target = NearestLinked(_awake, creature.Position, cell);

            if (target != null)
            {
                FightMinion(creature, cell, target, deltaTime);
            }
            else if (CanGoLooking(cell, _towardMinions))
            {
                Chase(creature, cell, _towardMinions, creature.Position, deltaTime);
            }
            else
            {
                FollowPiper(creature, cell, deltaTime);
            }
        }

        // A hunter ignores the front line and goes straight for whatever stands behind it.
        private bool Hunt(Creature creature, int cell, float deltaTime)
        {
            Minion prey = NearestLinked(_standingBack, creature.Position, cell);

            if (prey != null)
            {
                FightMinion(creature, cell, prey, deltaTime);
                return true;
            }

            if (CanGoLooking(cell, _towardStandingBack))
            {
                Chase(creature, cell, _towardStandingBack, creature.Position, deltaTime);
                return true;
            }

            return false;
        }

        private void FightMinion(Creature creature, int cell, Minion target, float deltaTime)
        {
            if (IsInReach(creature, target.Position))
            {
                HitMinions(creature, target);
            }
            else
            {
                Approach(creature, cell, target.Position, deltaTime);
            }
        }

        private bool CanGoLooking(int cell, StepField route)
        {
            bool isOnLeash = _towardPiper.StepsAt(cell) * (float)_maze.Shape.CellPitch <= _config.LeashTiles;
            return isOnLeash && route.StepsAt(cell) != StepField.Unreached;
        }

        private void MendCreatures(Creature healer, int cell)
        {
            if (!healer.Stats.Traits.Heals || !healer.CanHitNow)
            {
                return;
            }

            Creature hurt = MostHurtInReach(healer, cell, _creatures);
            bool isPiperInReach = IsInReach(healer, _piper.Position) && _maze.AreLinked(cell, _maze.CellAt(_piper.Position));
            float worstShare = hurt != null ? hurt.Health.Share : 1f;

            if (isPiperInReach && _piperHealth.Share < worstShare)
            {
                Heal(healer, _piperHealth, _piper.Position);
            }
            else if (hurt != null)
            {
                Heal(healer, hurt.Health, hurt.Position);
            }
        }

        private void MendMinions(Minion healer, int cell)
        {
            if (!healer.Stats.Traits.Heals || !healer.CanHitNow || Songs.StopsEnemyHealing)
            {
                return;
            }

            Minion hurt = MostHurtInReach(healer, cell, _awake);

            if (hurt != null)
            {
                Heal(healer, hurt.Health, hurt.Position);
            }
        }

        private T MostHurtInReach<T>(Fighter healer, int cell, List<T> side) where T : Fighter
        {
            T worst = null;
            float worstShare = 1f;

            foreach (T fighter in side)
            {
                bool canReach = fighter.IsAlive && IsInReach(healer, fighter.Position) && _maze.AreLinked(cell, _maze.CellAt(fighter.Position));

                if (canReach && fighter.Health.Share < worstShare)
                {
                    worst = fighter;
                    worstShare = fighter.Health.Share;
                }
            }

            return worst;
        }

        private void Heal(Fighter healer, Health health, Vector2 position)
        {
            healer.TurnToward(position);
            healer.MarkHit();
            health.Heal(healer.Stats.Traits.Healing);
            Healed?.Invoke(position);
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
            MendMinions(minion, cell);

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
            Creature target = GuardOver(minion, cell) ?? NearestLinked(_creatures, minion.Position, cell);

            if (target == null)
            {
                Chase(minion, cell, _towardCreatures, minion.Position, deltaTime);
            }
            else if (IsInReach(minion, target.Position))
            {
                HitCreatures(minion, target);
            }
            else
            {
                Approach(minion, cell, target.Position, deltaTime);
            }
        }

        // Only a guardian joined by a passage counts, so nothing is drawn to one through a wall.
        private Creature GuardOver(Minion minion, int cell)
        {
            Creature nearest = null;
            float nearestSquared = float.MaxValue;

            foreach (Creature creature in _creatures)
            {
                float range = creature.Stats.Traits.GuardTiles;
                float squared = (creature.Position - minion.Position).sqrMagnitude;
                bool isGuarding = creature.Stats.Traits.Guards && creature.IsAlive && squared <= range * range;

                if (isGuarding && squared < nearestSquared && _maze.AreLinked(cell, _maze.CellAt(creature.Position)))
                {
                    nearest = creature;
                    nearestSquared = squared;
                }
            }

            return nearest;
        }

        private void FightPiper(Minion minion, int cell, float deltaTime)
        {
            Vector2 piper = _piper.Position;

            if (!_maze.AreLinked(cell, _maze.CellAt(piper)) || !IsInReach(minion, piper))
            {
                Chase(minion, cell, _towardPiper, piper, deltaTime);
                return;
            }

            if (CanStrike(minion))
            {
                minion.TurnToward(piper);
                minion.MarkHit();
                _piperHealth.Damage(DamageOf(minion, _awake));
                Struck?.Invoke(new StrikeReport(piper, 0f, false));
            }
        }

        private void Chase(Fighter fighter, int cell, StepField route, Vector2 target, float deltaTime)
        {
            int next = route.StepToward(cell);
            Vector2 goal = next == cell ? target : _maze.CenterOf(next);
            fighter.WalkToward(goal, _maze, _config.UnitRadius, deltaTime);
        }

        // The target stands in this cell or the next, so there is no need for a route.
        private void Approach(Fighter fighter, int cell, Vector2 target, float deltaTime)
        {
            int targetCell = _maze.CellAt(target);
            Vector2 goal = targetCell == cell ? target : _maze.CenterOf(targetCell);
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

        private static bool IsStriker(Fighter fighter) => !fighter.Stats.Traits.Heals;

        private static bool CanStrike(Fighter attacker) => attacker.CanHitNow && IsStriker(attacker);

        private void HitMinions(Creature attacker, Minion target)
        {
            if (CanStrike(attacker))
            {
                attacker.TurnToward(target.Position);
                attacker.MarkHit();
                float damage = DamageOf(attacker, _creatures) * Songs.DamageMultiplier * FinisherOn(target);
                Land(attacker.Stats, damage, target, _minions, true);
            }
        }

        private void HitCreatures(Minion attacker, Creature target)
        {
            if (CanStrike(attacker))
            {
                attacker.TurnToward(target.Position);
                attacker.MarkHit();
                float damage = DamageOf(attacker, _awake) * (1f - Songs.ShieldShare);
                Land(attacker.Stats, damage, target, _creatures, false);
            }
        }

        private float FinisherOn(Minion target)
        {
            return target.Health.Share < _config.FinisherHealthShare ? Songs.FinisherMultiplier : 1f;
        }

        private float DamageOf<T>(T attacker, List<T> side) where T : Fighter
        {
            float bonus = attacker.Stats.Traits.PackBonus;
            return attacker.Stats.Damage * (1f + (bonus * PackmatesOf(attacker, side)));
        }

        private int PackmatesOf<T>(T attacker, List<T> side) where T : Fighter
        {
            if (attacker.Stats.Traits.PackBonus <= 0f)
            {
                return 0;
            }

            int packmates = 0;
            float rangeSquared = _config.PackTiles * _config.PackTiles;

            foreach (T fighter in side)
            {
                bool isPackmate = fighter != attacker && fighter.Stats.Traits.PackBonus > 0f;

                if (isPackmate && (fighter.Position - attacker.Position).sqrMagnitude <= rangeSquared)
                {
                    packmates++;
                }
            }

            return packmates;
        }

        private void Land<T>(FighterStats stats, float damage, T target, List<T> side, bool isByCreature) where T : Fighter
        {
            Vector2 center = target.Position;
            Struck?.Invoke(new StrikeReport(center, stats.AreaRadius, isByCreature));

            if (!stats.HitsAnArea)
            {
                Affect(target, stats, damage);
                return;
            }

            int cell = _maze.CellAt(center);

            foreach (T fighter in side)
            {
                bool isCovered = Vector2.Distance(fighter.Position, center) <= stats.AreaRadius
                    && _maze.AreLinked(cell, _maze.CellAt(fighter.Position));

                if (isCovered)
                {
                    Affect(fighter, stats, damage);
                }
            }
        }

        private static void Affect(Fighter fighter, FighterStats stats, float damage)
        {
            fighter.TakeHit(damage);

            if (stats.Traits.SlowShare > 0f)
            {
                fighter.Slow(stats.Traits.SlowShare, stats.Traits.SlowSeconds);
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
                    _standingBack.Remove(minion);
                    _routesAreStale = true;
                    MinionKilled?.Invoke(minion);
                }
            }
        }

        // Summoning works anywhere, so this is what stops a full army walking the maze for free. Healers
        // never hit, so a fight only they are left in could never end and counts as no fight at all.
        private void SendQuietCreaturesHome(float deltaTime)
        {
            bool isFighting = IsInBattle && HasAStriker();

            for (int index = _creatures.Count - 1; index >= 0; index--)
            {
                Creature creature = _creatures[index];

                if (isFighting)
                {
                    creature.KeepBusy();
                    continue;
                }

                creature.WaitQuietly(deltaTime);

                if (creature.QuietSeconds >= _config.QuietSecondsBeforeLeaving)
                {
                    _creatures.RemoveAt(index);
                    _routesAreStale = true;
                    CreatureLeft?.Invoke(creature);
                }
            }
        }

        private bool HasAStriker()
        {
            foreach (Creature creature in _creatures)
            {
                if (IsStriker(creature))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
