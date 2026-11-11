namespace DigitalStory.Logic
{
    /// <summary>Why a song whose notes were completed did not cast (<see cref="SwarmSongCaster.SongNotReady"/>).</summary>
    public enum SongRefusal
    {
        /// <summary>The song was cast a moment ago and is resting.</summary>
        CoolingDown = 0,

        /// <summary>The swarm has collapsed and has not come back yet.</summary>
        Collapsed = 1,

        /// <summary>The Colossus stands, and no song can replace it.</summary>
        GiantStanding = 2,

        /// <summary>The song needs a full Fury meter (the Colossus).</summary>
        NeedsFury = 3,

        /// <summary>Paying the song's price would take the swarm below its collapse line.</summary>
        NotEnoughWasps = 4
    }
}
