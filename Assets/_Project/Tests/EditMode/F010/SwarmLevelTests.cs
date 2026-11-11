using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Plan section H: each level multiplies every song's radius, reach and height, and its damage, by
    /// 1 + 0.12 x (level - 1), so level 3 is x1.24. The rules apply it, and the slot parameters the picture reads carry the
    /// grown sizes. Strength comes from the level, never from the live wasp count. The giant is in
    /// <see cref="HiveDefenseSessionTests"/>, which drives it.
    /// </summary>
    public sealed class SwarmLevelTests
    {
        private readonly List<Object> _assets = new List<Object>();

        private SwarmConfig _swarmConfig;
        private SwarmLevelConfig _levels;
        private SwarmBody _body;
        private FuryConfig _furyConfig;
        private FuryMeter _fury;
        private SwarmFormMachine _forms;
        private HiveSet _hives;
        private List<ISwarmTarget> _enemies;
        private FakeSmokeClearer _smoke;

        private SlotState Strike => _forms.Slot(SongSlot.Strike);

        [SetUp]
        public void SetUp()
        {
            _swarmConfig = Make<SwarmConfig>();
            _levels = Make<SwarmLevelConfig>();
            var hiveConfig = Make<HiveConfig>();
            _body = new SwarmBody(_swarmConfig);
            _furyConfig = Make<FuryConfig>();
            _fury = new FuryMeter(_furyConfig);
            _forms = new SwarmFormMachine(_body, _fury, Make<SquadConfig>());
            _hives = new HiveSet(new List<Hive> { new Hive(hiveConfig, new Vector3(-10f, 0f, 0f)), new Hive(hiveConfig, new Vector3(10f, 0f, 0f)) });
            _enemies = new List<ISwarmTarget>();
            _smoke = new FakeSmokeClearer();
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

        // ---- The multiplier ----

        [TestCase(1, 1f)]
        [TestCase(2, 1.12f)]
        [TestCase(3, 1.24f)]
        [TestCase(9, 1.96f)]
        public void Scale_IsOnePlusTwelvePercentForEachLevelAboveTheFirst(int level, float expected)
        {
            Assert.That(_levels.SizeScale(level), Is.EqualTo(expected).Within(1e-5f));
            Assert.That(_levels.DamageScale(level), Is.EqualTo(expected).Within(1e-5f));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Scale_BelowTheFirstLevel_Throws(int level)
        {
            Assert.That(() => _levels.SizeScale(level), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => _levels.DamageScale(level), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        // ---- The context carries the level ----

        [Test]
        public void Context_WithoutScales_IsTheFirstLevel()
        {
            var context = new SwarmSongContext(default, null, default, _body, _forms, _fury, _hives, _enemies, _smoke);

            Assert.That(context.SizeScale, Is.EqualTo(1f));
            Assert.That(context.DamageScale, Is.EqualTo(1f));
        }

        [TestCase(0f, 1f)]
        [TestCase(1f, 0f)]
        [TestCase(-1f, 1f)]
        [TestCase(float.NaN, 1f)]
        [TestCase(1f, float.PositiveInfinity)]
        public void Context_AScaleOfZeroOrLess_Throws(float size, float damage)
        {
            Assert.That(
                () => new SwarmSongContext(default, null, default, _body, _forms, _fury, _hives, _enemies, _smoke, null, false, size, damage),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void Context_AimedAt_KeepsTheScales()
        {
            SwarmSongContext context = Context(3);

            SwarmSongContext aimed = context.AimedAt(null, Vector3.one);

            Assert.That(aimed.SizeScale, Is.EqualTo(1.24f).Within(1e-5f));
            Assert.That(aimed.DamageScale, Is.EqualTo(1.24f).Within(1e-5f));
        }

        // ---- Sting: reach and damage ----

        [TestCase(1)]
        [TestCase(3)]
        public void Sting_Damage_GrowsWithTheLevel(int level)
        {
            var target = new FakeSwarmTarget { Position = new Vector3(2f, 0f, 0f) };
            Cast(Make<StingSong>(), level, aim: target);

            _forms.Tick(1.6f, _hives);

            Assert.That(target.DamageTaken, Is.EqualTo(25f * Scale(level)).Within(1e-3f), "1 s of 25 a second after the arrival");
            Assert.That(_fury.Value, Is.EqualTo(25f * Scale(level) / _furyConfig.DamagePerFuryPoint).Within(1e-3f), "the grown damage fills Fury");
        }

        [Test]
        public void Sting_Reach_GrowsWithTheLevel()
        {
            var target = new FakeSwarmTarget { Position = new Vector3(13f, 0f, 0f) };

            Cast(Make<StingSong>(), 1, aim: target);
            Assert.That(Strike.ParamA.x, Is.EqualTo(12f).Within(1e-3f), "13 m is beyond 12 m: it wraps the air toward it");

            Cast(Make<StingSong>(), 2, aim: target);
            Assert.That((Vector3)Strike.ParamA, Is.EqualTo(target.Position), "within 12 x 1.12 = 13.44 m");
        }

        [TestCase(1)]
        [TestCase(3)]
        public void AimedSongs_AskTheAimChooserWithinTheirGrownReach(int level)
        {
            var aim = new FakeSongAim { Target = null, Point = new Vector3(0f, 0f, 5f) };
            SwarmSongContext context = Context(level, aimChooser: aim);

            Make<StingSong>().Cast(in context);
            Assert.That(aim.LastRange, Is.EqualTo(12f * Scale(level)).Within(1e-4f));

            Make<MeteorSong>().Cast(in context);
            Assert.That(aim.LastRange, Is.EqualTo(18f * Scale(level)).Within(1e-4f));
        }

        // ---- Tornado: radius, height, wall and damage ----

        [TestCase(1)]
        [TestCase(3)]
        public void Tornado_ThePictureGetsTheGrownRadiusHeightAndWall(int level)
        {
            Cast(Make<TornadoSong>(), level);

            Assert.That(Strike.ParamA.x, Is.EqualTo(5f * Scale(level)).Within(1e-4f), "reach");
            Assert.That(Strike.ParamA.y, Is.EqualTo(3f * Scale(level)).Within(1e-4f), "height");
            Assert.That(Strike.ParamA.z, Is.EqualTo(6f), "the spin does not grow");
            Assert.That(Strike.ParamA.w, Is.EqualTo(0.3f), "nor the hit time");
            Assert.That(Strike.ParamB.x, Is.EqualTo(1.5f * Scale(level)).Within(1e-4f), "the wall's radius");
        }

        [TestCase(1)]
        [TestCase(3)]
        public void Tornado_Damage_GrowsWithTheLevelAndTheThrowDoesNot(int level)
        {
            var near = new FakeSwarmTarget { Position = new Vector3(3f, 0f, 0f) };
            _enemies.Add(near);
            Cast(Make<TornadoSong>(), level);

            _forms.Tick(0.3f, _hives);

            Assert.That(near.DamageTaken, Is.EqualTo(30f * Scale(level)).Within(1e-3f));
            Assert.That(near.LastKnockback.x, Is.EqualTo(6f).Within(1e-3f));
        }

        [Test]
        public void Tornado_Reach_GrowsWithTheLevel()
        {
            var edge = new FakeSwarmTarget { Position = new Vector3(5.5f, 0f, 0f) };
            _enemies.Add(edge);

            Cast(Make<TornadoSong>(), 1);
            _forms.Tick(0.3f, _hives);
            Assert.That(edge.DamageTaken, Is.EqualTo(0f), "beyond 5 m");

            Cast(Make<TornadoSong>(), 2);
            _forms.Tick(0.3f, _hives);
            Assert.That(edge.DamageTaken, Is.EqualTo(33.6f).Within(1e-3f), "within 5.6 m, for 30 x 1.12");
        }

        // ---- Meteor: height, reach, radius and damage ----

        [TestCase(1)]
        [TestCase(3)]
        public void Meteor_ThePictureGetsTheGrownHeightAndRadius(int level)
        {
            Cast(Make<MeteorSong>(), level, aimPoint: new Vector3(0f, 0f, 5f));

            Assert.That(Strike.ParamA.w, Is.EqualTo(12f * Scale(level)).Within(1e-4f), "height");
            Assert.That(Strike.ParamB.x, Is.EqualTo(0.8f), "the climb takes as long");
            Assert.That(Strike.ParamB.y, Is.EqualTo(0.5f), "and the dive");
            Assert.That(Strike.ParamB.z, Is.EqualTo(3f * Scale(level)).Within(1e-4f), "radius");
        }

        [Test]
        public void Meteor_Reach_GrowsWithTheLevel()
        {
            var far = new Vector3(0f, 0f, 20f);

            Cast(Make<MeteorSong>(), 1, aimPoint: far);
            Assert.That(Strike.ParamA.z, Is.EqualTo(18f).Within(1e-3f), "pulled in to 18 m");

            Cast(Make<MeteorSong>(), 2, aimPoint: far);
            Assert.That(Strike.ParamA.z, Is.EqualTo(20f).Within(1e-3f), "within 18 x 1.12 = 20.16 m");
        }

        [TestCase(1)]
        [TestCase(3)]
        public void Meteor_Damage_GrowsWithTheLevel(int level)
        {
            var target = new FakeSwarmTarget { Position = new Vector3(0f, 0f, 6f) };
            _enemies.Add(target);
            Cast(Make<MeteorSong>(), level, aim: target);

            _forms.Tick(1.3f, _hives);

            Assert.That(target.DamageTaken, Is.EqualTo(60f * Scale(level)).Within(1e-3f));
            Assert.That(target.LastStunSeconds, Is.EqualTo(0.5f), "the stun does not grow");
        }

        [Test]
        public void Meteor_Radius_GrowsWithTheLevel()
        {
            var beside = new FakeSwarmTarget { Position = new Vector3(3.2f, 0f, 6f) };
            _enemies.Add(beside);

            Cast(Make<MeteorSong>(), 1, aimPoint: new Vector3(0f, 0f, 6f));
            _forms.Tick(1.3f, _hives);
            Assert.That(beside.DamageTaken, Is.EqualTo(0f), "3.2 m from the impact, beyond 3 m");

            Cast(Make<MeteorSong>(), 2, aimPoint: new Vector3(0f, 0f, 6f));
            _forms.Tick(1.3f, _hives);
            Assert.That(beside.DamageTaken, Is.EqualTo(67.2f).Within(1e-3f), "within 3.36 m, for 60 x 1.12");
        }

        // ---- Dome: radius ----

        [TestCase(1)]
        [TestCase(3)]
        public void Dome_ThePictureGetsTheGrownRadius_AndItsShieldStaysTheSame(int level)
        {
            Cast(Make<DomeSong>(), level);

            Assert.That(_forms.Slot(SongSlot.Guard).ParamA.x, Is.EqualTo(2.2f * Scale(level)).Within(1e-4f));
            Assert.That(_forms.WaspLossFraction, Is.EqualTo(0.15f), "hits still cost 85% fewer wasps");
        }

        // ---- Gust: the radius it clears and pushes ----

        [TestCase(1)]
        [TestCase(3)]
        public void Gust_ClearsAndPulsesWithinTheGrownRadius(int level)
        {
            Cast(Make<GustSong>(), level);

            Assert.That(_smoke.LastRadius, Is.EqualTo(6f * Scale(level)).Within(1e-4f));
            Assert.That(_forms.GustRadius, Is.EqualTo(6f * Scale(level)).Within(1e-4f), "the picture's pulse reaches as far");
        }

        [Test]
        public void Gust_PushRadius_GrowsWithTheLevel()
        {
            var beside = new FakeSwarmTarget { Position = new Vector3(3.2f, 0f, 0f) };
            _enemies.Add(beside);

            Cast(Make<GustSong>(), 1);
            _forms.Tick(0.8f, _hives);
            Assert.That(beside.KnockbackCount, Is.EqualTo(0), "beyond 3 m");

            Cast(Make<GustSong>(), 2);
            _forms.Tick(0.8f, _hives);
            Assert.That(beside.KnockbackCount, Is.EqualTo(1), "within 3.36 m");
            Assert.That(beside.LastKnockback.x, Is.EqualTo(4f).Within(1e-3f), "the push is no faster");
        }

        // The ring starts where the shader starts it and grows to the grown radius, so the push still lands with the picture.
        [Test]
        public void Gust_RingRadius_GrowsToTheGrownRadiusFromTheSameStart()
        {
            var song = Make<GustSong>();

            Assert.That(song.RingRadius(0.1f, 1.24f), Is.EqualTo(-1f), "still drawing in");
            Assert.That(song.RingRadius(0.12f, 1.24f), Is.EqualTo(0.6f).Within(1e-4f));
            Assert.That(song.RingRadius(0.62f, 1.24f), Is.EqualTo(6f * 1.24f).Within(1e-4f));
            Assert.That(song.RingRadius(0.62f), Is.EqualTo(6f).Within(1e-4f), "the first level without a scale");
        }

        // ---- Strength comes from the level, never from the wasp count ----

        [Test]
        public void Songs_AtAHigherLevel_HitAsHardWithAThinSwarmAsWithAFullOne()
        {
            var target = new FakeSwarmTarget { Position = new Vector3(3f, 0f, 0f) };
            _enemies.Add(target);
            Cast(Make<TornadoSong>(), 3);
            _forms.Tick(0.3f, _hives);
            float full = target.DamageTaken;
            Vector4 fullPicture = Strike.ParamA;

            _body.Lose(_body.Count - _body.CollapseLine);
            Assert.That(_body.IsCollapsed, Is.False);
            Cast(Make<TornadoSong>(), 3);
            _forms.Tick(0.3f, _hives);

            Assert.That(target.DamageTaken - full, Is.EqualTo(full).Within(1e-4f), "the second hit, at 96 wasps, is as hard as the first at 800");
            Assert.That(Strike.ParamA, Is.EqualTo(fullPicture), "and is just as big");
        }

        private float Scale(int level)
        {
            return _levels.SizeScale(level);
        }

        private T Make<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }

        private void Cast(SwarmSongEffect song, int level, ISwarmTarget aim = null, Vector3 aimPoint = default)
        {
            SwarmSongContext context = Context(level, aim, aimPoint);
            song.Cast(in context);
        }

        private SwarmSongContext Context(int level, ISwarmTarget aim = null, Vector3 aimPoint = default, ISongAim aimChooser = null)
        {
            return new SwarmSongContext(
                Vector3.zero, aim, aimPoint, _body, _forms, _fury, _hives, _enemies, _smoke, aimChooser, false,
                _levels.SizeScale(level), _levels.DamageScale(level));
        }
    }
}
