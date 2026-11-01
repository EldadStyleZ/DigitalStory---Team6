using System.Collections.Generic;

namespace DigitalStory.Logic
{
    /// <summary>Minions that wake together: if one notices, they all do.</summary>
    public sealed class MinionGang
    {
        private readonly List<Minion> _members = new List<Minion>();

        public IReadOnlyList<Minion> Members => _members;

        public void Add(Minion minion) => _members.Add(minion);
    }
}
