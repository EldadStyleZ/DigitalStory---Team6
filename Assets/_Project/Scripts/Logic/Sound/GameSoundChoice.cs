using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Which recorded sound a moment of play takes (plan section AC). A cast takes its song family's sound (the Honeycomb and
    /// the Nest trap share the tower's); a kill takes the death and not the hit; a blow a shield blocks takes none, so the
    /// shield's own knock stays; the giant's roar sounds as it starts and its blows as they land; a combo note that breaks
    /// the combo, or restarts it, is a wrong note. Big moments duck the swarm's hum. Feedback only: it changes no game rule.
    /// </summary>
    public static class GameSoundChoice
    {
        /// <summary>The sound for a song's cast, if it has one. The Colossus has none: the giant forming has its own.</summary>
        public static bool ForCast(SwarmSongEffect song, out GameSoundEvent sound)
        {
            sound = GameSoundEvent.SongFailed;
            if (song is SlotSongEffect slotSong)
            {
                return ForSlotForm(slotSong.Form, out sound);
            }

            if (song is GustSong)
            {
                sound = GameSoundEvent.CastGust;
                return true;
            }

            if (song is HiveCallSong)
            {
                sound = GameSoundEvent.CastHiveCall;
                return true;
            }

            return false;
        }

        /// <summary>The sound for a song's or the giant's hit on a beekeeper, if it has one.</summary>
        public static bool ForHit(in SongHit hit, out GameSoundEvent sound)
        {
            sound = hit.IsKill ? GameSoundEvent.BeekeeperDeath : GameSoundEvent.BeekeeperHit;
            return !hit.IsBlocked;
        }

        /// <summary>The sound as a giant move starts, if it has one: only the roar, which is a voice, not a blow.</summary>
        public static bool ForColossusStarted(ColossusAttack attack, out GameSoundEvent sound)
        {
            sound = GameSoundEvent.GiantRoar;
            return attack == ColossusAttack.Roar;
        }

        /// <summary>The sound as a giant move lands, if it has one.</summary>
        public static bool ForColossusLanded(ColossusAttack attack, out GameSoundEvent sound)
        {
            switch (attack)
            {
                case ColossusAttack.Punch:
                case ColossusAttack.HeavyPunch:
                    sound = GameSoundEvent.GiantPunch;
                    return true;
                case ColossusAttack.Kick:
                case ColossusAttack.SpinKick:
                    sound = GameSoundEvent.GiantKick;
                    return true;
                case ColossusAttack.Slam:
                    sound = GameSoundEvent.GiantSlam;
                    return true;
                case ColossusAttack.LeapSlam:
                    sound = GameSoundEvent.GiantLeapLanding;
                    return true;
                default:
                    sound = GameSoundEvent.GiantPunch;
                    return false;
            }
        }

        /// <summary>The sound for a song's impact on the ground, if it has one: only the Meteor's.</summary>
        public static bool ForFormLanded(SwarmForm form, out GameSoundEvent sound)
        {
            sound = GameSoundEvent.MeteorImpact;
            return form == SwarmForm.Meteor;
        }

        /// <summary>The sound for a note at a nest's combo, if it has one. The last note's sound is the nest's collection.</summary>
        public static bool ForComboNote(NestComboStep step, out GameSoundEvent sound)
        {
            switch (step)
            {
                case NestComboStep.Advanced:
                    sound = GameSoundEvent.ComboRightNote;
                    return true;
                case NestComboStep.Broken:
                case NestComboStep.Restarted:
                    sound = GameSoundEvent.ComboWrongNote;
                    return true;
                default:
                    sound = GameSoundEvent.ComboRightNote;
                    return false;
            }
        }

        /// <summary>True for the big moments that duck the swarm's hum so they stand out.</summary>
        public static bool IsBig(GameSoundEvent sound)
        {
            switch (sound)
            {
                case GameSoundEvent.MeteorImpact:
                case GameSoundEvent.GiantForm:
                case GameSoundEvent.GiantSlam:
                case GameSoundEvent.GiantRoar:
                case GameSoundEvent.GiantLeapLanding:
                case GameSoundEvent.HiveLost:
                case GameSoundEvent.WaveStart:
                case GameSoundEvent.Win:
                case GameSoundEvent.Lose:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>True for the two loops under everything, which never play as one-shots.</summary>
        public static bool IsLoop(GameSoundEvent sound)
        {
            return sound == GameSoundEvent.SwarmLoop || sound == GameSoundEvent.SwarmAngry;
        }

        private static bool ForSlotForm(SwarmForm form, out GameSoundEvent sound)
        {
            switch (form)
            {
                case SwarmForm.Sting:
                    sound = GameSoundEvent.CastSting;
                    return true;
                case SwarmForm.Tornado:
                    sound = GameSoundEvent.CastTornado;
                    return true;
                case SwarmForm.Meteor:
                    sound = GameSoundEvent.CastMeteor;
                    return true;
                case SwarmForm.Hydra:
                    sound = GameSoundEvent.CastHydra;
                    return true;
                case SwarmForm.Scythe:
                    sound = GameSoundEvent.CastScythe;
                    return true;
                case SwarmForm.Lance:
                    sound = GameSoundEvent.CastLance;
                    return true;
                case SwarmForm.Dome:
                    sound = GameSoundEvent.CastDome;
                    return true;
                case SwarmForm.Honeycomb:
                case SwarmForm.Nest:
                    sound = GameSoundEvent.TowerPlaced;
                    return true;
                default:
                    sound = GameSoundEvent.SongFailed;
                    return false;
            }
        }
    }
}
