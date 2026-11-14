using System;

namespace DigitalStory.Logic
{
    /// <summary>One kill in a chain: how many kills the chain holds now, how many semitones its sound climbs, and the milestone it reached (0 for none).</summary>
    public readonly struct KillChainStep
    {
        public KillChainStep(int count, int semitones, int milestone)
        {
            Count = count;
            Semitones = semitones;
            Milestone = milestone;
        }

        /// <summary>The kills in the chain, this one included.</summary>
        public int Count { get; }

        /// <summary>How many semitones above the first kill this kill's sound plays.</summary>
        public int Semitones { get; }

        /// <summary>The milestone this kill reached (for example 3, 6 or 10), or 0 when it reached none.</summary>
        public int Milestone { get; }
    }

    /// <summary>
    /// The multi-kill chain (plan section Y, after Peggle and the Mario stomp chain). A kill within the window of the last one
    /// extends the chain; a longer gap starts a new one. Each kill in a chain climbs one semitone, up to a top, and the chain
    /// reports each milestone count once as it passes it. Feedback only: it changes no game rule.
    /// </summary>
    public sealed class KillChain
    {
        private readonly float _windowSeconds;
        private readonly int _topSemitones;
        private readonly int[] _milestones;
        private int _count;
        private float _lastKill;

        /// <param name="windowSeconds">The most seconds between two kills of one chain. More than 0.</param>
        /// <param name="topSemitones">The most semitones a kill climbs. 0 or more.</param>
        /// <param name="milestones">The counts that stamp, for example 3, 6 and 10. Kept, not copied.</param>
        /// <exception cref="ArgumentOutOfRangeException">The window is not more than 0.</exception>
        /// <exception cref="ArgumentNullException">The milestones are missing.</exception>
        public KillChain(float windowSeconds, int topSemitones, int[] milestones)
        {
            if (!(windowSeconds > 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(windowSeconds), windowSeconds, "A kill chain needs a window above 0 seconds.");
            }

            _windowSeconds = windowSeconds;
            _topSemitones = Math.Max(0, topSemitones);
            _milestones = milestones ?? throw new ArgumentNullException(nameof(milestones), "Give the kill chain its milestones; an empty array means none.");
        }

        /// <summary>The kills in the current chain, or 0 before the first.</summary>
        public int Count => _count;

        /// <summary>True while a kill now would still extend the chain.</summary>
        public bool IsRunning(float now)
        {
            return _count > 0 && now - _lastKill <= _windowSeconds;
        }

        /// <summary>Counts a kill at the given time (seconds, never going back) and returns where the chain stands.</summary>
        public KillChainStep Kill(float now)
        {
            _count = IsRunning(now) ? _count + 1 : 1;
            _lastKill = now;
            int milestone = 0;
            for (int index = 0; index < _milestones.Length; index++)
            {
                if (_milestones[index] == _count)
                {
                    milestone = _count;
                    break;
                }
            }

            return new KillChainStep(_count, Math.Min(_count - 1, _topSemitones), milestone);
        }

        /// <summary>Ends the chain, for example when a match starts again.</summary>
        public void Reset()
        {
            _count = 0;
        }
    }
}
