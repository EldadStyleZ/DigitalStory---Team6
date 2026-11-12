using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// The picture's times in WaspSwarm.compute against the rules', so an impact hurts on the frame it visibly lands. The
    /// songs send their impact times to the picture; the shader's own numbers are what it reads while a slot sends 0, and
    /// the Lance's flight, which both sides share. A release's choreography must also fit the slot's return beat
    /// (<see cref="SquadConfig.ReturnSeconds"/>): the slot empties then, and wasps still on their way home would switch to
    /// the river in mid-flight.
    /// </summary>
    public sealed class WaspSwarmTimingTests
    {
        private const string ShaderPath = "_Project/Art/F010/Shaders/WaspSwarm.compute";
        private const float Tolerance = 1e-5f;

        private static readonly Regex FloatConstant = new Regex(@"static\s+const\s+float\s+(\w+)\s*=\s*(-?[0-9]*\.?[0-9]+)\s*;");

        private readonly List<ScriptableObject> _assets = new List<ScriptableObject>();
        private Dictionary<string, float> _constants;

        [OneTimeSetUp]
        public void ReadTheShader()
        {
            string text = File.ReadAllText(Path.Combine(Application.dataPath, ShaderPath));
            _constants = new Dictionary<string, float>();
            foreach (Match match in FloatConstant.Matches(text))
            {
                _constants[match.Groups[1].Value] = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (ScriptableObject asset in _assets)
            {
                Object.DestroyImmediate(asset);
            }

            _assets.Clear();
        }

        [Test]
        public void Lance_ThePicturesFlight_IsTheSongs_SoTheTipLandsOnTheHit()
        {
            Assert.That(Constant("LanceFlightSeconds"), Is.EqualTo(LanceSong.FlightSeconds).Within(Tolerance));
        }

        [Test]
        public void Lance_TheDrawThePictureReadsFor0_IsTheSongsDefault()
        {
            Assert.That(Constant("LanceDrawSeconds"), Is.EqualTo(Make<LanceSong>().DrawSeconds).Within(Tolerance));
        }

        [Test]
        public void Hydra_TheBiteThePictureReadsFor0_IsTheSongsDefault()
        {
            Assert.That(Constant("HydraBiteSeconds"), Is.EqualTo(Make<HydraSong>().BiteSeconds).Within(Tolerance));
        }

        [Test]
        public void Sting_TheArrivalThePictureReadsFor0_IsTheSongsDefault()
        {
            Assert.That(Constant("StingFlightSeconds"), Is.EqualTo(Make<StingSong>().ArriveSeconds).Within(Tolerance));
        }

        // Each sting reports its damage as one hit on every squeeze (plan section O), so the squeeze the picture draws must
        // fall on the same beat.
        [Test]
        public void Sting_ThePicturesSqueeze_IsTheSongsSqueeze_SoEachHitFallsOnOne()
        {
            Assert.That(Constant("StingTickSeconds"), Is.EqualTo(Make<StingSong>().SqueezeSeconds).Within(Tolerance));
        }

        [Test]
        public void Nest_TheFuseThePictureReadsFor0_IsTheSongsDefault()
        {
            Assert.That(Constant("NestFuseSeconds"), Is.EqualTo(Make<NestSong>().FuseSeconds).Within(Tolerance));
        }

        [Test]
        public void Scythe_TheCutThePictureReadsFor0_IsTheSongsDefault()
        {
            Assert.That(Constant("ScytheHitSeconds"), Is.EqualTo(Make<ScytheSong>().HitSeconds).Within(Tolerance));
        }

        // The shared Home, and each song with a choreography of its own after the release.
        [TestCase("HomeSeconds", "HomeStagger")]
        [TestCase("StingPopSeconds", "StingHomeSeconds", "StingHomeStagger")]
        [TestCase("MeteorBurstSeconds", "MeteorHomeSeconds", "MeteorHomeStagger")]
        [TestCase("NestBurstHoldSeconds", "NestHomeSeconds", "NestHomeStagger")]
        [TestCase("HoneycombPopStagger", "HoneycombPopSeconds", "HoneycombHomeSeconds")]
        public void Release_TheLastWaspIsHomeByTheEndOfTheSlotsReturnBeat(params string[] beats)
        {
            float seconds = 0f;
            foreach (string beat in beats)
            {
                seconds += Constant(beat);
            }

            Assert.That(seconds, Is.LessThanOrEqualTo(Make<SquadConfig>().ReturnSeconds + Tolerance), string.Join(" + ", beats));
        }

        private float Constant(string name)
        {
            Assert.That(_constants.ContainsKey(name), Is.True, $"WaspSwarm.compute has no static const float {name}");
            return _constants[name];
        }

        private T Make<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }
    }
}
