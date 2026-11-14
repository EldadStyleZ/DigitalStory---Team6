namespace DigitalStory.View
{
    /// <summary>A synthesised note voice. <see cref="Fill"/> runs on the audio thread; the other two on the main thread.</summary>
    public interface INoteSynth
    {
        /// <summary>Sounds a hole, or lets the voice fall silent with <see cref="FluteSynth.Silent"/>.</summary>
        void Play(int hole);

        /// <summary>Raises every note by some semitones until it is set again; 0 plays the holes' own pitches.</summary>
        void Lift(float semitones);

        /// <summary>Writes the next samples over a buffer, each channel next to the others.</summary>
        void Fill(float[] buffer, int channels);
    }
}
