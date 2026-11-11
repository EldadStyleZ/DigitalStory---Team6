namespace DigitalStory.Logic
{
    /// <summary>How auto-aim picks a target for a song.</summary>
    public enum AimRule
    {
        /// <summary>The most threatening beekeeper (Sting, Scythe).</summary>
        Threat = 0,

        /// <summary>The few nearest the core, or nearest the cursor while the mouse is live (Hydra).</summary>
        ThreeNearest = 1,

        /// <summary>The beekeeper whose line from the core holds the most beekeepers (Lance).</summary>
        Line = 2,

        /// <summary>The beekeeper with the most others close by (Meteor, Nest, Maelstrom).</summary>
        Crowd = 3,

        /// <summary>The hive hit most recently, else the nearest standing hive (Honeycomb).</summary>
        Hive = 4,
    }
}
