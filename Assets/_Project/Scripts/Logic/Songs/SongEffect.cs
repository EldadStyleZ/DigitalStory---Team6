using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>One song, saved as an asset: how to play it and what it does.</summary>
    /// <remarks>
    /// It holds a rule, so it lives in Logic even though it is a ScriptableObject. Each song is a
    /// class of its own, so a new song plugs in without changing anything else.
    /// </remarks>
    public abstract class SongEffect : ScriptableObject
    {
        [SerializeField, Tooltip("The song's name, keys and cooldown.")]
        private SongDefinition _song;

        protected SongEffect(SongDefinition song) => _song = song;

        public SongDefinition Song => _song;

        /// <summary>Applies the song. Keep runtime state in the rules, never on this asset.</summary>
        public abstract void Cast(in SongContext context);
    }
}
