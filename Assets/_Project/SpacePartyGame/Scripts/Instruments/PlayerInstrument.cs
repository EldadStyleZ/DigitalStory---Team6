using System;
using System.Threading;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// The player's instrument. It turns the six note buttons into sound, with each note playing until its button is
    /// released or it has been held for <see cref="_maxHoldSeconds"/>, and tells the abilities when a note starts and
    /// ends. The sound itself comes out of the <see cref="InstrumentVoice"/> child.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerNotes))]
    public sealed class PlayerInstrument : MonoBehaviour
    {
        private const int NoteCount = 6;

        [SerializeField, Tooltip("Which instrument this player plays, and how it sounds.")]
        private InstrumentSettings _instrument;

        [SerializeField, Min(0.05f), Tooltip("The longest a note plays while held, in seconds.")]
        private float _maxHoldSeconds = 1f;

        [SerializeField, Tooltip("The child that plays the sound.")]
        private InstrumentVoice _voice;

        private readonly bool[] _wasActive = new bool[NoteCount];
        private PlayerNotes _notes;
        private NoteHolds _holds;
        private IInstrumentSynth _synth;
        private IInstrumentSynth[] _chord;
        private float[] _chordBuffer = new float[0];
        private float _gain = 1f;
        private float _chordVolume = 1f;
        private float _chordUntil = float.NegativeInfinity;
        private bool _chordSounding;

        /// <summary>
        /// How loud the instrument's notes are, 1 normally. The blow raises and lowers it while charging; the chord
        /// has its own loudness.
        /// </summary>
        public float Gain
        {
            get => Volatile.Read(ref _gain);
            set => Volatile.Write(ref _gain, Mathf.Max(0f, value));
        }

        /// <summary>Whether the blow's chord is playing now.</summary>
        public bool IsPlayingChord => Time.time < _chordUntil;

        /// <summary>Lets one note play past the hold limit while it is held, or null to put the limit back.</summary>
        public void HoldWithoutLimit(Note? note)
        {
            _holds.HoldWithoutLimit(note);
            RaiseChanges();
        }

        /// <summary>
        /// Stops every note and plays a chord on <paramref name="root"/> for a while: one voice per entry of
        /// <paramref name="semitones"/>, each that many semitones above the root. Notes stay silent until it ends.
        /// </summary>
        public void PlayChord(Note root, float[] semitones, float seconds, float volume)
        {
            _holds.ReleaseAll();
            _holds.HoldWithoutLimit(null);
            RaiseChanges();
            _synth?.Play(InstrumentKeys.Silent);
            Gain = 1f;

            int rate = AudioSettings.outputSampleRate;

            if (rate <= 0 || _instrument == null || semitones == null)
            {
                return;
            }

            var chord = new IInstrumentSynth[semitones.Length];

            for (int i = 0; i < chord.Length; i++)
            {
                chord[i] = _instrument.CreateSynth(rate, semitones[i]);
                chord[i].Play(InstrumentKeys.KeyOf(root));
            }

            Volatile.Write(ref _chordVolume, volume);
            Volatile.Write(ref _chord, chord);
            _chordUntil = Time.time + seconds;
            _chordSounding = true;
        }

        /// <summary>Raised when a note starts playing.</summary>
        public event Action<Note> NoteStarted;

        /// <summary>Raised when a note stops playing: released, or held for the limit.</summary>
        public event Action<Note> NoteEnded;

        /// <summary>The instrument this player plays.</summary>
        public InstrumentSettings Instrument => _instrument;

        /// <summary>Whether a note is playing now.</summary>
        public bool IsPlaying(Note note) => _holds.IsActive(note, Time.time);

        /// <summary>When a note was last pressed, in game time.</summary>
        public float StartedAt(Note note) => _holds.StartedAt(note);

        /// <summary>The note heard now, or null.</summary>
        public Note? Sounding => _holds.Sounding(Time.time);

        /// <summary>Writes the instrument's next samples. The voice calls it on the audio thread.</summary>
        public void FillAudio(float[] buffer, int channels)
        {
            // Volatile.Read(ref _synth)?.Fill(buffer, channels);
            IInstrumentSynth synth = Volatile.Read(ref _synth);

            if (synth == null)
            {
                return;
            }

            synth.Fill(buffer, channels);
            float gain = Volatile.Read(ref _gain);

            if (!Mathf.Approximately(gain, 1f))
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] *= gain;
                }
            }

            IInstrumentSynth[] chord = Volatile.Read(ref _chord);

            if (chord == null)
            {
                return;
            }

            if (_chordBuffer.Length != buffer.Length)
            {
                _chordBuffer = new float[buffer.Length];
            }

            float volume = Volatile.Read(ref _chordVolume);

            foreach (IInstrumentSynth voice in chord)
            {
                Array.Clear(_chordBuffer, 0, _chordBuffer.Length);
                voice.Fill(_chordBuffer, channels);

                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = Mathf.Clamp(buffer[i] + _chordBuffer[i] * volume, -1f, 1f);
                }
            }
        }

        private void Awake()
        {
            _notes = GetComponent<PlayerNotes>();
            _holds = new NoteHolds(_maxHoldSeconds);

            if (_instrument == null || _voice == null)
            {
                Debug.LogError($"{name}: PlayerInstrument is missing its instrument or voice.", this);
            }
        }

        private void OnEnable()
        {
            _notes.NotePressed += HandlePressed;
            _notes.NoteReleased += HandleReleased;
            AudioSettings.OnAudioConfigurationChanged += HandleAudioChanged;
        }

        private void OnDisable()
        {
            _notes.NotePressed -= HandlePressed;
            _notes.NoteReleased -= HandleReleased;
            AudioSettings.OnAudioConfigurationChanged -= HandleAudioChanged;
            _holds.ReleaseAll();
            RaiseChanges();
            _synth?.Play(InstrumentKeys.Silent);
        }

        private void Start()
        {
            StartTheSynth();
        }

        private void Update()
        {
            RaiseChanges();

            // While the blow's chord plays, the notes stay silent; when it ends, its voices fade out.
            // _synth?.Play(InstrumentKeys.KeyOf(Sounding));
            if (IsPlayingChord)
            {
                _synth?.Play(InstrumentKeys.Silent);
                return;
            }

            if (_chordSounding)
            {
                _chordSounding = false;
                IInstrumentSynth[] chord = Volatile.Read(ref _chord);

                if (chord != null)
                {
                    foreach (IInstrumentSynth voice in chord)
                    {
                        voice.Play(InstrumentKeys.Silent);
                    }
                }
            }

            _synth?.Play(InstrumentKeys.KeyOf(Sounding));
        }

        private void HandlePressed(Note note)
        {
            if (IsPlayingChord)
            {
                return;
            }

            _holds.Press(note, Time.time);
            RaiseChanges();
        }

        private void HandleReleased(Note note)
        {
            _holds.Release(note);
            RaiseChanges();
        }

        // Compares each note with last time, so a note that runs out of time ends here even while still held.
        private void RaiseChanges()
        {
            for (int i = 0; i < NoteCount; i++)
            {
                var note = (Note)i;
                bool active = _holds.IsActive(note, Time.time);

                if (active == _wasActive[i])
                {
                    continue;
                }

                _wasActive[i] = active;

                if (active)
                {
                    NoteStarted?.Invoke(note);
                }
                else
                {
                    NoteEnded?.Invoke(note);
                }
            }
        }

        private void StartTheSynth()
        {
            int sampleRate = AudioSettings.outputSampleRate;

            // A machine with no sound device has no sample rate, and the instrument simply stays silent.
            if (sampleRate <= 0 || _instrument == null || _voice == null)
            {
                return;
            }

            Volatile.Write(ref _synth, _instrument.CreateSynth(sampleRate));
            _voice.StartPlaying();
        }

        // Plugging in headphones can stop every sound and change the sample rate, so the synth starts again.
        private void HandleAudioChanged(bool deviceWasChanged)
        {
            StartTheSynth();
        }
    }
}
