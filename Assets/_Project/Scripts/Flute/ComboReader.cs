using System.Collections.Generic;

namespace PiperToy
{
    /// <summary>
    /// Remembers the last notes the flute played and finds the song they spell. A song counts only when all its
    /// notes come within a few seconds of each other.
    /// </summary>
    /// <remarks>
    /// This is a plain C# class, not a component, because it needs no GameObject: the Flute creates one with
    /// <c>new</c>. That also lets a test check it without a scene.
    /// </remarks>
    public sealed class ComboReader
    {
        private readonly float _windowSeconds;
        private readonly List<int> _keys = new List<int>();
        private readonly List<float> _times = new List<float>();

        public ComboReader(float windowSeconds)
        {
            _windowSeconds = windowSeconds;
        }

        /// <summary>The notes still remembered, oldest first.</summary>
        public IReadOnlyList<int> Keys => _keys;

        /// <summary>Remembers a note, and forgets any played too long before it.</summary>
        public void Add(int key, float time)
        {
            while (_times.Count > 0 && time - _times[0] > _windowSeconds)
            {
                _keys.RemoveAt(0);
                _times.RemoveAt(0);
            }

            _keys.Add(key);
            _times.Add(time);
        }

        /// <summary>
        /// The song whose keys are the last notes played, or null. When two songs fit, the longer one wins,
        /// because the shorter one is only its ending.
        /// </summary>
        public Song FindSong(IReadOnlyList<Song> songs)
        {
            Song best = null;

            foreach (Song song in songs)
            {
                bool isLonger = best == null || song.Keys.Count > best.Keys.Count;

                if (isLonger && EndsWith(song.Keys))
                {
                    best = song;
                }
            }

            return best;
        }

        /// <summary>Forgets every note, so the next song starts from scratch.</summary>
        public void Clear()
        {
            _keys.Clear();
            _times.Clear();
        }

        private bool EndsWith(IReadOnlyList<int> songKeys)
        {
            int start = _keys.Count - songKeys.Count;

            if (songKeys.Count == 0 || start < 0)
            {
                return false;
            }

            for (int index = 0; index < songKeys.Count; index++)
            {
                if (_keys[start + index] != songKeys[index])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
