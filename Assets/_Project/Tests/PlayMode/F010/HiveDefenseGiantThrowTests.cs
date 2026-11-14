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

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>
    /// Songs-v2 W in the hive defense scene: on a giant's move, the wasps of the limb that struck leave the body on the hit,
    /// burning red, and are all back, gold again, after the throw's time; the limbs that did not strike stay whole. The swarm
    /// is read back from the GPU here, which the game never does.
    /// </summary>
    public sealed class HiveDefenseGiantThrowTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const string SamplesPath = "Assets/_Project/GameData/F010/ColossusSampleSet.asset";
        private const string ColossusPath = "Assets/_Project/GameData/F010/ColossusConfig.asset";
        private const int FramesToSettle = 3;
        private const int MaxFrames = 300000;
        private const float FullFuryDamage = 2000f;
        private const float MaxWaitSeconds = 3f;
        private const float BuiltSeconds = 2f;
        private const int PunchHole = 1;
        private const int SlamHole = 3;

        // Seconds after the hit the burst is looked at: well into the way out, before the hold.
        private const float AwayShotSeconds = 0.12f;
        private const float BackMarginSeconds = 0.25f;

        // Metres from the hand bone, at the giant's size 1, within which a wasp counts as on the arm.
        private const float ArmMetres = 1.2f;

        // Of the striking limb's wasps, at least this share must be away at the burst: about the config's share.
        private const float LeastAwayShare = 0.6f;
        private const float RedOverGreen = 2.5f;

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
        public IEnumerator Punch_TheStrikingArmsWaspsLeaveRedOnTheHit_AreBackGoldAfterTheThrow_AndTheOtherArmStays()
        {
            yield return FormTheGiant();
            yield return WaitGameSeconds(BuiltSeconds);
            HiveDefenseSession session = _root.Session;
            var motor = Object.FindAnyObjectByType<ColossusMotor>();
            var swarm = Object.FindAnyObjectByType<SwarmGpuView>();
            var samples = AssetDatabase.LoadAssetAtPath<ColossusSampleSet>(SamplesPath);
            var giant = AssetDatabase.LoadAssetAtPath<ColossusConfig>(ColossusPath);
            var states = new WaspState[session.Swarm.Count];
            float armMetres = ArmMetres * session.ColossusSizeScale;
            List<int> right = WaspsOf(samples, ColossusLimb.RightArm, session.Swarm.Count);
            List<int> left = WaspsOf(samples, ColossusLimb.LeftArm, session.Swarm.Count);
            float rightBefore = ShareNear(swarm, states, right, motor.HandPosition(ColossusHand.Right), armMetres);
            float leftBefore = ShareNear(swarm, states, left, motor.HandPosition(ColossusHand.Left), armMetres);
            int throwsBefore = swarm.ThrowsStarted;

            yield return PressAndWaitForHit(PunchHole, ColossusAttack.Punch);
            int throwsAtHit = swarm.ThrowsStarted;
            ColossusLimb struck = motor.LastThrownLimbs;
            Assert.That(struck == ColossusLimb.RightArm || struck == ColossusLimb.LeftArm, Is.True, $"a punch threw {struck}");
            bool isRight = struck == ColossusLimb.RightArm;
            List<int> striking = isRight ? right : left;
            List<int> other = isRight ? left : right;
            ColossusHand strikingHand = isRight ? ColossusHand.Right : ColossusHand.Left;
            ColossusHand otherHand = isRight ? ColossusHand.Left : ColossusHand.Right;
            float strikingBefore = isRight ? rightBefore : leftBefore;
            float otherBefore = isRight ? leftBefore : rightBefore;

            yield return WaitGameSeconds(AwayShotSeconds);
            float strikingAway = 1f - ShareNear(swarm, states, striking, motor.HandPosition(strikingHand), armMetres);
            float otherAtBurst = ShareNear(swarm, states, other, motor.HandPosition(otherHand), armMetres);
            float awayRedness = Redness(states, striking, motor.HandPosition(strikingHand), armMetres, true);

            yield return WaitGameSeconds(giant.PunchThrow.BackSeconds - AwayShotSeconds + BackMarginSeconds);
            float strikingBack = ShareNear(swarm, states, striking, motor.HandPosition(strikingHand), armMetres);
            float strikingRednessBack = Redness(states, striking, motor.HandPosition(strikingHand), armMetres, false);
            float otherRedness = Redness(states, other, motor.HandPosition(otherHand), armMetres, false);

            Assert.That(session.IsColossusActive, Is.True, "the giant ended during the test");
            Assert.That(throwsAtHit, Is.EqualTo(throwsBefore + 1), "the punch's hit threw not exactly once");
            Assert.That(strikingBefore, Is.GreaterThan(0.6f), "the striking arm was not built before the punch");
            Assert.That(strikingAway, Is.GreaterThan(giant.PunchThrow.Share * LeastAwayShare), "too few of the striking arm's wasps left on the hit");
            Assert.That(otherAtBurst, Is.GreaterThan(otherBefore * 0.85f), "the other arm came apart with the striking one");
            Assert.That(awayRedness, Is.GreaterThan(RedOverGreen), "the thrown wasps do not burn red");
            Assert.That(strikingBack, Is.GreaterThan(strikingBefore * 0.9f), "the thrown wasps were not back after the throw's time");
            Assert.That(strikingRednessBack, Is.LessThan(otherRedness * 1.3f), "the returned wasps did not turn gold again");
        }

        [UnityTest]
        public IEnumerator Slam_BothArmsWaspsLeaveOnTheHit_AndAreBackAfterTheThrow()
        {
            yield return FormTheGiant();
            yield return WaitGameSeconds(BuiltSeconds);
            HiveDefenseSession session = _root.Session;
            var motor = Object.FindAnyObjectByType<ColossusMotor>();
            var swarm = Object.FindAnyObjectByType<SwarmGpuView>();
            var samples = AssetDatabase.LoadAssetAtPath<ColossusSampleSet>(SamplesPath);
            var giant = AssetDatabase.LoadAssetAtPath<ColossusConfig>(ColossusPath);
            var states = new WaspState[session.Swarm.Count];
            float armMetres = ArmMetres * session.ColossusSizeScale;
            List<int> right = WaspsOf(samples, ColossusLimb.RightArm, session.Swarm.Count);
            List<int> left = WaspsOf(samples, ColossusLimb.LeftArm, session.Swarm.Count);
            float rightBefore = ShareNear(swarm, states, right, motor.HandPosition(ColossusHand.Right), armMetres);
            float leftBefore = ShareNear(swarm, states, left, motor.HandPosition(ColossusHand.Left), armMetres);

            yield return PressAndWaitForHit(SlamHole, ColossusAttack.Slam);
            Assert.That(motor.LastThrownLimbs, Is.EqualTo(ColossusLimb.RightArm | ColossusLimb.LeftArm));
            yield return WaitGameSeconds(AwayShotSeconds);
            float rightAway = 1f - ShareNear(swarm, states, right, motor.HandPosition(ColossusHand.Right), armMetres);
            float leftAway = 1f - ShareNear(swarm, states, left, motor.HandPosition(ColossusHand.Left), armMetres);

            yield return WaitGameSeconds(giant.SlamThrow.BackSeconds - AwayShotSeconds + BackMarginSeconds);
            float rightBack = ShareNear(swarm, states, right, motor.HandPosition(ColossusHand.Right), armMetres);
            float leftBack = ShareNear(swarm, states, left, motor.HandPosition(ColossusHand.Left), armMetres);

            Assert.That(session.IsColossusActive, Is.True, "the giant ended during the test");
            float least = giant.SlamThrow.Share * LeastAwayShare;
            Assert.That(rightAway, Is.GreaterThan(least), "too few of the right arm's wasps left on the slam");
            Assert.That(leftAway, Is.GreaterThan(least), "too few of the left arm's wasps left on the slam");
            Assert.That(rightBack, Is.GreaterThan(rightBefore * 0.9f), "the right arm's wasps were not back");
            Assert.That(leftBack, Is.GreaterThan(leftBefore * 0.9f), "the left arm's wasps were not back");
        }

        private IEnumerator PressAndWaitForHit(int hole, ColossusAttack move)
        {
            bool hasLanded = false;
            void OnLanded(ColossusAttack attack) => hasLanded |= attack == move;
            _root.Session.ColossusAttackLanded += OnLanded;
            try
            {
                _root.PressHole(hole);
                float waited = 0f;
                int frames = 0;
                while (!hasLanded && waited < MaxWaitSeconds && frames < MaxFrames)
                {
                    yield return null;
                    waited += Time.deltaTime;
                    frames++;
                }
            }
            finally
            {
                _root.Session.ColossusAttackLanded -= OnLanded;
            }

            Assert.That(hasLanded, Is.True, $"{move} did not land");
        }

        private static List<int> WaspsOf(ColossusSampleSet samples, ColossusLimb limb, int count)
        {
            Assert.That(samples, Is.Not.Null);
            var limbs = new ColossusLimb[samples.BoneCount];
            ColossusThrows.FindLimbs(samples.BoneNames, limbs);
            var wasps = new List<int>();
            for (int wasp = 0; wasp < count; wasp++)
            {
                int bone = samples.BoneIndices[wasp % samples.SampleCount];
                if (bone >= 0 && bone < limbs.Length && limbs[bone] == limb)
                {
                    wasps.Add(wasp);
                }
            }

            Assert.That(wasps.Count, Is.GreaterThan(10), $"too few wasps on the {limb}");
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

        // The mean red over green of the wasps away from the point (or near it), from the states last read back.
        private static float Redness(WaspState[] states, List<int> wasps, Vector3 point, float metres, bool isAway)
        {
            float sum = 0f;
            int count = 0;
            for (int index = 0; index < wasps.Count; index++)
            {
                WaspState state = states[wasps[index]];
                bool isNear = (state.Position - point).sqrMagnitude <= metres * metres;
                if (isNear == isAway)
                {
                    continue;
                }

                sum += state.Tint.x / Mathf.Max(state.Tint.y, 0.001f);
                count++;
            }

            return count > 0 ? sum / count : 0f;
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
