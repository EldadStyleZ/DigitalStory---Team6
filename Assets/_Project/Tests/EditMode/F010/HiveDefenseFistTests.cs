using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Songs-v2 R: the giant's hand launch. The right hand's button (R2, right mouse, Space) and the left's (L2, left mouse,
    /// Left Shift) each thrust that arm, and at the end of the thrust its hand and forearm fly as one fist to the target, hit
    /// all around it, and stream back while the arm grows again. The two hands never wait for each other.
    /// </summary>
    public sealed class HiveDefenseFistTests
    {
        private const float ColossusSeconds = 12f;
        private const float Step = HiveDefenseWorld.Step;

        private HiveDefenseWorld _world;
        private List<ColossusHand> _thrown;
        private List<ColossusHand> _launched;
        private List<ColossusHand> _landed;
        private List<SongHit> _hits;

        [TearDown]
        public void TearDown()
        {
            _world?.Dispose();
            _world = null;
        }

        // A giant of the given seconds at the middle among sticks that stand still where they spawned, in spawn order, with
        // every fist event and hit recorded.
        private List<BeekeeperBrain> GiantAmong(float seconds, params Vector3[] spawns)
        {
            _world = new HiveDefenseWorld(new[] { new WaveEntry(spawns.Length, 0), new WaveEntry(1, 0) }, spawns: spawns, firstFarmSeconds: 3f);
            var keepers = new List<BeekeeperBrain>();
            _world.Session.BeekeeperSpawned += keeper =>
            {
                keeper.Stun(1000f);
                keepers.Add(keeper);
            };

            Assert.That(_world.RunUntil(() => _world.Session.ActiveBeekeeperCount == spawns.Length, 3f + (spawns.Length * 3f)), Is.True);
            Assert.That(_world.Session.Forms.StartColossus(seconds), Is.True);
            _world.Run(Step);
            Listen();
            return keepers;
        }

        private void Listen()
        {
            HiveDefenseSession session = _world.Session;
            _thrown = new List<ColossusHand>();
            _launched = new List<ColossusHand>();
            _landed = new List<ColossusHand>();
            _hits = new List<SongHit>();
            session.ColossusFistThrown += _thrown.Add;
            session.ColossusFistLaunched += _launched.Add;
            session.ColossusFistLanded += _landed.Add;
            session.SongLanded += _hits.Add;
        }

        private void Press(ColossusHand hand)
        {
            _world.RunWith(Step, _world.Frame(rightHand: hand == ColossusHand.Right, leftHand: hand == ColossusHand.Left));
        }

        [Test]
        public void RightHand_OnAFarBeekeeper_ThrustsThenFliesAndHitsAllAroundIt_AndFillsNoFury()
        {
            List<BeekeeperBrain> keepers = GiantAmong(
                ColossusSeconds, new Vector3(0f, 0f, 8f), new Vector3(0.8f, 0f, 8.6f), new Vector3(0f, 0f, -16f));
            HiveDefenseSession session = _world.Session;
            float health = _world.Stick.Health;

            Press(ColossusHand.Right);
            FistPhase thrusting = session.FistPhaseOf(ColossusHand.Right);
            _world.Run(_world.Colossus.FistThrustSeconds - (Step * 3f));
            int launchedEarly = _launched.Count;
            _world.Run(Step * 4f);
            FistPhase flying = session.FistPhaseOf(ColossusHand.Right);
            float flight = session.FistFlightSeconds(ColossusHand.Right);
            _world.Run(flight + (Step * 2f));

            Assert.That(_thrown, Is.EqualTo(new[] { ColossusHand.Right }));
            Assert.That(thrusting, Is.EqualTo(FistPhase.Thrust));
            Assert.That(launchedEarly, Is.EqualTo(0), "the fist leaves at the end of the thrust");
            Assert.That(flying, Is.EqualTo(FistPhase.Flight));
            Assert.That(flight, Is.EqualTo(8f / _world.Colossus.FistSpeed).Within(0.05f), "the fist's speed over the 8 m");
            Assert.That(_launched, Is.EqualTo(new[] { ColossusHand.Right }));
            Assert.That(_landed, Is.EqualTo(new[] { ColossusHand.Right }));
            Assert.That(session.FistPhaseOf(ColossusHand.Right), Is.EqualTo(FistPhase.Regrow));
            Assert.That(keepers[0].Health, Is.EqualTo(health - _world.Colossus.FistDamage).Within(0.01f));
            Assert.That(keepers[1].Health, Is.EqualTo(health - _world.Colossus.FistDamage).Within(0.01f), "within the fist's radius");
            Assert.That(keepers[2].Health, Is.EqualTo(health), "out of the fist's range and radius");
            Assert.That(_hits.Count, Is.EqualTo(2));
            Assert.That(_hits[0].Form, Is.EqualTo(SwarmForm.Colossus));
            Assert.That(_hits[0].IsHeavy, Is.True);
            Assert.That(session.Fury.Value, Is.EqualTo(0f), "like the giant's other blows it fills no Fury");
            Assert.That(session.ColossusFacing.z, Is.EqualTo(1f).Within(0.01f), "a giant standing still turns to its target");
        }

        [Test]
        public void Hand_WhileItsArmGrowsBack_CannotLaunch_ButTheOtherHandCan()
        {
            GiantAmong(ColossusSeconds, new Vector3(0f, 0f, 8f));
            HiveDefenseSession session = _world.Session;
            ColossusConfig giant = _world.Colossus;

            Press(ColossusHand.Right);
            Assert.That(_world.RunUntil(() => session.FistPhaseOf(ColossusHand.Right) == FistPhase.Regrow, 2f), Is.True);
            _world.Run(giant.FistRegrowSeconds * 0.5f);
            float halfGrown = session.FistCooldown01(ColossusHand.Right);
            Press(ColossusHand.Right);
            int whileGrowing = _thrown.Count;
            Press(ColossusHand.Left);
            int withTheOther = _thrown.Count;
            _world.Run(giant.FistRegrowSeconds * 0.5f);
            Press(ColossusHand.Right);

            Assert.That(halfGrown, Is.EqualTo(0.5f).Within(0.05f));
            Assert.That(whileGrowing, Is.EqualTo(1), "the right arm is still missing");
            Assert.That(withTheOther, Is.EqualTo(2), "the left hand does not wait for the right");
            Assert.That(_thrown, Is.EqualTo(new[] { ColossusHand.Right, ColossusHand.Left, ColossusHand.Right }));
        }

        [Test]
        public void BothHands_OnOneTick_LaunchBoth()
        {
            GiantAmong(ColossusSeconds, new Vector3(0f, 0f, 8f));

            _world.RunWith(Step, _world.Frame(rightHand: true, leftHand: true));
            _world.Run(1f);

            Assert.That(_thrown, Is.EqualTo(new[] { ColossusHand.Right, ColossusHand.Left }));
            Assert.That(_landed.Count, Is.EqualTo(2));
            Assert.That(_world.Session.ActiveBeekeeper(0).Health, Is.EqualTo(_world.Stick.Health - (2f * _world.Colossus.FistDamage)).Within(0.01f));
        }

        // After review: in the chase view the mouse and the right stick turn the camera, so the way the camera looks is the
        // aim. A fist flies to the beekeeper nearest that way, within FistAimConeDegrees of it and within the fist's range.
        [Test]
        public void Fist_FliesToTheBeekeeperTheCameraLooksAt()
        {
            var ahead = new Vector3(0f, 0f, 9f);
            var right = new Vector3(9f, 0f, 0f);
            List<BeekeeperBrain> keepers = GiantAmong(ColossusSeconds, ahead, right);
            HiveDefenseSession session = _world.Session;
            ahead = StandingNear(keepers, ahead);
            right = StandingNear(keepers, right);

            _world.RunWith(Step, _world.Frame(rightHand: true, cameraForward: Vector3.right));
            Vector3 rightFist = session.FistTarget(ColossusHand.Right);
            _world.RunWith(Step, _world.Frame(leftHand: true, cameraForward: new Vector3(0f, -0.6f, 0.8f)));
            Vector3 leftFist = session.FistTarget(ColossusHand.Left);

            Assert.That(GroundDistance(rightFist, right), Is.LessThan(0.01f), "the camera looked right");
            Assert.That(GroundDistance(leftFist, ahead), Is.LessThan(0.01f), "the camera looked ahead, and down at the ground");
        }

        // The mouse never moves a fist's target, so a click aims as Space and R2 do.
        [Test]
        public void Fist_AClickAimsAsSpaceDoes()
        {
            var ahead = new Vector3(0f, 0f, 9f);
            var right = new Vector3(9f, 0f, 0f);
            List<BeekeeperBrain> keepers = GiantAmong(ColossusSeconds, ahead, right);
            HiveDefenseSession session = _world.Session;
            ahead = StandingNear(keepers, ahead);
            right = StandingNear(keepers, right);

            _world.RunWith(Step, _world.Frame(rightHand: true, aim: right, isMouseLive: true, cameraForward: Vector3.forward));
            Vector3 clicked = session.FistTarget(ColossusHand.Right);
            _world.RunWith(Step, _world.Frame(leftHand: true, cameraForward: Vector3.forward));
            Vector3 pressed = session.FistTarget(ColossusHand.Left);

            Assert.That(GroundDistance(clicked, ahead), Is.LessThan(0.01f), "the click flew where the camera looks");
            Assert.That(GroundDistance(pressed, ahead), Is.LessThan(0.01f));
        }

        // With no beekeeper near the camera's way, auto-aim's most threatening one in range, never empty ground.
        [Test]
        public void Fist_WithNoBeekeeperWhereTheCameraLooks_FliesToOneInRange()
        {
            var ahead = new Vector3(0f, 0f, 9f);
            var right = new Vector3(9f, 0f, 0f);
            List<BeekeeperBrain> keepers = GiantAmong(ColossusSeconds, ahead, right);
            HiveDefenseSession session = _world.Session;
            ahead = StandingNear(keepers, ahead);
            right = StandingNear(keepers, right);

            _world.RunWith(Step, _world.Frame(rightHand: true, cameraForward: Vector3.back));
            Vector3 target = session.FistTarget(ColossusHand.Right);

            float nearest = Mathf.Min(GroundDistance(target, ahead), GroundDistance(target, right));
            Assert.That(nearest, Is.LessThan(0.01f), "the fist flew at empty ground");
        }

        // With no beekeeper in range at all, along the camera's way to the fist's range; the mouse does not move it.
        [Test]
        public void Fist_WithNoBeekeeperInRange_FliesAlongTheCameraToItsRange()
        {
            _world = new HiveDefenseWorld();
            HiveDefenseSession session = _world.Session;
            Assert.That(session.Forms.StartColossus(ColossusSeconds), Is.True);
            _world.Run(Step);
            Listen();

            _world.RunWith(Step, _world.Frame(rightHand: true, aim: new Vector3(0f, 0f, 30f), isMouseLive: true, cameraForward: Vector3.right));
            Vector3 target = session.FistTarget(ColossusHand.Right);
            _world.Run(_world.Colossus.FistThrustSeconds + Step);
            float flight = session.FistFlightSeconds(ColossusHand.Right);

            Assert.That(target.x, Is.EqualTo(_world.Colossus.FistRange).Within(1e-3f));
            Assert.That(target.z, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(flight, Is.EqualTo(_world.Colossus.FistRange / _world.Colossus.FistSpeed).Within(0.03f));
            Assert.That(session.ColossusFacing.x, Is.EqualTo(1f).Within(1e-3f), "it turns to where the fist flies");
        }

        private static float GroundDistance(Vector3 a, Vector3 b)
        {
            return new Vector2(a.x - b.x, a.z - b.z).magnitude;
        }

        // Where the stick that spawned at the spot stands.
        private static Vector3 StandingNear(List<BeekeeperBrain> keepers, Vector3 spot)
        {
            BeekeeperBrain nearest = null;
            for (int index = 0; index < keepers.Count; index++)
            {
                if (nearest == null || GroundDistance(keepers[index].Position, spot) < GroundDistance(nearest.Position, spot))
                {
                    nearest = keepers[index];
                }
            }

            Assert.That(nearest, Is.Not.Null);
            Assert.That(GroundDistance(nearest.Position, spot), Is.LessThan(2f), "no stick spawned at " + spot);
            return nearest.Position;
        }

        [Test]
        public void Fist_WithNoBeekeeperInRange_FliesAheadAndHitsNothing()
        {
            List<BeekeeperBrain> keepers = GiantAmong(ColossusSeconds, new Vector3(0f, 0f, -20f));

            Press(ColossusHand.Left);
            _world.Run(1.5f);

            Assert.That(_launched, Is.EqualTo(new[] { ColossusHand.Left }));
            Assert.That(_landed, Is.EqualTo(new[] { ColossusHand.Left }));
            Assert.That(keepers[0].Health, Is.EqualTo(_world.Stick.Health));
            Assert.That(_hits, Is.Empty);
        }

        [Test]
        public void Fist_FollowsItsBeekeeperAsItWalks()
        {
            _world = new HiveDefenseWorld(new[] { new WaveEntry(1, 0) }, spawns: new[] { new Vector3(0f, 0f, 10f) }, firstFarmSeconds: 3f);
            HiveDefenseSession session = _world.Session;
            Assert.That(_world.RunUntil(() => session.ActiveBeekeeperCount == 1, 6f), Is.True);
            Assert.That(session.Forms.StartColossus(ColossusSeconds), Is.True);
            _world.Run(Step);
            Listen();
            BeekeeperBrain keeper = session.ActiveBeekeeper(0);

            Press(ColossusHand.Right);
            Assert.That(_world.RunUntil(() => _landed.Count == 1, 2f), Is.True);

            Vector3 hit = session.FistTarget(ColossusHand.Right);
            Assert.That(keeper.Health, Is.EqualTo(_world.Stick.Health - _world.Colossus.FistDamage).Within(0.01f), "it hit the walking stick");
            Assert.That(hit.x, Is.EqualTo(keeper.Position.x).Within(0.6f));
            Assert.That(hit.z, Is.EqualTo(keeper.Position.z).Within(0.6f));
        }

        // Plan section H: the giant's blows follow the level it stood up at.
        [Test]
        public void Fist_AfterANest_HitsHarder()
        {
            var nests = (Vector3[])HiveDefenseWorld.DefaultNests.Clone();
            nests[0] = Vector3.zero;
            _world = new HiveDefenseWorld(new[] { new WaveEntry(1, 0) }, spawns: new[] { new Vector3(0f, 0f, 8f) }, nests: nests, firstFarmSeconds: 3f);
            HiveDefenseSession session = _world.Session;
            _world.Session.BeekeeperSpawned += keeper => keeper.Stun(1000f);
            Assert.That(_world.RunUntil(() => session.ActiveBeekeeperCount == 1, 5f), Is.True);
            session.Forms.StartColossus(ColossusSeconds);
            _world.Run(Step);
            Listen();
            BeekeeperBrain keeper = session.ActiveBeekeeper(0);

            Press(ColossusHand.Right);
            _world.Run(1f);

            Assert.That(keeper.Health, Is.EqualTo(_world.Stick.Health - (_world.Colossus.FistDamage * session.DamageScale)).Within(0.01f));
            Assert.That(session.DamageScale, Is.GreaterThan(1f));
        }

        [Test]
        public void Giant_EndingWhileAFistFlies_DropsItWithNoHit()
        {
            List<BeekeeperBrain> keepers = GiantAmong(0.5f, new Vector3(0f, 0f, 12f));
            HiveDefenseSession session = _world.Session;

            Press(ColossusHand.Right);
            Assert.That(_world.RunUntil(() => _launched.Count == 1, 1f), Is.True);
            Assert.That(_world.RunUntil(() => !session.IsColossusActive, 1f), Is.True);
            bool wasFlying = _landed.Count == 0;
            _world.Run(2f);

            Assert.That(wasFlying, Is.True, "the giant ended before the fist could hit");
            Assert.That(_landed, Is.Empty);
            Assert.That(keepers[0].Health, Is.EqualTo(_world.Stick.Health));
            Assert.That(session.FistPhaseOf(ColossusHand.Right), Is.EqualTo(FistPhase.Ready));
            Assert.That(session.FistCooldown01(ColossusHand.Right), Is.EqualTo(0f));
        }

        // What the hand buttons did before the giant had hands: nothing outside the giant, and so it stays.
        [Test]
        public void HandButtons_WithoutTheGiant_DoNothing()
        {
            _world = new HiveDefenseWorld(withSongs: true);
            HiveDefenseSession session = _world.Session;
            Listen();
            Vector3 core = session.CorePosition;

            _world.RunWith(Step, _world.Frame(rightHand: true, leftHand: true));
            _world.Run(1f);

            Assert.That(_thrown, Is.Empty);
            Assert.That(session.FistPhaseOf(ColossusHand.Right), Is.EqualTo(FistPhase.Ready));
            Assert.That(session.FistPhaseOf(ColossusHand.Left), Is.EqualTo(FistPhase.Ready));
            Assert.That(session.CorePosition, Is.EqualTo(core));
            Assert.That(session.Caster.RecentNoteCount, Is.EqualTo(0), "no note was played");
        }
    }
}
