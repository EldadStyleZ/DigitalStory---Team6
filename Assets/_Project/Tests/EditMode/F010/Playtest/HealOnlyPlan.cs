namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Keeps the hives healed and never attacks: it flies to the most hurt hive and holds Give while the swarm has wasps to
    /// spare, and otherwise waits beside the hive with the fullest store, taking it. It plays no song and never calls a wave.
    /// </summary>
    internal sealed class HealOnlyPlan : IPlaytestPlan
    {
        // Every hurt hive is worth healing to this plan.
        private const float HealBelowShare = 1f;

        public string Name => "HealOnly";

        public void Decide(PlaytestBot bot)
        {
            int hurt = bot.MostHurtHive(HealBelowShare);
            if (hurt >= 0 && bot.Session.Swarm.Spendable > 0)
            {
                bot.FlyTo(bot.BesideHive(hurt));
                if (bot.Session.NearHive == hurt)
                {
                    bot.HoldGive();
                }

                return;
            }

            int richest = bot.RichestHive();
            if (richest >= 0)
            {
                bot.FlyTo(bot.BesideHive(richest));
            }
        }
    }
}
