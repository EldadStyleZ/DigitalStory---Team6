using System;

namespace DigitalStory.Logic
{
    /// <summary>
    /// How hard a cast's camera punch lands, by the song's weight (plan section Z). The Sting, the quick jab that can be cast
    /// every 1.5 s and the weakest attack, punches at a light share, so a run of Stings does not rock the view like a Meteor;
    /// every other song punches fully.
    /// </summary>
    public static class CastWeight
    {
        /// <summary>The share of the full cast punch this song's form gets.</summary>
        /// <param name="form">The cast song's form.</param>
        /// <param name="lightShare">The share for a light song, 0 to 1.</param>
        /// <exception cref="ArgumentOutOfRangeException">The share is outside 0 to 1, or not a number.</exception>
        public static float PunchShare(SwarmForm form, float lightShare)
        {
            if (!(lightShare >= 0f && lightShare <= 1f))
            {
                throw new ArgumentOutOfRangeException(nameof(lightShare), lightShare, "The light cast share must be 0 to 1.");
            }

            return form == SwarmForm.Sting ? lightShare : 1f;
        }
    }
}
