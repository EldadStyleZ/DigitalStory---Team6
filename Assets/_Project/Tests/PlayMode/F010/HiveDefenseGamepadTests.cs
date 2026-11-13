#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>
    /// Plays the hive defense scene with a virtual controller, through the Input System's test fixture, which swaps the real
    /// devices out for the test and puts them back after it: the face buttons play the notes (South is J), the left stick walks
    /// the core, and the HUD names the controller's buttons once it is used and the keys again once a key is. The Input Manager
    /// cannot fake a key, so the key stands in through <see cref="HiveDefenseInput.RecordUse"/>.
    /// </summary>
    public sealed class HiveDefenseGamepadTests : InputTestFixture
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const int FramesToSettle = 3;
        private const int SouthHole = 1;
        private const int MaxWalkFrames = 120;
        private const float WalkFrameSeconds = 1f / 60f;
        private const float LeastWalkMetres = 0.5f;
        private const float SameWayDot = 0.9f;
        private const int MaxFramesToEnd = 60;
        private const int MaxFramesToRestart = 120;

        // The loss rumble lasts 0.9 s (GamepadConfig.LossPulse): it must still run well into that, and be over a while after.
        private const float LeastLossRumbleSeconds = 0.6f;
        private const float LongestLossRumbleSeconds = 1.6f;

        private Gamepad _pad;
        private HiveDefenseRoot _root;
        private HiveDefenseInput _input;

        /// <summary>Removes the virtual controller before the fixture puts the real devices back.</summary>
        public override void TearDown()
        {
            if (_pad != null && _pad.added)
            {
                InputSystem.RemoveDevice(_pad);
            }

            _pad = null;
            Time.captureDeltaTime = 0f;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator PadFaceButtons_TheStingsKeysFromSouth_CastTheSting()
        {
            yield return LoadSceneWithPad();
            IReadOnlyList<int> sting = KeysOfSting();
            var played = new List<int>();
            _input.HolePressed += played.Add;

            for (int index = 0; index < sting.Count; index++)
            {
                yield return PressAndRelease(ButtonFor(sting[index]));
            }

            yield return null; // The last note is read on the next tick.
            yield return null;
            _input.HolePressed -= played.Add;

            Assert.That(sting[0], Is.EqualTo(SouthHole), "the Sting no longer starts on the South button's note");
            Assert.That(HiveDefenseInput.HoleName(SouthHole), Is.EqualTo("J"));
            Assert.That(played, Is.EqualTo(sting), "each button press played its note exactly once");
            Assert.That(_root.Session.Forms.Slot(SongSlot.Strike).Form, Is.EqualTo(SwarmForm.Sting), "the pad did not cast the Sting");
        }

        [UnityTest]
        public IEnumerator PadLeftStick_LeanedUp_WalksTheCoreAwayFromTheCamera()
        {
            yield return LoadSceneWithPad();
            var core = Object.FindAnyObjectByType<SwarmCoreMotor>();
            var camera = Object.FindAnyObjectByType<FollowCamera>();
            Vector3 away = camera.GroundForward;
            Vector3 start = core.Position;

            // Batch mode runs thousands of frames a second, and a CharacterController ignores a step shorter than its least move
            // distance, so the walk runs at a steady 60 frames a second of game time, as a player's would.
            Time.captureDeltaTime = WalkFrameSeconds;
            Set(_pad.leftStick, new Vector2(0f, 1f));
            Vector3 walked = Vector3.zero;
            for (int frame = 0; frame < MaxWalkFrames && walked.magnitude < LeastWalkMetres; frame++)
            {
                yield return null;
                walked = core.Position - start;
                walked.y = 0f;
            }

            bool isLeaning = _input.LocalMove.y > 0f;
            Set(_pad.leftStick, Vector2.zero);
            Time.captureDeltaTime = 0f;

            Assert.That(isLeaning, Is.True, "the stick did not reach the move");
            Assert.That(walked.magnitude, Is.GreaterThan(LeastWalkMetres), "the core did not walk");
            Assert.That(Vector3.Dot(walked.normalized, away), Is.GreaterThan(SameWayDot), "the core walked the wrong way");
            Assert.That(_input.LastDevice, Is.EqualTo(ControlDevice.Gamepad));
        }

        [UnityTest]
        public IEnumerator PadUsed_ThenAKey_HudNamesThePadThenTheKeysAndStartRestarts()
        {
            yield return LoadSceneWithPad();
            var hud = Object.FindAnyObjectByType<HiveDefenseHudView>();
            VisualElement ui = hud.GetComponent<UIDocument>().rootVisualElement;
            Label hint = ui.Q<Label>("phase-hint");
            Label lesson = ui.Q<Label>("prompt-line");
            string keyHint = hint.text;
            string keyLesson = lesson.text;
            Assert.That(_input.LastDevice, Is.EqualTo(ControlDevice.KeyboardMouse));
            Assert.That(keyHint, Does.Contain("Enter"));

            yield return PressAndRelease(_pad.buttonSouth);
            yield return null;

            Label note = (Label)ui.Q<VisualElement>("notes")[0];
            VisualElement diamond = note.Q(className: "pad-diamond");
            Assert.That(_input.LastDevice, Is.EqualTo(ControlDevice.Gamepad));
            Assert.That(hud.ShownDevice, Is.EqualTo(ControlDevice.Gamepad));
            Assert.That(hint.text, Does.Contain("Start").And.Not.Contain("Enter"));
            Assert.That(note.text, Is.Empty, "the note still shows its letter");
            Assert.That(diamond, Is.Not.Null, "the note shows no button symbol");
            Assert.That(diamond.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(diamond.Q(className: "pad-dot--south").ClassListContains("pad-dot--on"), Is.True, "J is not the South dot");
            if (keyLesson.Contains("W A S D"))
            {
                Assert.That(lesson.text, Does.Contain("left stick"));
            }

            _input.RecordUse(true, false); // A key, which the Input Manager cannot fake.

            Assert.That(hud.ShownDevice, Is.EqualTo(ControlDevice.KeyboardMouse));
            Assert.That(hint.text, Is.EqualTo(keyHint));
            Assert.That(lesson.text, Is.EqualTo(keyLesson));
            Assert.That(note.text, Is.EqualTo("J"));
            Assert.That(diamond.style.display.value, Is.EqualTo(DisplayStyle.None));

            // Back on the pad, lose the match: the end screen names Start, and Start plays again.
            yield return PressAndRelease(_pad.buttonSouth);
            HiveDefenseSession session = _root.Session;
            for (int index = 0; index < session.Hives.Count; index++)
            {
                session.Hives[index].Damage(session.Hives[index].MaxHealth);
            }

            for (int frame = 0; frame < MaxFramesToEnd && session.State == HiveDefenseState.Playing; frame++)
            {
                yield return null;
            }

            var endScreen = Object.FindAnyObjectByType<EndScreenView>();
            Assert.That(session.State, Is.EqualTo(HiveDefenseState.Lost), "the match did not end");
            Assert.That(endScreen.IsShowing, Is.True);
            Assert.That(ui.Q<Label>("end-hint").text, Does.Contain("Start").And.Not.Contain(" R "));

            HiveDefenseRoot ended = _root;
            Press(_pad.startButton);
            HiveDefenseRoot restarted = null;
            for (int frame = 0; frame < MaxFramesToRestart && restarted == null; frame++)
            {
                yield return null;
                HiveDefenseRoot found = Object.FindAnyObjectByType<HiveDefenseRoot>();
                restarted = found != null && found != ended && found.Session != null ? found : null;
            }

            yield return null;
            Release(_pad.startButton);
            yield return null;

            Assert.That(restarted, Is.Not.Null, "Start on the end screen did not play again");
            Assert.That(restarted.Session.State, Is.EqualTo(HiveDefenseState.Playing));
            Assert.That(restarted.Session.Phase, Is.EqualTo(MatchPhase.Farm), "the held Start also called the wave early");
        }

        // Songs-v2 R, small fix 3: a match restarted from the pad still names the pad's buttons before it is touched again.
        [UnityTest]
        public IEnumerator Restart_FromThePad_TheNewMatchStillNamesThePad()
        {
            yield return LoadSceneWithPad();
            yield return PressAndRelease(_pad.buttonSouth);
            Assert.That(_input.LastDevice, Is.EqualTo(ControlDevice.Gamepad));
            yield return LoseTheMatch();

            HiveDefenseRoot ended = _root;
            Press(_pad.startButton);
            yield return null;
            Release(_pad.startButton);
            HiveDefenseRoot restarted = null;
            for (int frame = 0; frame < MaxFramesToRestart && restarted == null; frame++)
            {
                yield return null;
                HiveDefenseRoot found = Object.FindAnyObjectByType<HiveDefenseRoot>();
                restarted = found != null && found != ended && found.Session != null ? found : null;
            }

            yield return null;
            yield return null;
            var input = Object.FindAnyObjectByType<HiveDefenseInput>();
            var hud = Object.FindAnyObjectByType<HiveDefenseHudView>();

            Assert.That(restarted, Is.Not.Null, "Start on the end screen did not play again");
            Assert.That(input.LastDevice, Is.EqualTo(ControlDevice.Gamepad), "the new match forgot the pad");
            Assert.That(hud.ShownDevice, Is.EqualTo(ControlDevice.Gamepad), "the new match's HUD names the keys");
        }

        // Songs-v2 R, small fix 4: the last hive's long, low rumble starts on the tick the match ends, and plays out.
        [UnityTest]
        public IEnumerator LastHiveFalling_ItsRumblePlaysOutAfterTheMatchEnds_ThenStops()
        {
            yield return LoadSceneWithPad();
            yield return PressAndRelease(_pad.buttonSouth);
            var rumble = Object.FindAnyObjectByType<GamepadRumbleView>();
            Assert.That(rumble, Is.Not.Null, "the scene has no GamepadRumbleView");
            yield return LoseTheMatch();
            yield return null;
            float lowAtTheEnd = rumble.Speeds.Low;

            float started = Time.realtimeSinceStartup;
            float stillAt = -1f;
            while (Time.realtimeSinceStartup - started < LongestLossRumbleSeconds)
            {
                yield return null;
                if (stillAt < 0f && rumble.Speeds.IsStill)
                {
                    stillAt = Time.realtimeSinceStartup - started;
                }
            }

            Assert.That(lowAtTheEnd, Is.GreaterThan(0.5f), "the last hive's rumble was cut as the match ended");
            Assert.That(stillAt, Is.GreaterThan(LeastLossRumbleSeconds), "the rumble stopped before it played out");
            Assert.That(rumble.Speeds.IsStill, Is.True, "the rumble did not stop after it played out");
        }

        // Destroys every hive at once, on one tick, and waits for the match to end.
        private IEnumerator LoseTheMatch()
        {
            HiveDefenseSession session = _root.Session;
            for (int index = 0; index < session.Hives.Count; index++)
            {
                session.Hives[index].Damage(session.Hives[index].MaxHealth);
            }

            for (int frame = 0; frame < MaxFramesToEnd && session.State == HiveDefenseState.Playing; frame++)
            {
                yield return null;
            }

            Assert.That(session.State, Is.EqualTo(HiveDefenseState.Lost), "the match did not end");
        }

        private IEnumerator LoadSceneWithPad()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            _pad = InputSystem.AddDevice<Gamepad>();
            _pad.MakeCurrent();
            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                yield return null;
            }

            _root = Object.FindAnyObjectByType<HiveDefenseRoot>();
            _input = Object.FindAnyObjectByType<HiveDefenseInput>();
            Assert.That(_root, Is.Not.Null, "the scene has no root");
            Assert.That(_input, Is.Not.Null, "the scene has no input");
        }

        // One frame held, so the input sees the press, then one frame released.
        private IEnumerator PressAndRelease(ButtonControl button)
        {
            Press(button);
            yield return null;
            Release(button);
            yield return null;
        }

        // The face buttons in the holes' order: West H, South J, East K, North L.
        private ButtonControl ButtonFor(int hole)
        {
            ButtonControl[] buttons = { _pad.buttonWest, _pad.buttonSouth, _pad.buttonEast, _pad.buttonNorth };
            return buttons[hole];
        }

        private IReadOnlyList<int> KeysOfSting()
        {
            SwarmSongCaster caster = _root.Session.Caster;
            for (int song = 0; song < caster.Count; song++)
            {
                if (caster[song] is StingSong sting)
                {
                    return sting.Song.Keys;
                }
            }

            Assert.Fail("the scene has no Sting");
            return null;
        }
    }
}
#endif
