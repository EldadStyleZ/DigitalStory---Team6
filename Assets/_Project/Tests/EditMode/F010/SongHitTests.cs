using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// The owner asked to feel each hit (plan section O, point 7). A keyboard has no haptics, so every hit a song lands on a
    /// beekeeper is reported once (<see cref="SwarmFormMachine.SongLanded"/>, which the session passes on) with its place,
    /// damage, whether it killed and whether it is one of the big hits: Meteor, Lance, Hydra, Nest and the Tornado's throw.
    /// The Sting's squeezes are in <see cref="StingSongTests"/>; the blocked hits and the giant's blows are the session's,
    /// in <see cref="HiveDefenseSessionTests"/>.
    /// </summary>
    public sealed class SongHitTests
    {
        private readonly List<Object> _assets = new List<Object>();

        private FuryConfig _furyConfig;
        private SwarmBody _body;
        private FuryMeter _fury;
        private SwarmFormMachine _forms;
        private HiveSet _hives;
        private List<ISwarmTarget> _enemies;
        private List<SongHit> _hits;

        [SetUp]
        public void SetUp()
        {
            _furyConfig = Make<FuryConfig>();
            var hiveConfig = Make<HiveConfig>();
            _body = new SwarmBody(Make<SwarmConfig>());
            _fury = new FuryMeter(_furyConfig);
            _forms = new SwarmFormMachine(_body, _fury, Make<SquadConfig>());
            _hives = new HiveSet(new List<Hive> { new Hive(hiveConfig, new Vector3(-30f, 0f, 0f)), new Hive(hiveConfig, new Vector3(30f, 0f, 0f)) });
            _enemies = new List<ISwarmTarget>();
            _hits = new List<SongHit>();
            _forms.SongLanded += _hits.Add;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in _assets)
            {
                Object.DestroyImmediate(asset);
            }

            _assets.Clear();
        }

        [Test]
        public void HitsHard_IsTheOwnersBigHits()
        {
            Assert.That(Make<MeteorSong>().HitsHard, Is.True);
            Assert.That(Make<LanceSong>().HitsHard, Is.True);
            Assert.That(Make<HydraSong>().HitsHard, Is.True);
            Assert.That(Make<NestSong>().HitsHard, Is.True);
            Assert.That(Make<TornadoSong>().HitsHard, Is.True);
            Assert.That(Make<StingSong>().HitsHard, Is.False);
            Assert.That(Make<ScytheSong>().HitsHard, Is.False);
            Assert.That(Make<HoneycombSong>().HitsHard, Is.False);
            Assert.That(Make<DomeSong>().HitsHard, Is.False);
        }

        [Test]
        public void Tornado_ReportsOneHeavyHitForEachBeekeeperItThrows()
        {
            FakeSwarmTarget near = Add(3f, 0f);
            FakeSwarmTarget behind = Add(0f, -2f);
            Add(9f, 0f);
            var song = Make<TornadoSong>();
            Cast(song);

            _forms.Tick(song.HitSeconds - 0.01f, _hives);
            Assert.That(_hits, Is.Empty);
            _forms.Tick(0.02f, _hives);

            Assert.That(_hits.Count, Is.EqualTo(2));
            AssertHit(_hits[0], near.Position, song.Damage, false, SwarmForm.Tornado, true);
            AssertHit(_hits[1], behind.Position, song.Damage, false, SwarmForm.Tornado, true);
            _forms.Tick(1f, _hives);
            Assert.That(_hits.Count, Is.EqualTo(2), "once each");
        }

        [Test]
        public void Meteor_ReportsAHeavyHitForEachBeekeeperItLandsOn_AndTheKill()
        {
            FakeSwarmTarget survivor = Add(0f, 9f);
            FakeSwarmTarget killed = Add(1f, 9f);
            killed.DiesAfter = 10f;
            var song = Make<MeteorSong>();
            Cast(song, aimPoint: new Vector3(0f, 0f, 9f));

            _forms.Tick(song.RiseSeconds + song.DiveSeconds, _hives);

            Assert.That(_hits.Count, Is.EqualTo(2));
            AssertHit(_hits[0], survivor.Position, song.Damage, false, SwarmForm.Meteor, true);
            AssertHit(_hits[1], killed.Position, song.Damage, true, SwarmForm.Meteor, true);
        }

        [Test]
        public void Hydra_ReportsAHeavyHitForEachHeadThatBites()
        {
            FakeSwarmTarget first = Add(2f, 0f);
            FakeSwarmTarget second = Add(0f, 3f);
            var song = Make<HydraSong>();
            var aim = new FakeSongAim { Nearest = new ISwarmTarget[] { first, second } };
            Cast(song, aim: aim);

            _forms.Tick(song.BiteSeconds, _hives);

            Assert.That(_hits.Count, Is.EqualTo(2), "the spare head strikes the ground, which is no hit");
            AssertHit(_hits[0], first.Position, song.Damage, false, SwarmForm.Hydra, true);
            AssertHit(_hits[1], second.Position, song.Damage, false, SwarmForm.Hydra, true);
        }

        [Test]
        public void Lance_ReportsAHeavyHitForEachBeekeeperOnTheLine()
        {
            FakeSwarmTarget near = Add(0f, 4f);
            FakeSwarmTarget far = Add(0.3f, 10f);
            Add(3f, 6f);
            var song = Make<LanceSong>();
            Cast(song, aimPoint: new Vector3(0f, 0f, 10f));

            _forms.Tick(song.HitSeconds, _hives);

            Assert.That(_hits.Count, Is.EqualTo(2));
            AssertHit(_hits[0], near.Position, song.Damage, false, SwarmForm.Lance, true);
            AssertHit(_hits[1], far.Position, song.Damage, false, SwarmForm.Lance, true);
        }

        [Test]
        public void Scythe_ReportsAHitThatIsNotOneOfTheBigOnes()
        {
            FakeSwarmTarget ahead = Add(0f, 3f);
            var song = Make<ScytheSong>();
            Cast(song, aimPoint: new Vector3(0f, 0f, 3f));

            _forms.Tick(song.HitSeconds, _hives);

            Assert.That(_hits.Count, Is.EqualTo(1));
            AssertHit(_hits[0], ahead.Position, song.Damage, false, SwarmForm.Scythe, false);
        }

        [Test]
        public void Nest_ReportsAHeavyHitForEachBeekeeperInItsBurst()
        {
            var song = Make<NestSong>();
            Cast(song, aimPoint: new Vector3(0f, 0f, 6f));
            FakeSwarmTarget walker = Add(0f, 7f);

            _forms.Tick(song.ArmSeconds, _hives);
            _forms.Tick(song.FuseSeconds, _hives);

            Assert.That(_hits.Count, Is.EqualTo(1));
            AssertHit(_hits[0], walker.Position, song.Damage, false, SwarmForm.Nest, true);
        }

        [Test]
        public void Honeycomb_ReportsEachThornItStingsBackWith()
        {
            var song = Make<HoneycombSong>();
            Cast(song, aimPoint: new Vector3(30f, 0f, 0f));
            FakeSwarmTarget stick = Add(31f, 0f);

            _forms.NotifyHiveHit(1, stick);

            Assert.That(_hits.Count, Is.EqualTo(1));
            AssertHit(_hits[0], stick.Position, song.ThornDamage, false, SwarmForm.Honeycomb, false);
        }

        [Test]
        public void Gust_PushesButHurtsNoOne_SoItReportsNoHit()
        {
            Add(2f, 0f);
            var song = Make<GustSong>();
            SwarmSongContext context = Context();
            song.Cast(in context);

            _forms.Tick(song.Seconds, _hives);

            Assert.That(_hits, Is.Empty);
        }

        [Test]
        public void Hits_AllocateNothing()
        {
            Add(3f, 0f);
            var song = Make<TornadoSong>();
            Cast(song);
            _forms.Tick(0.01f, _hives);
            _hits.Capacity = 8;

            Assert.That(() => _forms.Tick(song.HitSeconds, _hives), Is.Not.AllocatingGCMemory());
            Assert.That(_hits.Count, Is.EqualTo(1));
        }

        private FakeSwarmTarget Add(float x, float z)
        {
            var target = new FakeSwarmTarget { Position = new Vector3(x, 0f, z) };
            _enemies.Add(target);
            return target;
        }

        private void Cast(SwarmSongEffect song, Vector3 aimPoint = default, ISongAim aim = null)
        {
            SwarmSongContext context = Context(aimPoint, aim);
            song.Cast(in context);
        }

        private SwarmSongContext Context(Vector3 aimPoint = default, ISongAim aim = null)
        {
            return new SwarmSongContext(Vector3.zero, null, aimPoint, _body, _forms, _fury, _hives, _enemies, null, aim);
        }

        private static void AssertHit(SongHit hit, Vector3 position, float damage, bool isKill, SwarmForm form, bool isHeavy)
        {
            Assert.That(hit.Position, Is.EqualTo(position));
            Assert.That(hit.Damage, Is.EqualTo(damage).Within(1e-3f));
            Assert.That(hit.IsKill, Is.EqualTo(isKill));
            Assert.That(hit.IsBlocked, Is.False);
            Assert.That(hit.Form, Is.EqualTo(form));
            Assert.That(hit.IsHeavy, Is.EqualTo(isHeavy));
        }

        private T Make<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }
    }
}
