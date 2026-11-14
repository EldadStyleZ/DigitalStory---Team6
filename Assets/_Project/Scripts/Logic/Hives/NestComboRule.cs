using System;
using System.Collections.Generic;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Draws a nest's combo (plan section U): <c>shortest</c> to <c>longest</c> notes, never the same note twice in a row, so
    /// each combo reads as a little tune. No song's keys lie inside it as consecutive notes, and no song's keys hold it, so
    /// playing a combo never casts a song and never looks like one. The same random numbers draw the same combo, so a seed
    /// replays its combos.
    /// </summary>
    public static class NestComboRule
    {
        /// <summary>Random draws tried before the rule falls back to the first fitting combo in order.</summary>
        public const int MaxTries = 64;

        /// <summary>
        /// Draws one combo from <paramref name="random"/>. It allocates the combo; call it when a match starts.
        /// </summary>
        /// <param name="songs">Each song's keys, holes counting from 0.</param>
        /// <exception cref="ArgumentNullException">An argument is missing.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The lengths or the hole count make no combo possible.</exception>
        /// <exception cref="InvalidOperationException">Every combo of the allowed lengths holds or is held by a song.</exception>
        public static int[] Draw(IRandomSource random, IReadOnlyList<IReadOnlyList<int>> songs, int holeCount, int shortest, int longest)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (songs == null)
            {
                throw new ArgumentNullException(nameof(songs));
            }

            if (holeCount < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(holeCount), holeCount, "A combo needs at least two holes to change note.");
            }

            if (shortest < 1 || longest < shortest)
            {
                throw new ArgumentOutOfRangeException(nameof(shortest), shortest, "A combo needs from 1 note, and longest at least shortest.");
            }

            for (int attempt = 0; attempt < MaxTries; attempt++)
            {
                int length = shortest + Pick(random, longest - shortest + 1);
                var combo = new int[length];
                combo[0] = Pick(random, holeCount);
                for (int index = 1; index < length; index++)
                {
                    // One of the other holes, so no note follows itself.
                    int next = Pick(random, holeCount - 1);
                    combo[index] = next >= combo[index - 1] ? next + 1 : next;
                }

                if (CastsNoSong(combo, songs))
                {
                    return combo;
                }
            }

            return FirstFitting(songs, holeCount, shortest, longest);
        }

        /// <summary>
        /// True when no song's keys lie inside <paramref name="combo"/> as consecutive notes, and no song's keys hold the
        /// combo as consecutive notes. It does not allocate.
        /// </summary>
        public static bool CastsNoSong(IReadOnlyList<int> combo, IReadOnlyList<IReadOnlyList<int>> songs)
        {
            for (int song = 0; song < songs.Count; song++)
            {
                IReadOnlyList<int> keys = songs[song];
                if (keys == null || keys.Count == 0)
                {
                    continue;
                }

                if (Holds(combo, keys) || Holds(keys, combo))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>The keys of every song, in order, for <see cref="Draw"/>.</summary>
        public static IReadOnlyList<IReadOnlyList<int>> KeysOf(IReadOnlyList<SwarmSongEffect> songs)
        {
            if (songs == null)
            {
                throw new ArgumentNullException(nameof(songs));
            }

            var keys = new IReadOnlyList<int>[songs.Count];
            for (int song = 0; song < songs.Count; song++)
            {
                keys[song] = songs[song].Song.Keys;
            }

            return keys;
        }

        // True when the inner notes lie in the outer ones as a run of consecutive notes.
        private static bool Holds(IReadOnlyList<int> outer, IReadOnlyList<int> inner)
        {
            for (int start = 0; start + inner.Count <= outer.Count; start++)
            {
                bool matches = true;
                for (int index = 0; index < inner.Count && matches; index++)
                {
                    matches = outer[start + index] == inner[index];
                }

                if (matches)
                {
                    return true;
                }
            }

            return false;
        }

        private static int Pick(IRandomSource random, int count)
        {
            int picked = (int)(random.NextDouble() * count);
            return picked < count ? picked : count - 1;
        }

        // Every combo of the allowed lengths, shortest first and in counting order: the first that fits, or none at all.
        private static int[] FirstFitting(IReadOnlyList<IReadOnlyList<int>> songs, int holeCount, int shortest, int longest)
        {
            for (int length = shortest; length <= longest; length++)
            {
                var combo = new int[length];
                double total = Math.Pow(holeCount, length);
                for (long code = 0; code < total; code++)
                {
                    long rest = code;
                    bool repeats = false;
                    for (int index = length - 1; index >= 0; index--)
                    {
                        combo[index] = (int)(rest % holeCount);
                        rest /= holeCount;
                        repeats |= index < length - 1 && combo[index] == combo[index + 1];
                    }

                    if (!repeats && CastsNoSong(combo, songs))
                    {
                        return combo;
                    }
                }
            }

            throw new InvalidOperationException("Every combo of " + shortest + " to " + longest + " notes holds or is held by a song.");
        }
    }
}
