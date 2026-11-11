namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Plays as <see cref="MixedPlan"/>, and in each farm phase first flies to every open nest, nearest first, and stays
    /// until it is collected. Its chores and the early call come after the nests.
    /// </summary>
    internal sealed class MixedWithNestsPlan : IPlaytestPlan
    {
        private readonly MixedPlan _mixed = new MixedPlan(avoidsNests: false);

        public string Name => "MixedWithNests";

        public void Decide(PlaytestBot bot)
        {
            int nest = bot.IsSurvive || bot.Session.IsColossusActive ? -1 : bot.NearestOpenNest();
            if (nest >= 0)
            {
                bot.FlyTo(bot.Session.Nests[nest].Position);
                return;
            }

            _mixed.Decide(bot);
        }
    }
}
