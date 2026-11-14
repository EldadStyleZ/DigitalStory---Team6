using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// The optional recorded sounds of plan section AC: one slot per moment (<see cref="GameSoundEvent"/>), which replaces the
    /// game's own synthesised sound for that moment when it is set, and the numbers of the swarm's hum and of the ducking under
    /// big moments. The game runs the same without it. No committed file refers to it: its asset and clips live in a local,
    /// untracked folder because they come from paid packs that the public repo may not publish, and the game finds it by name
    /// (<see cref="ResourceName"/>), as it finds the impact effects of section AB.
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
        public float MasterVolume { get; private set; } = 0.8f;

        [field: Header("Swarm hum")]
        [field: SerializeField, Min(1f), Tooltip("The swarm at which the hum is at its full volume. A smaller swarm hums more quietly.")]
        public float HumFullWasps { get; private set; } = 4000f;

        [field: SerializeField, Range(0f, 1f), Tooltip("The hum's volume for a swarm of a single wasp, as a share of its full volume. No wasps, no hum.")]
        public float HumQuietShare { get; private set; } = 0.35f;

        [field: SerializeField, Range(0.5f, 1f), Tooltip("The hum's pitch while the core stands still.")]
        public float HumRestPitch { get; private set; } = 0.94f;

        [field: SerializeField, Range(1f, 2f), Tooltip("The hum's pitch at the core's top speed.")]
        public float HumFastPitch { get; private set; } = 1.1f;

        [field: SerializeField, Min(0.1f), Tooltip("The core's speed, in metres a second, that gives the top pitch: the core's walking speed.")]
        public float HumFastMetresPerSecond { get; private set; } = 4.5f;

        [field: SerializeField, Min(0.01f), Tooltip("Seconds the hum takes to follow a change of size or speed, so it never steps.")]
        public float HumFollowSeconds { get; private set; } = 0.25f;

        [field: SerializeField, Min(0.01f), Tooltip("Seconds the hum takes to cross-fade to the angry buzz and back.")]
        public float AngryFadeSeconds { get; private set; } = 0.35f;

        [field: Header("Ducking")]
        [field: SerializeField, Range(-24f, 0f), Tooltip("How far a big moment (a slam, the Meteor, a hive lost, the giant forming, a stinger) lowers the hum, in decibels.")]
        public float DuckDecibels { get; private set; } = -6f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the hum stays lowered after a big moment.")]
        public float DuckHoldSeconds { get; private set; } = 0.3f;

        [field: SerializeField, Min(0.001f), Tooltip("Seconds the hum takes to dip.")]
        public float DuckAttackSeconds { get; private set; } = 0.03f;

        [field: SerializeField, Min(0.001f), Tooltip("Seconds the hum takes to come back.")]
        public float DuckReleaseSeconds { get; private set; } = 0.15f;

        /// <summary>The slot for a moment; an empty slot when the library has none for it.</summary>
        public GameSoundSlot Slot(GameSoundEvent kind)
        {
            int index = (int)kind;
            return _slots != null && index >= 0 && index < _slots.Length ? _slots[index] : default;
        }

        /// <summary>True when at least one moment has a recorded sound.</summary>
        public bool HasAnySound
        {
            get
            {
                for (int index = 0; index < EventCount; index++)
                {
                    if (Slot((GameSoundEvent)index).IsSet)
                    {
                        return true;
                    }
                }

                return false;
            }
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

        /// <summary>Empties every slot. For the Editor tool, before it fills the library from its recipe.</summary>
        public void ClearSlots()
        {
            _slots = new GameSoundSlot[EventCount];
        }

        /// <summary>Sets <see cref="MasterVolume"/>. For the Editor tool and for tests; the game never changes it.</summary>
        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
        }
    }
}
