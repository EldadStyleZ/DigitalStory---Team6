using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// The pool of vermin. It uses <see cref="SimplePool{T}"/> as it is, takes its size from
    /// <see cref="VerminSettings"/>, and gives each vermin the settings as it is made.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VerminPool : SimplePool<Vermin>
    {
        [SerializeField, Tooltip("How many vermin to make, and how they stroll.")]
        private VerminSettings _settings;

        [SerializeField, Tooltip("The cages, so each vermin knows whose cage it is in.")]
        private CageManager _cages;

        protected override int Size => _settings != null ? _settings.PoolSize : base.Size;

        protected override void Awake()
        {
            if (_settings == null || _cages == null)
            {
                Debug.LogError($"{name}: VerminPool is missing its settings or cages.", this);
            }

            base.Awake();
        }

        protected override void OnCreated(Vermin vermin)
        {
            vermin.Initialize(_settings, _cages);
        }
    }
}
