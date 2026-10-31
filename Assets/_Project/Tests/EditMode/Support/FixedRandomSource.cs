using DigitalStory.Logic;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>Always returns the same value, so a test's result does not depend on luck.</summary>
    internal sealed class FixedRandomSource : IRandomSource
    {
        // Wander turns by (value - 0.5), so this value keeps rats on a straight heading.
        private const double NoTurn = 0.5d;

        private readonly double _value;

        public FixedRandomSource(double value = NoTurn) => _value = value;

        public double NextDouble() => _value;
    }
}
