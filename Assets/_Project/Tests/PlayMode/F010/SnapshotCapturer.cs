#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>
    /// Test helper for HiveDefenseSnapshotTests: renders a camera into a PNG at the very end of LateUpdate.
    /// </summary>
    /// <remarks>
    /// A test coroutine resumes after Update and before LateUpdate, but SwarmGpuView submits its
    /// Graphics.RenderMeshIndirect in LateUpdate (execution order 100), so a render from the coroutine misses every
    /// wasp. Running last (order 32000) puts this frame's wasps, camera follow and hive bars in the picture.
    /// It lives in its own file so Unity gives it a MonoScript and honours its execution order.
    /// </remarks>
    [DefaultExecutionOrder(32000)]
    [AddComponentMenu("")]
    public sealed class SnapshotCapturer : MonoBehaviour
    {
        private const int GridColumns = 16;
        private const int GridRows = 9;
        private const float FlatTolerance = 0.02f;

        private Camera _camera;
        private RenderTexture _target;
        private Texture2D _image;
        private string _pendingPath;
        private bool _hasPose;
        private Vector3 _posePosition;
        private Quaternion _poseRotation;
        private RenderTexture _overlayTarget;
        private string _overlayReadPath;

        /// <summary>True until the requested picture has been written.</summary>
        public bool IsBusy => _pendingPath != null || _overlayReadPath != null;

        /// <summary>True when the last picture was a single flat colour.</summary>
        public bool LastWasFlat { get; private set; }

        /// <summary>Supplies the camera and the picture size. Call once, right after adding the component.</summary>
        public void Initialize(Camera camera, int width, int height)
        {
            _camera = camera;
            _target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            _image = new Texture2D(width, height, TextureFormat.RGB24, false);
        }

        /// <summary>Writes the camera's view to <paramref name="path"/> at the end of this frame.</summary>
        public void RequestCapture(string path)
        {
            _pendingPath = path;
            _hasPose = false;
        }

        /// <summary>Writes the view from a temporary camera pose, and puts the camera back afterwards.</summary>
        public void RequestCapture(string path, Vector3 position, Quaternion rotation)
        {
            _pendingPath = path;
            _hasPose = true;
            _posePosition = position;
            _poseRotation = rotation;
        }

        /// <summary>Renders the camera into <paramref name="target"/> every frame, so a UI panel can draw over it.</summary>
        public void BeginOverlay(RenderTexture target) => _overlayTarget = target;

        /// <summary>Saves what the overlay target holds (last frame's camera plus the UI drawn over it).</summary>
        public void RequestOverlayRead(string path) => _overlayReadPath = path;

        /// <summary>Stops rendering into the overlay target.</summary>
        public void EndOverlay()
        {
            _overlayTarget = null;
            _overlayReadPath = null;
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                return;
            }

            if (_overlayTarget != null)
            {
                if (_overlayReadPath != null)
                {
                    Save(_overlayTarget, _overlayReadPath);
                    _overlayReadPath = null;
                }

                Render(_overlayTarget);
                return;
            }

            if (_pendingPath == null)
            {
                return;
            }

            Vector3 savedPosition = _camera.transform.position;
            Quaternion savedRotation = _camera.transform.rotation;
            if (_hasPose)
            {
                _camera.transform.SetPositionAndRotation(_posePosition, _poseRotation);
            }

            Render(_target);
            _camera.transform.SetPositionAndRotation(savedPosition, savedRotation);
            Save(_target, _pendingPath);
            _pendingPath = null;
        }

        private void OnDestroy()
        {
            if (_target != null)
            {
                _target.Release();
                Destroy(_target);
            }

            if (_image != null)
            {
                Destroy(_image);
            }
        }

        private void Render(RenderTexture target)
        {
            RenderTexture previousTarget = _camera.targetTexture;
            try
            {
                _camera.targetTexture = target;
                _camera.Render();
            }
            finally
            {
                _camera.targetTexture = previousTarget;
            }
        }

        private void Save(RenderTexture source, string path)
        {
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                RenderTexture.active = source;
                _image.ReadPixels(new Rect(0, 0, _image.width, _image.height), 0, 0);
                _image.Apply();
            }
            finally
            {
                RenderTexture.active = previousActive;
            }

            File.WriteAllBytes(path, _image.EncodeToPNG());
            LastWasFlat = IsFlat(_image);
            Debug.Log($"[F010] snapshot {path}");
        }

        private static bool IsFlat(Texture2D image)
        {
            Color first = image.GetPixel(0, 0);
            for (int row = 0; row < GridRows; row++)
            {
                for (int column = 0; column < GridColumns; column++)
                {
                    int x = (column * (image.width - 1)) / (GridColumns - 1);
                    int y = (row * (image.height - 1)) / (GridRows - 1);
                    Color color = image.GetPixel(x, y);
                    if (Mathf.Abs(color.r - first.r) > FlatTolerance
                        || Mathf.Abs(color.g - first.g) > FlatTolerance
                        || Mathf.Abs(color.b - first.b) > FlatTolerance)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
#endif
