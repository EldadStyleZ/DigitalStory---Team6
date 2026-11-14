using System;
using System.Threading;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// Sounds the note of the hole the player just played, for as long as its key is held, in one of four synthesised voices
    /// (plan section AC): the flute, a kalimba-like pluck, a soft bell or a buzzing reed. The SoundConfig names the voice a
    /// match starts in, and <see cref="NextVoice"/> (F6 in the game) moves to the next one. A struck voice rings down on its
    /// own while the key is held and is damped once the note ends; the flute and the reed hold while it is held.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("DigitalStory/Swarm Flute Voice")]
    public sealed class SwarmFluteVoice : MonoBehaviour
    {
        // Every voice, numbered as NoteVoiceKind, as the HUD names them.
        private static readonly string[] VoiceTitles = { "Flute", "Kalimba", "Bell", "Reed hum" };

        // Seconds the music volume takes to follow a change, to about two thirds, so a moving slider never zippers.
        private const float GainFollowSeconds = 0.005f;

        // The voice chosen before a restart, kept for the scene that loads next (see CarryOverRestart).
        private static NoteVoiceKind? s_carriedVoice;

        private SoundConfig _sound;
        private CoreMoveConfig _move;
        private HiveDefenseInput _input;
        private AudioSource _source;
        private INoteSynth[] _synths;
        private int _buffersFilled;
        private float _secondsSounding;

        // The music volume: written by the main thread, read by the audio thread without a lock.
        private float _musicGainTarget = 1f;

        // The audio thread's own: the gain now, easing to the target sample by sample, and how fast.
        private float _musicGainNow = 1f;
        private float _musicGainStep = 1f;

        /// <summary>How many sound buffers have been filled, for tests and debugging.</summary>
        public int BuffersFilled => Volatile.Read(ref _buffersFilled);

        /// <summary>The hole being sounded, or <see cref="FluteSynth.Silent"/>. For tests and debugging.</summary>
        public int HolePlaying { get; private set; } = FluteSynth.Silent;

        /// <summary>The semitones the note now sounding is raised by, for tests and debugging.</summary>
        public float Lift { get; private set; }

        /// <summary>The voice the next note sounds in.</summary>
        public NoteVoiceKind Voice { get; private set; } = NoteVoiceKind.Pluck;

        /// <summary>The music volume the voice is heading for, 0 to 1 (plan section AC's settings).</summary>
        public float MusicGain => Volatile.Read(ref _musicGainTarget);

        /// <summary>
        /// Sets the music volume, 0 to 1. The audio thread reads it without a lock and eases to it over a few milliseconds,
        /// so a moving slider never zippers.
        /// </summary>
        public void SetMusicGain(float gain)
        {
            Volatile.Write(ref _musicGainTarget, Mathf.Clamp01(gain));
        }

        /// <summary>The voice's name, as the HUD shows it.</summary>
        public static string TitleOf(NoteVoiceKind voice)
        {
            int index = (int)voice;
            return index >= 0 && index < VoiceTitles.Length ? VoiceTitles[index] : voice.ToString();
        }

        /// <summary>
        /// Keeps the chosen voice for the scene that loads next, as a restart loads the scene again. The next voice to
        /// <see cref="Initialize"/> starts in it instead of the SoundConfig's.
        /// </summary>
        public static void CarryOverRestart(NoteVoiceKind voice)
        {
            s_carriedVoice = voice;
        }

        /// <summary>Supplies what to play. Call once, before this object starts.</summary>
        /// <exception cref="ArgumentNullException">A config is missing.</exception>
        public void Initialize(SoundConfig sound, CoreMoveConfig move)
        {
            _sound = sound != null ? sound : throw new ArgumentNullException(nameof(sound));
            _move = move != null ? move : throw new ArgumentNullException(nameof(move));
            Voice = s_carriedVoice ?? sound.NoteVoice;
            s_carriedVoice = null;
        }

        /// <summary>Optionally supplies the input, so a note lasts while its key is held. Call once.</summary>
        public void Connect(HiveDefenseInput input)
        {
            _input = input != null ? input : throw new ArgumentNullException(nameof(input));
        }

        /// <summary>Sounds a hole at its own pitch in the current voice, replacing any note now sounding. The hole counts from 0.</summary>
        public void PlayNote(int hole)
        {
            HolePlaying = hole;
            _secondsSounding = 0f;
            Lift = 0f;
            INoteSynth synth = CurrentSynth;
            synth?.Lift(0f);
            synth?.Play(hole);
        }

        /// <summary>
        /// Raises the note now sounding by <paramref name="semitones"/>, as a nest's combo climbs (plan section U). The next
        /// <see cref="PlayNote"/> plays at the hole's own pitch again.
        /// </summary>
        public void LiftNote(float semitones)
        {
            Lift = semitones;
            CurrentSynth?.Lift(semitones);
        }

        /// <summary>
        /// Moves to the next voice, after the last one back to the first, and returns it. The note now sounding is let go in
        /// its own voice, so it fades rather than stops; the next note sounds in the new one.
        /// </summary>
        public NoteVoiceKind NextVoice()
        {
            return UseVoice((NoteVoiceKind)(((int)Voice + 1) % VoiceTitles.Length));
        }

        /// <summary>Sounds the next notes in the given voice, letting the note now sounding go. Returns the voice.</summary>
        public NoteVoiceKind UseVoice(NoteVoiceKind voice)
        {
            CurrentSynth?.Play(FluteSynth.Silent);
            HolePlaying = FluteSynth.Silent;
            Voice = voice;
            return voice;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ForgetCarriedVoice()
        {
            s_carriedVoice = null;
        }

        private INoteSynth CurrentSynth
        {
            get
            {
                INoteSynth[] synths = _synths;
                int index = (int)Voice;
                return synths != null && index >= 0 && index < synths.Length ? synths[index] : null;
            }
        }

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
        }

        private void OnEnable() => AudioSettings.OnAudioConfigurationChanged += HandleAudioChanged;

        private void Start()
        {
            if (_sound == null || _move == null)
            {
                Debug.LogError("SwarmFluteVoice needs Initialize before it starts.", this);
                return;
            }

            StartTheVoices();
        }

        private void Update()
        {
            if (HolePlaying == FluteSynth.Silent)
            {
                return;
            }

            // Real time, so a note does not hang when the game is paused or slowed.
            _secondsSounding += Time.unscaledDeltaTime;
            bool isHeld = _input != null && _input.IsHoleHeld(HolePlaying);

            if (_secondsSounding >= _move.NoteSeconds && !isHeld)
            {
                HolePlaying = FluteSynth.Silent;
                CurrentSynth?.Play(FluteSynth.Silent);
            }
        }

        private void OnDisable() => AudioSettings.OnAudioConfigurationChanged -= HandleAudioChanged;

        // Unity calls this on the audio thread, so the voices have to arrive complete: see StartTheVoices. Every voice adds
        // its sound, so a note let go in one voice fades out while the next sounds in another; a silent voice returns at once.
        private void OnAudioFilterRead(float[] data, int channels)
        {
            INoteSynth[] synths = Volatile.Read(ref _synths);

            if (synths == null)
            {
                return;
            }

            Array.Clear(data, 0, data.Length);
            for (int index = 0; index < synths.Length; index++)
            {
                synths[index].Mix(data, channels);
            }

            ApplyMusicGain(data, channels);
            Interlocked.Increment(ref _buffersFilled);
        }

        // On the audio thread: the music volume, eased toward its target frame by frame. Full and settled costs nothing.
        private void ApplyMusicGain(float[] data, int channels)
        {
            float target = Volatile.Read(ref _musicGainTarget);
            if (target == 1f && _musicGainNow == 1f)
            {
                return;
            }

            float now = _musicGainNow;
            float step = _musicGainStep;
            for (int index = 0; index < data.Length; index += channels)
            {
                now += (target - now) * step;
                for (int channel = 0; channel < channels; channel++)
                {
                    data[index + channel] *= now;
                }
            }

            // Close enough is exact, so a settled gain stops easing.
            _musicGainNow = Math.Abs(target - now) < 1e-5f ? target : now;
        }

        private void StartTheVoices()
        {
            int sampleRate = AudioSettings.outputSampleRate;

            // A machine with no audio device reports no rate, and the game simply plays without sound.
            if (sampleRate <= 0)
            {
                return;
            }

            var holeHertz = new float[_sound.HoleCount];
            for (int hole = 0; hole < holeHertz.Length; hole++)
            {
                holeHertz[hole] = _sound.HertzOfHole(hole);
            }

            var synths = new INoteSynth[VoiceTitles.Length];
            synths[(int)NoteVoiceKind.Flute] = new FluteSynth(_sound, sampleRate, _sound.HoleCount);
            synths[(int)NoteVoiceKind.Pluck] = new StruckSynth(_sound.Pluck, holeHertz, _sound.GlideSeconds, sampleRate);
            synths[(int)NoteVoiceKind.Bell] = new StruckSynth(_sound.Bell, holeHertz, _sound.GlideSeconds, sampleRate);
            synths[(int)NoteVoiceKind.Reed] = new ReedSynth(_sound.Reed, holeHertz, _sound.GlideSeconds, sampleRate);

            _musicGainStep = 1f - (float)Math.Exp(-1d / (GainFollowSeconds * sampleRate));
            INoteSynth current = synths[(int)Voice];
            current.Lift(Lift);
            current.Play(HolePlaying);
            Volatile.Write(ref _synths, synths);
            _source.Play();
        }

        // Changing device, such as plugging in headphones, stops every source and can change the sample rate.
        private void HandleAudioChanged(bool deviceWasChanged)
        {
            if (_sound != null && _move != null)
            {
                StartTheVoices();
            }
        }
    }
}
