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
            Volatile.Read(ref _synth)?.Fill(buffer, channels);
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
            _synth?.Play(InstrumentKeys.KeyOf(Sounding));
        }

        private void HandlePressed(Note note)
        {
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
