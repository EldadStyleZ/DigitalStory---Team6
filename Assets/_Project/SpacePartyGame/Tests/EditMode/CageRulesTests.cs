using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SpacePartyGame.Tests
{
    public sealed class CagePlacementTests
    {
        // A plain 16 by 10 floor, centred on the origin.
        private static readonly Rect Floor = new Rect(-8f, -5f, 16f, 10f);
        private static readonly Vector2 CageHalf = new Vector2(1f, 1f);

        private static CagePlacement.Rules MakeRules(params Vector2[] spawns) => new CagePlacement.Rules
        {
            IsOnFloor = point => Floor.Contains(point),
            CageHalfSize = CageHalf,
            BorderMargin = 0.5f,
            Gap = 0.5f,
            SpawnPoints = spawns,
            SpawnClearance = 0.6f,
            Centre = Vector2.zero,
            MinCentreDistance = 3f,
            SearchRadius = 4f,
            SearchStep = 0.25f,
        };

        [Test]
        public void Place_ValidPreferredSpots_KeepsThemExactly()
        {
            var preferred = new[] { new Vector2(-5f, 0f), new Vector2(5f, 0f) };

            Vector2[] placed = CagePlacement.Place(preferred, MakeRules());

            Assert.That(placed, Is.EqualTo(preferred));
        }

        [Test]
        public void Place_SpotTooCloseToTheBorder_MovesItInside()
        {
            Vector2[] placed = CagePlacement.Place(new[] { new Vector2(-7.5f, 0f) }, MakeRules());

            Assert.That(placed[0].x - CageHalf.x - 0.5f, Is.GreaterThanOrEqualTo(Floor.xMin));
        }

        [Test]
        public void Place_SpotInTheCentre_MovesItOut()
        {
            Vector2[] placed = CagePlacement.Place(new[] { new Vector2(0.5f, 0f) }, MakeRules());

            Assert.That(placed[0].magnitude, Is.GreaterThanOrEqualTo(3f));
        }

        [Test]
        public void Place_SpotOnASpawnPoint_MovesItClear()
        {
            Vector2 spawn = new Vector2(4f, 2f);

            Vector2[] placed = CagePlacement.Place(new[] { new Vector2(4f, 2f) }, MakeRules(spawn));

            var cage = new Rect(placed[0] - CageHalf, CageHalf * 2f);
            Assert.That(Expand(cage, 0.6f).Contains(spawn), Is.False);
        }

        [Test]
        public void Place_TwoSpotsOnTopOfEachOther_SeparatesThem()
        {
            Vector2[] placed = CagePlacement.Place(new[] { new Vector2(5f, 0f), new Vector2(5f, 0.5f) }, MakeRules());

            var first = new Rect(placed[0] - CageHalf, CageHalf * 2f);
            var second = new Rect(placed[1] - CageHalf, CageHalf * 2f);
            Assert.That(Expand(first, 0.25f).Overlaps(Expand(second, 0.25f)), Is.False);
        }

        [Test]
        public void Place_NoRoomAnywhere_Throws()
        {
            CagePlacement.Rules rules = MakeRules();
            rules.IsOnFloor = point => false;

            Assert.Throws<System.InvalidOperationException>(() => CagePlacement.Place(new[] { Vector2.zero }, rules));
        }

        [Test]
        public void IsValid_ReportsEachRule()
        {
            CagePlacement.Rules rules = MakeRules(new Vector2(5f, 3f));
            var none = new List<Vector2>();

            Assert.That(CagePlacement.IsValid(new Vector2(-5f, 0f), none, rules), Is.True);
            Assert.That(CagePlacement.IsValid(new Vector2(-7f, 0f), none, rules), Is.False, "border");
            Assert.That(CagePlacement.IsValid(new Vector2(1f, 1f), none, rules), Is.False, "centre");
            Assert.That(CagePlacement.IsValid(new Vector2(5f, 2.5f), none, rules), Is.False, "spawn");
            Assert.That(CagePlacement.IsValid(new Vector2(-5f, 0f), new List<Vector2> { new Vector2(-5f, 1.5f) }, rules), Is.False, "overlap");
        }

        private static Rect Expand(Rect rect, float by) =>
            new Rect(rect.xMin - by, rect.yMin - by, rect.width + (by * 2f), rect.height + (by * 2f));
    }

    public sealed class CageCountingTests
    {
        private static readonly Rect Interior = new Rect(-1f, -1f, 2f, 2f);

        [Test]
        public void Inside_CountsOnlyActiveCentresInside()
        {
            var candidates = new[]
            {
                (Id: 1, Centre: new Vector2(0f, 0f), Active: true),
                (Id: 2, Centre: new Vector2(0.9f, -0.9f), Active: true),
                (Id: 3, Centre: new Vector2(1.2f, 0f), Active: true),
                (Id: 4, Centre: new Vector2(0f, 0.5f), Active: false),
            };

            ISet<int> inside = CageCounting.Inside(candidates, Interior);

            Assert.That(inside, Is.EquivalentTo(new[] { 1, 2 }));
        }

        [Test]
        public void Inside_NoCandidates_IsEmpty()
        {
            var none = new (int Id, Vector2 Centre, bool Active)[0];

            Assert.That(CageCounting.Inside(none, Interior), Is.Empty);
        }
    }

    public sealed class ScoreboardTests
    {
        [Test]
        public void SetCount_GivesPointsPerVermin()
        {
            var board = new Scoreboard(4, 5);

            board.SetCount(1, 3);

            Assert.That(board.ScoreOf(1), Is.EqualTo(15));
            Assert.That(board.ScoreOf(0), Is.EqualTo(0));
        }

        [Test]
        public void SetCount_FewerVermin_TakesPointsAway()
        {
            var board = new Scoreboard(4, 5);
            board.SetCount(0, 3);

            board.SetCount(0, 2);

            Assert.That(board.ScoreOf(0), Is.EqualTo(10));
        }

        [Test]
        public void SetCount_RaisesChangedOnlyWhenTheScoreChanges()
        {
            var board = new Scoreboard(4, 5);
            var seen = new List<(int, int)>();
            board.Changed += (slot, score) => seen.Add((slot, score));

            board.SetCount(2, 1);
            board.SetCount(2, 1);
            board.SetCount(2, 0);

            Assert.That(seen, Is.EqualTo(new[] { (2, 5), (2, 0) }));
        }

        [Test]
        public void Lock_FreezesEveryScore()
        {
            var board = new Scoreboard(4, 5);
            board.SetCount(0, 2);

            board.Lock();
            board.SetCount(0, 7);

            Assert.That(board.IsLocked, Is.True);
            Assert.That(board.ScoreOf(0), Is.EqualTo(10));
        }

        [Test]
        public void Reset_ClearsScoresAndUnlocks()
        {
            var board = new Scoreboard(4, 5);
            board.SetCount(3, 4);
            board.Lock();

            board.Reset();

            Assert.That(board.IsLocked, Is.False);
            Assert.That(board.Scores, Is.EqualTo(new[] { 0, 0, 0, 0 }));
        }
    }
}
