using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Plays the lab's sound effects and each combo's tune, all baked from data.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("DigitalStory/Lab/Lab Sound View")]
    public sealed class LabSoundView : MonoBehaviour
    {
        private readonly Dictionary<SoundCue, AudioClip> _clips = new Dictionary<SoundCue, AudioClip>();
        private readonly Dictionary<IComboSong, AudioClip> _songs = new Dictionary<IComboSong, AudioClip>();

        private LabSession _session;
        private LabSoundConfig _sound;
        private AudioSource _source;
        private AudioSource _songSource;
        private float _nextHitAt;

        /// <summary>The last sound played, for tests and debugging.</summary>
        public SoundCue LastPlayed { get; private set; }

        /// <summary>How many hit sounds have played, for tests and debugging.</summary>
        public int HitSoundsPlayed { get; private set; }

        /// <summary>The last combo's tune, for tests and debugging.</summary>
        public IComboSong LastSong { get; private set; }

        /// <summary>True while a combo's tune plays, for tests and debugging.</summary>
        public bool IsPlayingASong => _songSource != null && _songSource.isPlaying;

        /// <summary>Supplies what to play. Call once, before this object starts.</summary>
        public void Initialize(LabSession session, LabSoundConfig sound)
        {
            _session = session;
            _sound = sound;
        }

        // A tune gets its own source, so a new one can cut the last one off without silencing the fight.
        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _songSource = gameObject.AddComponent<AudioSource>();
            _songSource.playOnAwake = false;
        }

        private void Start()
        {
            // LabRoot has already reported the problem if nothing was supplied.
            if (_session == null)
            {
                return;
            }

            BakeEveryCue();
            AudioSettings.OnAudioConfigurationChanged += HandleAudioChanged;
            _session.NotePlayed += HandleNotePlayed;
            _session.ComboPlayed += HandleComboPlayed;
            _session.ComboFailed += HandleComboFailed;
            _session.SummonRefused += HandleRefused;
            _session.PiperDied += HandlePiperDied;
            _session.Won += HandleWon;
            _session.Levels.LevelledUp += HandleLevelledUp;
            _session.Battle.Struck += HandleStruck;
            _session.Ambushes.Warned += HandleAmbushWarned;
            _session.Ambushes.Sprang += HandleAmbushSprang;
        }

        private void OnDestroy()
        {
            // Unity also calls this when the scene unloads before Start ran, so nothing may be baked yet.
            if (_clips.Count == 0)
            {
                return;
            }

            AudioSettings.OnAudioConfigurationChanged -= HandleAudioChanged;
            _session.NotePlayed -= HandleNotePlayed;
            _session.ComboPlayed -= HandleComboPlayed;
            _session.ComboFailed -= HandleComboFailed;
            _session.SummonRefused -= HandleRefused;
            _session.PiperDied -= HandlePiperDied;
            _session.Won -= HandleWon;
            _session.Levels.LevelledUp -= HandleLevelledUp;
            _session.Battle.Struck -= HandleStruck;
            _session.Ambushes.Warned -= HandleAmbushWarned;
            _session.Ambushes.Sprang -= HandleAmbushSprang;
            ForgetEveryClip();
        }

        private void BakeEveryCue()
        {
            int sampleRate = AudioSettings.outputSampleRate;

            // A machine with no audio device reports no rate, and the game simply plays without sound.
            if (sampleRate <= 0)
            {
                return;
            }

            foreach (SoundCue cue in _sound.EveryCue)
            {
                _clips[cue] = NewClip(cue, sampleRate);
            }

            foreach (IComboSong song in _session.Songs)
            {
                _songs[song] = NewClip(ComboSongWriter.Write(song, _sound.Band), sampleRate);
            }
        }

        private static AudioClip NewClip(SoundCue cue, int sampleRate)
        {
            float[] samples = ToneBaker.Bake(cue, sampleRate);
            var clip = AudioClip.Create("Generated Sound", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void ForgetEveryClip()
        {
            foreach (AudioClip clip in _clips.Values)
            {
                Destroy(clip);
            }

            foreach (AudioClip clip in _songs.Values)
            {
                Destroy(clip);
            }

            _clips.Clear();
            _songs.Clear();
        }

        // Changing device empties clips made in code, and an empty clip plays silence.
        private void HandleAudioChanged(bool deviceWasChanged)
        {
            ForgetEveryClip();
            BakeEveryCue();
        }

        private void Play(SoundCue cue)
        {
            LastPlayed = cue;

            if (_clips.TryGetValue(cue, out AudioClip clip))
            {
                _source.PlayOneShot(clip);
            }
        }

        private void HandleNotePlayed(int note) => Play(_sound.Notes[note]);

        private void HandleComboPlayed(IComboSong song)
        {
            LastSong = song;

            if (_songs.TryGetValue(song, out AudioClip clip))
            {
                _songSource.clip = clip;
                _songSource.Play();
            }
        }

        private void HandleComboFailed() => Play(_sound.Failed);

        private void HandleRefused(SummonRefusal refusal) => Play(_sound.Refused);

        private void HandlePiperDied() => Play(_sound.Died);

        private void HandleWon() => Play(_sound.Won);

        private void HandleLevelledUp(int level) => Play(_sound.LevelUp);

        // A gang lands many hits at once, and every one of them would add up to a roar over the music.
        private void HandleStruck(StrikeReport strike)
        {
            if (Time.time < _nextHitAt)
            {
                return;
            }

            _nextHitAt = Time.time + _sound.HitGapSeconds;
            HitSoundsPlayed++;
            Play(strike.AreaRadius > 0f ? _sound.AreaHit : _sound.Hit);
        }

        private void HandleAmbushWarned(Vector2 position) => Play(_sound.AmbushWarning);

        private void HandleAmbushSprang(Vector2 position) => Play(_sound.AmbushSprung);
    }
}
