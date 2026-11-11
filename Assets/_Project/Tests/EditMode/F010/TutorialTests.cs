using System.Collections.Generic;
using DigitalStory.Logic;
using NUnit.Framework;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Plan section N: the first farm phase teaches one thing at a time. Each step completes only when it is the current
    /// one, and the lesson ends at once when the first wave starts. The session's own wiring is in
    /// <see cref="HiveDefenseSessionTests"/>.
    /// </summary>
    public sealed class TutorialTests
    {
        private Tutorial _tutorial;
        private List<TutorialStep> _changes;

        [SetUp]
        public void SetUp()
        {
            _tutorial = new Tutorial();
            _changes = new List<TutorialStep>();
            _tutorial.StepChanged += _changes.Add;
        }

        [Test]
        public void New_StartsAtMove()
        {
            Assert.That(_tutorial.Current, Is.EqualTo(TutorialStep.Move));
            Assert.That(_tutorial.IsDone, Is.False);
        }

        [Test]
        public void Complete_TheCurrentStep_MovesToTheNextAndRaisesStepChanged()
        {
            _tutorial.Complete(TutorialStep.Move);

            Assert.That(_tutorial.Current, Is.EqualTo(TutorialStep.PlaySting));
            Assert.That(_changes, Is.EqualTo(new[] { TutorialStep.PlaySting }));
        }

        [TestCase(TutorialStep.PlaySting)]
        [TestCase(TutorialStep.CollectNest)]
        [TestCase(TutorialStep.VisitHive)]
        [TestCase(TutorialStep.HealHive)]
        [TestCase(TutorialStep.CallWave)]
        public void Complete_AStepAheadOfTheCurrentOne_DoesNothing(TutorialStep step)
        {
            _tutorial.Complete(step);

            Assert.That(_tutorial.Current, Is.EqualTo(TutorialStep.Move));
            Assert.That(_changes, Is.Empty);
        }

        [Test]
        public void Complete_AStepAlreadyDone_DoesNothing()
        {
            _tutorial.Complete(TutorialStep.Move);
            _changes.Clear();

            _tutorial.Complete(TutorialStep.Move);

            Assert.That(_tutorial.Current, Is.EqualTo(TutorialStep.PlaySting));
            Assert.That(_changes, Is.Empty);
        }

        [Test]
        public void Complete_EveryStepInOrder_GoesThroughTheLessonToDone()
        {
            _tutorial.Complete(TutorialStep.Move);
            _tutorial.Complete(TutorialStep.PlaySting);
            _tutorial.Complete(TutorialStep.CollectNest);
            _tutorial.Complete(TutorialStep.VisitHive);
            _tutorial.Complete(TutorialStep.HealHive);
            _tutorial.Complete(TutorialStep.CallWave);

            Assert.That(_tutorial.Current, Is.EqualTo(TutorialStep.Done));
            Assert.That(_tutorial.IsDone, Is.True);
            Assert.That(
                _changes,
                Is.EqualTo(new[]
                {
                    TutorialStep.PlaySting, TutorialStep.CollectNest, TutorialStep.VisitHive, TutorialStep.HealHive, TutorialStep.CallWave,
                    TutorialStep.Done,
                }));
        }

        // Plan section N, step 3: "Fly to the glowing nest and stay until it is yours", right after the Sting.
        [Test]
        public void Complete_PlaySting_MovesToCollectNest()
        {
            _tutorial.Complete(TutorialStep.Move);
            _tutorial.Complete(TutorialStep.PlaySting);

            Assert.That(_tutorial.Current, Is.EqualTo(TutorialStep.CollectNest));

            _tutorial.Complete(TutorialStep.CollectNest);

            Assert.That(_tutorial.Current, Is.EqualTo(TutorialStep.VisitHive));
        }

        [Test]
        public void Complete_Done_Throws()
        {
            Assert.That(() => _tutorial.Complete(TutorialStep.Done), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        public void Finish_FromAnyStep_EndsTheLessonOnce(int stepsDone)
        {
            for (int step = 0; step < stepsDone; step++)
            {
                _tutorial.Complete((TutorialStep)step);
            }

            _changes.Clear();

            _tutorial.Finish();
            _tutorial.Finish();

            Assert.That(_tutorial.Current, Is.EqualTo(TutorialStep.Done));
            Assert.That(_changes, Is.EqualTo(new[] { TutorialStep.Done }));
        }

        [Test]
        public void Complete_AfterDone_DoesNothing()
        {
            _tutorial.Finish();
            _changes.Clear();

            _tutorial.Complete(TutorialStep.Move);

            Assert.That(_tutorial.Current, Is.EqualTo(TutorialStep.Done));
            Assert.That(_changes, Is.Empty);
        }
    }
}
