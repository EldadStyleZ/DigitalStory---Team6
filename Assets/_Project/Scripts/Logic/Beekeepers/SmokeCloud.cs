using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>One smoke cloud on the ground. Owned and pooled by <see cref="SmokeField"/>.</summary>
    public sealed class SmokeCloud
    {
        /// <summary>Where the cloud sits (y = 0).</summary>
        public Vector3 Position { get; private set; }

        /// <summary>How far the cloud reaches, in metres.</summary>
        public float Radius { get; private set; }

        /// <summary>Seconds until the cloud fades away.</summary>
        public float SecondsLeft { get; private set; }

        /// <summary>True while the field has this cloud on the ground.</summary>
        internal bool IsActive { get; private set; }

        /// <summary>Seconds of this tick during which the cloud was on the ground.</summary>
        internal float StepSeconds { get; set; }

        /// <summary>Seconds of this tick during which the cloud still hurt things (never past its life).</summary>
        internal float EffectSeconds { get; set; }

        internal void Form(Vector3 position, float radius, float life)
        {
            Position = new Vector3(position.x, 0f, position.z);
            Radius = radius;
            SecondsLeft = life;
            StepSeconds = 0f;
            EffectSeconds = 0f;
            IsActive = true;
        }

        internal void Age(float seconds)
        {
            SecondsLeft -= seconds;
        }

        internal void Deactivate()
        {
            IsActive = false;
        }

        internal bool Contains(Vector3 point)
        {
            float dx = point.x - Position.x;
            float dz = point.z - Position.z;
            return dx * dx + dz * dz <= Radius * Radius;
        }
    }
}
