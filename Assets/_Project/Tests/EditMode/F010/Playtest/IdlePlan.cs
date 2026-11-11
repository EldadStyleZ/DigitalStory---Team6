namespace DigitalStory.Tests.EditMode
{
    /// <summary>Does nothing: the core stands in the middle, plays no song and never calls a wave.</summary>
    internal sealed class IdlePlan : IPlaytestPlan
    {
        public string Name => "Idle";

        public void Decide(PlaytestBot bot)
        {
        }
    }
}
