using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Shows hits, area attacks and arriving creatures on reused sprites.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Lab/Lab Effects View")]
    public sealed class LabEffectsView : MonoBehaviour
    {
        private const int EffectOrder = 8;
        private const float StartingRing = 0.5f;
        private const float SummonRing = 3f;

        private LabSession _session;
        private TownArtConfig _sprites;
        private LabArtConfig _art;
        private EffectPool _pool;

        /// <summary>How many effects are on screen, for tests and debugging.</summary>
        public int ActiveCount => _pool == null ? 0 : _pool.ActiveCount;

        /// <summary>Supplies what to show. Call once, before this object starts.</summary>
        public void Initialize(LabSession session, TownArtConfig sprites, LabArtConfig art)
        {
            _session = session;
            _sprites = sprites;
            _art = art;
        }

        private void Start()
        {
            // LabRoot has already reported the problem if nothing was supplied.
            if (_session == null)
            {
                return;
            }

            _pool = new EffectPool(transform);
            _session.Battle.Struck += HandleStruck;
            _session.Summoned += HandleSummoned;
        }

        private void Update() => _pool?.Tick(Time.deltaTime);

        private void OnDestroy()
        {
            // Unity also calls this when the scene unloads before Start ran, so _pool can be null.
            if (_pool != null)
            {
                _session.Battle.Struck -= HandleStruck;
                _session.Summoned -= HandleSummoned;
            }
        }

        private void HandleStruck(StrikeReport strike)
        {
            if (strike.AreaRadius > 0f)
            {
                SpawnRing(strike.Position, strike.AreaRadius * 2f, strike.IsByCreature ? _art.CreatureAreaColor : _art.MinionAreaColor);
            }
            else
            {
                Spawn(_sprites.DotSprite, strike.Position, _art.HitSize, 0f, _art.HitColor);
            }
        }

        private void HandleSummoned(CreatureType creature)
        {
            SpawnRing(_session.Piper.Position, SummonRing, _art.SummonColor);
        }

        private void SpawnRing(Vector2 position, float width, Color color)
        {
            float growth = (width - StartingRing) / _art.EffectSeconds;
            Spawn(_sprites.RingSprite, position, StartingRing, growth, color);
        }

        // The pool works in town units, so tiles are converted on the way in.
        private void Spawn(Sprite sprite, Vector2 position, float size, float growth, Color color)
        {
            float townPerTile = 1f / TownSpace.ToSceneLength(1f);
            _pool.Spawn(new EffectSpawn(
                sprite,
                EffectOrder,
                TownSpace.ToTown(position),
                Vector2.zero,
                color,
                size * townPerTile,
                growth * townPerTile,
                1f,
                _art.EffectSeconds,
                _art.EffectSeconds));
        }
    }
}
