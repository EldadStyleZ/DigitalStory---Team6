using System.Globalization;
using System.Text.RegularExpressions;

namespace SpacePartyGame
{
    /// <summary>
    /// Moves an instrument's whole pitch up or down, for chord notes outside its six keys (such as the G sharp in the
    /// blow's E major chord). The sound settings' pitch is private, and the flute's belong to the toy, so a copy is
    /// made through its saved form with only <c>_baseHertz</c> changed.
    /// </summary>
    public static class InstrumentPitch
    {
        private static readonly Regex BaseHertz = new Regex("\"_baseHertz\":(-?[0-9.eE+-]+)");

        /// <summary>Multiplies the <c>_baseHertz</c> value in a sound's JSON by <paramref name="ratio"/>.</summary>
        public static string ShiftBaseHertz(string json, float ratio) =>
            BaseHertz.Replace(json, match =>
            {
                double hertz = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) * ratio;
                return "\"_baseHertz\":" + ((float)hertz).ToString("R", CultureInfo.InvariantCulture);
            });
    }
}
