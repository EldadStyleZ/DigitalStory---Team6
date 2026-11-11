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

        protected SwarmSongEffect(SongDefinition song)
        {
            _song = song;
        }

        /// <summary>The song's name, keys and cooldown.</summary>
        public SongDefinition Song => _song;

        /// <summary>Whether the song can be cast now. A song that cannot be cast still uses up the notes.</summary>
        public virtual bool CanCast(in SwarmSongContext context) => true;

        /// <summary>Applies the song.</summary>
        public abstract void Cast(in SwarmSongContext context);
    }
}
