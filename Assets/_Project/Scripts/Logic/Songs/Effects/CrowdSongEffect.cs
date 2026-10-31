using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>A song that does something to the rats around the Piper, nearest first.</summary>
    public abstract class CrowdSongEffect : SongEffect
    {
        private const float RingReach = 340f;
        private const float RingSpeed = 520f;

        private static readonly Color RingColor = new Color32(0xFF, 0x78, 0xDC, 0xFF);

        protected CrowdSongEffect(SongDefinition song, SoundCue jingle = null)
            : base(song, new SongFlourish(RingColor, RingReach, RingSpeed), jingle)
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
