using PiperToy;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>The instruments a player can play.</summary>
    public enum InstrumentKind
    {
        Flute,
        Cello,
    }

    /// <summary>
    /// One instrument and how it sounds: the flute uses the toy's FluteSound numbers, the cello its own. Both play
    /// the same six notes, the cello an octave lower.
    /// </summary>
    [CreateAssetMenu(fileName = "Instrument", menuName = "Space Party Game/Instrument")]
    public sealed class InstrumentSettings : ScriptableObject
    {
        [SerializeField, Tooltip("Which instrument this is.")]
        private InstrumentKind _kind;

        [SerializeField, Tooltip("How the flute sounds; used when the kind is Flute.")]
        private FluteSound _flute = new FluteSound();

        [SerializeField, Tooltip("How the cello sounds; used when the kind is Cello.")]
        private CelloSound _cello = new CelloSound();

        public InstrumentKind Kind => _kind;

        /// <summary>Makes the synthesizer for this instrument at the output's sample rate.</summary>
        public IInstrumentSynth CreateSynth(int sampleRate) =>
            _kind == InstrumentKind.Cello
                ? new CelloSynth(_cello, sampleRate)
                : new FluteInstrumentSynth(_flute, sampleRate);
    }
}
