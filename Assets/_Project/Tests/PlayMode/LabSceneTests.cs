using System.Collections;
using System.Linq;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DigitalStory.Tests.PlayMode
{
    public sealed class LabSceneTests
    {
        private const string SceneName = "eldad-lab-scene";
        private const float Tolerance = 0.001f;

        private LabRoot _root;
        private MazeView _mazeView;
        private LabPiperView _piperView;
        private Camera _camera;

        [UnitySetUp]
        public IEnumerator LoadLab()
        {
            yield return SceneManager.LoadSceneAsync(SceneName);
            yield return null;
            yield return null;

            _root = Object.FindAnyObjectByType<LabRoot>();
            _mazeView = Object.FindAnyObjectByType<MazeView>();
            _piperView = Object.FindAnyObjectByType<LabPiperView>();
            _camera = Camera.main;
        }

        [Test]
        public void Scene_WhenLoaded_HasARunningSession()
        {
            Assert.That(_root.Session, Is.Not.Null);
        }

        [Test]
        public void Maze_WhenDrawn_MatchesTheRulesTileForTile()
        {
            Maze maze = _root.Session.Maze;
            Sprite block = _mazeView.Tiles.GetSprite(Vector3Int.zero);

            Assert.That(block, Is.Not.Null, "Nothing was drawn.");
            Assert.That(_mazeView.Tiles.size.x, Is.EqualTo(maze.Shape.TileColumns));
            Assert.That(_mazeView.Tiles.size.y, Is.EqualTo(maze.Shape.TileRows));

            Color wall = _mazeView.Tiles.GetColor(Vector3Int.zero);

            for (int y = 0; y < maze.Shape.TileRows; y += 7)
            {
                for (int x = 0; x < maze.Shape.TileColumns; x += 7)
                {
                    bool looksLikeWall = _mazeView.Tiles.GetColor(new Vector3Int(x, y, 0)) == wall;
                    Assert.That(looksLikeWall, Is.EqualTo(!maze.IsFloor(x, y)), $"Tile {x},{y} is drawn wrongly.");
                }
            }
        }

        [Test]
        public void Arrows_WhenDrawn_MatchTheSignposts()
        {
            Signposts signposts = _root.Session.Signposts;

            Assert.That(signposts.Cells.Count, Is.GreaterThan(50));

            foreach (int cell in signposts.Cells)
            {
                AssertArrowMatches(signposts, cell);
            }
        }

        [UnityTest]
        public IEnumerator Arrows_WhenABossFalls_TurnTowardTheNext()
        {
            LabSession session = _root.Session;
            Signposts signposts = session.Signposts;
            Minion boss = session.Battle.Minions.First(minion => minion.IsBoss);
            int before = signposts.Cells.Count(cell => signposts.DirectionAt(cell) != Signposts.NoDirection);

            boss.Health.Damage(boss.Health.Max);
            session.Tick(Vector2.zero, 0.01f);
            yield return null;

            Assert.That(before, Is.GreaterThan(0));

            foreach (int cell in signposts.Cells)
            {
                AssertArrowMatches(signposts, cell);
            }
        }

        [Test]
        public void Arrows_SitOverTheFloorAndUnderTheFighters()
        {
            var arrows = _mazeView.Arrows.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            var floor = _mazeView.Tiles.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();

            Assert.That(arrows.sortingOrder, Is.GreaterThan(floor.sortingOrder));
            Assert.That(arrows.sortingOrder, Is.LessThan(3), "Minions draw at 3.");
        }

        private void AssertArrowMatches(Signposts signposts, int cell)
        {
            Vector3Int tile = MazeView.ArrowTileOf(_root.Session.Maze, cell);
            int direction = signposts.DirectionAt(cell);

            if (direction == Signposts.NoDirection)
            {
                Assert.That(_mazeView.Arrows.GetSprite(tile), Is.Null);
                return;
            }

            Assert.That(_mazeView.Arrows.GetSprite(tile), Is.Not.Null, $"Cell {cell} has no arrow.");
            Vector3 pointing = _mazeView.Arrows.GetTransformMatrix(tile).MultiplyVector(Vector3.right).normalized;
            Vector3 expected = Quaternion.Euler(0f, 0f, direction * 90f) * Vector3.right;
            Assert.That((pointing - expected).magnitude, Is.LessThan(Tolerance), $"The arrow at cell {cell} points the wrong way.");
        }

        [UnityTest]
        public IEnumerator Piper_WhenTheRulesMoveIt_IsDrawnThere()
        {
            _root.Session.Tick(Vector2.right, 0.05f);
            yield return null;

            Vector2 drawn = _piperView.transform.position;
            Assert.That((drawn - _root.Session.Piper.Position).magnitude, Is.LessThan(Tolerance));
        }

        [UnityTest]
        public IEnumerator Camera_WhenThePiperMoves_KeepsItCentered()
        {
            _root.Session.Tick(Vector2.right, 0.05f);
            yield return null;

            Vector2 centered = _camera.transform.position;
            Assert.That((centered - _root.Session.Piper.Position).magnitude, Is.LessThan(Tolerance));
        }
    }
}
