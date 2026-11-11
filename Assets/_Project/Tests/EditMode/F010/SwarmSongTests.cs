using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class SwarmSongTests
    {
        private SwarmConfig _swarmConfig;
        private FuryConfig _furyConfig;
        private HiveConfig _hiveConfig;
        private SwarmBody _body;
        private FuryMeter _fury;
        private SwarmFormMachine _forms;
        private HiveSet _hives;
        private List<ISwarmTarget> _enemies;
        private FakeSmokeClearer _smoke;
        private readonly List<Object> _assets = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _swarmConfig = ScriptableObject.CreateInstance<SwarmConfig>();
            _furyConfig = ScriptableObject.CreateInstance<FuryConfig>();
            _hiveConfig = ScriptableObject.CreateInstance<HiveConfig>();
            _body = new SwarmBody(_swarmConfig);
            _fury = new FuryMeter(_furyConfig);
            _forms = new SwarmFormMachine(_body, _fury);
            _hives = new HiveSet(new List<Hive> { new Hive(_hiveConfig, new Vector3(-10f, 0f, 0f)), new Hive(_hiveConfig, new Vector3(10f, 0f, 0f)) });
            _enemies = new List<ISwarmTarget>();
            _smoke = new FakeSmokeClearer();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_swarmConfig);
            Object.DestroyImmediate(_furyConfig);
            Object.DestroyImmediate(_hiveConfig);

            foreach (Object asset in _assets)
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void Songs_Defaults_MatchTheSpecsKeysAndCooldowns()
        {
            AssertSong(Make<DomeSong>(), "Dome", 8f, 0, 1, 0);
            AssertSong(Make<StingSong>(), "Sting", 1.5f, 2, 3, 4);
            AssertSong(Make<TornadoSong>(), "Tornado", 10f, 5, 4, 3, 5);
            AssertSong(Make<GustSong>(), "Gust", 6f, 1, 1, 2);
            AssertSong(Make<HiveCallSong>(), "Hive Call", 3f, 0, 5, 0);
            AssertSong(Make<ColossusSong>(), "Colossus", 0f, 3, 4, 5, 4, 3);
        }

        [Test]
        public void Songs_Defaults_MatchTheSpecsNumbers()
        {
            var dome = Make<DomeSong>();
            var sting = Make<StingSong>();
            var tornado = Make<TornadoSong>();
            var gust = Make<GustSong>();
            var colossus = Make<ColossusSong>();

            Assert.That(dome.Seconds, Is.EqualTo(5f));
            Assert.That(dome.Radius, Is.EqualTo(2.2f));
            Assert.That(sting.Seconds, Is.EqualTo(4f));
            Assert.That(sting.Fraction, Is.EqualTo(0.4f));
            Assert.That(sting.DamagePerSecond, Is.EqualTo(25f));
            Assert.That(tornado.Seconds, Is.EqualTo(1.5f));
            Assert.That(tornado.Radius, Is.EqualTo(5f));
            Assert.That(tornado.Damage, Is.EqualTo(30f));
            Assert.That(tornado.KnockbackSpeed, Is.EqualTo(6f));
            Assert.That(gust.Seconds, Is.EqualTo(0.8f));
            Assert.That(gust.ClearRadius, Is.EqualTo(6f));
            Assert.That(colossus.Seconds, Is.EqualTo(12f));
        }

        // Dome

        [Test]
        public void Dome_Cast_DomesTheSwarmForItsSeconds()
        {
            var song = Make<DomeSong>();
            SwarmSongContext context = Context();

            Assert.That(song.CanCast(in context), Is.True);
            song.Cast(in context);

            Assert.That(_forms.IsDomed, Is.True);
            _forms.Tick(5f, 2);
            Assert.That(_forms.IsDomed, Is.False);
        }

        // Sting

        [Test]
        public void Sting_CanCast_NeedsAnAimedLivingTarget()
        {
            var song = Make<StingSong>();
            var dead = new FakeSwarmTarget { IsAlive = false };

            SwarmSongContext none = Context(aim: null);
            SwarmSongContext deadAim = Context(aim: dead);
            SwarmSongContext alive = Context(aim: new FakeSwarmTarget());

            Assert.That(song.CanCast(in none), Is.False);
            Assert.That(song.CanCast(in deadAim), Is.False);
            Assert.That(song.CanCast(in alive), Is.True);
        }

        [Test]
        public void Sting_Cast_StreamsFortyPercentToTheTargetForFourSeconds()
        {
            var song = Make<StingSong>();
            var target = new FakeSwarmTarget();
            SwarmSongContext context = Context(aim: target);

            song.Cast(in context);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Sting));
            Assert.That(_forms.StingTarget, Is.SameAs(target));
            Assert.That(_forms.StingFraction, Is.EqualTo(0.4f));
            _forms.Tick(4f, 2);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(target.DamageTaken, Is.EqualTo(100f).Within(1e-2f));
        }

        // Tornado

        [Test]
        public void Tornado_Cast_HurtsAndThrowsEveryBeekeeperWithinFiveMetres()
        {
            var song = Make<TornadoSong>();
            var near = new FakeSwarmTarget { Position = new Vector3(3f, 0f, 0f) };
            var far = new FakeSwarmTarget { Position = new Vector3(5.5f, 0f, 0f) };
            _enemies.Add(near);
            _enemies.Add(far);
            SwarmSongContext context = Context();

            song.Cast(in context);

            Assert.That(near.DamageTaken, Is.EqualTo(30f));
            Assert.That(near.LastKnockback.x, Is.EqualTo(6f).Within(1e-3f), "6 m/s straight away from the Piper");
            Assert.That(near.LastKnockback.z, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(far.DamageTaken, Is.EqualTo(0f));
            Assert.That(far.LastKnockback, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void Tornado_Cast_ReachIncludesExactlyFiveMetresAndIgnoresHeight()
        {
            var song = Make<TornadoSong>();
            var edge = new FakeSwarmTarget { Position = new Vector3(0f, 9f, 5f) };
            _enemies.Add(edge);
            SwarmSongContext context = Context();

            song.Cast(in context);

            Assert.That(edge.DamageTaken, Is.EqualTo(30f), "5 m on the ground counts, however high");
            Assert.That(edge.LastKnockback.y, Is.EqualTo(0f), "thrown along the ground");
        }

        [Test]
        public void Tornado_Cast_MeasuresFromThePiper()
        {
            var song = Make<TornadoSong>();
            var target = new FakeSwarmTarget { Position = new Vector3(21f, 0f, 20f) };
            _enemies.Add(target);
            SwarmSongContext context = Context(piper: new Vector3(20f, 0f, 20f));

            song.Cast(in context);

            Assert.That(target.DamageTaken, Is.EqualTo(30f));
            Assert.That(target.LastKnockback.x, Is.EqualTo(6f).Within(1e-3f));
        }

        [Test]
        public void Tornado_Cast_SkipsTheDead()
        {
            var song = Make<TornadoSong>();
            var dead = new FakeSwarmTarget { Position = new Vector3(1f, 0f, 0f), IsAlive = false };
            _enemies.Add(dead);
            SwarmSongContext context = Context();

            song.Cast(in context);

            Assert.That(dead.DamageTaken, Is.EqualTo(0f));
            Assert.That(_fury.Value, Is.EqualTo(0f));
        }

        [Test]
        public void Tornado_Cast_FillsFuryFromTheTotalDamage()
        {
            var song = Make<TornadoSong>();
            _enemies.Add(new FakeSwarmTarget { Position = new Vector3(1f, 0f, 0f) });
            _enemies.Add(new FakeSwarmTarget { Position = new Vector3(0f, 0f, -2f) });
            _enemies.Add(new FakeSwarmTarget { Position = new Vector3(30f, 0f, 0f) });
            SwarmSongContext context = Context();

            song.Cast(in context);

            Assert.That(_fury.Value, Is.EqualTo(30f).Within(1e-3f), "60 damage at 2 damage per Fury point");
        }

        [Test]
        public void Tornado_Cast_WithNobodyAroundStillSpinsForItsSeconds()
        {
            var song = Make<TornadoSong>();
            SwarmSongContext context = Context();

            song.Cast(in context);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Tornado));
            _forms.Tick(1.5f, 2);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        [Test]
        public void Tornado_Cast_BeekeeperOnTheSpotIsStillThrownSixMetresPerSecond()
        {
            var song = Make<TornadoSong>();
            var onTop = new FakeSwarmTarget { Position = new Vector3(2f, 0f, 2f) };
            _enemies.Add(onTop);
            SwarmSongContext context = Context(piper: new Vector3(2f, 0f, 2f));

            song.Cast(in context);

            Assert.That(onTop.LastKnockback.magnitude, Is.EqualTo(6f).Within(1e-3f));
            Assert.That(float.IsNaN(onTop.LastKnockback.x), Is.False);
        }

        // Gust

        [Test]
        public void Gust_Cast_ClearsSmokeWithinSixMetresOfThePiper()
        {
            var song = Make<GustSong>();
            SwarmSongContext context = Context(piper: new Vector3(4f, 0f, 7f));

            song.Cast(in context);

            Assert.That(_smoke.CallCount, Is.EqualTo(1));
            Assert.That(_smoke.LastCenter, Is.EqualTo(new Vector3(4f, 0f, 7f)));
            Assert.That(_smoke.LastRadius, Is.EqualTo(6f));
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Gust));
            _forms.Tick(0.8f, 2);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        [Test]
        public void Gust_Cast_WithNoSmokeSystemStillBursts()
        {
            var song = Make<GustSong>();
            SwarmSongContext context = Context(smoke: false);

            song.Cast(in context);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Gust));
        }

        // Hive Call

        [Test]
        public void HiveCall_CanCast_NeedsAStandingHive()
        {
            var song = Make<HiveCallSong>();
            SwarmSongContext context = Context();
            Assert.That(song.CanCast(in context), Is.True);

            _hives[0].Damage(10000f);
            Assert.That(song.CanCast(in context), Is.True, "one hive still stands");

            _hives[1].Damage(10000f);
            Assert.That(song.CanCast(in context), Is.False);
        }

        [Test]
        public void HiveCall_CanCast_NotWhenTheSwarmIsFull()
        {
            var song = Make<HiveCallSong>();
            JsonUtility.FromJsonOverwrite("{\"<StartingWasps>k__BackingField\":10000}", _swarmConfig);
            _body = new SwarmBody(_swarmConfig);
            _forms = new SwarmFormMachine(_body, _fury);
            SwarmSongContext context = Context();

            Assert.That(song.CanCast(in context), Is.False);
        }

        [Test]
        public void HiveCall_Cast_StartsHiveCall()
        {
            var song = Make<HiveCallSong>();
            SwarmSongContext context = Context();

            song.Cast(in context);

            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.HiveCall));
            Assert.That(_forms.IsPiperRooted, Is.True);
        }

        // Colossus

        [Test]
        public void Colossus_CanCast_OnlyWithFullFury()
        {
            var song = Make<ColossusSong>();
            SwarmSongContext context = Context();

            _fury.AddDamage(199.9f);
            Assert.That(song.CanCast(in context), Is.False, "just under full");

            _fury.AddDamage(1f);
            Assert.That(song.CanCast(in context), Is.True);
        }

        [Test]
        public void Colossus_Cast_UsesAllFuryAndLastsTwelveSeconds()
        {
            var song = Make<ColossusSong>();
            _fury.AddDamage(1000f);
            SwarmSongContext context = Context();

            song.Cast(in context);

            Assert.That(_fury.Value, Is.EqualTo(0f));
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Colossus));
            _forms.Tick(11.5f, 2);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.Colossus));
            _forms.Tick(0.5f, 2);
            Assert.That(_forms.Current, Is.EqualTo(SwarmForm.River));
        }

        // Defaults that every song shares

        [Test]
        public void CanCast_SongsWithNoCondition_AlwaysAllow()
        {
            SwarmSongContext context = Context();

            Assert.That(Make<DomeSong>().CanCast(in context), Is.True);
            Assert.That(Make<TornadoSong>().CanCast(in context), Is.True);
            Assert.That(Make<GustSong>().CanCast(in context), Is.True);
        }

        private T Make<T>() where T : SwarmSongEffect
        {
            var song = ScriptableObject.CreateInstance<T>();
            _assets.Add(song);
            return song;
        }

        private SwarmSongContext Context(Vector3 piper = default, ISwarmTarget aim = null, bool smoke = true)
        {
            return new SwarmSongContext(piper, aim, _body, _forms, _fury, _hives, _enemies, smoke ? _smoke : null);
        }

        private static void AssertSong(SwarmSongEffect song, string title, float cooldown, params int[] keys)
        {
            Assert.That(song.Song.Title, Is.EqualTo(title));
            Assert.That(song.Song.CooldownSeconds, Is.EqualTo(cooldown));
            Assert.That(song.Song.Keys, Is.EqualTo(keys));
        }
    }
}
