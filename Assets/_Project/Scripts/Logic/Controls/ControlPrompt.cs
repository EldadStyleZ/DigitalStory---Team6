using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Turns a prompt written for the keyboard ("Play J K H.", "Hold E to heal") into what a controller player reads: each note
    /// key becomes a hole, for the HUD to draw as a button symbol, and each other key word becomes the controller's name for it.
    /// Only whole words count, in the same case, with any full stop, comma or colon after them kept as text.
    /// </summary>
    /// <remarks>It allocates, so call it when a prompt or the device changes, never every frame.</remarks>
    public static class ControlPrompt
    {
        private const char Space = ' ';
        private static readonly char[] Spaces = { Space };

        /// <summary>
        /// Cuts <paramref name="text"/> into runs of words and holes, in order, into <paramref name="parts"/>, which is cleared
        /// first. Words between holes are joined by single spaces into one part.
        /// </summary>
        /// <param name="text">The keyboard prompt. Null or empty gives no part.</param>
        /// <param name="holeNames">The key word of each hole, lowest hole first, such as H J K L.</param>
        /// <param name="swaps">
        /// Key words and what a controller player reads instead, such as ("E", "RB"). A key word may be several words, such as
        /// "W A S D", and then only the whole run is swapped.
        /// </param>
        /// <param name="parts">Filled with the parts.</param>
        /// <exception cref="ArgumentNullException">The hole names, the swaps or the list is missing.</exception>
        public static void Split(
            string text, IReadOnlyList<string> holeNames, IReadOnlyList<(string Keys, string Pad)> swaps, List<PromptPart> parts)
        {
            if (holeNames == null)
            {
                throw new ArgumentNullException(nameof(holeNames));
            }

            if (swaps == null)
            {
                throw new ArgumentNullException(nameof(swaps));
            }

            if (parts == null)
            {
                throw new ArgumentNullException(nameof(parts));
            }

            parts.Clear();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            string[] words = text.Split(Spaces, StringSplitOptions.RemoveEmptyEntries);
            var run = new StringBuilder(text.Length);
            int index = 0;
            while (index < words.Length)
            {
                if (TrySwap(words, index, swaps, out string swapped, out int used))
                {
                    AppendWord(run, swapped);
                    index += used;
                    continue;
                }

                string word = words[index];
                string core = Core(word);
                int hole = HoleOf(core, holeNames);
                index++;
                if (hole < 0)
                {
                    AppendWord(run, word);
                    continue;
                }

                // The hole stands alone; a stop after it ("H.") starts the next run of words.
                Flush(run, parts);
                parts.Add(PromptPart.OfHole(hole));
                run.Append(word, core.Length, word.Length - core.Length);
            }

            Flush(run, parts);
        }

        // A run of words that matches a swap's key words from here, the last one compared without its stop, which is kept after
        // the swapped name.
        private static bool TrySwap(
            string[] words, int start, IReadOnlyList<(string Keys, string Pad)> swaps, out string swapped, out int used)
        {
            for (int swap = 0; swap < swaps.Count; swap++)
            {
                (string keys, string pad) = swaps[swap];
                if (string.IsNullOrEmpty(keys))
                {
                    continue;
                }

                string[] keyWords = keys.Split(Spaces, StringSplitOptions.RemoveEmptyEntries);
                if (keyWords.Length == 0 || start + keyWords.Length > words.Length)
                {
                    continue;
                }

                bool isMatch = true;
                for (int offset = 0; offset < keyWords.Length && isMatch; offset++)
                {
                    string word = words[start + offset];
                    bool isLast = offset == keyWords.Length - 1;
                    isMatch = string.Equals(isLast ? Core(word) : word, keyWords[offset], StringComparison.Ordinal);
                }

                if (!isMatch)
                {
                    continue;
                }

                string last = words[start + keyWords.Length - 1];
                swapped = (pad ?? string.Empty) + last.Substring(Core(last).Length);
                used = keyWords.Length;
                return true;
            }

            swapped = null;
            used = 0;
            return false;
        }

        private static int HoleOf(string word, IReadOnlyList<string> holeNames)
        {
            if (word.Length == 0)
            {
                return -1;
            }

            for (int hole = 0; hole < holeNames.Count; hole++)
            {
                if (string.Equals(word, holeNames[hole], StringComparison.Ordinal))
                {
                    return hole;
                }
            }

            return -1;
        }

        // The word without the stops after it: "H." is "H", "Enter:" is "Enter" and "..." is empty.
        private static string Core(string word)
        {
            int end = word.Length;
            while (end > 0 && IsStop(word[end - 1]))
            {
                end--;
            }

            return end == word.Length ? word : word.Substring(0, end);
        }

        private static bool IsStop(char letter)
        {
            return letter == '.' || letter == ',' || letter == ':' || letter == ';' || letter == '!' || letter == '?';
        }

        private static void AppendWord(StringBuilder run, string word)
        {
            if (run.Length > 0)
            {
                run.Append(Space);
            }

            run.Append(word);
        }

        private static void Flush(StringBuilder run, List<PromptPart> parts)
        {
            if (run.Length == 0)
            {
                return;
            }

            parts.Add(PromptPart.Words(run.ToString()));
            run.Clear();
        }
    }
}
