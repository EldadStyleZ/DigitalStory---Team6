using System;
using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>One run through the lab's maze, and the only way the view changes it.</summary>
    public sealed class LabSession
    {
        private readonly StaminaConfig _staminaConfig;
        private readonly Dictionary<IComboSong, IComboAction> _actions = new Dictionary<IComboSong, IComboAction>();
        private readonly List<IComboSong> _songs = new List<IComboSong>();
        private readonly List<CreatureOffer> _party = new List<CreatureOffer>();
        private readonly Queue<CreatureChoice> _waiting = new Queue<CreatureChoice>();
        private readonly IReadOnlyList<CreatureChoice> _choices;

        private int _revivalCell;
        private bool _hasReportedDeath;

        public LabSession(LabRules rules)
        {
            _ = rules ?? throw new ArgumentNullException(nameof(rules));
            _staminaConfig = rules.Stamina;
            _choices = rules.Choices;

            var random = new Mulberry32Random((uint)rules.MazeSeed);
            Maze = MazeGenerator.Generate(rules.MazeShape, random);
            Piper = new LabPiper(rules.Player, Maze);
            PiperHealth = new Health(rules.Player.MaxHealth);
            Stamina = new Stamina(rules.Stamina);
            Levels = new LevelTrack(rules.Levels);
            Battle = new Battle(Maze, rules.Battle, Piper, PiperHealth);
            AllSongs = PrepareSongs(rules);
            Combos = new ComboReader(rules.Combos, Array.Empty<IComboSong>());
            Cooldowns = new ComboCooldowns(AllSongs);
            Learn(rules.Starter);

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

        /// <summary>Raised with the option picked.</summary>
        public event Action<CreatureOffer> ChoiceMade;

        public event Action ComboFailed;
        public event Action PiperDied;
        public event Action Won;

        public Maze Maze { get; }
        public LabPiper Piper { get; }
        public Health PiperHealth { get; }
        public Stamina Stamina { get; }
        public ComboReader Combos { get; }
        public ComboCooldowns Cooldowns { get; }

        /// <summary>Everything the flute can play in this run so far, by unlock level.</summary>
        public IReadOnlyList<IComboSong> Songs => _songs;

        /// <summary>Everything any run could play, picked or not.</summary>
        public IReadOnlyList<IComboSong> AllSongs { get; }

        /// <summary>The starter and every creature picked so far, each with its song.</summary>
        public IReadOnlyList<CreatureOffer> Party => _party;

        /// <summary>The choice waiting for a pick, if any.</summary>
        public CreatureChoice WaitingChoice => _waiting.Count > 0 ? _waiting.Peek() : null;

        /// <summary>The next choice still to come, if any.</summary>
        public CreatureChoice NextChoice
        {
            get
            {
                foreach (CreatureChoice choice in _choices)
                {
                    if (choice.Level > Levels.Level)
                    {
                        return choice;
                    }
                }

                return null;
            }
        }

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

        /// <summary>Picks an option of the waiting choice. The other never returns.</summary>
        public void Choose(int option)
        {
            CreatureChoice choice = WaitingChoice ?? throw new InvalidOperationException("No choice is waiting.");

            if (option < 0 || option >= choice.Options.Count)
            {
                throw new IndexOutOfRangeException($"{choice.Problem} has no option {option}.");
            }

            CreatureOffer offer = choice.Options[option];
            _waiting.Dequeue();
            Learn(offer);
            Combos.Clear();
            ChoiceMade?.Invoke(offer);
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
            var all = new List<IComboSong>();
            Prepare(rules.Starter, all);

            foreach (CreatureChoice choice in rules.Choices)
            {
                foreach (CreatureOffer option in choice.Options)
                {
                    Prepare(option, all);
                }
            }

            return all;
        }

        private void Prepare(CreatureOffer offer, List<IComboSong> all)
        {
            _actions[offer.Creature] = new SummonAction(Battle, offer.Creature);
            _actions[offer.Song] = new SongAction(Battle, offer.Song);
            all.Add(offer.Creature);
            all.Add(offer.Song);
        }

        private void Learn(CreatureOffer offer)
        {
            _party.Add(offer);
            _songs.Add(offer.Creature);
            _songs.Add(offer.Song);
            Combos.Learn(offer.Creature);
            Combos.Learn(offer.Song);
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

            foreach (CreatureChoice choice in _choices)
            {
                if (choice.Level == level)
                {
                    _waiting.Enqueue(choice);
                }
            }
        }
    }
}
