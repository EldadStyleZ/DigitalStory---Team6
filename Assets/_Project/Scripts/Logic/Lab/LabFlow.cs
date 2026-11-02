using System;

namespace DigitalStory.Logic
{
    /// <summary>Moves the lab between its phases, and ignores any move that does not apply.</summary>
    public sealed class LabFlow
    {
        /// <summary>Raised with the new phase after each move.</summary>
        public event Action<LabPhase> Changed;

        public LabPhase Phase { get; private set; } = LabPhase.Playing;

        public bool IsPlaying => Phase == LabPhase.Playing;

        public void TogglePause()
        {
            if (Phase == LabPhase.Paused)
            {
                Move(LabPhase.Paused, LabPhase.Playing);
            }
            else
            {
                Move(LabPhase.Playing, LabPhase.Paused);
            }
        }

        /// <summary>Stops the game until the player picks.</summary>
        public void OfferChoice() => Move(LabPhase.Playing, LabPhase.Choosing);

        public void FinishChoice() => Move(LabPhase.Choosing, LabPhase.Playing);

        public void Die() => Move(LabPhase.Playing, LabPhase.Dead);

        /// <summary>Returns to the maze after choosing to continue.</summary>
        public void Revive() => Move(LabPhase.Dead, LabPhase.Playing);

        public void Win() => Move(LabPhase.Playing, LabPhase.Won);

        private void Move(LabPhase from, LabPhase to)
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
