using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The timing of one of the giant's hand launches: the arm thrusts forward for <see cref="ColossusConfig.FistThrustSeconds"/>,
    /// the wasps of its hand and forearm fly to the target as one fist for the flight the session gives it, hit, and stream back
    /// while the arm grows again over <see cref="ColossusConfig.FistRegrowSeconds"/>. Until then the arm is missing and the hand
    /// cannot launch. Each hand has its own, so the two never wait for each other. Where the fist flies, and what it hits, the
    /// session decides as <see cref="Tick"/> reports each step.
    /// </summary>
    public sealed class ColossusFist
    {
        private readonly ColossusConfig _config;

        public ColossusFist(ColossusConfig config)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
        }

        /// <summary>Where the hand is in its launch.</summary>
        public FistPhase Phase { get; private set; }

        /// <summary>Seconds since the current phase began; 0 while ready.</summary>
        public float PhaseSeconds { get; private set; }

        /// <summary>Seconds the current fist flies from the arm to its target, from its launch; 0 before the first launch.</summary>
        public float FlightSeconds { get; private set; }

        /// <summary>True while the arm is whole and the hand can launch.</summary>
        public bool IsReady => Phase == FistPhase.Ready;

        /// <summary>Seconds since the fist left the arm, while it flies and while the arm grows back; -1 otherwise.</summary>
        public float SecondsSinceLaunch
        {
            get
            {
                switch (Phase)
                {
                    case FistPhase.Flight:
                        return PhaseSeconds;
                    case FistPhase.Regrow:
                        return FlightSeconds + PhaseSeconds;
                    default:
                        return -1f;
                }
            }
        }

        /// <summary>
        /// How much of the wait is left, for the HUD: 0 ready, 1 from the thrust until the hit, then falling to 0 as the arm grows
        /// back.
        /// </summary>
        public float Cooldown01
        {
            get
            {
                switch (Phase)
                {
                    case FistPhase.Thrust:
                    case FistPhase.Flight:
                        return 1f;
                    case FistPhase.Regrow:
                        return Mathf.Clamp01(1f - (PhaseSeconds / _config.FistRegrowSeconds));
                    default:
                        return 0f;
                }
            }
        }

        /// <summary>Starts the thrust. False, doing nothing, unless the arm is whole.</summary>
        public bool TryThrust()
        {
            if (!IsReady)
            {
                return false;
            }

            Phase = FistPhase.Thrust;
            PhaseSeconds = 0f;
            return true;
        }

        /// <summary>
        /// Gives the fist that has just left (<see cref="FistStep.Launch"/>) its flight to the target, never shorter than
        /// <see cref="ColossusConfig.FistLeastFlightSeconds"/>. Does nothing unless the fist is in flight.
        /// </summary>
        public void Fly(float seconds)
        {
            if (Phase != FistPhase.Flight)
            {
                return;
            }

            FlightSeconds = float.IsNaN(seconds) ? _config.FistLeastFlightSeconds : Mathf.Max(seconds, _config.FistLeastFlightSeconds);
        }

        /// <summary>
        /// Moves the launch on, at most one step a tick. A fist given no flight with <see cref="Fly"/> flies the least flight.
        /// </summary>
        public FistStep Tick(float deltaTime)
        {
            if (!(deltaTime > 0f) || Phase == FistPhase.Ready)
            {
                return FistStep.None;
            }

            PhaseSeconds += deltaTime;
            switch (Phase)
            {
                case FistPhase.Thrust:
                    return Advance(_config.FistThrustSeconds, FistPhase.Flight, FistStep.Launch);
                case FistPhase.Flight:
                    return Advance(FlightSeconds, FistPhase.Regrow, FistStep.Impact);
                default:
                    return Advance(_config.FistRegrowSeconds, FistPhase.Ready, FistStep.Regrown);
            }
        }

        /// <summary>
        /// Makes the arm whole at once and forgets the launch: a fist in flight never hits. The session calls it when a giant forms
        /// and when it ends, so nothing of one giant reaches the next or outlives it.
        /// </summary>
        public void Reset()
        {
            Phase = FistPhase.Ready;
            PhaseSeconds = 0f;
            FlightSeconds = 0f;
        }

        // The time over the phase's end runs on in the next phase, so the steps keep their times whatever the tick.
        private FistStep Advance(float phaseSeconds, FistPhase next, FistStep step)
        {
            if (PhaseSeconds < phaseSeconds)
            {
                return FistStep.None;
            }

            float over = PhaseSeconds - phaseSeconds;
            Phase = next;
            PhaseSeconds = next == FistPhase.Ready ? 0f : over;
            if (next == FistPhase.Flight)
            {
                FlightSeconds = _config.FistLeastFlightSeconds;
            }

            return step;
        }
    }
}
