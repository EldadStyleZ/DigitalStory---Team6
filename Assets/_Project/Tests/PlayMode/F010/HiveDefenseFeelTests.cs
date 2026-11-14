#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>
    /// Plan section O in the real scene, after the owner played: a stick walks past a core parked beside its hive and strikes
    /// the hive; a collapsed swarm is not chased and the HUD counts its return; a Sting with 800 wasps sends two stings to two
    /// beekeepers; a hive inside the Dome is shielded and the blocked blow is reported; a hit shakes the camera and shows a
    /// number; and a killing blow flashes the beekeeper it kills, not its neighbour. Every song is played with the keys on its
    /// own asset.
    /// </summary>
    /// <remarks>
    /// Every frame is pinned to 1/30 s of game time with Time.captureDeltaTime, so each wait is in game seconds. The beekeepers
    /// of the first wave (four sticks) are stunned as they come, then put where a test needs them.
    /// </remarks>
    public sealed class HiveDefenseFeelTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const int FramesToSettle = 3;
        private const float FrameSeconds = 1f / 30f;
        private const float StunSeconds = 60f;
        private const float MaxWaveSeconds = 60f;
        private const float SettleSeconds = 0.5f;
        private const string SwarmLineName = "swarm-line";
        private const string LineShownClass = "line--shown";

        // A stick starts 9 m south of the west hive and walks north to it, straight through the core parked 2 m south of it.
        private const float StickStartMetres = 9f;
        private const float BesideHiveMetres = 2f;
        private const float MaxHiveStrikeSeconds = 10f;

        // A provoked stick next to the core, then the swarm collapses under it.
        private const float ProvokeDamage = 1f;
        private const float ChaseSeconds = 0.5f;
        private const float BlindSeconds = 2f;

        // Two beekeepers 5 m in front of the core, either side: within the Sting's reach and well apart.
        private const int YoungSwarm = 800;
        private const int StingsAt800 = 2;
        private const float SamePlaceMetres = 0.1f;
        private const float AfterArrivalSeconds = 0.3f;

        // East of the hive, in a stick's reach of it, as the Honeycomb snapshot stands it.
        private const float MaxBlockedSeconds = 4f;
        private const float DomeWovenSeconds = 0.5f;

        private const float MaxHitSeconds = 1.5f;
        private const int FramesAfterHit = 3;
        private const float HitStopTestSeconds = 0.5f;

        // A beekeeper on its last breath half a metre inside the Sting's reach, and its neighbour half a metre beyond it.
        private const float NeighbourMetres = 1f;
        private const float LastBreathHealth = 1f;

        // Plan section O: the scene's moon shadows, and values no quality level uses, to see the root keep and put them back.
        private const float SceneShadowMetres = 45f;
        private const int SceneShadowCascades = 2;
        private const float OtherShadowMetres = 77f;
        private const int OtherShadowCascades = 4;
        private const string EmptySceneName = "F010 Empty";

        private static readonly Vector3 HiveStickOffset = new Vector3(1.75f, 0f, 0.3f);
        private static readonly Vector3 LeftOfCore = new Vector3(-3f, 0f, 4f);
        private static readonly Vector3 RightOfCore = new Vector3(3f, 0f, 4f);
        private static readonly Vector3 InFrontOfCore = new Vector3(0f, 0f, 3f);
        private static readonly Vector3 FarAway = new Vector3(-16f, 0f, -14f);

        private HiveDefenseRoot _root;
        private SwarmCoreMotor _core;
        private float _savedTimeScale;
        private float _savedCaptureDeltaTime;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            _savedTimeScale = Time.timeScale;
            _savedCaptureDeltaTime = Time.captureDeltaTime;
            Time.timeScale = 1f;
            Time.captureDeltaTime = FrameSeconds;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));

            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                yield return null;
            }

            _root = Object.FindAnyObjectByType<HiveDefenseRoot>();
            _core = Object.FindAnyObjectByType<SwarmCoreMotor>();
            Assert.That(_root, Is.Not.Null);
            Assert.That(_core, Is.Not.Null);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = _savedTimeScale;
            Time.captureDeltaTime = _savedCaptureDeltaTime;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Stick_WalkingPastACoreParkedBesideItsHive_StrikesTheHiveNotTheCore()
        {
            HiveDefenseSession session = _root.Session;
            yield return WaitForStunnedBeekeepers(1);
            Hive hive = session.Hives[0];
            _core.PlaceAt(hive.Position + (Vector3.back * BesideHiveMetres), true);
            yield return Wait(SettleSeconds);

            BeekeeperBrain stick = session.ActiveBeekeeper(0);
            Assert.That(stick.Config.Kind, Is.EqualTo(BeekeeperKind.Stick), "the first wave's beekeepers are sticks");
            stick.Spawn(hive.Position + (Vector3.back * StickStartMetres));
            float healthBefore = hive.Health;
            int waspsBefore = session.Swarm.Count;

            float waited = 0f;
            while (hive.Health >= healthBefore && waited < MaxHiveStrikeSeconds)
            {
                Assert.That(stick.State, Is.Not.EqualTo(BeekeeperState.ChaseCore), "the stick turned on the core it walked past");
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(hive.Health, Is.LessThan(healthBefore), "the stick never struck its hive");
            Assert.That(session.Swarm.Count, Is.EqualTo(waspsBefore), "the stick hit the core");
        }

        [UnityTest]
        public IEnumerator CollapsedSwarm_IsNotChased_AndTheHudCountsItsReturn()
        {
            HiveDefenseSession session = _root.Session;
            yield return WaitForStunnedBeekeepers(1);
            BeekeeperBrain stick = session.ActiveBeekeeper(0);
            stick.Spawn(_core.Position + RightOfCore);
            stick.TakeDamage(ProvokeDamage);
            yield return Wait(ChaseSeconds);
            Assert.That(stick.IsProvoked, Is.True);
            Assert.That(stick.State, Is.EqualTo(BeekeeperState.ChaseCore).Or.EqualTo(BeekeeperState.WindUp), "a hurt stick goes for the core");

            session.Swarm.Lose(session.Swarm.Count);
            Assert.That(session.Swarm.IsCollapsed, Is.True);
            float watched = 0f;
            while (watched < BlindSeconds)
            {
                watched += Time.deltaTime;
                yield return null;
                Assert.That(session.Swarm.IsCollapsed, Is.True, "the swarm came back during the watch");
                Assert.That(stick.State, Is.Not.EqualTo(BeekeeperState.ChaseCore), "the stick chases a collapsed swarm");
                Assert.That(stick.State == BeekeeperState.WindUp && !stick.IsTargetingHive, Is.False, "the stick swings at a collapsed swarm");
            }

            Label line = HudLabel(SwarmLineName);
            int seconds = Mathf.CeilToInt(session.Swarm.SecondsToRespawn);
            Assert.That(seconds, Is.GreaterThan(0));
            Assert.That(line.ClassListContains(LineShownClass), Is.True, "the HUD does not say the swarm is returning");
            Assert.That(line.text, Does.Contain(seconds.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        [UnityTest]
        public IEnumerator Sting_With800Wasps_SendsTwoStingsToTwoBeekeepers()
        {
            HiveDefenseSession session = _root.Session;
            SwarmBody swarm = session.Swarm;
            if (swarm.Count > YoungSwarm)
            {
                swarm.Lose(swarm.Count - YoungSwarm);
            }

            Assert.That(swarm.Count, Is.EqualTo(YoungSwarm), "the match starts with 800 wasps");
            yield return WaitForStunnedBeekeepers(2);
            BeekeeperBrain left = Place(session.ActiveBeekeeper(0), _core.Position + LeftOfCore);
            BeekeeperBrain right = Place(session.ActiveBeekeeper(1), _core.Position + RightOfCore);
            float leftBefore = left.Health;
            float rightBefore = right.Health;

            yield return Play(KeysOf<StingSong>());
            SwarmFormMachine forms = session.Forms;
            Assert.That(forms.Slot(SongSlot.Strike).Form, Is.EqualTo(SwarmForm.Sting));
            Assert.That(forms.StingCount, Is.EqualTo(StingsAt800));
            Vector3 first = forms.StingTarget(0);
            Vector3 second = forms.StingTarget(1);
            Assert.That(GroundDistance(first, second), Is.GreaterThan(1f), "both stings went to one beekeeper");
            Assert.That(IsOn(first, left) || IsOn(first, right), Is.True, "the first sting is on neither beekeeper");
            Assert.That(IsOn(second, left) || IsOn(second, right), Is.True, "the second sting is on neither beekeeper");

            yield return null;
            var swarmView = Object.FindAnyObjectByType<SwarmGpuView>();
            Assert.That(swarmView.StingTargetCount, Is.EqualTo(StingsAt800), "the picture was not given both stings");

            StingSong song = (StingSong)session.Caster[SongIndex<StingSong>()];
            yield return WaitForSlot(SongSlot.Strike, song.ArriveSeconds + AfterArrivalSeconds);
            Assert.That(left.Health, Is.LessThan(leftBefore), "the left beekeeper was not stung");
            Assert.That(right.Health, Is.LessThan(rightBefore), "the right beekeeper was not stung");
        }

        [UnityTest]
        public IEnumerator Dome_WithAHiveInsideIt_ShieldsTheHiveFromAStick()
        {
            HiveDefenseSession session = _root.Session;
            yield return WaitForStunnedBeekeepers(1);
            Place(session.ActiveBeekeeper(0), FarAway);
            Hive hive = session.Hives[0];
            _core.PlaceAt(hive.Position + (Vector3.back * BesideHiveMetres), true);
            yield return Wait(SettleSeconds);

            yield return Play(KeysOf<DomeSong>());
            Assert.That(session.Forms.Slot(SongSlot.Guard).Form, Is.EqualTo(SwarmForm.Dome));
            yield return Wait(DomeWovenSeconds);
            DomeSong dome = (DomeSong)session.Caster[SongIndex<DomeSong>()];
            Assert.That(session.Forms.HiveDamageFraction(0), Is.EqualTo(dome.LossFraction).Within(1e-4f), "the hive is not inside the Dome");

            var blocked = new List<SongHit>();
            System.Action<SongHit> onLanded = hit =>
            {
                if (hit.IsBlocked)
                {
                    blocked.Add(hit);
                }
            };

            session.SongLanded += onLanded;
            try
            {
                BeekeeperBrain stick = session.ActiveBeekeeper(0);
                stick.Spawn(hive.Position + HiveStickOffset);
                float healthBefore = hive.Health;
                float waited = 0f;
                while (blocked.Count == 0 && waited < MaxBlockedSeconds)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }

                Assert.That(blocked.Count, Is.GreaterThan(0), "no blocked blow was reported");
                SongHit hit = blocked[0];
                Assert.That(hit.Form, Is.EqualTo(SwarmForm.Dome));
                Assert.That(GroundDistance(hit.Position, hive.Position), Is.LessThan(SamePlaceMetres), "the blocked blow is not at the hive");
                float full = stick.Config.HiveDamage;
                Assert.That(healthBefore - hive.Health, Is.EqualTo(full * dome.LossFraction).Within(1e-3f), "the hive took more than the Dome lets through");
                Assert.That(hit.Damage, Is.EqualTo(full * (1f - dome.LossFraction)).Within(1e-3f), "the blocked blow does not carry the harm the Dome stopped");
            }
            finally
            {
                session.SongLanded -= onLanded;
            }
        }

        [UnityTest]
        public IEnumerator SongHit_ShakesTheCameraAndShowsANumber()
        {
            HiveDefenseSession session = _root.Session;
            yield return WaitForStunnedBeekeepers(1);
            Place(session.ActiveBeekeeper(0), _core.Position + InFrontOfCore);
            var camera = Object.FindAnyObjectByType<FollowCamera>();
            var numbers = Object.FindAnyObjectByType<DamageNumbersView>();
            Assert.That(camera, Is.Not.Null);
            Assert.That(numbers, Is.Not.Null, "the HUD made no DamageNumbersView");
            Assert.That(camera.ShakeStrength, Is.EqualTo(0f));

            int hits = 0;
            System.Action<SongHit> onLanded = hit =>
            {
                if (!hit.IsBlocked)
                {
                    hits++;
                }
            };

            session.SongLanded += onLanded;
            try
            {
                yield return Play(KeysOf<TornadoSong>());
                float waited = 0f;
                while (hits == 0 && waited < MaxHitSeconds)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }

                Assert.That(hits, Is.GreaterThan(0), "the Tornado landed no hit");
                float mostShake = 0f;
                int mostNumbers = 0;
                for (int frame = 0; frame < FramesAfterHit; frame++)
                {
                    mostShake = Mathf.Max(mostShake, camera.ShakeStrength);
                    mostNumbers = Mathf.Max(mostNumbers, numbers.ActiveCount);
                    yield return null;
                }

                Assert.That(mostShake, Is.GreaterThan(0f), "the hit did not shake the camera");
                Assert.That(mostNumbers, Is.GreaterThan(0), "the hit showed no number");
            }
            finally
            {
                session.SongLanded -= onLanded;
            }
        }

        // Plan section Y: a heavy hit holds the swarm still for a moment (the hit-stop), and a note played during it still counts:
        // the hit-stop freezes only the swarm's motion, never the song input or time.
        [UnityTest]
        public IEnumerator HitStop_ANotePressedDuringIt_IsNotDropped()
        {
            HiveDefenseSession session = _root.Session;
            var swarm = Object.FindAnyObjectByType<SwarmGpuView>();
            var feedback = Object.FindAnyObjectByType<HitFeedbackView>();
            Assert.That(swarm, Is.Not.Null);
            Assert.That(feedback, Is.Not.Null);
            Assert.That(feedback.Feel, Is.Not.Null, "the scene builder gave the feel director no FeelConfig");
            yield return Wait(SettleSeconds);

            int played = 0;
            System.Action<int> onNote = hole => played++;
            session.Caster.NotePlayed += onNote;
            try
            {
                swarm.Freeze(HitStopTestSeconds);
                float timeScale = Time.timeScale;
                _root.PressHole(0);
                for (int frame = 0; frame < FramesAfterHit; frame++)
                {
                    yield return null;
                }

                Assert.That(swarm.IsFrozen, Is.True, "the hit-stop ended before the note was read");
                Assert.That(Time.timeScale, Is.EqualTo(timeScale), "the hit-stop changed the time scale");
                Assert.That(played, Is.EqualTo(1), "the note played during the hit-stop was dropped");
            }
            finally
            {
                session.Caster.NotePlayed -= onNote;
            }
        }

        // The kill is reported after the killed beekeeper has begun to die, and it must still flash that one white, never a
        // neighbour a metre away. The neighbour stands beyond the Sting's reach, so nothing else hits it.
        [UnityTest]
        public IEnumerator Kill_FlashesTheKilledBeekeeper_NotItsNeighbourAMetreAway()
        {
            HiveDefenseSession session = _root.Session;
            yield return WaitForStunnedBeekeepers(2);
            StingSong song = (StingSong)session.Caster[SongIndex<StingSong>()];
            float reach = song.MaxRange * session.SizeScale;
            Vector3 inside = _core.Position + (Vector3.forward * (reach - (NeighbourMetres * 0.5f)));
            BeekeeperBrain victim = Place(session.ActiveBeekeeper(0), inside);
            BeekeeperBrain neighbour = Place(session.ActiveBeekeeper(1), inside + (Vector3.forward * NeighbourMetres));
            victim.TakeDamage(victim.Health - LastBreathHealth);
            yield return Wait(SettleSeconds);
            BeekeeperView victimView = ViewOf(victim);
            BeekeeperView neighbourView = ViewOf(neighbour);

            yield return Play(KeysOf<StingSong>());
            Assert.That(session.Forms.Slot(SongSlot.Strike).Form, Is.EqualTo(SwarmForm.Sting));
            bool victimFlashed = false;
            bool neighbourFlashed = false;
            int framesAfterKill = 0;
            float waited = 0f;
            while (framesAfterKill < FramesAfterHit && waited < MaxHitSeconds)
            {
                victimFlashed |= victimView.IsHitFlashing;
                neighbourFlashed |= neighbourView.IsHitFlashing;
                if (!victim.IsAlive)
                {
                    framesAfterKill++;
                }

                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(victim.IsAlive, Is.False, "the Sting did not kill the beekeeper in its reach");
            Assert.That(neighbour.IsAlive, Is.True, "the neighbour beyond the reach was stung");
            Assert.That(victimFlashed, Is.True, "the killing blow did not flash the beekeeper it killed");
            Assert.That(neighbourFlashed, Is.False, "the killing blow flashed the neighbour");
        }

        // Every light the scene uses exists and is enabled from the first frame, so none is switched on in play; the swarm's
        // attack light waits dark; and the nest lights never take a per-pixel slot.
        [UnityTest]
        public IEnumerator Lights_FromTheFirstFrame_AreAllEnabled_AndNestLightsAreNotImportant()
        {
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
            Assert.That(lights.Length, Is.GreaterThan(1));
            int nestLights = 0;
            foreach (Light light in lights)
            {
                Assert.That(light.enabled && light.gameObject.activeInHierarchy, Is.True, light.name + " is switched off");
                if (light.GetComponentInParent<NestView>(true) != null)
                {
                    nestLights++;
                    Assert.That(light.renderMode, Is.EqualTo(LightRenderMode.ForceVertex), light.name + " is an important light");
                }
            }

            Assert.That(nestLights, Is.GreaterThan(0), "the scene has no nest light");
            yield return null;
        }

        // The root sets the moon's shadows to 45 m in 2 cascades while the scene is loaded, and puts back whatever the quality
        // level had when the scene unloads.
        [UnityTest]
        public IEnumerator SceneShadows_WhileLoaded_Are45MetresIn2Cascades_AndComeBackWhenItUnloads()
        {
            Assert.That(QualitySettings.shadowDistance, Is.EqualTo(SceneShadowMetres));
            Assert.That(QualitySettings.shadowCascades, Is.EqualTo(SceneShadowCascades));
            yield return UnloadTheScene();
            float levelMetres = QualitySettings.shadowDistance;
            int levelCascades = QualitySettings.shadowCascades;
            try
            {
                QualitySettings.shadowDistance = OtherShadowMetres;
                QualitySettings.shadowCascades = OtherShadowCascades;
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;
                Assert.That(QualitySettings.shadowDistance, Is.EqualTo(SceneShadowMetres));
                Assert.That(QualitySettings.shadowCascades, Is.EqualTo(SceneShadowCascades));

                yield return UnloadTheScene();
                Assert.That(QualitySettings.shadowDistance, Is.EqualTo(OtherShadowMetres), "the shadow distance was not put back");
                Assert.That(QualitySettings.shadowCascades, Is.EqualTo(OtherShadowCascades), "the cascades were not put back");
            }
            finally
            {
                QualitySettings.shadowDistance = levelMetres;
                QualitySettings.shadowCascades = levelCascades;
            }
        }

        // Leaves an empty scene active and unloads the hive defense scene, whose root then goes.
        private static IEnumerator UnloadTheScene()
        {
            Scene hiveScene = SceneManager.GetActiveScene();
            Scene empty = SceneManager.CreateScene(EmptySceneName + " " + Time.frameCount);
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(hiveScene);
            yield return null;
            Assert.That(Object.FindAnyObjectByType<HiveDefenseRoot>(), Is.Null, "the hive defense scene is still loaded");
        }

        // Calls the first wave and stuns each beekeeper as it comes, for the rest of the test, until there are this many.
        private IEnumerator WaitForStunnedBeekeepers(int wanted)
        {
            HiveDefenseSession session = _root.Session;
            session.BeekeeperSpawned += keeper => keeper.Stun(StunSeconds);
            _root.CallWave();
            float elapsed = 0f;
            while (session.ActiveBeekeeperCount < wanted && elapsed < MaxWaveSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.That(session.ActiveBeekeeperCount, Is.GreaterThanOrEqualTo(wanted), "the wave brought too few beekeepers");
        }

        private static BeekeeperBrain Place(BeekeeperBrain keeper, Vector3 spot)
        {
            keeper.Spawn(spot);
            keeper.Stun(StunSeconds);
            return keeper;
        }

        private static bool IsOn(Vector3 target, BeekeeperBrain keeper) => GroundDistance(target, keeper.Position) < SamePlaceMetres;

        private static BeekeeperView ViewOf(BeekeeperBrain keeper)
        {
            IReadOnlyList<BeekeeperView> views = BeekeeperView.Live;
            for (int index = 0; index < views.Count; index++)
            {
                if (views[index].Brain == keeper)
                {
                    return views[index];
                }
            }

            Assert.Fail("No view shows the beekeeper.");
            return null;
        }

        private static Label HudLabel(string name)
        {
            var document = Object.FindAnyObjectByType<UIDocument>();
            Assert.That(document, Is.Not.Null);
            var label = document.rootVisualElement.Q<Label>(name);
            Assert.That(label, Is.Not.Null, "the HUD has no label " + name);
            return label;
        }

        private IEnumerator WaitForSlot(SongSlot slot, float seconds)
        {
            SwarmFormMachine forms = _root.Session.Forms;
            while (forms.Slot(slot).IsActive && forms.Slot(slot).Seconds < seconds)
            {
                yield return null;
            }

            Assert.That(forms.Slot(slot).IsActive, Is.True, slot + " emptied before " + seconds + " s");
        }

        private static IEnumerator Wait(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator Play(IReadOnlyList<int> holes)
        {
            for (int index = 0; index < holes.Count; index++)
            {
                _root.PressHole(holes[index]);
                yield return null;
            }

            // The last note is read on the next tick.
            yield return null;
        }

        private IReadOnlyList<int> KeysOf<T>() where T : SwarmSongEffect
        {
            return _root.Session.Caster[SongIndex<T>()].Song.Keys;
        }

        private int SongIndex<T>() where T : SwarmSongEffect
        {
            HiveDefenseSession session = _root.Session;
            for (int song = 0; song < session.Caster.Count; song++)
            {
                if (session.Caster[song] is T)
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
    }
}
#endif
