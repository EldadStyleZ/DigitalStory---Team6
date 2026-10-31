using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Places the night's cats on street junctions, away from the Piper.</summary>
    public sealed class CatSpawner
    {
        private const double FullTurn = Math.PI * 2d;

        private readonly GameRules _rules;
        private readonly IRandomSource _random;

        public CatSpawner(GameRules rules, IRandomSource random)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public Cat[] Spawn(int count, TownLayout town, Vector2 piperStart)
        {
            var cats = new Cat[count];

            if (count > 0)
            {
                RequireAStart(town, piperStart);
            }

            for (int index = 0; index < count; index++)
            {
                Junction start = PickStart(town, piperStart);
                cats[index] = new Cat(start, (float)(_random.NextDouble() * FullTurn));
            }

            return cats;
        }

        // Without a place to start, PickStart would draw forever and freeze the Editor.
        private void RequireAStart(TownLayout town, Vector2 piperStart)
        {
            foreach (Junction junction in town.Junctions)
            {
                if (CanStart(junction, town, piperStart))
                {
                    return;
                }
            }

            throw new InvalidOperationException(
                $"No junction is at least {nameof(CatConfig.MinimumStartDistance)} ({_rules.Cats.MinimumStartDistance}) from the Piper.");
        }

        private Junction PickStart(TownLayout town, Vector2 piperStart)
        {
            while (true)
            {
                int pick = (int)Math.Floor(_random.NextDouble() * town.Junctions.Count);
                Junction candidate = town.Junctions[pick];

                if (CanStart(candidate, town, piperStart))
                {
                    return candidate;
                }
            }
        }

        private bool CanStart(Junction junction, TownLayout town, Vector2 piperStart)
        {
            return junction.Column < town.Columns
                && Vector2.Distance(junction.Position, piperStart) >= _rules.Cats.MinimumStartDistance;
        }
    }
}
