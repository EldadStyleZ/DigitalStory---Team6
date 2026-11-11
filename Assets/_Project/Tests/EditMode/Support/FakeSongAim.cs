using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>An aim chooser that records what a song asked for and answers with a chosen target and point.</summary>
    internal sealed class FakeSongAim : ISongAim
    {
        public ISwarmTarget Target { get; set; }
        public Vector3 Point { get; set; }
        public int Calls { get; private set; }
        public AimRule LastRule { get; private set; }
        public float LastRange { get; private set; }

        public void Choose(AimRule rule, float range, out ISwarmTarget target, out Vector3 point)
        {
            Calls++;
            LastRule = rule;
            LastRange = range;
            target = Target;
            point = Point;
        }
    }
}
