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

        // Where a line from a point first meets something that never moves: the edge of the field or an obstacle.
        private static Vector2 EdgeFrom(Vector2 start, Vector2 direction)
        {
            foreach (RaycastHit2D hit in Physics2D.RaycastAll(start, direction, 100f))
            {
                if (hit.collider.attachedRigidbody == null)
                {
                    return hit.point;
                }
            }

            throw new AssertionException($"Nothing stands in the way from {start} going {direction}.");
        }

        private static Transform Obstacles => GameObject.Find("Obstacles").transform;

        private static IEnumerable<Collider2D> ObstacleColliders => Obstacles.GetComponentsInChildren<Collider2D>();

        [Test]
        public void Scene_HasTheToysGameObjects()
        {
            string[] roots = SceneManager.GetActiveScene().GetRootGameObjects().Select(root => root.name).ToArray();

            Assert.That(roots, Is.EquivalentTo(new[] { "Game", "Main Camera", "Ground", "Obstacles", "Piper", "Rats", "HUD" }));
            Assert.That(_herd.Rats, Is.Not.Empty);
            Assert.That(_herd.Rats.All(rat => rat.transform.parent == _herd.transform), Is.True);
        }

        [Test]
        public void Restart_IsOnTheRKey()
        {
            Assert.That(Object.FindAnyObjectByType<RestartKey>().Key, Is.EqualTo(KeyCode.R));
        }

        // The test cannot press a real key, so it calls what the R key calls.
        [UnityTest]
        public IEnumerator Restart_StartsTheToyAgainFromTheBeginning()
        {
            var movement = _flute.GetComponent<PiperMovement>();
            _flute.GetComponent<PiperInput>().enabled = false;
            Vector2 start = Piper;
            int frogs = _herd.Rats.Count;
            movement.Direction = Vector2.left;
            yield return new WaitForSeconds(0.5f);
            Assert.That(Vector2.Distance(Piper, start), Is.GreaterThan(1f), "The Piper has to move for this test to mean anything.");

            Object.FindAnyObjectByType<RestartKey>().Restart();
            yield return null;
            yield return null;

            var flute = Object.FindAnyObjectByType<Flute>();
            Assert.That(_flute == null && _herd == null, Is.True, "The old Piper and frogs are gone.");
            Assert.That(Vector2.Distance(flute.transform.position, start), Is.LessThan(0.01f), "The new Piper stands where the game starts.");
            Assert.That(flute.GetComponent<PiperInput>().enabled, Is.True, "The new Piper listens to the keyboard again.");
            Assert.That(Object.FindAnyObjectByType<RatHerd>().Rats.Count, Is.EqualTo(frogs), "A fresh herd of frogs.");
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

        // The Piper walks through the crowd; only the rats bump into each other.
        [UnityTest]
        public IEnumerator Piper_WalkingIntoARat_DoesNotPushIt()
        {
            var movement = _flute.GetComponent<PiperMovement>();
            _flute.GetComponent<PiperInput>().enabled = false;
            ClearTheField();
            Rat rat = _herd.Rats[0];
            Place(rat, Piper + new Vector2(1f, 0f));
            Vector2 start = rat.transform.position;

            movement.Direction = Vector2.right;
            yield return new WaitForSeconds(0.6f);

            Assert.That(Piper.x, Is.GreaterThan(start.x), "The Piper has to walk past the rat for this test to mean anything.");
            Assert.That(Vector2.Distance(rat.transform.position, start), Is.LessThan(0.6f));
        }

        [UnityTest]
        public IEnumerator Piper_Turning_EasesIntoItsNewHeading()
        {
            var movement = _flute.GetComponent<PiperMovement>();
            _flute.GetComponent<PiperInput>().enabled = false;
            Transform body = _flute.transform.Find("Body");
            yield return null;
            Assert.That(Mathf.DeltaAngle(body.eulerAngles.z, 0f), Is.EqualTo(0f).Within(1f), "It starts facing right.");

            // Frames in the test runner can be very short or, on a hiccup, long, so this only asks that the turn has
            // started and not finished: a snap would already read 90.
            movement.Direction = Vector2.up;
            yield return new WaitForSeconds(0.05f);
            float soon = Mathf.DeltaAngle(0f, body.eulerAngles.z);
            Assert.That(soon, Is.GreaterThan(0.1f).And.LessThan(89.5f), "Partway round soon after.");

            yield return new WaitForSeconds(1f);
            Assert.That(Mathf.DeltaAngle(body.eulerAngles.z, 90f), Is.EqualTo(0f).Within(1f), "Facing up in the end.");
        }

        [UnityTest]
        public IEnumerator Piper_AtTheEdgeOfTheField_CannotLeaveIt()
        {
            var movement = _flute.GetComponent<PiperMovement>();
            _flute.GetComponent<PiperInput>().enabled = false;
            Vector2 edge = EdgeFrom(Piper, Vector2.right);

            movement.Direction = Vector2.right;
            yield return new WaitForSeconds(5f);

            Assert.That(Piper.x, Is.LessThan(edge.x));
            Assert.That(Piper.x, Is.GreaterThan(edge.x - 1f), "It walks right up to the edge.");
        }

        [Test]
        public void Obstacles_AreTheShapesTheTeamChose()
        {
            string[] chosen = { "Shape1", "Shape2", "Shape4", "Shape5", "Shape6" };
            SpriteRenderer[] shapes = Obstacles.GetComponentsInChildren<SpriteRenderer>();

            Assert.That(shapes.Length, Is.GreaterThanOrEqualTo(8));
            Assert.That(shapes.Select(shape => shape.sprite.name).Distinct(), Is.EquivalentTo(chosen));
            Assert.That(shapes.All(shape => shape.GetComponent<PolygonCollider2D>() != null), Is.True, "Every obstacle is solid.");
        }

        // The Piper starts in the middle, so the middle stays open; the obstacles all stand on the field.
        [Test]
        public void Obstacles_StandOnTheFieldAndClearOfTheMiddle()
        {
            Collider2D field = GameObject.Find("Ground").GetComponent<EdgeCollider2D>();

            foreach (Transform obstacle in Obstacles)
            {
                Assert.That(Vector2.Distance(obstacle.position, Piper), Is.GreaterThan(3f), obstacle.name);
                Assert.That(field.bounds.Contains(new Vector3(obstacle.position.x, obstacle.position.y, field.bounds.center.z)), Is.True, obstacle.name);
            }
        }

        [UnityTest]
        public IEnumerator Obstacles_StopThePiper()
        {
            var movement = _flute.GetComponent<PiperMovement>();
            _flute.GetComponent<PiperInput>().enabled = false;
            Transform nearest = Obstacles.Cast<Transform>().OrderBy(obstacle => Vector2.Distance(obstacle.position, Piper)).First();
            Collider2D solid = nearest.GetComponent<Collider2D>();

            movement.Direction = ((Vector2)nearest.position - Piper).normalized;

            for (float waited = 0f; waited < 4f; waited += Time.deltaTime)
            {
                yield return null;
                Assert.That(solid.OverlapPoint(Piper), Is.False, "The Piper walked into the obstacle.");
            }

            Assert.That(Vector2.Distance(Piper, nearest.position), Is.LessThan(Vector2.Distance(Vector2.zero, nearest.position)), "The Piper has to walk towards it for this test to mean anything.");
        }

        // A wall over half the start area: a rat that ignored it would start inside about half the time.
        [UnityTest]
        public IEnumerator Rats_StartOnlyWhereThereIsRoom()
        {
            var wall = new GameObject("Test Wall").AddComponent<BoxCollider2D>();
            wall.size = new Vector2(8f, 14f);
            wall.transform.position = _herd.transform.position + new Vector3(-4f, 0f, 0f);
            yield return new WaitForFixedUpdate();

            for (int spot = 0; spot < 100; spot++)
            {
                Assert.That(wall.OverlapPoint(_herd.FreeSpot()), Is.False);
            }
        }

        // The Piper waits on the far side of an obstacle and a charmed frog walks straight at it, so it has to bump
        // into the obstacle on the way.
        [UnityTest]
        public IEnumerator Obstacles_StopTheFrogs()
        {
            _flute.GetComponent<PiperInput>().enabled = false;
            ClearTheField();
            Transform obstacle = Obstacles.Cast<Transform>().OrderBy(each => Vector2.Distance(each.position, Piper)).First();
            Collider2D solid = obstacle.GetComponent<Collider2D>();
            Vector2 middle = obstacle.position;
            Vector2 across = (middle - Piper).normalized;
            float reach = Mathf.Max(solid.bounds.size.x, solid.bounds.size.y) / 2f + 1.5f;

            Vector2 beyond = middle + (across * reach);
            _flute.transform.position = beyond;
            _flute.GetComponent<Rigidbody2D>().position = beyond;
            Rat rat = _herd.Rats[0];
            Vector2 start = middle - (across * reach);
            Place(rat, start);
            rat.Charm(10f);

            for (float waited = 0f; waited < 3f; waited += Time.deltaTime)
            {
                yield return null;
                Assert.That(solid.OverlapPoint(rat.transform.position), Is.False, "The frog walked into the obstacle.");
            }

            Assert.That(Vector2.Distance(rat.transform.position, middle), Is.LessThan(reach - 0.5f), "The frog has to walk up to the obstacle for this test to mean anything.");
        }

        [Test]
        public void Rats_NeverStartInsideAnObstacle()
        {
            foreach (Rat rat in _herd.Rats)
            {
                Assert.That(ObstacleColliders.Any(solid => solid.OverlapPoint(rat.transform.position)), Is.False, rat.name);
            }
        }

        [Test]
        public void Art_TheRats_AreFrogs()
        {
            foreach (Rat rat in _herd.Rats)
            {
                Assert.That(rat.transform.Find("Body/Sprite").GetComponent<SpriteRenderer>().sprite.name, Is.EqualTo("Frog"));
            }
        }

        [Test]
        public void Art_TheGround_IsTheTeamsBackground()
        {
            Assert.That(GameObject.Find("Ground").GetComponent<SpriteRenderer>().sprite.name, Is.EqualTo("Background"));
        }

        [UnityTest]
        public IEnumerator MusicNotes_WhileANoteIsHeld_FloatOutOfThePiper()
        {
            var notes = Object.FindAnyObjectByType<MusicNotes>().GetComponent<ParticleSystem>();
            yield return new WaitForSeconds(0.3f);
            Assert.That(notes.particleCount, Is.EqualTo(0), "Nothing floats while the flute is silent.");

            _flute.Press(1);
            yield return new WaitForSeconds(0.6f);
            Assert.That(notes.particleCount, Is.GreaterThan(0));

            _flute.Release(1);
            yield return new WaitForSeconds(notes.main.startLifetime.constantMax + 0.3f);
            Assert.That(notes.particleCount, Is.EqualTo(0), "They fade away after the note stops.");
        }

        // As in the old game, each note takes the colour of the hole being played.
        [UnityTest]
        public IEnumerator MusicNotes_TakeTheColourOfTheHeldHole()
        {
            var notes = Object.FindAnyObjectByType<MusicNotes>();
            var particles = notes.GetComponent<ParticleSystem>();

            _flute.Press(3);
            yield return new WaitForSeconds(0.6f);

            var floating = new ParticleSystem.Particle[particles.particleCount];
            particles.GetParticles(floating);
            Color32 green = notes.ColorOf(3);
            Assert.That(floating, Is.Not.Empty);
            Assert.That(floating.All(note => note.startColor.r == green.r && note.startColor.g == green.g && note.startColor.b == green.b), Is.True);
        }

        [Test]
        public void MusicNotes_AreMusicalNotes()
        {
            var renderer = Object.FindAnyObjectByType<MusicNotes>().GetComponent<ParticleSystemRenderer>();

            Assert.That(renderer.sharedMaterial.mainTexture.name, Is.EqualTo("Note"));
        }

        private static MusicRing Ring => Object.FindAnyObjectByType<MusicRing>();

        private static SpriteRenderer[] RingWaves =>
            Ring.GetComponentsInChildren<SpriteRenderer>().Where(wave => wave.gameObject != Ring.gameObject).ToArray();

        [UnityTest]
        public IEnumerator MusicRing_ItsEdgeIsWhereTheMusicStopsReaching()
        {
            yield return null;

            Assert.That(Ring.EdgeRadius, Is.EqualTo(_flute.HearingRadius).Within(0.05f));
        }

        // The glow fades in, in the colour of the held hole, like the notes, and fades out when the music stops.
        [UnityTest]
        public IEnumerator MusicRing_WhileANoteIsHeld_GlowsInThatHolesColour()
        {
            var glow = Ring.GetComponent<SpriteRenderer>();
            Color green = Object.FindAnyObjectByType<MusicNotes>().ColorOf(3);
            yield return new WaitForSeconds(0.3f);
            Assert.That(glow.color.a, Is.LessThan(0.01f), "Nothing shows while the flute is silent.");

            _flute.Press(3);
            yield return new WaitForSeconds(0.6f);
            Assert.That(glow.color.a, Is.GreaterThan(0.1f));
            Assert.That(Vector3.Distance(new Vector3(glow.color.r, glow.color.g, glow.color.b), new Vector3(green.r, green.g, green.b)), Is.LessThan(0.05f));

            _flute.Release(3);
            yield return new WaitForSeconds(0.6f);
            Assert.That(glow.color.a, Is.LessThan(0.01f), "It fades out when the note stops.");
        }

        // While a note is held, soft waves roll out from the Piper to the edge of the music's reach, and fade.
        [UnityTest]
        public IEnumerator MusicRing_WhileANoteIsHeld_SendsWavesOutToTheEdge()
        {
            _flute.Press(1);
            yield return new WaitForSeconds(0.2f);
            SpriteRenderer wave = RingWaves.OrderByDescending(each => each.color.a).First();
            float early = wave.bounds.extents.x;
            Assert.That(wave.color.a, Is.GreaterThan(0.05f), "A wave rolls out as the note starts.");

            yield return new WaitForSeconds(0.3f);
            Assert.That(wave.bounds.extents.x, Is.GreaterThan(early), "The wave travels outwards.");
            Assert.That(wave.bounds.extents.x, Is.LessThanOrEqualTo(Ring.GetComponent<SpriteRenderer>().bounds.extents.x + 0.01f),
                "It stays inside the glow.");

            _flute.Release(1);
            yield return new WaitForSeconds(1.5f);
            Assert.That(RingWaves.All(each => each.color.a < 0.01f), Is.True, "The waves fade away after the note stops.");
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
                { SongEffect.CrowdSurf, RatMood.Carry },
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

        // Six frogs scattered round the Piper: all of them, not just the nearest, should be in line behind it soon.
        private Rat[] SixFrogsAround()
        {
            ClearTheField();
            var frogs = new Rat[6];

            for (int index = 0; index < frogs.Length; index++)
            {
                float angle = index * 60f * Mathf.Deg2Rad;
                frogs[index] = _herd.Rats[index];
                Place(frogs[index], Piper + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (2.5f + (index * 0.4f))));
            }

            return frogs;
        }

        [UnityTest]
        public IEnumerator Song_Conga_EveryFrogLinesUpBehindThePiperAtOnce()
        {
            Rat[] frogs = SixFrogsAround();
            Vector2 behind = -_flute.GetComponent<PiperMovement>().Facing;

            yield return Play(SongNamed("Conga").Keys);
            yield return new WaitForSeconds(2.5f);

            foreach (Rat frog in frogs)
            {
                Vector2 offset = (Vector2)frog.transform.position - Piper;
                float across = Mathf.Abs((offset.x * behind.y) - (offset.y * behind.x));
                Assert.That(frog.Mood, Is.EqualTo(RatMood.Conga), frog.name);
                Assert.That(Vector2.Dot(offset, behind), Is.GreaterThan(0f), $"{frog.name} is behind the Piper.");
                Assert.That(across, Is.LessThan(0.8f), $"{frog.name} is in the line.");
            }
        }

        // The Piper walks right and then up; the frogs go round the corner where it went, one after another.
        [UnityTest]
        public IEnumerator Song_Conga_TheLineFollowsThePipersFootsteps()
        {
            var movement = _flute.GetComponent<PiperMovement>();
            _flute.GetComponent<PiperInput>().enabled = false;
            Rat[] frogs = SixFrogsAround();
            yield return Play(SongNamed("Conga").Keys);
            yield return new WaitForSeconds(2.5f);
            Vector2 start = Piper;

            movement.Direction = Vector2.right;
            yield return new WaitForSeconds(0.75f);
            Vector2 corner = Piper;
            movement.Direction = Vector2.up;
            yield return new WaitForSeconds(0.75f);
            movement.Direction = Vector2.zero;
            yield return new WaitForSeconds(1.5f);

            Assert.That(corner.x - start.x, Is.GreaterThan(2f), "The Piper has to walk for this test to mean anything.");
            Assert.That(Piper.y - corner.y, Is.GreaterThan(2f), "The Piper has to turn for this test to mean anything.");

            foreach (Rat frog in frogs)
            {
                float offThePath = Mathf.Min(
                    DistanceToSegment(frog.transform.position, start, corner),
                    DistanceToSegment(frog.transform.position, corner, Piper));
                Assert.That(frog.Mood, Is.EqualTo(RatMood.Conga), frog.name);
                Assert.That(offThePath, Is.LessThan(0.45f), $"{frog.name} is on the Piper's path.");
            }
        }

        private static float DistanceToSegment(Vector2 point, Vector2 from, Vector2 to)
        {
            Vector2 along = to - from;
            float share = Mathf.Clamp01(Vector2.Dot(point - from, along) / along.sqrMagnitude);
            return Vector2.Distance(point, from + (along * share));
        }

        // Holds a frog still, as if an obstacle stood between it and its place.
        private static void HoldStill(Rat frog)
        {
            frog.GetComponent<Rigidbody2D>().constraints |= RigidbodyConstraints2D.FreezePosition;
        }

        // Waits until every frog's song clock is running, for at most a few seconds, and says whether they all are.
        private static IEnumerator WaitForTheClocks(Rat[] frogs, float seconds, bool[] started)
        {
            float giveUpAt = Time.time + seconds;

            while (frogs.Any(frog => frog.IsWaitingForTheOthers) && Time.time < giveUpAt)
            {
                yield return null;
            }

            started[0] = frogs.All(frog => !frog.IsWaitingForTheOthers);
        }

        // The song's seconds count from the moment the whole line stands in place, not from the last note.
        [UnityTest]
        public IEnumerator Song_Conga_TheClockStartsOnceEveryFrogIsInLine()
        {
            Rat[] frogs = SixFrogsAround();
            Song conga = SongNamed("Conga");

            yield return Play(conga.Keys);
            Assert.That(frogs.All(frog => frog.Mood == RatMood.Conga && frog.IsWaitingForTheOthers), Is.True,
                "Nobody's clock runs while the frogs are still running to their places.");

            var started = new bool[1];
            yield return WaitForTheClocks(frogs, 4f, started);
            Assert.That(started[0], Is.True, "Once the line is formed, the clock starts.");
            Assert.That(frogs.All(frog => frog.IsInPlace), Is.True, "The clock started with every frog in its place.");

            yield return new WaitForSeconds(conga.Seconds - 0.5f);
            Assert.That(frogs.All(frog => frog.Mood == RatMood.Conga), Is.True, "They dance the song's full time.");

            yield return new WaitForSeconds(1f);
            Assert.That(frogs.Any(frog => frog.Mood == RatMood.Conga), Is.False, "Then the conga ends.");
        }

        // A frog that cannot get to its place, for example because an obstacle stands on it, does not hold the
        // dance up: once it stops getting closer, the others stop waiting for it.
        [UnityTest]
        public IEnumerator Song_Conga_AFrogThatCannotReachItsPlace_DoesNotHoldUpTheDance()
        {
            Rat[] frogs = SixFrogsAround();
            HoldStill(frogs[0]);

            yield return Play(SongNamed("Conga").Keys);
            var started = new bool[1];
            yield return WaitForTheClocks(frogs, 3f, started);

            Assert.That(started[0], Is.True, "The others start dancing soon, without the stuck frog.");
            Assert.That(frogs.All(frog => frog.Mood == RatMood.Conga), Is.True);
        }

        [UnityTest]
        public IEnumerator Song_Heart_TheClockStartsOnceEveryFrogIsInPlace()
        {
            Rat[] frogs = SixFrogsAround();
            Song heart = SongNamed("Heart");

            yield return Play(heart.Keys);
            Assert.That(frogs.All(frog => frog.Mood == RatMood.Heart && frog.IsWaitingForTheOthers), Is.True,
                "Nobody's clock runs while the frogs are still running to their places.");

            var started = new bool[1];
            yield return WaitForTheClocks(frogs, 4f, started);
            Assert.That(started[0], Is.True, "Once the heart is formed, the clock starts.");
            Assert.That(frogs.All(frog => frog.IsInPlace), Is.True, "The clock started with every frog in its place.");

            yield return new WaitForSeconds(heart.Seconds - 0.5f);
            Assert.That(frogs.All(frog => frog.Mood == RatMood.Heart), Is.True, "They hold the heart the song's full time.");

            yield return new WaitForSeconds(1f);
            Assert.That(frogs.Any(frog => frog.Mood == RatMood.Heart), Is.False, "Then the heart ends.");
        }

        [UnityTest]
        public IEnumerator Song_CrowdSurf_GathersTheRatsUnderThePiper()
        {
            ClearTheField();
            Rat[] crowd = { RatNearThePiper(0), RatNearThePiper(1), RatNearThePiper(2) };

            yield return Play(SongNamed("Crowd Surf").Keys);
            yield return new WaitForSeconds(1.5f);

            foreach (Rat rat in crowd)
            {
                Assert.That(rat.Mood, Is.EqualTo(RatMood.Carry));
                Assert.That(Vector2.Distance(rat.transform.position, Piper), Is.LessThan(1f));
            }
        }

        // The player steers the Piper, and the crowd under it goes the same way.
        [UnityTest]
        public IEnumerator Song_CrowdSurf_TheCrowdMovesWhereThePlayerSteers()
        {
            var movement = _flute.GetComponent<PiperMovement>();
            _flute.GetComponent<PiperInput>().enabled = false;
            ClearTheField();
            Rat[] crowd = { RatNearThePiper(0), RatNearThePiper(1), RatNearThePiper(2) };
            yield return Play(SongNamed("Crowd Surf").Keys);
            yield return new WaitForSeconds(1.5f);
            Vector2 start = Piper;

            movement.Direction = Vector2.left;
            yield return new WaitForSeconds(1f);

            Assert.That(Piper.x, Is.LessThan(start.x - 2f), "The Piper has to travel for this test to mean anything.");

            foreach (Rat rat in crowd)
            {
                Assert.That(Vector2.Distance(rat.transform.position, Piper), Is.LessThan(1.3f));
            }
        }

        // Once the crowd is under the Piper, the player rides it at twice the walking speed, and the crowd keeps up.
        [UnityTest]
        public IEnumerator Song_CrowdSurf_OnceTheCrowdIsUnderThePiper_ItGoesTwiceAsFast()
        {
            var movement = _flute.GetComponent<PiperMovement>();
            _flute.GetComponent<PiperInput>().enabled = false;
            ClearTheField();
            Rat[] crowd = { RatNearThePiper(0), RatNearThePiper(1), RatNearThePiper(2) };
            Song crowdSurf = SongNamed("Crowd Surf");

            Vector2 start = Piper;
            movement.Direction = Vector2.left;
            yield return new WaitForSeconds(0.25f);
            movement.Direction = Vector2.zero;
            float walked = start.x - Piper.x;

            yield return Play(crowdSurf.Keys);
            Assert.That(movement.IsCarried, Is.False, "Not until the crowd is under the Piper.");
            yield return new WaitForSeconds(1.5f);
            Assert.That(movement.IsCarried, Is.True, "The crowd is under the Piper now.");

            start = Piper;
            movement.Direction = Vector2.right;
            yield return new WaitForSeconds(0.25f);
            movement.Direction = Vector2.zero;
            float ridden = Piper.x - start.x;
            yield return new WaitForSeconds(0.3f);

            Assert.That(walked, Is.GreaterThan(0.5f), "The Piper has to walk for this test to mean anything.");
            Assert.That(ridden / walked, Is.EqualTo(2f).Within(0.3f));

            foreach (Rat rat in crowd)
            {
                Assert.That(Vector2.Distance(rat.transform.position, Piper), Is.LessThan(1.3f), $"{rat.name} keeps up.");
            }

            yield return new WaitForSeconds(crowdSurf.Seconds);
            Assert.That(movement.IsCarried, Is.False, "When the crowd lets go, the Piper walks again.");
        }

        // A disco frog spins round its own middle on the spot, with a light under it in the game's colours.
        [UnityTest]
        public IEnumerator Song_Disco_FrogsSpinOnTheSpotOverALight()
        {
            ClearTheField();
            Rat frog = RatNearThePiper();
            Transform body = frog.transform.Find("Body");
            SpriteRenderer light = frog.transform.Find("Disco Light").GetComponent<SpriteRenderer>();
            Song disco = SongNamed("Disco");
            yield return null;
            Assert.That(light.color.a, Is.LessThan(0.01f), "The light is off until the disco.");

            yield return Play(disco.Keys);
            yield return new WaitForSeconds(0.5f);
            Vector2 spot = frog.transform.position;
            float turned = body.eulerAngles.z;
            yield return new WaitForSeconds(0.2f);

            Assert.That(Vector2.Distance(frog.transform.position, spot), Is.LessThan(0.05f), "It stays on its spot.");
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(turned, body.eulerAngles.z)), Is.GreaterThan(20f), "It spins.");
            Assert.That(body.localPosition, Is.EqualTo(Vector3.zero), "It spins round its own middle.");
            Assert.That(light.color.a, Is.GreaterThan(0.3f), "Its light is on.");

            yield return new WaitForSeconds(disco.Seconds);
            Assert.That(light.color.a, Is.LessThan(0.01f), "The light goes out after the disco.");
        }

        // A balloon rat stops walking and waddling, and drifts up the screen and a little to the right.
        [UnityTest]
        public IEnumerator Song_Balloons_FloatRatsSlowlyUpAndALittleRight()
        {
            ClearTheField();
            Rat rat = RatNearThePiper();
            Transform body = rat.transform.Find("Body");

            yield return Play(SongNamed("Balloons").Keys);
            yield return null;
            Vector2 start = rat.transform.position;
            float heading = body.eulerAngles.z;

            yield return new WaitForSeconds(1.5f);
            Vector2 moved = (Vector2)rat.transform.position - start;

            Assert.That(rat.Mood, Is.EqualTo(RatMood.Float));
            Assert.That(rat.GetComponent<Collider2D>().enabled, Is.True, "It still bumps into the edge of the field.");
            Assert.That(moved.y, Is.GreaterThan(0.4f), "Upwards.");
            Assert.That(moved.x, Is.GreaterThan(0f).And.LessThan(moved.y), "A little to the right.");
            Assert.That(moved.magnitude, Is.LessThan(2f), "Slowly.");
            Assert.That(Mathf.DeltaAngle(body.eulerAngles.z, heading), Is.EqualTo(0f).Within(1f), "It stops turning to walk.");
            Assert.That(rat.GetComponent<Animator>().GetFloat("Speed"), Is.EqualTo(0f), "It stops waddling.");
        }

        [UnityTest]
        public IEnumerator Song_Balloons_NeverFloatOffTheField()
        {
            ClearTheField();
            Rat rat = RatNearThePiper();
            Vector2 top = EdgeFrom(rat.transform.position, Vector2.up);
            Place(rat, top + Vector2.down);
            _flute.transform.position = top + new Vector2(-Near, -1f);
            _flute.GetComponent<Rigidbody2D>().position = _flute.transform.position;
            yield return new WaitForFixedUpdate();

            yield return Play(SongNamed("Balloons").Keys);
            yield return new WaitForSeconds(3f);

            Assert.That(rat.Mood, Is.EqualTo(RatMood.Float));
            Assert.That(rat.transform.position.y, Is.LessThan(top.y));
        }

        [UnityTest]
        public IEnumerator FluteVoice_HoldingAHole_PlaysThatHole()
        {
            var voice = Object.FindAnyObjectByType<FluteVoice>();

            _flute.Press(4);
            yield return null;
            Assert.That(voice.KeyPlaying, Is.EqualTo(4));

            _flute.Release(4);
            yield return null;
            Assert.That(voice.KeyPlaying, Is.EqualTo(FluteSynth.Silent));
        }

        [UnityTest]
        public IEnumerator Hud_HasNoHowToPlayText()
        {
            yield return null;
            VisualElement root = Object.FindAnyObjectByType<ComboRow>().GetComponent<UIDocument>().rootVisualElement;

            Assert.That(root.Q("hint"), Is.Null);
            Assert.That(root.Query<Label>().ToList().Any(label => label.text.Contains("WASD")), Is.False);
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
        public IEnumerator FluteBar_AHeldHole_ShowsThatHolePressed()
        {
            var bar = Object.FindAnyObjectByType<FluteBar>();
            yield return null;
            Assert.That(bar.ShownPicture.name, Is.EqualTo("Flute"));

            _flute.Press(5);
            yield return null;
            Assert.That(bar.ShownPicture.name, Is.EqualTo("FluteHole5"));

            _flute.Release(5);
            yield return null;
            Assert.That(bar.ShownPicture.name, Is.EqualTo("Flute"));
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
