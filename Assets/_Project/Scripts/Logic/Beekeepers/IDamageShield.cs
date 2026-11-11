namespace DigitalStory.Logic
{
    /// <summary>
    /// How much of the beekeepers' harm gets through the songs that run now: the share of each wasp loss that reaches the
    /// swarm, and the share of damage that reaches each hive. 1 lets everything through.
    /// </summary>
    public interface IDamageShield
    {
        /// <summary>The share, from 0 to 1, of each wasp loss (a hit on the core, or smoke) that reaches the swarm.</summary>
        float WaspLossFraction { get; }

        /// <summary>The share, from 0 to 1, of the damage from sticks and smoke that reaches hive number <paramref name="hive"/>.</summary>
        float HiveDamageFraction(int hive);
    }
}
