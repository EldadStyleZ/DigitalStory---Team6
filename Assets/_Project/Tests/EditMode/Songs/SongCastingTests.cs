using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class SongCastingTests
    {
        private const float Step = 1f / 60f;
        private const int FirstKey = 2;
        private const int SecondKey = 1;
        private const int ThirdKey = 0;

        [Test]
        public void Tick_PlayingACombo_CastsItsEffect()
        {
            FakeSongEffect song = NewSong();
            NightSession session = SessionWith(song);

            PlayCombo(session);

            Assert.That(song.CastCount, Is.EqualTo(1));
            Assert.That(song.LastPiperPosition, Is.EqualTo(session.Piper.Position));
        }

        [Test]
        public void Tick_PlayingACombo_RaisesSongCast()
        {
            NightSession session = SessionWith(NewSong());
            int? cast = null;
            session.SongCast += song => cast = song;

            PlayCombo(session);

            Assert.That(cast, Is.EqualTo(0));
        }

        [Test]
        public void Tick_PlayingOnlyPartOfACombo_CastsNothing()
        {
            FakeSongEffect song = NewSong();
            NightSession session = SessionWith(song);

            Press(session, FirstKey);
            Press(session, SecondKey);

            Assert.That(song.CastCount, Is.EqualTo(0));
        }

        [Test]
        public void Tick_OutOfBreath_NotesDoNotCount()
        {
            FakeSongEffect song = NewSong();
            NightSession session = SessionWith(song);
            session.Tick(new PiperIntent(Vector2.zero, 1, 0), 10f);
            Assert.That(session.Breath.IsExhausted, Is.True);

            PlayCombo(session);

            Assert.That(song.CastCount, Is.EqualTo(0));
        }

        [Test]
        public void Tick_OutOfBreath_StillRemembersTheLastHolePressed()
        {
            NightSession session = SessionWith(NewSong());
            session.Tick(new PiperIntent(Vector2.zero, 1, 0), 10f);

            session.Tick(new PiperIntent(Vector2.zero, HoleBit(FirstKey) | HoleBit(ThirdKey), HoleBit(ThirdKey)), Step);

            Assert.That(session.Flute.ChooseHole(HoleBit(FirstKey) | HoleBit(ThirdKey)), Is.EqualTo(ThirdKey));
        }

        [Test]
        public void Tick_SongOnCooldown_CannotBeCastUntilGameTimePasses()
        {
            FakeSongEffect song = NewSong();
            NightSession session = SessionWith(song);
            PlayCombo(session);

            PlayCombo(session);
            Assert.That(song.CastCount, Is.EqualTo(1));

            session.Tick(new PiperIntent(Vector2.zero, 0, 0), FakeSongEffect.CooldownSeconds + 0.1f);
            PlayCombo(session);
            Assert.That(song.CastCount, Is.EqualTo(2));
        }

        private static FakeSongEffect NewSong() => ScriptableObject.CreateInstance<FakeSongEffect>();

        private static NightSession SessionWith(params SongEffect[] songs)
        {
            NightRules night = TestRules.PrototypeNight(0);
            GameRules rules = TestRules.WithSongs(songs);
            return new NightSession(rules, night, new Mulberry32Random((uint)night.Seed), new FixedRandomSource());
        }

        private static void PlayCombo(NightSession session)
        {
            Press(session, FirstKey);
            Press(session, SecondKey);
            Press(session, ThirdKey);
        }

        private static void Press(NightSession session, int hole)
        {
            session.Tick(new PiperIntent(Vector2.zero, HoleBit(hole), HoleBit(hole)), Step);
        }

        private static int HoleBit(int hole) => 1 << hole;
    }
}
