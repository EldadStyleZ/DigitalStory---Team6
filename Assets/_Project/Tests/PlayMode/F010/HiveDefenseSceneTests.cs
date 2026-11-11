#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
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
    /// Loads the hive defense scene and checks that it starts, draws the swarm, answers the flute, keeps the core inside the
    /// giant, the camera on the core and the core in the play area and out of the hives, and animates the beekeepers. Every song is played with the keys on its own asset, so a change of keys never
    /// breaks these tests.
    /// </summary>
    public sealed class HiveDefenseSceneTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const string SwarmConfigPath = "Assets/_Project/GameData/F010/SwarmConfig.asset";
        private const string ColossusConfigPath = "Assets/_Project/GameData/F010/ColossusConfig.asset";
        private const string CoreMoveConfigPath = "Assets/_Project/GameData/F010/CoreMoveConfig.asset";
        private const string GroundName = "Ground";
        private const string SceneryName = "Scenery";
        private const int FramesToSettle = 3;
        private const int FramesToRun = 60;
        private const int StandingHives = 2;
        private const int SongsKnownAtStart = 1;
        private const float FastTimeScale = 5f;
        private const int MaxFramesForWave = 300000;
        private const float MaxWaveSeconds = 40f;
        private const int FramesBetweenNotes = 1;
        private const string LegBone = "LeftUpperLeg";
        private const float WatchSeconds = 0.7f;
        private const float SmallestSwingDegrees = 10f;
        private const int SoundedHole = 3;
        private const float FullFuryDamage = 1000f;
        private const float MaxColossusWaitSeconds = 2f;
        private const float PushSeconds = 0.5f;
        private const float SamePlaceMetres = 0.01f;
        private const float CameraBlendSeconds = 2.5f;
        private const float StepBackMetres = 2f;
        private const float PastEdgeMetres = 6f;
        private const float FallenMetres = 0.5f;
        private const float ShortWayOutMetres = 2f;
        private const float WalkSeconds = 0.2f;
        private const float SameWayDot = 0.95f;
        private const float WallHalfSizeMetres = 0.8f;
        private const float WallInsideEdgeMetres = 2f;

        // The controller rests its skin width away from what it touches, so a capsule this much thinner than the core's
        // overlaps only what the core is really inside.
        private const float TouchingMetres = 0.01f;
        private const int GuardSlot = (int)SongSlot.Guard;
        private const int StrikeSlot = (int)SongSlot.Strike;

        private HiveDefenseRoot _root;
        private SwarmConfig _swarmConfig;
        private float _savedTimeScale;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            _savedTimeScale = Time.timeScale;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));

            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                yield return null;
            }

            _root = Object.FindAnyObjectByType<HiveDefenseRoot>();
            _swarmConfig = AssetDatabase.LoadAssetAtPath<SwarmConfig>(SwarmConfigPath);
            Assert.That(_swarmConfig, Is.Not.Null, "There is no SwarmConfig at " + SwarmConfigPath + ". Rebuild the scene.");
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = _savedTimeScale;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Scene_RunsSixtyFrames_LogsNoError()
        {
            for (int frame = 0; frame < FramesToRun; frame++)
            {
                yield return null;
            }

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Scene_OnLoad_HasAPlayingSessionWithBothHives()
        {
            Assert.That(_root, Is.Not.Null);
            Assert.That(_root.Session, Is.Not.Null);
            Assert.That(_root.Session.State, Is.EqualTo(HiveDefenseState.Playing));
            Assert.That(_root.Session.Hives.StandingCount, Is.EqualTo(StandingHives));
            Assert.That(_root.Session.Swarm.Count, Is.EqualTo(_swarmConfig.StartingWasps));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Scene_OnLoad_KnowsOnlySting()
        {
            SongDiscovery discovery = _root.Session.Discovery;
            SwarmSongCaster caster = _root.Session.Caster;
            for (int song = 0; song < caster.Count; song++)
            {
                Assert.That(discovery.IsKnown(song), Is.EqualTo(caster[song] is StingSong), caster[song].Song.Title);
            }

            Assert.That(discovery.KnownCount, Is.EqualTo(SongsKnownAtStart));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PressHole_WithTheDomeKeys_MakesTheDomeKnown()
        {
            int dome = SongIndex<DomeSong>();
            Assert.That(_root.Session.Discovery.IsKnown(dome), Is.False, "The Dome is known before it is played.");

            yield return Play(KeysOf<DomeSong>());

            Assert.That(_root.Session.Discovery.IsKnown(dome), Is.True);
        }

        [UnityTest]
        public IEnumerator PressHole_WithTheDomeKeys_ColoursTheGuardSquadInTheDefenseColour()
        {
            SwarmGpuView view = Object.FindAnyObjectByType<SwarmGpuView>();

            yield return Play(KeysOf<DomeSong>());

            Assert.That(_root.Session.Forms.Slot(SongSlot.Guard).Form, Is.EqualTo(SwarmForm.Dome));
            Assert.That(view.IsSlotLive(GuardSlot), Is.True);
            Assert.That(view.SlotColor(GuardSlot), Is.EqualTo(_swarmConfig.DefenseColor));
            Assert.That(_root.Session.Forms.Current, Is.EqualTo(SwarmForm.River), "the rest of the swarm stays the river");
        }

        // Plan section G, M1 is done when the Dome and the Sting run together in Play Mode.
        [UnityTest]
        public IEnumerator PressHole_TheDomeThenTheSting_RunBothAtOnceInTheirOwnColours()
        {
            SwarmGpuView view = Object.FindAnyObjectByType<SwarmGpuView>();

            yield return Play(KeysOf<DomeSong>());
            yield return Play(KeysOf<StingSong>());

            SwarmFormMachine forms = _root.Session.Forms;
            Assert.That(forms.Slot(SongSlot.Guard).Form, Is.EqualTo(SwarmForm.Dome));
            Assert.That(forms.Slot(SongSlot.Strike).Form, Is.EqualTo(SwarmForm.Sting));
            Assert.That(forms.IsDomed, Is.True);
            Assert.That(forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(view.IsSlotLive(GuardSlot), Is.True, "the view does not draw the Dome");
            Assert.That(view.IsSlotLive(StrikeSlot), Is.True, "the view does not draw the Sting");
            Assert.That(view.SlotColor(GuardSlot), Is.EqualTo(_swarmConfig.DefenseColor));
            Assert.That(view.SlotColor(StrikeSlot), Is.EqualTo(_swarmConfig.AttackColor));
        }

        [UnityTest]
        public IEnumerator PressHole_ASecondAttack_ReplacesTheFirstWhileTheDomeStays()
        {
            SwarmGpuView view = Object.FindAnyObjectByType<SwarmGpuView>();
            yield return Play(KeysOf<DomeSong>());
            yield return Play(KeysOf<StingSong>());
            Assert.That(_root.Session.Forms.Slot(SongSlot.Strike).Form, Is.EqualTo(SwarmForm.Sting), "the Sting did not start");

            yield return Play(KeysOf<TornadoSong>());

            SwarmFormMachine forms = _root.Session.Forms;
            Assert.That(forms.Slot(SongSlot.Strike).Form, Is.EqualTo(SwarmForm.Tornado));
            Assert.That(forms.Slot(SongSlot.Guard).Form, Is.EqualTo(SwarmForm.Dome));
            Assert.That(forms.IsDomed, Is.True);
            Assert.That(view.SlotColor(GuardSlot), Is.EqualTo(_swarmConfig.DefenseColor));
            Assert.That(view.SlotColor(StrikeSlot), Is.EqualTo(_swarmConfig.AttackColor));
        }

        [UnityTest]
        public IEnumerator SwarmView_RunsAfterStart_AndStopsWhenDisabled()
        {
            SwarmGpuView view = Object.FindAnyObjectByType<SwarmGpuView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.IsRunning, Is.True, "The buffers are made when the scene starts.");

            view.gameObject.SetActive(false);
            yield return null;

            Assert.That(view.IsRunning, Is.False, "The buffers are released when the object is disabled.");
        }

        [UnityTest]
        public IEnumerator PressHole_WithTheDomeKeys_FormsTheDome()
        {
            yield return Play(KeysOf<DomeSong>());

            Assert.That(_root.Session.Forms.Slot(SongSlot.Guard).Form, Is.EqualTo(SwarmForm.Dome));
        }

        // Plan section A: an aimed song casts even with nobody on the field, and strikes ahead of the core.
        [UnityTest]
        public IEnumerator PressHole_WithTheMeteorKeys_FormsTheMeteor()
        {
            Assert.That(_root.Session.ActiveBeekeeperCount, Is.EqualTo(0), "a beekeeper came before the test played");

            yield return Play(KeysOf<MeteorSong>());

            Assert.That(_root.Session.Forms.Slot(SongSlot.Strike).Form, Is.EqualTo(SwarmForm.Meteor));
        }

        [UnityTest]
        public IEnumerator PressHole_OnAHole_SoundsThatHoleOnTheFluteVoice()
        {
            SwarmFluteVoice voice = Object.FindAnyObjectByType<SwarmFluteVoice>();
            Assert.That(voice, Is.Not.Null, "The scene has no SwarmFluteVoice.");

            _root.PressHole(SoundedHole);

            Assert.That(voice.HolePlaying, Is.EqualTo(SoundedHole));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Scene_PastTheFirstWaveDelay_HasAnActiveBeekeeper()
        {
            Time.timeScale = FastTimeScale;

            // Batch mode runs thousands of frames a second, so count game time, not frames.
            int frames = 0;
            float elapsed = 0f;
            while (_root.Session.ActiveBeekeeperCount < 1 && elapsed < MaxWaveSeconds && frames < MaxFramesForWave)
            {
                elapsed += Time.deltaTime;
                frames++;
                yield return null;
            }

            Assert.That(_root.Session.ActiveBeekeeperCount, Is.GreaterThanOrEqualTo(1));
        }

        // Plan section K: there is no Piper. The swarm's core is an invisible point with a CharacterController.
        [UnityTest]
        public IEnumerator Core_IsACharacterControllerWithNothingToSee()
        {
            var motor = Object.FindAnyObjectByType<SwarmCoreMotor>();
            Assert.That(motor, Is.Not.Null, "The scene has no SwarmCoreMotor.");
            Assert.That(motor.GetComponent<CharacterController>(), Is.Not.Null);
            Assert.That(motor.GetComponentsInChildren<Renderer>(true), Is.Empty, "the core has no model");
            Assert.That(motor.GetComponentsInChildren<Animator>(true), Is.Empty, "the core has no Animator");
            yield return null;
        }

        // Plan section K: the core rides inside the giant. A push on the core while the giant stands is undone on the next
        // frame, because the root puts the core where the session says it is.
        [UnityTest]
        public IEnumerator Core_WhileTheColossusStands_RidesInsideTheGiant()
        {
            var motor = Object.FindAnyObjectByType<SwarmCoreMotor>();
            HiveDefenseSession session = _root.Session;
            yield return FormTheColossus();

            Assert.That(session.IsCoreRooted, Is.False);

            motor.Move(Vector3.right, 1f, false, PushSeconds);
            Assert.That(GroundDistance(motor.Position, session.ColossusPosition), Is.GreaterThan(SamePlaceMetres), "the push moved the core");
            yield return null;

            Assert.That(GroundDistance(motor.Position, session.ColossusPosition), Is.LessThan(SamePlaceMetres));
        }

        // Plan section K: the camera always follows the core, which rides inside the giant. While the giant stands the
        // camera only steps back to the giant's distance, and it comes forward again when the giant ends, here because the
        // swarm collapsed.
        [UnityTest]
        public IEnumerator Camera_WhileTheColossusStands_FollowsTheCoreFromFurtherBack()
        {
            var motor = Object.FindAnyObjectByType<SwarmCoreMotor>();
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            var colossus = AssetDatabase.LoadAssetAtPath<ColossusConfig>(ColossusConfigPath);
            Assert.That(follow, Is.Not.Null, "The scene has no FollowCamera.");
            Assert.That(colossus, Is.Not.Null, "There is no ColossusConfig at " + ColossusConfigPath + ". Rebuild the scene.");
            Assert.That(colossus.CameraDistance, Is.GreaterThan(follow.DefaultDistance + StepBackMetres), "the giant's view is not further back");
            HiveDefenseSession session = _root.Session;

            Assert.That(follow.Target, Is.SameAs(motor.transform), "before the giant");
            Assert.That(follow.WantedDistance, Is.EqualTo(follow.DefaultDistance).Within(1e-4f));
            float before = GroundDistance(follow.transform.position, motor.Position);

            yield return FormTheColossus();
            Time.timeScale = FastTimeScale;
            yield return WaitGameSeconds(CameraBlendSeconds);
            Time.timeScale = _savedTimeScale;

            Assert.That(session.IsColossusActive, Is.True, "the giant ended before the camera was measured");
            Assert.That(follow.Target, Is.SameAs(motor.transform), "the camera keeps the core, inside the giant");
            Assert.That(follow.WantedDistance, Is.EqualTo(colossus.CameraDistance).Within(1e-4f));
            Assert.That(GroundDistance(follow.transform.position, motor.Position), Is.GreaterThan(before + StepBackMetres), "the camera did not step back");

            session.Swarm.Lose(session.Swarm.Count - (_swarmConfig.CollapseBelow / 2));
            Assert.That(session.Forms.Current, Is.EqualTo(SwarmForm.Collapsed));
            yield return null;

            Assert.That(session.IsColossusActive, Is.False, "the collapse ended the giant");
            Assert.That(follow.Target, Is.SameAs(motor.transform), "after the giant");
            Assert.That(follow.WantedDistance, Is.EqualTo(follow.DefaultDistance).Within(1e-4f));
        }

        // Plan section K: a giant can stand over a hive and carry the core inside the hive's collider. When the giant ends
        // there, the root puts the core down at the same place, clear of the hive. Nothing pops on the next frame, and the
        // first step goes the way the player asked, not the way out of the hive.
        [UnityTest]
        public IEnumerator Core_PutDownWhereTheGiantStoodOverAHive_StandsClearOfIt()
        {
            var motor = Object.FindAnyObjectByType<SwarmCoreMotor>();
            var controller = motor.GetComponent<CharacterController>();
            Vector3 hive = _root.Session.Hives[0].Position;
            Collider[] solids = SolidCollidersOf(NearestHiveView(hive).gameObject);
            Assert.That(solids, Is.Not.Empty, "the hive has no collider to hold the core inside it");

            yield return PutDownInside(motor, controller, solids, hive);

            Vector3 away = motor.Position - hive;
            away.y = 0f;
            Assert.That(away.magnitude, Is.LessThan(ShortWayOutMetres), "the core was not put down beside the hive");
            Vector3 before = motor.Position;
            motor.Move(away.normalized, 1f, false, WalkSeconds);
            Vector3 walked = motor.Position - before;
            walked.y = 0f;
            Assert.That(walked.magnitude, Is.GreaterThan(SamePlaceMetres), "the core did not walk");
            Assert.That(Vector3.Dot(walked.normalized, away.normalized), Is.GreaterThan(SameWayDot), "the first step jumped another way");
        }

        // The same for scenery: a giant that ends inside a ruin leaves the core beside it.
        [UnityTest]
        public IEnumerator Core_PutDownWhereTheGiantStoodInAWall_StandsClearOfIt()
        {
            var motor = Object.FindAnyObjectByType<SwarmCoreMotor>();
            var controller = motor.GetComponent<CharacterController>();
            float edge = LoadCoreMove().PlayAreaHalfSize - WallInsideEdgeMetres;
            GameObject scenery = GameObject.Find(SceneryName);
            Assert.That(scenery, Is.Not.Null, "The scene has no " + SceneryName + ".");
            BoxCollider wall = null;
            foreach (BoxCollider box in scenery.GetComponentsInChildren<BoxCollider>())
            {
                Bounds bounds = box.bounds;
                bool isBig = bounds.extents.x > WallHalfSizeMetres && bounds.extents.z > WallHalfSizeMetres;
                bool isInPlay = Mathf.Abs(bounds.center.x) < edge && Mathf.Abs(bounds.center.z) < edge;
                if (box.enabled && !box.isTrigger && isBig && isInPlay)
                {
                    wall = box;
                    break;
                }
            }

            Assert.That(wall, Is.Not.Null, "the scenery has no wall big enough to stand inside");
            Vector3 spot = new Vector3(wall.bounds.center.x, motor.Position.y, wall.bounds.center.z);

            yield return PutDownInside(motor, controller, SolidCollidersOf(scenery), spot);
        }

        // The core keeps to the play area, which lies inside the ground: a core put past its edge is brought back to the
        // edge on the next frame, on the ground. A wall at the edge may push it a little further.
        [UnityTest]
        public IEnumerator Core_PutPastThePlayArea_IsBackAtItsEdgeOnTheGroundNextFrame()
        {
            var motor = Object.FindAnyObjectByType<SwarmCoreMotor>();
            GameObject ground = GameObject.Find(GroundName);
            Assert.That(ground, Is.Not.Null, "The scene has no " + GroundName + ".");
            float edge = LoadCoreMove().PlayAreaHalfSize;
            Bounds groundBounds = ground.GetComponent<Collider>().bounds;
            Assert.That(groundBounds.max.x, Is.GreaterThan(edge + PastEdgeMetres), "the ground ends inside the play area");
            Assert.That(groundBounds.min.x, Is.LessThan(-edge - PastEdgeMetres));
            Assert.That(groundBounds.max.z, Is.GreaterThan(edge + PastEdgeMetres));
            Assert.That(groundBounds.min.z, Is.LessThan(-edge - PastEdgeMetres));
            float height = motor.Position.y;

            motor.PlaceAt(new Vector3(edge + PastEdgeMetres, 0f, 0f), true);
            yield return null;

            Assert.That(_root.Session.CorePosition.x, Is.EqualTo(edge).Within(SamePlaceMetres), "the session did not hold the core at the edge");
            Assert.That(motor.Position.x, Is.LessThan(edge + (PastEdgeMetres * 0.5f)), "the core was not brought back");
            Assert.That(motor.Position.y, Is.GreaterThan(height - FallenMetres), "the core fell");
        }

        // Carries the core into the solids as the giant would, puts it down at the same place as the root does when the
        // giant ends, and checks it is clear and stays put on the next frame.
        private static IEnumerator PutDownInside(SwarmCoreMotor motor, CharacterController controller, Collider[] solids, Vector3 spot)
        {
            motor.PlaceAt(spot, false);
            Assert.That(IsInside(controller, solids), Is.True, "the giant could not carry the core inside");

            motor.PlaceAt(spot, true);
            Assert.That(IsInside(controller, solids), Is.False, "the core was put down inside");
            Vector3 putDown = motor.Position;
            yield return null;

            Assert.That(GroundDistance(motor.Position, putDown), Is.LessThan(SamePlaceMetres), "the core popped on the next frame");
            Assert.That(IsInside(controller, solids), Is.False);
        }

        private static CoreMoveConfig LoadCoreMove()
        {
            var coreMove = AssetDatabase.LoadAssetAtPath<CoreMoveConfig>(CoreMoveConfigPath);
            Assert.That(coreMove, Is.Not.Null, "There is no CoreMoveConfig at " + CoreMoveConfigPath + ". Rebuild the scene.");
            return coreMove;
        }

        // Fills Fury, plays the Colossus keys and waits until the giant stands.
        private IEnumerator FormTheColossus()
        {
            HiveDefenseSession session = _root.Session;
            session.Fury.AddDamage(FullFuryDamage);
            Assert.That(session.Fury.IsFull, Is.True);
            yield return Play(KeysOf<ColossusSong>());

            float waited = 0f;
            while (!session.IsColossusActive && waited < MaxColossusWaitSeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(session.IsColossusActive, Is.True, "The Colossus keys at full Fury did not form the Colossus.");
        }

        // Counts game time, not frames, because batch mode runs thousands of frames a second.
        private static IEnumerator WaitGameSeconds(float seconds)
        {
            float elapsed = 0f;
            int frames = 0;
            while (elapsed < seconds && frames < MaxFramesForWave)
            {
                yield return null;
                elapsed += Time.deltaTime;
                frames++;
            }
        }

        private static HiveView NearestHiveView(Vector3 place)
        {
            HiveView nearest = null;
            float best = float.MaxValue;
            foreach (HiveView view in Object.FindObjectsByType<HiveView>())
            {
                float distance = GroundDistance(view.transform.position, place);
                if (distance < best)
                {
                    best = distance;
                    nearest = view;
                }
            }

            Assert.That(nearest, Is.Not.Null, "The scene has no HiveView.");
            return nearest;
        }

        private static Collider[] SolidCollidersOf(GameObject owner)
        {
            var solid = new List<Collider>();
            foreach (Collider collider in owner.GetComponentsInChildren<Collider>())
            {
                if (collider.enabled && !collider.isTrigger)
                {
                    solid.Add(collider);
                }
            }

            return solid.ToArray();
        }

        // Whether the core's capsule, made a hair thinner so touching does not count, overlaps any of the colliders.
        private static bool IsInside(CharacterController controller, Collider[] colliders)
        {
            Vector3 center = controller.transform.TransformPoint(controller.center);
            float radius = controller.radius - TouchingMetres;
            float half = Mathf.Max(0f, (controller.height * 0.5f) - controller.radius);
            Collider[] overlaps = Physics.OverlapCapsule(
                center + (Vector3.up * half), center - (Vector3.up * half), radius, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            foreach (Collider overlap in overlaps)
            {
                if (System.Array.IndexOf(colliders, overlap) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        [UnityTest]
        public IEnumerator Beekeeper_WhileWalkingToAHive_MovesItsLegs()
        {
            Time.timeScale = FastTimeScale;

            int frames = 0;
            float elapsed = 0f;
            while (_root.Session.ActiveBeekeeperCount < 1 && elapsed < MaxWaveSeconds && frames < MaxFramesForWave)
            {
                elapsed += Time.deltaTime;
                frames++;
                yield return null;
            }

            Time.timeScale = _savedTimeScale;
            BeekeeperView view = null;
            foreach (BeekeeperView candidate in Object.FindObjectsByType<BeekeeperView>(FindObjectsSortMode.None))
            {
                if (candidate.isActiveAndEnabled && candidate.Brain != null)
                {
                    view = candidate;
                    break;
                }
            }

            Assert.That(view, Is.Not.Null, "No beekeeper is shown.");
            Transform leg = FindBone(view.transform, LegBone);
            Assert.That(leg, Is.Not.Null, "The beekeeper's model has no " + LegBone + " bone.");

            float swing = 0f;
            yield return WatchBone(leg, WatchSeconds, angle => swing = angle);

            Assert.That(swing, Is.GreaterThan(SmallestSwingDegrees));
        }

        private IEnumerator Play(IReadOnlyList<int> holes)
        {
            for (int index = 0; index < holes.Count; index++)
            {
                _root.PressHole(holes[index]);
                for (int frame = 0; frame < FramesBetweenNotes; frame++)
                {
                    yield return null;
                }
            }

            // The last note is read on the next tick.
            yield return null;
            yield return null;
        }

        // The keys on the scene's own song asset.
        private IReadOnlyList<int> KeysOf<T>() where T : SwarmSongEffect
        {
            return _root.Session.Caster[SongIndex<T>()].Song.Keys;
        }

        private int SongIndex<T>() where T : SwarmSongEffect
        {
            SwarmSongCaster caster = _root.Session.Caster;
            for (int song = 0; song < caster.Count; song++)
            {
                if (caster[song] is T)
                {
                    return song;
                }
            }

            Assert.Fail("The scene has no " + typeof(T).Name + ".");
            return -1;
        }

        private static float GroundDistance(Vector3 from, Vector3 to)
        {
            Vector3 offset = to - from;
            offset.y = 0f;
            return offset.magnitude;
        }

        private static Transform FindBone(Transform root, string boneName)
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == boneName)
                {
                    return candidate;
                }
            }

            return null;
        }

        // Reports the widest angle the bone turned away from where it started, over some game seconds.
        private static IEnumerator WatchBone(Transform bone, float seconds, System.Action<float> report)
        {
            Quaternion start = bone.localRotation;
            float widest = 0f;
            float elapsed = 0f;
            int frames = 0;
            while (elapsed < seconds && frames < MaxFramesForWave)
            {
                yield return null;
                elapsed += Time.deltaTime;
                frames++;
                widest = Mathf.Max(widest, Quaternion.Angle(start, bone.localRotation));
            }

            report(widest);
        }
    }
}
#endif
