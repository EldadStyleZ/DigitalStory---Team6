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
            Assert.That(result.Song, Is.SameAs(_mouse));
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
            Assert.That(result.Song, Is.SameAs(_cat));
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
        public void Constructor_ASongClashingWithACreature_FailsWithAClearError()
        {
            IComboSong[] clashing = { Creature("Short", 1, H, J), Song(default, "Long", 2, H, J, K) };

            var error = Assert.Throws<ArgumentException>(() => new ComboReader(_config, clashing));
            Assert.That(error.Message, Does.Contain("Short").And.Contain("Long"));
        }

        [Test]
        public void Candidates_AfterTheFirstNote_ListEveryKnownComboThatFits()
        {
            CreatureType longer = Creature("Longer", 1, H, K, L);
            var reader = new ComboReader(_config, new[] { _mouse, _cat, longer });

            reader.Press(H, 1);

            Assert.That(reader.Candidates, Is.EqualTo(new[] { _mouse, longer }));
        }

        [Test]
        public void Candidates_LeaveOutCombosStillLocked()
        {
            _reader.Press(K, 1);

            Assert.That(_reader.Candidates, Is.Empty);
        }

        [Test]
        public void Candidates_AfterAnotherNote_Narrow()
        {
            CreatureType longer = Creature("Longer", 1, H, K, L);
            var reader = new ComboReader(_config, new[] { _mouse, _cat, longer });
            reader.Press(H, 1);

            reader.Press(K, 1);

            Assert.That(reader.Candidates, Is.EqualTo(new[] { longer }));
        }

        [Test]
        public void Candidates_OnceTheComboFinishes_AreEmpty()
        {
            _reader.Press(H, 1);

            _reader.Press(J, 1);

            Assert.That(_reader.Candidates, Is.Empty);
        }

        [Test]
        public void Candidates_OnceTheComboFails_AreEmpty()
        {
            _reader.Press(H, 1);

            _reader.Press(L, 1);

            Assert.That(_reader.Candidates, Is.Empty);
        }

        [Test]
        public void Candidates_AfterClear_AreEmpty()
        {
            _reader.Press(H, 1);

            _reader.Clear();

            Assert.That(_reader.Candidates, Is.Empty);
        }

        [Test]
        public void Learn_ANewCombo_LetsItBePlayed()
        {
            var reader = new ComboReader(_config, new[] { _mouse });
            CreatureType learned = Creature("Learned", 1, K, L);

            reader.Learn(learned);
            reader.Press(K, 1);

            Assert.That(reader.Press(L, 1).Song, Is.SameAs(learned));
        }

        [Test]
        public void Learn_AComboStartingAKnownOne_FailsWithAClearError()
        {
            var error = Assert.Throws<ArgumentException>(() => _reader.Learn(Creature("Stub", 1, K, L)));

            Assert.That(error.Message, Does.Contain("Stub").And.Contain("Cat"));
        }

        [Test]
        public void Learn_AComboAKnownOneStarts_FailsWithAClearError()
        {
            Assert.Throws<ArgumentException>(() => _reader.Learn(Creature("Long", 1, H, J, K)));
        }

        [Test]
        public void Learn_TheSameCombo_FailsWithAClearError()
        {
            Assert.Throws<ArgumentException>(() => _reader.Learn(Creature("Twin", 1, H, J)));
        }

        [Test]
        public void Learn_ANoteThatIsNotOnTheFlute_FailsWithAClearError()
        {
            Assert.Throws<ArgumentException>(() => _reader.Learn(Creature("Odd", 1, K, 4)));
        }

        [Test]
        public void TheRealRoster_EveryPathOfChoices_LeavesEveryComboPlayable()
        {
            var roster = ScriptableObject.CreateInstance<CreatureRoster>();

            for (int path = 0; path < 1 << roster.Choices.Count; path++)
            {
                var reader = new ComboReader(_config, new IComboSong[] { roster.Starter.Creature, roster.Starter.Song });

                for (int index = 0; index < roster.Choices.Count; index++)
                {
                    CreatureOffer offer = roster.Choices[index].Options[(path >> index) & 1];
                    Assert.DoesNotThrow(() => reader.Learn(offer.Creature), offer.Creature.Name);
                    Assert.DoesNotThrow(() => reader.Learn(offer.Song), offer.Song.Name);
                }
            }
        }

        [Test]
        public void TheRealRoster_EachPair_SharesItsCombos()
        {
            foreach (CreatureChoice choice in ScriptableObject.CreateInstance<CreatureRoster>().Choices)
            {
                CreatureOffer first = choice.Options[0];
                CreatureOffer second = choice.Options[1];

                Assert.That(second.Creature.Combo, Is.EqualTo(first.Creature.Combo), choice.Problem);
                Assert.That(second.Song.Combo, Is.EqualTo(first.Song.Combo), choice.Problem);
            }
        }

        [Test]
        public void TheRealRoster_EachPair_CostsTheSame()
        {
            foreach (CreatureChoice choice in ScriptableObject.CreateInstance<CreatureRoster>().Choices)
            {
                Assert.That(choice.Options[1].Creature.StaminaCost, Is.EqualTo(choice.Options[0].Creature.StaminaCost), choice.Problem);
                Assert.That(choice.Options[1].Song.StaminaCost, Is.EqualTo(choice.Options[0].Song.StaminaCost), choice.Problem);
            }
        }

        [Test]
        public void TheRealRoster_EachPair_OffersTwoOptions_AtItsOwnLevel()
        {
            var roster = ScriptableObject.CreateInstance<CreatureRoster>();
            var levels = ScriptableObject.CreateInstance<LevelConfig>();

            Assert.That(roster.Choices.Select(choice => choice.Level), Is.EqualTo(Enumerable.Range(2, levels.XpToReach.Count - 1)));

            foreach (CreatureChoice choice in roster.Choices)
            {
                Assert.That(choice.Options.Count, Is.EqualTo(2), choice.Problem);
                Assert.That(choice.Problem, Is.Not.Empty);
                Assert.That(choice.Options.All(offer => offer.Creature.UnlockLevel == choice.Level && offer.Song.UnlockLevel == choice.Level), Is.True, choice.Problem);
            }
        }

        [Test]
        public void TheRealRoster_ThirdChoice_ReachesTheBackLineEitherWay()
        {
            CreatureChoice third = ScriptableObject.CreateInstance<CreatureRoster>().Choices.Single(choice => choice.Level == 3);

            Assert.That(third.Options.All(offer => offer.Creature.Stats.Traits.HuntsBackline), Is.True);
        }

        [Test]
        public void TheRealRoster_NoTwoCreaturesOrSongsAreTheSame()
        {
            var roster = ScriptableObject.CreateInstance<CreatureRoster>();
            string[] names = AllSongs(roster).Select(song => song.Name).ToArray();

            Assert.That(names, Is.Unique);
        }

        [Test]
        public void TheRealRoster_EveryComboSaysWhatItIsFor()
        {
            var roster = ScriptableObject.CreateInstance<CreatureRoster>();

            foreach (IComboSong song in AllSongs(roster))
            {
                Assert.That(song.Role, Is.Not.Empty, song.Name);
            }
        }

        [Test]
        public void TheRealRoster_LevelOneHasOneCombo()
        {
            var roster = ScriptableObject.CreateInstance<CreatureRoster>();

            Assert.That(AllSongs(roster).Count(song => song.UnlockLevel == 1), Is.EqualTo(1));
        }

        [Test]
        public void TheRealRoster_HigherChoices_CostMoreTakeLongerAndCoolLonger()
        {
            var roster = ScriptableObject.CreateInstance<CreatureRoster>();
            CreatureType lower = roster.Starter.Creature;

            foreach (CreatureChoice choice in roster.Choices)
            {
                foreach (CreatureOffer offer in choice.Options)
                {
                    CreatureType higher = offer.Creature;
                    Assert.That(higher.StaminaCost, Is.GreaterThan(lower.StaminaCost), higher.Name);
                    Assert.That(higher.CooldownSeconds, Is.GreaterThan(lower.CooldownSeconds), higher.Name);
                    Assert.That(higher.Combo.Count, Is.GreaterThan(lower.Combo.Count), higher.Name);
                }

                lower = choice.Options[0].Creature;
            }
        }
    }
}
