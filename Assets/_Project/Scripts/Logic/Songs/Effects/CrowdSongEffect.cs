using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>A song that does something to the rats around the Piper, nearest first.</summary>
    public abstract class CrowdSongEffect : SongEffect
    {
        protected CrowdSongEffect(SongDefinition song)
            : base(song)
        {
        }

        [field: SerializeField, Min(0f), Tooltip("How far the song reaches.")]
        public float Radius { get; private set; } = 360f;

        public sealed override void Cast(in SongContext context)
        {
            Affect(context.Swarm.GatherNear(context.PiperPosition, Radius), in context);
        }

        /// <param name="rats">The rats in reach, nearest first.</param>
        /// <param name="context">What the song can change.</param>
        protected abstract void Affect(int[] rats, in SongContext context);
    }
}
