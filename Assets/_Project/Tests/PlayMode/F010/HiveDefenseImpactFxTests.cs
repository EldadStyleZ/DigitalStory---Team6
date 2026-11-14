#if UNITY_EDITOR
using System.Collections;
using DigitalStory.Data;
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
    /// Plan section AB, the optional impact effects. They must work for a teammate without the paid pack, so nothing here
    /// refers to it: a spawner without a library makes and plays nothing; one with a stand-in library, built from a particle
    /// object made here, makes every instance at load and never one more, and reuses the oldest; and in the real scene the
    /// feel director still shakes and shows numbers without a library, and plays the stand-in's effects on a hit when given one.
    /// </summary>
    public sealed class HiveDefenseImpactFxTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const int FramesToSettle = 3;
        private const float FrameSeconds = 1f / 30f;
        private const float StunSeconds = 60f;
        private const float MaxWaveSeconds = 60f;
        private const float MaxHitSeconds = 1.5f;
        private const int FramesAfterHit = 3;
        private const int HitCap = 3;
        private const int DeathCap = 2;
        private const int Plays = 10;
        private static readonly Vector3 InFrontOfCore = new Vector3(0f, 0f, 3f);

        private float _savedTimeScale;
        private float _savedCaptureDeltaTime;
        private GameObject _template;
        private ImpactFxLibrary _standIn;

        [UnitySetUp]
        public IEnumerator Prepare()
        {
            _savedTimeScale = Time.timeScale;
            _savedCaptureDeltaTime = Time.captureDeltaTime;
            Time.timeScale = 1f;
            Time.captureDeltaTime = FrameSeconds;
            _template = MakeParticleTemplate();
            _standIn = ScriptableObject.CreateInstance<ImpactFxLibrary>();
            _standIn.name = "Stand-in Impact Library";
            _standIn.SetSlot(ImpactFxKind.Hit, new ImpactFxSlot(_template, 1f, HitCap, 0f, 1f));
            _standIn.SetSlot(ImpactFxKind.Death, new ImpactFxSlot(_template, 1f, DeathCap, 0f, 0.5f));
            _standIn.SetHitLeastDamage(0f);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = _savedTimeScale;
            Time.captureDeltaTime = _savedCaptureDeltaTime;
            Object.Destroy(_template);
            Object.Destroy(_standIn);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Spawner_WithNoLibrary_MakesNothingAndPlaysNothing()
        {
            var holder = new GameObject("Impact Holder");
            try
            {
                var spawner = new ImpactFxSpawner(null, holder.transform);
                yield return null;

                Assert.That(spawner.HasEffects, Is.False);
                foreach (ImpactFxKind kind in System.Enum.GetValues(typeof(ImpactFxKind)))
                {
                    Assert.That(spawner.Play(kind, Vector3.zero, Time.time, 1f), Is.False, kind + " played without a library");
                }

                Assert.That(holder.transform.childCount, Is.EqualTo(0), "the spawner made objects without a library");
                Assert.That(spawner.PlayCount, Is.EqualTo(0));
            }
            finally
            {
                Object.Destroy(holder);
            }
        }

        [UnityTest]
        public IEnumerator Spawner_WithAStandInLibrary_MakesEveryInstanceAtLoad_NeverMore_AndReusesTheOldest()
        {
            var holder = new GameObject("Impact Holder");
            try
            {
                var spawner = new ImpactFxSpawner(_standIn, holder.transform);
                Assert.That(spawner.HasEffects, Is.True);
                Assert.That(spawner.InstanceCountOf(ImpactFxKind.Hit), Is.EqualTo(HitCap));
                Assert.That(spawner.InstanceCountOf(ImpactFxKind.Death), Is.EqualTo(DeathCap));
                Assert.That(spawner.InstanceCountOf(ImpactFxKind.MeteorLanding), Is.EqualTo(0), "an empty slot made instances");
                Assert.That(holder.transform.childCount, Is.EqualTo(HitCap + DeathCap));

                for (int play = 0; play < Plays; play++)
                {
                    Assert.That(spawner.Play(ImpactFxKind.Hit, new Vector3(play, 0f, 0f), Time.time, 1f), Is.True);
                    yield return null;
                }

                Assert.That(spawner.Play(ImpactFxKind.GiantSlam, Vector3.zero, Time.time, 1f), Is.False, "a kind without a slot played");
                Assert.That(holder.transform.childCount, Is.EqualTo(HitCap + DeathCap), "play made new objects");
                Assert.That(spawner.PlayCount, Is.EqualTo(Plays));

                // Ten plays on three instances: the last three plays hold the three instances, the newest on the one the
                // tenth play took (the first, after 0, 1, 2, 0, 1, 2, 0, 1, 2, 0), each lifted by the slot's height.
                Transform newest = spawner.Instance(ImpactFxKind.Hit, (Plays - 1) % HitCap);
                Assert.That(newest.position, Is.EqualTo(new Vector3(Plays - 1, 1f, 0f)));
                Assert.That(newest.GetComponent<ParticleSystem>().isPlaying, Is.True, "the reused instance is not playing");
            }
            finally
            {
                Object.Destroy(holder);
            }
        }

        [UnityTest]
        public IEnumerator Scene_WithoutALibrary_AHitStillShakesAndShowsANumber_AndNoEffectIsMade()
        {
            yield return LoadScene();
            var feedback = Object.FindAnyObjectByType<HitFeedbackView>();
            Assert.That(feedback, Is.Not.Null);
            feedback.UseImpactFx(null);
            Assert.That(feedback.ImpactFx.HasEffects, Is.False);

            var camera = Object.FindAnyObjectByType<FollowCamera>();
            var numbers = Object.FindAnyObjectByType<DamageNumbersView>();
            Assert.That(numbers, Is.Not.Null);
            float mostShake = 0f;
            int mostNumbers = 0;
            yield return HitWithTheTornado(() =>
            {
                mostShake = Mathf.Max(mostShake, camera.ShakeStrength);
                mostNumbers = Mathf.Max(mostNumbers, numbers.ActiveCount);
            });

            Assert.That(mostShake, Is.GreaterThan(0f), "without a library the hit did not shake the camera");
            Assert.That(mostNumbers, Is.GreaterThan(0), "without a library the hit showed no number");
            Assert.That(feedback.ImpactFx.PlayCount, Is.EqualTo(0));
            Assert.That(feedback.ImpactFx.InstanceCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Scene_WithAStandInLibrary_AHitPlaysItsEffect()
        {
            yield return LoadScene();
            var feedback = Object.FindAnyObjectByType<HitFeedbackView>();
            Assert.That(feedback, Is.Not.Null);
            feedback.UseImpactFx(_standIn);
            Assert.That(feedback.ImpactFx.InstanceCount, Is.EqualTo(HitCap + DeathCap));

            yield return HitWithTheTornado(null);

            Assert.That(feedback.ImpactFx.PlayCount, Is.GreaterThan(0), "the Tornado's hit played no impact effect");
            Assert.That(feedback.ImpactFx.InstanceCount, Is.EqualTo(HitCap + DeathCap), "play made new instances");
        }

        // An object with a short burst, inactive so it never plays by itself; the spawner's copies are made active. It outlives
        // the scene loads, as a prefab would.
        private static GameObject MakeParticleTemplate()
        {
            var template = new GameObject("Stand-in Burst");
            template.SetActive(false);
            Object.DontDestroyOnLoad(template);
            var system = template.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.3f;
            main.startLifetime = 0.3f;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 5) });
            return template;
        }

        private IEnumerator LoadScene()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                yield return null;
            }
        }

        // A stick stunned 3 m ahead of the core, then the Tornado, watching each frame until a few frames after its first hit.
        private static IEnumerator HitWithTheTornado(System.Action eachFrame)
        {
            var root = Object.FindAnyObjectByType<HiveDefenseRoot>();
            var core = Object.FindAnyObjectByType<SwarmCoreMotor>();
            Assert.That(root, Is.Not.Null);
            Assert.That(core, Is.Not.Null);
            HiveDefenseSession session = root.Session;
            session.BeekeeperSpawned += keeper => keeper.Stun(StunSeconds);
            root.CallWave();
            float waited = 0f;
            while (session.ActiveBeekeeperCount < 1 && waited < MaxWaveSeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(session.ActiveBeekeeperCount, Is.GreaterThan(0), "the wave brought no beekeeper");
            BeekeeperBrain stick = session.ActiveBeekeeper(0);
            stick.Spawn(core.Position + InFrontOfCore);
            stick.Stun(StunSeconds);

            int hits = 0;
            System.Action<SongHit> onLanded = hit => hits += hit.IsBlocked ? 0 : 1;
            session.SongLanded += onLanded;
            try
            {
                var keys = session.Caster[TornadoIndex(session)].Song.Keys;
                for (int key = 0; key < keys.Count; key++)
                {
                    root.PressHole(keys[key]);
                    yield return null;
                }

                yield return null;
                waited = 0f;
                while (hits == 0 && waited < MaxHitSeconds)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }

                Assert.That(hits, Is.GreaterThan(0), "the Tornado landed no hit");
                for (int frame = 0; frame < FramesAfterHit; frame++)
                {
                    eachFrame?.Invoke();
                    yield return null;
                }
            }
            finally
            {
                session.SongLanded -= onLanded;
            }
        }

        private static int TornadoIndex(HiveDefenseSession session)
        {
            for (int song = 0; song < session.Caster.Count; song++)
            {
                if (session.Caster[song] is TornadoSong)
                {
                    return song;
                }
            }

            Assert.Fail("The scene has no TornadoSong.");
            return -1;
        }
    }
}
#endif
