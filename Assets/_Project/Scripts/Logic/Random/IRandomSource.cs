namespace DigitalStory.Logic
{
    /// <summary>Supplies random numbers, so tests can control them.</summary>
    public interface IRandomSource
    {
        /// <summary>Returns a number from 0 up to, but not including, 1.</summary>
        double NextDouble();
    }
}
