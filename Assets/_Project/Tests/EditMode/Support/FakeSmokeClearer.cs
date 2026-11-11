using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>A smoke stand-in that records the last clear and answers with a number a test sets.</summary>
    internal sealed class FakeSmokeClearer : ISmokeClearer
    {
        public Vector3 LastCenter { get; private set; }
        public float LastRadius { get; private set; }
        public int CallCount { get; private set; }
        public int CloudsToClear { get; set; }

        public int ClearWithin(Vector3 center, float radius)
        {
            LastCenter = center;
            LastRadius = radius;
            CallCount++;
            return CloudsToClear;
        }
    }
}
