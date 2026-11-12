using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>One swarm song, saved as an asset: how to play it, what it costs and what it does.</summary>
    /// <remarks>Like <see cref="SongEffect"/>, but for the wasp swarm. Keep runtime state in the rules, never on the asset.</remarks>
    public abstract class SwarmSongEffect : ScriptableObject
    {
        [SerializeField, Tooltip("The song's name, keys and cooldown.")]
        private SongDefinition _song;

        [SerializeField, Tooltip("What the song is for. The wasps take its colour while the song's form is active.")]
        private SongCategory _category;

        [SerializeField, Min(0), Tooltip("Wasps the song costs, paid when it is cast in a survive phase, and in a farm phase too for a tower. A song's first cast in a match is free, and a cast that would take the swarm below its collapse line is refused.")]
        private int _waspCost;

        [SerializeField, Tooltip("True when the player knows the song at the start of a match.")]
        private bool _knownFromStart;

        [SerializeField, Tooltip("True when the song's notes are shown the first time Fury fills, although the song is not yet known.")]
        private bool _revealedWhenFuryFills;

        /// <summary>Sets the song's keys, category and price. Pass the plan's numbers as the defaults.</summary>
        protected SwarmSongEffect(SongDefinition song, SongCategory category, int waspCost, bool knownFromStart = false, bool revealedWhenFuryFills = false)
        {
            _song = song;
            _category = category;
            _waspCost = waspCost;
            _knownFromStart = knownFromStart;
            _revealedWhenFuryFills = revealedWhenFuryFills;
        }

        /// <summary>The song's name, keys and cooldown.</summary>
        public SongDefinition Song => _song;

        /// <summary>What the song is for: Attack, Defense or Power.</summary>
        public SongCategory Category => _category;

        /// <summary>
        /// Wasps the song costs, paid when it is cast in a survive phase, and in a farm phase too while
        /// <see cref="CostsInFarmPhase"/> (plan section M). <see cref="SwarmSongCaster.Price"/> says what a cast costs now:
        /// a song's first cast in a match is free.
        /// </summary>
        public int WaspCost => _waspCost;

        /// <summary>
        /// What one cast costs when <paramref name="livingWasps"/> are alive as its notes complete: its
        /// <see cref="WaspCost"/>. Only the Sting reads the count, by the owner's decision (plan section O): it sends one
        /// sting for every 400 living wasps and costs its price for each.
        /// </summary>
        public virtual int WaspCostFor(int livingWasps)
        {
            return WaspCost;
        }

        /// <summary>
        /// True for a tower, a song whose wasps stay on the map: it costs its price in a farm phase too, where other songs are
        /// free. False unless a song says otherwise.
        /// </summary>
        public virtual bool CostsInFarmPhase => false;

        /// <summary>True when the player knows the song at the start of a match.</summary>
        public bool KnownFromStart => _knownFromStart;

        /// <summary>True when the song's notes are shown the first time Fury fills, although the song is not yet known.</summary>
        public bool RevealedWhenFuryFills => _revealedWhenFuryFills;

        /// <summary>
        /// Whether the song can be cast now. When it cannot, the caster refuses it with <see cref="SongRefusal.NeedsFury"/>,
        /// the one reason a song gives today (the Colossus without full Fury), and the notes are still used up.
        /// </summary>
        public virtual bool CanCast(in SwarmSongContext context) => true;

        /// <summary>Applies the song.</summary>
        public abstract void Cast(in SwarmSongContext context);
    }
}
