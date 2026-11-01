using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>Brings a creature to the Piper.</summary>
    public sealed class SummonAction : IComboAction
    {
        private readonly Battle _battle;
        private readonly CreatureType _creature;

        public SummonAction(Battle battle, CreatureType creature)
        {
            _battle = battle;
            _creature = creature;
        }

        public bool HasRoom => _battle.HasRoomForACreature;

        public void Perform() => _battle.AddCreature(_creature);
    }
}
