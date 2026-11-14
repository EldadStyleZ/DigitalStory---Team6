namespace DigitalStory.Logic
{
    /// <summary>
    /// A synthesised note voice (plan section AC). The main thread asks for notes; the audio thread adds the sound to its
    /// buffers. <see cref="Mix"/> runs on the audio thread, so an implementation makes no Unity call and allocates nothing there.
    /// </summary>
    public interface INoteSynth
    {
        /// <summary>
        /// Sounds a hole (counted from 0), or lets the note go with <see cref="Flute.NoHole"/>. A struck voice strikes again on
        /// every call, even for the hole already ringing; the flute and the reed slide on to it.
        /// </summary>
        void Play(int hole);

        /// <summary>Raises the newest note by <paramref name="semitones"/> until it is set again, gliding there. 0 is the hole's own pitch.</summary>
        void Lift(float semitones);

        /// <summary>Adds the next samples to a buffer, the channels of each frame side by side. Called on the audio thread.</summary>
        void Mix(float[] buffer, int channels);
    }
}
