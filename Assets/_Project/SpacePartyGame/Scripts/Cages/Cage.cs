using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// A player's cage: four invisible walls along its painted bars that vermin cannot cross (players pass, by
    /// layer), and an interior zone that knows which vermin are inside.
    /// </summary>
    /// <remarks>
    /// The contents are worked out again every physics step from what overlaps the interior, keeping only active
    /// vermin whose centre is inside. Nothing is counted up or down on enter and exit, so a vermin that is pooled,
    /// disabled or moved away can never leave a stale count behind.
    /// </remarks>
    [SelectionBase]
    [DisallowMultipleComponent]
    public sealed class Cage : MonoBehaviour
    {
        [SerializeField, Tooltip("The cage picture.")]
        private SpriteRenderer _body;

        [SerializeField, Tooltip("The trigger inside the bars, on the CageWall layer.")]
        private BoxCollider2D _interior;

        [SerializeField, Tooltip("Half the size of the walls' outer edge at scale 1, in units; multiplied by the cage's scale.")]
        private Vector2 _outerHalfSize = new Vector2(0.93f, 0.98f);

        [SerializeField, Tooltip("Which layers count as vermin.")]
        private LayerMask _verminLayers;

        private readonly List<Collider2D> _overlaps = new List<Collider2D>();
        private readonly HashSet<Transform> _inside = new HashSet<Transform>();
        private readonly Dictionary<int, Transform> _byId = new Dictionary<int, Transform>();
        private ContactFilter2D _filter;

        /// <summary>Raised when vermin come in or go out.</summary>
        public event Action<Cage> ContentsChanged;

        /// <summary>The player this cage belongs to.</summary>
        public PlayerIdentity Owner { get; private set; }

        /// <summary>Half the size of the cage's outer edge in world units (scaled with the cage), for placing cages.</summary>
        // public Vector2 OuterHalfSize => _outerHalfSize;
        public Vector2 OuterHalfSize => Vector2.Scale(_outerHalfSize, transform.lossyScale);

        /// <summary>How many vermin are inside.</summary>
        public int Count => _inside.Count;

        /// <summary>The vermin inside.</summary>
        public IReadOnlyCollection<Transform> Contents => _inside;

        /// <summary>Whether a vermin is inside this cage.</summary>
        public bool Holds(Transform vermin) => _inside.Contains(vermin);

        /// <summary>The four solid walls, so a following vermin can pass through them.</summary>
        public IReadOnlyList<Collider2D> Walls => _walls;

        /// <summary>The middle of the cage, in world space.</summary>
        public Vector2 Centre => _interior.bounds.center;

        /// <summary>Half the size of the space inside the walls.</summary>
        public Vector2 InnerHalfSize => _interior.bounds.extents;

        private readonly List<Collider2D> _walls = new List<Collider2D>();

        /// <summary>Gives the cage its owner and the owner's cage picture.</summary>
        public void Initialize(PlayerIdentity owner, Sprite picture)
        {
            Owner = owner;
            _body.sprite = picture;
        }

        private void Awake()
        {
            if (_body == null || _interior == null)
            {
                Debug.LogError($"{name}: Cage is missing its body or interior.", this);
            }

            _filter = new ContactFilter2D { useLayerMask = true, layerMask = _verminLayers, useTriggers = false };

            foreach (Collider2D collider in GetComponentsInChildren<Collider2D>(true))
            {
                if (collider != _interior && !collider.isTrigger)
                {
                    _walls.Add(collider);
                }
            }
        }

        private void FixedUpdate()
        {
            Recount();
        }

        private void OnDisable()
        {
            if (_inside.Count > 0)
            {
                _inside.Clear();
                ContentsChanged?.Invoke(this);
            }
        }

        private void Recount()
        {
            _overlaps.Clear();
            _byId.Clear();
            _interior.Overlap(_filter, _overlaps);

            var candidates = new List<(int Id, Vector2 Centre, bool Active)>(_overlaps.Count);
            var seen = new HashSet<Transform>();

            foreach (Collider2D collider in _overlaps)
            {
                Transform vermin = collider.attachedRigidbody != null ? collider.attachedRigidbody.transform : collider.transform;

                if (!seen.Add(vermin))
                {
                    continue;
                }

                int id = candidates.Count;
                _byId[id] = vermin;
                Vector2 centre = collider.attachedRigidbody != null ? collider.attachedRigidbody.position : (Vector2)vermin.position;
                candidates.Add((id, centre, collider.isActiveAndEnabled));
            }

            Bounds area = _interior.bounds;
            ISet<int> inside = CageCounting.Inside(candidates, new Rect(area.min, area.size));

            var now = new HashSet<Transform>();

            foreach (int id in inside)
            {
                now.Add(_byId[id]);
            }

            if (now.SetEquals(_inside))
            {
                return;
            }

            _inside.Clear();
            _inside.UnionWith(now);
            ContentsChanged?.Invoke(this);
        }
    }
}
