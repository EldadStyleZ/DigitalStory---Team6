using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// A mystery box's life once it appears: it sits idle for the idle time, then flashes on and off for the flash
    /// time, then it is over and goes back to the pool.
    /// </summary>
    public sealed class MysteryBoxLife
    {
        private readonly float _idleSeconds;
        private readonly float _flashSeconds;
        private readonly float _flashesPerSecond;
        private float _age;

        /// <param name="flashesPerSecond">How many times a second it blinks off and on again while flashing.</param>
        public MysteryBoxLife(float idleSeconds, float flashSeconds, float flashesPerSecond)
        {
            _idleSeconds = Mathf.Max(0f, idleSeconds);
            _flashSeconds = Mathf.Max(0f, flashSeconds);
            _flashesPerSecond = Mathf.Max(0.01f, flashesPerSecond);
        }

        /// <summary>Whether it is in its last seconds, flashing.</summary>
        public bool IsFlashing => _age >= _idleSeconds && !IsOver;

        /// <summary>Whether its time is up.</summary>
        public bool IsOver => _age >= _idleSeconds + _flashSeconds;

        /// <summary>Whether it is drawn now: always while idle; while flashing, the first half of each blink.</summary>
        public bool IsVisible => !IsFlashing || Mathf.FloorToInt((_age - _idleSeconds) * _flashesPerSecond * 2f) % 2 == 0;

        /// <summary>Moves its clock on.</summary>
        public void Tick(float deltaTime) => _age += deltaTime;

        /// <summary>Starts its life again, as when it is spawned.</summary>
        public void Restart() => _age = 0f;
    }

    /// <summary>
    /// How long the spawner waits before the next mystery box: never less than the minimum, usually a little more,
    /// now and then a lot more, and on average the average. The extra time is exponential, so once the minimum has
    /// passed a box is as likely to come at any moment.
    /// </summary>
    public static class SpawnWait
    {
        // Keeps the highest roll finite: about nine times the average extra at most.
        private const float HighestRoll = 0.9999f;

        /// <param name="roll">A random number from 0 to 1.</param>
        public static float Next(float minimum, float average, float roll)
        {
            float extra = Mathf.Max(0f, average - minimum);
            float clamped = Mathf.Clamp(roll, 0f, HighestRoll);
            return minimum + (extra * -Mathf.Log(1f - clamped));
        }
    }

    /// <summary>The box's idle spin (around the world's up axis) and bob.</summary>
    public static class BoxSpin
    {
        /// <summary>
        /// Whether, turned this far, the box shows its back. The back is drawn flipped, so the question mark always
        /// reads the right way round and the box never looks hollow.
        /// </summary>
        public static bool ShowsBack(float degrees) => Mathf.Cos(degrees * Mathf.Deg2Rad) < 0f;

        /// <summary>How far above its resting place the box is at a time, bobbing by the height once a period.</summary>
        public static float Bob(float time, float height, float periodSeconds) =>
            periodSeconds <= 0f ? 0f : Mathf.Sin(time / periodSeconds * 2f * Mathf.PI) * height;
    }
}
