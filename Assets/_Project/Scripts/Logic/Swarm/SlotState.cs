using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>What the view needs to draw one song slot: the song that holds it, how far along it is and which wasps it takes.</summary>
    public readonly struct SlotState
    {
        /// <summary>Makes a slot's state. An empty slot is inactive, with an empty seed range.</summary>
        public SlotState(
            bool isActive,
            SwarmForm form,
            SongCategory category,
            float seconds,
            float releasedSeconds,
            float low,
            float high,
            Vector4 paramA,
            Vector4 paramB)
        {
            IsActive = isActive;
            Form = form;
            Category = category;
            Seconds = seconds;
            ReleasedSeconds = releasedSeconds;
            Low = low;
            High = high;
            ParamA = paramA;
            ParamB = paramB;
        }

        /// <summary>True while a song, or the return beat after it, holds the slot.</summary>
        public bool IsActive { get; }

        /// <summary>The song's picture: the form id its squad flies in the compute shader. River while the slot is empty.</summary>
        public SwarmForm Form { get; }

        /// <summary>What the song is for, which gives its colour. Means nothing while the slot is empty.</summary>
        public SongCategory Category { get; }

        /// <summary>Seconds since the song was cast.</summary>
        public float Seconds { get; }

        /// <summary>Seconds since the song let go, or -1 while it still runs.</summary>
        public float ReleasedSeconds { get; }

        /// <summary>The lowest wasp seed in the song's squad.</summary>
        public float Low { get; }

        /// <summary>The end of the song's squad: it is every living wasp with a seed from <see cref="Low"/> up to, but not including, this.</summary>
        public float High { get; }

        /// <summary>The song's own numbers for its picture (p0), as the song's "CPU:" line in plan section B says.</summary>
        public Vector4 ParamA { get; }

        /// <summary>The song's next numbers for its picture (p1). Its w is always 0 and left free for the view, which puts the release time there.</summary>
        public Vector4 ParamB { get; }
    }
}
