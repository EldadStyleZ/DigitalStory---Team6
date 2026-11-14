using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Which optional impact effect a moment of play shows (plan section AB). A kill shows the death and not the hit; a hit
    /// shows the hit from the least damage up, so the Sting's many small squeezes stay clean; a blow a shield blocks shows
    /// none, as the shield's own blue flash says it. Only the Meteor's landing and the giant's two ground blows (the slam and
    /// the leap's landing) show a landing. Feedback only: it changes no game rule.
    /// </summary>
    public static class ImpactFxChoice
    {
        /// <summary>The effect for a song's or the giant's hit, if it shows one.</summary>
        /// <param name="hit">The hit.</param>
        /// <param name="hitLeastDamage">The least damage of a hit that does not kill and still shows the hit effect.</param>
        /// <param name="kind">The effect, when the method returns true.</param>
        public static bool ForHit(in SongHit hit, float hitLeastDamage, out ImpactFxKind kind)
        {
            kind = ImpactFxKind.Hit;
            if (hit.IsBlocked)
            {
                return false;
            }

            if (hit.IsKill)
            {
                kind = ImpactFxKind.Death;
                return true;
            }

            return hit.Damage >= hitLeastDamage;
        }

        /// <summary>The effect for a song's impact on the ground, if it shows one: only the Meteor's.</summary>
        public static bool ForFormLanded(SwarmForm form, out ImpactFxKind kind)
        {
            kind = ImpactFxKind.MeteorLanding;
            return form == SwarmForm.Meteor;
        }

        /// <summary>The effect for a giant blow that lands, if it shows one: the ground slam and the leap slam's landing.</summary>
        public static bool ForColossusAttack(ColossusAttack attack, out ImpactFxKind kind)
        {
            kind = ImpactFxKind.GiantSlam;
            return attack == ColossusAttack.Slam || attack == ColossusAttack.LeapSlam;
        }
    }
}
