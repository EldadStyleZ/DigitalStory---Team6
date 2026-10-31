using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Shows the Piper where the rules put it.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Piper View")]
    public sealed class PiperView : MonoBehaviour
    {
        private Piper _piper;

        /// <summary>Supplies the Piper to show. Call once, before this object updates.</summary>
        public void Initialize(Piper piper) => _piper = piper;

        private void LateUpdate()
        {
            if (_piper == null)
            {
                return;
            }

            transform.SetPositionAndRotation(
                TownSpace.ToScene(_piper.Position),
                TownSpace.ToSceneRotation(_piper.Facing));
        }
    }
}
