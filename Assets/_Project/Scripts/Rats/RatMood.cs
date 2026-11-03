namespace PiperToy
{
    /// <summary>What a rat is doing right now. Each mood moves it a different way.</summary>
    public enum RatMood
    {
        /// <summary>Strolls about on its own.</summary>
        Wander,

        /// <summary>Charmed: walks after the Piper.</summary>
        Follow,

        /// <summary>Charmed: walks ahead of the Piper, the way it faces.</summary>
        March,

        /// <summary>Scared: runs away from the Piper.</summary>
        Flee,

        /// <summary>Walks behind the rat in front of it, in the conga line.</summary>
        Conga,

        /// <summary>Stands in its place in the heart around the Piper.</summary>
        Heart,

        /// <summary>Spins round and round in flashing colors.</summary>
        Disco,

        /// <summary>Swollen up and floating on the wind, until it pops.</summary>
        Float,

        /// <summary>Follows the Piper, hopping and singing.</summary>
        Sing,

        /// <summary>Lies on its back, playing dead.</summary>
        Faint,

        /// <summary>Crowd Surf: one of the crowd under the Piper, carrying it wherever the player steers.</summary>
        Carry,
    }
}
