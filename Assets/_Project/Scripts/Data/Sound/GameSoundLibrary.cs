using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// The optional recorded sounds of plan section AC: one slot per moment, which replaces the game's own sound for that
    /// moment when it is set. The game runs the same without it. No committed file refers to it: its asset and clips live in
    /// a local, untracked folder because they come from paid packs that the public repo may not publish, and the game finds
    /// it by name (<see cref="ResourceName"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/Game Sound Library", fileName = ResourceName)]
    public sealed class GameSoundLibrary : ScriptableObject
    {
        /// <summary>The asset's name inside a Resources folder, which the game loads it by.</summary>
        public const string ResourceName = "GameSoundLibrary";

        private static readonly int EventCount = Enum.GetValues(typeof(GameSoundEvent)).Length;

        [SerializeField, Tooltip("One slot per moment, in the order of GameSoundEvent.")]
        private GameSoundSlot[] _slots = new GameSoundSlot[EventCount];

        [field: SerializeField, Range(0f, 1f), Tooltip("The volume of every recorded sound.")]
        public float MasterVolume { get; private set; } = 1f;

        [field: SerializeField, Range(0f, 1f), Tooltip("The swarm loop's volume at the smallest swarm, as a share of its full volume at the largest.")]
        public float SwarmLoopQuietShare { get; private set; } = 0.6f;

        /// <summary>The slot for a moment; an empty slot when the library has none for it.</summary>
        public GameSoundSlot Slot(GameSoundEvent kind)
        {
            int index = (int)kind;
            return _slots != null && index >= 0 && index < _slots.Length ? _slots[index] : default;
        }

        /// <summary>Sets a moment's slot. For the Editor tool that fills the local library and for tests; the game never changes it.</summary>
        public void SetSlot(GameSoundEvent kind, GameSoundSlot slot)
        {
            if (_slots == null || _slots.Length < EventCount)
            {
                Array.Resize(ref _slots, EventCount);
            }

            _slots[(int)kind] = slot;
        }
    }
}
