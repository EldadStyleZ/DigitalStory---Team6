using System;

namespace DigitalStory.Logic
{
    /// <summary>Moves the game between its phases, and ignores any move that does not apply.</summary>
    public sealed class GameFlow
    {
        /// <summary>Raised with the new phase after each move.</summary>
        public event Action<GamePhase> Changed;

        public GamePhase Phase { get; private set; } = GamePhase.Title;

        public bool IsPlaying => Phase == GamePhase.Playing;

        public void Begin() => Move(GamePhase.Title, GamePhase.Playing);

        public void TogglePause()
        {
            if (Phase == GamePhase.Paused)
            {
                Move(GamePhase.Paused, GamePhase.Playing);
            }
            else
            {
                Pause();
            }
        }

        /// <summary>Pauses a night in progress, and does nothing otherwise.</summary>
        public void Pause() => Move(GamePhase.Playing, GamePhase.Paused);

        public void End() => Move(GamePhase.Playing, GamePhase.Ended);

        private void Move(GamePhase from, GamePhase to)
        {
            if (Phase != from)
            {
                return;
            }

            Phase = to;
            Changed?.Invoke(to);
        }
    }
}
