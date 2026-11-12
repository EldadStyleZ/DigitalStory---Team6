namespace DigitalStory.Logic
{
    /// <summary>
    /// The moves the Colossus can start. The last four came with the giant's version 2 (plan section I): L is the ground
    /// slam, H the roar, K K the spin kick and L L the leap slam.
    /// </summary>
    public enum ColossusAttack
    {
        None,
        Punch,
        HeavyPunch,
        Kick,
        Slam,
        Roar,
        SpinKick,
        LeapSlam,
    }
}
