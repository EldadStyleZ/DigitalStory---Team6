using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>The rule of a <see cref="FakeSlotSong"/>: it only records what the slot engine did to it, and lets go when its song says.</summary>
    internal sealed class FakeSongRule : SongRule
    {
        private readonly FakeSlotSong _song;

        public FakeSongRule(FakeSlotSong song)
            : base(song)
        {
            _song = song;
        }

        public int StartCount { get; private set; }

        public int TickCount { get; private set; }

        public int HitCount { get; private set; }

        public override Vector4 ParamA => new Vector4(1f, 2f, 3f, 4f);

        public override Vector4 ParamB => new Vector4(5f, 6f, 7f, 0f);

        protected override void Begin(in SwarmSongContext context)
        {
            StartCount++;
        }

        protected override void Tick(in SongTick tick)
        {
            TickCount++;

            if (tick.Seconds >= _song.ReleaseAfter)
            {
                Release(_song.ReleaseAfter);
            }
        }

        protected override void NotifyPiperHit(Vector3 direction, float seconds)
        {
            HitCount++;
        }
    }
}
