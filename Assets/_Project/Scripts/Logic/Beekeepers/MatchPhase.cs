namespace DigitalStory.Logic
{
    /// <summary>The two halves of a round (plan section L).</summary>
    public enum MatchPhase
    {
        /// <summary>A timed phase with no beekeeper on the map: songs cost nothing, and the player gathers wasps and heals hives.</summary>
        Farm = 0,

        /// <summary>The round's wave. It ends when every beekeeper of the wave is dead.</summary>
        Survive = 1
    }
}
