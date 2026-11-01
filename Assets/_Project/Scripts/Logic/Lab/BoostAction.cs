using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>Makes every creature hit harder for a while.</summary>
    public sealed class BoostAction : IComboAction
    {
        private readonly Battle _battle;
        private readonly BattleSong _song;

        public BoostAction(Battle battle, BattleSong song)
        {
            _battle = battle;
            _song = song;
        }

        public bool HasRoom => true;

        public void Perform() => _battle.Boost(_song.DamageMultiplier, _song.Seconds);
    }
}
