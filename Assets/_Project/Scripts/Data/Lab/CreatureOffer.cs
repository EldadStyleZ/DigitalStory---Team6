using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>A creature and the buff song it brings with it.</summary>
    [Serializable]
    public sealed class CreatureOffer
    {
        [SerializeField, Tooltip("The creature.")]
        private CreatureType _creature;

        [SerializeField, Tooltip("The song that comes with it.")]
        private BattleSong _song;

        public CreatureOffer(CreatureType creature, BattleSong song)
        {
            _creature = creature;
            _song = song;
        }

        public CreatureType Creature => _creature;
        public BattleSong Song => _song;
    }
}
