using System;
using System.Linq;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Moves every rat: flocking, wandering, following the flute and drowning.</summary>
    /// <remarks>
    /// Rats update in order and read neighbours that have already moved this step, as in the prototype.
    /// The result depends on array order, so do not run them in parallel without re-tuning.
    /// </remarks>
    public sealed class RatSwarm
    {
        // Stops two rats on the same spot dividing by zero.
        private const float SmallestDistance = 0.01f;

        private readonly Rat[] _rats;
        private readonly RatConfig _config;
        private readonly FluteConfig _flute;
        private readonly CatConfig _cats;
        private readonly TownConfig _town;
        private readonly RiverBank _bank;
        private readonly HouseCollider _houses;
        private readonly IRandomSource _random;
        private readonly RatGrid _grid;
        private readonly float _neighbourRadiusSquared;

        private float _time;

        public RatSwarm(Rat[] rats, GameRules rules, RiverBank bank, HouseCollider houses, IRandomSource random)
        {
            _rats = rats ?? throw new ArgumentNullException(nameof(rats));

            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            _config = rules.Rats;
            _flute = rules.Flute;
            _cats = rules.Cats;
            _town = rules.Town;
            _bank = bank ?? throw new ArgumentNullException(nameof(bank));
            _houses = houses ?? throw new ArgumentNullException(nameof(houses));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _grid = new RatGrid(_town.Width, _town.Height, _config.NeighbourRadius, rats.Length);
            _neighbourRadiusSquared = _config.NeighbourRadius * _config.NeighbourRadius;

            foreach (Rat rat in rats)
            {
                AliveCount += rat.IsAlive ? 1 : 0;
            }
        }

        public int Count => _rats.Length;
        public int AliveCount { get; private set; }
        public int DrownedCount { get; private set; }

        /// <summary>How many rats cats have eaten.</summary>
        public int LostCount { get; private set; }

        public ref readonly Rat this[int index] => ref _rats[index];

        public bool IsCharmed(int index) => _rats[index].Charm > _config.CharmedAt;

        /// <summary>Fully charms every living rat in range.</summary>
        public void CharmEveryRatNear(Vector2 centre, float radius)
        {
            for (int index = 0; index < _rats.Length; index++)
            {
                ref Rat rat = ref _rats[index];

                if (rat.IsAlive && Vector2.Distance(rat.Position, centre) < radius)
                {
                    rat.Charm = 1f;
                }
            }
        }

        /// <summary>Finds the living rats in range that songs can reach, nearest first.</summary>
        /// <remarks>Allocates, so call it only when a song is cast.</remarks>
        public int[] GatherNear(Vector2 centre, float radius)
        {
            return Enumerable.Range(0, _rats.Length)
                .Where(index => CanBeGathered(in _rats[index], centre, radius))
                .OrderBy(index => Vector2.Distance(_rats[index].Position, centre))
                .ToArray();
        }

        /// <summary>Fully charms one rat.</summary>
        public void FullyCharm(int index) => _rats[index].Charm = 1f;

        /// <summary>Puts a rat in a trance, replacing any it was already in.</summary>
        /// <param name="index">The rat.</param>
        /// <param name="trance">What holds it.</param>
        /// <param name="seconds">How long it lasts.</param>
        /// <param name="slot">Its place in a formation.</param>
        /// <param name="drift">Its own random drift while floating.</param>
        public void Enchant(int index, RatTrance trance, float seconds, int slot = 0, Vector2 drift = default)
        {
            ref Rat rat = ref _rats[index];
            rat.Trance = trance;
            rat.TranceLeft = seconds;
            rat.TranceSlot = slot;
            rat.TranceDrift = drift;
        }

        /// <summary>Removes a rat that a cat caught.</summary>
        public void Eat(int index)
        {
            _rats[index].IsAlive = false;
            AliveCount--;
            LostCount++;
        }

        /// <summary>Advances every living rat by one step.</summary>
        public void Tick(in SwarmInfluence influence, float deltaTime)
        {
            _time += deltaTime;
            SortIntoGrid();

            for (int index = 0; index < _rats.Length; index++)
            {
                if (_rats[index].IsAlive)
                {
                    UpdateRat(index, in influence, deltaTime);
                }
            }
        }

        private static bool CanBeGathered(in Rat rat, Vector2 centre, float radius)
        {
            return rat.IsAlive && !rat.IsOutOfReach && Vector2.Distance(rat.Position, centre) < radius;
        }

        private void SortIntoGrid()
        {
            _grid.Clear();

            for (int index = 0; index < _rats.Length; index++)
            {
                if (_rats[index].IsAlive)
                {
                    _grid.Add(index, _rats[index].Position);
                }
            }
        }

        private void UpdateRat(int index, in SwarmInfluence influence, float deltaTime)
        {
            ref Rat rat = ref _rats[index];

            if (rat.Trance != null && rat.Trance.MovesTheRat)
            {
                UpdateRatInTrance(ref rat, deltaTime);
                return;
            }

            NeighbourSurvey neighbours = SurveyNeighbours(index);

            Vector2 force = FlockingForce(in rat, in neighbours) + WanderForce(ref rat, deltaTime);
            force += UpdateCharm(ref rat, in influence, neighbours.HighestCharm, deltaTime);
            force += rat.Charm > _config.MinimumFollowCharm ? FollowForce(in rat, in influence) : HomeForce(in rat);
            CountDownFormation(ref rat, deltaTime);

            float bankEdge = _bank.EdgeAt(rat.Position.y);
            force += BankFearForce(in rat, bankEdge);
            force += CatFearForce(ref rat, influence.Cats, deltaTime);

            Move(ref rat, force, deltaTime);
            BounceOffTownEdges(ref rat);

            if (rat.Position.x > bankEdge + _config.DrownMargin)
            {
                Drown(ref rat);
            }
        }

        private void UpdateRatInTrance(ref Rat rat, float deltaTime)
        {
            var step = new TranceStep(deltaTime, _time, _town, _houses, _config.Radius);
            rat.TranceLeft -= deltaTime;
            rat.Trance.Move(ref rat, in step);

            if (rat.TranceLeft > 0f)
            {
                return;
            }

            rat.Trance.Finish(ref rat, in step);
            rat.Trance = null;

            if (rat.Position.x > _bank.EdgeAt(rat.Position.y) + _config.DrownMargin)
            {
                Drown(ref rat);
            }
        }

        private NeighbourSurvey SurveyNeighbours(int index)
        {
            Vector2 position = _rats[index].Position;
            int column = _grid.ColumnOf(position.x);
            int row = _grid.RowOf(position.y);
            int lastColumn = Mathf.Min(column + 1, _grid.Columns - 1);
            int lastRow = Mathf.Min(row + 1, _grid.Rows - 1);
            var survey = default(NeighbourSurvey);

            for (int y = Mathf.Max(row - 1, 0); y <= lastRow; y++)
            {
                for (int x = Mathf.Max(column - 1, 0); x <= lastColumn; x++)
                {
                    SurveyCell(x, y, index, position, ref survey);
                }
            }

            return survey;
        }

        private void SurveyCell(int column, int row, int index, Vector2 position, ref NeighbourSurvey survey)
        {
            for (int other = _grid.FirstIn(column, row); other != RatGrid.End; other = _grid.NextAfter(other))
            {
                if (other != index)
                {
                    AddNeighbour(position, in _rats[other], ref survey);
                }
            }
        }

        private void AddNeighbour(Vector2 position, in Rat other, ref NeighbourSurvey survey)
        {
            Vector2 away = position - other.Position;
            float distanceSquared = away.sqrMagnitude;

            if (distanceSquared > _neighbourRadiusSquared)
            {
                return;
            }

            float distance = Mathf.Max(Mathf.Sqrt(distanceSquared), SmallestDistance);

            if (distance < _config.SeparationDistance)
            {
                survey.Separation += away / distance * (_config.SeparationDistance - distance);
            }

            survey.VelocitySum += other.Velocity;
            survey.PositionSum += other.Position;
            survey.Count++;
            survey.HighestCharm = Mathf.Max(survey.HighestCharm, other.Charm);
        }

        private Vector2 FlockingForce(in Rat rat, in NeighbourSurvey neighbours)
        {
            Vector2 force = neighbours.Separation * _config.SeparationStrength;

            if (neighbours.Count == 0)
            {
                return force;
            }

            float share = rat.Charm > _config.LooseFlockCharm ? _config.LooseFlockShare : 1f;
            Vector2 meanVelocity = neighbours.VelocitySum / neighbours.Count;
            Vector2 meanPosition = neighbours.PositionSum / neighbours.Count;

            force += (meanVelocity - rat.Velocity) * (_config.AlignmentStrength * share);
            force += (meanPosition - rat.Position) * (_config.CohesionStrength * share);
            return force;
        }

        private Vector2 WanderForce(ref Rat rat, float deltaTime)
        {
            rat.WanderAngle += ((float)_random.NextDouble() - 0.5f) * _config.WanderTurnRate * deltaTime;
            return new Vector2(Mathf.Cos(rat.WanderAngle), Mathf.Sin(rat.WanderAngle)) * _config.WanderStrength;
        }

        // Returns the scare's push. Every other charm change adds no force.
        private Vector2 UpdateCharm(ref Rat rat, in SwarmInfluence influence, float highestNearbyCharm, float deltaTime)
        {
            Vector2 toPiper = influence.PiperPosition - rat.Position;
            float distance = toPiper.magnitude;

            if (rat.Trance != null || distance < influence.CharmZoneRadius)
            {
                rat.Charm = 1f;
                return Vector2.zero;
            }

            bool isInMusic = distance < influence.MusicRadius;

            if (influence.Mode == FluteMode.Scare && isInMusic)
            {
                rat.Charm = Mathf.Max(0f, rat.Charm - (_flute.ScareCharmLossPerSecond * deltaTime));
                return ScarePush(toPiper, distance, influence.MusicRadius);
            }

            rat.Charm = NextCharm(rat.Charm, in influence, isInMusic, distance, highestNearbyCharm, deltaTime);
            return Vector2.zero;
        }

        private Vector2 ScarePush(Vector2 toPiper, float distance, float musicRadius)
        {
            float safeDistance = distance > 0f ? distance : 1f;
            float strength = _flute.ScareStrength * (1f - (distance / musicRadius));
            return -toPiper / safeDistance * strength;
        }

        private float NextCharm(
            float charm,
            in SwarmInfluence influence,
            bool isInMusic,
            float distance,
            float highestNearbyCharm,
            float deltaTime)
        {
            if (influence.IsPlaying && isInMusic)
            {
                float closeness = 1f - (distance / influence.MusicRadius);
                float risePerSecond = (_config.CharmRiseNearPerSecond * closeness) + _config.CharmRiseBasePerSecond;
                return Mathf.Min(1f, charm + (deltaTime * risePerSecond));
            }

            bool canCatchCharm = influence.IsPlaying
                && influence.Mode != FluteMode.Scare
                && highestNearbyCharm > _config.CharmSpreadThreshold;

            return canCatchCharm
                ? Mathf.Min(1f, charm + (deltaTime * _config.CharmSpreadPerSecond))
                : Mathf.Max(0f, charm - (deltaTime * _config.CharmFadePerSecond));
        }

        private Vector2 FollowForce(in Rat rat, in SwarmInfluence influence)
        {
            Vector2 target = influence.PiperPosition;
            float stopDistance = _config.FollowStopDistance;

            if (rat.Trance != null)
            {
                target = rat.Trance.Target(in rat, influence.PiperPosition);
                stopDistance = rat.Trance.StopDistance;
            }
            else if (influence.Mode == FluteMode.March)
            {
                var heading = new Vector2(Mathf.Cos(influence.PiperFacing), Mathf.Sin(influence.PiperFacing));
                target += heading * _flute.MarchDistance;
                stopDistance = _flute.MarchStopDistance;
            }

            Vector2 toTarget = target - rat.Position;
            float distance = toTarget.magnitude;
            float safeDistance = distance > 0f ? distance : 1f;
            float slowdown = safeDistance > stopDistance ? 1f : safeDistance / stopDistance;
            float speed = TopSpeed(rat.Charm) * slowdown;
            Vector2 desiredVelocity = toTarget / safeDistance * speed;

            return (desiredVelocity - rat.Velocity) * (_config.SteeringStrength * rat.Charm);
        }

        private static void CountDownFormation(ref Rat rat, float deltaTime)
        {
            if (rat.Trance == null)
            {
                return;
            }

            rat.TranceLeft -= deltaTime;

            if (rat.TranceLeft <= 0f)
            {
                rat.Trance = null;
            }
        }

        private Vector2 HomeForce(in Rat rat)
        {
            Vector2 toHome = rat.Home - rat.Position;
            float distance = toHome.magnitude;

            return distance > _config.HomeRange ? toHome / distance * _config.HomeStrength : Vector2.zero;
        }

        private Vector2 BankFearForce(in Rat rat, float bankEdge)
        {
            float fearLine = bankEdge - _config.BankFearDistance;

            if (rat.Charm >= _config.BankFearCharm || rat.Position.x <= fearLine)
            {
                return Vector2.zero;
            }

            return Vector2.left * ((rat.Position.x - fearLine) * _config.BankFearStrength);
        }

        // Returns the push away from awake cats, which also drain the rat's charm.
        private Vector2 CatFearForce(ref Rat rat, CatPack cats, float deltaTime)
        {
            Vector2 force = Vector2.zero;

            for (int index = 0; index < cats.Count; index++)
            {
                ref readonly Cat cat = ref cats[index];
                Vector2 away = rat.Position - cat.Position;
                float distance = away.magnitude;

                if (cat.IsAsleep || distance >= _cats.FearRange)
                {
                    continue;
                }

                float safeDistance = distance > 0f ? distance : 1f;
                force += away / safeDistance * (_cats.FearStrength * (1f - (distance / _cats.FearRange)));
                rat.Charm = Mathf.Max(0f, rat.Charm - (deltaTime * _cats.CharmLossPerSecond));
            }

            return force;
        }

        private void Move(ref Rat rat, Vector2 force, float deltaTime)
        {
            rat.Velocity = Vector2.ClampMagnitude(rat.Velocity + (force * deltaTime), TopSpeed(rat.Charm));
            rat.Position += rat.Velocity * deltaTime;
            _houses.PushOut(ref rat.Position, ref rat.Velocity, _config.Radius);
        }

        private float TopSpeed(float charm) => _config.BaseSpeed + (_config.CharmSpeedBonus * charm);

        private void BounceOffTownEdges(ref Rat rat)
        {
            float radius = _config.Radius;
            float bottom = _town.Height - radius;

            if (rat.Position.x < radius)
            {
                rat.Position.x = radius;
                rat.Velocity.x = Mathf.Abs(rat.Velocity.x);
            }

            if (rat.Position.y < radius)
            {
                rat.Position.y = radius;
                rat.Velocity.y = Mathf.Abs(rat.Velocity.y);
            }

            if (rat.Position.y > bottom)
            {
                rat.Position.y = bottom;
                rat.Velocity.y = -Mathf.Abs(rat.Velocity.y);
            }
        }

        private void Drown(ref Rat rat)
        {
            rat.IsAlive = false;
            AliveCount--;
            DrownedCount++;
        }

        private struct NeighbourSurvey
        {
            public Vector2 Separation;
            public Vector2 VelocitySum;
            public Vector2 PositionSum;
            public int Count;
            public float HighestCharm;
        }
    }
}
