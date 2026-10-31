using System;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>Builds the rules tests run against, using each config's default values.</summary>
    internal static class TestRules
    {
        private static GameRules _default;

        public static GameRules Default => _default ??= new GameRules(
            ScriptableObject.CreateInstance<TownConfig>(),
            ScriptableObject.CreateInstance<PiperConfig>(),
            ScriptableObject.CreateInstance<BreathConfig>(),
            ScriptableObject.CreateInstance<FluteConfig>(),
            ScriptableObject.CreateInstance<RatConfig>(),
            ScriptableObject.CreateInstance<CatConfig>());

        public static NightRules PrototypeNight(int index) => index switch
        {
            0 => new NightRules(1234, 70, 45, 130f, 3, 0),
            1 => new NightRules(2211, 170, 115, 150f, 5, 1),
            2 => new NightRules(3188, 290, 210, 180f, 7, 3),
            3 => new NightRules(4165, 430, 330, 210f, 9, 5),
            _ => throw new ArgumentOutOfRangeException(nameof(index), index, "The prototype has four seeds."),
        };

        public static HouseCollider NoHouses() => new HouseCollider(Array.Empty<House>());

        public static RiverBank Bank() => new RiverBank(Default.Town);
    }
}
