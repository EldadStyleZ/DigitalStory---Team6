using System.Collections;
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
