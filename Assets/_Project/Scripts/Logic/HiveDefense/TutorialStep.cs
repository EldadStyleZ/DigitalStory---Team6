namespace DigitalStory.Logic
{
    /// <summary>
    /// The prompts of the first round's lesson (plan section N), in the order they are taught. A new step (the nest, with
    /// the nests) goes in its place in this list.
    /// </summary>
    public enum TutorialStep
    {
        /// <summary>Move with W A S D.</summary>
        Move = 0,

        /// <summary>Play J K H: cast the Sting.</summary>
        PlaySting = 1,

        /// <summary>Fly to a hive and stay until some of its store has flowed in.</summary>
        VisitHive = 2,

        /// <summary>
        /// Hold E beside a hurt hive to give it wasps. The session ends it as soon as no standing hive is hurt, so a hive
        /// healed before this step does not leave it waiting.
        /// </summary>
        HealHive = 3,

        /// <summary>Enemies come when the timer ends; press Enter when ready. Shown until the first wave starts.</summary>
        CallWave = 4,

        /// <summary>The lesson is over. Nothing is shown.</summary>
        Done = 5
    }
}
