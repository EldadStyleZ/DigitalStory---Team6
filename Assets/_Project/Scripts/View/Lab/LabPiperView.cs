using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Shows the Piper where the lab's rules put it.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("DigitalStory/Lab/Lab Piper View")]
    public sealed class LabPiperView : MonoBehaviour
    {
        private LabPiper _piper;

        /// <summary>Supplies the Piper to show. Call once, before this object starts.</summary>
        public void Initialize(LabPiper piper, LabArtConfig art)
        {
            _piper = piper;
            float drawnSize = GetComponent<SpriteRenderer>().sprite.bounds.size.x;
            transform.localScale = Vector3.one * (art.PiperSize / drawnSize);
        }

        private void LateUpdate()
        {
            if (_piper != null)
            {
                transform.SetPositionAndRotation(_piper.Position, Quaternion.Euler(0f, 0f, _piper.Facing));
            }
        }
    }
}
