namespace DigitalStory.Logic
{
    /// <summary>
    /// The scene's single say over the time scale (plan section AC): requests in priority order, the settings panel's pause
    /// over the brief slow-down (when the giant forms and a wave is cleared) over the scale it found. The root writes the time
    /// scale only from <see cref="Scale"/>. The slow-down's real seconds do not run while paused, so closing the panel goes
    /// back into what was left of it. A time scale someone else set meanwhile (a test, the benchmark) becomes the scale found
    /// and ends a running slow-down, as the root always kept such a change.
    /// </summary>
    public sealed class TimeScaleRule
    {
        // Two time scales closer than this are the same one, as Mathf.Approximately would have it.
        private const float Same = 1e-5f;

        private float _found = 1f;
        private float _written;
        private bool _hasWritten;
        private float _slowShare = 1f;
        private float _slowSecondsLeft;

        /// <summary>True while the pause holds time still.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>True while the slow-down runs (paused or not).</summary>
        public bool IsSlowing { get; private set; }

        /// <summary>The time scale the requests ask for now: 0 while paused, the slowed scale while slowing, else the one found.</summary>
        public float Scale => IsPaused ? 0f : (IsSlowing ? _found * _slowShare : _found);

        /// <summary>Starts or restarts the slow-down: the scale found times <paramref name="share"/> for that many real seconds.</summary>
        public void StartSlow(float share, float realSeconds, float current)
        {
            Adopt(current);
            if (!(realSeconds > 0f) || !(share > 0f))
            {
                return;
            }

            IsSlowing = true;
            _slowShare = share;
            _slowSecondsLeft = realSeconds;
        }

        /// <summary>Pauses time, or lets it run again.</summary>
        public void Pause(bool isPaused, float current)
        {
            Adopt(current);
            IsPaused = isPaused;
        }

        /// <summary>Moves the slow-down on by real seconds; it does not run while paused.</summary>
        public void Step(float realSeconds, float current)
        {
            Adopt(current);
            if (!IsSlowing || IsPaused)
            {
                return;
            }

            _slowSecondsLeft -= realSeconds;
            if (_slowSecondsLeft <= 0f)
            {
                IsSlowing = false;
            }
        }

        /// <summary>Records the time scale the root wrote from <see cref="Scale"/>, so a change by someone else can be told apart.</summary>
        public void Wrote(float scale)
        {
            _written = scale;
            _hasWritten = true;
        }

        /// <summary>
        /// Ends every request, for a scene that unloads: returns the scale to leave behind, the one found, unless someone else
        /// changed it since the last write.
        /// </summary>
        public float EndAll(float current)
        {
            Adopt(current);
            IsPaused = false;
            IsSlowing = false;
            return _found;
        }

        private void Adopt(float current)
        {
            bool isChangedElsewhere = !_hasWritten || System.Math.Abs(current - _written) > Same;
            if (!isChangedElsewhere)
            {
                return;
            }

            // Only a scale the requests did not set is the one found: the first call's, or someone else's.
            _found = current;
            IsSlowing = false;
            _hasWritten = true;
            _written = current;
        }
    }
}
