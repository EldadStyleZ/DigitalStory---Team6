#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using DigitalStory.View;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>
    /// Renders the Wasp Piper scene's camera into PNG files so a reviewer can see the swarm without opening the Editor.
    /// Each test is ignored unless the F010_SNAPSHOT_DIR environment variable names a folder.
    /// </summary>
    [Category("F010Snapshot")]
    public sealed class HiveDefenseSnapshotTests
    {
        private const string DirectoryVariable = "F010_SNAPSHOT_DIR";
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const int Width = 960;
        private const int Height = 540;
        private const int GridColumns = 16;
        private const int GridRows = 9;
        private const float FlatTolerance = 0.02f;
        private const int FramesToSettle = 3;
        private const float StandSeconds = 2f;
        private const float WalkSeconds = 3f;
        private const float WalkSpeed = 4.5f;
        private const float TurnRate = 0.5f;
        private const float SongSeconds = 1.5f;
        private const float StingSeconds = 1f;
        private const float FastTimeScale = 5f;
        private const float MaxWaveSeconds = 40f;
        private const int WantedBeekeepers = 2;
        private const int MaxFrames = 300000;

        private static readonly int[] DomeHoles = { 0, 1, 0 };
        private static readonly int[] HiveCallHoles = { 0, 5, 0 };
        private static readonly int[] StingHoles = { 2, 3, 4 };

        private HiveDefenseRoot _root;
        private Camera _camera;
        private string _directory;
        private float _savedTimeScale;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            _directory = Environment.GetEnvironmentVariable(DirectoryVariable);
            if (string.IsNullOrEmpty(_directory))
            {
                Assert.Ignore($"Set {DirectoryVariable} to a folder to write the F-010 snapshots.");
            }

            Directory.CreateDirectory(_directory);
            _savedTimeScale = Time.timeScale;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));

            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                yield return null;
            }

            _root = UnityEngine.Object.FindAnyObjectByType<HiveDefenseRoot>();
            _camera = Camera.main != null ? Camera.main : UnityEngine.Object.FindAnyObjectByType<Camera>();
            Assert.That(_root, Is.Not.Null);
            Assert.That(_camera, Is.Not.Null);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = _savedTimeScale;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Snapshot1_RiverStanding()
        {
            yield return Wait(StandSeconds);
            Capture("snap-1-river-standing.png");
        }

        [UnityTest]
        public IEnumerator Snapshot2_RiverWalking()
        {
            CharacterController controller = _root.GetComponentInChildren<CharacterController>();
            if (controller == null)
            {
                controller = UnityEngine.Object.FindAnyObjectByType<PiperMotor>().GetComponent<CharacterController>();
            }

            float elapsed = 0f;
            float angle = 0f;
            while (elapsed < WalkSeconds)
            {
                float dt = Time.deltaTime;
                angle += TurnRate * dt;
                var direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                controller.Move(direction * (WalkSpeed * dt));
                elapsed += dt;
                yield return null;
            }

            Capture("snap-2-river-walking.png");
        }

        [UnityTest]
        public IEnumerator Snapshot3_Dome()
        {
            yield return Play(DomeHoles);
            yield return Wait(SongSeconds);
            Capture("snap-3-dome.png");
        }

        [UnityTest]
        public IEnumerator Snapshot4_HiveCall()
        {
            yield return Play(HiveCallHoles);
            yield return Wait(SongSeconds);
            Capture("snap-4-hivecall.png");
        }

        [UnityTest]
        public IEnumerator Snapshot5_Wave()
        {
            yield return WaitForBeekeepers(WantedBeekeepers);
            Assert.That(_root.Session.ActiveBeekeeperCount, Is.GreaterThanOrEqualTo(WantedBeekeepers));
            Capture("snap-5-wave.png");
        }

        [UnityTest]
        public IEnumerator Snapshot6_Sting()
        {
            yield return WaitForBeekeepers(1);
            Assert.That(_root.Session.ActiveBeekeeperCount, Is.GreaterThanOrEqualTo(1));

            yield return Play(StingHoles);
            yield return Wait(StingSeconds);
            Capture("snap-6-sting.png");
        }

        private IEnumerator Wait(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator Play(int[] holes)
        {
            for (int index = 0; index < holes.Length; index++)
            {
                _root.PressHole(holes[index]);
                yield return null;
            }
        }

        private IEnumerator WaitForBeekeepers(int wanted)
        {
            Time.timeScale = FastTimeScale;
            float elapsed = 0f;
            int frames = 0;
            while (_root.Session.ActiveBeekeeperCount < wanted && elapsed < MaxWaveSeconds && frames < MaxFrames)
            {
                elapsed += Time.deltaTime;
                frames++;
                yield return null;
            }

            Time.timeScale = _savedTimeScale;
            yield return null;
        }

        // Called right after a frame's LateUpdate, so the camera has already followed the Piper.
        private void Capture(string fileName)
        {
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            RenderTexture previousTarget = _camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;

            try
            {
                _camera.targetTexture = target;
                _camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                image.Apply();
            }
            finally
            {
                _camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
            }

            string path = Path.Combine(_directory, fileName);
            File.WriteAllBytes(path, image.EncodeToPNG());
            Debug.Log($"[F010] snapshot {path}");

            bool isFlat = IsFlat(image);
            UnityEngine.Object.Destroy(image);
            target.Release();
            UnityEngine.Object.Destroy(target);

            Assert.That(File.Exists(path), Is.True, "The snapshot file was not written.");
            Assert.That(isFlat, Is.False, $"{fileName} is a single flat colour.");
        }

        private static bool IsFlat(Texture2D image)
        {
            Color first = image.GetPixel(0, 0);
            for (int row = 0; row < GridRows; row++)
            {
                for (int column = 0; column < GridColumns; column++)
                {
                    int x = (column * (Width - 1)) / (GridColumns - 1);
                    int y = (row * (Height - 1)) / (GridRows - 1);
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
