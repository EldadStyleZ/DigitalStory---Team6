using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>The player's character: walks the streets and wades into the shallows.</summary>
    public sealed class Piper
    {
        private const float FullTurn = Mathf.PI * 2f;

        private readonly PiperConfig _config;
        private readonly TownConfig _town;
        private readonly RiverBank _bank;
        private readonly HouseCollider _houses;

        private Vector2 _position;

        public Piper(PiperConfig config, TownConfig town, RiverBank bank, HouseCollider houses, Vector2 start)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _town = town != null ? town : throw new ArgumentNullException(nameof(town));
            _bank = bank ?? throw new ArgumentNullException(nameof(bank));
            _houses = houses ?? throw new ArgumentNullException(nameof(houses));
            _position = start;
            Facing = config.StartFacing;
        }

        public Vector2 Position => _position;

        /// <summary>The direction the Piper faces, in radians.</summary>
        public float Facing { get; private set; }

        public bool IsMoving { get; private set; }
        public bool IsWading { get; private set; }

        public void Tick(Vector2 move, float deltaTime)
        {
            IsWading = _position.x > _bank.EdgeAt(_position.y);
            IsMoving = move != Vector2.zero;

            if (IsMoving)
            {
                Walk(move.normalized, deltaTime);
            }

            _houses.PushOut(ref _position, _config.Radius);
            StayInBounds();
        }

        private void Walk(Vector2 direction, float deltaTime)
        {
            float speed = IsWading ? _config.WadeSpeed : _config.WalkSpeed;
            _position += direction * (speed * deltaTime);
            TurnTowards(Mathf.Atan2(direction.y, direction.x), deltaTime);
        }

        private void TurnTowards(float target, float deltaTime)
        {
            float shortestTurn = Mathf.Repeat(target - Facing + Mathf.PI, FullTurn) - Mathf.PI;
            Facing += shortestTurn * Mathf.Min(1f, deltaTime * _config.TurnRate);
        }

        private void StayInBounds()
        {
            float margin = _config.TownMargin;
            _position.y = Mathf.Clamp(_position.y, margin, _town.Height - margin);

            float endOfShallows = _bank.EdgeAt(_position.y) + _town.ShallowsWidth;
            _position.x = Mathf.Clamp(_position.x, margin, endOfShallows - margin);
        }
    }
}
