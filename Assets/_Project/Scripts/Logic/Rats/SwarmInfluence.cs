using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>What the rats react to this step.</summary>
    public readonly struct SwarmInfluence
    {
        public SwarmInfluence(Vector2 piperPosition, float piperFacing, FluteMode mode, float musicRadius)
        {
            PiperPosition = piperPosition;
            PiperFacing = piperFacing;
            Mode = mode;
            MusicRadius = musicRadius;
        }

        public Vector2 PiperPosition { get; }
        public float PiperFacing { get; }
        public FluteMode Mode { get; }
        public float MusicRadius { get; }

        public bool IsPlaying => Mode != FluteMode.None;
    }
}
