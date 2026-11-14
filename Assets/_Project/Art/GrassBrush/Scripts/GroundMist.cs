using UnityEngine;

namespace DigitalStory.GrassBrush
{
    /// <summary>
    /// Holds the ground mist's sheets and makes sure the cameras draw a depth texture, which the mist needs to fade softly
    /// where it meets the ground, the trees and the water. Built by Tools/DigitalStory/Ground Mist.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Ground Mist")]
    public sealed class GroundMist : MonoBehaviour
    {
        private void OnEnable()
        {
            Camera.onPreCull += NeedDepth;
        }

        private void OnDisable()
        {
            Camera.onPreCull -= NeedDepth;
        }

        private static void NeedDepth(Camera camera)
        {
            camera.depthTextureMode |= DepthTextureMode.Depth;
        }
    }
}
