namespace DigitalStory.View
{
    /// <summary>A fast source of white noise, safe to use on the audio thread.</summary>
    public struct WhiteNoise
    {
        private const uint Seed = 2463534242u;
        private const float Scale = 2147483648f;

        private uint _state;

        public static WhiteNoise Start() => new WhiteNoise { _state = Seed };

        /// <summary>Returns the next sample, from -1 to 1.</summary>
        public float Next()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return (_state / Scale) - 1f;
        }
    }
}
