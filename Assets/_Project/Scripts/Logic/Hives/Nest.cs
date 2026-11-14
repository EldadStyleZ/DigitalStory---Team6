using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// One wild nest in a tree (plan sections H and U). It is closed until its wave is cleared, then open until the core,
    /// standing in its ring, plays its combo, and then it is gone. <see cref="NestField"/> opens it and hands it the notes.
    /// </summary>
    public sealed class Nest
    {
        private readonly int[] _combo;

        internal Nest(Vector3 position, int[] combo)
        {
            if (combo == null || combo.Length == 0)
            {
                throw new ArgumentException("A nest needs a combo of at least one note.", nameof(combo));
            }

            Position = position;
            _combo = combo;
        }

        /// <summary>Where the nest hangs.</summary>
        public Vector3 Position { get; }

        /// <summary>The notes that collect the nest, in order, holes counting from 0.</summary>
        public IReadOnlyList<int> Combo => _combo;

        /// <summary>How many of the combo's notes have been played in order since the core stepped into the ring.</summary>
        public int NotesPlayed { get; private set; }

        /// <summary>True while the nest can be collected: its wave has been cleared and it is not yet collected.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>True once the core has collected the nest. A collected nest is gone, and is no longer open.</summary>
        public bool IsCollected { get; private set; }

        /// <summary>How far the combo has got, from 0 to 1: the share of its notes played. 1 once collected, 0 while closed.</summary>
        public float Progress01 => IsCollected ? 1f : (float)NotesPlayed / _combo.Length;

        /// <summary>Opens the nest, so the core can collect it. A collected nest stays gone.</summary>
        internal void Open()
        {
            if (!IsCollected)
            {
                IsOpen = true;
            }
        }

        /// <summary>The core left the ring: the combo starts again from nothing.</summary>
        internal void Restart()
        {
            NotesPlayed = 0;
        }

        /// <summary>
        /// Hears one note. The combo's next note fills one more slot, and its last collects the nest. Any other note empties
        /// the combo, and counts as its first when it is the first. Only an open nest hears notes.
        /// </summary>
        internal NestComboStep Play(int hole)
        {
            if (!IsOpen)
            {
                return NestComboStep.Ignored;
            }

            if (_combo[NotesPlayed] == hole)
            {
                NotesPlayed++;
                if (NotesPlayed < _combo.Length)
                {
                    return NestComboStep.Advanced;
                }

                IsOpen = false;
                IsCollected = true;
                return NestComboStep.Completed;
            }

            bool hadNotes = NotesPlayed > 0;
            bool isFirst = _combo[0] == hole;
            NotesPlayed = isFirst ? 1 : 0;
            if (!hadNotes)
            {
                return NestComboStep.Ignored;
            }

            return isFirst ? NestComboStep.Restarted : NestComboStep.Broken;
        }
    }
}
