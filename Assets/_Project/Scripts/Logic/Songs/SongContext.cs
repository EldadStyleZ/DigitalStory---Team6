using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>What a song can change when it is cast.</summary>
    public readonly struct SongContext
    {
        public SongContext(Vector2 piperPosition, RatSwarm swarm, CatPack cats, IRandomSource random)
        {
            PiperPosition = piperPosition;
            Swarm = swarm;
            Cats = cats;
            Random = random;
        }

        public Vector2 PiperPosition { get; }
        public RatSwarm Swarm { get; }
        public CatPack Cats { get; }
        public IRandomSource Random { get; }
    }
}
