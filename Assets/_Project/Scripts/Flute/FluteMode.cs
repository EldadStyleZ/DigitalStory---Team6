namespace PiperToy
{
    /// <summary>What the flute's sound does to the rats that hear it, as in the old game.</summary>
    public enum FluteMode
    {
        /// <summary>No hole is held.</summary>
        Silent,

        /// <summary>Holes 1 and 2: the rats come and follow the Piper.</summary>
        Call,

        /// <summary>Holes 3 and 4: the rats march ahead of the Piper.</summary>
        March,

        /// <summary>Holes 5 and 6: the rats run away.</summary>
        Scare,
    }
}
