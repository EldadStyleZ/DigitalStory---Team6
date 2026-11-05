using System.Collections.Generic;
using UnityEngine;

namespace PiperToy
{
    /// <summary>
    /// The path a conga line dances along: the footsteps the Piper has left behind it. Each rat in the line stands a
    /// set distance back along this path, so the rats go one after another wherever the Piper went, round corners
    /// too, instead of standing in a stiff row behind its back. The RatHerd records the Piper every physics step.
    /// </summary>
    /// <remarks>Plain C#, not a MonoBehaviour, so EditMode tests can check it without a scene.</remarks>
    public sealed class CongaLine
    {
        // How far the Piper walks before the path gets a new footstep. Smaller is smoother but keeps more points.
        private const float Step = 0.1f;

        // Oldest first. The Piper itself is the front of the line, after the newest footstep.
        private readonly List<Vector2> _footsteps = new List<Vector2>();
        private Vector2 _piper;
        private float _length;

        /// <summary>
        /// Starts a new line as a straight path behind the Piper, so the rats have places to run to before the Piper
        /// takes a step.
        /// </summary>
        /// <param name="back">Which way the line starts from the Piper.</param>
        /// <param name="length">How long the line is, so the path keeps that much of the Piper's footsteps.</param>
        public void Begin(Vector2 piper, Vector2 back, float length)
        {
            _footsteps.Clear();
            _footsteps.Add(piper + (back.normalized * length));
            _footsteps.Add(piper);
            _piper = piper;
            _length = length;
        }

        /// <summary>Adds where the Piper is now and forgets the footsteps behind the last rat.</summary>
        public void Record(Vector2 piper)
        {
            _piper = piper;

            if (_footsteps.Count == 0 || Vector2.Distance(piper, _footsteps[_footsteps.Count - 1]) >= Step)
            {
                _footsteps.Add(piper);
            }

            ForgetTheOldFootsteps();
        }

        /// <summary>The spot this far back along the path from the Piper. Past the end of the line, its last point.</summary>
        public Vector2 SpotBehind(float distance)
        {
            Vector2 here = _piper;

            for (int index = _footsteps.Count - 1; index >= 0; index--)
            {
                Vector2 next = _footsteps[index];
                float stretch = Vector2.Distance(here, next);

                if (stretch >= distance && stretch > 0f)
                {
                    return Vector2.Lerp(here, next, distance / stretch);
                }

                distance -= stretch;
                here = next;
            }

            return here;
        }

        // Walks back from the Piper; once the path is as long as the line, every older footstep can go.
        private void ForgetTheOldFootsteps()
        {
            float walked = 0f;
            Vector2 here = _piper;

            for (int index = _footsteps.Count - 1; index > 0; index--)
            {
                walked += Vector2.Distance(here, _footsteps[index]);
                here = _footsteps[index];

                if (walked >= _length)
                {
                    _footsteps.RemoveRange(0, index);
                    return;
                }
            }
        }
    }
}
