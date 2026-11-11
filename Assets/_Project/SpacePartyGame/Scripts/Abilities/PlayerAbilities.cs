using System.Collections.Generic;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// What a player's notes do. For now only the root note does something: while it plays, the player calls, a
    /// ring in their colour shows the call's reach, and every vermin inside it hears the call and follows. The other
    /// five notes only make sound.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInstrument), typeof(PlayerIdentity))]
    public sealed class PlayerAbilities : MonoBehaviour
    {
        [Header("Call (root note)")]
        [SerializeField, Tooltip("The ring shown while calling: the player's Call Area child, centred on the body. The call reaches out from its centre.")]
        private CallArea _callArea;

        [SerializeField, Min(0.1f), Tooltip("How far the call reaches from the player, in units.")]
        private float _callRadius = 2.5f;

        [SerializeField, Tooltip("Which layers hold vermin.")]
        private LayerMask _verminLayers;

        private readonly List<Collider2D> _heard = new List<Collider2D>();
        private PlayerInstrument _instrument;
        private PlayerIdentity _identity;
        private ContactFilter2D _filter;

        /// <summary>Whether the player is calling now.</summary>
        public bool IsCalling => _instrument.IsPlaying(Note.Root);

        /// <summary>How far the call reaches.</summary>
        public float CallRadius => _callRadius;

        private void Awake()
        {
            _instrument = GetComponent<PlayerInstrument>();
            _identity = GetComponent<PlayerIdentity>();
            _filter = new ContactFilter2D { useLayerMask = true, layerMask = _verminLayers, useTriggers = false };

            if (_callArea == null)
            {
                Debug.LogError($"{name}: PlayerAbilities is missing its call area child.", this);
                return;
            }

            _callArea.SetRadius(_callRadius);
            _callArea.HideNow();
        }

        // The middle of the call: the Call Area child, which sits on the player's body.
        private Vector2 CallCentre => _callArea != null ? (Vector2)_callArea.transform.position : (Vector2)transform.position;

        private void OnEnable()
        {
            _instrument.NoteStarted += HandleNoteStarted;
            _instrument.NoteEnded += HandleNoteEnded;
        }

        private void OnDisable()
        {
            _instrument.NoteStarted -= HandleNoteStarted;
            _instrument.NoteEnded -= HandleNoteEnded;
        }

        private void HandleNoteStarted(Note note)
        {
            if (note == Note.Root && _callArea != null)
            {
                _callArea.Show(_identity.Colour);
            }
        }

        private void HandleNoteEnded(Note note)
        {
            if (note == Note.Root && _callArea != null)
            {
                _callArea.Hide();
            }
        }

        private void FixedUpdate()
        {
            if (!IsCalling)
            {
                return;
            }

            float callStartedAt = _instrument.StartedAt(Note.Root);
            Physics2D.OverlapCircle(CallCentre, _callRadius, _filter, _heard);

            foreach (Collider2D collider in _heard)
            {
                Rigidbody2D body = collider.attachedRigidbody;
                Vermin vermin = body != null ? body.GetComponent<Vermin>() : collider.GetComponent<Vermin>();

                if (vermin != null)
                {
                    vermin.HearCall(_identity, callStartedAt);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(CallCentre, _callRadius);
        }
    }
}
