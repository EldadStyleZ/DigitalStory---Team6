using System.Collections.Generic;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// One vermin. It strolls at random (see <see cref="VerminWander"/>) and flips its sprite to face the way it
    /// walks. When it hears a player's call it follows that player (see <see cref="VerminFollow"/>), shows a thin
    /// outline in their colour, and passes through cage walls; if it was caged, it first pushes against the bars for
    /// a moment, flattening as it does, before it squeezes through. Once it stops following it first moves clear of any
    /// wall, then the walls are solid to it again.
    /// </summary>
    /// <remarks>Its root sits on the Vermin layer, so cages count it and the map's edge stops it.</remarks>
    [SelectionBase]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class Vermin : MonoBehaviour
    {
        [SerializeField, Tooltip("The sprite child that flips.")]
        private SpriteRenderer _body;

        [SerializeField, Tooltip("Whether the art faces right; if not, the flip is reversed.")]
        private bool _artFacesRight = true;

        [SerializeField, Tooltip("How it fades in when it spawns.")]
        private VerminAppearance _appearance;

        [SerializeField, Tooltip("The thin outline shown in the leader's colour while it follows.")]
        private SpriteRenderer _outline;

        [SerializeField, Tooltip("Which layers hold cage walls.")]
        private LayerMask _cageWallLayers;

        [SerializeField, Tooltip("Which layers hold other vermin, to keep a little space from them.")]
        private LayerMask _verminLayers;

        private readonly List<Collider2D> _wallHits = new List<Collider2D>();
        private readonly List<Collider2D> _neighbourHits = new List<Collider2D>();
        private readonly List<Vector2> _neighbours = new List<Vector2>();
        private ContactFilter2D _verminFilter;
        private float _spotDistance;
        private Vector2 _spotOffset;
        private Rigidbody2D _rigidbody;
        private Knockback _blown;
        private CircleCollider2D _collider;
        private VerminSettings _settings;
        private CageManager _cages;
        private VerminWander _wander;
        private VerminFollow _follow;
        private CageSqueeze _squeeze;
        private Vector2 _squeezeNormal = Vector2.right;
        private Vector3 _bodyScale;
        private Vector3 _outlineScale;
        private PlayerIdentity _leader;
        private PlayerMovement _leaderMovement;
        private ContactFilter2D _wallFilter;
        private float _startFacingLeftChance = 0.5f;
        private int _facing = 1;
        private bool _passesThroughCages;

        /// <summary>How it fades in when it spawns.</summary>
        public VerminAppearance Appearance => _appearance;

        /// <summary>Which way it faces: 1 right, -1 left.</summary>
        public int Facing => _facing;

        /// <summary>Whether it is following a player now.</summary>
        public bool IsFollowing => _follow != null && _follow.IsFollowing(Time.time);

        /// <summary>The player it follows now, or null.</summary>
        public PlayerIdentity Leader => IsFollowing ? _leader : null;

        /// <summary>Whether cage walls are open to it, while it follows or is still moving clear of a wall.</summary>
        public bool PassesThroughCages => _passesThroughCages;

        /// <summary>Gives the vermin its settings and the cages. The pool calls it once for every vermin it makes.</summary>
        public void Initialize(VerminSettings settings, CageManager cages)
        {
            _cages = cages;

            if (settings == null)
            {
                return;
            }

            _settings = settings;
            _wander = new VerminWander(settings.WanderSpeed, settings.MinWanderSeconds, settings.MaxWanderSeconds,
                settings.PauseChance, () => Random.value);
            _follow = new VerminFollow(settings.FollowSeconds);
            _squeeze = new CageSqueeze(settings.SqueezeSeconds);
            _startFacingLeftChance = settings.StartFacingLeftChance;
        }

        /// <summary>
        /// It hears a player's call, one that started at a given time. It ignores its own cage owner, and otherwise
        /// follows whoever called last.
        /// </summary>
        public void HearCall(PlayerIdentity caller, float callStartedAt)
        {
            if (_follow == null || caller == null)
            {
                return;
            }

            Cage cage = CurrentCage();
            int? owner = cage != null && cage.Owner != null ? cage.Owner.Slot : (int?)null;

            if (!CallRules.Hears(caller.Slot, owner))
            {
                return;
            }

            bool wasFollowingThem = IsFollowing && _leader == caller;
            _follow.Hear(caller.Slot, callStartedAt, Time.time);

            if (_follow.Leader == caller.Slot)
            {
                if (!wasFollowingThem)
                {
                    PickASpot();
                }

                _leader = caller;
                _leaderMovement = caller.GetComponent<PlayerMovement>();

                // A caged vermin has to squeeze out through the bars first; a free one just passes through walls.
                if (!_passesThroughCages && !_squeeze.IsActive)
                {
                    if (cage != null)
                    {
                        _squeeze.Begin();
                    }
                    else
                    {
                        SetPassThroughCages(true);
                    }
                }
            }
        }

        /// <summary>Whether a blow is carrying it now.</summary>
        public bool IsBlown => _blown.IsActive(Time.time);

        /// <summary>
        /// A blow carries it away: it stops following, and cage walls do not stop it while the shove lasts. Once
        /// it stops, it walks clear of any wall it ended up in, as after following.
        /// </summary>
        public void Blow(Vector2 velocity, float seconds)
        {
            StopFollowing();
            _squeeze?.End();
            _blown = new Knockback(velocity, Time.time, seconds);
            SetPassThroughCages(true);
        }

        /// <summary>Stops following its leader now, such as when the round ends.</summary>
        public void StopFollowing()
        {
            _follow?.Stop();
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<CircleCollider2D>();
            _wallFilter = new ContactFilter2D { useLayerMask = true, layerMask = _cageWallLayers, useTriggers = false };
            _verminFilter = new ContactFilter2D { useLayerMask = true, layerMask = _verminLayers, useTriggers = false };

            if (_body == null || _appearance == null || _outline == null)
            {
                Debug.LogError($"{name}: Vermin is missing its body sprite, appearance or outline.", this);
                return;
            }

            _bodyScale = _body.transform.localScale;
            _outlineScale = _outline.transform.localScale;
        }

        // A reused vermin starts fresh: a new direction, a random facing, following no one, walls solid again
        // (Unity forgets ignored collisions when a collider is switched off).
        private void OnEnable()
        {
            _wander?.Restart();
            _follow?.Stop();
            _squeeze?.End();
            _blown = default;
            _leader = null;
            _leaderMovement = null;
            _passesThroughCages = false;
            _facing = VerminFacing.AtSpawn(Random.value, _startFacingLeftChance);
            ShowFacing();
            _outline.enabled = false;
        }

        private void OnDisable()
        {
            _rigidbody.linearVelocity = Vector2.zero;
        }

        private void FixedUpdate()
        {
            if (_wander == null)
            {
                return;
            }

            if (IsBlown)
            {
                _rigidbody.linearVelocity = _blown.Velocity(Time.time);
                return;
            }

            if (IsFollowing)
            {
                Cage cage = CurrentCage();

                // Walking into its leader's own cage ends the follow: it stays caught.
                if (cage != null && cage.Owner == _leader)
                {
                    _follow.Stop();
                }
                else
                {
                    Vector2 spot = (Vector2)_leader.transform.position + _spotOffset;
                    float speed = FollowSpeedNow();
                    Vector2 follow = Steering.Toward(_rigidbody.position, spot, _spotDistance, speed);
                    _rigidbody.linearVelocity = WithSpacing(follow, speed);

                    if (_squeeze.IsActive)
                    {
                        PushAgainstBars(spot - _rigidbody.position);
                    }

                    return;
                }
            }

            _squeeze.End();

            if (_passesThroughCages && MoveClearOfWalls())
            {
                return;
            }

            _rigidbody.linearVelocity = WithSpacing(_wander.Tick(Time.fixedDeltaTime), _settings.FollowSpeed);
        }

        // Its own place near the leader, so followers do not all head for one point.
        private void PickASpot()
        {
            _spotDistance = FollowSpot.Distance(_settings.FollowDistance, _settings.FollowDistanceSpread, Random.value);
            _spotOffset = FollowSpot.Offset(_settings.FollowOffsetRadius, Random.value, Random.value);
        }

        // The follow speed now: faster while the leader runs, so followers are not left behind.
        private float FollowSpeedNow() => _leaderMovement == null
            ? _settings.FollowSpeed
            : FollowPace.Speed(_settings.FollowSpeed, _leaderMovement.WalkSpeed, _leaderMovement.TopSpeed, _settings.MatchLeaderRun);

        // Adds a nudge away from close neighbours, never going faster than the top speed given.
        private Vector2 WithSpacing(Vector2 velocity, float topSpeed)
        {
            _neighbours.Clear();
            Physics2D.OverlapCircle(_rigidbody.position, _settings.SpacingRadius, _verminFilter, _neighbourHits);

            foreach (Collider2D hit in _neighbourHits)
            {
                if (hit != _collider)
                {
                    _neighbours.Add(hit.attachedRigidbody != null ? hit.attachedRigidbody.position : (Vector2)hit.transform.position);
                }
            }

            Vector2 push = VerminSpacing.Push(_rigidbody.position, _neighbours, _settings.SpacingRadius, _settings.SpacingStrength);
            return Vector2.ClampMagnitude(velocity + push, topSpeed);
        }

        private void Update()
        {
            _facing = FacingRule.Next(_facing, _rigidbody.linearVelocity.x);
            ShowFacing();
            ShowOutline();
            ShowSqueeze();
        }

        // While squeezing out of a cage, the walls stay solid. Time counts only while it touches a wall and heads
        // out through it; once it has pushed long enough, the walls open to it.
        private void PushAgainstBars(Vector2 heading)
        {
            Physics2D.OverlapCircle(_rigidbody.position, WorldRadius() * 1.15f, _wallFilter, _wallHits);
            Cage cage = CageAmong(_wallHits);

            if (cage == null)
            {
                return;
            }

            Vector2 normal = CageSqueeze.WallNormal(_rigidbody.position - cage.Centre, cage.InnerHalfSize);

            if (Vector2.Dot(heading, normal) <= 0f)
            {
                return;
            }

            _squeezeNormal = normal;

            if (_squeeze.Press(Time.fixedDeltaTime))
            {
                _squeeze.End();
                SetPassThroughCages(true);
            }
        }

        // Flattens the sprite and outline against the bars as the squeeze goes on; back to normal once through.
        private void ShowSqueeze()
        {
            if (_squeeze == null)
            {
                return;
            }

            Vector2 scale = CageSqueeze.Scale(_squeezeNormal, _squeeze.Progress, _settings.SqueezeAmount);
            var factor = new Vector3(scale.x, scale.y, 1f);
            _body.transform.localScale = Vector3.Scale(_bodyScale, factor);
            _outline.transform.localScale = Vector3.Scale(_outlineScale, factor);
        }

        private float WorldRadius() => _collider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);

        private static Cage CageAmong(List<Collider2D> hits)
        {
            foreach (Collider2D hit in hits)
            {
                Cage cage = hit.GetComponentInParent<Cage>();

                if (cage != null)
                {
                    return cage;
                }
            }

            return null;
        }

        // After following, it may stand in a wall. Until it is clear it keeps passing through walls and walks out
        // of the wall: into the cage if it is more inside than out, away from it otherwise. Then walls are solid.
        private bool MoveClearOfWalls()
        {
            Physics2D.OverlapCircle(_rigidbody.position, WorldRadius(), _wallFilter, _wallHits);
            Cage cage = CageAmong(_wallHits);

            if (cage == null)
            {
                SetPassThroughCages(false);
                _wander.Restart();
                return false;
            }

            Vector2 offset = _rigidbody.position - cage.Centre;
            Vector2 wallMiddle = (cage.InnerHalfSize + cage.OuterHalfSize) * 0.5f;
            bool isMostlyInside = Mathf.Abs(offset.x) < wallMiddle.x && Mathf.Abs(offset.y) < wallMiddle.y;
            Vector2 away = offset.sqrMagnitude > 1e-6f ? offset.normalized : Vector2.up;
            _rigidbody.linearVelocity = (isMostlyInside ? -away : away) * _settings.FollowSpeed;
            return true;
        }

        private void SetPassThroughCages(bool on)
        {
            if (_passesThroughCages == on || _cages == null)
            {
                return;
            }

            _passesThroughCages = on;

            foreach (Cage cage in _cages.Cages)
            {
                foreach (Collider2D wall in cage.Walls)
                {
                    Physics2D.IgnoreCollision(_collider, wall, on);
                }
            }
        }

        private Cage CurrentCage() => _cages != null ? _cages.CageOf(transform) : null;

        private void ShowFacing()
        {
            _body.flipX = _artFacesRight ? _facing < 0 : _facing > 0;
        }

        // The outline matches the body's flip and fade, in the leader's colour.
        private void ShowOutline()
        {
            PlayerIdentity leader = Leader;
            _outline.enabled = leader != null;

            if (leader == null)
            {
                return;
            }

            Color colour = leader.Colour;
            colour.a = _body.color.a;
            _outline.color = colour;
            _outline.flipX = _body.flipX;
        }
    }
}
