using System;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The parts of the Colossus a move can throw wasps off (songs-v2 W). Flags, so a slam throws both arms at once. The numbers
    /// are the bits WaspSwarm.compute reads for each bone, so keep them in step with it.
    /// </summary>
    [Flags]
    public enum ColossusLimb
    {
        None = 0,
        RightArm = 1,
        LeftArm = 2,
        RightLeg = 4,
        LeftLeg = 8,
        HeadAndChest = 16,
    }
}
