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

        /// <summary>Raised when a note's button goes down.</summary>
        public event Action<Note> NotePressed;

        /// <summary>Raised when a note's button comes up.</summary>
        public event Action<Note> NoteReleased;

        /// <summary>Whether a note's button is held now.</summary>
        public bool IsHeld(Note note) => _actions[(int)note].IsPressed();

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
            foreach (InputAction action in _actions)
            {
                action.started += HandleStarted;
                action.canceled += HandleCanceled;
            }
        }

        private void OnDisable()
        {
            foreach (InputAction action in _actions)
            {
                action.started -= HandleStarted;
                action.canceled -= HandleCanceled;
            }
        }

        private void HandleStarted(InputAction.CallbackContext context)
        {
            NotePressed?.Invoke(NoteOf(context.action));
        }

        private void HandleCanceled(InputAction.CallbackContext context)
        {
            NoteReleased?.Invoke(NoteOf(context.action));
        }

        private Note NoteOf(InputAction action) => (Note)Array.IndexOf(_actions, action);
    }
}
