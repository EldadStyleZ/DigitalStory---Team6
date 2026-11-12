using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// A three-quarter view from above that follows a target with smoothing, and a third-person chase view behind a target (the giant)
    /// that the player can orbit. Shake, kick, push-in, wave pull-back and lean are offsets added when the camera is placed, so they
    /// never move the followed target or the follow itself.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Follow Camera")]
    public sealed class FollowCamera : MonoBehaviour
    {
        private const int ObstacleHitCapacity = 16;

        [SerializeField, Range(10f, 80f), Tooltip("How far the camera looks down, in degrees.")]
        private float _pitchDegrees = 55f;

        [SerializeField, Tooltip("The compass heading of the view, in degrees. 0 looks along +Z.")]
        private float _yawDegrees;

        [SerializeField, Min(1f), Tooltip("How far back the camera sits from the point it looks at, in metres.")]
        private float _distance = 14f;

        [SerializeField, Min(0f), Tooltip("How high above the target's feet the camera looks, in metres.")]
        private float _focusHeight = 1f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the camera takes to catch up with the target.")]
        private float _followSmoothSeconds = 0.25f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the camera takes to blend to another distance.")]
        private float _distanceSmoothSeconds = 0.6f;

        [Header("Shake and kick")]
        [SerializeField, Min(0f), Tooltip("The most a shake moves the camera, in metres, however hard the hit.")]
        private float _maxShakeMetres = 0.6f;

        [SerializeField, Min(1f), Tooltip("How fast a shake wobbles, in cycles per second.")]
        private float _shakeHertz = 28f;

        [SerializeField, Min(0.01f), Tooltip("Seconds a shake takes to die away.")]
        private float _shakeSeconds = 0.25f;

        [SerializeField, Min(0f), Tooltip("The most a kick pushes the camera along its view, in metres.")]
        private float _maxKickMetres = 0.5f;

        [SerializeField, Min(0.01f), Tooltip("Seconds a kick takes to settle.")]
        private float _kickSeconds = 0.18f;

        [Header("Chase view (the giant)")]
        [SerializeField, Min(0.05f), Tooltip("Seconds the camera takes to swoop between the top view and the chase view.")]
        private float _chaseBlendSeconds = 0.8f;

        [SerializeField, Min(1f), Tooltip("The height of the target the chase numbers below are made for, in metres. A taller or shorter target scales them.")]
        private float _chaseReferenceHeight = 6f;

        [SerializeField, Min(1f), Tooltip("How far behind the look-at point the chase camera sits, in metres, for the reference height. About 15 m makes a 6 m giant fill a third of the screen at a 60 degree field of view.")]
        private float _chaseDistance = 15f;

        [SerializeField, Min(0f), Tooltip("How high above the target's feet the chase camera looks, in metres, for the reference height. About chest height.")]
        private float _chasePivotHeight = 3.6f;

        [SerializeField, Min(0f), Tooltip("How far ahead of the target the chase camera looks, in metres, for the reference height.")]
        private float _chaseLookAhead = 3f;

        [SerializeField, Range(0f, 60f), Tooltip("How far the chase camera looks down when nobody orbits it, in degrees.")]
        private float _chasePitchDegrees = 16f;

        [SerializeField, Range(0f, 60f), Tooltip("The lowest the orbit can bring the chase camera, in degrees of look-down.")]
        private float _chaseMinPitchDegrees = 4f;

        [SerializeField, Range(0f, 80f), Tooltip("The highest the orbit can lift the chase camera, in degrees of look-down.")]
        private float _chaseMaxPitchDegrees = 55f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the chase camera takes to catch up with the target.")]
        private float _chaseFollowSeconds = 0.12f;

        [SerializeField, Min(0f), Tooltip("Seconds without an Orbit call before the chase camera eases back behind the target.")]
        private float _orbitIdleSeconds = 2.5f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the return behind the target takes to settle.")]
        private float _orbitReturnSeconds = 0.6f;

        [SerializeField, Min(0f), Tooltip("The chase camera stays at least this far from the target's axis, in metres, for the reference height.")]
        private float _keepOutRadius = 4f;

        [SerializeField, Tooltip("The height of the ground in world space. The chase camera never goes below it plus the minimum height.")]
        private float _groundHeight;

        [SerializeField, Min(0f), Tooltip("The lowest the chase camera can be above the ground, in metres.")]
        private float _minHeightAboveGround = 1f;

        [SerializeField, Tooltip("What the chase camera never looks through: when something on these layers stands between the camera and the target's chest or feet, the camera moves in front of it. Triggers and character controllers never count.")]
        private LayerMask _chaseObstacleLayers = Physics.DefaultRaycastLayers;

        [SerializeField, Min(0.01f), Tooltip("The radius of the sphere swept from the target's chest to the camera to find what is in the way, in metres.")]
        private float _obstacleRadius = 0.4f;

        [SerializeField, Min(0f), Tooltip("How far the camera keeps in front of what is in the way, in metres.")]
        private float _obstacleMargin = 0.4f;

        [SerializeField, Min(0f), Tooltip("How high above the target's feet the low line of sight to the camera starts, in metres, so the legs stay in view too.")]
        private float _obstacleLowHeight = 0.5f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the camera takes to ease back out once nothing is in the way. Moving in is at once.")]
        private float _obstacleReturnSeconds = 0.5f;

        [Header("Effects")]
        [SerializeField, Range(0f, 2f), Tooltip("Scales the push-in, the wave pull-back and the lean. 0 turns them all off. The shake and the kick are not scaled.")]
        private float _liveliness = 1f;

        [SerializeField, Min(0f), Tooltip("How far a full push-in moves the camera toward what it looks at, in metres.")]
        private float _pushInMetres = 2f;

        [SerializeField, Min(0.01f), Tooltip("Seconds a push-in takes to reach its peak.")]
        private float _pushInSeconds = 0.08f;

        [SerializeField, Min(0.01f), Tooltip("Seconds a push-in takes to ease back.")]
        private float _pushOutSeconds = 0.9f;

        [SerializeField, Min(0f), Tooltip("How far the camera backs away while a wave runs, in metres.")]
        private float _wavePullBackMetres = 1.5f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the pull-back takes to come or go.")]
        private float _wavePullBackSeconds = 1.2f;

        [SerializeField, Min(0f), Tooltip("How far a full lean moves the look target toward the point, in metres.")]
        private float _leanMetres = 1.5f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the lean takes to build and to fade after the last call.")]
        private float _leanSeconds = 0.5f;

        // Not game tuning: room for every collider a sweep can meet at once.
        private readonly RaycastHit[] _obstacleHits = new RaycastHit[ObstacleHitCapacity];

        private Transform _target;
        private float _shake;
        private float _kick;
        private float _shakeClock;
        private Vector3 _offset;
        private Vector3 _localOffset;
        private Vector3 _focus;
        private Vector3 _focusVelocity;
        private float _wantedDistance;
        private float _currentDistance;
        private float _distanceVelocity;

        private Transform _chaseTarget;
        private bool _isChasing;
        private float _chaseBlend;
        private float _chaseScale = 1f;
        private float _chaseYaw;
        private float _chasePitch;
        private float _yawVelocity;
        private float _pitchVelocity;
        private float _orbitIdle;
        private Vector3 _chasePivot;
        private Vector3 _chasePivotVelocity;
        private float _clearDistance = float.MaxValue;
        private float _clearVelocity;
        private Vector3 _groundForward = Vector3.forward;

        private float _pushHold;
        private float _pushShown;
        private float _wavePull;
        private float _wavePullVelocity;
        private bool _waveRunning;
        private float _lean;
        private float _leanVelocity;
        private float _leanWant;
        private Vector3 _leanPoint;

        /// <summary>The distance the camera returns to after SetDistance.</summary>
        public float DefaultDistance => _distance;

        /// <summary>What the camera follows, or null before <see cref="SetTarget"/>.</summary>
        public Transform Target => _target;

        /// <summary>The distance the camera is blending to: the default, or the last one SetDistance asked for.</summary>
        public float WantedDistance => _wantedDistance;

        /// <summary>The shake's current strength, 0 to 1, falling to 0 as it dies away. A test reads it to see a shake happened.</summary>
        public float ShakeStrength => _shake;

        /// <summary>The kick's current strength, 0 to 1, falling to 0 as it settles.</summary>
        public float KickStrength => _kick;

        /// <summary>True from <see cref="EnterChase(Transform)"/> until <see cref="ExitChase"/>, while the camera is or is blending to the chase view.</summary>
        public bool IsChasing => _isChasing;

        /// <summary>How far the swoop has gone: 0 is the top view, 1 is the chase view.</summary>
        public float ChaseBlend => _chaseBlend;

        /// <summary>
        /// What the camera's forward is on the ground, unit length, in both views. The root puts it in the frame so walking is
        /// relative to the camera. It is the camera's up flattened when the camera looks straight down.
        /// </summary>
        public Vector3 GroundForward => _groundForward;

        /// <summary>The scale the Liveliness setting gives push-in, wave pull-back and lean: 0 turns them off.</summary>
        public float Liveliness
        {
            get => _liveliness;
            set => _liveliness = Mathf.Max(0f, value);
        }

        /// <summary>The push-in's current strength, 0 to 1, easing back to 0.</summary>
        public float PushInStrength => _pushShown;

        /// <summary>How far the wave pull-back has gone, 0 to 1.</summary>
        public float WavePullBack01 => _wavePull;

        /// <summary>The lean's current strength, 0 to 1, fading to 0 when <see cref="LeanToward"/> is no longer called.</summary>
        public float LeanStrength => _lean;

        /// <summary>The offset the shake and the kick add to the followed position this frame, in metres.</summary>
        public Vector3 CurrentOffset => _offset;

        /// <summary>
        /// Shakes the camera: strength 0 to 1 (more is clamped). A new shake never lowers one still running, so several hits in a frame
        /// give one shake of the hardest.
        /// </summary>
        public void Shake(float strength)
        {
            _shake = Mathf.Max(_shake, Mathf.Clamp01(strength));
        }

        /// <summary>Kicks the camera forward along its view and lets it settle back: strength 0 to 1. Used the frame a song is cast.</summary>
        public void Kick(float strength)
        {
            _kick = Mathf.Max(_kick, Mathf.Clamp01(strength));
        }

        /// <summary>
        /// Pushes the camera in toward what it looks at, fast, then eases it back slowly: strength 0 to 1 (more is clamped). Used on
        /// heavy impacts. A new push never lowers one still rising. Scaled by Liveliness.
        /// </summary>
        public void PushIn(float strength)
        {
            _pushHold = Mathf.Max(_pushHold, Mathf.Clamp01(strength));
        }

        /// <summary>Backs the camera away a little while a wave runs and returns it when it ends. Call on the wave's start and end. Scaled by Liveliness.</summary>
        public void SetWavePullBack(bool isWaveRunning)
        {
            _waveRunning = isWaveRunning;
        }

        /// <summary>
        /// Moves what the camera looks at a little toward a point on the ground, for example a hive under attack. It fades when it
        /// is not called, so call it every frame while it applies. Strength 0 to 1. Scaled by Liveliness.
        /// </summary>
        public void LeanToward(Vector3 worldPoint, float strength)
        {
            _leanPoint = worldPoint;
            _leanWant = Mathf.Max(_leanWant, Mathf.Clamp01(strength));
        }

        /// <summary>Swoops to a chase view behind the target for a target of the reference height (6 m).</summary>
        public void EnterChase(Transform target)
        {
            EnterChase(target, _chaseReferenceHeight);
        }

        /// <summary>
        /// Swoops over about 0.8 s to a chase view: behind and above the target, looking along its forward. Distance, pivot height,
        /// look-ahead and the keep-out radius are made for a target 6 m tall and are multiplied by targetHeight / 6 (clamped to
        /// 0.5 to 2), so the giant fills the same share of the screen at 4 m and at 8 m.
        /// </summary>
        /// <param name="target">What to chase. Never null.</param>
        /// <param name="targetHeight">The target's height in metres, from its feet.</param>
        public void EnterChase(Transform target, float targetHeight)
        {
            if (target == null)
            {
                throw new System.ArgumentNullException(nameof(target), "FollowCamera.EnterChase needs a target to chase.");
            }

            bool wasAway = _chaseTarget != target || _chaseBlend <= 0f;
            _chaseTarget = target;
            _isChasing = true;
            _chaseScale = Mathf.Clamp(targetHeight / _chaseReferenceHeight, 0.5f, 2f);

            if (wasAway)
            {
                _chaseYaw = FacingYaw(target, _yawDegrees);
                _chasePitch = _chasePitchDegrees;
                _chasePivot = PivotOf(target);
                _chasePivotVelocity = Vector3.zero;
                _clearDistance = _chaseDistance * _chaseScale;
                _clearVelocity = 0f;
            }

            _yawVelocity = 0f;
            _pitchVelocity = 0f;
            _orbitIdle = _orbitIdleSeconds;
        }

        /// <summary>Swoops back to the top view over about 0.8 s.</summary>
        public void ExitChase()
        {
            _isChasing = false;
        }

        /// <summary>
        /// Turns the chase view around the target: yawDelta in degrees (positive turns the view to the right), pitchDelta in degrees
        /// (positive lifts the camera higher and looks down more). The pitch stays within its limits. Ignored outside a chase.
        /// Restarts the wait before the camera drifts back behind the target.
        /// </summary>
        public void Orbit(float yawDelta, float pitchDelta)
        {
            if (!_isChasing)
            {
                return;
            }

            _chaseYaw = Mathf.Repeat(_chaseYaw + yawDelta, 360f);
            _chasePitch = Mathf.Clamp(_chasePitch + pitchDelta, _chaseMinPitchDegrees, _chaseMaxPitchDegrees);
            _yawVelocity = 0f;
            _pitchVelocity = 0f;
            _orbitIdle = 0f;
        }

        /// <summary>Chooses what to follow and jumps to it at once.</summary>
        public void SetTarget(Transform target)
        {
            _target = target;
            _focusVelocity = Vector3.zero;

            if (target != null)
            {
                _focus = FocusOf(target);
                Apply();
            }
        }

        /// <summary>Blends to another distance, for example the Colossus's camera distance.</summary>
        public void SetDistance(float distance)
        {
            _wantedDistance = Mathf.Max(1f, distance);
        }

        /// <summary>Blends back to the default distance.</summary>
        public void ResetDistance()
        {
            _wantedDistance = _distance;
        }

        private void Awake()
        {
            _wantedDistance = _distance;
            _currentDistance = _distance;
            _chasePitch = _chasePitchDegrees;
            _groundForward = FlatForward(Quaternion.Euler(_pitchDegrees, _yawDegrees, 0f));
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            StepOffset(deltaTime);
            _focus = Vector3.SmoothDamp(_focus, FocusOf(_target), ref _focusVelocity, _followSmoothSeconds, Mathf.Infinity, deltaTime);
            _currentDistance = Mathf.SmoothDamp(_currentDistance, _wantedDistance, ref _distanceVelocity, _distanceSmoothSeconds, Mathf.Infinity, deltaTime);
            StepChase(deltaTime);
            StepEffects(deltaTime);
            Apply();
        }

        private void StepChase(float deltaTime)
        {
            if (_chaseTarget == null)
            {
                _isChasing = false;
                _chaseBlend = 0f;
                return;
            }

            _chaseBlend = Mathf.MoveTowards(_chaseBlend, _isChasing ? 1f : 0f, deltaTime / _chaseBlendSeconds);

            if (_chaseBlend <= 0f && !_isChasing)
            {
                _chaseTarget = null;
                return;
            }

            _chasePivot = Vector3.SmoothDamp(_chasePivot, PivotOf(_chaseTarget), ref _chasePivotVelocity, _chaseFollowSeconds, Mathf.Infinity, deltaTime);
            _orbitIdle += deltaTime;

            if (_isChasing && _orbitIdle >= _orbitIdleSeconds)
            {
                _chaseYaw = Mathf.SmoothDampAngle(_chaseYaw, FacingYaw(_chaseTarget, _chaseYaw), ref _yawVelocity, _orbitReturnSeconds, Mathf.Infinity, deltaTime);
                _chasePitch = Mathf.SmoothDamp(_chasePitch, _chasePitchDegrees, ref _pitchVelocity, _orbitReturnSeconds, Mathf.Infinity, deltaTime);
            }

            StepClearDistance(deltaTime);
        }

        // How far back the chase camera can sit with nothing between it and the target's chest or feet: in front of the
        // nearest thing in the way at once, and back out slowly once it is gone, so passing a tree never makes it jump.
        private void StepClearDistance(float deltaTime)
        {
            // As far back as the effects may take it (the wave's pull-back); a push-in only brings it nearer.
            float wanted = (_chaseDistance * _chaseScale) + Mathf.Max(0f, -EffectPull());
            Vector3 back = -(Quaternion.Euler(_chasePitch, _chaseYaw, 0f) * Vector3.forward);
            Vector3 camera = _chasePivot + (back * wanted);
            float clear = Mathf.Min(SweepClear(_chasePivot, back, wanted), wanted);

            // The low line runs from just above the feet to where the camera would be; the camera moves in by the share of
            // that line that is clear, as both lines meet at the camera.
            Vector3 low = _chaseTarget.position + (Vector3.up * _obstacleLowHeight);
            Vector3 lowLine = camera - low;
            float lowLength = lowLine.magnitude;
            if (lowLength > 0f)
            {
                float lowClear = SweepClear(low, lowLine / lowLength, lowLength);
                clear = Mathf.Min(clear, wanted * (lowClear / lowLength));
            }

            clear = Mathf.Max(1f, clear);
            if (clear < _clearDistance)
            {
                _clearDistance = clear;
                _clearVelocity = 0f;
            }
            else
            {
                _clearDistance = Mathf.SmoothDamp(_clearDistance, clear, ref _clearVelocity, _obstacleReturnSeconds, Mathf.Infinity, deltaTime);
            }
        }

        // The metres along the line from the start that are clear of the obstacle layers, less the margin. What the sweep starts
        // inside (the target's own core, a hive it stands in) and triggers do not count.
        private float SweepClear(Vector3 start, Vector3 direction, float length)
        {
            int count = Physics.SphereCastNonAlloc(
                start, _obstacleRadius, direction, _obstacleHits, length, _chaseObstacleLayers, QueryTriggerInteraction.Ignore);
            float clear = length;
            for (int index = 0; index < count; index++)
            {
                RaycastHit hit = _obstacleHits[index];
                if (hit.distance <= 0f || hit.collider is CharacterController)
                {
                    continue;
                }

                clear = Mathf.Min(clear, hit.distance - _obstacleMargin);
            }

            return clear;
        }

        private void StepEffects(float deltaTime)
        {
            if (_pushShown < _pushHold)
            {
                _pushShown = Mathf.MoveTowards(_pushShown, _pushHold, deltaTime / _pushInSeconds);
            }
            else
            {
                _pushHold = Mathf.Max(0f, _pushHold - (deltaTime / _pushOutSeconds));
                _pushShown = _pushHold;
            }

            _wavePull = Mathf.SmoothDamp(_wavePull, _waveRunning ? 1f : 0f, ref _wavePullVelocity, _wavePullBackSeconds, Mathf.Infinity, deltaTime);
            _lean = Mathf.Max(0f, Mathf.SmoothDamp(_lean, _leanWant, ref _leanVelocity, _leanSeconds, Mathf.Infinity, deltaTime));
            _leanWant = 0f;
        }

        // The shake and the kick are only an offset added when the camera is placed, never to the followed focus, so they cannot fight it.
        private void StepOffset(float deltaTime)
        {
            _shake = Mathf.Max(0f, _shake - (deltaTime / _shakeSeconds));
            _kick = Mathf.Max(0f, _kick - (deltaTime / _kickSeconds));
            _shakeClock += deltaTime * _shakeHertz;

            float amount = _shake * _shake * _maxShakeMetres;
            float x = Mathf.Sin(_shakeClock * 2.1f) * amount;
            float y = Mathf.Sin((_shakeClock * 1.7f) + 1.3f) * amount;
            float push = _kick * _kick * _maxKickMetres;
            _localOffset = new Vector3(x, y, push);
        }

        private Vector3 FocusOf(Transform target)
        {
            return target.position + (Vector3.up * _focusHeight);
        }

        private Vector3 PivotOf(Transform target)
        {
            return target.position + (Vector3.up * _chasePivotHeight * _chaseScale);
        }

        // The compass heading of the target's forward, or the fallback when it faces straight up or down.
        private static float FacingYaw(Transform target, float fallback)
        {
            Vector3 forward = target.forward;
            forward.y = 0f;
            return forward.sqrMagnitude < 1e-6f ? fallback : Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        // How far the effects bring the camera toward what it looks at, in metres. Negative backs it away.
        private float EffectPull()
        {
            float push = Mathf.SmoothStep(0f, 1f, _pushShown) * _pushInMetres;
            float pull = _wavePull * _wavePullBackMetres;
            return (push - pull) * _liveliness;
        }

        private Vector3 LeanFrom(Vector3 from)
        {
            if (_lean <= 0f || _liveliness <= 0f)
            {
                return Vector3.zero;
            }

            Vector3 toward = _leanPoint - from;
            toward.y = 0f;
            float length = toward.magnitude;
            if (length < 1e-3f)
            {
                return Vector3.zero;
            }

            return toward * (Mathf.Min(_leanMetres * Mathf.SmoothStep(0f, 1f, _lean) * _liveliness, length) / length);
        }

        private void Apply()
        {
            float pull = EffectPull();
            Quaternion rotation = Quaternion.Euler(_pitchDegrees, _yawDegrees, 0f);
            Vector3 lookAt = _focus + LeanFrom(_focus);
            Vector3 position = lookAt - (rotation * Vector3.forward * Mathf.Max(1f, _currentDistance - pull));

            if (_chaseBlend > 0f && _chaseTarget != null)
            {
                PlaceChase(pull, out Vector3 chasePosition, out Quaternion chaseRotation);
                float t = Mathf.SmoothStep(0f, 1f, _chaseBlend);
                position = Vector3.Lerp(position, chasePosition, t);
                rotation = Quaternion.Slerp(rotation, chaseRotation, t);
            }

            _offset = rotation * _localOffset;
            _groundForward = FlatForward(rotation);
            transform.SetPositionAndRotation(position + _offset, rotation);
        }

        private void PlaceChase(float pull, out Vector3 position, out Quaternion rotation)
        {
            Vector3 pivot = _chasePivot + LeanFrom(_chasePivot);
            Quaternion aim = Quaternion.Euler(_chasePitch, _chaseYaw, 0f);
            Vector3 heading = Quaternion.Euler(0f, _chaseYaw, 0f) * Vector3.forward;
            float distance = Mathf.Min((_chaseDistance * _chaseScale) - pull, _clearDistance);
            position = pivot - (aim * Vector3.forward * Mathf.Max(1f, distance));

            // Outside the keep-out cylinder around the target, then above the ground.
            Vector3 target = _chaseTarget.position;
            Vector3 flat = new Vector3(position.x - target.x, 0f, position.z - target.z);
            float radius = _keepOutRadius * _chaseScale;
            if (flat.sqrMagnitude < radius * radius)
            {
                flat = flat.sqrMagnitude > 1e-6f ? flat.normalized : -heading;
                position.x = target.x + (flat.x * radius);
                position.z = target.z + (flat.z * radius);
            }

            position.y = Mathf.Max(position.y, _groundHeight + _minHeightAboveGround);

            Vector3 look = pivot + (heading * _chaseLookAhead * _chaseScale) - position;
            rotation = look.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(look.normalized) : aim;
        }

        private static Vector3 FlatForward(Quaternion rotation)
        {
            Vector3 forward = rotation * Vector3.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 1e-6f)
            {
                forward = rotation * Vector3.up;
                forward.y = 0f;
            }

            return forward.sqrMagnitude < 1e-6f ? Vector3.forward : forward.normalized;
        }
    }
}
