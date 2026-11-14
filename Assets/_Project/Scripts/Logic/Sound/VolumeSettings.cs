using System;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The player's three volumes (plan section AC): Master, Music and Effects, each 0 to 100%. A group's gain is the master's
    /// gain times its own, each through <see cref="VolumeCurve"/>. They load from and save to a store that comes in, so the
    /// game keeps them in PlayerPrefs and tests in memory. A percentage out of range is held to 0 to 100.
    /// </summary>
    public sealed class VolumeSettings
    {
        /// <summary>The store's key of each group, in the order of <see cref="VolumeGroup"/>.</summary>
        public static readonly string[] Keys = { "WaspPiper.Volume.Master", "WaspPiper.Volume.Music", "WaspPiper.Volume.Effects" };

        private readonly int[] _defaults;
        private readonly int[] _percents;

        /// <param name="masterPercent">The master volume before anything is saved, 0 to 100.</param>
        /// <param name="musicPercent">The music volume before anything is saved, 0 to 100.</param>
        /// <param name="effectsPercent">The effects volume before anything is saved, 0 to 100.</param>
        public VolumeSettings(int masterPercent, int musicPercent, int effectsPercent)
        {
            _defaults = new[] { VolumeCurve.Clamp(masterPercent), VolumeCurve.Clamp(musicPercent), VolumeCurve.Clamp(effectsPercent) };
            _percents = (int[])_defaults.Clone();
        }

        /// <summary>Raised with the group whose percentage changed.</summary>
        public event Action<VolumeGroup> Changed;

        /// <summary>A group's percentage, 0 to 100.</summary>
        public int PercentOf(VolumeGroup group) => _percents[(int)group];

        /// <summary>
        /// The gain to play a group at, 0 to 1: the master's curve times the group's own. The master's own gain is its curve
        /// alone.
        /// </summary>
        public float GainOf(VolumeGroup group)
        {
            float master = VolumeCurve.GainOf(_percents[(int)VolumeGroup.Master]);
            return group == VolumeGroup.Master ? master : master * VolumeCurve.GainOf(_percents[(int)group]);
        }

        /// <summary>Sets a group's percentage, held to 0 to 100. Returns true and raises <see cref="Changed"/> when it changed.</summary>
        public bool Set(VolumeGroup group, int percent)
        {
            int value = VolumeCurve.Clamp(percent);
            if (_percents[(int)group] == value)
            {
                return false;
            }

            _percents[(int)group] = value;
            Changed?.Invoke(group);
            return true;
        }

        /// <summary>Moves a group's percentage by <paramref name="step"/>, held to 0 to 100. Returns true when it changed.</summary>
        public bool Step(VolumeGroup group, int step) => Set(group, _percents[(int)group] + step);

        /// <summary>
        /// Takes each group's percentage from the store, held to 0 to 100, or its default when the store has none. Raises
        /// <see cref="Changed"/> for each group that changed.
        /// </summary>
        /// <exception cref="ArgumentNullException">The store is missing.</exception>
        public void Load(IVolumeStore store)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            for (int group = 0; group < _percents.Length; group++)
            {
                Set((VolumeGroup)group, store.TryGet(Keys[group], out int value) ? value : _defaults[group]);
            }
        }

        /// <summary>Keeps every group's percentage in the store and writes it out.</summary>
        /// <exception cref="ArgumentNullException">The store is missing.</exception>
        public void Save(IVolumeStore store)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            for (int group = 0; group < _percents.Length; group++)
            {
                store.Set(Keys[group], _percents[group]);
            }

            store.Save();
        }
    }
}
