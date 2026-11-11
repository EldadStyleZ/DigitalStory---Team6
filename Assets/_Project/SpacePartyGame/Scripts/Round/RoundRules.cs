using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>The round's clock: counts down from the round's length to 0.</summary>
    public sealed class RoundTimer
    {
        private float _remaining;

        public RoundTimer(float seconds)
        {
            _remaining = Mathf.Max(0f, seconds);
        }

        /// <summary>Seconds left, exactly.</summary>
        public float Remaining => _remaining;

        /// <summary>The number to show: whole seconds, rounded up, so it shows 50 until a full second has gone.</summary>
        public int Shown => Mathf.CeilToInt(_remaining);

        public bool IsFinished => _remaining <= 0f;

        /// <summary>Moves the clock on, and returns true only on the tick that reaches 0.</summary>
        public bool Tick(float deltaTime)
        {
            if (IsFinished)
            {
                return false;
            }

            _remaining = Mathf.Max(0f, _remaining - deltaTime);
            return IsFinished;
        }
    }

    /// <summary>How the clock looks and sounds near the end.</summary>
    public static class RoundClock
    {
        /// <summary>Whether the clock shows its warning colour: from <paramref name="warnFrom"/> seconds down.</summary>
        public static bool IsWarning(int shown, int warnFrom) => shown <= warnFrom;

        /// <summary>Whether a second ticks aloud: from <paramref name="tickFrom"/> down to 1.</summary>
        public static bool Ticks(int shown, int tickFrom) => shown >= 1 && shown <= tickFrom;
    }

    /// <summary>Who won the round.</summary>
    public static class RoundResult
    {
        /// <summary>The result when the best score is shared, or nobody played.</summary>
        public const int Draw = -1;

        /// <summary>The slot with the highest score among the first <paramref name="players"/> slots, or <see cref="Draw"/>.</summary>
        public static int WinnerOf(IReadOnlyList<int> scores, int players)
        {
            int count = Mathf.Min(players, scores.Count);
            int best = Draw;
            bool shared = false;

            for (int slot = 0; slot < count; slot++)
            {
                if (best == Draw || scores[slot] > scores[best])
                {
                    best = slot;
                    shared = false;
                }
                else if (scores[slot] == scores[best])
                {
                    shared = true;
                }
            }

            return shared ? Draw : best;
        }
    }

    /// <summary>What a player may do in a stage of the game.</summary>
    public readonly struct PlayerPermissions : IEquatable<PlayerPermissions>
    {
        public static readonly PlayerPermissions None = new PlayerPermissions(false, false, false, false, false);
        public static readonly PlayerPermissions All = new PlayerPermissions(true, true, true, true, true);

        public PlayerPermissions(bool canMove, bool canPlay, bool canCall, bool canRun, bool canBlow)
        {
            CanMove = canMove;
            CanPlay = canPlay;
            CanCall = canCall;
            CanRun = canRun;
            CanBlow = canBlow;
        }

        /// <summary>Walk and aim with the stick or WASD.</summary>
        public bool CanMove { get; }

        /// <summary>Play notes.</summary>
        public bool CanPlay { get; }

        /// <summary>Call vermin with the root note.</summary>
        public bool CanCall { get; }

        /// <summary>Run by alternating LT and RT.</summary>
        public bool CanRun { get; }

        /// <summary>Charge and release the blow.</summary>
        public bool CanBlow { get; }

        /// <summary>
        /// Everything while playing. At the round's end the winner may play notes and blow, but not move, call or
        /// run; the loser, and everyone in a draw, may do nothing. Nothing in any other stage.
        /// </summary>
        public static PlayerPermissions For(GameState state, bool isWinner)
        {
            switch (state)
            {
                case GameState.Playing:
                    return All;
                case GameState.RoundEnd when isWinner:
                    return new PlayerPermissions(false, true, false, false, true);
                default:
                    return None;
            }
        }

        public bool Equals(PlayerPermissions other) =>
            CanMove == other.CanMove && CanPlay == other.CanPlay && CanCall == other.CanCall && CanRun == other.CanRun
            && CanBlow == other.CanBlow;

        public override bool Equals(object obj) => obj is PlayerPermissions other && Equals(other);

        public override int GetHashCode() =>
            (CanMove ? 1 : 0) | (CanPlay ? 2 : 0) | (CanCall ? 4 : 0) | (CanRun ? 8 : 0) | (CanBlow ? 16 : 0);

        public override string ToString() =>
            $"Move {CanMove}, Play {CanPlay}, Call {CanCall}, Run {CanRun}, Blow {CanBlow}";
    }
}
