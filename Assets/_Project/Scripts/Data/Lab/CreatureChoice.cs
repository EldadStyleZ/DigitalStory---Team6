using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// Two creatures to pick between at one level. The other is gone for the run, so both must answer the
    /// same problem, or a run could reach an enemy it has no answer to. Both options share their combos,
    /// so the fingers learn the same notes whichever is picked.
    /// </summary>
    [Serializable]
    public sealed class CreatureChoice
    {
        [SerializeField, Min(2), Tooltip("The level that brings this choice.")]
        private int _level;

        [SerializeField, Tooltip("The problem both options answer, for the screen.")]
        private string _problem;

        [SerializeField, Tooltip("The two options.")]
        private CreatureOffer[] _options;

        public CreatureChoice(int level, string problem, CreatureOffer first, CreatureOffer second)
        {
            _level = level;
            _problem = problem;
            _options = new[] { first, second };
        }

        public int Level => _level;
        public string Problem => _problem;
        public IReadOnlyList<CreatureOffer> Options => _options;
    }
}
