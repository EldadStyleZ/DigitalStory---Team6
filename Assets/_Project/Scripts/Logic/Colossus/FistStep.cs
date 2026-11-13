namespace DigitalStory.Logic
{
    /// <summary>What a tick of <see cref="ColossusFist"/> brought about, for whoever ticks it to act on.</summary>
    public enum FistStep
    {
        /// <summary>Nothing new: the phase goes on.</summary>
        None,

        /// <summary>The thrust is over and the fist leaves the arm. Give it its flight with <see cref="ColossusFist.Fly"/>.</summary>
        Launch,

        /// <summary>The fist reached its target and hits now. Its arm starts to grow back.</summary>
        Impact,

        /// <summary>The arm is whole again and the hand can launch.</summary>
        Regrown,
    }
}
