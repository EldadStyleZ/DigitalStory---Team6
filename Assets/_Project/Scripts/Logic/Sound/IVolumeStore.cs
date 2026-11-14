namespace DigitalStory.Logic
{
    /// <summary>Where the volume settings are kept between sessions, so tests can keep them in memory instead of PlayerPrefs.</summary>
    public interface IVolumeStore
    {
        /// <summary>Reads a whole number kept under the key; false when none is kept.</summary>
        bool TryGet(string key, out int value);

        /// <summary>Keeps a whole number under the key.</summary>
        void Set(string key, int value);

        /// <summary>Writes what was kept to lasting storage.</summary>
        void Save();
    }
}
