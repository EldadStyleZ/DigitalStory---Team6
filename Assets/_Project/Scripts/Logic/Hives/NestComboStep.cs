namespace DigitalStory.Logic
{
    /// <summary>What one note did to the combo of the nest whose ring the core stands in (plan section U).</summary>
    public enum NestComboStep
    {
        /// <summary>The note neither continued the combo nor started it, and there was nothing to lose.</summary>
        Ignored = 0,

        /// <summary>The note was the combo's next one: one more slot is filled.</summary>
        Advanced = 1,

        /// <summary>The note did not continue the combo: what was played is lost and the combo is empty again.</summary>
        Broken = 2,

        /// <summary>The note did not continue the combo but is its first note: what was played is lost, and it counts as the first.</summary>
        Restarted = 3,

        /// <summary>The note was the combo's last one: the nest is collected.</summary>
        Completed = 4,
    }
}
