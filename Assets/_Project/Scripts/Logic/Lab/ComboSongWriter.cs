using System;
using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Turns a combo's notes into a short tune: the player gives the notes, and the band adds the rhythm,
    /// chords and ending. It is worked out from the notes, so a new combo gets a tune with no new code.
    /// </summary>
    public static class ComboSongWriter
    {
        private const int BeatsPerBar = 4;
        private const float BeatsPerStrum = 2f;
        private const float EndingBeats = 2f;
        private const float DrumStepBeats = 0.5f;
        private const float NoteRing = 1.5f;
        private const float OctaveUp = 2f;
        private const float StrumSeconds = 0.012f;
        private const float KickStartHertz = 140f;
        private const float KickEndHertz = 50f;
        private const float KickSeconds = 0.14f;
        private const float SnareHertz = 2400f;
        private const float SnareEndHertz = 900f;
        private const float SnareSeconds = 0.12f;
        private const float HatHertz = 7000f;
        private const float HatSeconds = 0.04f;

        public static SoundCue Write(IComboSong song, SongBand band)
        {
            _ = song ?? throw new ArgumentNullException(nameof(song));
            _ = band ?? throw new ArgumentNullException(nameof(band));

            if (song.Style.BeatsPerMinute <= 0f)
            {
                throw new ArgumentException($"{song.Name} has no tempo, so it has no tune.", nameof(song));
            }

            var tones = new List<Tone>();
            float tuneBeats = AddTune(tones, song, band);
            AddEnding(tones, song.Style, band, tuneBeats);
            AddChords(tones, song.Style, band, tuneBeats);

            if (song.Style.Drums > 0f)
            {
                AddDrums(tones, song.Style, band, tuneBeats);
            }

            return new SoundCue(tones.ToArray());
        }

        private static float AddTune(List<Tone> tones, IComboSong song, SongBand band)
        {
            SongStyle style = song.Style;
            int notes = song.Combo.Count * style.Repeats;
            float seconds = style.BeatsPerNote * style.SecondsPerBeat;

            for (int index = 0; index < notes; index++)
            {
                float hertz = band.NoteHertz[song.Combo[index % song.Combo.Count]] * style.Pitch;
                AddLead(tones, style, band, hertz, seconds * NoteRing, index * seconds);
            }

            return notes * style.BeatsPerNote;
        }

        private static void AddLead(List<Tone> tones, SongStyle style, SongBand band, float hertz, float seconds, float delay)
        {
            tones.Add(new Tone(style.Lead, hertz, hertz, seconds, band.LeadVolume, delay));
            tones.Add(new Tone(ToneShape.Sine, hertz * OctaveUp, hertz * OctaveUp, seconds, band.AirVolume, delay));
        }

        // Every tune comes home to the first note of the scale over the first chord, so it sounds finished.
        private static void AddEnding(List<Tone> tones, SongStyle style, SongBand band, float tuneBeats)
        {
            float seconds = EndingBeats * style.SecondsPerBeat;
            float delay = tuneBeats * style.SecondsPerBeat;

            AddLead(tones, style, band, band.NoteHertz[0] * style.Pitch, seconds, delay);
            AddChord(tones, band, band.Chords[0], seconds, delay);
        }

        private static void AddChords(List<Tone> tones, SongStyle style, SongBand band, float tuneBeats)
        {
            float seconds = BeatsPerStrum * style.SecondsPerBeat;

            for (float beat = 0f; beat < tuneBeats; beat += BeatsPerStrum)
            {
                int bar = Mathf.FloorToInt(beat / BeatsPerBar);
                AddChord(tones, band, band.Chords[bar % band.Chords.Count], seconds, beat * style.SecondsPerBeat);
            }
        }

        private static void AddChord(List<Tone> tones, SongBand band, SongChord chord, float seconds, float delay)
        {
            tones.Add(new Tone(ToneShape.Sine, chord.BassHertz, chord.BassHertz, seconds, band.BassVolume, delay));

            for (int index = 0; index < chord.Hertz.Count; index++)
            {
                float hertz = chord.Hertz[index];
                tones.Add(new Tone(ToneShape.Triangle, hertz, hertz, seconds, band.ChordVolume, delay + (index * StrumSeconds)));
            }
        }

        private static void AddDrums(List<Tone> tones, SongStyle style, SongBand band, float tuneBeats)
        {
            for (float beat = 0f; beat <= tuneBeats; beat += DrumStepBeats)
            {
                AddDrum(tones, style, band, beat);
            }
        }

        // The kick falls on beats one and three, the snare on two and four, and the hi-hat between them.
        private static void AddDrum(List<Tone> tones, SongStyle style, SongBand band, float beat)
        {
            float delay = beat * style.SecondsPerBeat;
            float wholeBeat = Mathf.Repeat(beat, BeatsPerStrum);

            if (Mathf.Approximately(wholeBeat, 0f))
            {
                tones.Add(new Tone(ToneShape.Sine, KickStartHertz, KickEndHertz, KickSeconds, band.KickVolume * style.Drums, delay));
            }
            else if (Mathf.Approximately(wholeBeat, 1f))
            {
                tones.Add(new Tone(ToneShape.Noise, SnareHertz, SnareEndHertz, SnareSeconds, band.SnareVolume * style.Drums, delay));
            }
            else
            {
                tones.Add(new Tone(ToneShape.Noise, HatHertz, HatHertz, HatSeconds, band.HatVolume * style.Drums, delay));
            }
        }
    }
}
