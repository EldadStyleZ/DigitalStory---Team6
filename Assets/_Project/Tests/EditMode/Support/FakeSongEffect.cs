using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>A song played on holes 3, 2 and 1 that only records being cast.</summary>
    internal sealed class FakeSongEffect : SongEffect
    {
        public const float CooldownSeconds = 10f;

        public FakeSongEffect()
            : base(new SongDefinition("Fake", string.Empty, CooldownSeconds, 2, 1, 0))
        {
        }

        public int CastCount { get; private set; }
        public Vector2 LastPiperPosition { get; private set; }

        public override void Cast(in SongContext context)
        {
            CastCount++;
            LastPiperPosition = context.PiperPosition;
        }
    }
}
