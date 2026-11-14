using System;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// Plays the optional recorded sounds of plan section AC from a <see cref="GameSoundLibrary"/>: the swarm's hum under
    /// everything, cross-fading to an angrier buzz while a strike runs or the giant stands, and a one-shot for each moment
    /// the library has a sound for. Every voice is made when the player is made, at load (each moment's own sources, as many
    /// as its cap), and every clip's data is loaded then, so play never adds a component, instantiates or allocates. A moment
    /// shows at most its cap at once, a new one taking the voice that started longest ago (<see cref="ImpactFxPool"/>), never
    /// closer than its least gap; its clip is chosen at random but never the last one again (<see cref="ClipChoice"/>); a kill
    /// climbs the kill chain's ladder; a big moment ducks the hum (<see cref="SoundDuck"/>). The root makes it only when a
    /// library with sounds is there; without one the game's own synthesised sounds play, as before.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Game Sound Player")]
    public sealed class GameSoundPlayer : MonoBehaviour
    {
        private static readonly int EventCount = Enum.GetValues(typeof(GameSoundEvent)).Length;

        private HiveDefenseSession _session;
        private HitFeedbackView _feedback;
        private GameSoundLibrary _library;
        private IRandomSource _random;
        private Moment[] _moments;
        private AudioSource _calmLoop;
        private AudioSource _angryLoop;
        private float _calmLoopVolume;
        private float _angryLoopVolume;
        private SwarmHumMix _hum;
        private SoundDuck _duck;
        private Vector3 _lastCore;
        private bool _hasLastCore;
        private bool _wasColossus;
        private bool _isWaveClearedPending;
        private bool _isReady;

        /// <summary>How many one-shots have played since Initialize. For tests and the benchmark.</summary>
        public int PlayCount { get; private set; }

        /// <summary>Every AudioSource made at load, the loops included. For tests and the player log.</summary>
        public int SourceCount { get; private set; }

        /// <summary>Every clip the library gave, the loops included. For tests and the player log.</summary>
        public int ClipCount { get; private set; }

        /// <summary>How many moments have a recorded sound. For tests and the player log.</summary>
        public int SoundCount { get; private set; }

        /// <summary>The share of every recorded sound's volume the player's Effects setting lets through, 0 to 1.</summary>
        public float EffectsGain { get; private set; } = 1f;

        /// <summary>The hum's mix, for tests and debugging.</summary>
        public SwarmHumMix Hum => _hum;

        /// <summary>The ducking of the hum, for tests and debugging.</summary>
        public SoundDuck Duck => _duck;

        /// <summary>The calm hum's source, or null without one. For tests.</summary>
        public AudioSource CalmLoop => _calmLoop;

        /// <summary>
        /// Makes every voice for the library's sounds, loads every clip's data and starts the hum. Call once, at load; the
        /// root makes a new player for a new library.
        /// </summary>
        /// <exception cref="ArgumentNullException">An argument is missing.</exception>
        public void Initialize(HiveDefenseSession session, HitFeedbackView feedback, GameSoundLibrary library, IRandomSource random)
        {
            if (_isReady)
            {
                throw new InvalidOperationException("GameSoundPlayer.Initialize runs once; make a new player for a new library.");
            }

            _session = session ?? throw new ArgumentNullException(nameof(session));
            _feedback = feedback != null ? feedback : throw new ArgumentNullException(nameof(feedback));
            _library = library != null ? library : throw new ArgumentNullException(nameof(library));
            _random = random ?? throw new ArgumentNullException(nameof(random));

            _hum = new SwarmHumMix(
                library.HumFullWasps, library.HumQuietShare, library.HumRestPitch, library.HumFastPitch,
                library.HumFastMetresPerSecond, library.HumFollowSeconds, library.AngryFadeSeconds);
            _duck = new SoundDuck(library.DuckDecibels, library.DuckHoldSeconds, library.DuckAttackSeconds, library.DuckReleaseSeconds);
            MakeMoments();
            _calmLoop = MakeLoop(GameSoundEvent.SwarmLoop, out _calmLoopVolume);
            _angryLoop = MakeLoop(GameSoundEvent.SwarmAngry, out _angryLoopVolume);
            _wasColossus = session.IsColossusActive;
            Listen();
            _isReady = true;
        }

        /// <summary>True when the library has a sound for the moment, which then replaces the game's own.</summary>
        public bool Has(GameSoundEvent sound)
        {
            if (_moments == null)
            {
                return false;
            }

            if (GameSoundChoice.IsLoop(sound))
            {
                return sound == GameSoundEvent.SwarmLoop ? _calmLoop != null : _angryLoop != null;
            }

            return _moments[(int)sound] != null;
        }

        /// <summary>True when the song's cast has a recorded sound, which then replaces the game's cast tone.</summary>
        public bool HasCastSound(int song)
        {
            return _session != null && song >= 0 && song < _session.Caster.Count
                && GameSoundChoice.ForCast(_session.Caster[song], out GameSoundEvent sound) && Has(sound);
        }

        /// <summary>
        /// Plays a moment's sound at its own pitch, varied by its jitter, unless its cap and least gap refuse it or it has
        /// none. Returns true when it played.
        /// </summary>
        public bool Play(GameSoundEvent sound) => Play(sound, 1f);

        /// <summary>Plays a moment's sound at a playback rate, varied by its jitter. Returns true when it played.</summary>
        public bool Play(GameSoundEvent sound, float rate)
        {
            if (!_isReady || GameSoundChoice.IsLoop(sound))
            {
                return false;
            }

            Moment moment = _moments[(int)sound];
            if (moment == null)
            {
                return false;
            }

            int voice = moment.Voices.Claim(Time.unscaledTime);
            if (voice == ImpactFxPool.None)
            {
                return false;
            }

            AudioSource source = moment.Sources[voice];
            source.Stop();
            source.clip = moment.Clips[moment.Choice.Next()];
            source.pitch = SoundPitch.Varied(rate, moment.Slot.PitchJitter, _random.NextDouble());
            source.volume = moment.Slot.Volume * _library.MasterVolume * EffectsGain;
            source.Play();
            PlayCount++;
            if (GameSoundChoice.IsBig(sound))
            {
                _duck.Trigger();
            }

            return true;
        }

        /// <summary>Sets the share of every recorded sound's volume to let through, 0 to 1 (the Effects setting).</summary>
        public void SetEffectsGain(float gain)
        {
            EffectsGain = Mathf.Clamp01(gain);
        }

        /// <summary>Stops listening and every sound. The root calls it before it destroys the player.</summary>
        public void Shutdown()
        {
            Release();
            if (_moments != null)
            {
                for (int index = 0; index < _moments.Length; index++)
                {
                    Moment moment = _moments[index];
                    for (int voice = 0; moment != null && voice < moment.Sources.Length; voice++)
                    {
                        moment.Sources[voice].Stop();
                    }
                }
            }

            if (_calmLoop != null)
            {
                _calmLoop.Stop();
            }

            if (_angryLoop != null)
            {
                _angryLoop.Stop();
            }
        }

        private void Update()
        {
            if (!_isReady)
            {
                return;
            }

            StepMoments();
            StepHum(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            Release();
        }

        private void MakeMoments()
        {
            _moments = new Moment[EventCount];
            for (int index = 0; index < EventCount; index++)
            {
                var sound = (GameSoundEvent)index;
                GameSoundSlot slot = _library.Slot(sound);
                if (!slot.IsSet)
                {
                    continue;
                }

                SoundCount++;
                AudioClip[] clips = LoadedClips(slot);
                if (GameSoundChoice.IsLoop(sound))
                {
                    continue; // MakeLoop makes the hum's own sources.
                }

                if (slot.MostAtOnce < 1)
                {
                    Debug.LogError($"{_library.name}: the {sound} sound needs at least one voice (it has {slot.MostAtOnce}), so it plays nothing.", _library);
                    continue;
                }

                var sources = new AudioSource[slot.MostAtOnce];
                for (int voice = 0; voice < sources.Length; voice++)
                {
                    sources[voice] = MakeSource(slot.Priority, false);
                }

                _moments[index] = new Moment(slot, clips, sources, new ImpactFxPool(slot.MostAtOnce, slot.LeastGapSeconds), new ClipChoice(clips.Length, _random));
            }
        }

        // The slot's clips without empty entries, each with its data loaded now, so its first play is not a slow frame.
        private AudioClip[] LoadedClips(GameSoundSlot slot)
        {
            int count = 0;
            for (int index = 0; index < slot.Clips.Length; index++)
            {
                count += slot.Clips[index] != null ? 1 : 0;
            }

            var clips = new AudioClip[count];
            int next = 0;
            for (int index = 0; index < slot.Clips.Length; index++)
            {
                AudioClip clip = slot.Clips[index];
                if (clip == null)
                {
                    continue;
                }

                clip.LoadAudioData();
                clips[next++] = clip;
            }

            ClipCount += count;
            return clips;
        }

        private AudioSource MakeLoop(GameSoundEvent sound, out float volume)
        {
            GameSoundSlot slot = _library.Slot(sound);
            volume = slot.Volume;
            if (!slot.IsSet)
            {
                return null;
            }

            AudioClip[] clips = LoadedClips(slot);
            ClipCount -= clips.Length; // Counted once already, in MakeMoments.
            AudioSource source = MakeSource(slot.Priority, true);
            source.clip = clips[new ClipChoice(clips.Length, _random).Next()];
            source.volume = 0f;
            source.Play();
            return source;
        }

        private AudioSource MakeSource(int priority, bool isLoop)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = isLoop;
            source.spatialBlend = 0f;
            source.priority = Mathf.Clamp(priority, 0, 256);
            SourceCount++;
            return source;
        }

        private void Listen()
        {
            _session.SongCastNow += OnSongCastNow;
            _session.SongLanded += OnSongLanded;
            _session.Forms.Landed += OnFormLanded;
            _session.ColossusAttackStarted += OnColossusStarted;
            _session.ColossusAttackLanded += OnColossusLanded;
            _session.ColossusFistLanded += OnColossusFistLanded;
            _session.Caster.SongNotReady += OnSongNotReady;
            _session.Nests.ComboNote += OnComboNote;
            _session.Nests.NestCollected += OnNestCollected;
            _session.Waves.PhaseChanged += OnPhaseChanged;
            _session.Waves.WaveCleared += OnWaveCleared;
            _session.Hives.HiveDestroyed += OnHiveDestroyed;
            _session.StateChanged += OnStateChanged;
            for (int index = 0; index < _session.Hives.Count; index++)
            {
                _session.Hives[index].Damaged += OnHiveDamaged;
            }

            _feedback.KillChained += OnKillChained;
        }

        private void Release()
        {
            if (!_isReady)
            {
                return;
            }

            _isReady = false;
            _session.SongCastNow -= OnSongCastNow;
            _session.SongLanded -= OnSongLanded;
            _session.Forms.Landed -= OnFormLanded;
            _session.ColossusAttackStarted -= OnColossusStarted;
            _session.ColossusAttackLanded -= OnColossusLanded;
            _session.ColossusFistLanded -= OnColossusFistLanded;
            _session.Caster.SongNotReady -= OnSongNotReady;
            _session.Nests.ComboNote -= OnComboNote;
            _session.Nests.NestCollected -= OnNestCollected;
            _session.Waves.PhaseChanged -= OnPhaseChanged;
            _session.Waves.WaveCleared -= OnWaveCleared;
            _session.Hives.HiveDestroyed -= OnHiveDestroyed;
            _session.StateChanged -= OnStateChanged;
            for (int index = 0; index < _session.Hives.Count; index++)
            {
                _session.Hives[index].Damaged -= OnHiveDamaged;
            }

            if (_feedback != null)
            {
                _feedback.KillChained -= OnKillChained;
            }
        }

        // The moments read from the session's state: the giant forming and ending, and a wave cleared that did not win.
        private void StepMoments()
        {
            bool isColossus = _session.IsColossusActive && _session.State == HiveDefenseState.Playing;
            if (isColossus != _wasColossus)
            {
                Play(isColossus ? GameSoundEvent.GiantForm : GameSoundEvent.GiantEnd);
                _wasColossus = isColossus;
            }

            if (_isWaveClearedPending)
            {
                _isWaveClearedPending = false;
                if (_session.State == HiveDefenseState.Playing)
                {
                    Play(GameSoundEvent.WaveCleared);
                }
            }
        }

        private void StepHum(float deltaTime)
        {
            Vector3 core = _session.CorePosition;
            float gameSeconds = Time.deltaTime;
            float speed = 0f;
            if (_hasLastCore && gameSeconds > 0f)
            {
                Vector3 moved = core - _lastCore;
                moved.y = 0f;
                speed = moved.magnitude / gameSeconds;
            }

            _lastCore = core;
            _hasLastCore = true;

            SlotState strike = _session.Forms.Slot(SongSlot.Strike);
            bool isAngry = (strike.IsActive && strike.ReleasedSeconds < 0f) || _session.IsColossusActive;
            // The hum fades away while the match is over or paused (the settings panel), and swells back after.
            bool isRunning = _session.State == HiveDefenseState.Playing && Time.timeScale > 0f;
            int wasps = isRunning ? _session.Swarm.Count : 0;
            _hum.Step(wasps, speed, isAngry && _angryLoop != null, deltaTime);
            float duck = _duck.Step(deltaTime);
            float shared = _library.MasterVolume * EffectsGain * duck;

            // Without an angry loop the calm hum carries the whole volume; without a calm one the angry buzz is all there is.
            if (_calmLoop != null)
            {
                _calmLoop.volume = (_angryLoop != null ? _hum.CalmVolume : _hum.Volume) * _calmLoopVolume * shared;
                _calmLoop.pitch = _hum.Pitch;
            }

            if (_angryLoop != null)
            {
                _angryLoop.volume = _hum.AngryVolume * _angryLoopVolume * shared;
                _angryLoop.pitch = _hum.Pitch;
            }
        }

        private void OnSongCastNow(int song)
        {
            if (GameSoundChoice.ForCast(_session.Caster[song], out GameSoundEvent sound))
            {
                Play(sound);
            }
        }

        // A kill sounds through the kill chain (OnKillChained), so its pitch climbs; here only hits that do not kill.
        private void OnSongLanded(SongHit hit)
        {
            if (!hit.IsKill && GameSoundChoice.ForHit(in hit, out GameSoundEvent sound))
            {
                Play(sound);
            }
        }

        private void OnKillChained(KillChainStep step)
        {
            Play(GameSoundEvent.BeekeeperDeath, SoundPitch.OfSemitones(step.Semitones));
        }

        private void OnFormLanded(SwarmForm form, Vector3 point)
        {
            if (GameSoundChoice.ForFormLanded(form, out GameSoundEvent sound))
            {
                Play(sound);
            }
        }

        private void OnColossusStarted(ColossusAttack attack)
        {
            if (GameSoundChoice.ForColossusStarted(attack, out GameSoundEvent sound))
            {
                Play(sound);
            }
        }

        private void OnColossusLanded(ColossusAttack attack)
        {
            if (GameSoundChoice.ForColossusLanded(attack, out GameSoundEvent sound))
            {
                Play(sound);
            }
        }

        private void OnColossusFistLanded(ColossusHand hand)
        {
            Play(GameSoundEvent.GiantPunch);
        }

        private void OnSongNotReady(int song, SongRefusal refusal)
        {
            Play(GameSoundEvent.SongFailed);
        }

        private void OnComboNote(int nest, NestComboStep step)
        {
            if (GameSoundChoice.ForComboNote(step, out GameSoundEvent sound))
            {
                Play(sound);
            }
        }

        private void OnNestCollected(int nest)
        {
            Play(GameSoundEvent.NestCollected);
        }

        private void OnPhaseChanged(MatchPhase phase)
        {
            if (phase == MatchPhase.Survive)
            {
                Play(GameSoundEvent.WaveStart);
            }
        }

        // The last wave's clearing is the win, whose own stinger plays instead: decided once the frame's state has settled.
        private void OnWaveCleared(int round)
        {
            _isWaveClearedPending = true;
        }

        private void OnHiveDamaged(Hive hive)
        {
            Play(GameSoundEvent.HiveHit);
        }

        private void OnHiveDestroyed(int hive)
        {
            Play(GameSoundEvent.HiveLost);
        }

        private void OnStateChanged(HiveDefenseState state)
        {
            if (state == HiveDefenseState.Won)
            {
                Play(GameSoundEvent.Win);
            }
            else if (state == HiveDefenseState.Lost)
            {
                Play(GameSoundEvent.Lose);
            }
        }

        // One moment's voices: made at load, each a source of its own, with its cap, its least gap and its clip choice.
        private sealed class Moment
        {
            public Moment(GameSoundSlot slot, AudioClip[] clips, AudioSource[] sources, ImpactFxPool voices, ClipChoice choice)
            {
                Slot = slot;
                Clips = clips;
                Sources = sources;
                Voices = voices;
                Choice = choice;
            }

            public GameSoundSlot Slot { get; }
            public AudioClip[] Clips { get; }
            public AudioSource[] Sources { get; }
            public ImpactFxPool Voices { get; }
            public ClipChoice Choice { get; }
        }
    }
}
