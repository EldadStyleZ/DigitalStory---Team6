using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>An enemy in the maze, which sleeps until something comes close.</summary>
    public sealed class Minion : Fighter
    {
        private readonly MinionRank _rank;

        public Minion(MinionType type, Vector2 position, MinionRank rank)
            : base(type.Stats, type.Look, position)
        {
            Type = type;
            _rank = rank;
        }

        public MinionType Type { get; }
        public bool IsAwake { get; private set; }
        public bool IsBoss => _rank != MinionRank.Ordinary;
        public bool IsFinalBoss => _rank == MinionRank.FinalBoss;

        public void Wake() => IsAwake = true;

        public void Calm() => IsAwake = false;
    }
}
