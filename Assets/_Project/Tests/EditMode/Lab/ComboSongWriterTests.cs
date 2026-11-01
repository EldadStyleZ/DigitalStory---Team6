using System;
using System.Linq;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;
using static DigitalStory.Tests.EditMode.LabTestData;

namespace DigitalStory.Tests.EditMode
{
    public sealed class ComboSongWriterTests
    {
        private const float Tolerance = 0.001f;

        private static readonly FighterLook Plain = new FighterLook(Color.white, 1f, FighterShape.Rat);

        private readonly SongBand _band = new SongBand();

        private static CreatureType Song(SongStyle style, params int[] notes)
        {
            return new CreatureType("Song", 1, notes, 1f, 1f, Sturdy, Plain, "Role", style);
        }

        private static Tone[] LeadOf(SoundCue cue, SongStyle style)
        {
            return cue.Tones.Where(tone => tone.Shape == style.Lead).OrderBy(tone => tone.Delay).ToArray();
        }

        [Test]
        public void Write_TheTune_PlaysTheComboInOrder()
        {
            var style = new SongStyle(120f, ToneShape.Sawtooth, 1f, 1f, 1, 0f);

            Tone[] lead = LeadOf(ComboSongWriter.Write(Song(style, K, H, L), _band), style);

            Assert.That(lead[0].StartHertz, Is.EqualTo(_band.NoteHertz[K]).Within(Tolerance));
            Assert.That(lead[1].StartHertz, Is.EqualTo(_band.NoteHertz[H]).Within(Tolerance));
            Assert.That(lead[2].StartHertz, Is.EqualTo(_band.NoteHertz[L]).Within(Tolerance));
        }

        [Test]
        public void Write_TheTempo_SetsTheGapBetweenNotes()
        {
            var style = new SongStyle(120f, ToneShape.Sawtooth, 1f, 0.5f, 1, 0f);

            Tone[] lead = LeadOf(ComboSongWriter.Write(Song(style, H, J, K), _band), style);

            Assert.That(lead[1].Delay - lead[0].Delay, Is.EqualTo(0.25f).Within(Tolerance));
            Assert.That(lead[2].Delay - lead[1].Delay, Is.EqualTo(0.25f).Within(Tolerance));
        }

        [Test]
        public void Write_Repeats_PlaysTheComboAgain()
        {
            var style = new SongStyle(120f, ToneShape.Sawtooth, 1f, 0.5f, 2, 0f);

            Tone[] lead = LeadOf(ComboSongWriter.Write(Song(style, H, J), _band), style);

            Assert.That(lead.Length, Is.EqualTo(5), "Four notes and the ending.");
            Assert.That(lead[2].StartHertz, Is.EqualTo(lead[0].StartHertz));
            Assert.That(lead[3].StartHertz, Is.EqualTo(lead[1].StartHertz));
        }

        [Test]
        public void Write_ThePitch_MovesTheWholeTune()
        {
            var style = new SongStyle(120f, ToneShape.Sawtooth, 2f, 1f, 1, 0f);

            Tone[] lead = LeadOf(ComboSongWriter.Write(Song(style, J), _band), style);

            Assert.That(lead[0].StartHertz, Is.EqualTo(_band.NoteHertz[J] * 2f).Within(Tolerance));
        }

        [Test]
        public void Write_TheEnding_IsALongFirstNoteOfTheScale()
        {
            var style = new SongStyle(120f, ToneShape.Sawtooth, 1f, 0.5f, 1, 0f);

            Tone[] lead = LeadOf(ComboSongWriter.Write(Song(style, K, L), _band), style);
            Tone ending = lead[lead.Length - 1];

            Assert.That(ending.StartHertz, Is.EqualTo(_band.NoteHertz[H]).Within(Tolerance));
            Assert.That(ending.Delay, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(ending.Seconds, Is.GreaterThan(lead[0].Seconds));
        }

        [Test]
        public void Write_TheBand_PlaysTheBassOfTheFirstChordFirst()
        {
            var style = new SongStyle(120f, ToneShape.Sawtooth, 1f, 1f, 2, 0f);

            SoundCue cue = ComboSongWriter.Write(Song(style, H, J), _band);

            Assert.That(cue.Tones.Any(tone => tone.Delay < Tolerance && Mathf.Approximately(tone.StartHertz, _band.Chords[0].BassHertz)), Is.True);
        }

        [Test]
        public void Write_ALongTune_MovesToTheNextChordEachBar()
        {
            var style = new SongStyle(120f, ToneShape.Sawtooth, 1f, 1f, 2, 0f);

            SoundCue cue = ComboSongWriter.Write(Song(style, H, J, K), _band);

            Assert.That(cue.Tones.Any(tone => Mathf.Abs(tone.Delay - 2f) < Tolerance && Mathf.Approximately(tone.StartHertz, _band.Chords[1].BassHertz)), Is.True);
        }

        [Test]
        public void Write_AnyTune_EndsOnTheFirstChord()
        {
            var style = new SongStyle(120f, ToneShape.Sawtooth, 1f, 1f, 2, 0f);

            SoundCue cue = ComboSongWriter.Write(Song(style, H, J, K), _band);
            float lastBass = cue.Tones.Where(tone => _band.Chords.Any(chord => Mathf.Approximately(chord.BassHertz, tone.StartHertz))).Max(tone => tone.Delay);

            Assert.That(cue.Tones.Any(tone => Mathf.Approximately(tone.Delay, lastBass) && Mathf.Approximately(tone.StartHertz, _band.Chords[0].BassHertz)), Is.True);
        }

        [Test]
        public void Write_NoDrums_LeavesThemOut()
        {
            var style = new SongStyle(120f, ToneShape.Triangle, 1f, 1f, 1, 0f);

            SoundCue cue = ComboSongWriter.Write(Song(style, H, J, K), _band);

            Assert.That(cue.Tones.Any(tone => tone.Shape == ToneShape.Noise), Is.False);
        }

        [Test]
        public void Write_WithDrums_AddsThem()
        {
            var style = new SongStyle(120f, ToneShape.Triangle, 1f, 1f, 1, 1f);

            SoundCue cue = ComboSongWriter.Write(Song(style, H, J, K), _band);

            Assert.That(cue.Tones.Count(tone => tone.Shape == ToneShape.Noise), Is.GreaterThan(1));
        }

        [Test]
        public void Write_SofterDrums_AreQuieter()
        {
            var loud = new SongStyle(120f, ToneShape.Triangle, 1f, 1f, 1, 1f);
            var soft = new SongStyle(120f, ToneShape.Triangle, 1f, 1f, 1, 0.5f);

            float loudest = ComboSongWriter.Write(Song(loud, H, J), _band).Tones.Where(tone => tone.Shape == ToneShape.Noise).Max(tone => tone.Volume);
            float softest = ComboSongWriter.Write(Song(soft, H, J), _band).Tones.Where(tone => tone.Shape == ToneShape.Noise).Max(tone => tone.Volume);

            Assert.That(softest, Is.EqualTo(loudest / 2f).Within(Tolerance));
        }

        [Test]
        public void Write_NoTempo_FailsWithAClearError()
        {
            var error = Assert.Throws<ArgumentException>(() => ComboSongWriter.Write(Song(default, H, J), _band));

            Assert.That(error.Message, Does.Contain("Song"));
        }

        [Test]
        public void TheRealSongs_EachSoundsDifferent()
        {
            var roster = ScriptableObject.CreateInstance<CreatureRoster>();
            IComboSong[] songs = roster.Creatures.Concat<IComboSong>(roster.BattleSongs).ToArray();

            Assert.That(songs.Select(song => song.Style.BeatsPerMinute).Distinct().Count(), Is.EqualTo(songs.Length), "Each has its own tempo.");
            Assert.That(songs.Select(song => ComboSongWriter.Write(song, _band).Tones.Count).All(count => count > 0), Is.True);
        }

        [Test]
        public void TheRealSongs_LastAFewSeconds()
        {
            var roster = ScriptableObject.CreateInstance<CreatureRoster>();

            foreach (IComboSong song in roster.Creatures.Concat<IComboSong>(roster.BattleSongs))
            {
                float seconds = ComboSongWriter.Write(song, _band).Tones.Max(tone => tone.EndsAt);

                Assert.That(seconds, Is.InRange(1f, 4.5f), song.Name);
            }
        }

        [Test]
        public void TheRealSounds_TheFluteNotesMatchTheBand()
        {
            var sound = ScriptableObject.CreateInstance<LabSoundConfig>();

            for (int note = 0; note < sound.Notes.Count; note++)
            {
                Assert.That(sound.Notes[note].Tones[0].StartHertz, Is.EqualTo(sound.Band.NoteHertz[note]).Within(Tolerance));
            }
        }
    }
}
