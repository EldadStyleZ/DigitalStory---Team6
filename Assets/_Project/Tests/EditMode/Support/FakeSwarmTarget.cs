using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// A beekeeper stand-in that only records what was done to it. It stays alive until a test says otherwise, or until it
    /// has taken <see cref="DiesAfter"/> damage.
    /// </summary>
    internal sealed class FakeSwarmTarget : ISwarmTarget
    {
        public Vector3 Position { get; set; }
        public bool IsAlive { get; set; } = true;
        public float DamageTaken { get; private set; }
        public Vector3 LastKnockback { get; private set; }
        public int KnockbackCount { get; private set; }
        public float LastStunSeconds { get; private set; }
        public int StunCount { get; private set; }

        /// <summary>The damage that kills it. Infinity keeps it alive.</summary>
        public float DiesAfter { get; set; } = float.PositiveInfinity;

        public void TakeDamage(float amount)
        {
            DamageTaken += amount;
            if (DamageTaken >= DiesAfter)
            {
                IsAlive = false;
            }
        }

        public void Knockback(Vector3 velocity)
        {
            LastKnockback = velocity;
            KnockbackCount++;
        }

        public void Stun(float seconds)
        {
            LastStunSeconds = seconds;
            StunCount++;
        }
    }
}
