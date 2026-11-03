using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace PiperToy.Tests
{
    // These load the real Toy scene and play the flute the way PiperInput does, by calling Press and Release.
    public sealed class ToyTests
    {
        private const string SceneName = "Toy";
        private const float Near = 2.5f;

        private Flute _flute;
        private RatHerd _herd;
        private PiperLook _look;
        private ComboRow _row;

        private Vector2 Piper => _flute.transform.position;

        [UnitySetUp]
        public IEnumerator LoadTheToy()
        {
            yield return SceneManager.LoadSceneAsync(SceneName);
            yield return null;

            _flute = Object.FindAnyObjectByType<Flute>();
            _herd = Object.FindAnyObjectByType<RatHerd>();
            _look = Object.FindAnyObjectByType<PiperLook>();
            _row = Object.FindAnyObjectByType<ComboRow>();
        }

        private static void Place(Rat rat, Vector2 spot)
        {
            rat.GetComponent<Rigidbody2D>().position = spot;
            rat.transform.position = spot;
        }

        // Sends every rat to a far corner, out of reach of the flute and of every song.
        private void ClearTheField()
        {
            foreach (Rat rat in _herd.Rats)
            {
                Place(rat, Piper + new Vector2(12f, 7f));
            }
        }

        private Rat RatNearThePiper(int index = 0)
        {
            Rat rat = _herd.Rats[index];
            Place(rat, Piper + new Vector2(Near, 0f));
            return rat;
        }

        private IEnumerator Play(IEnumerable<int> keys)
        {
            foreach (int key in keys)
            {
                _flute.Press(key);
                yield return null;
                _flute.Release(key);
                yield return null;
            }
        }

        private Song SongNamed(string title) => _flute.Songs.Single(song => song.Title == title);

        [Test]
        public void Scene_HasTheToysGameObjects()
        {
            string[] roots = SceneManager.GetActiveScene().GetRootGameObjects().Select(root => root.name).ToArray();

            Assert.That(roots, Is.EquivalentTo(new[] { "Main Camera", "Ground", "Piper", "Rats", "HUD" }));
            Assert.That(_herd.Rats, Is.Not.Empty);
            Assert.That(_herd.Rats.All(rat => rat.transform.parent == _herd.transform), Is.True);
        }

        // PiperInput would set the direction from the real keyboard every frame, so it is switched off here.
        [UnityTest]
        public IEnumerator Piper_GivenADirection_WalksThatWayAndFacesIt()
        {
            var movement = _flute.GetComponent<PiperMovement>();
            _flute.GetComponent<PiperInput>().enabled = false;
            Vector2 start = Piper;

            movement.Direction = Vector2.left;
            yield return new WaitForSeconds(0.5f);

            Assert.That(Piper.x, Is.LessThan(start.x - 1f));
            Assert.That(Mathf.Abs(Piper.y - start.y), Is.LessThan(0.1f));
            Assert.That(movement.Facing, Is.EqualTo(Vector2.left));
        }

        [UnityTest]
        public IEnumerator Piper_AtTheEdgeOfTheField_CannotLeaveIt()
        {
            var movement = _flute.GetComponent<PiperMovement>();
            _flute.GetComponent<PiperInput>().enabled = false;
            float edge = Object.FindAnyObjectByType<EdgeCollider2D>().bounds.max.x;

            movement.Direction = Vector2.right;
            yield return new WaitForSeconds(5f);

            Assert.That(Piper.x, Is.LessThan(edge));
        }

        [UnityTest]
        public IEnumerator Flute_HoldingACallHole_MakesANearRatFollow()
        {
            ClearTheField();
            Rat rat = RatNearThePiper();

            _flute.Press(1);
            yield return new WaitForSeconds(0.8f);

            Assert.That(rat.Mood, Is.EqualTo(RatMood.Follow));
            Assert.That(Vector2.Distance(rat.transform.position, Piper), Is.LessThan(Near - 0.5f));
        }

        [UnityTest]
        public IEnumerator Flute_HoldingACallHole_LeavesAFarRatAlone()
        {
            ClearTheField();
            Rat far = _herd.Rats[0];

            _flute.Press(2);
            yield return new WaitForSeconds(0.3f);

            Assert.That(far.Mood, Is.EqualTo(RatMood.Wander));
        }

        [UnityTest]
        public IEnumerator Flute_HoldingAMarchHole_MarchesANearRatAhead()
        {
            ClearTheField();
            Rat rat = RatNearThePiper();

            _flute.Press(3);
            yield return new WaitForSeconds(0.3f);

            Assert.That(rat.Mood, Is.EqualTo(RatMood.March));
        }

        [UnityTest]
        public IEnumerator Flute_HoldingAScareHole_ChasesANearRatAway()
        {
            ClearTheField();
            Rat rat = RatNearThePiper();

            _flute.Press(6);
            yield return new WaitForSeconds(0.6f);

            Assert.That(rat.Mood, Is.EqualTo(RatMood.Flee));
            Assert.That(Vector2.Distance(rat.transform.position, Piper), Is.GreaterThan(Near + 0.5f));
        }

        [UnityTest]
        public IEnumerator Flute_TheLastHolePressed_DecidesWhatTheRatsDo()
        {
            _flute.Press(1);
            _flute.Press(5);
            yield return null;
            Assert.That(_flute.Mode, Is.EqualTo(FluteMode.Scare));

            _flute.Release(5);
            yield return null;
            Assert.That(_flute.Mode, Is.EqualTo(FluteMode.Call));

            _flute.Release(1);
            yield return null;
            Assert.That(_flute.Mode, Is.EqualTo(FluteMode.Silent));
        }

        [UnityTest]
        public IEnumerator Flute_PlayingASongsKeys_PlaysIt()
        {
            var played = new List<Song>();
            _flute.SongPlayed += played.Add;

            yield return Play(new[] { 1, 3, 5 });

            Assert.That(played, Is.EqualTo(new[] { SongNamed("Dance") }));
        }

        [UnityTest]
        public IEnumerator Flute_ASongJustPlayed_RestsBeforeItPlaysAgain()
        {
            var played = new List<Song>();
            _flute.SongPlayed += played.Add;

            yield return Play(new[] { 1, 3, 5, 1, 3, 5 });

            Assert.That(played.Count, Is.EqualTo(1));
            Assert.That(_flute.IsReady(SongNamed("Dance")), Is.False);
        }

        // Each song gets a fresh pair of rats: one close enough to hear it and one too far away.
        [UnityTest]
        public IEnumerator Songs_EveryOne_GivesTheRatsInReachItsMood()
        {
            var expected = new Dictionary<SongEffect, RatMood>
            {
                { SongEffect.Dance, RatMood.Follow },
                { SongEffect.Conga, RatMood.Conga },
                { SongEffect.Heart, RatMood.Heart },
                { SongEffect.Disco, RatMood.Disco },
                { SongEffect.Balloons, RatMood.Float },
                { SongEffect.Choir, RatMood.Sing },
                { SongEffect.CrowdSurf, RatMood.Follow },
                { SongEffect.Faint, RatMood.Faint },
            };
            Assert.That(_herd.Rats.Count, Is.GreaterThanOrEqualTo(_flute.Songs.Count * 2), "Each song needs two rats of its own.");

            for (int index = 0; index < _flute.Songs.Count; index++)
            {
                Song song = _flute.Songs[index];
                ClearTheField();
                Rat near = RatNearThePiper(index * 2);
                Rat far = _herd.Rats[(index * 2) + 1];

                yield return Play(song.Keys);
                yield return null;

                Assert.That(near.Mood, Is.EqualTo(expected[song.Effect]), song.Title);
                Assert.That(far.Mood, Is.EqualTo(RatMood.Wander), song.Title);
            }
        }

        [UnityTest]
        public IEnumerator Song_CrowdSurf_LiftsThePiper()
        {
            Assert.That(_look.IsLifted, Is.False);

            yield return Play(SongNamed("Crowd Surf").Keys);

            Assert.That(_look.IsLifted, Is.True);
        }

        [UnityTest]
        public IEnumerator Song_Balloons_FloatsRatsOverTheOthersUntilTheyPop()
        {
            ClearTheField();
            Rat rat = RatNearThePiper();

            yield return Play(SongNamed("Balloons").Keys);
            yield return new WaitForSeconds(1f);

            Assert.That(rat.Mood, Is.EqualTo(RatMood.Float));
            Assert.That(rat.GetComponent<Collider2D>().enabled, Is.False, "It floats over the others.");
            Assert.That(rat.transform.position.x, Is.GreaterThan(Piper.x + Near + 0.5f), "The wind carries it east.");
        }

        [UnityTest]
        public IEnumerator FluteBar_AHeldKey_LightsItsHoleOnly()
        {
            var bar = Object.FindAnyObjectByType<FluteBar>();
            _flute.Press(3);
            yield return null;

            Assert.That(Enumerable.Range(1, Flute.HoleCount).Where(bar.IsLit), Is.EqualTo(new[] { 3 }));

            _flute.Release(3);
            yield return null;
            Assert.That(bar.IsLit(3), Is.False);
        }

        [UnityTest]
        public IEnumerator FluteBar_PressingAHoleWithTheMouse_PlaysTheFlute()
        {
            yield return null;
            VisualElement hole = Object.FindAnyObjectByType<FluteBar>().GetComponent<UIDocument>().rootVisualElement.Q("hole-2");
            var mouse = new Event { button = 0, mousePosition = hole.worldBound.center };

            mouse.type = EventType.MouseDown;
            using (PointerDownEvent down = PointerDownEvent.GetPooled(mouse))
            {
                down.target = hole;
                hole.SendEvent(down);
            }

            yield return null;
            Assert.That(_flute.IsHeld(2), Is.True);

            mouse.type = EventType.MouseUp;
            using (PointerUpEvent up = PointerUpEvent.GetPooled(mouse))
            {
                up.target = hole;
                hole.SendEvent(up);
            }

            yield return null;
            Assert.That(_flute.IsHeld(2), Is.False);
        }

        [UnityTest]
        public IEnumerator ComboRow_ShowsEverySongWithItsKeys()
        {
            yield return null;

            Assert.That(_row.ChipCount, Is.EqualTo(_flute.Songs.Count));

            for (int index = 0; index < _flute.Songs.Count; index++)
            {
                Song song = _flute.Songs[index];
                Assert.That(_row.ChipText(index), Is.EqualTo($"{song.Title} {string.Join(" ", song.Keys)}"));
            }
        }

        [UnityTest]
        public IEnumerator ComboRow_APlayedSong_GlowsThenRests()
        {
            yield return Play(SongNamed("Dance").Keys);
            yield return null;
            Assert.That(_row.IsGlowing(0), Is.True);

            yield return new WaitForSeconds(1.5f);
            Assert.That(_row.IsGlowing(0), Is.False);
            Assert.That(_row.IsResting(0), Is.True);
        }
    }
}
