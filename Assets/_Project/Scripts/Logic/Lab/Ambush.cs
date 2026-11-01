using System;
using System.Collections.Generic;
using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>Enemies that do not exist yet: a group hidden in the maze until the Piper comes close.</summary>
    public sealed class Ambush
    {
        public Ambush(int cell, IReadOnlyList<MinionType> members)
        {
            Cell = cell;
            Members = members ?? throw new ArgumentNullException(nameof(members));
        }

        public int Cell { get; }
        public IReadOnlyList<MinionType> Members { get; }
        public AmbushState State { get; private set; }

        /// <summary>The Piper has come close, and the wait before the warning begins.</summary>
        public void Stir() => State = AmbushState.Waiting;

        public void Warn() => State = AmbushState.Warning;

        public void Spring() => State = AmbushState.Sprung;

        /// <summary>Puts it back to sleep, unless its enemies are already out.</summary>
        public void Calm()
        {
            if (State != AmbushState.Sprung)
            {
                State = AmbushState.Dormant;
            }
        }
    }
}
