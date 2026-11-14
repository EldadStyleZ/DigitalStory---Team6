using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Keeps the volume settings (plan section AC) in PlayerPrefs, so they survive a restart of the game.</summary>
    public sealed class PlayerPrefsVolumeStore : IVolumeStore
    {
        /// <inheritdoc/>
        public bool TryGet(string key, out int value)
        {
            if (PlayerPrefs.HasKey(key))
            {
                value = PlayerPrefs.GetInt(key);
                return true;
            }

            value = 0;
            return false;
        }

        /// <inheritdoc/>
        public void Set(string key, int value) => PlayerPrefs.SetInt(key, value);

        /// <inheritdoc/>
        public void Save() => PlayerPrefs.Save();
    }
}
