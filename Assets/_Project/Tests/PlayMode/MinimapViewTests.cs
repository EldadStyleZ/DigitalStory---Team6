using System.Collections;
using DigitalStory.Data;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DigitalStory.Tests.PlayMode
{
    public sealed class MinimapViewTests
    {
        private const string SceneName = "Town";
        private const int FramesToSettle = 3;
        private const float TownHeight = 1300f;
        private const float DeepWaterX = 1990f;

        private GameRoot _root;
        private MinimapView _map;
        private TownArtConfig _art;
        private HudConfig _hud;

        [UnitySetUp]
        public IEnumerator LoadTown()
        {
            yield return SceneManager.LoadSceneAsync(SceneName);

            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                yield return null;
            }

            _root = Object.FindAnyObjectByType<GameRoot>();
            _map = Object.FindAnyObjectByType<MinimapView>();
            _art = Resources.FindObjectsOfTypeAll<TownArtConfig>()[0];
            _hud = Resources.FindObjectsOfTypeAll<HudConfig>()[0];
            _root.Flow.Begin();
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Minimap_WhilePlaying_FitsOnScreen()
        {
            yield return null;

            Rect screen = _map.Element.panel.visualTree.worldBound;
            Rect map = _map.Element.worldBound;

            Assert.That(map.width, Is.GreaterThan(0f));
            Assert.That(map.height, Is.GreaterThan(0f));
            Assert.That(map.xMax, Is.LessThanOrEqualTo(screen.xMax));
            Assert.That(map.yMin, Is.GreaterThanOrEqualTo(screen.yMin));
        }

        [UnityTest]
        public IEnumerator Minimap_Drawn_ShowsTheRiver()
        {
            yield return null;

            var deepWater = new Vector2(1950f, 30f);

            AssertSameColor(_map.PixelAt(deepWater), _art.RiverColor);
        }

        [UnityTest]
        public IEnumerator Minimap_Drawn_RiverHasNoGaps()
        {
            yield return null;
            string river = ColorOf(_art.RiverColor);
            string window = ColorOf(_hud.MinimapWindowColor);

            for (float y = 1f; y < TownHeight; y += 1f)
            {
                string pixel = ColorOf(_map.PixelAt(new Vector2(DeepWaterX, y)));
                Assert.That(pixel == river || pixel == window, Is.True, $"At y {y} the map shows {pixel}.");
            }
        }

        [Test]
        public void Painter_ThisTown_HasNoRowPastTheTown()
        {
            var painter = new MinimapPainter(300, 2000f, TownHeight);

            Assert.That(painter.Height, Is.EqualTo(195));
        }

        [Test]
        public void Painter_FillingTheWholeTown_LeavesNoPixelUnpainted()
        {
            var painter = new MinimapPainter(300, 2000f, TownHeight);

            painter.PaintBackground(new Rect(0f, 0f, 2000f, TownHeight), Color.white);
            painter.Clear();

            foreach (Color32 pixel in painter.Pixels)
            {
                Assert.That(pixel.a, Is.EqualTo(255));
            }
        }

        [UnityTest]
        public IEnumerator Minimap_WhilePaused_StopsRepainting()
        {
            yield return null;
            RatSwarm swarm = _root.Session.Swarm;
            Vector2 rat = swarm[0].Position;
            _map.Paint();
            string before = ColorOf(_map.PixelAt(rat));
            _root.Flow.Pause();

            for (int index = 0; index < swarm.Count; index++)
            {
                swarm.Eat(index);
            }

            yield return null;
            yield return null;

            Assert.That(ColorOf(_map.PixelAt(rat)), Is.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator Minimap_Drawn_ShowsTheHouses()
        {
            yield return null;

            House house = _root.Session.Town.Houses[0];
            Vector2 middle = new Vector2(house.X + (house.Width / 2f), house.Y + (house.Height / 2f));

            AssertSameColor(_map.PixelAt(middle), _art.RoofColor(house.RoofStyle));
        }

        [UnityTest]
        public IEnumerator Minimap_Drawn_ShowsThePiperOnTop()
        {
            yield return null;

            AssertSameColor(_map.PixelAt(_root.Session.Piper.Position), _hud.MinimapPiperColor);
        }

        [UnityTest]
        public IEnumerator Minimap_Drawn_ShowsTheCat()
        {
            yield return null;
            _map.Paint();

            AssertSameColor(_map.PixelAt(_root.Session.Cats[0].Position), _hud.MinimapCatColor);
        }

        [UnityTest]
        public IEnumerator Minimap_Drawn_ShowsARat()
        {
            yield return null;
            RatSwarm swarm = _root.Session.Swarm;

            _map.Paint();

            AssertSameColor(_map.PixelAt(swarm[0].Position), _art.CalmRatColor);
        }

        [UnityTest]
        public IEnumerator Minimap_DeadRat_IsNotDrawn()
        {
            yield return null;
            RatSwarm swarm = _root.Session.Swarm;
            Vector2 lastRat = swarm[swarm.Count - 1].Position;

            for (int index = 0; index < swarm.Count; index++)
            {
                swarm.Eat(index);
            }

            _map.Paint();

            Assert.That(ColorOf(_map.PixelAt(lastRat)), Is.Not.EqualTo(ColorOf(_art.CalmRatColor)));
        }

        [UnityTest]
        public IEnumerator Minimap_Painting_AllocatesNothing()
        {
            yield return null;

            Assert.That(() => _map.Paint(), Is.Not.AllocatingGCMemory());
        }

        private static void AssertSameColor(Color32 actual, Color expected)
        {
            Assert.That(ColorOf(actual), Is.EqualTo(ColorOf(expected)));
        }

        private static string ColorOf(Color32 color) => $"{color.r},{color.g},{color.b}";
    }
}
