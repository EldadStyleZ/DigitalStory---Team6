#if UNITY_EDITOR
using System.Collections;
using DigitalStory.GrassBrush;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>
    /// Plan section AD's regression guard for the grass: in the game's top view over the forest-clearing map the field draws
    /// only the small cells in view (the merged branch drew about 1.4 million clumps a frame from 64 m cells), and once its
    /// cells are filled it builds none while the camera stands still, so nothing is sent to the GPU per frame.
    /// </summary>
    public sealed class HiveDefenseGrassGuardTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const int FramesToSettle = 60;
        private const int StillFrames = 30;

        // Measured in the built player at the map's start: about 190,000 clumps (2.8 million triangles) a frame. Twice that
        // still fails a return to drawing whole 64 m cells.
        private const int MostTopViewClumps = 400000;
        private const int MostTopViewCells = 200;

        [UnityTest]
        public IEnumerator TopView_DrawsOnlyTheCellsInView_AndBuildsNoneWhileTheCameraStandsStill()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            GrassField grass = Object.FindAnyObjectByType<GrassField>();
            Camera camera = Camera.main;
            Assert.That(grass, Is.Not.Null, "The scene has no grass field.");
            Assert.That(camera, Is.Not.Null, "The scene has no main camera.");

            // Batch mode has no game view, so the camera is rendered by hand; each render draws the grass for it.
            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                camera.Render();
                yield return null;
            }

            int built = grass.CellsBuilt;
            for (int frame = 0; frame < StillFrames; frame++)
            {
                camera.Render();
                yield return null;
            }

            Assert.That(grass.CellsBuilt, Is.EqualTo(built), "cells were built again while the camera stood still");
            Assert.That(grass.DrawnCells, Is.InRange(1, MostTopViewCells), "draw calls (cells) in the top view");
            Assert.That(grass.DrawnClumps, Is.InRange(1, MostTopViewClumps), "clumps drawn in the top view");
        }
    }
}
#endif
