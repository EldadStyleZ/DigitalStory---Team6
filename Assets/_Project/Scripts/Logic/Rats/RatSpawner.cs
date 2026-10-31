using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Picks the night's nests and places its rats around them.</summary>
    public sealed class RatSpawner
    {
        private const double FullTurn = Math.PI * 2d;

        private readonly GameRules _rules;
        private readonly HouseCollider _houses;
        private readonly IRandomSource _random;

        public RatSpawner(GameRules rules, HouseCollider houses, IRandomSource random)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _houses = houses ?? throw new ArgumentNullException(nameof(houses));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public Rat[] Spawn(NightRules night, TownLayout town, Vector2 piperStart)
        {
            Junction[] nests = PickNests(night.NestCount, town, piperStart);
            var rats = new Rat[night.RatCount];

            for (int index = 0; index < rats.Length; index++)
            {
                rats[index] = SpawnRat(nests[index % nests.Length].Position);
            }

            return rats;
        }

        private Junction[] PickNests(int count, TownLayout town, Vector2 piperStart)
        {
            var nests = new Junction[count];
            var found = 0;

            while (found < count)
            {
                int pick = (int)Math.Floor(_random.NextDouble() * town.Junctions.Count);
                Junction candidate = town.Junctions[pick];
                bool isInTown = candidate.Column < town.Columns;
                float distance = Vector2.Distance(candidate.Position, piperStart);

                if (isInTown && distance > _rules.Rats.MinimumNestDistance)
                {
                    nests[found] = candidate;
                    found++;
                }
            }

            return nests;
        }

        private Rat SpawnRat(Vector2 nest)
        {
            double angle = _random.NextDouble() * FullTurn;
            double distance = _random.NextDouble() * _rules.Rats.NestScatter;
            float wanderAngle = (float)(_random.NextDouble() * FullTurn);
            float phase = (float)(_random.NextDouble() * FullTurn);

            var scatter = new Vector2((float)(Math.Cos(angle) * distance), (float)(Math.Sin(angle) * distance));
            var rat = new Rat(nest + scatter, nest, wanderAngle, phase);

            _houses.PushOut(ref rat.Position, ref rat.Velocity, _rules.Rats.Radius);
            KeepInTown(ref rat.Position);
            return rat;
        }

        private void KeepInTown(ref Vector2 position)
        {
            float margin = _rules.Rats.SpawnMargin;
            position.x = Mathf.Clamp(position.x, margin, _rules.Town.Width);
            position.y = Mathf.Clamp(position.y, margin, _rules.Town.Height - margin);
        }
    }
}
