using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Chooses what an aimed song strikes, at the moment it is cast, by the song's own aim rule.</summary>
    public interface ISongAim
    {
        /// <summary>
        /// Chooses a target by <paramref name="rule"/> within <paramref name="range"/> metres of the core. The mouse wins
        /// while it is live. <paramref name="target"/> is the chosen beekeeper, or null when there is none; then
        /// <paramref name="point"/> is where the song strikes: the mouse's ground point, or the ground ahead of the core.
        /// </summary>
        void Choose(AimRule rule, float range, out ISwarmTarget target, out Vector3 point);

        /// <summary>
        /// Chooses several targets by the <see cref="AimRule.ThreeNearest"/> rule, for a song that strikes one each (the
        /// Hydra's heads): the living beekeepers within <paramref name="range"/> metres of the core that stand nearest the
        /// core, or nearest the mouse's ground point while the mouse is live, nearest first. Writes up to
        /// <paramref name="count"/> of them, and no more than <paramref name="targets"/> holds, into
        /// <paramref name="targets"/> from index 0, and returns how many it wrote. <paramref name="point"/> is the aim, as
        /// <see cref="Choose"/> gives it.
        /// </summary>
        int ChooseNearest(float range, int count, ISwarmTarget[] targets, out Vector3 point);

        /// <summary>
        /// Chooses several different beekeepers by the <see cref="AimRule.Threat"/> rule, for a song that strikes one with each
        /// of its parts (the Sting's stings, plan section O): the living beekeepers within <paramref name="range"/> metres of
        /// the core, most threatening first, the one <see cref="Choose"/> would pick by that rule first of all, so the first
        /// agrees with the aim marker and the mouse. Writes up to <paramref name="count"/> of them, and no more than
        /// <paramref name="targets"/> holds, into <paramref name="targets"/> from index 0, and returns how many it wrote.
        /// <paramref name="point"/> is the aim, as <see cref="Choose"/> gives it.
        /// </summary>
        int ChooseThreats(float range, int count, ISwarmTarget[] targets, out Vector3 point);
    }
}
