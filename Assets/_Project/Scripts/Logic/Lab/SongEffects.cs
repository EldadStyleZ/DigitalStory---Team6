using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The battle songs still working, and what they add up to. Different songs work together, because
    /// playing a second song should never cancel the first.
    /// </summary>
    public sealed class SongEffects
    {
        private readonly List<RunningSong> _running = new List<RunningSong>();

        public IReadOnlyList<RunningSong> Running => _running;
        public bool IsAnyRunning => _running.Count > 0;

        /// <summary>The most time any song has left.</summary>
        public float LongestSecondsLeft { get; private set; }

        public float DamageMultiplier { get; private set; } = 1f;

        /// <summary>The share of each hit that creatures ignore.</summary>
        public float ShieldShare { get; private set; }

        public float PaceMultiplier { get; private set; } = 1f;
        public float FinisherMultiplier { get; private set; } = 1f;
        public bool StopsEnemyHealing { get; private set; }

        /// <summary>Starts a song, or starts it again if it is already running.</summary>
        public void Add(BattleSong song)
        {
            foreach (RunningSong running in _running)
            {
                if (running.Song == song)
                {
                    running.Restart();
                    Recount();
                    return;
                }
            }

            _running.Add(new RunningSong(song));
            Recount();
        }

        public void Tick(float deltaTime)
        {
            bool hasOneEnded = false;

            for (int index = _running.Count - 1; index >= 0; index--)
            {
                _running[index].Tick(deltaTime);

                if (_running[index].SecondsLeft <= 0f)
                {
                    _running.RemoveAt(index);
                    hasOneEnded = true;
                }
            }

            LongestSecondsLeft = Longest();

            if (hasOneEnded)
            {
                Recount();
            }
        }

        public void Clear()
        {
            _running.Clear();
            Recount();
        }

        private float Longest()
        {
            float longest = 0f;

            foreach (RunningSong running in _running)
            {
                longest = Mathf.Max(longest, running.SecondsLeft);
            }

            return longest;
        }

        private void Recount()
        {
            float damage = 1f;
            float exposed = 1f;
            float pace = 1f;
            float finisher = 1f;
            bool stopsHealing = false;

            foreach (RunningSong running in _running)
            {
                SongBuff buff = running.Song.Buff;
                damage *= Multiplier(buff.DamageMultiplier);
                exposed *= 1f - buff.ShieldShare;
                pace *= Multiplier(buff.PaceMultiplier);
                finisher *= Multiplier(buff.FinisherMultiplier);
                stopsHealing |= buff.StopsEnemyHealing;
            }

            DamageMultiplier = damage;
            ShieldShare = 1f - exposed;
            PaceMultiplier = pace;
            FinisherMultiplier = finisher;
            StopsEnemyHealing = stopsHealing;
            LongestSecondsLeft = Longest();
        }

        // Zero means the song leaves this alone, not that it stops it.
        private static float Multiplier(float value) => value > 0f ? value : 1f;
    }
}
