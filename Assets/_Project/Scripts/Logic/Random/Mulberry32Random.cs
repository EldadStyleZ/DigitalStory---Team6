namespace DigitalStory.Logic
{
    /// <summary>A small seeded generator, so the same seed always gives the same town.</summary>
    /// <remarks>Mulberry32 by Tommy Ettinger, released into the public domain.</remarks>
    public sealed class Mulberry32Random : IRandomSource
    {
        private const uint Increment = 0x6D2B79F5u;
        private const double Range = 4294967296d;

        private uint _state;

        public Mulberry32Random(uint seed) => _state = seed;

        public double NextDouble()
        {
            unchecked
            {
                _state += Increment;
                uint mixed = (_state ^ (_state >> 15)) * (_state | 1u);
                mixed = (mixed + ((mixed ^ (mixed >> 7)) * (mixed | 61u))) ^ mixed;
                return (mixed ^ (mixed >> 14)) / Range;
            }
        }
    }
}
