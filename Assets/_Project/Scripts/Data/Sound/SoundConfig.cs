using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>How the game sounds. Everything is generated in code, so nothing here is an audio file.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Sound Config", fileName = "SoundConfig")]
    public sealed class SoundConfig : ScriptableObject
    {
        [field: Header("Overall")]
        [field: SerializeField, Range(0f, 1f), Tooltip("The volume of everything.")]
        public float MasterVolume { get; private set; } = .9f;

        [field: Header("Flute pitch")]
        [field: SerializeField, Min(1f), Tooltip("The pitch of hole 1, in hertz.")]
        public float FluteBaseHertz { get; private set; } = 587.33f;

        [SerializeField, Tooltip("How many semitones above hole 1 each hole sounds.")]
        private int[] _fluteSemitones = { 0, 2, 5, 7, 9, 12 };

        [field: SerializeField, Min(0f), Tooltip("How fast the vibrato wobbles, in hertz.")]
        public float VibratoHertz { get; private set; } = 5.3f;

        [field: SerializeField, Min(0f), Tooltip("How far the vibrato bends the pitch, in hertz.")]
        public float VibratoDepthHertz { get; private set; } = 6f;

        [field: SerializeField, Min(0.0001f), Tooltip("Seconds to slide to a new note.")]
        public float GlideSeconds { get; private set; } = .012f;

        [field: Header("Flute tone")]
        [field: SerializeField, Range(0f, 1f), Tooltip("How loud the octave above the note is.")]
        public float OvertoneShare { get; private set; } = .25f;

        [field: SerializeField, Min(1f), Tooltip("The brightest the tone gets, in hertz.")]
        public float BrightnessHertz { get; private set; } = 2800f;

        [field: SerializeField, Min(1f), Tooltip("The pitch of the breath noise, in hertz.")]
        public float BreathHertz { get; private set; } = 1900f;

        [field: SerializeField, Range(0f, 1f), Tooltip("How loud the breath noise is.")]
        public float BreathShare { get; private set; } = .12f;

        [field: Header("Flute volume")]
        [field: SerializeField, Range(0f, 1f), Tooltip("The flute's volume while a note holds.")]
        public float FluteVolume { get; private set; } = .13f;

        [field: SerializeField, Range(0f, 1f), Tooltip("The volume a new note starts from, which gives it a soft attack.")]
        public float NoteStartVolume { get; private set; } = .06f;

        [field: SerializeField, Min(0f), Tooltip("Seconds a new note stays at its start volume.")]
        public float NoteStartSeconds { get; private set; } = .035f;

        [field: SerializeField, Min(0.0001f), Tooltip("Seconds for the flute to swell in.")]
        public float SwellSeconds { get; private set; } = .03f;

        [field: SerializeField, Min(0.0001f), Tooltip("Seconds for the flute to fade out.")]
        public float FadeSeconds { get; private set; } = .05f;

        public int HoleCount => _fluteSemitones.Length;

        [field: Header("Sound effects")]
        [field: SerializeField, Tooltip("A rat falling under the spell, or waking up.")]
        public SoundCue Squeak { get; private set; } = new SoundCue(new Tone(ToneShape.Sine, 3350f, 2300f, .07f, .03f));

        [field: SerializeField, Tooltip("A rat falling in the river.")]
        public SoundCue Splash { get; private set; } = new SoundCue(
            new Tone(ToneShape.Noise, 950f, 950f, .28f, .14f),
            new Tone(ToneShape.Sine, 420f, 180f, .12f, .04f));

        [field: SerializeField, Tooltip("A cat catching a rat.")]
        public SoundCue Eaten { get; private set; } = new SoundCue(new Tone(ToneShape.Sine, 1800f, 700f, .12f, .04f));

        [field: SerializeField, Tooltip("A balloon popping.")]
        public SoundCue Pop { get; private set; } = new SoundCue(new Tone(ToneShape.Square, 320f, 90f, .07f, .05f));

        [field: SerializeField, Tooltip("Any song being cast.")]
        public SoundCue SongChime { get; private set; } = new SoundCue(
            new Tone(ToneShape.Triangle, 880f, 1320f, .25f, .06f),
            new Tone(ToneShape.Triangle, 1175f, 1760f, .3f, .05f, .12f));

        [field: SerializeField, Tooltip("Winning the night.")]
        public SoundCue Won { get; private set; } = new SoundCue(
            new Tone(ToneShape.Triangle, 587f, 880f, .25f, .08f),
            new Tone(ToneShape.Triangle, 880f, 1175f, .35f, .08f, .18f));

        [field: SerializeField, Tooltip("Losing the night.")]
        public SoundCue Lost { get; private set; } = new SoundCue(new Tone(ToneShape.Triangle, 440f, 220f, .5f, .07f));

        [field: Header("Beats")]
        [field: SerializeField, Tooltip("The kick during Disco.")]
        public SoundCue DiscoKick { get; private set; } = new SoundCue(new Tone(ToneShape.Sine, 150f, 45f, .16f, .14f));

        [field: SerializeField, Tooltip("The tick on every other Disco beat.")]
        public SoundCue DiscoTick { get; private set; } = new SoundCue(new Tone(ToneShape.Square, 5200f, 3800f, .03f, .012f));

        [field: SerializeField, Min(0.01f), Tooltip("Seconds between Disco beats.")]
        public float DiscoBeatSeconds { get; private set; } = .25f;

        [field: SerializeField, Tooltip("A normal beat during Conga.")]
        public SoundCue CongaBeat { get; private set; } = new SoundCue(new Tone(ToneShape.Triangle, 220f, 190f, .09f, .05f));

        [field: SerializeField, Tooltip("The last beat of each Conga bar.")]
        public SoundCue CongaAccent { get; private set; } = new SoundCue(new Tone(ToneShape.Triangle, 330f, 260f, .09f, .09f));

        [field: SerializeField, Min(0.01f), Tooltip("Seconds between Conga beats.")]
        public float CongaBeatSeconds { get; private set; } = .28f;

        [field: SerializeField, Min(1), Tooltip("How many beats are in a Conga bar.")]
        public int CongaBeatsPerBar { get; private set; } = 4;

        [field: Header("Crowding")]
        [field: SerializeField, Min(0f), Tooltip("Shortest gap between squeaks, so a swarm does not buzz.")]
        public float SqueakGapSeconds { get; private set; } = .07f;

        [field: SerializeField, Min(0f), Tooltip("Shortest gap between splashes, so a swarm does not buzz.")]
        public float SplashGapSeconds { get; private set; } = .045f;

        /// <summary>The pitch of a hole, in hertz.</summary>
        public float HertzOfHole(int hole) => FluteBaseHertz * Mathf.Pow(2f, _fluteSemitones[hole] / 12f);
    }
}
