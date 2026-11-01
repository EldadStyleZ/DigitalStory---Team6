using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>An enemy in the maze, which sleeps until something comes close.</summary>
    public sealed class Minion : Fighter
    {
        private readonly MinionRank _rank;

        public Minion(MinionType type, Vector2 position, MinionRank rank, MinionGang gang = null)
            : base(type.Stats, type.Look, position)
        {
            Type = type;
            _rank = rank;
            Gang = gang;
        }

        public MinionType Type { get; }

        /// <summary>The group it wakes with, if any.</summary>
        public MinionGang Gang { get; }

        public bool IsAwake { get; private set; }
        public bool IsBoss => _rank != MinionRank.Ordinary;
        public bool IsFinalBoss => _rank == MinionRank.FinalBoss;

        public void Wake() => IsAwake = true;

        public void Calm() => IsAwake = false;
    }
}
