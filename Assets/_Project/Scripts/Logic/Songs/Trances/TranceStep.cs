using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>What a trance needs to move a rat for one step.</summary>
    public readonly struct TranceStep
    {
        public TranceStep(float deltaTime, float time, TownConfig town, HouseCollider houses, float ratRadius)
        {
            DeltaTime = deltaTime;
            Time = time;
            Town = town;
            Houses = houses;
            RatRadius = ratRadius;
        }

        public float DeltaTime { get; }

        /// <summary>Seconds of game time since the night began.</summary>
        public float Time { get; }

        public TownConfig Town { get; }
        public HouseCollider Houses { get; }
        public float RatRadius { get; }
    }
}
