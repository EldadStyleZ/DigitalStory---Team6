using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>One swarm song, saved as an asset: how to play it and what it does.</summary>
    /// <remarks>Like <see cref="SongEffect"/>, but for the wasp swarm. Keep runtime state in the rules, never on the asset.</remarks>
    public abstract class SwarmSongEffect : ScriptableObject
    {
        [SerializeField, Tooltip("The song's name, keys and cooldown.")]
        private SongDefinition _song;

        [SerializeField, Tooltip("What the song is for. The wasps take its colour while the song's form is active.")]
        private SongCategory _category;

        [SerializeField, Tooltip("True when the player knows the song at the start of a match.")]
        private bool _knownFromStart;

        [SerializeField, Tooltip("True when the song's notes are shown the first time Fury fills, although the song is not yet known.")]
        private bool _revealedWhenFuryFills;

        protected SwarmSongEffect(SongDefinition song, SongCategory category, bool knownFromStart = false, bool revealedWhenFuryFills = false)
        {
            _song = song;
            _category = category;
            _knownFromStart = knownFromStart;
            _revealedWhenFuryFills = revealedWhenFuryFills;
        }

        /// <summary>The song's name, keys and cooldown.</summary>
        public SongDefinition Song => _song;

        /// <summary>What the song is for: Attack, Defense or Power.</summary>
        public SongCategory Category => _category;

        /// <summary>True when the player knows the song at the start of a match.</summary>
        public bool KnownFromStart => _knownFromStart;

        /// <summary>True when the song's notes are shown the first time Fury fills, although the song is not yet known.</summary>
        public bool RevealedWhenFuryFills => _revealedWhenFuryFills;

        /// <summary>Whether the song can be cast now. A song that cannot be cast still uses up the notes.</summary>
        public virtual bool CanCast(in SwarmSongContext context) => true;

        /// <summary>Applies the song.</summary>
        public abstract void Cast(in SwarmSongContext context);
    }
}
