namespace DigitalStory.Logic
{
    /// <summary>The speeds of a controller's two motors, 0 to 1: the heavy low one and the light high one.</summary>
    public readonly struct MotorSpeeds
    {
        /// <summary>Makes the pair.</summary>
        public MotorSpeeds(float low, float high)
        {
            Low = low;
            High = high;
        }

        /// <summary>The heavy, low-frequency motor's speed, 0 to 1.</summary>
        public float Low { get; }

        /// <summary>The light, high-frequency motor's speed, 0 to 1.</summary>
        public float High { get; }

        /// <summary>True when both motors are off.</summary>
        public bool IsStill => !(Low > 0f) && !(High > 0f);
    }
}
