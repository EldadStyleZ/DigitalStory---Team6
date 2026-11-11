using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>A beekeeper stand-in that only records what was done to it. It stays alive until a test says otherwise.</summary>
    internal sealed class FakeSwarmTarget : ISwarmTarget
    {
        public Vector3 Position { get; set; }
        public bool IsAlive { get; set; } = true;
        public float DamageTaken { get; private set; }
        public Vector3 LastKnockback { get; private set; }
        public float LastStunSeconds { get; private set; }

        public void TakeDamage(float amount)
        {
            DamageTaken += amount;
        }

        public void Knockback(Vector3 velocity)
        {
            LastKnockback = velocity;
        }

        public void Stun(float seconds)
        {
            LastStunSeconds = seconds;
        }
    }
}
