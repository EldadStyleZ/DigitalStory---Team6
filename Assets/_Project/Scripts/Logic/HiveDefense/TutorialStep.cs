namespace DigitalStory.Logic
{
    /// <summary>
    /// The prompts of the first round's lesson (plan section N), in the order they are taught. A new step goes in its place
    /// in this list.
    /// </summary>
    public enum TutorialStep
    {
        /// <summary>Move with W A S D.</summary>
        Move = 0,

        /// <summary>Play J K H: cast the Sting.</summary>
        PlaySting = 1,

        /// <summary>
        /// Fly to the glowing nest and stay until it is yours. The session ends it when a nest is collected, or at once if
        /// one was collected before this step.
        /// </summary>
        CollectNest = 2,

        /// <summary>
        /// Fly to a hive to take its wasps. The session ends it when the core reaches a standing hive: its store flows in by
        /// itself while the swarm has room, and the swarm starts full.
        /// </summary>
        VisitHive = 3,

        /// <summary>
        /// Hold E beside a hurt hive to give it wasps. The session ends it as soon as no standing hive is hurt, so a hive
        /// healed before this step does not leave it waiting.
        /// </summary>
        HealHive = 4,

        /// <summary>Enemies come when the timer ends; press Enter when ready. Shown until the first wave starts.</summary>
        CallWave = 5,

        /// <summary>The lesson is over. Nothing is shown.</summary>
        Done = 6
    }
}
