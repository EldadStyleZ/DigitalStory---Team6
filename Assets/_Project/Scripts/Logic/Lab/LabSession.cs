using System;
using System.Collections.Generic;
using System.Linq;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>One run through the lab's maze, and the only way the view changes it.</summary>
    public sealed class LabSession
    {
        private readonly StaminaConfig _staminaConfig;
        private readonly Dictionary<IComboSong, IComboAction> _actions = new Dictionary<IComboSong, IComboAction>();

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
            Levels = new LevelTrack(rules.Levels);
            Battle = new Battle(Maze, rules.Battle, Piper, PiperHealth);
            Songs = PrepareSongs(rules);
            Combos = new ComboReader(rules.Combos, Songs);
            Cooldowns = new ComboCooldowns(Songs);

            _revivalCell = Maze.Entrance;
            Ambushes = new AmbushWatch(Maze, rules.Ambush, Battle, MinionPlacer.Populate(Battle, Maze, rules.Population, random));
            Signposts = new Signposts(Maze, rules.SignpostChance, random);
            PointSignpostsAtTheNextBoss();
            Battle.MinionKilled += HandleMinionKilled;
            Levels.LevelledUp += HandleLevelledUp;
        }

        /// <summary>Raised when a finished combo could not do anything.</summary>
        public event Action<SummonRefusal> SummonRefused;

        /// <summary>Raised with each note the flute answers.</summary>
        public event Action<int> NotePlayed;

        /// <summary>Raised after a note that leaves the combo unfinished.</summary>
        public event Action ComboBuilding;

        /// <summary>Raised when a finished combo does what it does.</summary>
        public event Action<IComboSong> ComboPlayed;

        public event Action ComboFailed;
        public event Action PiperDied;
        public event Action Won;

        public Maze Maze { get; }
        public LabPiper Piper { get; }
        public Health PiperHealth { get; }
        public Stamina Stamina { get; }
        public ComboReader Combos { get; }
        public ComboCooldowns Cooldowns { get; }

        /// <summary>Everything the flute can play, by unlock level.</summary>
        public IReadOnlyList<IComboSong> Songs { get; }

        public AmbushWatch Ambushes { get; }
        public Signposts Signposts { get; }
        public LevelTrack Levels { get; }
        public Battle Battle { get; }

        /// <summary>Plays a note, in a fight or out of one.</summary>
        public void Press(int note)
        {
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
            Ambushes.Tick(deltaTime);
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
            Ambushes.Calm();
            Piper.MoveTo(_revivalCell);
            _hasReportedDeath = false;
        }

        private IReadOnlyList<IComboSong> PrepareSongs(LabRules rules)
        {
            foreach (CreatureType creature in rules.Creatures)
            {
                _actions[creature] = new SummonAction(Battle, creature);
            }

            foreach (BattleSong song in rules.BattleSongs)
            {
                _actions[song] = new BoostAction(Battle, song);
            }

            return rules.Creatures.Concat<IComboSong>(rules.BattleSongs).OrderBy(song => song.UnlockLevel).ToArray();
        }

        private void Apply(ComboResult result)
        {
            if (result.Outcome == ComboOutcome.Failed)
            {
                Stamina.Lose(_staminaConfig.FailedComboCost);
                ComboFailed?.Invoke();
            }
            else if (result.Outcome == ComboOutcome.Building)
            {
                ComboBuilding?.Invoke();
            }
            else if (result.Outcome == ComboOutcome.Completed)
            {
                TryPlay(result.Song);
            }
        }

        private void TryPlay(IComboSong song)
        {
            IComboAction action = _actions[song];

            if (!action.HasRoom)
            {
                SummonRefused?.Invoke(SummonRefusal.NoRoom);
            }
            else if (!Cooldowns.IsReady(song))
            {
                SummonRefused?.Invoke(SummonRefusal.CoolingDown);
            }
            else if (!Stamina.TrySpend(song.StaminaCost))
            {
                SummonRefused?.Invoke(SummonRefusal.TooTired);
            }
            else
            {
                Cooldowns.Start(song);
                action.Perform();
                ComboPlayed?.Invoke(song);
            }
        }

        // Bosses are added shallowest first and never reordered, so the first left is the next one.
        private void PointSignpostsAtTheNextBoss()
        {
            foreach (Minion minion in Battle.Minions)
            {
                if (minion.IsBoss)
                {
                    Signposts.PointTo(Maze.CellAt(minion.Position));
                    return;
                }
            }

            Signposts.Clear();
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
                PointSignpostsAtTheNextBoss();
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
