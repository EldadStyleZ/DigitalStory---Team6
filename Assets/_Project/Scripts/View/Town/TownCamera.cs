using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Follows the Piper and stops at the edges of the town.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("DigitalStory/Town Camera")]
    public sealed class TownCamera : MonoBehaviour
    {
        private const float CameraDepth = -10f;

        private Camera _camera;
        private Piper _piper;
        private TownConfig _town;
        private TownArtConfig _art;

        /// <summary>Supplies what to follow. Call once, before this object starts.</summary>
        public void Initialize(Piper piper, TownConfig town, TownArtConfig art)
        {
            _piper = piper;
            _town = town;
            _art = art;
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
        }

        private void LateUpdate()
        {
            if (_piper == null)
            {
                return;
            }

            float visibleHeight = _art.VisibleTownHeight;
            _camera.orthographicSize = TownSpace.ToSceneLength(visibleHeight) / 2f;

            float visibleWidth = visibleHeight * _camera.aspect;
            float x = Follow(_piper.Position.x, visibleWidth, _town.Width);
            float y = Follow(_piper.Position.y, visibleHeight, _town.Height);

            Vector3 position = TownSpace.ToScene(new Vector2(x, y));
            position.z = CameraDepth;
            transform.position = position;
        }

        private static float Follow(float target, float visible, float town)
        {
            return visible >= town ? town / 2f : Mathf.Clamp(target, visible / 2f, town - (visible / 2f));
        }
    }
}
