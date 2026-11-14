using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Plan sections H and U, on the default NestConfig: 8 nests, 2 open at the start, then 2, 2, 1 and 1 more as waves 1 to 4
    /// are cleared, in the order given. Standing within 2.5 m of an open nest shows its combo, and playing the combo's notes in
    /// order collects it. Nest i stands 10 m along x per index, so only one is ever in reach unless a test says otherwise, and
    /// every nest's combo is H J K L (holes 0 1 2 3) unless a test says otherwise.
    /// </summary>
    public sealed class NestFieldTests
    {
        private static readonly Vector3 Far = new Vector3(-500f, 0f, -500f);

        private NestConfig _config;
        private NestField _nests;
        private static readonly int[] Combo = { 0, 1, 2, 3 };

        private List<int> _opened;
        private List<int> _collected;
        private List<int> _shown;
        private List<int> _hidden;
        private List<NestComboStep> _steps;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<NestConfig>();
            Build(Spaced(8));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void New_MissingConfigOrPositions_Throws()
        {
            Assert.That(() => new NestField(null, Spaced(8), Combos(8)), Throws.ArgumentNullException);
            Assert.That(() => new NestField(_config, null, Combos(8)), Throws.ArgumentNullException);
            Assert.That(() => new NestField(_config, Spaced(8), null), Throws.ArgumentNullException);
        }

        [TestCase(7)]
        [TestCase(9)]
        public void New_ADifferentNumberOfPositionsThanTheConfigOpens_Throws(int count)
        {
            Assert.That(() => new NestField(_config, Spaced(count), Combos(count)), Throws.ArgumentException);
        }

        [Test]
        public void New_ADifferentNumberOfCombosThanNests_Throws()
        {
            Assert.That(() => new NestField(_config, Spaced(8), Combos(7)), Throws.ArgumentException);
        }

        // ---- Opening ----

        [Test]
        public void New_TheFirstTwoAreOpenAndNoneIsCollected()
        {
            Assert.That(_nests.Count, Is.EqualTo(8));
            Assert.That(_nests.OpenCount, Is.EqualTo(2));
            Assert.That(_nests.CollectedCount, Is.EqualTo(0));
            Assert.That(_nests[0].IsOpen, Is.True);
            Assert.That(_nests[1].IsOpen, Is.True);
            for (int index = 2; index < _nests.Count; index++)
            {
                Assert.That(_nests[index].IsOpen, Is.False, "nest " + index);
            }

            Assert.That(_nests[3].Position, Is.EqualTo(new Vector3(30f, 0f, 0f)));
        }

        [Test]
        public void OpenAfterWave_EachWaveCleared_OpensTwoTwoOneAndOneMoreInOrder()
        {
            int[] openAfter = { 4, 6, 7, 8 };

            for (int wave = 1; wave <= 4; wave++)
            {
                _nests.OpenAfterWave(wave);

                Assert.That(_nests.OpenCount, Is.EqualTo(openAfter[wave - 1]), "after wave " + wave);
                for (int index = 0; index < _nests.Count; index++)
                {
                    Assert.That(_nests[index].IsOpen, Is.EqualTo(index < openAfter[wave - 1]), "nest " + index + " after wave " + wave);
                }
            }

            Assert.That(_opened, Is.EqualTo(new[] { 2, 3, 4, 5, 6, 7 }), "NestOpened once for each, in order");
        }

        [Test]
        public void OpenAfterWave_TheLastWaveOrLater_OpensNothingMore()
        {
            for (int wave = 1; wave <= 4; wave++)
            {
                _nests.OpenAfterWave(wave);
            }

            _opened.Clear();

            _nests.OpenAfterWave(5);
            _nests.OpenAfterWave(9);

            Assert.That(_nests.OpenCount, Is.EqualTo(8));
            Assert.That(_opened, Is.Empty);
        }

        [Test]
        public void OpenAfterWave_TheSameWaveTwice_OpensItsNestsOnce()
        {
            _nests.OpenAfterWave(1);
            _nests.OpenAfterWave(1);

            Assert.That(_nests.OpenCount, Is.EqualTo(4));
            Assert.That(_opened, Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void OpenAfterWave_AWaveSkipped_OpensEverythingUpToIt()
        {
            _nests.OpenAfterWave(2);

            Assert.That(_nests.OpenCount, Is.EqualTo(6));
            Assert.That(_opened, Is.EqualTo(new[] { 2, 3, 4, 5 }));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void OpenAfterWave_NoSuchWave_Throws(int wave)
        {
            Assert.That(() => _nests.OpenAfterWave(wave), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        // ---- The ring and the combo ----

        [Test]
        public void Tick_InsideAnOpenNestsRing_ShowsItsComboEmpty()
        {
            _nests.Tick(_nests[0].Position + new Vector3(2.5f, 7f, 0f), true);

            Assert.That(_nests.ActiveNest, Is.EqualTo(0), "2.5 m on the ground counts, however high");
            Assert.That(_shown, Is.EqualTo(new[] { 0 }));
            Assert.That(_nests[0].NotesPlayed, Is.EqualTo(0));
            Assert.That(_nests[0].Combo, Is.EqualTo(Combo));
        }

        [Test]
        public void Tick_StandingStillInTheRing_NeverCollects()
        {
            for (int step = 0; step < 600; step++)
            {
                _nests.Tick(_nests[0].Position, true);
            }

            Assert.That(_nests[0].IsCollected, Is.False);
            Assert.That(_nests[0].Progress01, Is.EqualTo(0f));
            Assert.That(_shown, Is.EqualTo(new[] { 0 }), "shown once, not every tick");
        }

        [Test]
        public void Tick_OutsideEveryRing_ShowsNothingAndNotesDoNothing()
        {
            _nests.Tick(_nests[0].Position + new Vector3(2.6f, 0f, 0f), true);

            Assert.That(_nests.ActiveNest, Is.EqualTo(NestField.NoNest));
            Assert.That(_nests.PlayNote(0), Is.EqualTo(NestComboStep.Ignored));
            Assert.That(_nests[0].NotesPlayed, Is.EqualTo(0));
            Assert.That(_shown, Is.Empty);
        }

        [Test]
        public void PlayNote_TheCombosNotesInOrder_FillItAndTheLastCollectsTheNest()
        {
            _nests.Tick(_nests[0].Position, true);

            Assert.That(_nests.PlayNote(0), Is.EqualTo(NestComboStep.Advanced));
            Assert.That(_nests.PlayNote(1), Is.EqualTo(NestComboStep.Advanced));
            Assert.That(_nests[0].Progress01, Is.EqualTo(0.5f));
            Assert.That(_nests.PlayNote(2), Is.EqualTo(NestComboStep.Advanced));
            Assert.That(_collected, Is.Empty, "three of four notes is not the nest");
            Assert.That(_nests.PlayNote(3), Is.EqualTo(NestComboStep.Completed));

            Assert.That(_collected, Is.EqualTo(new[] { 0 }));
            Assert.That(_nests[0].IsCollected, Is.True);
            Assert.That(_nests[0].IsOpen, Is.False);
            Assert.That(_nests[0].Progress01, Is.EqualTo(1f));
            Assert.That(_nests.CollectedCount, Is.EqualTo(1));
            Assert.That(_nests.OpenCount, Is.EqualTo(1));
            Assert.That(_nests.ActiveNest, Is.EqualTo(NestField.NoNest), "a collected nest's combo is no longer shown");
            Assert.That(_steps, Is.EqualTo(new[]
            {
                NestComboStep.Advanced, NestComboStep.Advanced, NestComboStep.Advanced, NestComboStep.Completed,
            }));
            Assert.That(_hidden, Is.Empty, "completing is not hiding");
        }

        [Test]
        public void PlayNote_AWrongNote_EmptiesTheCombo()
        {
            _nests.Tick(_nests[0].Position, true);
            _nests.PlayNote(0);
            _nests.PlayNote(1);

            Assert.That(_nests.PlayNote(3), Is.EqualTo(NestComboStep.Broken));
            Assert.That(_nests[0].NotesPlayed, Is.EqualTo(0));
            Assert.That(_nests[0].IsOpen, Is.True, "a wrong note costs nothing more");
        }

        [Test]
        public void PlayNote_AWrongNoteThatIsTheFirst_CountsAsTheFirst()
        {
            _nests.Tick(_nests[0].Position, true);
            _nests.PlayNote(0);
            _nests.PlayNote(1);

            Assert.That(_nests.PlayNote(0), Is.EqualTo(NestComboStep.Restarted));
            Assert.That(_nests[0].NotesPlayed, Is.EqualTo(1));
            _nests.PlayNote(1);
            _nests.PlayNote(2);
            Assert.That(_nests.PlayNote(3), Is.EqualTo(NestComboStep.Completed));
        }

        [Test]
        public void PlayNote_AWrongNoteBeforeAnyRightOne_IsIgnored()
        {
            _nests.Tick(_nests[0].Position, true);

            Assert.That(_nests.PlayNote(2), Is.EqualTo(NestComboStep.Ignored));
            Assert.That(_steps, Is.Empty, "nothing changed, so nothing is raised");
        }

        [Test]
        public void Tick_LeavingTheRing_EmptiesTheComboAndHidesIt()
        {
            _nests.Tick(_nests[0].Position, true);
            _nests.PlayNote(0);
            _nests.PlayNote(1);

            _nests.Tick(Far, true);

            Assert.That(_nests.ActiveNest, Is.EqualTo(NestField.NoNest));
            Assert.That(_nests[0].NotesPlayed, Is.EqualTo(0), "the progress is gone");
            Assert.That(_hidden, Is.EqualTo(new[] { 0 }));

            _nests.Tick(_nests[0].Position, true);
            Assert.That(_nests.PlayNote(2), Is.EqualTo(NestComboStep.Ignored), "back in the ring, the combo starts from its first note");
        }

        [Test]
        public void Tick_WhileTheSwarmCannotCollect_HidesTheComboAndEmptiesIt()
        {
            _nests.Tick(_nests[0].Position, true);
            _nests.PlayNote(0);

            _nests.Tick(_nests[0].Position, false);

            Assert.That(_nests.ActiveNest, Is.EqualTo(NestField.NoNest));
            Assert.That(_nests[0].NotesPlayed, Is.EqualTo(0));
            Assert.That(_hidden, Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void Tick_BesideAClosedNest_ShowsNothing()
        {
            _nests.Tick(_nests[2].Position, true);

            Assert.That(_nests.ActiveNest, Is.EqualTo(NestField.NoNest));
            _nests.PlayNote(0);
            Assert.That(_nests[2].NotesPlayed, Is.EqualTo(0));
        }

        [Test]
        public void Tick_BesideANestOnceItOpens_ShowsIt()
        {
            _nests.Tick(_nests[2].Position, true);
            _nests.OpenAfterWave(1);
            _nests.Tick(_nests[2].Position, true);

            Assert.That(_nests.ActiveNest, Is.EqualTo(2));
        }

        [Test]
        public void Tick_TwoRingsOverlap_OnlyTheNearestIsActive()
        {
            var positions = Spaced(8);
            positions[1] = new Vector3(3f, 0f, 0f);
            Build(positions, Combos(8));

            _nests.Tick(new Vector3(1f, 0f, 0f), true);
            Assert.That(_nests.ActiveNest, Is.EqualTo(0), "1 m from nest 0, 2 m from nest 1");
            _nests.PlayNote(0);

            _nests.Tick(new Vector3(2f, 0f, 0f), true);
            Assert.That(_nests.ActiveNest, Is.EqualTo(1), "now nest 1 is nearer");
            Assert.That(_nests[0].NotesPlayed, Is.EqualTo(0), "the old ring's combo is emptied");
            Assert.That(_shown, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(_hidden, Is.EqualTo(new[] { 0 }));

            _nests.PlayNote(0);
            Assert.That(_nests[1].NotesPlayed, Is.EqualTo(1));
            Assert.That(_nests[0].NotesPlayed, Is.EqualTo(0), "a note reaches only the active nest");
        }

        [Test]
        public void Tick_AfterCollectingOneOfTwoOverlappingNests_ShowsTheOther()
        {
            var positions = Spaced(8);
            positions[1] = new Vector3(3f, 0f, 0f);
            Build(positions, Combos(8));
            _nests.Tick(new Vector3(1f, 0f, 0f), true);
            PlayAll(Combo);

            _nests.Tick(new Vector3(1f, 0f, 0f), true);

            Assert.That(_nests.ActiveNest, Is.EqualTo(1));
        }

        [Test]
        public void PlayNote_EachNestHearsItsOwnCombo()
        {
            var combos = Combos(8);
            combos[1] = new[] { 3, 1, 3, 2, 0 };
            Build(Spaced(8), combos);
            _nests.Tick(_nests[1].Position, true);

            PlayAll(combos[1]);

            Assert.That(_nests[1].IsCollected, Is.True);
            Assert.That(_nests[0].IsCollected, Is.False);
        }

        [Test]
        public void TickAndPlayNote_EveryStep_AllocateNothing()
        {
            Vector3 at = _nests[0].Position;
            _nests.Tick(at, true);
            _nests.PlayNote(0);
            _nests.PlayNote(3);
            _steps.Capacity = 256; // The test's own list must not grow while it is measured.

            Assert.That(() =>
            {
                _nests.Tick(at, true);
                _nests.PlayNote(0);
                _nests.PlayNote(3);
            }, Is.Not.AllocatingGCMemory());
        }

        private static Vector3[] Spaced(int count)
        {
            var positions = new Vector3[count];
            for (int index = 0; index < count; index++)
            {
                positions[index] = new Vector3(10f * index, 0f, 0f);
            }

            return positions;
        }

        private static int[][] Combos(int count)
        {
            var combos = new int[count][];
            for (int index = 0; index < count; index++)
            {
                combos[index] = (int[])Combo.Clone();
            }

            return combos;
        }

        private void Build(IReadOnlyList<Vector3> positions, IReadOnlyList<int[]> combos = null)
        {
            _nests = new NestField(_config, positions, combos ?? Combos(positions.Count));
            _opened = new List<int>();
            _collected = new List<int>();
            _shown = new List<int>();
            _hidden = new List<int>();
            _steps = new List<NestComboStep>();
            _nests.NestOpened += _opened.Add;
            _nests.NestCollected += _collected.Add;
            _nests.ComboShown += _shown.Add;
            _nests.ComboHidden += _hidden.Add;
            _nests.ComboNote += (nest, step) => _steps.Add(step);
        }

        private void PlayAll(IReadOnlyList<int> holes)
        {
            for (int index = 0; index < holes.Count; index++)
            {
                _nests.PlayNote(holes[index]);
            }
        }
    }
}
