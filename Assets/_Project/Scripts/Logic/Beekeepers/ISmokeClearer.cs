using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Something that can clear smoke clouds, such as the Gust song.</summary>
    public interface ISmokeClearer
    {
        /// <summary>Clears every cloud whose centre lies within <paramref name="radius"/> metres of <paramref name="center"/> (y ignored).</summary>
        /// <returns>How many clouds were cleared.</returns>
        int ClearWithin(Vector3 center, float radius);
    }
}
