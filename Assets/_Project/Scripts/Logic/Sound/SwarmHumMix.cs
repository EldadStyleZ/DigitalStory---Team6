using System;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The swarm's hum (plan section AC): its volume grows with the swarm (none without wasps, a quiet share for a few, full at
    /// the full swarm, on a square-root curve so a young swarm is already heard), its pitch rises with the core's speed, and
    /// while a strike runs or the giant stands it cross-fades to an angrier buzz, at equal power so the swap does not dip. Every
    /// change is eased, so the hum never steps. Feedback only.
    /// </summary>
    public sealed class SwarmHumMix
    {
        private readonly float _fullWasps;
        private readonly float _quietShare;
        private readonly float _restPitch;
        private readonly float _fastPitch;
        private readonly float _fastSpeed;
        private readonly float _followSeconds;
        private readonly float _angryFadeSeconds;
        private float _volume;

        /// <param name="fullWasps">The swarm at which the hum is full. Above 0.</param>
        /// <param name="quietShare">The share of the full volume for a swarm of one wasp, 0 to 1.</param>
        /// <param name="restPitch">The pitch while the core stands still. Above 0.</param>
        /// <param name="fastPitch">The pitch at <paramref name="fastSpeed"/> and above. Above 0.</param>
        /// <param name="fastSpeed">Metres a second that give the top pitch. Above 0.</param>
        /// <param name="followSeconds">Seconds the volume and pitch take to follow, to about two thirds. Above 0.</param>
        /// <param name="angryFadeSeconds">Seconds the cross-fade to the angry buzz, or back, takes in all. Above 0.</param>
        /// <exception cref="ArgumentOutOfRangeException">A number is out of its range.</exception>
        public SwarmHumMix(
            float fullWasps, float quietShare, float restPitch, float fastPitch, float fastSpeed, float followSeconds,
            float angryFadeSeconds)
        {
            if (!(fullWasps > 0f) || !(restPitch > 0f) || !(fastPitch > 0f) || !(fastSpeed > 0f) || !(followSeconds > 0f)
                || !(angryFadeSeconds > 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(fullWasps), "The hum's full swarm, pitches, speed and times must be above 0.");
            }

            if (!(quietShare >= 0f && quietShare <= 1f))
            {
                throw new ArgumentOutOfRangeException(nameof(quietShare), quietShare, "The quiet share must be 0 to 1.");
            }

            _fullWasps = fullWasps;
            _quietShare = quietShare;
            _restPitch = restPitch;
            _fastPitch = fastPitch;
            _fastSpeed = fastSpeed;
            _followSeconds = followSeconds;
            _angryFadeSeconds = angryFadeSeconds;
            Pitch = restPitch;
        }

        /// <summary>The hum's volume now, calm and angry together, 0 to 1, before the library's volumes.</summary>
        public float Volume => _volume;

        /// <summary>The calm hum's volume now, 0 to 1, before the library's volumes.</summary>
        public float CalmVolume { get; private set; }

        /// <summary>The angry buzz's volume now, 0 to 1, before the library's volumes.</summary>
        public float AngryVolume { get; private set; }

        /// <summary>The playback rate of both loops now.</summary>
        public float Pitch { get; private set; }

        /// <summary>How far the cross-fade has gone to the angry buzz: 0 calm, 1 angry.</summary>
        public float AngryShare { get; private set; }

        /// <summary>The volume the hum heads for with this many wasps, 0 to 1.</summary>
        public float VolumeFor(int wasps)
        {
            if (wasps <= 0)
            {
                return 0f;
            }

            float grown = (float)Math.Sqrt(Math.Min(1f, wasps / _fullWasps));
            return _quietShare + ((1f - _quietShare) * grown);
        }

        /// <summary>The pitch the hum heads for at this speed, in metres a second.</summary>
        public float PitchFor(float speed)
        {
            float share = Math.Max(0f, Math.Min(1f, speed / _fastSpeed));
            return _restPitch + ((_fastPitch - _restPitch) * share);
        }

        /// <summary>Moves on by <paramref name="deltaTime"/> seconds (real time) toward the hum of this swarm, speed and mood.</summary>
        public void Step(int wasps, float speed, bool isAngry, float deltaTime)
        {
            if (!(deltaTime > 0f))
            {
                return;
            }

            float follow = 1f - (float)Math.Exp(-deltaTime / _followSeconds);
            _volume += (VolumeFor(wasps) - _volume) * follow;
            Pitch += (PitchFor(speed) - Pitch) * follow;

            float fade = deltaTime / _angryFadeSeconds;
            AngryShare = isAngry ? Math.Min(1f, AngryShare + fade) : Math.Max(0f, AngryShare - fade);
            double angle = AngryShare * Math.PI * 0.5d;
            CalmVolume = _volume * (float)Math.Cos(angle);
            AngryVolume = _volume * (float)Math.Sin(angle);
        }
    }
}
