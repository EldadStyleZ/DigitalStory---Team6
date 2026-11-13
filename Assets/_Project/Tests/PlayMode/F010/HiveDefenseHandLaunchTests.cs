#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DigitalStory.Data;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>
    /// Songs-v2 R in the hive defense scene: the giant's hand launch on the GPU (the wasps of the right hand and forearm fly to
    /// the target and grow back onto the arm while the left arm stays whole), the marker where a leap will land, and the giant's
    /// HUD, which names the hands for the device in use and keeps clear of the giant in the chase view. The swarm is read back
    /// from the GPU here, which the game never does.
    /// </summary>
    public sealed class HiveDefenseHandLaunchTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const string SamplesPath = "Assets/_Project/GameData/F010/ColossusSampleSet.asset";
        private const string ColossusPath = "Assets/_Project/GameData/F010/ColossusConfig.asset";
        private const int FramesToSettle = 3;
        private const int MaxFrames = 300000;
        private const float FullFuryDamage = 1000f;
        private const float MaxWaitSeconds = 3f;
        private const float BuiltSeconds = 2f;
        private const float SwoopSeconds = 1.5f;
        private const float RegrownMarginSeconds = 0.4f;
        private const float MissingShare = 0.3f;
        private const int SlamHole = 3;

        // Metres from the hand bone, at the giant's size 1, within which a wasp counts as on the arm: the forearm and the hand.
        private const float ArmMetres = 1.2f;

        private HiveDefenseRoot _root;
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
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = _savedTimeScale;
            yield return null;
        }

        [UnityTest]
        public IEnumerator RightHand_ItsWaspsFlyToTheTarget_TheArmIsMissing_ThenGrowsBack_AndTheLeftArmStays()
        {
            yield return FormTheGiant();
            yield return WaitGameSeconds(BuiltSeconds);
            HiveDefenseSession session = _root.Session;
            var motor = Object.FindAnyObjectByType<ColossusMotor>();
            var swarm = Object.FindAnyObjectByType<SwarmGpuView>();
            var samples = AssetDatabase.LoadAssetAtPath<ColossusSampleSet>(SamplesPath);
            var giant = AssetDatabase.LoadAssetAtPath<ColossusConfig>(ColossusPath);
            Assert.That(samples, Is.Not.Null);
            Assert.That(giant, Is.Not.Null);
            List<int> right = WaspsOf(samples, ColossusHand.Right, session.Swarm.Count);
            List<int> left = WaspsOf(samples, ColossusHand.Left, session.Swarm.Count);
            Assert.That(right.Count, Is.GreaterThan(10), "too few wasps on the right arm to see a fist");
            var states = new WaspState[session.Swarm.Count];
            float armMetres = ArmMetres * session.ColossusSizeScale;

            float rightBefore = ShareNear(swarm, states, right, motor.HandPosition(ColossusHand.Right), armMetres);
            float leftBefore = ShareNear(swarm, states, left, motor.HandPosition(ColossusHand.Left), armMetres);
            float restReach = GroundDistance(motor.HandPosition(ColossusHand.Right), session.ColossusPosition);
            _root.PressHand(ColossusHand.Right);
            float mostReach = restReach;
            int frames = 0;
            while (session.FistPhaseOf(ColossusHand.Right) != FistPhase.Regrow && frames < MaxFrames)
            {
                mostReach = Mathf.Max(mostReach, GroundDistance(motor.HandPosition(ColossusHand.Right), session.ColossusPosition));
                frames++;
                yield return null;
            }

            yield return null;
            Vector3 target = session.FistTarget(ColossusHand.Right);
            Vector3 atHit = MeanOf(swarm, states, right);
            float rightAtHit = ShareNear(swarm, states, right, motor.HandPosition(ColossusHand.Right), armMetres);
            float leftAtHit = ShareNear(swarm, states, left, motor.HandPosition(ColossusHand.Left), armMetres);

            yield return WaitGameSeconds(giant.FistRegrowSeconds * MissingShare);
            float rightMissing = ShareNear(swarm, states, right, motor.HandPosition(ColossusHand.Right), armMetres);

            frames = 0;
            while (session.FistPhaseOf(ColossusHand.Right) != FistPhase.Ready && frames < MaxFrames)
            {
                frames++;
                yield return null;
            }

            yield return WaitGameSeconds(RegrownMarginSeconds);
            float rightRegrown = ShareNear(swarm, states, right, motor.HandPosition(ColossusHand.Right), armMetres);

            Assert.That(session.IsColossusActive, Is.True, "the giant ended during the test");
            Assert.That(motor.HasFistTriggers, Is.True, "the Colossus controller has no fist states");
            Assert.That(mostReach - restReach, Is.GreaterThan(0.1f), "the arm did not thrust forward");
            Assert.That(rightBefore, Is.GreaterThan(0.6f), "the right arm was not built before the launch");
            Assert.That(GroundDistance(atHit, target), Is.LessThan(giant.FistRadius * session.ColossusSizeScale), "the fist did not reach its target");
            Assert.That(rightAtHit, Is.LessThan(0.1f), "the right arm's wasps were not all in the fist");
            Assert.That(leftAtHit, Is.GreaterThan(leftBefore * 0.8f), "the left arm came apart with the right");
            Assert.That(rightMissing, Is.LessThan(rightBefore * 0.5f), "the right arm was not missing while it grew back");
            Assert.That(rightRegrown, Is.GreaterThan(rightBefore * 0.85f), "the right arm did not grow back");
        }

        // Songs-v2 R, small fix 1: a ring marks where the leap will land from its take-off until it lands.
        [UnityTest]
        public IEnumerator LeapMarker_FromTakeOffUntilTheLanding_MarksTheLandingSpot()
        {
            yield return FormTheGiant();
            HiveDefenseSession session = _root.Session;
            var motor = Object.FindAnyObjectByType<ColossusMotor>();
            Assert.That(motor.IsLeapMarkerShowing, Is.False, "the marker shows before any leap");

            _root.PressHole(SlamHole);
            yield return null;
            _root.PressHole(SlamHole);
            int frames = 0;
            while (!session.IsColossusLeaping && frames < MaxFrames)
            {
                frames++;
                yield return null;
            }

            Assert.That(session.IsColossusLeaping, Is.True, "the leap did not take off");
            yield return null;
            bool showedInTheAir = motor.IsLeapMarkerShowing;
            Vector3 marker = motor.LeapMarkerPosition;
            Vector3 landing = session.ColossusLeapTarget;
            frames = 0;
            while (session.IsColossusLeaping && frames < MaxFrames)
            {
                frames++;
                yield return null;
            }

            yield return null;

            Assert.That(showedInTheAir, Is.True, "no marker while the giant was in the air");
            Assert.That(marker.x, Is.EqualTo(landing.x).Within(0.01f));
            Assert.That(marker.z, Is.EqualTo(landing.z).Within(0.01f));
            Assert.That(motor.IsLeapMarkerShowing, Is.False, "the marker stayed after the landing");
        }

        // Songs-v2 R: the hint line and the cooldown marks name the hands by the device in use.
        [UnityTest]
        public IEnumerator GiantHud_NamesTheHands_RTAndLTOnAPad_TheMouseAndKeysOtherwise()
        {
            yield return FormTheGiant();
            yield return null;
            var hud = Object.FindAnyObjectByType<HiveDefenseHudView>();
            var giantHud = Object.FindAnyObjectByType<GiantHudView>();

            hud.ShowControls(ControlDevice.Gamepad);
            string padLine = giantHud.HandsText;
            string padMark = giantHud.RightMarkText;
            hud.ShowControls(ControlDevice.KeyboardMouse);

            Assert.That(giantHud.IsHandsHintShowing, Is.True, "the hands' line does not show as the giant forms");
            Assert.That(padLine, Does.Contain("RT").And.Contain("LT"));
            Assert.That(padMark, Does.StartWith("RT"));
            Assert.That(giantHud.HandsText, Does.Contain("Right click").And.Contain("Space").And.Contain("Shift"));
            Assert.That(giantHud.RightMarkText, Does.StartWith("RMB"));
        }

        // Songs-v2 R, small fix 2: in the chase view the time ring with its marks, and the "New song" toast as the giant is
        // first cast, keep clear of the giant.
        [UnityTest]
        public IEnumerator GiantHud_InTheChaseView_CoversNotTheGiant()
        {
            yield return FormTheGiant();
            yield return WaitGameSeconds(SwoopSeconds);
            HiveDefenseSession session = _root.Session;
            var giantHud = Object.FindAnyObjectByType<GiantHudView>();
            var giant = AssetDatabase.LoadAssetAtPath<ColossusConfig>(ColossusPath);
            var hud = Object.FindAnyObjectByType<HiveDefenseHudView>();
            VisualElement ui = hud.GetComponent<UIDocument>().rootVisualElement;
            Camera camera = Camera.main;
            Rect body = GiantOnPanel(ui.panel, camera, session.ColossusPosition, giant.Height * session.ColossusSizeScale);
            Rect strip = ui.Q("giant-strip").worldBound;
            Label message = ui.Q<Label>("message-line");

            Assert.That(giantHud.IsShowing, Is.True);
            Assert.That(giantHud.IsBottomMoved, Is.True, "the bottom lines did not move aside");
            Assert.That(body.width, Is.GreaterThan(1f), "the giant is not on the screen");
            Assert.That(strip.Overlaps(body), Is.False, $"the time ring {strip} covers the giant {body}");
            if (!string.IsNullOrEmpty(message.text) && message.resolvedStyle.opacity > 0f)
            {
                Assert.That(message.worldBound.Overlaps(body), Is.False, $"the toast {message.worldBound} covers the giant {body}");
            }
        }

        // The giant from its feet to its head and a quarter of its height to each side, as the HUD's panel sees it.
        private static Rect GiantOnPanel(IPanel panel, Camera camera, Vector3 feet, float height)
        {
            Vector3 side = camera.transform.right;
            side.y = 0f;
            side = side.normalized * (height * 0.25f);
            var corners = new[]
            {
                feet - side, feet + side, feet + (Vector3.up * height) - side, feet + (Vector3.up * height) + side,
            };

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int index = 0; index < corners.Length; index++)
            {
                Vector2 point = RuntimePanelUtils.CameraTransformWorldToPanel(panel, corners[index], camera);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static List<int> WaspsOf(ColossusSampleSet samples, ColossusHand hand, int count)
        {
            Assert.That(ColossusArmBones.Find(samples.BoneNames, hand, out int first, out int last, out _), Is.True);
            var wasps = new List<int>();
            for (int wasp = 0; wasp < count; wasp++)
            {
                int bone = samples.BoneIndices[wasp % samples.SampleCount];
                if (bone >= first && bone <= last)
                {
                    wasps.Add(wasp);
                }
            }

            return wasps;
        }

        private static float ShareNear(SwarmGpuView swarm, WaspState[] states, List<int> wasps, Vector3 point, float metres)
        {
            swarm.WaspStates.GetData(states, 0, 0, states.Length);
            int near = 0;
            for (int index = 0; index < wasps.Count; index++)
            {
                if ((states[wasps[index]].Position - point).sqrMagnitude <= metres * metres)
                {
                    near++;
                }
            }

            return wasps.Count > 0 ? near / (float)wasps.Count : 0f;
        }

        private static Vector3 MeanOf(SwarmGpuView swarm, WaspState[] states, List<int> wasps)
        {
            swarm.WaspStates.GetData(states, 0, 0, states.Length);
            Vector3 sum = Vector3.zero;
            for (int index = 0; index < wasps.Count; index++)
            {
                sum += states[wasps[index]].Position;
            }

            return wasps.Count > 0 ? sum / wasps.Count : Vector3.zero;
        }

        private static float GroundDistance(Vector3 a, Vector3 b)
        {
            Vector3 offset = a - b;
            offset.y = 0f;
            return offset.magnitude;
        }

        // Fills Fury, plays the Colossus keys and waits until the giant stands.
        private IEnumerator FormTheGiant()
        {
            HiveDefenseSession session = _root.Session;
            session.Fury.AddDamage(FullFuryDamage);
            IReadOnlyList<int> keys = ColossusKeys();
            for (int index = 0; index < keys.Count; index++)
            {
                _root.PressHole(keys[index]);
                yield return null;
            }

            float waited = 0f;
            int frames = 0;
            while (!session.IsColossusActive && waited < MaxWaitSeconds && frames < MaxFrames)
            {
                waited += Time.deltaTime;
                frames++;
                yield return null;
            }

            Assert.That(session.IsColossusActive, Is.True, "The Colossus keys at full Fury did not form the Colossus.");
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

        // 48 bytes, WaspDynamic in WaspSwarm.compute.
        [StructLayout(LayoutKind.Sequential)]
        private struct WaspState
        {
            public Vector3 Position;
            public float State;
            public Vector3 Velocity;
            public float StateTime;
            public Vector4 Tint;
        }
    }
}
#endif
