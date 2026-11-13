using System;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// What the swarm's core did over the last moments, kept at a fixed time step so it reads the same at any frame rate:
    /// how many metres the core has travelled since each moment, how far the swarm was wound into the figure-eight then,
    /// and how far the eight had swung. Each wasp of the free swarm follows the core with its own delay through this
    /// history (plan section Q), so a start, a stop, a turn or a change of shape travels down the swarm as a wave.
    /// </summary>
    public sealed class CoreHistory
    {
        private readonly int _count;
        private readonly double _step;

        // The samples at whole steps of time, in a ring: the newest is at _newest, and older ones step back from it.
        private readonly double[] _travelled;
        private readonly float[] _wound;
        private readonly double[] _swing;
        private int _newest;
        private long _stepIndex;

        // Now, which lies from 0 to one step after the newest sample, and the state now.
        private double _now;
        private double _travelledNow;
        private float _woundNow;
        private double _swingNow;

        /// <summary>Keeps <paramref name="sampleCount"/> samples, one every <paramref name="stepSeconds"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException">Fewer than two samples, or a step that is not above zero.</exception>
        public CoreHistory(int sampleCount, float stepSeconds)
        {
            if (sampleCount < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleCount), sampleCount, "A history needs at least two samples.");
            }

            if (!(stepSeconds > 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(stepSeconds), stepSeconds, "The step must be above zero.");
            }

            _count = sampleCount;
            _step = stepSeconds;
            _travelled = new double[sampleCount];
            _wound = new float[sampleCount];
            _swing = new double[sampleCount];
            Reset(0f, 0f);
        }

        /// <summary>How many samples <see cref="CopyTo"/> writes.</summary>
        public int SampleCount => _count;

        /// <summary>Seconds from one sample to the next.</summary>
        public float StepSeconds => (float)_step;

        /// <summary>Seconds from now back to the oldest sample.</summary>
        public float SpanSeconds => (float)((_count - 1) * _step);

        /// <summary>Forgets the past: from now on the core reads as having stood still forever, wound and swung this far.</summary>
        public void Reset(float wound, float swing)
        {
            for (int index = 0; index < _count; index++)
            {
                _travelled[index] = 0.0;
                _wound[index] = wound;
                _swing[index] = swing;
            }

            _newest = 0;
            _stepIndex = 0;
            _now = 0.0;
            _travelledNow = 0.0;
            _woundNow = wound;
            _swingNow = swing;
        }

        /// <summary>
        /// Adds one frame: <paramref name="deltaSeconds"/> of time in which the core's odometer reached
        /// <paramref name="travelledMetres"/> and the wound and swing reached the given values. Each falls on the samples
        /// in between along a straight line, so frames of any length give the same samples. A frame of no time changes only
        /// the state now. Allocates nothing.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The time is negative.</exception>
        public void Record(float deltaSeconds, double travelledMetres, float wound, float swing)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time cannot run backwards.");
            }

            if (deltaSeconds > 0f)
            {
                double start = _now;
                double end = _now + deltaSeconds;
                long lastStep = (long)Math.Floor(end / _step);

                // Only the newest samples fit; a frame longer than the whole span skips the ones that would drop out.
                long firstStep = Math.Max(_stepIndex + 1, lastStep - _count + 1);
                for (long step = firstStep; step <= lastStep; step++)
                {
                    double share = (step * _step - start) / deltaSeconds;
                    _newest = (_newest + 1) % _count;
                    _travelled[_newest] = _travelledNow + (travelledMetres - _travelledNow) * share;
                    _wound[_newest] = (float)(_woundNow + (wound - _woundNow) * share);
                    _swing[_newest] = _swingNow + (swing - _swingNow) * share;
                }

                _stepIndex = Math.Max(_stepIndex, lastStep);
                _now = end;
            }

            _travelledNow = travelledMetres;
            _woundNow = wound;
            _swingNow = swing;
        }

        /// <summary>
        /// Writes the moments now, one step ago, two steps ago and so on, <see cref="SampleCount"/> of them: x the metres the
        /// core has travelled since then, y how far the swarm was wound then, z the swing then less the swing now, w 0.
        /// Moments before the last <see cref="Reset"/> read as the reset. Allocates nothing.
        /// </summary>
        /// <exception cref="ArgumentNullException">The target is missing.</exception>
        /// <exception cref="ArgumentException">The target holds fewer than <see cref="SampleCount"/> elements.</exception>
        public void CopyTo(Vector4[] target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (target.Length < _count)
            {
                throw new ArgumentException($"The target needs room for {_count} samples.", nameof(target));
            }

            target[0] = new Vector4(0f, _woundNow, 0f, 0f);

            // A moment j steps back lies between the samples j - 1 and j steps behind the newest, this far toward the older.
            double sinceNewest = _now - (_stepIndex * _step);
            float towardOlder = (float)(1.0 - Mathf.Clamp01((float)(sinceNewest / _step)));
            for (int back = 1; back < _count; back++)
            {
                int newer = Ring(back - 1);
                int older = Ring(back);
                double travelled = _travelled[newer] + (_travelled[older] - _travelled[newer]) * towardOlder;
                float wound = _wound[newer] + (_wound[older] - _wound[newer]) * towardOlder;
                double swing = _swing[newer] + (_swing[older] - _swing[newer]) * towardOlder;
                target[back] = new Vector4((float)(_travelledNow - travelled), wound, (float)(swing - _swingNow), 0f);
            }
        }

        private int Ring(int stepsBack) => (_newest - stepsBack + _count) % _count;
    }
}
