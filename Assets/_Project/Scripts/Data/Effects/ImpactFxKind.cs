namespace DigitalStory.Data
{
    /// <summary>The moments an optional impact effect can mark (plan section AB). Numbered for the library's slots.</summary>
    public enum ImpactFxKind
    {
        /// <summary>A song's or the giant's hit on a beekeeper that does not kill it.</summary>
        Hit = 0,

        /// <summary>A killing blow.</summary>
        Death = 1,

        /// <summary>A wild nest bursting as its combo collects it.</summary>
        NestBurst = 2,

        /// <summary>The Meteor striking the ground.</summary>
        MeteorLanding = 3,

        /// <summary>The giant's ground slam, and its leap slam's landing.</summary>
        GiantSlam = 4,
    }
}
