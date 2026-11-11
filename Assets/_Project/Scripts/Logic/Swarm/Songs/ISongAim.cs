using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Chooses what an aimed song strikes, at the moment it is cast, by the song's own aim rule.</summary>
    public interface ISongAim
    {
        /// <summary>
        /// Chooses a target by <paramref name="rule"/> within <paramref name="range"/> metres of the Piper. The mouse wins
        /// while it is live. <paramref name="target"/> is the chosen beekeeper, or null when there is none; then
        /// <paramref name="point"/> is where the song strikes: the mouse's ground point, or the ground ahead of the Piper.
        /// </summary>
        void Choose(AimRule rule, float range, out ISwarmTarget target, out Vector3 point);
    }
}
