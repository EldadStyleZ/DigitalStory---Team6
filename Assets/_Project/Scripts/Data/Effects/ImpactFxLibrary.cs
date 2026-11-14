using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// The optional impact effects of plan section AB: one particle prefab per moment (a hit, a kill, a nest's burst, the
    /// Meteor's landing, the giant's slam), layered over the game's own effects. The game runs the same without it. No
    /// committed file refers to it: its one asset lives in a local, untracked folder because its prefabs come from a paid pack
    /// that the public repo may not publish, and the game finds it by name (<see cref="ResourceName"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/Impact Fx Library", fileName = ResourceName)]
    public sealed class ImpactFxLibrary : ScriptableObject
    {
        /// <summary>The asset's name inside a Resources folder, which the game loads it by.</summary>
        public const string ResourceName = "ImpactFxLibrary";

        private static readonly int KindCount = Enum.GetValues(typeof(ImpactFxKind)).Length;

        [SerializeField, Tooltip("One slot per moment, in the order of ImpactFxKind: Hit, Death, NestBurst, MeteorLanding, GiantSlam.")]
        private ImpactFxSlot[] _slots = new ImpactFxSlot[KindCount];

        [field: SerializeField, Min(0f), Tooltip("The least damage of a hit that does not kill and still shows the hit effect, so a stream of small hits stays clean.")]
        public float HitLeastDamage { get; private set; } = 1f;

        /// <summary>The slot for a moment; an empty slot when the library has none for it.</summary>
        public ImpactFxSlot Slot(ImpactFxKind kind)
        {
            int index = (int)kind;
            return _slots != null && index >= 0 && index < _slots.Length ? _slots[index] : default;
        }

        /// <summary>Sets a moment's slot. For the Editor tool that fills the local library and for tests; the game never changes it.</summary>
        public void SetSlot(ImpactFxKind kind, ImpactFxSlot slot)
        {
            if (_slots == null || _slots.Length < KindCount)
            {
                Array.Resize(ref _slots, KindCount);
            }

            _slots[(int)kind] = slot;
        }

        /// <summary>Sets <see cref="HitLeastDamage"/>. For the Editor tool and for tests; the game never changes it.</summary>
        public void SetHitLeastDamage(float damage)
        {
            HitLeastDamage = Mathf.Max(0f, damage);
        }
    }
}
