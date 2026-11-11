namespace DigitalStory.Logic
{
    /// <summary>
    /// The three places a song runs in. One song runs in each, beside the others, and each takes its wasps from its own
    /// band of seeds (see <see cref="DigitalStory.Data.SquadConfig"/>). The numbers are also the slot indices in the compute shader.
    /// </summary>
    public enum SongSlot
    {
        /// <summary>Something left on the map, such as a nest or a shell on a hive.</summary>
        Post = 0,

        /// <summary>A defense around the core, such as the Dome.</summary>
        Guard = 1,

        /// <summary>An attack, such as the Sting, the Tornado or the Meteor.</summary>
        Strike = 2,
    }
}
