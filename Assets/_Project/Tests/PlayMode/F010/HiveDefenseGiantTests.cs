#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>
    /// The giant, version 2 (plan section I), in the hive defense scene through the root, as the keys would drive it: H J K L
    /// are its moves and no song is cast while it stands, W walks it away from the chase camera, the line of its keys shows
    /// when it forms and fades, and time slows for a moment as it forms and puts itself back. The camera's effects answer the
    /// giant's heavy blows and the waves.
    /// </summary>
    public sealed class HiveDefenseGiantTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const int FramesToSettle = 3;
        private const int MaxFrames = 300000;
        private const float FullFuryDamage = 1000f;
        private const float MaxColossusWaitSeconds = 2f;
        private const float MaxMoveWaitSeconds = 2f;
        private const float SwoopSeconds = 1.2f;
        private const float BetweenMovesSeconds = 0.9f;
        private const float WalkSeconds = 0.6f;
        private const float QuarterTurnDegrees = 90f;
        private const float SameWayDot = 0.95f;
        private const float LeastWalkMetres = 0.5f;
        private const float HintSeconds = 6f;
        private const float FadeMarginSeconds = 0.5f;
        private const float FastTimeScale = 5f;
        private const float WavePullSeconds = 2.5f;
        private const float HalfWay = 0.5f;

        // Holes count from 0 as H J K L.
        private const int RoarHole = 0;
        private const int PunchHole = 1;
        private const int KickHole = 2;
        private const int SlamHole = 3;

        private readonly List<ColossusAttack> _started = new List<ColossusAttack>();
        private HiveDefenseRoot _root;
        private int _songsCast;
        private float _savedTimeScale;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            _savedTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                yield return null;
            }

            _root = Object.FindAnyObjectByType<HiveDefenseRoot>();
            Assert.That(_root, Is.Not.Null);
            _started.Clear();
            _songsCast = 0;
            _root.Session.ColossusAttackStarted += attack => _started.Add(attack);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (_root != null)
            {
                _root.HoldMove(Vector2.zero);
            }

            Time.timeScale = _savedTimeScale;
            yield return null;
        }

        // Plan section I: J punches, K kicks, L slams and H roars; K K is the spin kick and L L the leap slam. None of the
        // presses reaches the songs.
        [UnityTest]
        public IEnumerator GiantKeys_WhileItStands_DoItsMovesAndCastNoSong()
        {
            yield return FormTheGiant();
            HiveDefenseSession session = _root.Session;
            session.Caster.SongCast += _ => _songsCast++;

            yield return Press(PunchHole, ColossusAttack.Punch);
            yield return Press(KickHole, ColossusAttack.Kick);
            yield return Press(SlamHole, ColossusAttack.Slam);
            yield return Press(RoarHole, ColossusAttack.Roar);

            _root.PressHole(KickHole);
            yield return null;
            _root.PressHole(KickHole);
            yield return WaitForMove(ColossusAttack.SpinKick);
            Assert.That(_started, Is.EqualTo(new[] { ColossusAttack.Kick, ColossusAttack.SpinKick }), "K K is not a kick and then a spin kick");
            _started.Clear();
            yield return WaitGameSeconds(BetweenMovesSeconds);

            while (session.SlamCooldown01 > 0f)
            {
                yield return null;
            }

            _root.PressHole(SlamHole);
            yield return null;
            _root.PressHole(SlamHole);
            yield return WaitForMove(ColossusAttack.LeapSlam);
            Assert.That(_started, Is.EqualTo(new[] { ColossusAttack.Slam, ColossusAttack.LeapSlam }), "L L is not a slam and then a leap slam");
            Assert.That(session.IsColossusLeaping, Is.True, "the leap did not take off");

            Assert.That(session.IsColossusActive, Is.True, "the giant ended during the moves");
            Assert.That(_songsCast, Is.EqualTo(0), "a song was cast while the giant stood");
            Assert.That(session.Forms.Current, Is.EqualTo(SwarmForm.Colossus));
        }

        // Plan section I: the giant walks relative to the chase camera, so W walks it the way the camera looks on the ground,
        // here after the camera has been turned a quarter turn away from the world's north.
        [UnityTest]
        public IEnumerator W_InTheChaseView_WalksTheGiantAwayFromTheCamera()
        {
            yield return FormTheGiant();
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            yield return WaitGameSeconds(SwoopSeconds);
            follow.Orbit(QuarterTurnDegrees, 0f);
            yield return null;
            yield return null;
            Vector3 away = follow.GroundForward;
            Assert.That(Mathf.Abs(Vector3.Dot(away, Vector3.forward)), Is.LessThan(1f - SameWayDot), "the turn did not turn the camera");

            HiveDefenseSession session = _root.Session;
            Vector3 before = session.ColossusPosition;
            _root.HoldMove(Vector2.up);
            yield return WaitGameSeconds(WalkSeconds);
            _root.HoldMove(Vector2.zero);

            Vector3 walked = session.ColossusPosition - before;
            walked.y = 0f;
            Assert.That(walked.magnitude, Is.GreaterThan(LeastWalkMetres), "the giant did not walk");
            Assert.That(Vector3.Dot(walked.normalized, away), Is.GreaterThan(SameWayDot), "W did not walk the giant away from the camera");
            Vector3 fromCamera = session.ColossusPosition - follow.transform.position;
            fromCamera.y = 0f;
            Assert.That(Vector3.Dot(fromCamera.normalized, away), Is.GreaterThan(0f), "the camera is not behind the giant");
        }

        // Plan section I and the owner's ask: each time the giant forms, the line of its keys shows, with the phrases the first
        // times, for a few seconds, and then fades.
        [UnityTest]
        public IEnumerator Hint_WhenTheGiantForms_ShowsItsKeysAndFades()
        {
            var hint = Object.FindAnyObjectByType<GiantHudView>();
            Assert.That(hint, Is.Not.Null, "The scene has no GiantHudView.");
            Assert.That(hint.IsHintShowing, Is.False, "the keys show before the giant");
            Assert.That(hint.IsSongbookHidden, Is.False, "the songbook is hidden before the giant");

            yield return FormTheGiant();
            yield return null;

            Assert.That(hint.IsShowing, Is.True, "the giant strip does not show");
            Assert.That(hint.IsHintShowing, Is.True, "the line of the giant's keys does not show");
            Assert.That(hint.IsPhrasesHintShowing, Is.True, "the phrases do not show the first time");
            Assert.That(hint.IsSongbookHidden, Is.True, "the songbook still covers the chase view");

            Time.timeScale = FastTimeScale;
            yield return WaitGameSeconds(HintSeconds + FadeMarginSeconds);
            Time.timeScale = 1f;

            Assert.That(_root.Session.IsColossusActive, Is.True, "the giant ended before the line faded");
            Assert.That(hint.IsShowing, Is.True, "the strip went with the line");
            Assert.That(hint.IsHintShowing, Is.False, "the line did not fade");
        }

        // The brief slow-down as the giant forms runs on its own and gives the time scale back exactly.
        [UnityTest]
        public IEnumerator SlowMotion_WhenTheGiantForms_SlowsTimeAndPutsItBack()
        {
            yield return FormTheGiant();
            Assert.That(_root.IsSlowingTime, Is.True, "time did not slow as the giant formed");
            Assert.That(Time.timeScale, Is.LessThan(1f));

            float waited = 0f;
            int frames = 0;
            while (_root.IsSlowingTime && waited < MaxColossusWaitSeconds && frames < MaxFrames)
            {
                waited += Time.unscaledDeltaTime;
                frames++;
                yield return null;
            }

            Assert.That(_root.IsSlowingTime, Is.False, "the slow-down did not end");
            Assert.That(Time.timeScale, Is.EqualTo(1f), "the time scale was not put back");
        }

        // The owner's ask for camera effects, wired by the root and the hit feel: the giant's slam lands with a push-in.
        [UnityTest]
        public IEnumerator Slam_WhenItLands_PushesTheCameraIn()
        {
            yield return FormTheGiant();
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            bool hasLanded = false;
            _root.Session.ColossusAttackLanded += attack => hasLanded |= attack == ColossusAttack.Slam;

            _root.PressHole(SlamHole);
            float waited = 0f;
            int frames = 0;
            while (!hasLanded && waited < MaxMoveWaitSeconds && frames < MaxFrames)
            {
                waited += Time.deltaTime;
                frames++;
                yield return null;
            }

            Assert.That(hasLanded, Is.True, "the slam did not land");
            yield return null;
            Assert.That(follow.PushInStrength, Is.GreaterThan(0f), "the slam did not push the camera in");
        }

        // The owner's ask for camera effects: while a wave runs the camera backs away a little.
        [UnityTest]
        public IEnumerator Wave_WhileItRuns_PullsTheCameraBack()
        {
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            Assert.That(follow.WavePullBack01, Is.EqualTo(0f).Within(1e-3f), "the camera is pulled back before the wave");

            _root.CallWave();
            yield return WaitGameSeconds(WavePullSeconds);

            Assert.That(_root.Session.Phase, Is.EqualTo(MatchPhase.Survive), "the wave did not start");
            Assert.That(follow.WavePullBack01, Is.GreaterThan(HalfWay), "the camera did not back away while the wave runs");
        }

        // Fills Fury, plays the Colossus keys and waits until the giant stands.
        private IEnumerator FormTheGiant()
        {
            HiveDefenseSession session = _root.Session;
            session.Fury.AddDamage(FullFuryDamage);
            Assert.That(session.Fury.IsFull, Is.True);
            IReadOnlyList<int> keys = ColossusKeys();
            for (int index = 0; index < keys.Count; index++)
            {
                _root.PressHole(keys[index]);
                yield return null;
            }

            float waited = 0f;
            int frames = 0;
            while (!session.IsColossusActive && waited < MaxColossusWaitSeconds && frames < MaxFrames)
            {
                waited += Time.deltaTime;
                frames++;
                yield return null;
            }

            Assert.That(session.IsColossusActive, Is.True, "The Colossus keys at full Fury did not form the Colossus.");
            _started.Clear();
        }

        // Presses one of the giant's keys and checks it starts that move alone, then waits past its phrase window and cooldown.
        private IEnumerator Press(int hole, ColossusAttack move)
        {
            _root.PressHole(hole);
            yield return WaitForMove(move);
            Assert.That(_started, Is.EqualTo(new[] { move }), "hole " + hole + " started other moves");
            _started.Clear();
            yield return WaitGameSeconds(BetweenMovesSeconds);
        }

        private IEnumerator WaitForMove(ColossusAttack move)
        {
            float waited = 0f;
            int frames = 0;
            while (!_started.Contains(move) && waited < MaxMoveWaitSeconds && frames < MaxFrames)
            {
                waited += Time.deltaTime;
                frames++;
                yield return null;
            }

            Assert.That(_started, Does.Contain(move), move + " did not start");
        }

        private IReadOnlyList<int> ColossusKeys()
        {
            SwarmSongCaster caster = _root.Session.Caster;
            for (int song = 0; song < caster.Count; song++)
            {
                if (caster[song] is ColossusSong)
                {
                    return caster[song].Song.Keys;
                }
            }

            Assert.Fail("The scene has no ColossusSong.");
            return null;
        }

        // Counts game time, not frames, because batch mode runs thousands of frames a second.
        private static IEnumerator WaitGameSeconds(float seconds)
        {
            float elapsed = 0f;
            int frames = 0;
            while (elapsed < seconds && frames < MaxFrames)
            {
                yield return null;
                elapsed += Time.deltaTime;
                frames++;
            }
        }
    }
}
#endif
