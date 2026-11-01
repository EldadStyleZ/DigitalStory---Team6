using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Keeps the Piper in the middle of the screen.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("DigitalStory/Lab/Lab Camera")]
    public sealed class LabCamera : MonoBehaviour
    {
        private const float CameraDepth = -10f;

        private LabPiper _piper;

        /// <summary>Supplies what to follow. Call once, before this object updates.</summary>
        public void Initialize(LabPiper piper, LabArtConfig art)
        {
            _piper = piper;
            var view = GetComponent<Camera>();
            view.orthographic = true;
            view.orthographicSize = art.VisibleTiles / 2f;
            view.backgroundColor = art.WallColor;
        }

        private void LateUpdate()
        {
            if (_piper != null)
            {
                Vector2 position = _piper.Position;
                transform.position = new Vector3(position.x, position.y, CameraDepth);
            }
        }
    }
}
