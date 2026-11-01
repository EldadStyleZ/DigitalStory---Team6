using System;
using System.Linq;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;
using static DigitalStory.Tests.EditMode.LabTestData;

namespace DigitalStory.Tests.EditMode
{
    public sealed class ComboReaderTests
    {
        private ComboConfig _config;
        private CreatureType _mouse;
        private CreatureType _cat;
        private ComboReader _reader;

        [SetUp]
        public void CreateReader()
        {
            _config = ScriptableObject.CreateInstance<ComboConfig>();
            _mouse = Creature("Mouse", 1, H, J);
            _cat = Creature("Cat", 2, K, L, H);
            _reader = new ComboReader(_config, new[] { _mouse, _cat });
        }

        [Test]
        public void Press_TheFirstNote_KeepsBuilding()
        {
            ComboResult result = _reader.Press(H, 1);

            Assert.That(result.Outcome, Is.EqualTo(ComboOutcome.Building));
            Assert.That(_reader.Notes, Is.EqualTo(new[] { H }));
        }

        [Test]
        public void Press_TheLastNote_CompletesTheCombo()
        {
            _reader.Press(H, 1);

            ComboResult result = _reader.Press(J, 1);

            Assert.That(result.Outcome, Is.EqualTo(ComboOutcome.Completed));
            Assert.That(result.Creature, Is.SameAs(_mouse));
            Assert.That(_reader.Notes, Is.Empty);
        }

        [Test]
        public void Press_AWrongNote_FailsAndStartsOver()
        {
            _reader.Press(H, 1);

            ComboResult result = _reader.Press(L, 1);

            Assert.That(result.Outcome, Is.EqualTo(ComboOutcome.Failed));
            Assert.That(_reader.Notes, Is.Empty);
        }

        [Test]
        public void Press_ALockedCombo_Fails()
        {
            Assert.That(_reader.Press(K, 1).Outcome, Is.EqualTo(ComboOutcome.Failed));
        }

        [Test]
        public void Press_AComboUnlockedAtThisLevel_Works()
        {
            _reader.Press(K, 2);
            _reader.Press(L, 2);

            ComboResult result = _reader.Press(H, 2);

            Assert.That(result.Outcome, Is.EqualTo(ComboOutcome.Completed));
            Assert.That(result.Creature, Is.SameAs(_cat));
        }

        [Test]
        public void Tick_PausingTooLongMidCombo_Fails()
        {
            _reader.Press(H, 1);

            ComboResult result = _reader.Tick(_config.SecondsToNextNote + 0.01f);

            Assert.That(result.Outcome, Is.EqualTo(ComboOutcome.Failed));
            Assert.That(_reader.Notes, Is.Empty);
        }

        [Test]
        public void Tick_ShortPauseMidCombo_KeepsGoing()
        {
            _reader.Press(H, 1);

            ComboResult result = _reader.Tick(_config.SecondsToNextNote * 0.5f);

            Assert.That(result.Outcome, Is.EqualTo(ComboOutcome.None));
            Assert.That(_reader.Notes, Is.Not.Empty);
        }

        [Test]
        public void Press_ARightNote_RestartsThePause()
        {
            _reader.Press(K, 2);
            _reader.Tick(_config.SecondsToNextNote * 0.8f);
            _reader.Press(L, 2);

            ComboResult result = _reader.Tick(_config.SecondsToNextNote * 0.8f);

            Assert.That(result.Outcome, Is.EqualTo(ComboOutcome.None));
        }

        [Test]
        public void Tick_WithNoCombo_NeverFails()
        {
            Assert.That(_reader.Tick(100f).Outcome, Is.EqualTo(ComboOutcome.None));
        }

        [Test]
        public void Clear_MidCombo_DropsItWithoutFailing()
        {
            _reader.Press(H, 1);

            _reader.Clear();

            Assert.That(_reader.Notes, Is.Empty);
            Assert.That(_reader.Tick(100f).Outcome, Is.EqualTo(ComboOutcome.None));
        }

        [Test]
        public void Constructor_OneComboStartsAnother_FailsWithAClearError()
        {
            CreatureType[] clashing = { Creature("Short", 1, H, J), Creature("Long", 2, H, J, K) };

            var error = Assert.Throws<ArgumentException>(() => new ComboReader(_config, clashing));
            Assert.That(error.Message, Does.Contain("Short").And.Contain("Long"));
        }

        [Test]
        public void Constructor_ANoteThatIsNotOnTheFlute_FailsWithAClearError()
        {
            Assert.Throws<ArgumentException>(() => new ComboReader(_config, new[] { Creature("Odd", 1, H, 4) }));
        }

        [Test]
        public void TheRealRoster_NoComboStartsAnother()
        {
            var roster = ScriptableObject.CreateInstance<CreatureRoster>();

            Assert.DoesNotThrow(() => new ComboReader(_config, roster.Creatures));
        }

        [Test]
        public void TheRealRoster_LevelOneHasOneCombo()
        {
            var roster = ScriptableObject.CreateInstance<CreatureRoster>();

            Assert.That(roster.Creatures.Count(creature => creature.UnlockLevel == 1), Is.EqualTo(1));
        }

        [Test]
        public void TheRealRoster_HigherLevels_CostMoreTakeLongerAndCoolLonger()
        {
            CreatureType[] byLevel = ScriptableObject.CreateInstance<CreatureRoster>().Creatures.OrderBy(creature => creature.UnlockLevel).ToArray();

            for (int index = 1; index < byLevel.Length; index++)
            {
                CreatureType lower = byLevel[index - 1];
                CreatureType higher = byLevel[index];
                Assert.That(higher.StaminaCost, Is.GreaterThan(lower.StaminaCost), higher.Name);
                Assert.That(higher.CooldownSeconds, Is.GreaterThan(lower.CooldownSeconds), higher.Name);
                Assert.That(higher.Combo.Count, Is.GreaterThan(lower.Combo.Count), higher.Name);
            }
        }
    }
}
