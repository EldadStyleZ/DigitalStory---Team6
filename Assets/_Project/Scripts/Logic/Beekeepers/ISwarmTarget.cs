using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Something the swarm can hurt, push and stun, such as a beekeeper.</summary>
    public interface ISwarmTarget
    {
        /// <summary>Where it stands, in metres.</summary>
        Vector3 Position { get; }

        /// <summary>False once it is dead.</summary>
        bool IsAlive { get; }

        /// <summary>Takes away health.</summary>
        void TakeDamage(float amount);

        /// <summary>Shoves it, in metres per second. The push decays quickly.</summary>
        void Knockback(Vector3 velocity);

        /// <summary>Freezes it for the given seconds.</summary>
        void Stun(float seconds);
    }
}
