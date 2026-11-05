using System.Collections.Generic;
using UnityEngine;

namespace PiperToy
{
    /// <summary>
    /// The disco's colour rule: each colour holds for most of a beat, then fades quickly into the next, so the
    /// lights read as clear colours that change on the beat instead of a smeared rainbow.
    /// </summary>
    /// <remarks>Plain C#, so EditMode tests can check it without a scene.</remarks>
    public static class DiscoPalette
    {
        // The last part of each beat is the fade into the next colour.
        private const float FadeShare = 0.3f;

        /// <summary>The colour after this many beats, counting from the first colour.</summary>
        public static Color ColorAt(IReadOnlyList<Color> colors, float beats)
        {
            float wholeBeats = Mathf.Floor(beats);
            int index = (int)Mathf.Repeat(wholeBeats, colors.Count);
            Color now = colors[index];
            Color next = colors[(index + 1) % colors.Count];

            float intoTheBeat = beats - wholeBeats;
            float fade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f - FadeShare, 1f, intoTheBeat));
            return Color.Lerp(now, next, fade);
        }
    }
}
