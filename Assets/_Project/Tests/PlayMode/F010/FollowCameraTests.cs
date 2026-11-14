using System.Collections;
using System.Reflection;
using DigitalStory.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>
    /// The follow camera's chase view and its effects, with a camera and a target the tests create themselves (no scene from the
    /// builder). Every frame is pinned to 1/60 s with Time.captureDeltaTime, so each wait is in game seconds.
    /// </summary>
    public sealed class FollowCameraTests
    {
        private const float FrameSeconds = 1f / 60f;
        private const float BlendSeconds = 0.8f;
        private const float GiantHeight = 6f;
        private const float Tolerance = 1e-3f;

        private GameObject _cameraHost;
        private GameObject _targetHost;
        private FollowCamera _camera;
        private Transform _target;

        [SetUp]
        public void Create()
        {
            Time.captureDeltaTime = FrameSeconds;
            _targetHost = new GameObject("Target");
            _target = _targetHost.transform;
            _cameraHost = new GameObject("Camera", typeof(Camera));
            _camera = _cameraHost.AddComponent<FollowCamera>();
            _camera.SetTarget(_target);
        }

        [TearDown]
        public void Destroy()
        {
            Time.captureDeltaTime = 0f;
            Object.DestroyImmediate(_cameraHost);
            Object.DestroyImmediate(_targetHost);
        }

        [UnityTest]
        public IEnumerator EnterChase_EndsBehindTheTargetWithinTheBlendTime()
        {
            _target.rotation = Quaternion.Euler(0f, 90f, 0f);
            yield return Wait(1f);
            _camera.EnterChase(_target, GiantHeight);
            yield return Wait(BlendSeconds + 0.3f);

            Vector3 toTarget = _target.position - _camera.transform.position;
            toTarget.y = 0f;
            Assert.That(_camera.IsChasing, Is.True);
            Assert.That(_camera.ChaseBlend, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(Vector3.Dot(toTarget.normalized, _target.forward), Is.GreaterThan(0.99f), "The camera is not behind the target.");
            Assert.That(Vector3.Dot(_camera.GroundForward, _target.forward), Is.GreaterThan(0.99f), "The camera does not look where the target faces.");
            Assert.That(_camera.transform.position.y, Is.GreaterThan(_target.position.y), "The camera is not above the feet.");
        }

        [UnityTest]
        public IEnumerator EnterChase_FillsAboutAThirdOfTheScreenHeight()
        {
            _camera.EnterChase(_target, GiantHeight);
            yield return Wait(BlendSeconds + 0.5f);

            Camera camera = _cameraHost.GetComponent<Camera>();
            Vector3 feet = camera.WorldToViewportPoint(_target.position);
            Vector3 head = camera.WorldToViewportPoint(_target.position + (Vector3.up * GiantHeight));
            Assert.That(head.y - feet.y, Is.InRange(0.25f, 0.45f));
        }

        [UnityTest]
        public IEnumerator Orbit_TurnsTheViewAndDriftsBackAfterTheIdleTime()
        {
            SetField("_orbitIdleSeconds", 0.5f);
            SetField("_orbitReturnSeconds", 0.2f);
            _camera.EnterChase(_target, GiantHeight);
            yield return Wait(BlendSeconds + 0.3f);

            _camera.Orbit(70f, 10f);
            yield return Wait(FrameSeconds * 3f);
            Assert.That(Vector3.Dot(_camera.GroundForward, _target.forward), Is.LessThan(0.6f), "Orbit did not turn the view.");

            yield return Wait(0.5f + 1.5f);
            Assert.That(Vector3.Dot(_camera.GroundForward, _target.forward), Is.GreaterThan(0.99f), "The view did not drift back behind the target.");
        }

        [UnityTest]
        public IEnumerator Orbit_KeepsTheViewAwayFromTheTargetWhileCalledEveryFrame()
        {
            SetField("_orbitIdleSeconds", 0.3f);
            _camera.EnterChase(_target, GiantHeight);
            yield return Wait(BlendSeconds + 0.3f);

            for (int i = 0; i < 90; i++)
            {
                _camera.Orbit(0.5f, 0f);
                yield return null;
            }

            Assert.That(Vector3.Dot(_camera.GroundForward, _target.forward), Is.LessThan(0.99f), "The camera drifted back while it was being orbited.");
        }

        [UnityTest]
        public IEnumerator Chase_NeverGoesBelowTheGroundOrInsideTheKeepOutRadius()
        {
            SetField("_chaseDistance", 2f);
            SetField("_keepOutRadius", 3f);
            SetField("_minHeightAboveGround", 1f);
            SetField("_orbitIdleSeconds", 100f);
            _camera.EnterChase(_target);
            yield return Wait(BlendSeconds + 0.3f);

            for (int step = 0; step < 24; step++)
            {
                _camera.Orbit(15f, step < 12 ? -10f : 10f);
                yield return Wait(FrameSeconds * 4f);

                Vector3 position = _camera.transform.position;
                Vector3 flat = new Vector3(position.x - _target.position.x, 0f, position.z - _target.position.z);
                Assert.That(position.y, Is.GreaterThanOrEqualTo(1f - Tolerance), "The camera went under the ground at step " + step + ".");
                Assert.That(flat.magnitude, Is.GreaterThanOrEqualTo(3f - Tolerance), "The camera went inside the keep-out radius at step " + step + ".");
            }
        }

        // The scene's tree crowns have no collider, so the builder gives the camera boxes it never looks through: with one
        // behind the target, the chase camera stops in front of it, still behind the target, rather than inside or past it.
        [UnityTest]
        public IEnumerator Chase_WithABlockerBehindTheTarget_StopsInFrontOfIt()
        {
            var blocker = new Bounds(new Vector3(0f, 4f, -8f), new Vector3(6f, 6f, 2f));
            _camera.SetChaseBlockers(new[] { blocker });
            _camera.EnterChase(_target, GiantHeight);
            yield return Wait(BlendSeconds + 0.5f);

            Vector3 position = _camera.transform.position;
            Assert.That(blocker.Contains(position), Is.False, "The camera is inside the blocker.");
            Assert.That(position.z, Is.GreaterThan(blocker.max.z), "The camera is past the blocker, looking through it.");
            Assert.That(position.z, Is.LessThan(_target.position.z), "The camera is not behind the target.");
        }

        [Test]
        public void SetChaseBlockers_WithNull_FailsLoudly()
        {
            Assert.Throws<System.ArgumentNullException>(() => _camera.SetChaseBlockers(null));
        }

        [UnityTest]
        public IEnumerator GroundForward_IsFlatAndUnitLengthInBothViews()
        {
            yield return Wait(0.2f);
            AssertFlatUnit(_camera.GroundForward, "top view");

            _camera.EnterChase(_target, GiantHeight);

            for (int i = 0; i < 70; i++)
            {
                yield return null;
                AssertFlatUnit(_camera.GroundForward, "blend frame " + i);
            }

            _camera.Orbit(33f, 20f);
            yield return Wait(0.2f);
            AssertFlatUnit(_camera.GroundForward, "orbited chase view");

            _camera.ExitChase();
            yield return Wait(BlendSeconds + 0.3f);
            AssertFlatUnit(_camera.GroundForward, "top view again");
            Assert.That(_camera.IsChasing, Is.False);
            Assert.That(_camera.ChaseBlend, Is.EqualTo(0f).Within(Tolerance));
        }

        [UnityTest]
        public IEnumerator Effects_ReturnToRest()
        {
            yield return Wait(1.5f);
            Vector3 rest = _camera.transform.position;

            _camera.PushIn(1f);
            _camera.SetWavePullBack(true);
            _camera.LeanToward(new Vector3(12f, 0f, 5f), 1f);
            yield return Wait(0.3f);
            Assert.That(_camera.PushInStrength, Is.GreaterThan(0f));
            Assert.That(_camera.LeanStrength, Is.GreaterThan(0f));
            Assert.That(Vector3.Distance(_camera.transform.position, rest), Is.GreaterThan(0.1f), "The effects did not move the camera.");

            _camera.SetWavePullBack(false);
            yield return Wait(6f);

            Assert.That(_camera.PushInStrength, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_camera.WavePullBack01, Is.EqualTo(0f).Within(0.01f));
            Assert.That(_camera.LeanStrength, Is.EqualTo(0f).Within(0.01f));
            Assert.That(Vector3.Distance(_camera.transform.position, rest), Is.LessThan(0.02f), "The camera did not return to rest.");
        }

        [UnityTest]
        public IEnumerator Effects_ReturnToRestInTheChaseViewToo()
        {
            _camera.EnterChase(_target, GiantHeight);
            yield return Wait(BlendSeconds + 1f);
            Vector3 rest = _camera.transform.position;

            _camera.PushIn(1f);
            yield return Wait(0.2f);
            Assert.That(Vector3.Distance(_camera.transform.position, rest), Is.GreaterThan(0.1f));

            yield return Wait(5f);
            Assert.That(Vector3.Distance(_camera.transform.position, rest), Is.LessThan(0.02f));
        }

        [UnityTest]
        public IEnumerator Liveliness_Zero_LeavesTheViewUntouchedByAnyEffect()
        {
            _camera.Liveliness = 0f;
            yield return Wait(1.5f);
            Vector3 position = _camera.transform.position;
            Quaternion rotation = _camera.transform.rotation;

            for (int i = 0; i < 30; i++)
            {
                _camera.PushIn(1f);
                _camera.SetWavePullBack(true);
                _camera.LeanToward(new Vector3(20f, 0f, 20f), 1f);
                yield return null;
            }

            Assert.That(Vector3.Distance(_camera.transform.position, position), Is.LessThan(Tolerance));
            Assert.That(Quaternion.Angle(_camera.transform.rotation, rotation), Is.LessThan(Tolerance));

            _camera.EnterChase(_target, GiantHeight);
            yield return Wait(BlendSeconds + 0.5f);
            Vector3 chasePosition = _camera.transform.position;

            for (int i = 0; i < 30; i++)
            {
                _camera.PushIn(1f);
                _camera.LeanToward(new Vector3(20f, 0f, 20f), 1f);
                yield return null;
            }

            Assert.That(Vector3.Distance(_camera.transform.position, chasePosition), Is.LessThan(Tolerance));
        }

        [UnityTest]
        public IEnumerator Shake_DuringAChase_DoesNotMoveTheFollowTarget()
        {
            _target.position = new Vector3(3f, 0f, -2f);
            _camera.EnterChase(_target, GiantHeight);
            yield return Wait(BlendSeconds + 0.3f);
            Vector3 before = _target.position;
            Quaternion facing = _target.rotation;
            Vector3 resting = _camera.transform.position;

            _camera.Shake(1f);
            _camera.Kick(1f);
            _camera.PushIn(1f);
            yield return Wait(FrameSeconds * 3f);

            Assert.That(_camera.ShakeStrength, Is.GreaterThan(0f));
            Assert.That(_camera.Target, Is.SameAs(_target));
            Assert.That(_target.position, Is.EqualTo(before));
            Assert.That(_target.rotation, Is.EqualTo(facing));
            Assert.That(Vector3.Distance(_camera.transform.position, resting), Is.GreaterThan(0.01f), "The shake did not move the camera.");
        }

        // Plan section Y: the trauma shake turns the view, falls to rest within a second, and leaves the view exactly where it was.
        [UnityTest]
        public IEnumerator Shake_TraumaFallsToRest_AndTheViewEndsWhereItStarted()
        {
            yield return Wait(1.5f);
            Vector3 position = _camera.transform.position;
            Quaternion rotation = _camera.transform.rotation;

            _camera.Shake(1f);
            yield return Wait(0.1f);
            Assert.That(_camera.ShakeStrength, Is.GreaterThan(0.5f));
            Assert.That(Quaternion.Angle(_camera.transform.rotation, rotation), Is.GreaterThan(0.05f), "The shake did not turn the view.");

            yield return Wait(1.2f);
            Assert.That(_camera.ShakeStrength, Is.EqualTo(0f));
            Assert.That(Vector3.Distance(_camera.transform.position, position), Is.LessThan(Tolerance));
            Assert.That(Quaternion.Angle(_camera.transform.rotation, rotation), Is.LessThan(0.01f));
        }

        // Plan section Y: the master scale at 0 leaves the camera still whatever shakes, kicks, punches or nudges it.
        [UnityTest]
        public IEnumerator ShakeScale_Zero_LeavesTheCameraStill()
        {
            _camera.ShakeScale = 0f;
            yield return Wait(1.5f);
            Camera view = _cameraHost.GetComponent<Camera>();
            Vector3 position = _camera.transform.position;
            Quaternion rotation = _camera.transform.rotation;
            float fieldOfView = view.fieldOfView;

            for (int i = 0; i < 30; i++)
            {
                _camera.Shake(1f);
                _camera.Kick(1f);
                _camera.Punch(0.1f, 2f);
                _camera.Nudge(Vector3.right, 1f);
                yield return null;
            }

            Assert.That(Vector3.Distance(_camera.transform.position, position), Is.LessThan(Tolerance));
            Assert.That(Quaternion.Angle(_camera.transform.rotation, rotation), Is.LessThan(Tolerance));
            Assert.That(view.fieldOfView, Is.EqualTo(fieldOfView).Within(Tolerance));
        }

        // Plan section Y: a camera moment (the punch and the sprung kick) is only an offset on the follow. While the player
        // steers through it, the camera is the plain follow plus the kick, every frame, and afterwards it is the plain follow.
        [UnityTest]
        public IEnumerator Moment_YieldsToSteeringAtOnce_AndEndsWhereThePlainFollowIs()
        {
            var plainHost = new GameObject("Plain Camera", typeof(Camera));
            try
            {
                FollowCamera plain = plainHost.AddComponent<FollowCamera>();
                plain.SetTarget(_target);
                yield return Wait(1.5f);
                Camera view = _cameraHost.GetComponent<Camera>();
                Camera plainView = plainHost.GetComponent<Camera>();

                _camera.Punch(0.08f, 1f);
                _camera.Nudge(Vector3.right, 0.4f);
                bool sawKick = false;
                for (int frame = 0; frame < 30; frame++)
                {
                    _target.position += new Vector3(0f, 0f, 6f * FrameSeconds);
                    yield return null;
                    Vector3 kick = _camera.transform.position - plain.transform.position;
                    Assert.That(Vector3.Distance(kick, _camera.NudgeOffset), Is.LessThan(Tolerance), "The moment held the follow back.");
                    sawKick |= _camera.NudgeOffset.magnitude > 0.1f;
                }

                Assert.That(sawKick, Is.True, "The nudge did not kick the camera.");
                yield return Wait(1.5f);
                Assert.That(Vector3.Distance(_camera.transform.position, plain.transform.position), Is.LessThan(Tolerance));
                Assert.That(Quaternion.Angle(_camera.transform.rotation, plain.transform.rotation), Is.LessThan(Tolerance));
                Assert.That(view.fieldOfView, Is.EqualTo(plainView.fieldOfView).Within(Tolerance));
            }
            finally
            {
                Object.DestroyImmediate(plainHost);
            }
        }

        [Test]
        public void Configure_WithNull_FailsLoudly()
        {
            Assert.Throws<System.ArgumentNullException>(() => _camera.Configure(null));
        }

        [Test]
        public void EnterChase_WithoutATarget_FailsLoudly()
        {
            Assert.Throws<System.ArgumentNullException>(() => _camera.EnterChase(null));
        }

        private static IEnumerator Wait(float seconds)
        {
            int frames = Mathf.CeilToInt(seconds / FrameSeconds);

            for (int i = 0; i < frames; i++)
            {
                yield return null;
            }
        }

        private static void AssertFlatUnit(Vector3 direction, string where)
        {
            Assert.That(direction.y, Is.EqualTo(0f).Within(Tolerance), "GroundForward is not flat in the " + where + ".");
            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(Tolerance), "GroundForward is not unit length in the " + where + ".");
        }

        // The camera's tuning is serialized and private, so a test sets it the way the Inspector would.
        private void SetField(string name, float value)
        {
            FieldInfo field = typeof(FollowCamera).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "FollowCamera has no field " + name + ".");
            field.SetValue(_camera, value);
        }
    }
}
