using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>A battle song still working, and how long it has left.</summary>
    public sealed class RunningSong
    {
        public RunningSong(BattleSong song)
        {
            Song = song;
            Restart();
        }

        public BattleSong Song { get; }
        public float SecondsLeft { get; private set; }

        public void Restart() => SecondsLeft = Song.Buff.Seconds;

        public void Tick(float deltaTime) => SecondsLeft -= deltaTime;
    }
}
