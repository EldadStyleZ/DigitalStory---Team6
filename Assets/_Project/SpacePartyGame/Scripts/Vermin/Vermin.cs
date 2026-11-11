using System.Collections.Generic;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// One vermin. It strolls at random (see <see cref="VerminWander"/>) and flips its sprite to face the way it
    /// walks. When it hears a player's call it follows that player (see <see cref="VerminFollow"/>), shows a thin
    /// outline in their colour, and passes through cage walls; once it stops following it first moves clear of any
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

        private readonly List<Collider2D> _wallHits = new List<Collider2D>();
        private Rigidbody2D _rigidbody;
        private CircleCollider2D _collider;
        private VerminSettings _settings;
        private CageManager _cages;
        private VerminWander _wander;
        private VerminFollow _follow;
        private PlayerIdentity _leader;
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

            _follow.Hear(caller.Slot, callStartedAt, Time.time);

            if (_follow.Leader == caller.Slot)
            {
                _leader = caller;
                SetPassThroughCages(true);
            }
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<CircleCollider2D>();
            _wallFilter = new ContactFilter2D { useLayerMask = true, layerMask = _cageWallLayers, useTriggers = false };

            if (_body == null || _appearance == null || _outline == null)
            {
                Debug.LogError($"{name}: Vermin is missing its body sprite, appearance or outline.", this);
            }
        }

        // A reused vermin starts fresh: a new direction, a random facing, following no one, walls solid again
        // (Unity forgets ignored collisions when a collider is switched off).
        private void OnEnable()
        {
            _wander?.Restart();
            _follow?.Stop();
            _leader = null;
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
                    _rigidbody.linearVelocity = Steering.Toward(_rigidbody.position, _leader.transform.position,
                        _settings.FollowDistance, _settings.FollowSpeed);
                    return;
                }
            }

            if (_passesThroughCages && MoveClearOfWalls())
            {
                return;
            }

            _rigidbody.linearVelocity = _wander.Tick(Time.fixedDeltaTime);
        }

        private void Update()
        {
            _facing = FacingRule.Next(_facing, _rigidbody.linearVelocity.x);
            ShowFacing();
            ShowOutline();
        }

        // After following, it may stand in a wall. Until it is clear it keeps passing through walls and walks out
        // of the wall: into the cage if it is more inside than out, away from it otherwise. Then walls are solid.
        private bool MoveClearOfWalls()
        {
            float radius = _collider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            Physics2D.OverlapCircle(_rigidbody.position, radius, _wallFilter, _wallHits);

            Cage cage = null;

            foreach (Collider2D hit in _wallHits)
            {
                cage = hit.GetComponentInParent<Cage>();

                if (cage != null)
                {
                    break;
                }
            }

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
