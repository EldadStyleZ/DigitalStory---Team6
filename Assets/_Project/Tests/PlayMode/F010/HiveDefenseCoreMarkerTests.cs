#if UNITY_EDITOR
using System.Collections;
using DigitalStory.View;
using NUnit.Framework;
using Unity.Collections;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>
    /// Watches the swarm from straight above as its invisible core walks, stops, stands and walks off. Nothing marks the
    /// core: while it stands, the free swarm flows round a figure-eight that crosses itself over the core. A broken eight
    /// reads as a ring or a blob, so these tests look for two separate lobes, each round a hole, and the crossing on the
    /// core, with the young swarm of 800 and the full swarm of 4,000. They also check that the river winds into the eight
    /// at a stop, and out of it at a start, without piling wasps onto the core.
    /// </summary>
    /// <remarks>
    /// Every frame is pinned to 1/60 s of game time with Time.captureDeltaTime, the rate the game plays at. A camera of
    /// the test's own looks straight down at the core, orthographic, so a metre is the same number of pixels everywhere.
    /// It clips away the ground and clears to black, so a bright pixel is a wasp. It draws into a texture after every
    /// LateUpdate, the wasps included, and each step of a test reads the picture the frame before drew.
    /// </remarks>
    public sealed class HiveDefenseCoreMarkerTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const int FramesToSettle = 3;
        private const int Size = 512;
        private const int DepthBits = 24;
        private const float FrameSeconds = 1f / 60f;

        // The camera hangs this high over the core and sees ViewHalfMetres to each side. It draws from HighestMetres down
        // to GroundClipMetres over the core, so the ground never shows; the eight's lowest wasps fly 0.35 m up.
        private const float CameraHeight = 15f;
        private const float ViewHalfMetres = 5f;
        private const float HighestMetres = 6f;
        private const float GroundClipMetres = 0.25f;

        // A pixel this bright (0 to 1) is a wasp: the wasps' light bands glow far brighter, the black sky not at all. Only
        // pixels within AnalysedMetres of the core count, so a hive or a tree further off never does.
        private const float WaspLuma = 0.5f;
        private const float AnalysedMetres = 3.5f;

        // Two seconds at the core's 4.5 m/s trails a river of 9 m, far past SwarmConfig.StandingTrailMeters.
        private const float WalkSeconds = 2f;

        // The full swarm's new wasps are born at a hive and fly in.
        private const float GrowSettleSeconds = 4f;

        // SwarmConfig.StandDelaySeconds plus StandBlendSeconds is 1.25 s; this is the most a stop may take to wind in.
        private const float MostWindSeconds = 3f;

        // After the eight has wound in fully, the springs settle into it; then the standing eight is measured.
        private const float SettleSeconds = 1.5f;
        private const int MeasuredFrames = 12;

        // The walk off sideways after the stand, while the eight unwinds into the river (0.75 s) and a little after.
        private const float StartSeconds = 1.5f;

        // An eight is about three times as long as it is wide (a ring or a blob, about as wide as long); each of its lobes
        // holds half of it, round a hole about 0.8 m across, and the two lobes' middles are about 2.9 m apart, with the
        // crossing, two strands over each other, on the core.
        private const float LeastElongation = 2f;
        private const float LeastLobeShare = 0.35f;
        private const float HoleHalfMetres = 0.2f;
        private const float MostHoleFill = 0.25f;
        private const float LeastLobesApartMetres = 2f;
        private const float CrossingHalfMetres = 0.35f;
        private const float LeastCrossingFill = 0.5f;

        // The pile: how much of a disc round the core is wasps. In the standing eight the crossing fills most of it; while
        // the river winds in or out it may fill it only this much more.
        private const float PileRadiusMetres = 1.2f;
        private const float MostPileRise = 0.1f;

        private HiveDefenseRoot _root;
        private SwarmCoreMotor _core;
        private SwarmGpuView _swarmView;
        private Camera _topCamera;
        private RenderTexture _target;
        private Texture2D _reader;
        private readonly bool[] _isWasp = new bool[Size * Size];
        private float _savedTimeScale;
        private float _savedCaptureDeltaTime;

        private static float PixelsPerMetre => Size / (2f * ViewHalfMetres);

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

            _root = Object.FindAnyObjectByType<HiveDefenseRoot>();
            _core = Object.FindAnyObjectByType<SwarmCoreMotor>();
            _swarmView = Object.FindAnyObjectByType<SwarmGpuView>();
            Assert.That(_root, Is.Not.Null);
            Assert.That(_core, Is.Not.Null);
            Assert.That(_swarmView, Is.Not.Null);
            Assert.That(_swarmView.IsRunning, Is.True, "The swarm's GPU view must run for the eight to show.");

            _target = new RenderTexture(Size, Size, DepthBits, RenderTextureFormat.ARGB32);
            _target.Create();
            _reader = new Texture2D(Size, Size, TextureFormat.RGBA32, false);

            // Made in the loaded scene, so the next test's scene load removes it. Straight down, with world +x to the right
            // of the picture and world +z up it.
            var cameraObject = new GameObject("Eight Top Camera");
            _topCamera = cameraObject.AddComponent<Camera>();
            _topCamera.orthographic = true;
            _topCamera.orthographicSize = ViewHalfMetres;
            _topCamera.clearFlags = CameraClearFlags.SolidColor;
            _topCamera.backgroundColor = Color.black;
            _topCamera.nearClipPlane = CameraHeight - HighestMetres;
            _topCamera.farClipPlane = CameraHeight - GroundClipMetres;
            _topCamera.allowHDR = false;
            _topCamera.allowMSAA = false;
            _topCamera.targetTexture = _target;
            _topCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            FollowCore();
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (_topCamera != null)
            {
                _topCamera.targetTexture = null;
                Object.Destroy(_topCamera.gameObject);
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
        public IEnumerator Eight_OfTheYoungSwarm_ShowsTwoLobesCrossingOnTheCoreAndWindsWithoutAPile()
        {
            yield return CheckEight("800");
        }

        [UnityTest]
        public IEnumerator Eight_OfTheFullSwarm_ShowsTwoLobesCrossingOnTheCoreAndWindsWithoutAPile()
        {
            // The capacity grows to its ceiling, as nests raise it, and wasps that arrive fill the swarm to it.
            _root.Session.Swarm.GrowCapacity(_root.Session.Swarm.Max);
            _root.Session.Swarm.Gain(_root.Session.Swarm.Max);
            Assert.That(_root.Session.Swarm.Count, Is.EqualTo(_root.Session.Swarm.Max));
            yield return Stand(GrowSettleSeconds);

            yield return CheckEight(_root.Session.Swarm.Count.ToString());
        }

        private IEnumerator CheckEight(string swarmName)
        {
            // Walk, so the swarm trails a river behind the core.
            yield return Walk(Vector3.forward, WalkSeconds);

            // Stop: the river winds into the eight. The pile is watched until the eight has wound in and settled.
            float stopPeak = 0f;
            float waited = 0f;
            while (_swarmView.EightShare < 1f && waited < MostWindSeconds)
            {
                Read();
                stopPeak = Mathf.Max(stopPeak, PileFill());
                waited += Time.deltaTime;
                yield return StepStanding();
            }

            Assert.That(_swarmView.EightShare, Is.EqualTo(1f), "The river never wound into the eight round the stopped core.");

            float settled = 0f;
            while (settled < SettleSeconds)
            {
                Read();
                stopPeak = Mathf.Max(stopPeak, PileFill());
                settled += Time.deltaTime;
                yield return StepStanding();
            }

            // The standing eight.
            var sum = new EightShape();
            float steadyPile = 0f;
            for (int frame = 0; frame < MeasuredFrames; frame++)
            {
                Read();
                sum = sum.Plus(MeasureShape());
                steadyPile += PileFill();
                yield return StepStanding();
            }

            EightShape shape = sum.Over(MeasuredFrames);
            steadyPile /= MeasuredFrames;

            // Walk off sideways: the eight unwinds into the river.
            float startPeak = 0f;
            float walked = 0f;
            while (walked < StartSeconds)
            {
                Read();
                startPeak = Mathf.Max(startPeak, PileFill());
                walked += Time.deltaTime;
                yield return StepWalking(Vector3.right);
            }

            Debug.Log(
                $"[F010] eight of {swarmName}: elongation {shape.Elongation:0.00}, smaller lobe {shape.LeastLobeShare:0.00}, " +
                $"fullest hole {shape.MostHoleFill:0.00}, lobes {shape.LobesApart:0.00} m apart, crossing {shape.CrossingFill:0.00}; " +
                $"pile round the core: standing {steadyPile:0.00}, stop {stopPeak:0.00}, start {startPeak:0.00}");

            Assert.That(shape.Elongation, Is.GreaterThanOrEqualTo(LeastElongation), "The standing swarm is as wide as it is long: a ring or a blob, not an eight.");
            Assert.That(shape.LeastLobeShare, Is.GreaterThanOrEqualTo(LeastLobeShare), "One side of the core holds most of the swarm: the eight has lost a lobe.");
            Assert.That(shape.MostHoleFill, Is.LessThanOrEqualTo(MostHoleFill), "A lobe's middle is full of wasps: the eight reads as a blob.");
            Assert.That(shape.LobesApart, Is.GreaterThanOrEqualTo(LeastLobesApartMetres), "The two lobes sit on top of each other.");
            Assert.That(shape.CrossingFill, Is.GreaterThanOrEqualTo(LeastCrossingFill), "Nothing crosses over the core: the eight has a hole where its crossing should be.");
            Assert.That(stopPeak, Is.LessThanOrEqualTo(steadyPile + MostPileRise), "The river piled onto the core as it wound into the eight at the stop.");
            Assert.That(startPeak, Is.LessThanOrEqualTo(steadyPile + MostPileRise), "The eight piled onto the core as it unwound into the river at the start.");
        }

        private IEnumerator Walk(Vector3 direction, float seconds)
        {
            float walked = 0f;
            while (walked < seconds)
            {
                walked += Time.deltaTime;
                yield return StepWalking(direction);
            }
        }

        private IEnumerator Stand(float seconds)
        {
            float stood = 0f;
            while (stood < seconds)
            {
                stood += Time.deltaTime;
                yield return StepStanding();
            }
        }

        // One frame. The camera is put over the core first: the root has already fed the swarm this frame's core, and the
        // move below only shows in the next frame.
        private object StepWalking(Vector3 direction)
        {
            FollowCore();
            _core.Move(direction, 1f, false, Time.deltaTime);
            return null;
        }

        private object StepStanding()
        {
            FollowCore();
            return null;
        }

        private void FollowCore() => _topCamera.transform.position = _core.Position + (Vector3.up * CameraHeight);

        // Marks which pixels of the picture the frame before drew are wasps.
        private void Read()
        {
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = _target;
                _reader.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                _reader.Apply(false);
            }
            finally
            {
                RenderTexture.active = previous;
            }

            NativeArray<Color32> pixels = _reader.GetPixelData<Color32>(0);
            for (int index = 0; index < _isWasp.Length; index++)
            {
                Color32 pixel = pixels[index];
                float luma = ((0.299f * pixel.r) + (0.587f * pixel.g) + (0.114f * pixel.b)) / 255f;
                _isWasp[index] = luma > WaspLuma && IsAnalysed(index % Size, index / Size);
            }
        }

        // The shape of the wasps round the core: its long axis by the spread of the wasp pixels; the two sides of the line
        // through the core across that axis, which hold the lobes; the middle of each side's wasps, which is a lobe's hole;
        // and the crossing on the core.
        private EightShape MeasureShape()
        {
            double count = 0.0;
            double meanX = 0.0;
            double meanY = 0.0;
            ForEachWasp((x, y) =>
            {
                count++;
                meanX += x;
                meanY += y;
            });

            Assert.That(count, Is.GreaterThan(0.0), "No wasp shows from above the standing core.");
            meanX /= count;
            meanY /= count;

            double spreadXx = 0.0;
            double spreadYy = 0.0;
            double spreadXy = 0.0;
            double centreX = meanX;
            double centreY = meanY;
            ForEachWasp((x, y) =>
            {
                spreadXx += (x - centreX) * (x - centreX);
                spreadYy += (y - centreY) * (y - centreY);
                spreadXy += (x - centreX) * (y - centreY);
            });

            double half = 0.5 * (spreadXx + spreadYy);
            double gap = System.Math.Sqrt(System.Math.Max((0.25 * (spreadXx - spreadYy) * (spreadXx - spreadYy)) + (spreadXy * spreadXy), 0.0));
            double elongation = (half + gap) / System.Math.Max(half - gap, 1e-9);
            double angle = 0.5 * System.Math.Atan2(2.0 * spreadXy, spreadXx - spreadYy);
            double axisX = System.Math.Cos(angle);
            double axisY = System.Math.Sin(angle);

            double firstCount = 0.0;
            double firstX = 0.0;
            double firstY = 0.0;
            double secondX = 0.0;
            double secondY = 0.0;
            ForEachWasp((x, y) =>
            {
                if ((x * axisX) + (y * axisY) >= 0.0)
                {
                    firstCount++;
                    firstX += x;
                    firstY += y;
                }
                else
                {
                    secondX += x;
                    secondY += y;
                }
            });

            double secondCount = count - firstCount;
            var firstMiddle = new Vector2((float)(firstX / System.Math.Max(firstCount, 1.0)), (float)(firstY / System.Math.Max(firstCount, 1.0)));
            var secondMiddle = new Vector2((float)(secondX / System.Math.Max(secondCount, 1.0)), (float)(secondY / System.Math.Max(secondCount, 1.0)));

            return new EightShape(
                (float)elongation,
                (float)(System.Math.Min(firstCount, secondCount) / count),
                Mathf.Max(BoxFill(firstMiddle, HoleHalfMetres), BoxFill(secondMiddle, HoleHalfMetres)),
                Vector2.Distance(firstMiddle, secondMiddle),
                BoxFill(Vector2.zero, CrossingHalfMetres));
        }

        // The share of a disc round the core that is wasps.
        private float PileFill()
        {
            int inside = 0;
            int wasps = 0;
            float radius = PileRadiusMetres * PixelsPerMetre;
            float half = Size * 0.5f;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = x + 0.5f - half;
                    float dy = y + 0.5f - half;
                    if ((dx * dx) + (dy * dy) <= radius * radius)
                    {
                        inside++;
                        wasps += _isWasp[(y * Size) + x] ? 1 : 0;
                    }
                }
            }

            return inside > 0 ? (float)wasps / inside : 0f;
        }

        // The share of a square, halfMetres to each side of a point given in metres from the core, that is wasps.
        private float BoxFill(Vector2 middle, float halfMetres)
        {
            int inside = 0;
            int wasps = 0;
            int xMin = PixelOf(middle.x - halfMetres);
            int xMax = PixelOf(middle.x + halfMetres);
            int yMin = PixelOf(middle.y - halfMetres);
            int yMax = PixelOf(middle.y + halfMetres);
            for (int y = Mathf.Max(yMin, 0); y <= Mathf.Min(yMax, Size - 1); y++)
            {
                for (int x = Mathf.Max(xMin, 0); x <= Mathf.Min(xMax, Size - 1); x++)
                {
                    inside++;
                    wasps += _isWasp[(y * Size) + x] ? 1 : 0;
                }
            }

            return inside > 0 ? (float)wasps / inside : 0f;
        }

        // Calls back with each wasp pixel's place in metres from the core: x along world +x, y along world +z.
        private void ForEachWasp(System.Action<double, double> visit)
        {
            float half = Size * 0.5f;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (_isWasp[(y * Size) + x])
                    {
                        visit((x + 0.5f - half) / PixelsPerMetre, (y + 0.5f - half) / PixelsPerMetre);
                    }
                }
            }
        }

        private static bool IsAnalysed(int x, int y)
        {
            float half = Size * 0.5f;
            float dx = (x + 0.5f - half) / PixelsPerMetre;
            float dy = (y + 0.5f - half) / PixelsPerMetre;
            return (dx * dx) + (dy * dy) <= AnalysedMetres * AnalysedMetres;
        }

        private static int PixelOf(float metres) => Mathf.FloorToInt((metres * PixelsPerMetre) + (Size * 0.5f));

        // What one picture of the standing swarm shows, or the sum of several.
        private readonly struct EightShape
        {
            public EightShape(float elongation, float leastLobeShare, float mostHoleFill, float lobesApart, float crossingFill)
            {
                Elongation = elongation;
                LeastLobeShare = leastLobeShare;
                MostHoleFill = mostHoleFill;
                LobesApart = lobesApart;
                CrossingFill = crossingFill;
            }

            public float Elongation { get; }

            public float LeastLobeShare { get; }

            public float MostHoleFill { get; }

            public float LobesApart { get; }

            public float CrossingFill { get; }

            public EightShape Plus(EightShape other) => new EightShape(
                Elongation + other.Elongation,
                LeastLobeShare + other.LeastLobeShare,
                MostHoleFill + other.MostHoleFill,
                LobesApart + other.LobesApart,
                CrossingFill + other.CrossingFill);

            public EightShape Over(int count) => new EightShape(
                Elongation / count, LeastLobeShare / count, MostHoleFill / count, LobesApart / count, CrossingFill / count);
        }
    }
}
#endif
