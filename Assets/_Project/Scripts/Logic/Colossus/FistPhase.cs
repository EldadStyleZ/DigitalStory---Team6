namespace DigitalStory.Logic
{
    /// <summary>Where one of the giant's hands is in its launch (<see cref="ColossusFist"/>).</summary>
    public enum FistPhase
    {
        /// <summary>The arm is whole and the hand can launch.</summary>
        Ready,

        /// <summary>The arm thrusts forward; the fist has not left yet.</summary>
        Thrust,

        /// <summary>The wasps of the hand and forearm fly to the target as one fist.</summary>
        Flight,

        /// <summary>The fist has hit; its wasps stream back and the arm grows again. It cannot launch until it is whole.</summary>
        Regrow,
    }
}
