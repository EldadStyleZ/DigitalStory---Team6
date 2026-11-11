using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The three song slots and their fixed seed bands (plan section C). A cast replaces only the song in its own slot. A song
    /// that lets go plays a return beat, which is only a picture, and then its slot empties. Ending a slot empties it at once.
    /// </summary>
    internal sealed class SongSlots
    {
        private const int SlotCount = 3;
        private const float StillRunning = -1f;

        private readonly SongRule[] _rules = new SongRule[SlotCount];
        private readonly float[] _seconds = new float[SlotCount];
        private readonly float[] _low = new float[SlotCount];
        private readonly float[] _high = new float[SlotCount];
        private readonly float _returnSeconds;

        /// <summary>Reads the bands and the return beat. Bands must lie within 0 to 1 and never overlap.</summary>
        public SongSlots(SquadConfig squads)
        {
            if (squads == null)
            {
                throw new ArgumentNullException(nameof(squads));
            }

            SetBand(SongSlot.Post, squads.PostLow, squads.PostHigh);
            SetBand(SongSlot.Guard, squads.GuardLow, squads.GuardHigh);
            SetBand(SongSlot.Strike, squads.StrikeLow, squads.StrikeHigh);
            RequireApart(SongSlot.Post, SongSlot.Guard);
            RequireApart(SongSlot.Post, SongSlot.Strike);
            RequireApart(SongSlot.Guard, SongSlot.Strike);

            if (!(squads.ReturnSeconds >= 0f))
            {
                throw new ArgumentException("SquadConfig.ReturnSeconds cannot be negative.", nameof(squads));
            }

            _returnSeconds = squads.ReturnSeconds;
        }

        /// <summary>True while a song runs in the slot, not counting its return beat.</summary>
        public bool IsRunning(SongSlot slot)
        {
            SongRule rule = _rules[Index(slot)];
            return rule != null && !rule.IsReleased;
        }

        /// <summary>The form of the song running in the slot, or River when none runs.</summary>
        public SwarmForm RunningForm(SongSlot slot)
        {
            return IsRunning(slot) ? _rules[(int)slot].Song.Form : SwarmForm.River;
        }

        /// <summary>Puts a freshly started rule in the slot in place of whatever held it. A rule is never in two slots.</summary>
        public void Replace(SongSlot slot, SongRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            int index = Index(slot);

            for (int other = 0; other < SlotCount; other++)
            {
                if (other != index && _rules[other] == rule)
                {
                    Empty(other);
                }
            }

            _rules[index] = rule;
            _seconds[index] = 0f;
        }

        /// <summary>Empties the slot at once, with no return beat.</summary>
        public void End(SongSlot slot)
        {
            Empty(Index(slot));
        }

        /// <summary>Empties every slot at once, with no return beat.</summary>
        public void EndAll()
        {
            for (int index = 0; index < SlotCount; index++)
            {
                Empty(index);
            }
        }

        /// <summary>Advances every slot: running rules act, and a return beat that is over empties its slot.</summary>
        public void Tick(float deltaTime, Vector3 corePosition)
        {
            for (int index = 0; index < SlotCount; index++)
            {
                SongRule rule = _rules[index];

                if (rule == null)
                {
                    continue;
                }

                float before = _seconds[index];
                _seconds[index] = before + deltaTime;

                if (!rule.IsReleased)
                {
                    rule.Tick(new SongTick(before, _seconds[index], corePosition));

                    // Something that heard the rule land may have cast over it.
                    if (_rules[index] != rule)
                    {
                        continue;
                    }
                }

                if (rule.IsReleased && _seconds[index] - rule.ReleasedAt >= _returnSeconds)
                {
                    Empty(index);
                }
            }
        }

        /// <summary>Tells every running rule that the core was hit from <paramref name="direction"/>.</summary>
        public void NotifyCoreHit(Vector3 direction)
        {
            for (int index = 0; index < SlotCount; index++)
            {
                if (TryGetRunning(index, out SongRule rule))
                {
                    rule.NotifyCoreHit(direction, _seconds[index]);
                }
            }
        }

        /// <summary>Tells every running rule that the stick <paramref name="attacker"/> hit hive number <paramref name="hive"/>.</summary>
        public void NotifyHiveHit(int hive, ISwarmTarget attacker)
        {
            for (int index = 0; index < SlotCount; index++)
            {
                if (TryGetRunning(index, out SongRule rule))
                {
                    rule.NotifyHiveHit(hive, attacker, _seconds[index]);
                }
            }
        }

        /// <summary>Tells every running rule that the match entered <paramref name="phase"/>.</summary>
        public void NotifyPhase(MatchPhase phase)
        {
            for (int index = 0; index < SlotCount; index++)
            {
                if (TryGetRunning(index, out SongRule rule))
                {
                    rule.NotifyPhase(phase, _seconds[index]);
                }
            }
        }

        /// <summary>The share of each wasp loss that the running rules let through, multiplied together. 1 with none.</summary>
        public float WaspLossFraction()
        {
            float fraction = 1f;
            for (int index = 0; index < SlotCount; index++)
            {
                if (TryGetRunning(index, out SongRule rule))
                {
                    fraction *= rule.WaspLossFraction;
                }
            }

            return fraction;
        }

        /// <summary>The running rules' multipliers of the core's speed, multiplied together. 1 with none.</summary>
        public float CoreSpeedMultiplier()
        {
            float multiplier = 1f;
            for (int index = 0; index < SlotCount; index++)
            {
                if (TryGetRunning(index, out SongRule rule))
                {
                    multiplier *= rule.CoreSpeedMultiplier;
                }
            }

            return multiplier;
        }

        /// <summary>False when a running rule keeps smoke from slowing the core.</summary>
        public bool SmokeSlowsCore()
        {
            for (int index = 0; index < SlotCount; index++)
            {
                if (TryGetRunning(index, out SongRule rule) && !rule.SmokeSlowsCore)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>The share of damage the running rules let through to hive number <paramref name="hive"/>, multiplied together. 1 with none.</summary>
        public float HiveDamageFraction(int hive)
        {
            float fraction = 1f;
            for (int index = 0; index < SlotCount; index++)
            {
                if (TryGetRunning(index, out SongRule rule))
                {
                    fraction *= rule.HiveDamageFraction(hive);
                }
            }

            return fraction;
        }

        /// <summary>What the view needs to draw the slot.</summary>
        public SlotState State(SongSlot slot)
        {
            int index = Index(slot);
            SongRule rule = _rules[index];
            float low = _low[index];

            if (rule == null)
            {
                return new SlotState(false, SwarmForm.River, default, 0f, StillRunning, low, low, Vector4.zero, Vector4.zero);
            }

            SlotSongEffect song = rule.Song;
            float released = rule.IsReleased ? Math.Max(0f, _seconds[index] - rule.ReleasedAt) : StillRunning;
            float high = Math.Min(low + song.Share, _high[index]);
            return new SlotState(true, song.Form, song.Category, _seconds[index], released, low, high, rule.ParamA, rule.ParamB);
        }

        private static int Index(SongSlot slot)
        {
            int index = (int)slot;

            if (index < 0 || index >= SlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), slot, "There is no such song slot.");
            }

            return index;
        }

        // A rule that has let go plays only its return beat, so it changes nothing in the match.
        private bool TryGetRunning(int index, out SongRule rule)
        {
            rule = _rules[index];
            return rule != null && !rule.IsReleased;
        }

        private void Empty(int index)
        {
            _rules[index] = null;
            _seconds[index] = 0f;
        }

        private void SetBand(SongSlot slot, float low, float high)
        {
            if (!(low >= 0f && low <= high && high <= 1f))
            {
                throw new ArgumentException($"SquadConfig: the {slot} band must run from low to high within 0 to 1, but runs from {low} to {high}.", "squads");
            }

            _low[(int)slot] = low;
            _high[(int)slot] = high;
        }

        // No wasp may ever jump between two running songs, so no seed may lie in two bands.
        private void RequireApart(SongSlot first, SongSlot second)
        {
            int a = (int)first;
            int b = (int)second;

            if (_low[a] < _high[b] && _low[b] < _high[a])
            {
                throw new ArgumentException($"SquadConfig: the {first} and {second} bands overlap.", "squads");
            }
        }
    }
}
