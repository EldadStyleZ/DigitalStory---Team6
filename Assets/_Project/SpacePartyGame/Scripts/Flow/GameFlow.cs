using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>The stages of a game, from the start menu round to the leaderboard.</summary>
    public enum GameState
    {
        StartMenu,
        Join,
        Countdown,
        Playing,
        Paused,
        RoundEnd,
        NameEntry,
        Leaderboard,
    }

    /// <summary>
    /// The game's current stage and the rules for moving between stages. A plain C# class, so the rules are tested
    /// without a scene; <see cref="GameFlowManager"/> drives it in the game.
    /// </summary>
    public sealed class GameFlow
    {
        /// <summary>How many players can play at once.</summary>
        public const int MaxPlayers = 4;

        private static readonly HashSet<(GameState, GameState)> Allowed = new HashSet<(GameState, GameState)>
        {
            (GameState.StartMenu, GameState.Join),
            (GameState.Join, GameState.Countdown),
            (GameState.Join, GameState.StartMenu),
            (GameState.Countdown, GameState.Playing),
            (GameState.Playing, GameState.Paused),
            (GameState.Playing, GameState.RoundEnd),
            (GameState.Paused, GameState.Playing),
            (GameState.Paused, GameState.StartMenu),
            (GameState.RoundEnd, GameState.NameEntry),
            (GameState.RoundEnd, GameState.Leaderboard),
            (GameState.NameEntry, GameState.Leaderboard),
            (GameState.Leaderboard, GameState.StartMenu),
            (GameState.Leaderboard, GameState.Join),

            // With the join screen off, the start menu goes straight to the countdown.
            (GameState.StartMenu, GameState.Countdown),

            // With the leaderboard off, the round end goes straight back to the start menu.
            (GameState.RoundEnd, GameState.StartMenu),

            // Esc or Start resets the game from the countdown or from play.
            (GameState.Countdown, GameState.StartMenu),
            (GameState.Playing, GameState.StartMenu),
        };

        /// <summary>The current stage.</summary>
        public GameState State { get; private set; } = GameState.StartMenu;

        /// <summary>Raised after every move, with the stage left and the stage entered.</summary>
        public event Action<GameState, GameState> Changed;

        /// <summary>Whether the game may go straight from one stage to another.</summary>
        public static bool CanMove(GameState from, GameState to) => Allowed.Contains((from, to));

        /// <summary>Whether a match may start with this many players. It needs two, or one when testing alone.</summary>
        public static bool CanStartMatch(int joined, bool allowSinglePlayer) =>
            joined <= MaxPlayers && joined >= (allowSinglePlayer ? 1 : 2);

        /// <summary>Whether Esc or Start may reset the game to the start menu now: any time during a game.</summary>
        public static bool CanReset(GameState state) =>
            state == GameState.Countdown || state == GameState.Playing || state == GameState.Paused
            || state == GameState.RoundEnd;

        /// <summary>The stage the start menu leads to: the join screen, or straight to the countdown without it.</summary>
        public static GameState AfterStartMenu(bool useJoinScreen) =>
            useJoinScreen ? GameState.Join : GameState.Countdown;

        /// <summary>The stage a round's end leads to: the leaderboard, or back to the start menu without it.</summary>
        public static GameState AfterRoundEnd(bool useLeaderboard) =>
            useLeaderboard ? GameState.Leaderboard : GameState.StartMenu;

        /// <summary>Moves to a stage if the rules allow it, and says whether it did.</summary>
        public bool TryMoveTo(GameState to)
        {
            if (!CanMove(State, to))
            {
                return false;
            }

            GameState from = State;
            State = to;
            Changed?.Invoke(from, to);
            return true;
        }
    }

    /// <summary>
    /// The fixed two-player setup used while the join screen is off: P1 always plays on the keyboard and mouse, and
    /// P2 on a gamepad when one is connected. P2 still exists without one, standing still until a gamepad arrives.
    /// </summary>
    public static class FixedPlayers
    {
        /// <summary>How many players the fixed setup spawns.</summary>
        public const int Count = 2;

        /// <summary>The control scheme names in <c>SpacePartyControls.inputactions</c>.</summary>
        public const string KeyboardScheme = "Keyboard&Mouse";
        public const string GamepadScheme = "Gamepad";

        /// <summary>The control scheme a slot plays with: keyboard for P1, gamepad for P2.</summary>
        public static string SchemeFor(int slot) => slot == 0 ? KeyboardScheme : GamepadScheme;

        /// <summary>Whether a slot is waiting for a gamepad: true for P2 while it has none.</summary>
        public static bool WantsGamepad(int slot, bool hasGamepad) => SchemeFor(slot) == GamepadScheme && !hasGamepad;
    }

    /// <summary>Counts down whole seconds, such as 3, 2, 1.</summary>
    public sealed class CountdownTimer
    {
        private float _remaining;

        public CountdownTimer(int seconds)
        {
            _remaining = seconds;
        }

        /// <summary>The number to show: 3 for the first second, then 2, then 1, and 0 once finished.</summary>
        public int Shown => Mathf.CeilToInt(_remaining);

        public bool IsFinished => _remaining <= 0f;

        /// <summary>Moves the count on, and returns true only on the tick that finishes it.</summary>
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

    /// <summary>Which action map players use in each stage, so the flow and the players stay in step.</summary>
    public static class PlayerInputModes
    {
        public const string JoinMap = "Join";
        public const string PlayerMap = "Player";
        public const string UIMap = "UI";

        /// <summary>The map players use in a stage, or null when their input is off.</summary>
        public static string MapFor(GameState state)
        {
            switch (state)
            {
                case GameState.Join:
                    return JoinMap;
                // The countdown and the round end use the player map too, so Esc or Start can reset the game;
                // PlayerPermissions decides what each player may do then.
                case GameState.Countdown:
                case GameState.RoundEnd:
                case GameState.Playing:
                    return PlayerMap;
                case GameState.Paused:
                case GameState.NameEntry:
                case GameState.Leaderboard:
                    return UIMap;
                default:
                    return null;
            }
        }

        /// <summary>Whether players may move and play notes in a stage.</summary>
        // public static bool CanMoveAndPlay(GameState state) => MapFor(state) == PlayerMap;
        public static bool CanMoveAndPlay(GameState state) => state == GameState.Playing;
    }
}
