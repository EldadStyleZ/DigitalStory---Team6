namespace DigitalStory.Logic
{
    /// <summary>Where a Hive Defense match stands.</summary>
    public enum HiveDefenseState
    {
        /// <summary>The match is running.</summary>
        Playing = 0,

        /// <summary>Every wave was cleared.</summary>
        Won = 1,

        /// <summary>Every hive was destroyed.</summary>
        Lost = 2
    }
}
