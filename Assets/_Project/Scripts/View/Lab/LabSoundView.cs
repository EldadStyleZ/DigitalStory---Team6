using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Plays the lab's sound effects, baked from data.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("DigitalStory/Lab/Lab Sound View")]
    public sealed class LabSoundView : MonoBehaviour
    {
        private readonly Dictionary<SoundCue, AudioClip> _clips = new Dictionary<SoundCue, AudioClip>();

        private LabSession _session;
        private LabSoundConfig _sound;
        private AudioSource _source;

        /// <summary>The last sound played, for tests and debugging.</summary>
        public SoundCue LastPlayed { get; private set; }

        /// <summary>Supplies what to play. Call once, before this object starts.</summary>
        public void Initialize(LabSession session, LabSoundConfig sound)
        {
            _session = session;
            _sound = sound;
        }

        private void Awake() => _source = GetComponent<AudioSource>();

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
            _session.Summoned += HandleSummoned;
            _session.ComboFailed += HandleComboFailed;
            _session.SummonRefused += HandleRefused;
            _session.PiperDied += HandlePiperDied;
            _session.Won += HandleWon;
            _session.Levels.LevelledUp += HandleLevelledUp;
            _session.Battle.Struck += HandleStruck;
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
            _session.Summoned -= HandleSummoned;
            _session.ComboFailed -= HandleComboFailed;
            _session.SummonRefused -= HandleRefused;
            _session.PiperDied -= HandlePiperDied;
            _session.Won -= HandleWon;
            _session.Levels.LevelledUp -= HandleLevelledUp;
            _session.Battle.Struck -= HandleStruck;
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
                float[] samples = ToneBaker.Bake(cue, sampleRate);
                var clip = AudioClip.Create("Generated Sound", samples.Length, 1, sampleRate, false);
                clip.SetData(samples, 0);
                _clips[cue] = clip;
            }
        }

        private void ForgetEveryClip()
        {
            foreach (AudioClip clip in _clips.Values)
            {
                Destroy(clip);
            }

            _clips.Clear();
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

        private void HandleSummoned(CreatureType creature) => Play(_sound.Summoned);

        private void HandleComboFailed() => Play(_sound.Failed);

        private void HandleRefused(SummonRefusal refusal) => Play(_sound.Refused);

        private void HandlePiperDied() => Play(_sound.Died);

        private void HandleWon() => Play(_sound.Won);

        private void HandleLevelledUp(int level) => Play(_sound.LevelUp);

        private void HandleStruck(StrikeReport strike) => Play(strike.AreaRadius > 0f ? _sound.AreaHit : _sound.Hit);
    }
}
