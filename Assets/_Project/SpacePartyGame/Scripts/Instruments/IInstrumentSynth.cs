using PiperToy;

namespace SpacePartyGame
{
    /// <summary>Something that sounds keys 1 to 6 on the audio thread, such as the flute or the cello.</summary>
    public interface IInstrumentSynth
    {
        /// <summary>Sounds a key from 1 to 6, or 0 to fade out. Called from the main thread.</summary>
        void Play(int key);

        /// <summary>Writes the next samples over a buffer. Called on the audio thread.</summary>
        void Fill(float[] buffer, int channels);
    }

    /// <summary>The toy's flute synthesizer, used as it is, so P1's flute sounds exactly like the toy's.</summary>
    public sealed class FluteInstrumentSynth : IInstrumentSynth
    {
        private readonly FluteSynth _synth;

        public FluteInstrumentSynth(FluteSound sound, int sampleRate)
        {
            _synth = new FluteSynth(sound, sampleRate, sound.HoleCount);
        }

        public void Play(int key) => _synth.Play(key);

        public void Fill(float[] buffer, int channels) => _synth.Fill(buffer, channels);
    }
}
