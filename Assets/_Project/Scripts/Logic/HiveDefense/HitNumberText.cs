using System;
using System.Globalization;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The text a hit number shows (plan sections V and Y). A hit below 1 shows its tenths, from "0.1" to "0.9", so one
    /// sting's squeeze of 0.3 does not read as 1; from 1 up it shows the whole number, up to "999+". The strings are made once
    /// for the whole game, so asking for one never allocates after the first call.
    /// </summary>
    public static class HitNumberText
    {
        private const int CachedNumbers = 1000;
        private const int TenthsPerOne = 10;

        private static string[] _wholes;
        private static string[] _tenths;

        /// <summary>The text for a hit of <paramref name="damage"/>. It allocates only on the first call.</summary>
        public static string Of(float damage)
        {
            BuildTables();
            int tenths = Math.Max(1, (int)Math.Round(damage * TenthsPerOne, MidpointRounding.AwayFromZero));
            if (tenths < TenthsPerOne)
            {
                return _tenths[tenths];
            }

            int whole = Math.Max(1, (int)Math.Round(damage, MidpointRounding.AwayFromZero));
            return whole < CachedNumbers ? _wholes[whole] : _wholes[CachedNumbers - 1];
        }

        private static void BuildTables()
        {
            if (_wholes != null)
            {
                return;
            }

            var tenths = new string[TenthsPerOne];
            for (int tenth = 0; tenth < TenthsPerOne; tenth++)
            {
                tenths[tenth] = (tenth / (float)TenthsPerOne).ToString("0.0", CultureInfo.InvariantCulture);
            }

            var wholes = new string[CachedNumbers];
            for (int number = 0; number < CachedNumbers - 1; number++)
            {
                wholes[number] = number.ToString(CultureInfo.InvariantCulture);
            }

            wholes[CachedNumbers - 1] = (CachedNumbers - 1).ToString(CultureInfo.InvariantCulture) + "+";
            _tenths = tenths;
            _wholes = wholes;
        }
    }
}
