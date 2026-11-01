using System;
using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Springs the maze's hidden groups. One runs at a time: the Piper comes close, a moment passes, a
    /// warning shows where they will appear, then they arrive behind the Piper. A surprise with no warning
    /// feels unfair, so the warning always comes first, and a rest follows so the tension can drop.
    /// </summary>
    public sealed class AmbushWatch
    {
        private readonly Maze _maze;
        private readonly AmbushConfig _config;
        private readonly Battle _battle;
        private readonly List<Ambush> _ambushes;
        private readonly StepField _fromHome;

        private Ambush _active;
        private int _arrivalCell;
        private float _secondsLeft;
        private float _secondsOfRest;

        public AmbushWatch(Maze maze, AmbushConfig config, Battle battle, IReadOnlyList<Ambush> ambushes)
        {
            _maze = maze ?? throw new ArgumentNullException(nameof(maze));
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _battle = battle ?? throw new ArgumentNullException(nameof(battle));
            _ambushes = new List<Ambush>(ambushes ?? throw new ArgumentNullException(nameof(ambushes)));
            _fromHome = new StepField(maze);
        }

        /// <summary>Raised with the place the enemies will appear.</summary>
        public event Action<Vector2> Warned;

        /// <summary>Raised with the place the enemies appeared.</summary>
        public event Action<Vector2> Sprang;

        /// <summary>Hides another group in the maze.</summary>
        public void Add(Ambush ambush) => _ambushes.Add(ambush);

        public void Tick(float deltaTime)
        {
            _secondsOfRest -= deltaTime;

            if (_active != null)
            {
                Advance(deltaTime);
            }
            else if (_secondsOfRest <= 0f)
            {
                LookForOne();
            }
        }

        /// <summary>Puts back any group that has not appeared yet, so it can come again.</summary>
        public void Calm()
        {
            _active?.Calm();
            _active = null;
        }

        private void LookForOne()
        {
            int reach = Mathf.FloorToInt(_config.TriggerTiles / _maze.Shape.CellPitch);

            foreach (Ambush ambush in _ambushes)
            {
                if (ambush.State == AmbushState.Dormant && _battle.StepsToPiper(ambush.Cell) <= reach)
                {
                    ambush.Stir();
                    _active = ambush;
                    _secondsLeft = _config.DelaySeconds;
                    return;
                }
            }
        }

        private void Advance(float deltaTime)
        {
            _secondsLeft -= deltaTime;

            if (_secondsLeft > 0f)
            {
                return;
            }

            if (_active.State == AmbushState.Waiting)
            {
                Warn();
            }
            else
            {
                Spring();
            }
        }

        private void Warn()
        {
            _arrivalCell = CellBehindThePiper(_active.Cell);
            _active.Warn();
            _secondsLeft = _config.WarningSeconds;
            Warned?.Invoke(_maze.CenterOf(_arrivalCell));
        }

        private void Spring()
        {
            IReadOnlyList<MinionType> members = _active.Members;

            for (int index = 0; index < members.Count; index++)
            {
                _battle.AddMinion(members[index], _maze.SpotAround(_arrivalCell, index, members.Count)).Wake();
            }

            _active.Spring();
            _active = null;
            _secondsOfRest = _config.RestSeconds;
            Sprang?.Invoke(_maze.CenterOf(_arrivalCell));
        }

        // As far from the Piper as allowed, then as far from where they hid: the side the Piper came from.
        private int CellBehindThePiper(int home)
        {
            _fromHome.Fill(home);
            int best = home;
            int bestFromPiper = 0;
            int bestFromHome = -1;

            for (int cell = 0; cell < _maze.CellCount; cell++)
            {
                int fromPiper = _battle.StepsToPiper(cell);
                int fromHome = _fromHome.StepsAt(cell);
                bool isAllowed = fromPiper >= 1 && fromPiper <= _config.EmergeSteps;

                if (isAllowed && (fromPiper > bestFromPiper || (fromPiper == bestFromPiper && fromHome > bestFromHome)))
                {
                    best = cell;
                    bestFromPiper = fromPiper;
                    bestFromHome = fromHome;
                }
            }

            return best;
        }
    }
}
