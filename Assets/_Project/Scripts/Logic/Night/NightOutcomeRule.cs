namespace DigitalStory.Logic
{
    /// <summary>Decides whether the night has ended.</summary>
    public static class NightOutcomeRule
    {
        /// <summary>Returns the outcome. Reaching the quota beats dawn when both happen together.</summary>
        public static NightOutcome Resolve(int drowned, int alive, int quota, float timeLeft)
        {
            if (drowned >= quota)
            {
                return NightOutcome.Won;
            }

            if (drowned + alive < quota)
            {
                return NightOutcome.QuotaOutOfReach;
            }

            return timeLeft <= 0f ? NightOutcome.DawnBroke : NightOutcome.InProgress;
        }
    }
}
