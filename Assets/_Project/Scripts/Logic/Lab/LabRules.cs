using System;
using System.Collections.Generic;
using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>Everything the lab's rules read.</summary>
    public sealed class LabRules
    {
        public LabRules(
            MazeShape mazeShape,
            int mazeSeed,
            LabPlayerConfig player,
            StaminaConfig stamina,
            ComboConfig combos,
            LevelConfig levels,
            BattleConfig battle,
            IReadOnlyList<CreatureType> creatures,
            IReadOnlyList<BattleSong> battleSongs,
            PopulationPlan population,
            AmbushConfig ambush,
            float signpostChance)
        {
            MazeShape = mazeShape;
            MazeSeed = mazeSeed;
            Player = player != null ? player : throw new ArgumentNullException(nameof(player));
            Stamina = stamina != null ? stamina : throw new ArgumentNullException(nameof(stamina));
            Combos = combos != null ? combos : throw new ArgumentNullException(nameof(combos));
            Levels = levels != null ? levels : throw new ArgumentNullException(nameof(levels));
            Battle = battle != null ? battle : throw new ArgumentNullException(nameof(battle));
            Creatures = creatures ?? throw new ArgumentNullException(nameof(creatures));
            BattleSongs = battleSongs ?? throw new ArgumentNullException(nameof(battleSongs));
            Population = population;
            Ambush = ambush != null ? ambush : throw new ArgumentNullException(nameof(ambush));
            SignpostChance = signpostChance;
        }

        public MazeShape MazeShape { get; }
        public int MazeSeed { get; }
        public LabPlayerConfig Player { get; }
        public StaminaConfig Stamina { get; }
        public ComboConfig Combos { get; }
        public LevelConfig Levels { get; }
        public BattleConfig Battle { get; }
        public IReadOnlyList<CreatureType> Creatures { get; }
        public IReadOnlyList<BattleSong> BattleSongs { get; }
        public PopulationPlan Population { get; }
        public AmbushConfig Ambush { get; }

        /// <summary>The chance that a junction has an arrow on its floor.</summary>
        public float SignpostChance { get; }
    }
}
