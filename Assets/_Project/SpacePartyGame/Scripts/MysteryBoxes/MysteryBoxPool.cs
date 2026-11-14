using System;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// The pool of mystery boxes. It uses <see cref="SimplePool{T}"/>, takes its size from
    /// <see cref="MysteryBoxSettings"/>, makes more boxes if they are all out at once, and despawns each box when it
    /// is collected or its time is up.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MysteryBoxPool : SimplePool<MysteryBox>
    {
        [SerializeField, Tooltip("How many boxes to make, and how they behave.")]
        private MysteryBoxSettings _settings;

        [SerializeField, Tooltip("The game's stages, so boxes stop their clocks while paused.")]
        private GameFlowManager _flow;

        /// <summary>Raised when a player collects a box, with that player, just before the box goes back.</summary>
        public event Action<MysteryBox, PlayerIdentity> Collected;

        protected override int Size => _settings != null ? _settings.PoolSize : base.Size;

        protected override bool GrowsWhenEmpty => true;

        protected override void Awake()
        {
            if (_settings == null || _flow == null)
            {
                Debug.LogError($"{name}: MysteryBoxPool is missing its settings or game flow.", this);
            }

            base.Awake();
        }

        protected override void OnCreated(MysteryBox box)
        {
            box.Initialize(_settings, _flow);
            box.Collected += HandleCollected;
            box.Expired += Despawn;
        }

        private void HandleCollected(MysteryBox box, PlayerIdentity player)
        {
            Collected?.Invoke(box, player);
            Despawn(box);
        }
    }
}
