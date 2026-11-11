using System;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The first round's lesson (plan section N): one prompt at a time, each gone once the player has done it. A step
    /// completes only while it is the current one, so the steps are learned in order; the lesson ends at once when the
    /// first wave starts. It only tells, and never blocks play: whatever the step, every input still works.
    /// </summary>
    public sealed class Tutorial
    {
        private TutorialStep _current = TutorialStep.Move;

        /// <summary>The prompt shown now, or <see cref="TutorialStep.Done"/> when the lesson is over.</summary>
        public TutorialStep Current => _current;

        /// <summary>True once the lesson is over.</summary>
        public bool IsDone => _current == TutorialStep.Done;

        /// <summary>Raised with the new step each time the lesson moves on, Done included.</summary>
        public event Action<TutorialStep> StepChanged;

        /// <summary>
        /// The player just did <paramref name="step"/>. It completes it and moves to the next step only when it is the current
        /// one; otherwise it does nothing.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The step is Done, which is no task.</exception>
        public void Complete(TutorialStep step)
        {
            if (step == TutorialStep.Done)
            {
                throw new ArgumentOutOfRangeException(nameof(step), step, "Done is the end of the lesson, not a step to complete.");
            }

            if (step != _current)
            {
                return;
            }

            SetStep(_current + 1);
        }

        /// <summary>Ends the lesson from any step: the first wave has started. Raises <see cref="StepChanged"/> once.</summary>
        public void Finish()
        {
            if (_current != TutorialStep.Done)
            {
                SetStep(TutorialStep.Done);
            }
        }

        private void SetStep(TutorialStep step)
        {
            _current = step;
            StepChanged?.Invoke(step);
        }
    }
}
