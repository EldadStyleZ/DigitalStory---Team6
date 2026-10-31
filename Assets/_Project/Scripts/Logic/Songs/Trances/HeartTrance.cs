using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>The rat takes its place in a heart around the Piper.</summary>
    public sealed class HeartTrance : RatTrance
    {
        private const float FullTurn = Mathf.PI * 2f;

        // The classic heart curve, which the prototype uses.
        private const float WidthWave = 16f;
        private const float FirstHeightWave = 13f;
        private const float SecondHeightWave = 5f;
        private const float ThirdHeightWave = 2f;
        private const float FourthHeightWave = 1f;

        private readonly int _ratCount;
        private readonly float _scale;

        /// <param name="ratCount">How many rats share the heart.</param>
        /// <param name="scale">Town units per step of the heart curve.</param>
        /// <param name="stopDistance">How close to its place a rat starts to slow down.</param>
        public HeartTrance(int ratCount, float scale, float stopDistance)
        {
            _ratCount = ratCount;
            _scale = scale;
            StopDistance = stopDistance;
        }

        public override float StopDistance { get; }

        public override Vector2 Target(in Rat rat, Vector2 piperPosition)
        {
            float angle = rat.TranceSlot * FullTurn / _ratCount;
            float sine = Mathf.Sin(angle);
            float across = WidthWave * sine * sine * sine;
            float up = (FirstHeightWave * Mathf.Cos(angle))
                - (SecondHeightWave * Mathf.Cos(2f * angle))
                - (ThirdHeightWave * Mathf.Cos(3f * angle))
                - (FourthHeightWave * Mathf.Cos(4f * angle));

            // Town space points down, so a higher place on the heart is a smaller y.
            return piperPosition + (new Vector2(across, -up) * _scale);
        }
    }
}
