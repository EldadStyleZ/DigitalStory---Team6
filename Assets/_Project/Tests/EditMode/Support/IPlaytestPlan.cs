namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// One way of playing a whole match, for the playtest bot. Each tick the bot asks the plan what to do, and the plan
    /// answers through the bot's actions: where to fly, which song to play, whether to hold Give or call the wave.
    /// </summary>
    internal interface IPlaytestPlan
    {
        /// <summary>The plan's name in the results table.</summary>
        string Name { get; }

        /// <summary>Chooses this tick's actions. Anything not asked for this tick is not done: the core stands and nothing is held.</summary>
        void Decide(PlaytestBot bot);
    }
}
