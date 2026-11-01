using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>One run through the lab's maze, and the only way the view changes it.</summary>
    public sealed class LabSession
    {
        private readonly StaminaConfig _staminaConfig;

        private int _revivalCell;
        private bool _hasReportedDeath;

        public LabSession(LabRules rules)
        {
            _ = rules ?? throw new ArgumentNullException(nameof(rules));
            _staminaConfig = rules.Stamina;

            var random = new Mulberry32Random((uint)rules.MazeSeed);
            Maze = MazeGenerator.Generate(rules.MazeShape, random);
            Piper = new LabPiper(rules.Player, Maze);
            PiperHealth = new Health(rules.Player.MaxHealth);
            Stamina = new Stamina(rules.Stamina);
            Combos = new ComboReader(rules.Combos, rules.Creatures);
            Cooldowns = new ComboCooldowns(rules.Creatures);
            Levels = new LevelTrack(rules.Levels);
            Battle = new Battle(Maze, rules.Battle, Piper, PiperHealth);

            _revivalCell = Maze.Entrance;
            MinionPlacer.Populate(Battle, Maze, rules.Population, random);
            Battle.MinionKilled += HandleMinionKilled;
            Levels.LevelledUp += HandleLevelledUp;
        }

        /// <summary>Raised when a note or a combo could not summon anything.</summary>
        public event Action<SummonRefusal> SummonRefused;

        /// <summary>Raised with each note the flute answers.</summary>
        public event Action<int> NotePlayed;

        public event Action<CreatureType> Summoned;
        public event Action ComboFailed;
        public event Action PiperDied;
        public event Action Won;

        public Maze Maze { get; }
        public LabPiper Piper { get; }
        public Health PiperHealth { get; }
        public Stamina Stamina { get; }
        public ComboReader Combos { get; }
        public ComboCooldowns Cooldowns { get; }
        public LevelTrack Levels { get; }
        public Battle Battle { get; }

        /// <summary>
        /// Plays a note. Outside a battle the flute does nothing, so nobody can gather creatures while
        /// idle, and a stray key costs nothing.
        /// </summary>
        public void Press(int note)
        {
            if (!Battle.IsInBattle)
            {
                SummonRefused?.Invoke(SummonRefusal.NotInBattle);
                return;
            }

            NotePlayed?.Invoke(note);
            Apply(Combos.Press(note, Levels.Level));
        }

        /// <summary>Advances the run by one step.</summary>
        public void Tick(Vector2 move, float deltaTime)
        {
            Piper.Tick(move, deltaTime);
            Stamina.Tick(deltaTime);
            Cooldowns.Tick(deltaTime);
            Apply(Combos.Tick(deltaTime));
            Battle.Tick(deltaTime);

            if (!Battle.IsInBattle)
            {
                Combos.Clear();
            }

            ReportDeath();
        }

        /// <summary>
        /// Brings the Piper back with the same level and experience, at the last boss beaten or the
        /// entrance. Minions already killed stay dead.
        /// </summary>
        public void Continue()
        {
            PiperHealth.Refill();
            Stamina.Refill();
            Cooldowns.Reset();
            Combos.Clear();
            Battle.Retreat();
            Piper.MoveTo(_revivalCell);
            _hasReportedDeath = false;
        }

        private void Apply(ComboResult result)
        {
            if (result.Outcome == ComboOutcome.Failed)
            {
                Stamina.Lose(_staminaConfig.FailedComboCost);
                ComboFailed?.Invoke();
            }
            else if (result.Outcome == ComboOutcome.Completed)
            {
                TrySummon(result.Creature);
            }
        }

        private void TrySummon(CreatureType creature)
        {
            if (!Battle.HasRoomForACreature)
            {
                SummonRefused?.Invoke(SummonRefusal.NoRoom);
            }
            else if (!Cooldowns.IsReady(creature))
            {
                SummonRefused?.Invoke(SummonRefusal.CoolingDown);
            }
            else if (!Stamina.TrySpend(creature.StaminaCost))
            {
                SummonRefused?.Invoke(SummonRefusal.TooTired);
            }
            else
            {
                Cooldowns.Start(creature);
                Battle.AddCreature(creature);
                Summoned?.Invoke(creature);
            }
        }

        private void ReportDeath()
        {
            if (!PiperHealth.IsAlive && !_hasReportedDeath)
            {
                _hasReportedDeath = true;
                PiperDied?.Invoke();
            }
        }

        private void HandleMinionKilled(Minion minion)
        {
            Levels.Add(minion.Type.XpReward);

            if (minion.IsBoss)
            {
                _revivalCell = Maze.CellAt(minion.Position);
            }

            if (minion.IsFinalBoss)
            {
                Won?.Invoke();
            }
        }

        private void HandleLevelledUp(int level)
        {
            Stamina.Refill();
            PiperHealth.Refill();
        }
    }
}
