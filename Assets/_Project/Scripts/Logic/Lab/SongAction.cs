using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>Plays a battle song.</summary>
    public sealed class SongAction : IComboAction
    {
        private readonly Battle _battle;
        private readonly BattleSong _song;

        public SongAction(Battle battle, BattleSong song)
        {
            _battle = battle;
            _song = song;
        }

        public bool HasRoom => true;

        public void Perform() => _battle.Play(_song);
    }
}
