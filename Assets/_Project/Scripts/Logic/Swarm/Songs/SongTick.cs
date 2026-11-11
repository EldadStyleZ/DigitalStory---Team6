using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>One step of a running song's rule: the song's clock before and after the step, and where the Piper stands.</summary>
    public readonly struct SongTick
    {
        /// <summary>Makes a step from <paramref name="secondsBefore"/> to <paramref name="seconds"/> after the cast.</summary>
        public SongTick(float secondsBefore, float seconds, Vector3 piperPosition)
        {
            SecondsBefore = secondsBefore;
            Seconds = seconds;
            PiperPosition = piperPosition;
        }

        /// <summary>Seconds since the cast when the step began.</summary>
        public float SecondsBefore { get; }

        /// <summary>Seconds since the cast when the step ends.</summary>
        public float Seconds { get; }

        /// <summary>Where the Piper stands now.</summary>
        public Vector3 PiperPosition { get; }
    }
}
