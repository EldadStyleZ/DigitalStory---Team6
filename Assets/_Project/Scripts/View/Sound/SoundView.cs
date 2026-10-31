using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Plays the sound effects that follow what happens in the night.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("DigitalStory/Sound View")]
    public sealed class SoundView : MonoBehaviour
    {
        private readonly Dictionary<SoundCue, AudioClip> _clips = new Dictionary<SoundCue, AudioClip>();
        private readonly HashSet<SoundCue> _played = new HashSet<SoundCue>();

        private NightSession _session;
        private GameFlow _flow;
        private SoundConfig _sound;
        private AudioSource _source;
        private float _nextSqueak;
        private float _nextSplash;
        private float _beatWait;
        private int _beatCount;
        private bool _wasDisco;

        /// <summary>How many sounds have played, for tests and debugging.</summary>
        public int PlayCount { get; private set; }

        /// <summary>The last sound played, for tests and debugging.</summary>
        public SoundCue LastPlayed { get; private set; }

        /// <summary>True once a sound has played, for tests and debugging.</summary>
        public bool HasPlayed(SoundCue cue) => _played.Contains(cue);

        /// <summary>Supplies what to follow. Call once, before this object is enabled.</summary>
        public void Initialize(NightSession session, GameFlow flow, SoundConfig sound)
        {
            _session = session;
            _flow = flow;
            _sound = sound;
        }

        private void Awake() => _source = GetComponent<AudioSource>();

        private void OnEnable()
        {
            if (_session == null)
            {
                return;
            }

            AudioSettings.OnAudioConfigurationChanged += HandleAudioChanged;
            _flow.Changed += HandleFlowChanged;
            _session.SongCast += HandleSongCast;
            _session.Swarm.RatDrowned += HandleRatDrowned;
            _session.Swarm.RatEaten += HandleRatEaten;
            _session.Swarm.RatCharmed += HandleRatCharmed;
            _session.Swarm.TranceEnded += HandleTranceEnded;
        }

        private void Start()
        {
            // GameRoot has already reported the problem if nothing was supplied.
            if (_session != null)
            {
                BakeEveryCue();
            }
        }

        private void Update()
        {
            if (_session == null || !_flow.IsPlaying)
            {
                return;
            }

            bool isDisco = _session.Stage.IsDiscoing;

            // Every Disco starts on the same beat, however the last dance left the count.
            if (isDisco && !_wasDisco)
            {
                _beatCount = 0;
                _beatWait = 0f;
            }

            _wasDisco = isDisco;

            if (!isDisco && !_session.Stage.IsDancingConga)
            {
                return;
            }

            _beatWait -= Time.deltaTime;

            if (_beatWait <= 0f)
            {
                _beatCount++;
                _beatWait = isDisco ? _sound.DiscoBeatSeconds : _sound.CongaBeatSeconds;
                PlayBeat(isDisco);
            }
        }

        private void OnDisable()
        {
            // Unity also calls this when the scene unloads before Initialize ran, so _session can be null.
            if (_session == null)
            {
                return;
            }

            AudioSettings.OnAudioConfigurationChanged -= HandleAudioChanged;
            _flow.Changed -= HandleFlowChanged;
            _session.SongCast -= HandleSongCast;
            _session.Swarm.RatDrowned -= HandleRatDrowned;
            _session.Swarm.RatEaten -= HandleRatEaten;
            _session.Swarm.RatCharmed -= HandleRatCharmed;
            _session.Swarm.TranceEnded -= HandleTranceEnded;
        }

        private void OnDestroy()
        {
            ForgetEveryClip();
        }

        private void ForgetEveryClip()
        {
            // Clips made in code are not freed with the scene.
            foreach (AudioClip clip in _clips.Values)
            {
                Destroy(clip);
            }

            _clips.Clear();
        }

        private void BakeEveryCue()
        {
            SoundCue[] cues =
            {
                _sound.Eaten, _sound.Pop, _sound.SongChime, _sound.Won, _sound.Lost,
                _sound.DiscoKick, _sound.DiscoTick, _sound.CongaBeat, _sound.CongaAccent,
            };

            foreach (SoundCue cue in cues)
            {
                Bake(cue);
            }

            foreach (SoundCue squeak in _sound.Squeaks)
            {
                Bake(squeak);
            }

            foreach (SoundCue splash in _sound.Splashes)
            {
                Bake(splash);
            }

            for (int song = 0; song < _session.Songs.Count; song++)
            {
                Bake(_session.JingleOf(song));
            }
        }

        private void Bake(SoundCue cue)
        {
            int sampleRate = AudioSettings.outputSampleRate;

            // A machine with no audio device reports no rate, and the game simply plays without sound.
            if (cue.Tones.Count == 0 || sampleRate <= 0)
            {
                return;
            }

            float[] samples = ToneBaker.Bake(cue, sampleRate);
            var clip = AudioClip.Create("Generated Sound", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            _clips[cue] = clip;
        }

        private void Play(SoundCue cue)
        {
            if (_clips.TryGetValue(cue, out AudioClip clip))
            {
                _source.PlayOneShot(clip);
                PlayCount++;
                LastPlayed = cue;
                _played.Add(cue);
            }
        }

        private void PlayBeat(bool isDisco)
        {
            if (isDisco)
            {
                Play(_sound.DiscoKick);

                if (_beatCount % 2 == 1)
                {
                    Play(_sound.DiscoTick);
                }

                return;
            }

            bool isLastBeatOfTheBar = _beatCount % _sound.CongaBeatsPerBar == _sound.CongaBeatsPerBar - 1;
            Play(isLastBeatOfTheBar ? _sound.CongaAccent : _sound.CongaBeat);
        }

        private void PlaySqueak()
        {
            if (Time.time >= _nextSqueak)
            {
                _nextSqueak = Time.time + _sound.SqueakGapSeconds;
                Play(OneOf(_sound.Squeaks));
            }
        }

        private static SoundCue OneOf(IReadOnlyList<SoundCue> cues) => cues[Random.Range(0, cues.Count)];

        // Changing device, such as plugging in headphones, loses clips made in code.
        private void HandleAudioChanged(bool deviceWasChanged)
        {
            ForgetEveryClip();
            BakeEveryCue();
        }

        private void HandleFlowChanged(GamePhase phase)
        {
            if (phase == GamePhase.Ended)
            {
                Play(_session.Outcome == NightOutcome.Won ? _sound.Won : _sound.Lost);
            }
        }

        private void HandleSongCast(int song)
        {
            Play(_sound.SongChime);
            Play(_session.JingleOf(song));
        }

        private void HandleRatDrowned(Vector2 position)
        {
            if (Time.time >= _nextSplash)
            {
                _nextSplash = Time.time + _sound.SplashGapSeconds;
                Play(OneOf(_sound.Splashes));
            }
        }

        private void HandleRatEaten(Vector2 position) => Play(_sound.Eaten);

        private void HandleRatCharmed(Vector2 position) => PlaySqueak();

        private void HandleTranceEnded(Vector2 position, TranceEnding ending)
        {
            if (ending == TranceEnding.Popped)
            {
                Play(_sound.Pop);
            }
            else if (ending == TranceEnding.Woke)
            {
                PlaySqueak();
            }
        }
    }
}
