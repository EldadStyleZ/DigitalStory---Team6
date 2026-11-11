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
    /// Plan section H, on the default NestConfig: 8 nests, 2 open at the start, then 2, 2, 1 and 1 more as waves 1 to 4 are
    /// cleared, in the order given. The core collects an open nest by staying within 2.5 m of it for 2 s without a break.
    /// Nest i stands 10 m along x per index, so only one is ever in reach unless a test says otherwise.
    /// </summary>
    public sealed class NestFieldTests
    {
        private const float Step = 0.1f;
        private static readonly Vector3 Far = new Vector3(-500f, 0f, -500f);

        private NestConfig _config;
        private NestField _nests;
        private List<int> _opened;
        private List<int> _collected;

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
            Assert.That(() => new NestField(null, Spaced(8)), Throws.ArgumentNullException);
            Assert.That(() => new NestField(_config, null), Throws.ArgumentNullException);
        }

        [TestCase(7)]
        [TestCase(9)]
        public void New_ADifferentNumberOfPositionsThanTheConfigOpens_Throws(int count)
        {
            Assert.That(() => new NestField(_config, Spaced(count)), Throws.ArgumentException);
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

        // ---- Collecting ----

        [Test]
        public void Tick_CoreBesideAnOpenNestForTwoSeconds_CollectsIt()
        {
            Stay(_nests[0].Position, 1.9f);
            Assert.That(_nests[0].IsCollected, Is.False);
            Assert.That(_nests[0].Progress01, Is.EqualTo(0.95f).Within(0.001f));

            Stay(_nests[0].Position, 0.1f);

            Assert.That(_nests[0].IsCollected, Is.True);
            Assert.That(_nests[0].IsOpen, Is.False, "a collected nest is gone");
            Assert.That(_nests[0].Progress01, Is.EqualTo(1f));
            Assert.That(_collected, Is.EqualTo(new[] { 0 }));
            Assert.That(_nests.CollectedCount, Is.EqualTo(1));
            Assert.That(_nests.OpenCount, Is.EqualTo(1));
        }

        // 120 steps of 1/60 s can add up to just under 2 s in floats; the nest is due all the same.
        [Test]
        public void Tick_TwoSecondsInSixtiethsOfASecond_CollectsOnTheLastStep()
        {
            for (int tick = 0; tick < 119; tick++)
            {
                _nests.Tick(1f / 60f, _nests[0].Position, true);
            }

            Assert.That(_nests[0].IsCollected, Is.False);

            _nests.Tick(1f / 60f, _nests[0].Position, true);

            Assert.That(_nests[0].IsCollected, Is.True);
        }

        [Test]
        public void Tick_ReachIncludesExactlyTwoAndAHalfMetresAndIgnoresHeight()
        {
            Vector3 edge = _nests[0].Position + new Vector3(0f, 5f, 2.5f);
            Vector3 outside = _nests[1].Position + new Vector3(2.51f, 0f, 0f);

            Stay(edge, 0.5f);
            Assert.That(_nests[0].Progress01, Is.GreaterThan(0f), "2.5 m on the ground counts, however high");

            Stay(outside, 0.5f);
            Assert.That(_nests[1].Progress01, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_LeavingTheCircle_StartsTheTwoSecondsAgain()
        {
            Stay(_nests[0].Position, 1.5f);

            Stay(Far, Step);
            Assert.That(_nests[0].Progress01, Is.EqualTo(0f), "the clock is reset");

            Stay(_nests[0].Position, 1.5f);
            Assert.That(_nests[0].IsCollected, Is.False, "1.5 s after coming back, not 3 s in all");

            Stay(_nests[0].Position, 0.5f);
            Assert.That(_nests[0].IsCollected, Is.True);
        }

        [Test]
        public void NotifyCoreHit_StartsTheTwoSecondsAgain()
        {
            Stay(_nests[0].Position, 1.5f);

            _nests.NotifyCoreHit();

            Assert.That(_nests[0].Progress01, Is.EqualTo(0f));
            Stay(_nests[0].Position, 1.5f);
            Assert.That(_nests[0].IsCollected, Is.False, "the hit broke the stay");
            Stay(_nests[0].Position, 0.5f);
            Assert.That(_nests[0].IsCollected, Is.True);
        }

        [Test]
        public void Tick_WhileTheSwarmCannotCollect_MakesNoProgressAndStartsAgain()
        {
            Stay(_nests[0].Position, 1.5f);

            _nests.Tick(1f, _nests[0].Position, false);

            Assert.That(_nests[0].IsCollected, Is.False);
            Assert.That(_nests[0].Progress01, Is.EqualTo(0f), "a collapsed swarm cannot hold the nest");
        }

        [Test]
        public void Tick_BesideAClosedNest_MakesNoProgressAndCollectsNothing()
        {
            Stay(_nests[2].Position, 5f);

            Assert.That(_nests[2].IsCollected, Is.False);
            Assert.That(_nests[2].Progress01, Is.EqualTo(0f));
            Assert.That(_collected, Is.Empty);
        }

        [Test]
        public void Tick_BesideANestOnceItOpens_CollectsIt()
        {
            Stay(_nests[2].Position, 1f);

            _nests.OpenAfterWave(1);
            Stay(_nests[2].Position, 2f);

            Assert.That(_nests[2].IsCollected, Is.True, "the clock starts when it opens, not before");
            Assert.That(_collected, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void Tick_BesideACollectedNest_CollectsItOnlyOnce()
        {
            Stay(_nests[0].Position, 2f);

            Stay(_nests[0].Position, 5f);

            Assert.That(_collected, Is.EqualTo(new[] { 0 }));
            Assert.That(_nests.CollectedCount, Is.EqualTo(1));
        }

        // Each open nest keeps its own clock, so a core in reach of two collects both.
        [Test]
        public void Tick_TwoOpenNestsInReach_CollectsBoth()
        {
            Build(new[] { new Vector3(0f, 0f, 0f), new Vector3(2f, 0f, 0f), new Vector3(20f, 0f, 0f), new Vector3(30f, 0f, 0f), new Vector3(40f, 0f, 0f), new Vector3(50f, 0f, 0f), new Vector3(60f, 0f, 0f), new Vector3(70f, 0f, 0f) });

            Stay(new Vector3(1f, 0f, 0f), 2f);

            Assert.That(_collected, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(_nests.CollectedCount, Is.EqualTo(2));
        }

        [Test]
        public void Tick_LeavingOneOfTwoNestsInReach_RestartsOnlyThatOne()
        {
            Build(new[] { new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), new Vector3(20f, 0f, 0f), new Vector3(30f, 0f, 0f), new Vector3(40f, 0f, 0f), new Vector3(50f, 0f, 0f), new Vector3(60f, 0f, 0f), new Vector3(70f, 0f, 0f) });
            Stay(new Vector3(2f, 0f, 0f), 1f);

            Stay(new Vector3(0f, 0f, 0f), 1f);

            Assert.That(_collected, Is.EqualTo(new[] { 0 }));
            Assert.That(_nests[1].Progress01, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_EveryStep_AllocatesNothing()
        {
            Vector3 at = _nests[0].Position;
            _nests.Tick(Step, at, true);

            Assert.That(() => { _nests.Tick(Step, at, true); }, Is.Not.AllocatingGCMemory());
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

        private void Build(IReadOnlyList<Vector3> positions)
        {
            _nests = new NestField(_config, positions);
            _opened = new List<int>();
            _collected = new List<int>();
            _nests.NestOpened += _opened.Add;
            _nests.NestCollected += _collected.Add;
        }

        private void Stay(Vector3 core, float seconds)
        {
            int steps = Mathf.RoundToInt(seconds / Step);
            for (int step = 0; step < steps; step++)
            {
                _nests.Tick(Step, core, true);
            }
        }
    }
}
