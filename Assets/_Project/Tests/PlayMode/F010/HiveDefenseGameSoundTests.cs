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
    /// Plan section AC, the optional recorded sounds. They must work for a teammate without the paid packs, so nothing here
    /// refers to them: in the real scene without a library no player is made and the HUD's synthesised sounds play as before;
    /// with a stand-in library of clips made here, every voice is made at load and never one more, a moment with a recorded
    /// sound no longer plays the synthesised one (nothing doubles), a moment's cap holds however fast it is asked for, and the
    /// hum plays under the swarm.
    /// </summary>
    public sealed class HiveDefenseGameSoundTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const int FramesToSettle = 3;
        private const float FrameSeconds = 1f / 30f;
        private const float StunSeconds = 60f;
        private const float MaxWaveSeconds = 60f;
        private const float MaxHitSeconds = 1.5f;
        private const int FramesAfterHit = 3;
        private const int CastCap = 2;
        private const int HitCap = 3;
        private const int DeathCap = 2;
        private const int Plays = 10;
        private const int Rate = 48000;
        private static readonly Vector3 InFrontOfCore = new Vector3(0f, 0f, 3f);

        private float _savedTimeScale;
        private float _savedCaptureDeltaTime;
        private AudioClip _shortClip;
        private AudioClip _otherClip;
        private AudioClip _loopClip;
        private GameSoundLibrary _standIn;

        [UnitySetUp]
        public IEnumerator Prepare()
        {
            _savedTimeScale = Time.timeScale;
            _savedCaptureDeltaTime = Time.captureDeltaTime;
            Time.timeScale = 1f;
            Time.captureDeltaTime = FrameSeconds;
            _shortClip = MakeClip("Stand-in Short", 0.5f, 440f);
            _otherClip = MakeClip("Stand-in Other", 0.5f, 660f);
            _loopClip = MakeClip("Stand-in Loop", 1f, 110f);
            _standIn = ScriptableObject.CreateInstance<GameSoundLibrary>();
            _standIn.name = "Stand-in Sound Library";
            _standIn.SetSlot(GameSoundEvent.CastTornado, new GameSoundSlot(new[] { _shortClip }, 0.4f, 0.03f, CastCap, 0f, 30));
            _standIn.SetSlot(GameSoundEvent.BeekeeperHit, new GameSoundSlot(new[] { _shortClip, _otherClip }, 0.3f, 0.05f, HitCap, 0f, 80));
            _standIn.SetSlot(GameSoundEvent.BeekeeperDeath, new GameSoundSlot(new[] { _otherClip }, 0.4f, 0.05f, DeathCap, 0f, 60));
            _standIn.SetSlot(GameSoundEvent.SwarmLoop, new GameSoundSlot(new[] { _loopClip }, 0.12f, 0f, 1, 0f, 0));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = _savedTimeScale;
            Time.captureDeltaTime = _savedCaptureDeltaTime;
            Object.Destroy(_standIn);
            Object.Destroy(_shortClip);
            Object.Destroy(_otherClip);
            Object.Destroy(_loopClip);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Scene_WithoutALibrary_MakesNoPlayer_AndTheHudsOwnSoundsPlay()
        {
            yield return LoadScene();
            HiveDefenseRoot root = Object.FindAnyObjectByType<HiveDefenseRoot>();
            root.UseRecordedSounds(null);
            HitSoundView hitSound = Object.FindAnyObjectByType<HitSoundView>();
            Assert.That(hitSound, Is.Not.Null);
            int before = hitSound.PlayedCount;

            yield return HitWithTheTornado(null);

            Assert.That(root.GameSound, Is.Null, "a player was made without a library");
            Assert.That(Object.FindAnyObjectByType<GameSoundPlayer>(), Is.Null);
            Assert.That(hitSound.PlayedCount, Is.GreaterThan(before), "without a library the cast and the hit played no sound");
        }

        [UnityTest]
        public IEnumerator Scene_WithAStandIn_MakesEveryVoiceAtLoad_AndReplacesTheHudsSounds_WithoutDoubling()
        {
            yield return LoadScene();
            HiveDefenseRoot root = Object.FindAnyObjectByType<HiveDefenseRoot>();
            root.UseRecordedSounds(_standIn);
            GameSoundPlayer player = root.GameSound;
            Assert.That(player, Is.Not.Null);
            int voices = CastCap + HitCap + DeathCap + 1;
            Assert.That(player.SourceCount, Is.EqualTo(voices), "one source per voice of every moment, and one for the hum");
            Assert.That(player.GetComponents<AudioSource>().Length, Is.EqualTo(voices));
            Assert.That(player.ClipCount, Is.EqualTo(5));
            Assert.That(player.SoundCount, Is.EqualTo(4));
            HitSoundView hitSound = Object.FindAnyObjectByType<HitSoundView>();
            int synthesised = hitSound.PlayedCount;

            yield return HitWithTheTornado(null);

            Assert.That(player.PlayCount, Is.GreaterThan(0), "the cast and its hit played no recorded sound");
            Assert.That(hitSound.PlayedCount, Is.EqualTo(synthesised), "the synthesised cast, hit or kill sound played as well");
            Assert.That(player.GetComponents<AudioSource>().Length, Is.EqualTo(voices), "play made new sources");
        }

        [UnityTest]
        public IEnumerator Player_ASoundAskedForTenTimesAtOnce_NeverSoundsMoreThanItsCap()
        {
            yield return LoadScene();
            HiveDefenseRoot root = Object.FindAnyObjectByType<HiveDefenseRoot>();
            root.UseRecordedSounds(_standIn);
            GameSoundPlayer player = root.GameSound;

            for (int play = 0; play < Plays; play++)
            {
                Assert.That(player.Play(GameSoundEvent.BeekeeperHit), Is.True);
            }

            Assert.That(player.Play(GameSoundEvent.GiantSlam), Is.False, "a moment without a sound played");
            Assert.That(player.Play(GameSoundEvent.SwarmLoop), Is.False, "the hum played as a one-shot");
            Assert.That(player.PlayCount, Is.EqualTo(Plays));
            int playing = 0;
            foreach (AudioSource source in player.GetComponents<AudioSource>())
            {
                playing += source.isPlaying && source.clip != _loopClip ? 1 : 0;
            }

            Assert.That(playing, Is.LessThanOrEqualTo(HitCap));
            Assert.That(player.Has(GameSoundEvent.BeekeeperHit), Is.True);
            Assert.That(player.Has(GameSoundEvent.GiantSlam), Is.False);
        }

        [UnityTest]
        public IEnumerator Player_TheHumPlaysUnderTheSwarm_AndABigMomentDucksIt()
        {
            yield return LoadScene();
            HiveDefenseRoot root = Object.FindAnyObjectByType<HiveDefenseRoot>();
            _standIn.SetSlot(GameSoundEvent.GiantSlam, new GameSoundSlot(new[] { _shortClip }, 0.5f, 0f, 1, 0f, 20));
            root.UseRecordedSounds(_standIn);
            GameSoundPlayer player = root.GameSound;

            // The hum eases in over real time (0.25 s to about two thirds): give it a second to settle.
            yield return new WaitForSecondsRealtime(1f);

            Assert.That(player.CalmLoop, Is.Not.Null);
            Assert.That(player.CalmLoop.loop, Is.True);
            float settled = player.CalmLoop.volume;
            Assert.That(settled, Is.GreaterThan(0f), "the swarm hums");

            Assert.That(player.Play(GameSoundEvent.GiantSlam), Is.True);

            // The duck runs on real time, which batch frames barely move: wait into its hold, well past its 30 ms dip.
            yield return new WaitForSecondsRealtime(0.12f);

            Assert.That(player.Duck.Gain, Is.LessThan(0.8f), "the slam did not duck the hum");
            Assert.That(player.CalmLoop.volume, Is.LessThan(settled));
        }

        private static AudioClip MakeClip(string name, float seconds, float hertz)
        {
            int count = (int)(seconds * Rate);
            var samples = new float[count];
            for (int index = 0; index < count; index++)
            {
                samples[index] = 0.2f * Mathf.Sin(2f * Mathf.PI * hertz * index / Rate);
            }

            AudioClip clip = AudioClip.Create(name, count, 1, Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static IEnumerator LoadScene()
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
