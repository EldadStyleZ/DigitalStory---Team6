using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Moves every cat: patrolling the streets, hunting rats, fleeing and sleeping.</summary>
    public sealed class CatPack
    {
        private const int NoPrey = -1;

        // Right, left, down, up. The order matters, because a random pick must match the prototype's.
        private static readonly int[] NextColumn = { 1, -1, 0, 0 };
        private static readonly int[] NextRow = { 0, 0, 1, -1 };

        private readonly Cat[] _cats;
        private readonly CatConfig _config;
        private readonly TownConfig _town;
        private readonly RiverBank _bank;
        private readonly HouseCollider _houses;
        private readonly IRandomSource _random;
        private readonly int _columns;
        private readonly int _rows;

        public CatPack(Cat[] cats, GameRules rules, TownLayout town, RiverBank bank, HouseCollider houses, IRandomSource random)
        {
            _cats = cats ?? throw new ArgumentNullException(nameof(cats));

            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            if (town == null)
            {
                throw new ArgumentNullException(nameof(town));
            }

            _config = rules.Cats;
            _town = rules.Town;
            _bank = bank ?? throw new ArgumentNullException(nameof(bank));
            _houses = houses ?? throw new ArgumentNullException(nameof(houses));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _columns = town.Columns;
            _rows = town.Rows;
        }

        public int Count => _cats.Length;

        public ref readonly Cat this[int index] => ref _cats[index];

        /// <summary>Sends awake cats near the music running from the Piper.</summary>
        public void HearScare(Vector2 piperPosition, float musicRadius)
        {
            float reach = musicRadius + _config.ScareExtraRange;

            for (int index = 0; index < _cats.Length; index++)
            {
                ref Cat cat = ref _cats[index];

                if (!cat.IsAsleep && Vector2.Distance(cat.Position, piperPosition) < reach)
                {
                    cat.FleeLeft = Mathf.Max(cat.FleeLeft, _config.ScareSeconds);
                }
            }
        }

        /// <summary>Puts every cat in range to sleep and stops it fleeing.</summary>
        public void PutToSleepNear(Vector2 centre, float radius, float seconds)
        {
            for (int index = 0; index < _cats.Length; index++)
            {
                ref Cat cat = ref _cats[index];

                if (Vector2.Distance(cat.Position, centre) < radius)
                {
                    cat.SleepLeft = seconds;
                    cat.FleeLeft = 0f;
                }
            }
        }

        /// <summary>Advances every cat by one step.</summary>
        public void Tick(Vector2 piperPosition, RatSwarm swarm, float deltaTime)
        {
            for (int index = 0; index < _cats.Length; index++)
            {
                UpdateCat(ref _cats[index], piperPosition, swarm, deltaTime);
            }
        }

        private void UpdateCat(ref Cat cat, Vector2 piperPosition, RatSwarm swarm, float deltaTime)
        {
            if (cat.IsAsleep)
            {
                cat.SleepLeft -= deltaTime;
                cat.Velocity *= _config.SleepSlowdown;
                return;
            }

            cat.EatLeft = Mathf.Max(0f, cat.EatLeft - deltaTime);
            cat.FleeLeft = Mathf.Max(0f, cat.FleeLeft - deltaTime);

            Vector2 wanted = cat.FleeLeft > 0f ? Flee(ref cat, piperPosition) : HuntOrPatrol(ref cat, swarm);
            Move(ref cat, wanted, deltaTime);
            KeepOutOfTheWater(ref cat);
        }

        private Vector2 Flee(ref Cat cat, Vector2 piperPosition)
        {
            cat.IsHunting = false;
            return Direction(cat.Position, piperPosition) * _config.FleeSpeed;
        }

        private Vector2 HuntOrPatrol(ref Cat cat, RatSwarm swarm)
        {
            int prey = cat.EatLeft > 0f ? NoPrey : NearestPrey(in cat, swarm);
            return prey == NoPrey ? Patrol(ref cat) : Hunt(ref cat, swarm, prey);
        }

        private int NearestPrey(in Cat cat, RatSwarm swarm)
        {
            int nearest = NoPrey;
            float nearestDistance = _config.HuntRange;

            for (int index = 0; index < swarm.Count; index++)
            {
                ref readonly Rat rat = ref swarm[index];

                if (!rat.IsAlive || rat.IsOutOfReach)
                {
                    continue;
                }

                float distance = Mathf.Abs(rat.Position.x - cat.Position.x) + Mathf.Abs(rat.Position.y - cat.Position.y);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = index;
                }
            }

            return nearest;
        }

        private Vector2 Hunt(ref Cat cat, RatSwarm swarm, int prey)
        {
            Vector2 preyPosition = swarm[prey].Position;
            cat.IsHunting = true;

            if (Vector2.Distance(preyPosition, cat.Position) < _config.EatDistance)
            {
                swarm.Eat(prey);
                cat.EatLeft = _config.EatPauseSeconds;
            }

            return Direction(preyPosition, cat.Position) * _config.HuntSpeed;
        }

        private Vector2 Patrol(ref Cat cat)
        {
            if (cat.IsHunting)
            {
                cat.IsHunting = false;
                ReturnToTheStreets(ref cat);
            }

            if (Vector2.Distance(cat.Target, cat.Position) < _config.ArrivalDistance)
            {
                ChooseNextJunction(ref cat);
                return Vector2.zero;
            }

            return Direction(cat.Target, cat.Position) * _config.PatrolSpeed;
        }

        private void ReturnToTheStreets(ref Cat cat)
        {
            cat.Column = Mathf.Clamp(RoundUpFromHalf(cat.Position.x / _town.BlockWidth), 0, _columns - 1);
            cat.Row = Mathf.Clamp(RoundUpFromHalf(cat.Position.y / _town.BlockHeight), 0, _rows);
            cat.Target = JunctionPosition(cat.Column, cat.Row);
        }

        private void ChooseNextJunction(ref Cat cat)
        {
            int choices = 0;

            for (int option = 0; option < NextColumn.Length; option++)
            {
                choices += IsInTown(cat.Column + NextColumn[option], cat.Row + NextRow[option]) ? 1 : 0;
            }

            int pick = (int)Math.Floor(_random.NextDouble() * choices);

            for (int option = 0; option < NextColumn.Length; option++)
            {
                int column = cat.Column + NextColumn[option];
                int row = cat.Row + NextRow[option];

                if (!IsInTown(column, row))
                {
                    continue;
                }

                if (pick == 0)
                {
                    cat.Column = column;
                    cat.Row = row;
                    cat.Target = JunctionPosition(column, row);
                    return;
                }

                pick--;
            }
        }

        private bool IsInTown(int column, int row) => column >= 0 && column < _columns && row >= 0 && row <= _rows;

        private Vector2 JunctionPosition(int column, int row)
        {
            float lowestStreet = _town.Height - _town.BottomStreetInset;
            return new Vector2(column * _town.BlockWidth, Mathf.Min(row * _town.BlockHeight, lowestStreet));
        }

        private void Move(ref Cat cat, Vector2 wanted, float deltaTime)
        {
            cat.Velocity += (wanted - cat.Velocity) * Mathf.Min(1f, deltaTime * _config.SteeringRate);
            cat.Position += cat.Velocity * deltaTime;
            _houses.PushOut(ref cat.Position, ref cat.Velocity, _config.Radius);
        }

        private void KeepOutOfTheWater(ref Cat cat)
        {
            float furthest = _bank.EdgeAt(cat.Position.y) - _config.RiverSetback;

            if (cat.Position.x > furthest)
            {
                cat.Position.x = furthest;
                cat.Velocity.x = -Mathf.Abs(cat.Velocity.x);
            }

            float margin = _config.TownMargin;
            cat.Position.x = Mathf.Clamp(cat.Position.x, margin, _town.Width);
            cat.Position.y = Mathf.Clamp(cat.Position.y, margin, _town.Height - margin);
        }

        private static Vector2 Direction(Vector2 to, Vector2 from)
        {
            Vector2 offset = to - from;
            float distance = offset.magnitude;
            return distance > 0f ? offset / distance : offset;
        }

        // Mathf.RoundToInt rounds halves to the nearest even number, but the prototype's Math.round rounds them up.
        private static int RoundUpFromHalf(float value) => Mathf.FloorToInt(value + 0.5f);
    }
}
