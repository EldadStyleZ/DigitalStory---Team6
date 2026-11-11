#if UNITY_EDITOR
using System.Collections;
using DigitalStory.View;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>
    /// Watches the swarm's invisible core from the game camera, frame by frame, as the core walks, stops and stands. The
    /// inner ring of wasps marks the core. When the core stops, the river curls into the ring around it; it must flow
    /// around the inner ring, never across the core, or hundreds of wasps pile up on the marker for half a second.
    /// </summary>
    /// <remarks>
    /// Every frame is pinned to 1/60 s of game time with Time.captureDeltaTime, the rate the game plays at, and the game
    /// camera is drawn into a texture after every LateUpdate by <see cref="SnapshotCapturer"/>.
    /// </remarks>
    public sealed class HiveDefenseCoreMarkerTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const int FramesToSettle = 3;
        private const int Width = 960;
        private const int Height = 540;
        private const int DepthBits = 24;
        private const float FrameSeconds = 1f / 60f;
        private const float WalkSeconds = 2f;

        // The inner ring settles on the stopped core within this; it lags the walking core by half a metre.
        private const float RingSettleSeconds = 0.3f;

        // SwarmConfig.StandDelaySeconds plus StandBlendSeconds, and the springs settling into the ring around the core.
        private const float CurlSeconds = 2f;
        private const float StandSeconds = 1f;

        // The inner ring flies at FlyHeight (WaspSwarm.compute) above the core. Its hole is 0.35 m across at the least.
        private const float MarkerHeight = 1f;
        private const float CentreHalfMetres = 0.12f;
        private const float RingHalfMetres = 0.85f;

        // How much brighter than at rest the middle of the inner ring may get while the river curls around it: a wasp's
        // glow drifting by, not the river crossing it.
        private const float MostCentreRise = 0.1f;

        // How much brighter than its middle the inner ring's brightest quarter stays, so the marker still shows.
        private const float LeastRingContrast = 0.2f;

        private Camera _camera;
        private SwarmCoreMotor _core;
        private SnapshotCapturer _capturer;
        private RenderTexture _target;
        private Texture2D _reader;
        private float _savedTimeScale;
        private float _savedCaptureDeltaTime;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            _savedTimeScale = Time.timeScale;
            _savedCaptureDeltaTime = Time.captureDeltaTime;
            Time.timeScale = 1f;
            Time.captureDeltaTime = FrameSeconds;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));

            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                yield return null;
            }

            _core = Object.FindAnyObjectByType<SwarmCoreMotor>();
            _camera = Camera.main;
            Assert.That(_core, Is.Not.Null);
            Assert.That(_camera, Is.Not.Null);

            _target = new RenderTexture(Width, Height, DepthBits, RenderTextureFormat.ARGB32);
            _target.Create();
            _reader = new Texture2D(Width, Height, TextureFormat.RGB24, false);

            // Made in the loaded scene, so the next test's scene load removes it.
            _capturer = new GameObject("Core Marker Capturer").AddComponent<SnapshotCapturer>();
            _capturer.Initialize(_camera, Width, Height);
            _capturer.BeginOverlay(_target);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (_capturer != null)
            {
                _capturer.EndOverlay();
            }

            if (_target != null)
            {
                _target.Release();
                Object.Destroy(_target);
            }

            if (_reader != null)
            {
                Object.Destroy(_reader);
            }

            Time.timeScale = _savedTimeScale;
            Time.captureDeltaTime = _savedCaptureDeltaTime;
            yield return null;
        }

        [UnityTest]
        public IEnumerator CoreMarker_WhileTheRiverCurlsAroundTheStoppedCore_StaysClearAndShows()
        {
            float walked = 0f;
            while (walked < WalkSeconds)
            {
                _core.Move(Vector3.forward, 1f, false, Time.deltaTime);
                walked += Time.deltaTime;
                yield return null;
            }

            yield return Wait(RingSettleSeconds);

            // While the river curls into the ring around the core.
            float brightestCentre = 0f;
            float curled = 0f;
            while (curled < CurlSeconds)
            {
                brightestCentre = Mathf.Max(brightestCentre, CentreBrightness());
                curled += Time.deltaTime;
                yield return null;
            }

            // At rest, once the ring around the core has formed.
            float restingCentre = 0f;
            float ringContrast = float.MaxValue;
            int restingFrames = 0;
            float stood = 0f;
            while (stood < StandSeconds)
            {
                float centre = CentreBrightness();
                restingCentre += centre;
                ringContrast = Mathf.Min(ringContrast, RingTopBrightness() - centre);
                restingFrames++;
                stood += Time.deltaTime;
                yield return null;
            }

            restingCentre /= Mathf.Max(restingFrames, 1);
            Debug.Log($"[F010] core marker: centre at rest {restingCentre:0.000}, brightest while the river curls {brightestCentre:0.000}, ring contrast {ringContrast:0.000}");
            Assert.That(
                brightestCentre - restingCentre,
                Is.LessThanOrEqualTo(MostCentreRise),
                "The middle of the inner ring lit up while the river curled into the ring around the stopped core: the river crossed the core.");
            Assert.That(ringContrast, Is.GreaterThanOrEqualTo(LeastRingContrast), "The inner ring no longer stands out from its middle.");
        }

        private static IEnumerator Wait(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // The mean brightness of a small square in the middle of the inner ring, as last frame's game camera drew it.
        private float CentreBrightness()
        {
            Read();
            RectInt box = BoxAroundCore(CentreHalfMetres);
            float sum = 0f;
            for (int y = box.yMin; y < box.yMax; y++)
            {
                for (int x = box.xMin; x < box.xMax; x++)
                {
                    sum += Brightness(_reader.GetPixel(x, y));
                }
            }

            return sum / Mathf.Max(box.width * box.height, 1);
        }

        // The mean brightness of the brightest quarter of a square that holds the whole inner ring.
        private float RingTopBrightness()
        {
            RectInt box = BoxAroundCore(RingHalfMetres);
            var values = new float[box.width * box.height];
            int index = 0;
            for (int y = box.yMin; y < box.yMax; y++)
            {
                for (int x = box.xMin; x < box.xMax; x++)
                {
                    values[index] = Brightness(_reader.GetPixel(x, y));
                    index++;
                }
            }

            System.Array.Sort(values);
            int first = (values.Length * 3) / 4;
            float sum = 0f;
            for (int at = first; at < values.Length; at++)
            {
                sum += values[at];
            }

            return sum / Mathf.Max(values.Length - first, 1);
        }

        private void Read()
        {
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = _target;
                _reader.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                _reader.Apply();
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        // A square on the picture around the inner ring's middle, halfMetres across each way at the ring's distance. The
        // camera is still where it drew last frame's picture, since it only moves in LateUpdate.
        private RectInt BoxAroundCore(float halfMetres)
        {
            Vector3 middle = _core.Position + (Vector3.up * MarkerHeight);
            Matrix4x4 projection = Matrix4x4.Perspective(_camera.fieldOfView, (float)Width / Height, _camera.nearClipPlane, _camera.farClipPlane);
            Vector3 view = _camera.worldToCameraMatrix.MultiplyPoint(middle);
            Vector3 clip = projection.MultiplyPoint(view);
            float focal = 0.5f * Height / Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            int half = Mathf.Max(1, Mathf.RoundToInt(halfMetres * focal / -view.z));
            int centreX = Mathf.RoundToInt((clip.x * 0.5f + 0.5f) * Width);
            int centreY = Mathf.RoundToInt((clip.y * 0.5f + 0.5f) * Height);
            int xMin = Mathf.Clamp(centreX - half, 0, Width - 1);
            int yMin = Mathf.Clamp(centreY - half, 0, Height - 1);
            int xMax = Mathf.Clamp(centreX + half, xMin + 1, Width);
            int yMax = Mathf.Clamp(centreY + half, yMin + 1, Height);
            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private static float Brightness(Color color) => (0.299f * color.r) + (0.587f * color.g) + (0.114f * color.b);
    }
}
#endif
