using DigitalStory.Logic;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>A damage shield whose shares a test chooses. It lets everything through unless a test says otherwise.</summary>
    internal sealed class FakeDamageShield : IDamageShield
    {
        public float WaspLossFraction { get; set; } = 1f;

        /// <summary>The share that reaches every hive.</summary>
        public float HiveFraction { get; set; } = 1f;

        public float HiveDamageFraction(int hive)
        {
            return HiveFraction;
        }
    }
}
