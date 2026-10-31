using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>The configs a night runs on.</summary>
    public sealed class GameRules
    {
        public GameRules(TownConfig town, PiperConfig piper, BreathConfig breath, FluteConfig flute, RatConfig rats)
        {
            Town = town != null ? town : throw new ArgumentNullException(nameof(town));
            Piper = piper != null ? piper : throw new ArgumentNullException(nameof(piper));
            Breath = breath != null ? breath : throw new ArgumentNullException(nameof(breath));
            Flute = flute != null ? flute : throw new ArgumentNullException(nameof(flute));
            Rats = rats != null ? rats : throw new ArgumentNullException(nameof(rats));
        }

        public TownConfig Town { get; }
        public PiperConfig Piper { get; }
        public BreathConfig Breath { get; }
        public FluteConfig Flute { get; }
        public RatConfig Rats { get; }

        /// <summary>Where the Piper begins.</summary>
        public Vector2 PiperStart => new Vector2(Town.RiverX - Piper.StartRiverSetback, Town.Height / 2f);
    }
}
