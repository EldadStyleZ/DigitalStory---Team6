using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpacePartyGame
{
    /// <summary>The six notes a player can play.</summary>
    public enum Note
    {
        Root,
        Up,
        Left,
        Right,
        LT,
        RT,
    }

    /// <summary>
    /// Turns the player's six note actions into events. Instruments listen to <see cref="NotePressed"/> and
    /// <see cref="NoteReleased"/>; this class makes no sound itself.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerNotes : MonoBehaviour
    {
        private static readonly (Note Note, string Action)[] Bindings =
        {
            (Note.Root, "Player/NoteRoot"),
            (Note.Up, "Player/NoteUp"),
            (Note.Left, "Player/NoteLeft"),
            (Note.Right, "Player/NoteRight"),
            (Note.LT, "Player/NoteLT"),
            (Note.RT, "Player/NoteRT"),
        };

        private InputAction[] _actions;
        private readonly NotePressLatch _latch = new NotePressLatch();

        /// <summary>Raised when a note's button goes down.</summary>
        public event Action<Note> NotePressed;

        /// <summary>Raised when a note's button comes up.</summary>
        public event Action<Note> NoteReleased;

        private bool _canPlay = true;

        /// <summary>Whether a note's button is held now (never while playing is switched off).</summary>
        // public bool IsHeld(Note note) => _actions[(int)note].IsPressed();
        public bool IsHeld(Note note) => _canPlay && _actions[(int)note].IsPressed();

        /// <summary>
        /// Whether the player may play notes. Switching it off releases every note, so nothing keeps sounding, and
        /// ignores presses until it is switched on again.
        /// </summary>
        public bool CanPlay
        {
            get => _canPlay;
            set
            {
                if (_canPlay == value)
                {
                    return;
                }

                _canPlay = value;

                if (!value)
                {
                    _latch.ReleaseAll();

                    foreach (Note note in Enum.GetValues(typeof(Note)))
                    {
                        NoteReleased?.Invoke(note);
                    }
                }
            }
        }

        private void Awake()
        {
            InputActionAsset asset = GetComponent<PlayerInput>().actions;
            _actions = new InputAction[Bindings.Length];

            foreach ((Note note, string action) in Bindings)
            {
                _actions[(int)note] = asset.FindAction(action, true);
            }
        }

        private void OnEnable()
        {
            // Performed, not started: a trigger counts as pressed past its press point, not at the lightest touch.
            foreach (InputAction action in _actions)
            {
                action.performed += HandlePerformed;
                action.canceled += HandleCanceled;
            }
        }

        private void OnDisable()
        {
            foreach (InputAction action in _actions)
            {
                action.performed -= HandlePerformed;
                action.canceled -= HandleCanceled;
            }
        }

        private void HandlePerformed(InputAction.CallbackContext context)
        {
            Note note = NoteOf(context.action);

            if (!_canPlay || !_latch.Press(note))
            {
                return;
            }

            NotePressed?.Invoke(note);
        }

        private void HandleCanceled(InputAction.CallbackContext context)
        {
            Note note = NoteOf(context.action);

            if (!_canPlay || !_latch.Release(note))
            {
                return;
            }

            NoteReleased?.Invoke(note);
        }

        private Note NoteOf(InputAction action) => (Note)Array.IndexOf(_actions, action);
    }
}
