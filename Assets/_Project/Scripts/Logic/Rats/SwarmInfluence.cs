using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>What the rats react to this step.</summary>
    public readonly struct SwarmInfluence
    {
        public SwarmInfluence(
            Vector2 piperPosition,
            float piperFacing,
            FluteMode mode,
            float musicRadius,
            CatPack cats = null,
            float charmZoneRadius = 0f)
        {
            PiperPosition = piperPosition;
            PiperFacing = piperFacing;
            Mode = mode;
            MusicRadius = musicRadius;
            Cats = cats;
            CharmZoneRadius = charmZoneRadius;
        }

        public Vector2 PiperPosition { get; }
        public float PiperFacing { get; }
        public FluteMode Mode { get; }
        public float MusicRadius { get; }

        /// <summary>The cats rats should fear, or null when there are none.</summary>
        public CatPack Cats { get; }

        /// <summary>How close to the Piper a rat stays fully charmed, or 0 when no song does this.</summary>
        public float CharmZoneRadius { get; }

        public bool IsPlaying => Mode != FluteMode.None;
    }
}
