using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

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

        [Test]
        public void Tick_NightEndsDuringASong_TheSongStops()
        {
            var night = new NightRules(1234, 70, 45, 0.01f, 3);
            GameRules rules = TestRules.WithSongs(NewSong());
            var session = new NightSession(rules, night, new Mulberry32Random((uint)night.Seed), new FixedRandomSource());
            session.Stage.StartSurf(60f, 220f);
            session.Stage.StartChoir(60f);

            session.Tick(new PiperIntent(Vector2.zero, 0, 0), Step);

            Assert.That(session.Outcome, Is.Not.EqualTo(NightOutcome.InProgress));
            Assert.That(session.Stage.IsSurfing, Is.False);
            Assert.That(session.Stage.IsSinging, Is.False);
        }

        [TestCase(typeof(BalloonsEffect))]
        [TestCase(typeof(FaintEffect))]
        [TestCase(typeof(CrowdSurfEffect))]
        [TestCase(typeof(ChoirEffect))]
        public void Jingle_SongsWithATune_HaveOne(System.Type song)
        {
            var effect = (SongEffect)ScriptableObject.CreateInstance(song);

            Assert.That(effect.Jingle.Tones.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Tick_EverySongRunningAtOnce_AllocatesNothing()
        {
            NightRules night = TestRules.PrototypeNight(3);
            var session = new NightSession(TestRules.Default, night, new Mulberry32Random((uint)night.Seed), new FixedRandomSource());
            Vector2 anywhere = session.Piper.Position;
            int[] rats = session.Swarm.GatherNear(anywhere, 100000f);
            var faint = new FaintTrance(1f);
            var heart = new HeartTrance(rats.Length, 5f, 6f);
            var conga = new CongaTrance(session.Swarm, rats, 8, 14f);

            for (int index = 0; index < rats.Length; index++)
            {
                RatTrance trance = index % 3 == 0 ? faint : index % 3 == 1 ? (RatTrance)heart : conga;
                session.Swarm.Enchant(rats[index], trance, 60f, index);
            }

            session.Stage.StartSurf(60f, 220f);
            session.Stage.StartChoir(60f);
            var intent = new PiperIntent(Vector2.left, 1, 0);

            Assert.That(() => session.Tick(in intent, Step), Is.Not.AllocatingGCMemory());
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
