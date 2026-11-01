using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class LevelTrackTests
    {
        private LevelConfig _config;
        private LevelTrack _track;
        private List<int> _reached;

        [SetUp]
        public void CreateTrack()
        {
            _config = ScriptableObject.CreateInstance<LevelConfig>();
            _track = new LevelTrack(_config);
            _reached = new List<int>();
            _track.LevelledUp += _reached.Add;
        }

        [Test]
        public void Track_AtTheStart_IsLevelOneWithNoExperience()
        {
            Assert.That(_track.Level, Is.EqualTo(1));
            Assert.That(_track.Xp, Is.EqualTo(0));
        }

        [Test]
        public void Add_BelowTheNextLevel_StaysThere()
        {
            _track.Add(_config.XpToReach[1] - 1);

            Assert.That(_track.Level, Is.EqualTo(1));
            Assert.That(_reached, Is.Empty);
        }

        [Test]
        public void Add_ReachingTheNextLevel_LevelsUp()
        {
            _track.Add(_config.XpToReach[1]);

            Assert.That(_track.Level, Is.EqualTo(2));
            Assert.That(_reached, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void Add_EnoughForTwoLevels_ReportsBoth()
        {
            _track.Add(_config.XpToReach[2]);

            Assert.That(_track.Level, Is.EqualTo(3));
            Assert.That(_reached, Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void Add_PastTheLastLevel_StopsThere()
        {
            _track.Add(int.MaxValue / 2);

            Assert.That(_track.Level, Is.EqualTo(_config.XpToReach.Count));
            Assert.That(_track.IsAtMaxLevel, Is.True);
        }

        [Test]
        public void Progress_HalfwayToTheNextLevel_IsOneHalf()
        {
            _track.Add(_config.XpToReach[1] / 2);

            Assert.That(_track.Progress, Is.EqualTo(0.5f).Within(0.05f));
        }

        [Test]
        public void Progress_AtTheLastLevel_IsFull()
        {
            _track.Add(int.MaxValue / 2);

            Assert.That(_track.Progress, Is.EqualTo(1f));
        }
    }
}
