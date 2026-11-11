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
    /// Uses the default configs, except that the first wave comes after 1 s. The hives stand far from the core
    /// unless a test moves them, so a beekeeper that spawns beside the core goes for the core.
    /// </summary>
    public sealed class HiveDefenseSessionTests
    {
        private static readonly Vector3 Origin = Vector3.zero;

        // H K J.
        private static readonly int[] DomeHoles = { 0, 2, 1 };

        private HiveDefenseWorld _world;

        [TearDown]
        public void TearDown()
        {
            _world?.Dispose();
            _world = null;
        }

        private static WaveEntry[] OneStick() => new[] { new WaveEntry(1, 0) };

        // Stuns every beekeeper for good the moment it spawns, so it stays where it stood. Returns them in spawn order.
        private List<BeekeeperBrain> StandStillAsTheySpawn()
        {
            var keepers = new List<BeekeeperBrain>();
            _world.Session.BeekeeperSpawned += keeper =>
            {
                keeper.Stun(1000f);
                keepers.Add(keeper);
            };

            return keepers;
        }

        // A cast context on the session's own swarm, forms and hives, with no beekeepers, for a test's own song.
        private SwarmSongContext SongContext()
        {
            HiveDefenseSession session = _world.Session;
            return new SwarmSongContext(
                _world.Core, null, _world.Core, session.Swarm, session.Forms, session.Fury, session.Hives, new List<ISwarmTarget>(), session.Smoke);
        }

        // The swarm starts full at its capacity (plan section H), so a test of wasps flowing in first loses half of them to
        // leave room. Returns the count left.
        private int LeaveRoom()
        {
            SwarmBody swarm = _world.Session.Swarm;
            swarm.Lose(swarm.Count / 2);
            return swarm.Count;
        }

        // A stick that spawns 1 m from the core, which stands at the origin.
        private HiveDefenseWorld StickBesideCore(WaveEntry[] waves = null, bool withSongs = false)
        {
            _world = new HiveDefenseWorld(waves ?? OneStick(), spawns: new[] { new Vector3(1f, 0f, 0f) }, withSongs: withSongs);
            return _world;
        }

        [Test]
        public void New_Session_IsPlayingWithAFullSwarmAndTwoStandingHives()
        {
            _world = new HiveDefenseWorld();
            HiveDefenseSession session = _world.Session;

            Assert.That(session.State, Is.EqualTo(HiveDefenseState.Playing));
            Assert.That(session.Swarm.Count, Is.EqualTo(_world.Swarm.StartingWasps));
            Assert.That(session.Swarm.Count, Is.EqualTo(session.Swarm.Capacity), "full");
            Assert.That(session.Hives.Count, Is.EqualTo(2));
            Assert.That(session.Hives.StandingCount, Is.EqualTo(2));
            Assert.That(session.Hives[0].Position, Is.EqualTo(new Vector3(-40f, 0f, 40f)));
            Assert.That(session.ActiveBeekeeperCount, Is.EqualTo(0));
            Assert.That(session.IsColossusActive, Is.False);
            Assert.That(session.CoreSpeedMultiplier, Is.EqualTo(1f));
            Assert.That(session.IsCoreRooted, Is.False);
            Assert.That(session.AimTarget, Is.Null);
        }

        [Test]
        public void Tick_BothHivesDestroyed_IsLost()
        {
            _world = new HiveDefenseWorld();
            var changes = new List<HiveDefenseState>();
            _world.Session.StateChanged += changes.Add;

            _world.Session.Hives[0].Damage(_world.Hive.Health);
            _world.Run(HiveDefenseWorld.Step);
            Assert.That(_world.Session.State, Is.EqualTo(HiveDefenseState.Playing), "one hive still stands");

            _world.Session.Hives[1].Damage(_world.Hive.Health);
            _world.Run(HiveDefenseWorld.Step);

            Assert.That(_world.Session.State, Is.EqualTo(HiveDefenseState.Lost));
            Assert.That(changes, Is.EqualTo(new[] { HiveDefenseState.Lost }));
        }

        [Test]
        public void Tick_AfterLost_DoesNothing()
        {
            _world = new HiveDefenseWorld();
            _world.Session.Hives[0].Damage(_world.Hive.Health);
            _world.Session.Hives[1].Damage(_world.Hive.Health);
            _world.Run(HiveDefenseWorld.Step);
            int wasps = _world.Session.Swarm.Count;
            float untilWave = _world.Session.FarmSecondsLeft;

            _world.Run(5f);

            Assert.That(_world.Session.State, Is.EqualTo(HiveDefenseState.Lost));
            Assert.That(_world.Session.Phase, Is.EqualTo(MatchPhase.Farm));
            Assert.That(_world.Session.FarmSecondsLeft, Is.EqualTo(untilWave));
            Assert.That(_world.Session.ActiveBeekeeperCount, Is.EqualTo(0));
            Assert.That(_world.Session.Swarm.Count, Is.EqualTo(wasps));
        }

        [Test]
        public void Tick_LastWaveCleared_IsWon()
        {
            _world = new HiveDefenseWorld(new[] { new WaveEntry(2, 0) });
            var changes = new List<HiveDefenseState>();
            _world.Session.StateChanged += changes.Add;

            bool spawned = _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 2, 10f);
            Assert.That(spawned, Is.True);
            _world.Run(1f);
            Assert.That(_world.Session.State, Is.EqualTo(HiveDefenseState.Playing), "the beekeepers still live");

            _world.KillAllBeekeepers();
            _world.Run(0.5f);

            Assert.That(_world.Session.Waves.AllWavesCleared, Is.True);
            Assert.That(_world.Session.State, Is.EqualTo(HiveDefenseState.Won));
            Assert.That(changes, Is.EqualTo(new[] { HiveDefenseState.Won }));
        }

        [Test]
        public void Tick_AfterWon_DoesNothing()
        {
            _world = new HiveDefenseWorld();
            _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 1, 10f);
            _world.KillAllBeekeepers();
            _world.Run(0.5f);
            Assert.That(_world.Session.State, Is.EqualTo(HiveDefenseState.Won));
            _world.Session.Hives[0].Damage(_world.Hive.Health);
            _world.Session.Hives[1].Damage(_world.Hive.Health);

            _world.Run(1f);

            Assert.That(_world.Session.State, Is.EqualTo(HiveDefenseState.Won), "a won match cannot be lost afterwards");
        }

        [Test]
        public void Tick_FirstFarmPhaseNotOver_NoBeekeeperYet()
        {
            _world = new HiveDefenseWorld();

            _world.Run(0.5f);

            Assert.That(_world.Session.Phase, Is.EqualTo(MatchPhase.Farm));
            Assert.That(_world.Session.ActiveBeekeeperCount, Is.EqualTo(0));
            Assert.That(_world.Session.State, Is.EqualTo(HiveDefenseState.Playing));
        }

        [Test]
        public void Tick_HugeDeltaTime_IsClampedToTheMaximumStep()
        {
            _world = new HiveDefenseWorld();
            float before = _world.Session.FarmSecondsLeft;

            _world.Session.Tick(_world.Frame(), 10f);

            Assert.That(_world.Session.FarmSecondsLeft, Is.EqualTo(before - _world.Swarm.MaxDeltaTime).Within(0.0001f));
        }

        [Test]
        public void SpawnRequest_PutsALivingBeekeeperAtTheSpawnPoint()
        {
            _world = new HiveDefenseWorld(spawns: new[] { new Vector3(30f, 0f, -30f), new Vector3(-30f, 0f, -30f) });
            var spawned = new List<BeekeeperBrain>();
            _world.Session.BeekeeperSpawned += spawned.Add;

            bool arrived = _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 1, 10f);

            Assert.That(arrived, Is.True);
            BeekeeperBrain keeper = _world.Session.ActiveBeekeeper(0);
            Assert.That(keeper.IsAlive, Is.True);
            Assert.That(keeper.Position.x, Is.EqualTo(30f).Within(0.01f));
            Assert.That(keeper.Position.z, Is.EqualTo(-30f).Within(0.01f));
            Assert.That(spawned, Is.EqualTo(new[] { keeper }));
        }

        [Test]
        public void SpawnRequest_NextBeekeeper_GoesToTheNextSpawnPoint()
        {
            _world = new HiveDefenseWorld(new[] { new WaveEntry(2, 0) }, spawns: new[] { new Vector3(30f, 0f, -30f), new Vector3(-30f, 0f, -30f) });

            _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 2, 10f);

            Assert.That(_world.Session.ActiveBeekeeperCount, Is.EqualTo(2));
            Assert.That(_world.Session.ActiveBeekeeper(0).Position.x, Is.GreaterThan(0f));
            Assert.That(_world.Session.ActiveBeekeeper(1).Position.x, Is.LessThan(0f));
        }

        [Test]
        public void SpawnRequest_Smoker_UsesTheSmokerConfig()
        {
            _world = new HiveDefenseWorld(new[] { new WaveEntry(0, 1) });

            _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 1, 10f);

            Assert.That(_world.Session.ActiveBeekeeper(0).Config, Is.SameAs(_world.Smoker));
        }

        [Test]
        public void DeadBeekeeper_LeavesTheActiveListAndRaisesBeekeeperDied()
        {
            _world = new HiveDefenseWorld(new[] { new WaveEntry(2, 0) });
            var died = new List<BeekeeperBrain>();
            _world.Session.BeekeeperDied += died.Add;
            _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 2, 10f);
            BeekeeperBrain victim = _world.Session.ActiveBeekeeper(0);

            victim.TakeDamage(10000f);
            _world.Run(HiveDefenseWorld.Step);

            Assert.That(died, Is.EqualTo(new[] { victim }));
            Assert.That(_world.Session.ActiveBeekeeperCount, Is.EqualTo(1));
        }

        [Test]
        public void StickHitOnCore_RemovesTheWaspsItKnocksOff()
        {
            StickBesideCore();
            _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 1, 10f);

            bool hit = _world.RunUntil(() => _world.Session.Swarm.Count < _world.Swarm.StartingWasps, 10f);

            Assert.That(hit, Is.True);
            Assert.That(_world.Session.Swarm.Count, Is.EqualTo(_world.Swarm.StartingWasps - _world.Stick.WaspsKnockedOff));
        }

        [Test]
        public void StickHitOnCore_UnderTheDome_RemovesFewerWasps()
        {
            StickBesideCore(withSongs: true);
            _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 1, 10f);
            _world.Play(DomeHoles);
            Assert.That(_world.Session.Forms.IsDomed, Is.True);

            // Counted from after the cast: in a survive phase the Dome itself costs wasps when it is cast.
            int afterCast = _world.Session.Swarm.Count;
            bool hit = _world.RunUntil(() => _world.Session.Swarm.Count < afterCast, 10f);

            int lost = afterCast - _world.Session.Swarm.Count;
            var dome = (DomeSong)_world.Songs[HiveDefenseWorld.DomeIndex];
            Assert.That(hit, Is.True);
            Assert.That(lost, Is.EqualTo(Mathf.RoundToInt(_world.Stick.WaspsKnockedOff * dome.LossFraction)));
            Assert.That(lost, Is.LessThan(_world.Stick.WaspsKnockedOff));
        }

        [Test]
        public void CoreSpeedMultiplier_UnderTheDome_IsTheDomeAssetsMultiplier()
        {
            _world = new HiveDefenseWorld(withSongs: true);
            var dome = (DomeSong)_world.Songs[HiveDefenseWorld.DomeIndex];
            JsonUtility.FromJsonOverwrite("{\"<SpeedMultiplier>k__BackingField\":0.3}", dome);

            _world.Play(DomeHoles);

            Assert.That(_world.Session.CoreSpeedMultiplier, Is.EqualTo(0.3f).Within(1e-5f));
        }

        [Test]
        public void StickHitOnCore_UnderTheDome_RipplesTheDomeFromTheHittersSide()
        {
            StickBesideCore(withSongs: true);
            _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 1, 10f);
            _world.Play(DomeHoles);

            // Counted from after the cast: in a survive phase the Dome itself costs wasps when it is cast.
            int afterCast = _world.Session.Swarm.Count;
            bool hit = _world.RunUntil(() => _world.Session.Swarm.Count < afterCast, 10f);

            Vector4 dome = _world.Session.Forms.Slot(SongSlot.Guard).ParamA;
            Assert.That(hit, Is.True);
            Assert.That(dome.w, Is.GreaterThan(0f), "the dome's seconds when it blocked the hit");
            Assert.That(dome.y, Is.EqualTo(1f).Within(0.05f), "the stick stands on the core's +x side");
            Assert.That(dome.z, Is.EqualTo(0f).Within(0.3f));
        }

        // Hive Call now ends when the stores run dry, so the stores flow slowly here to last until the stick's hit.
        [Test]
        public void StickHitOnCore_DuringHiveCallUnderTheDome_KeepsHiveCall()
        {
            StickBesideCore(withSongs: true);
            JsonUtility.FromJsonOverwrite("{\"<CollectPerSecond>k__BackingField\":1}", _world.Hive);
            _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 1, 10f);
            int hits = 0;
            _world.Session.ActiveBeekeeper(0).CoreHit += brain => hits++;
            _world.Play(DomeHoles);
            Assert.That(_world.Session.Forms.StartHiveCall(), Is.True);

            bool hit = _world.RunUntil(() => hits > 0, 10f);

            Assert.That(hit, Is.True);
            Assert.That(_world.Session.Forms.Current, Is.EqualTo(SwarmForm.HiveCall), "Dome then Hive Call is the safe refill");
        }

        // Plan section A: Hive Call ends when the core moves. The core is held in place while it lasts, so its position
        // never changes: asking to move must end it, or the held key would be refused without a sign.
        [Test]
        public void HiveCall_ThePlayerAsksToMove_EndsIt_ThoughTheCoreIsHeldInPlace()
        {
            _world = new HiveDefenseWorld();
            LeaveRoom();
            Assert.That(_world.Session.Forms.StartHiveCall(), Is.True);
            _world.Run(HiveDefenseWorld.Step);
            Assert.That(_world.Session.Forms.Current, Is.EqualTo(SwarmForm.HiveCall), "standing still keeps it");

            _world.Session.Tick(_world.Frame(move: new Vector2(1f, 0f)), HiveDefenseWorld.Step);

            Assert.That(_world.Session.Forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_world.Session.IsCoreRooted, Is.False);
        }

        [Test]
        public void HiveCall_UnderTheDome_ThePlayerAsksToMove_EndsHiveCallAndKeepsTheDome()
        {
            _world = new HiveDefenseWorld(withSongs: true);
            _world.Play(DomeHoles);
            Assert.That(_world.Session.Forms.StartHiveCall(), Is.True);

            _world.Session.Tick(_world.Frame(move: new Vector2(0f, -1f)), HiveDefenseWorld.Step);

            Assert.That(_world.Session.Forms.Current, Is.EqualTo(SwarmForm.River), "a hit would not end it under the dome, but moving does");
            Assert.That(_world.Session.Forms.IsDomed, Is.True);
        }

        // Hive Call now fills the swarm from the stores and ends when they run dry, so the stores flow slowly here, the hit
        // is counted at the stick, and the call is seen running up to the tick of the hit.
        [Test]
        public void StickHitOnCore_CancelsHiveCallAndSetsTheLastHitPoint()
        {
            StickBesideCore();
            JsonUtility.FromJsonOverwrite("{\"<CollectPerSecond>k__BackingField\":1}", _world.Hive);
            _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 1, 10f);
            int hits = 0;
            _world.Session.ActiveBeekeeper(0).CoreHit += brain => hits++;
            LeaveRoom();
            Assert.That(_world.Session.Forms.StartHiveCall(), Is.True);
            bool callingBeforeTheHit = false;

            bool hit = _world.RunUntil(
                () =>
                {
                    if (hits == 0)
                    {
                        callingBeforeTheHit = _world.Session.Forms.Current == SwarmForm.HiveCall;
                    }

                    return hits > 0;
                },
                10f);

            Assert.That(hit, Is.True);
            Assert.That(callingBeforeTheHit, Is.True, "Hive Call still ran when the stick struck");
            Assert.That(_world.Session.Forms.Current, Is.Not.EqualTo(SwarmForm.HiveCall));
            Assert.That(_world.Session.LastHitPoint.x, Is.EqualTo(Origin.x).Within(0.5f));
            Assert.That(_world.Session.LastHitPoint.z, Is.EqualTo(Origin.z).Within(0.5f));
        }

        [Test]
        public void StickHitOnHive_DamagesThatHive()
        {
            _world = new HiveDefenseWorld(
                OneStick(),
                hives: new[] { new Vector3(0f, 0f, 0f), new Vector3(60f, 0f, 0f) },
                spawns: new[] { new Vector3(1f, 0f, 0f) },
                core: new Vector3(0f, 0f, 80f));

            bool hit = _world.RunUntil(() => _world.Session.Hives[0].Health < _world.Hive.Health, 15f);

            Assert.That(hit, Is.True);
            Assert.That(_world.Session.Hives[0].Health, Is.EqualTo(_world.Hive.Health - _world.Stick.HiveDamage));
            Assert.That(_world.Session.Hives[1].Health, Is.EqualTo(_world.Hive.Health * _world.Hive.DamagedStartFraction), "untouched: the last hive starts hurt");
            Assert.That(_world.Session.Swarm.Count, Is.EqualTo(_world.Swarm.StartingWasps));
        }

        // A song that shields a hive needs only its rule: the session asks the running songs how much gets through and
        // tells them about the hit.
        [Test]
        public void StickHitOnHive_WithASongShieldingIt_TakesOnlyTheShareLetThroughAndTellsTheSong()
        {
            _world = new HiveDefenseWorld(
                OneStick(),
                hives: new[] { new Vector3(0f, 0f, 0f), new Vector3(60f, 0f, 0f) },
                spawns: new[] { new Vector3(1f, 0f, 0f) },
                core: new Vector3(0f, 0f, 80f));
            var shell = ScriptableObject.CreateInstance<FakeSlotSong>();
            try
            {
                shell.ShieldedHive = 0;
                shell.HiveDamageFraction = 0.25f;
                SwarmSongContext context = SongContext();
                shell.Cast(in context);

                bool hit = _world.RunUntil(() => _world.Session.Hives[0].Health < _world.Hive.Health, 15f);

                Assert.That(hit, Is.True);
                Assert.That(_world.Session.Hives[0].Health, Is.EqualTo(_world.Hive.Health - (_world.Stick.HiveDamage * 0.25f)).Within(1e-3f));
                Assert.That(shell.Rule.HiveHitCount, Is.EqualTo(1));
                Assert.That(shell.Rule.LastHiveHit, Is.EqualTo(0));
                Assert.That(shell.Rule.LastHiveAttacker, Is.SameAs(_world.Session.ActiveBeekeeper(0)));
            }
            finally
            {
                Object.DestroyImmediate(shell);
            }
        }

        [Test]
        public void CoreSpeedMultiplier_InSmoke_IsSlowerUnlessARunningSongKeepsTheSmokeOff()
        {
            _world = new HiveDefenseWorld();
            var wings = ScriptableObject.CreateInstance<FakeSlotSong>();
            try
            {
                _world.Session.Smoke.Throw(Origin, Origin);
                _world.Run(1.5f);
                Assert.That(_world.Session.Smoke.IsCoreInSmoke, Is.True);
                Assert.That(_world.Session.CoreSpeedMultiplier, Is.EqualTo(_world.CoreMove.SmokeSpeedMultiplier).Within(1e-5f));

                wings.SlotToUse = SongSlot.Guard;
                wings.CoreSpeedMultiplier = 1.6f;
                wings.SmokeSlowsCore = false;
                SwarmSongContext context = SongContext();
                wings.Cast(in context);

                Assert.That(_world.Session.CoreSpeedMultiplier, Is.EqualTo(1.6f).Within(1e-5f));
            }
            finally
            {
                Object.DestroyImmediate(wings);
            }
        }

        [Test]
        public void SmokerGrenade_IsThrownIntoTheSmokeField()
        {
            _world = new HiveDefenseWorld(new[] { new WaveEntry(0, 1) }, spawns: new[] { new Vector3(6f, 0f, 0f) });
            int thrown = 0;
            _world.Session.Smoke.GrenadeThrown += (from, to, seconds) => thrown++;

            _world.RunUntil(() => thrown > 0, 10f);

            Assert.That(thrown, Is.EqualTo(1));
        }

        [Test]
        public void Tick_PressedHole_ReachesTheCaster()
        {
            _world = new HiveDefenseWorld();
            var notes = new List<int>();
            _world.Session.Caster.NotePlayed += notes.Add;

            _world.Session.Tick(_world.Frame(holes: HiveDefenseFrame.HoleBit(2)), HiveDefenseWorld.Step);
            _world.Session.Tick(_world.Frame(), HiveDefenseWorld.Step);

            Assert.That(notes, Is.EqualTo(new[] { 2 }));
        }

        // Plan section E: the mouse wins while it is live, and picks the beekeeper nearest its ground point within 3 m.
        [Test]
        public void AimTarget_WhileTheMouseIsLive_IsTheLivingBeekeeperNearTheMousePoint()
        {
            _world = new HiveDefenseWorld(new[] { new WaveEntry(2, 0) }, spawns: new[] { new Vector3(30f, 0f, -30f), new Vector3(-30f, 0f, -30f) });
            _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 2, 10f);
            BeekeeperBrain east = _world.Session.ActiveBeekeeper(0);
            BeekeeperBrain west = _world.Session.ActiveBeekeeper(1);

            _world.Session.Tick(_world.Frame(aim: west.Position, isMouseLive: true), HiveDefenseWorld.Step);
            Assert.That(_world.Session.AimTarget, Is.SameAs(west));

            _world.Session.Tick(_world.Frame(aim: east.Position, isMouseLive: true), HiveDefenseWorld.Step);
            Assert.That(_world.Session.AimTarget, Is.SameAs(east));

            Vector3 eastPoint = east.Position;
            east.TakeDamage(10000f);
            _world.Session.Tick(_world.Frame(aim: eastPoint, isMouseLive: true), HiveDefenseWorld.Step);
            Assert.That(_world.Session.AimTarget, Is.Null, "a dead beekeeper cannot be aimed at, and the other is far from the mouse");
            Assert.That(_world.Session.AimPoint, Is.EqualTo(eastPoint), "with nobody near the mouse, the song strikes where it points");
        }

        [Test]
        public void AimTarget_WithTheMouseIdle_IsTheNearestBeekeeperWithinTheStingsReach()
        {
            _world = new HiveDefenseWorld(new[] { new WaveEntry(2, 0) }, spawns: new[] { new Vector3(5f, 0f, -5f), new Vector3(-30f, 0f, -30f) });
            _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 2, 10f);
            BeekeeperBrain near = _world.Session.ActiveBeekeeper(0);

            _world.Session.Tick(_world.Frame(aim: new Vector3(-30f, 0f, -30f)), HiveDefenseWorld.Step);

            Assert.That(_world.Session.AimTarget, Is.SameAs(near), "an idle mouse point is ignored");

            // Chosen before the beekeepers stepped in the same tick, so it trails the walking target by one step.
            Assert.That(Vector3.Distance(_world.Session.AimPoint, near.Position), Is.LessThan(0.1f));
        }

        private const float ColossusSeconds = 12f;

        [Test]
        public void Colossus_FollowsTheFormAndSteersWithTheInput()
        {
            _world = new HiveDefenseWorld(core: new Vector3(5f, 0f, 5f));
            _world.Session.Fury.AddDamage(_world.Fury.Max * _world.Fury.DamagePerFuryPoint);
            Assert.That(_world.Session.Fury.TryUse(), Is.True);
            Assert.That(_world.Session.Forms.StartColossus(ColossusSeconds), Is.True);

            _world.Session.Tick(_world.Frame(), HiveDefenseWorld.Step);
            Assert.That(_world.Session.IsColossusActive, Is.True);
            Assert.That(_world.Session.ColossusPosition.x, Is.EqualTo(5f).Within(0.01f));
            Assert.That(_world.Session.ColossusPosition.z, Is.EqualTo(5f).Within(0.01f));

            _world.RunWith(1f, _world.Frame(move: new Vector2(1f, 0f)));

            Assert.That(_world.Session.ColossusPosition.x, Is.EqualTo(5f + _world.Colossus.Speed).Within(0.2f));
            Assert.That(_world.Session.ColossusFacing.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(_world.Session.ColossusFacing.z, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void Colossus_AfterItsDuration_IsNoLongerActive()
        {
            _world = new HiveDefenseWorld();
            _world.Session.Fury.AddDamage(_world.Fury.Max * _world.Fury.DamagePerFuryPoint);
            _world.Session.Fury.TryUse();
            _world.Session.Forms.StartColossus(ColossusSeconds);
            _world.Run(HiveDefenseWorld.Step);
            Assert.That(_world.Session.IsColossusActive, Is.True);

            _world.Run(ColossusSeconds + 1f);

            Assert.That(_world.Session.IsColossusActive, Is.False);
            Assert.That(_world.Session.IsCoreRooted, Is.False);
        }

        [Test]
        public void Colossus_Punch_HurtsAndStunsABeekeeperInReachAndFillsFury()
        {
            // The giant faces +z by default, and the stick spawns 2.5 m ahead of it.
            _world = new HiveDefenseWorld(OneStick(), spawns: new[] { new Vector3(0f, 0f, 2.5f) });
            _world.Session.Fury.AddDamage(_world.Fury.Max * _world.Fury.DamagePerFuryPoint);
            _world.Session.Fury.TryUse();
            _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 1, 10f);
            _world.Session.Forms.StartColossus(ColossusSeconds);
            _world.Run(HiveDefenseWorld.Step);
            BeekeeperBrain keeper = _world.Session.ActiveBeekeeper(0);
            var landed = new List<ColossusAttack>();
            _world.Session.ColossusAttackLanded += landed.Add;

            _world.Session.Tick(_world.Frame(punch: true), HiveDefenseWorld.Step);
            _world.Run(_world.Colossus.PunchHitDelay + 0.1f);

            Assert.That(landed, Is.EqualTo(new[] { ColossusAttack.Punch }));
            Assert.That(keeper.Health, Is.EqualTo(_world.Stick.Health - _world.Colossus.PunchDamage).Within(0.01f));
            Assert.That(keeper.State, Is.EqualTo(BeekeeperState.Stunned));
            Assert.That(_world.Session.Fury.Value, Is.GreaterThan(0f));
        }

        // Plan section K: the Colossus is the whole swarm. The core rides inside it, so a place the View gives the core
        // counts for nothing while the giant stands, and nothing is left rooted behind.
        [Test]
        public void Colossus_WhileItStands_TheCoreIsWhereTheGiantIsAndIsNotRooted()
        {
            _world = new HiveDefenseWorld(core: new Vector3(5f, 0f, 5f));
            HiveDefenseSession session = _world.Session;
            Assert.That(session.Forms.StartColossus(ColossusSeconds), Is.True);

            _world.RunWith(1f, _world.Frame(move: new Vector2(1f, 0f)));

            Assert.That(session.IsColossusActive, Is.True);
            Assert.That(session.IsCoreRooted, Is.False, "nothing is left standing behind");
            Assert.That(session.ColossusPosition.x, Is.EqualTo(5f + _world.Colossus.Speed).Within(0.2f));
            Assert.That(session.CorePosition.x, Is.EqualTo(session.ColossusPosition.x).Within(1e-4f));
            Assert.That(session.CorePosition.z, Is.EqualTo(session.ColossusPosition.z).Within(1e-4f));
            Assert.That(session.CoreFacing.x, Is.EqualTo(1f).Within(1e-4f), "the core moved with the giant");

            Vector3 giant = session.ColossusPosition;
            var elsewhere = new HiveDefenseFrame(new Vector3(-20f, 0f, -20f), Vector3.zero, Vector2.zero, 0, false, false);
            session.Tick(in elsewhere, HiveDefenseWorld.Step);

            Assert.That(session.CorePosition.x, Is.EqualTo(giant.x).Within(1e-4f), "the View's place for the core is ignored");
            Assert.That(session.CorePosition.z, Is.EqualTo(giant.z).Within(1e-4f));
        }

        [Test]
        public void Colossus_WhenItEnds_TheCoreStaysWhereTheGiantStood()
        {
            _world = new HiveDefenseWorld();
            HiveDefenseSession session = _world.Session;
            Assert.That(session.Forms.StartColossus(1f), Is.True);
            _world.RunWith(0.5f, _world.Frame(move: new Vector2(0f, 1f)));
            Vector3 stood = session.ColossusPosition;
            Assert.That(stood.z, Is.GreaterThan(1f), "the giant walked");

            bool ended = _world.RunUntil(() => !session.IsColossusActive, 2f);

            Assert.That(ended, Is.True);
            Assert.That(session.CorePosition.x, Is.EqualTo(stood.x).Within(1e-4f));
            Assert.That(session.CorePosition.z, Is.EqualTo(stood.z).Within(1e-4f));

            _world.Run(0.5f);

            Assert.That(session.CorePosition.z, Is.EqualTo(stood.z).Within(1e-4f), "once the View has put it there, it stays");
        }

        // Plan section K: beekeepers chase and hit the core, which rides in the giant, so a hit on the giant costs wasps as a
        // hit on the core does. The stick spawns 12 m from where the core stood, too far to notice it there.
        [Test]
        public void StickHitOnCore_WhileTheColossusStands_HitsTheGiantAndCostsWasps()
        {
            _world = new HiveDefenseWorld(OneStick(), spawns: new[] { new Vector3(12f, 0f, 0f) });
            HiveDefenseSession session = _world.Session;
            Assert.That(session.Forms.StartColossus(ColossusSeconds), Is.True);
            _world.RunWith(2.8f, _world.Frame(move: new Vector2(1f, 0f)));
            Assert.That(session.ActiveBeekeeperCount, Is.EqualTo(1));

            bool hit = _world.RunUntil(() => session.Swarm.Count < _world.Swarm.StartingWasps, 8f);

            Assert.That(hit, Is.True);
            Assert.That(session.IsColossusActive, Is.True, "the hit came while the giant stood");
            Assert.That(session.Swarm.Count, Is.EqualTo(_world.Swarm.StartingWasps - _world.Stick.WaspsKnockedOff));
            Assert.That(session.LastHitPoint.x, Is.EqualTo(session.ColossusPosition.x).Within(0.01f), "the hit lands on the giant");
            Assert.That(session.LastHitPoint.x, Is.GreaterThan(5f), "not where the core stood before the giant");
        }

        // The ground ends, so the giant keeps to the play area: a giant left past it would drop the core off the ground.
        [Test]
        public void Colossus_WalkingPastThePlayArea_StopsAtItsEdge()
        {
            _world = new HiveDefenseWorld();
            float edge = _world.CoreMove.PlayAreaHalfSize;
            _world.MoveCoreTo(new Vector3(edge - 2f, 0f, 1f));
            HiveDefenseSession session = _world.Session;
            Assert.That(session.Forms.StartColossus(ColossusSeconds), Is.True);

            _world.RunWith(3f, _world.Frame(move: new Vector2(1f, -1f)));

            Assert.That(session.IsColossusActive, Is.True);
            Assert.That(session.ColossusPosition.x, Is.EqualTo(edge).Within(1e-4f), "stopped at the edge");
            Assert.That(session.ColossusPosition.z, Is.LessThan(-3f), "still free to walk along it");
            Assert.That(session.CorePosition.x, Is.EqualTo(edge).Within(1e-4f), "the core rides inside the giant");

            _world.RunWith(0.5f, _world.Frame(move: new Vector2(-1f, 0f)));

            Assert.That(session.ColossusPosition.x, Is.LessThan(edge - 1f), "free to walk back in");
        }

        // The core keeps to the play area as well: a core the View moved past it is held at its edge, and the View puts
        // it there, so it never walks off the ground. A side of 1 or -1 is 10 m past that edge, and 0 is 5 m in.
        [TestCase(1, 0)]
        [TestCase(-1, -1)]
        [TestCase(0, 1)]
        public void Core_MovedPastThePlayArea_IsHeldAtItsEdge(int sideX, int sideZ)
        {
            _world = new HiveDefenseWorld();
            HiveDefenseSession session = _world.Session;
            float edge = _world.CoreMove.PlayAreaHalfSize;

            _world.MoveCoreTo(new Vector3(Past(sideX, edge), 0.5f, Past(sideZ, edge)));

            Assert.That(session.CorePosition.x, Is.EqualTo(HeldAt(sideX, edge)).Within(1e-4f));
            Assert.That(session.CorePosition.z, Is.EqualTo(HeldAt(sideZ, edge)).Within(1e-4f));
            Assert.That(session.CorePosition.y, Is.EqualTo(0.5f).Within(1e-4f), "only the place on the ground is held");
        }

        private const float InsideMetres = 5f;
        private const float PastMetres = 10f;

        private static float Past(int side, float edge) => side == 0 ? InsideMetres : side * (edge + PastMetres);

        private static float HeldAt(int side, float edge) => side == 0 ? InsideMetres : side * edge;

        // When the giant ends inside a hive or a wall, the core is put there, and the controller pushes it out on the next
        // frame. That push is the core being put somewhere, not the player moving it, so it keeps the giant's facing,
        // which aimed songs strike ahead of. The core turns again on the next real step.
        [Test]
        public void Colossus_ThePushOutWhereTheGiantEnded_DoesNotTurnTheCore()
        {
            _world = new HiveDefenseWorld();
            HiveDefenseSession session = _world.Session;
            Assert.That(session.Forms.StartColossus(1f), Is.True);
            _world.RunWith(0.5f, _world.Frame(move: new Vector2(0f, 1f)));
            Assert.That(_world.RunUntil(() => !session.IsColossusActive, 2f), Is.True);
            Vector3 stood = session.CorePosition;

            _world.MoveCoreTo(stood + new Vector3(1.2f, 0f, 0f));

            Assert.That(session.CorePosition.x, Is.EqualTo(stood.x + 1.2f).Within(1e-4f), "the core is where it was pushed");
            Assert.That(session.CoreFacing.z, Is.EqualTo(1f).Within(1e-4f), "still the way the giant walked");

            _world.MoveCoreTo(session.CorePosition + new Vector3(1f, 0f, 0f));

            Assert.That(session.CoreFacing.x, Is.EqualTo(1f).Within(1e-4f), "a real step turns it");
        }

        // Plan section K: a collapse ends the giant, from a hit or the smoke later in the same tick. The core stays where
        // the giant stood, and from the next tick the View's core counts again.
        [Test]
        public void Colossus_WhenTheSwarmCollapses_TheCoreStaysWhereTheGiantStoodAndMovesAgain()
        {
            _world = new HiveDefenseWorld();
            HiveDefenseSession session = _world.Session;
            Assert.That(session.Forms.StartColossus(ColossusSeconds), Is.True);
            _world.RunWith(0.5f, _world.Frame(move: new Vector2(1f, 0f)));
            Vector3 stood = session.ColossusPosition;
            Assert.That(stood.x, Is.GreaterThan(1f), "the giant walked");

            session.Swarm.Lose(session.Swarm.Count - (session.Swarm.CollapseLine / 2));
            Assert.That(session.Forms.Current, Is.EqualTo(SwarmForm.Collapsed));
            _world.Run(HiveDefenseWorld.Step);
            _world.Run(HiveDefenseWorld.Step);

            Assert.That(session.IsColossusActive, Is.False);
            Assert.That(session.CorePosition.x, Is.EqualTo(stood.x).Within(1e-4f), "where the giant stood");
            Assert.That(session.CorePosition.z, Is.EqualTo(stood.z).Within(1e-4f));

            _world.MoveCoreTo(stood + new Vector3(0f, 0f, 3f));

            Assert.That(session.CorePosition.z, Is.EqualTo(stood.z + 3f).Within(1e-4f), "the View's core counts again");
            Assert.That(session.CoreFacing.z, Is.EqualTo(1f).Within(1e-4f));
        }

        // ---- Song discovery ----

        private void PlayDome()
        {
            _world.Play(DomeHoles);
        }

        [Test]
        public void Discovery_NewSession_KnowsOnlyTheStingAndRevealsNothingElse()
        {
            _world = new HiveDefenseWorld(withSongs: true);
            SongDiscovery discovery = _world.Session.Discovery;

            Assert.That(discovery.Count, Is.EqualTo(3));
            Assert.That(discovery.KnownCount, Is.EqualTo(1));
            Assert.That(discovery.IsKnown(HiveDefenseWorld.StingIndex), Is.True);
            Assert.That(discovery.IsRevealed(HiveDefenseWorld.DomeIndex), Is.False);
            Assert.That(discovery.IsRevealed(HiveDefenseWorld.ColossusIndex), Is.False);
        }

        [Test]
        public void Discovery_ASuccessfulCast_MakesTheSongKnownOnce()
        {
            _world = new HiveDefenseWorld(withSongs: true);
            var discovered = new List<int>();
            _world.Session.Discovery.Discovered += discovered.Add;

            PlayDome();

            Assert.That(_world.Session.Forms.IsDomed, Is.True);
            Assert.That(_world.Session.Discovery.IsKnown(HiveDefenseWorld.DomeIndex), Is.True);
            Assert.That(discovered, Is.EqualTo(new[] { HiveDefenseWorld.DomeIndex }));
        }

        [Test]
        public void Discovery_NotesPlayedWhileTheSongIsNotReady_DoNotMakeItKnown()
        {
            _world = new HiveDefenseWorld(withSongs: true);
            var notReady = new List<int>();
            _world.Session.Caster.SongNotReady += (refused, reason) => notReady.Add(refused);
            _world.Play(new[] { 3, 1, 2, 1, 3 });

            Assert.That(notReady, Is.EqualTo(new[] { HiveDefenseWorld.ColossusIndex }), "Fury is empty");
            Assert.That(_world.Session.Discovery.IsKnown(HiveDefenseWorld.ColossusIndex), Is.False);
        }

        [Test]
        public void Discovery_FuryFillsTheFirstTime_RevealsTheColossusButDoesNotMakeItKnown()
        {
            _world = new HiveDefenseWorld(withSongs: true);
            var revealed = new List<int>();
            _world.Session.Discovery.Revealed += revealed.Add;
            _world.Session.Fury.AddDamage(_world.Fury.Max * _world.Fury.DamagePerFuryPoint * 0.5f);
            Assert.That(revealed, Is.Empty, "half full reveals nothing");

            _world.Session.Fury.AddDamage(_world.Fury.Max * _world.Fury.DamagePerFuryPoint);

            Assert.That(_world.Session.Fury.IsFull, Is.True);
            Assert.That(revealed, Is.EqualTo(new[] { HiveDefenseWorld.ColossusIndex }));
            Assert.That(_world.Session.Discovery.IsRevealed(HiveDefenseWorld.ColossusIndex), Is.True);
            Assert.That(_world.Session.Discovery.IsKnown(HiveDefenseWorld.ColossusIndex), Is.False);
            Assert.That(_world.Session.Discovery.IsRevealed(HiveDefenseWorld.DomeIndex), Is.False, "other songs stay hidden");
        }

        [Test]
        public void Discovery_FuryFillsAgainAfterBeingUsed_RevealsNothingNew()
        {
            _world = new HiveDefenseWorld(withSongs: true);
            var revealed = new List<int>();
            _world.Session.Discovery.Revealed += revealed.Add;
            float full = _world.Fury.Max * _world.Fury.DamagePerFuryPoint;
            _world.Session.Fury.AddDamage(full);
            Assert.That(_world.Session.Fury.TryUse(), Is.True);

            _world.Session.Fury.AddDamage(full);

            Assert.That(revealed.Count, Is.EqualTo(1));
        }

        // ---- The giant takes no notes ----

        [Test]
        public void Tick_WhileTheColossusStands_NoNoteReachesTheCaster()
        {
            _world = new HiveDefenseWorld(withSongs: true);
            var notes = new List<int>();
            var notReady = new List<int>();
            _world.Session.Caster.NotePlayed += notes.Add;
            _world.Session.Caster.SongNotReady += (refused, reason) => notReady.Add(refused);
            Assert.That(_world.Session.Forms.StartColossus(ColossusSeconds), Is.True);

            _world.Play(new[] { 0, 2, 1, 3, 1, 2, 1, 3 });

            Assert.That(notes, Is.Empty);
            Assert.That(notReady, Is.Empty);
            Assert.That(_world.Session.Caster.RecentNoteCount, Is.EqualTo(0));
            Assert.That(_world.Session.Forms.IsDomed, Is.False);
        }

        [Test]
        public void Tick_NotesPlayedWhileTheColossusStood_DoNotFinishASongAfterIt()
        {
            _world = new HiveDefenseWorld(withSongs: true);
            var cast = new List<int>();
            _world.Session.Caster.SongCast += cast.Add;
            _world.Session.Forms.StartColossus(0.5f);
            _world.Play(new[] { 0, 2 });
            _world.Run(0.6f);
            Assert.That(_world.Session.Forms.Current, Is.EqualTo(SwarmForm.River), "the giant has gone");

            _world.Play(new[] { 1 });

            Assert.That(cast, Is.Empty, "H K were played to the giant, so J alone casts no Dome");
            Assert.That(_world.Session.Forms.IsDomed, Is.False);
        }

        // ---- Songs cast even with nothing to hit ----

        // Plan section K: with no Piper to face anywhere, "ahead" is the way the core last moved. It keeps that way while
        // the core stands still to play.
        [TestCase(HiveDefenseWorld.MeteorIndex)]
        [TestCase(HiveDefenseWorld.StingIndex)]
        public void Cast_AimedSongWithNothingToHit_StrikesSixMetresAheadOfWhereTheCoreLastMoved(int song)
        {
            _world = new HiveDefenseWorld(core: new Vector3(4f, 0f, 5f), withAllSongs: true);
            _world.MoveCoreTo(new Vector3(5f, 0f, 5f));
            Assert.That(_world.Session.CoreFacing, Is.EqualTo(Vector3.right), "the core moved along +x");
            var cast = new List<int>();
            _world.Session.Caster.SongCast += cast.Add;

            _world.Play(_world.Session.Caster[song].Song.Keys);

            Assert.That(_world.Session.ActiveBeekeeperCount, Is.EqualTo(0));
            Assert.That(cast, Is.EqualTo(new[] { song }));
            Vector4 strike = _world.Session.Forms.Slot(SongSlot.Strike).ParamA;
            Assert.That(strike.x, Is.EqualTo(11f).Within(1e-3f));
            Assert.That(strike.y, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(strike.z, Is.EqualTo(5f).Within(1e-3f));
        }

        [Test]
        public void Cast_AfterAMoveWithHeightAndAnyLength_StillStrikesSixMetresAheadOnTheGround()
        {
            _world = new HiveDefenseWorld(core: new Vector3(0f, 0f, 0.5f), withAllSongs: true);
            _world.MoveCoreTo(new Vector3(0f, 3f, 0f));

            _world.Play(_world.Session.Caster[HiveDefenseWorld.MeteorIndex].Song.Keys);

            Vector4 strike = _world.Session.Forms.Slot(SongSlot.Strike).ParamA;
            Assert.That(strike.x, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(strike.z, Is.EqualTo(-6f).Within(1e-3f));
        }

        // Plan section E: the Meteor strikes the beekeeper with the most others within 3.5 m. The marker picks by Threat every
        // tick, and its pick a moment ago must not decide where the Meteor lands.
        [Test]
        public void Cast_MeteorJustAfterTheMarkerPickedALoner_StillStrikesTheCrowd()
        {
            _world = new HiveDefenseWorld(
                new[] { new WaveEntry(5, 0) },
                spawns: new[]
                {
                    new Vector3(0f, 0f, 3f), new Vector3(0f, 0f, 5f),
                    new Vector3(0f, 0f, -14f), new Vector3(1f, 0f, -14f), new Vector3(-1f, 0f, -14f),
                },
                withAllSongs: true);
            List<BeekeeperBrain> keepers = StandStillAsTheySpawn();
            _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 5, 10f);
            _world.Run(HiveDefenseWorld.Step);
            Assert.That(_world.Session.AimTarget, Is.SameAs(keepers[0]), "the nearest, 3 m away");

            keepers[0].TakeDamage(10000f);
            _world.Run(0.3f);
            Assert.That(_world.Session.AimTarget, Is.SameAs(keepers[1]), "the loner became the marker's pick 0.3 s ago");

            _world.Play(_world.Session.Caster[HiveDefenseWorld.MeteorIndex].Song.Keys);

            Vector4 strike = _world.Session.Forms.Slot(SongSlot.Strike).ParamA;
            Assert.That(_world.Session.Forms.Slot(SongSlot.Strike).Form, Is.EqualTo(SwarmForm.Meteor));
            Assert.That(strike.x, Is.EqualTo(0f).Within(0.1f), "the crowd 14 m behind, not the loner");
            Assert.That(strike.z, Is.EqualTo(-14f).Within(0.1f));
        }

        // The marker and the Sting share one reach, the Sting asset's, so tuning the song moves both.
        [Test]
        public void AimTarget_WithTheMouseIdle_ReachesAsFarAsTheStingAssetSays()
        {
            _world = new HiveDefenseWorld(OneStick(), spawns: new[] { new Vector3(0f, 0f, 14f) });
            StandStillAsTheySpawn();
            _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 1, 10f);
            _world.Run(HiveDefenseWorld.Step);
            Assert.That(_world.Session.AimTarget, Is.Null, "14 m is beyond the Sting's 12 m");

            JsonUtility.FromJsonOverwrite("{\"<MaxRange>k__BackingField\":15}", _world.Sting);
            _world.Run(HiveDefenseWorld.Step);

            Assert.That(_world.Session.AimTarget, Is.SameAs(_world.Session.ActiveBeekeeper(0)));
        }

        [Test]
        public void New_SongsWithoutAStartingThreatSong_Throws()
        {
            _world = new HiveDefenseWorld();
            var dome = ScriptableObject.CreateInstance<DomeSong>();
            try
            {
                var rules = new HiveDefenseRules(
                    _world.Swarm, _world.Squads, _world.CoreMove, _world.Fury, _world.Colossus, _world.Hive, _world.Nests,
                    _world.SwarmLevel, _world.Smoke, _world.WaveConfig, _world.Stick, _world.Smoker, _world.Aim, _world.SongConfig,
                    new SwarmSongEffect[] { dome }, HiveDefenseWorld.HoleCount);

                Assert.That(
                    () => new HiveDefenseSession(
                        rules, new[] { Vector3.zero }, HiveDefenseWorld.DefaultNests, new[] { Vector3.one }, Vector3.zero, new FixedRandomSource()),
                    Throws.ArgumentException);
            }
            finally
            {
                Object.DestroyImmediate(dome);
            }
        }

        [Test]
        public void Cast_MeteorWithABeekeeperOnTheField_StrikesTheBeekeeper()
        {
            _world = new HiveDefenseWorld(OneStick(), spawns: new[] { new Vector3(8f, 0f, 3f) }, withAllSongs: true);
            _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 1, 10f);
            BeekeeperBrain keeper = _world.Session.ActiveBeekeeper(0);

            _world.Play(_world.Session.Caster[HiveDefenseWorld.MeteorIndex].Song.Keys);

            Vector4 strike = _world.Session.Forms.Slot(SongSlot.Strike).ParamA;
            Assert.That(strike.x, Is.EqualTo(keeper.Position.x).Within(0.1f));
            Assert.That(strike.z, Is.EqualTo(keeper.Position.z).Within(0.1f));
        }

        [TestCase(HiveDefenseWorld.DomeIndex)]
        [TestCase(HiveDefenseWorld.StingIndex)]
        [TestCase(HiveDefenseWorld.TornadoIndex)]
        [TestCase(HiveDefenseWorld.MeteorIndex)]
        [TestCase(HiveDefenseWorld.GustIndex)]
        [TestCase(HiveDefenseWorld.HiveCallIndex)]
        public void Cast_EverySongButTheColossus_CastsWithNobodyOnTheField(int song)
        {
            _world = new HiveDefenseWorld(withAllSongs: true);
            var cast = new List<int>();
            var notReady = new List<int>();
            _world.Session.Caster.SongCast += cast.Add;
            _world.Session.Caster.SongNotReady += (refused, reason) => notReady.Add(refused);

            _world.Play(_world.Session.Caster[song].Song.Keys);

            Assert.That(_world.Session.ActiveBeekeeperCount, Is.EqualTo(0));
            Assert.That(cast, Is.EqualTo(new[] { song }));
            Assert.That(notReady, Is.Empty);
        }

        // ---- Farm and survive (plan section L) ----

        [Test]
        public void New_Session_StartsInTheFirstFarmPhaseWithFreeSongs()
        {
            _world = new HiveDefenseWorld(new[] { new WaveEntry(1, 0), new WaveEntry(2, 0) }, firstFarmSeconds: 45f);
            HiveDefenseSession session = _world.Session;

            Assert.That(session.Phase, Is.EqualTo(MatchPhase.Farm));
            Assert.That(session.Round, Is.EqualTo(1));
            Assert.That(session.RoundCount, Is.EqualTo(2));
            Assert.That(session.FarmSecondsLeft, Is.EqualTo(45f).Within(0.001f));
            Assert.That(session.AreSongsFree, Is.True);
            Assert.That(session.NearHive, Is.EqualTo(-1));
            Assert.That(session.IsCollecting, Is.False);
            Assert.That(session.IsGiving, Is.False);
            Assert.That(session.Tutorial.Current, Is.EqualTo(TutorialStep.Move));
        }

        [Test]
        public void Tick_InAFarmPhase_NoBeekeeperComes()
        {
            _world = new HiveDefenseWorld(firstFarmSeconds: 20f);

            _world.Run(10f);

            Assert.That(_world.Session.ActiveBeekeeperCount, Is.EqualTo(0));
            Assert.That(_world.Session.Phase, Is.EqualTo(MatchPhase.Farm));
        }

        [Test]
        public void Tick_FarmTimerEnds_TheWaveComesAndSongsCost()
        {
            _world = new HiveDefenseWorld();

            _world.Run(HiveDefenseWorld.ShortFirstFarmSeconds + 0.1f);

            Assert.That(_world.Session.Phase, Is.EqualTo(MatchPhase.Survive));
            Assert.That(_world.Session.AreSongsFree, Is.False);
            Assert.That(_world.Session.ActiveBeekeeperCount, Is.EqualTo(1));
        }

        [Test]
        public void CallWave_InAFarmPhase_StartsTheWaveAndGrantsWaspsForEachSecondSkipped()
        {
            _world = new HiveDefenseWorld(firstFarmSeconds: 20f);
            HiveDefenseSession session = _world.Session;
            int before = LeaveRoom();
            float skipped = -1f;
            var granted = new List<int>();
            session.Waves.WaveCalledEarly += seconds => skipped = seconds;
            session.WaspsGranted += granted.Add;

            _world.RunWith(HiveDefenseWorld.Step, _world.Frame(callWave: true));

            int expected = Mathf.FloorToInt(skipped * _world.WaveConfig.EarlyBonusPerSecond);
            Assert.That(session.Phase, Is.EqualTo(MatchPhase.Survive));
            Assert.That(session.ActiveBeekeeperCount, Is.EqualTo(1), "the wave comes at once");
            Assert.That(skipped, Is.EqualTo(20f - HiveDefenseWorld.Step).Within(0.001f), "the farm phase's own tick ran first");
            Assert.That(granted, Is.EqualTo(new[] { expected }));
            Assert.That(expected, Is.EqualTo(99), "5 wasps for each of the 19.98 s skipped");
            Assert.That(session.Swarm.Count, Is.EqualTo(before + expected));
        }

        [Test]
        public void CallWave_GrantsNoMoreThanTheSwarmHasRoomFor()
        {
            _world = new HiveDefenseWorld(firstFarmSeconds: 20f);
            HiveDefenseSession session = _world.Session;
            session.Swarm.Lose(30);
            var granted = new List<int>();
            session.WaspsGranted += granted.Add;

            _world.RunWith(HiveDefenseWorld.Step, _world.Frame(callWave: true));

            Assert.That(granted, Is.EqualTo(new[] { 30 }));
            Assert.That(session.Swarm.Count, Is.EqualTo(session.Swarm.Capacity), "up to the capacity, not the ceiling");
        }

        [Test]
        public void CallWave_InASurvivePhase_DoesNothing()
        {
            _world = new HiveDefenseWorld();
            HiveDefenseSession session = _world.Session;
            _world.Run(HiveDefenseWorld.ShortFirstFarmSeconds + 0.1f);
            int wasps = session.Swarm.Count;
            var granted = new List<int>();
            session.WaspsGranted += granted.Add;

            _world.RunWith(HiveDefenseWorld.Step, _world.Frame(callWave: true));

            Assert.That(granted, Is.Empty);
            Assert.That(session.Swarm.Count, Is.EqualTo(wasps));
            Assert.That(session.Phase, Is.EqualTo(MatchPhase.Survive));
            Assert.That(session.ActiveBeekeeperCount, Is.EqualTo(1));
        }

        // ---- Costs (plan section M) ----

        [Test]
        public void Cast_InAFarmPhase_IsFree_AndInASurvivePhase_CostsItsWasps()
        {
            _world = new HiveDefenseWorld(withSongs: true);
            HiveDefenseSession session = _world.Session;

            _world.Play(_world.Sting.Song.Keys);
            Assert.That(session.Swarm.Count, Is.EqualTo(_world.Swarm.StartingWasps), "songs cost nothing in a farm phase");

            _world.Run(1.6f);
            Assert.That(session.Phase, Is.EqualTo(MatchPhase.Survive));
            _world.Play(_world.Sting.Song.Keys);

            Assert.That(session.Swarm.Count, Is.EqualTo(_world.Swarm.StartingWasps - _world.Sting.WaspCost));
        }

        [Test]
        public void Cast_RefusedForWantOfWasps_FillsTheTableButDoesNotMakeTheSongKnown()
        {
            _world = new HiveDefenseWorld(withSongs: true);
            HiveDefenseSession session = _world.Session;
            _world.Run(HiveDefenseWorld.ShortFirstFarmSeconds + 0.1f);
            session.Swarm.Lose(session.Swarm.Count - session.Swarm.CollapseLine - 5);
            var reasons = new List<SongRefusal>();
            session.Caster.SongNotReady += (refused, reason) => reasons.Add(reason);

            _world.Play(DomeHoles);

            Assert.That(reasons, Is.EqualTo(new[] { SongRefusal.NotEnoughWasps }));
            Assert.That(session.Discovery.IsKnown(HiveDefenseWorld.DomeIndex), Is.False);
            Assert.That(session.Discovery.KnownNotes(HiveDefenseWorld.DomeIndex), Is.EqualTo(3), "the notes still fill in the table");
            Assert.That(session.Swarm.Count, Is.EqualTo(session.Swarm.CollapseLine + 5));
            Assert.That(session.Forms.IsDomed, Is.False);
        }

        // ---- The song table fills in (plan section N) ----

        [Test]
        public void Discovery_TheBeginningOfASongPlayed_FillsItsSlot()
        {
            _world = new HiveDefenseWorld(withSongs: true);

            _world.Play(new[] { 0, 2 });

            Assert.That(_world.Session.Discovery.KnownNotes(HiveDefenseWorld.DomeIndex), Is.EqualTo(2));
            Assert.That(_world.Session.Discovery.IsKnown(HiveDefenseWorld.DomeIndex), Is.False);
        }

        // ---- The hives' stores (plan section M) ----

        [Test]
        public void New_Session_TheLastHiveStartsHurtAndEveryStoreHoldsStartStore()
        {
            _world = new HiveDefenseWorld();
            HiveDefenseSession session = _world.Session;

            Assert.That(session.Hives[0].Health, Is.EqualTo(_world.Hive.Health));
            Assert.That(session.Hives[1].Health, Is.EqualTo(_world.Hive.Health * _world.Hive.DamagedStartFraction).Within(0.001f));
            Assert.That(session.Hives[0].Store, Is.EqualTo(_world.Hive.StartStore));
            Assert.That(session.Hives[1].Store, Is.EqualTo(_world.Hive.StartStore));
        }

        [Test]
        public void Tick_HealthyHives_FillTheirStores()
        {
            _world = new HiveDefenseWorld(firstFarmSeconds: 20f);

            _world.Run(2f);

            Assert.That(_world.Session.Hives[0].Store, Is.EqualTo(_world.Hive.StartStore + (2f * _world.Hive.ProductionPerSecond)).Within(0.2f));
        }

        [Test]
        public void Tick_CoreBesideAHive_CollectsItsStore()
        {
            _world = new HiveDefenseWorld(hives: new[] { new Vector3(2f, 0f, 0f), new Vector3(40f, 0f, 40f) }, firstFarmSeconds: 20f);
            HiveDefenseSession session = _world.Session;
            int before = LeaveRoom();

            _world.Run(1f);

            Assert.That(session.NearHive, Is.EqualTo(0));
            Assert.That(session.Swarm.Count, Is.GreaterThanOrEqualTo(before + (int)_world.Hive.StartStore));
            Assert.That(session.Hives[0].Store, Is.LessThan(1f));
            Assert.That(session.Hives[1].Store, Is.GreaterThan(_world.Hive.StartStore), "the far hive's store is left alone");
        }

        [Test]
        public void Tick_GiveHeldBesideTheHurtHive_HealsItWithWasps()
        {
            _world = new HiveDefenseWorld(hives: new[] { new Vector3(40f, 0f, 40f), new Vector3(2f, 0f, 0f) }, firstFarmSeconds: 20f);
            HiveDefenseSession session = _world.Session;
            float health = session.Hives[1].Health;

            _world.RunWith(1f, _world.Frame(give: true));

            Assert.That(session.NearHive, Is.EqualTo(1));
            Assert.That(session.IsGiving, Is.True);
            Assert.That(session.IsCollecting, Is.False);
            Assert.That(session.Swarm.Count, Is.EqualTo(_world.Swarm.StartingWasps - (int)_world.Hive.HealWaspsPerSecond).Within(1));
            Assert.That(session.Hives[1].Health, Is.EqualTo(health + (_world.Hive.HealWaspsPerSecond / _world.Hive.WaspsPerHealthPoint)).Within(0.6f));
            Assert.That(session.Hives[1].Store, Is.GreaterThanOrEqualTo(_world.Hive.StartStore), "while Give is held the hive hands out nothing");
        }

        [Test]
        public void HiveCall_CallsEveryStoreFromAnywhereUntilTheyRunDry()
        {
            _world = new HiveDefenseWorld(withAllSongs: true, firstFarmSeconds: 20f);
            HiveDefenseSession session = _world.Session;
            int before = LeaveRoom();

            _world.Play(_world.Session.Caster[HiveDefenseWorld.HiveCallIndex].Song.Keys);
            Assert.That(session.Forms.Current, Is.EqualTo(SwarmForm.HiveCall));
            _world.Run(0.3f);

            Assert.That(session.Swarm.Count, Is.GreaterThan(before + 60), "both far stores flow in");
            Assert.That(session.Forms.Current, Is.EqualTo(SwarmForm.HiveCall));

            bool ended = _world.RunUntil(() => session.Forms.Current != SwarmForm.HiveCall, 2f);

            Assert.That(ended, Is.True);
            Assert.That(session.Forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(session.Swarm.Count, Is.GreaterThanOrEqualTo(before + (2 * (int)_world.Hive.StartStore)));
        }

        // The hurt last hive stands 2 m from the core, so each tick collects from it or gives to it.
        [Test]
        public void Tick_CollectingGivingAndCallingTheWave_AllocateNothing()
        {
            _world = new HiveDefenseWorld(hives: new[] { new Vector3(0f, 0f, 10f), new Vector3(2f, 0f, 0f) }, firstFarmSeconds: 20f);
            HiveDefenseSession session = _world.Session;
            HiveDefenseFrame collect = _world.Frame();
            HiveDefenseFrame give = _world.Frame(give: true);
            HiveDefenseFrame callWave = _world.Frame(callWave: true);
            session.Tick(collect, HiveDefenseWorld.Step);
            session.Tick(give, HiveDefenseWorld.Step);

            Assert.That(() => { session.Tick(collect, HiveDefenseWorld.Step); }, Is.Not.AllocatingGCMemory());
            Assert.That(session.IsCollecting, Is.True);
            Assert.That(() => { session.Tick(give, HiveDefenseWorld.Step); }, Is.Not.AllocatingGCMemory());
            Assert.That(session.IsGiving, Is.True);
            Assert.That(() => { session.Tick(callWave, HiveDefenseWorld.Step); }, Is.Not.AllocatingGCMemory());
            Assert.That(session.Phase, Is.EqualTo(MatchPhase.Survive));
        }

        [Test]
        public void Tick_RespawnAfterACollapse_TakesTheStoresAndComesBackWithAtLeastTheFloor()
        {
            _world = new HiveDefenseWorld(firstFarmSeconds: 20f);
            HiveDefenseSession session = _world.Session;

            // Emptied, the stores make only what the hives produce during the block, less than the floor of 120.
            session.Hives[0].TakeFromStore(session.Hives[0].Store);
            session.Hives[1].TakeFromStore(session.Hives[1].Store);
            session.Swarm.Lose(session.Swarm.Count);
            Assert.That(session.Swarm.IsCollapsed, Is.True);

            bool back = _world.RunUntil(() => !session.Swarm.IsCollapsed, _world.Swarm.CollapseBlockSeconds + 0.5f);

            // The respawn took every whole wasp; a part of a wasp stays, and the hives made a little more later in that tick.
            float left = 1f + (_world.Hive.ProductionPerSecond * HiveDefenseWorld.Step) + 0.001f;
            Assert.That(back, Is.True);
            Assert.That(session.Swarm.Count, Is.EqualTo(session.Swarm.MinRespawnWasps), "the stores held less than the floor");
            Assert.That(session.Hives[0].Store, Is.LessThan(left));
            Assert.That(session.Hives[1].Store, Is.LessThan(left));
        }

        // ---- The first-round lesson (plan section N) ----

        // Hive 0 stands 10 m ahead of the core, the hurt last hive 10 m behind.
        private HiveDefenseWorld LessonWorld(float firstFarmSeconds = 60f)
        {
            _world = new HiveDefenseWorld(
                hives: new[] { new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, -10f) }, withSongs: true, firstFarmSeconds: firstFarmSeconds);
            return _world;
        }

        [Test]
        public void Tutorial_EachStepDoneInOrder_WalksThroughTheLesson()
        {
            LessonWorld();
            var steps = new List<TutorialStep>();
            _world.Session.Tutorial.StepChanged += steps.Add;

            _world.MoveCoreTo(new Vector3(1f, 0f, 0f));
            _world.Play(_world.Sting.Song.Keys);
            Assert.That(_world.CollectNest(0), Is.True);
            Assert.That(_world.Session.Swarm.Room, Is.LessThan(1f), "the swarm is full, so the visit takes nothing");
            _world.MoveCoreTo(new Vector3(0f, 0f, 9f));
            _world.MoveCoreTo(new Vector3(0f, 0f, -9f));
            _world.RunWith(0.1f, _world.Frame(give: true));
            Assert.That(_world.Session.Tutorial.Current, Is.EqualTo(TutorialStep.CallWave));
            _world.RunWith(HiveDefenseWorld.Step, _world.Frame(callWave: true));

            Assert.That(
                steps,
                Is.EqualTo(new[]
                {
                    TutorialStep.PlaySting, TutorialStep.CollectNest, TutorialStep.VisitHive, TutorialStep.HealHive, TutorialStep.CallWave,
                    TutorialStep.Done,
                }));
        }

        // Plan section N, step 3: after the Sting the lesson sends the player to a nest.
        [Test]
        public void Tutorial_AfterTheSting_AsksForANestUntilOneIsCollected()
        {
            LessonWorld();
            _world.MoveCoreTo(new Vector3(1f, 0f, 0f));
            _world.Play(_world.Sting.Song.Keys);

            Assert.That(_world.Session.Tutorial.Current, Is.EqualTo(TutorialStep.CollectNest));

            _world.MoveCoreTo(_world.NestPositions[0]);
            _world.Run(1f);
            Assert.That(_world.Session.Tutorial.Current, Is.EqualTo(TutorialStep.CollectNest), "half the stay is not yet the nest");

            _world.Run(1.1f);
            Assert.That(_world.Session.Nests[0].IsCollected, Is.True);
            Assert.That(_world.Session.Tutorial.Current, Is.EqualTo(TutorialStep.VisitHive));
        }

        [Test]
        public void Tutorial_ANestCollectedBeforeItsTurn_IsDoneWhenTheStepComes()
        {
            LessonWorld();
            _world.MoveCoreTo(new Vector3(1f, 0f, 0f));
            Assert.That(_world.CollectNest(0), Is.True);
            Assert.That(_world.Session.Tutorial.Current, Is.EqualTo(TutorialStep.PlaySting), "a nest before its turn does not count then");

            _world.Play(_world.Sting.Song.Keys);

            Assert.That(_world.Session.Tutorial.Current, Is.EqualTo(TutorialStep.VisitHive), "the nest was already the player's");
        }

        [Test]
        public void Tutorial_AStepDoneBeforeItsTurn_DoesNotCountButIsNotBlocked()
        {
            LessonWorld();

            _world.Play(_world.Sting.Song.Keys);

            Assert.That(_world.Session.Forms.Slot(SongSlot.Strike).IsActive, Is.True, "the lesson never blocks play");
            Assert.That(_world.Session.Tutorial.Current, Is.EqualTo(TutorialStep.Move));

            _world.MoveCoreTo(new Vector3(1f, 0f, 0f));

            Assert.That(_world.Session.Tutorial.Current, Is.EqualTo(TutorialStep.PlaySting), "the Sting must be played in its turn");
        }

        [Test]
        public void Tutorial_AHiveIsStillHurtWhenTheHealStepComes_WaitsForTheGive()
        {
            LessonWorld();

            _world.MoveCoreTo(new Vector3(1f, 0f, 0f));
            _world.Play(_world.Sting.Song.Keys);
            _world.CollectNest(0);
            _world.MoveCoreTo(new Vector3(0f, 0f, 9f));
            _world.Run(0.5f);

            Assert.That(_world.Session.Hives[1].IsDamaged, Is.True);
            Assert.That(_world.Session.Tutorial.Current, Is.EqualTo(TutorialStep.HealHive));
        }

        // Healing cannot be repeated: once the hurt hive is full, nothing in the first farm phase hurts it again. The hurt
        // last hive stands 2 m from where the core starts, so Give heals it before the core has moved; hive 0 stands 10 m ahead.
        [Test]
        public void Tutorial_TheHurtHiveHealedBeforeItsTurn_SkipsTheHealStep()
        {
            _world = new HiveDefenseWorld(
                hives: new[] { new Vector3(0f, 0f, 10f), new Vector3(2f, 0f, 0f) }, withSongs: true, firstFarmSeconds: 60f);
            HiveDefenseSession session = _world.Session;
            var steps = new List<TutorialStep>();
            session.Tutorial.StepChanged += steps.Add;

            HiveConfig hive = _world.Hive;
            float healSeconds = hive.Health * (1f - hive.DamagedStartFraction) * hive.WaspsPerHealthPoint / hive.HealWaspsPerSecond;
            _world.RunWith(healSeconds + 0.5f, _world.Frame(give: true));
            Assert.That(session.Hives[1].IsDamaged, Is.False, "Give healed the hurt hive to full during the first step");
            Assert.That(session.Tutorial.Current, Is.EqualTo(TutorialStep.Move), "a heal before its turn does not count then");

            _world.MoveCoreTo(new Vector3(-5f, 0f, 0f));
            _world.Play(_world.Sting.Song.Keys);
            _world.CollectNest(0);
            _world.MoveCoreTo(new Vector3(0f, 0f, 9f));

            Assert.That(session.Tutorial.Current, Is.EqualTo(TutorialStep.CallWave), "with no hive hurt, the heal step has nothing to teach");
            Assert.That(
                steps,
                Is.EqualTo(new[] { TutorialStep.PlaySting, TutorialStep.CollectNest, TutorialStep.VisitHive, TutorialStep.HealHive, TutorialStep.CallWave }));
        }

        [Test]
        public void Tutorial_TheFirstWaveStartsOnTheTimer_EndsTheLesson()
        {
            LessonWorld(HiveDefenseWorld.ShortFirstFarmSeconds);

            _world.Run(HiveDefenseWorld.ShortFirstFarmSeconds + 0.1f);

            Assert.That(_world.Session.Tutorial.Current, Is.EqualTo(TutorialStep.Done));
        }

        [Test]
        public void Tutorial_CallingTheWaveEarly_EndsTheLesson()
        {
            LessonWorld();

            _world.RunWith(HiveDefenseWorld.Step, _world.Frame(callWave: true));

            Assert.That(_world.Session.Tutorial.Current, Is.EqualTo(TutorialStep.Done));
        }

        // ---- Nests and the swarm's level (plan section H) ----

        // The default nests, with the first one moved to where the core starts, so it is collected in the first 2 s.
        private static Vector3[] NestUnderTheCore()
        {
            var nests = (Vector3[])HiveDefenseWorld.DefaultNests.Clone();
            nests[0] = Vector3.zero;
            return nests;
        }

        [Test]
        public void New_Session_StartsAtEightHundredOfEightHundredAtLevelOneWithTwoOfEightNestsOpen()
        {
            _world = new HiveDefenseWorld();
            HiveDefenseSession session = _world.Session;

            Assert.That(session.Swarm.Count, Is.EqualTo(800));
            Assert.That(session.Swarm.Capacity, Is.EqualTo(800));
            Assert.That(session.Swarm.Max, Is.EqualTo(4000));
            Assert.That(session.Level, Is.EqualTo(1));
            Assert.That(session.SizeScale, Is.EqualTo(1f));
            Assert.That(session.DamageScale, Is.EqualTo(1f));
            Assert.That(session.ColossusSizeScale, Is.EqualTo(1f));
            Assert.That(session.Nests.Count, Is.EqualTo(8));
            Assert.That(session.Nests.OpenCount, Is.EqualTo(2));
            Assert.That(session.Nests.CollectedCount, Is.EqualTo(0));
            Assert.That(session.Nests[0].Position, Is.EqualTo(HiveDefenseWorld.DefaultNests[0]));
        }

        [Test]
        public void New_WrongNumberOfNestPositions_Throws()
        {
            _world = new HiveDefenseWorld();
            var rules = new HiveDefenseRules(
                _world.Swarm, _world.Squads, _world.CoreMove, _world.Fury, _world.Colossus, _world.Hive, _world.Nests,
                _world.SwarmLevel, _world.Smoke, _world.WaveConfig, _world.Stick, _world.Smoker, _world.Aim, _world.SongConfig,
                _world.Songs, HiveDefenseWorld.HoleCount);
            Vector3[] hives = { new Vector3(-40f, 0f, 40f) };
            Vector3[] spawns = { Vector3.one };

            Assert.That(
                () => new HiveDefenseSession(rules, hives, new Vector3[7], spawns, Vector3.zero, new FixedRandomSource()),
                Throws.ArgumentException);
            Assert.That(
                () => new HiveDefenseSession(rules, hives, null, spawns, Vector3.zero, new FixedRandomSource()),
                Throws.ArgumentNullException);
        }

        [Test]
        public void New_NoNestOrLevelConfig_Throws()
        {
            _world = new HiveDefenseWorld();

            Assert.That(
                () => new HiveDefenseRules(
                    _world.Swarm, _world.Squads, _world.CoreMove, _world.Fury, _world.Colossus, _world.Hive, null, _world.SwarmLevel,
                    _world.Smoke, _world.WaveConfig, _world.Stick, _world.Smoker, _world.Aim, _world.SongConfig, _world.Songs,
                    HiveDefenseWorld.HoleCount),
                Throws.ArgumentNullException);
            Assert.That(
                () => new HiveDefenseRules(
                    _world.Swarm, _world.Squads, _world.CoreMove, _world.Fury, _world.Colossus, _world.Hive, _world.Nests, null,
                    _world.Smoke, _world.WaveConfig, _world.Stick, _world.Smoker, _world.Aim, _world.SongConfig, _world.Songs,
                    HiveDefenseWorld.HoleCount),
                Throws.ArgumentNullException);
        }

        [Test]
        public void TheTestWorldsNests_KeepTheLayoutRules()
        {
            _world = new HiveDefenseWorld();

            Assert.That(
                () => NestLayout.Check(_world.NestPositions, new[] { new Vector3(-40f, 0f, 40f), new Vector3(40f, 0f, 40f) }, _world.Nests, _world.CoreMove.PlayAreaHalfSize),
                Throws.Nothing);
        }

        [Test]
        public void Nests_OpenOnTheTickTheirWaveIsCleared()
        {
            _world = new HiveDefenseWorld(new[] { new WaveEntry(1, 0), new WaveEntry(1, 0) });
            HiveDefenseSession session = _world.Session;
            var opened = new List<int>();
            session.Nests.NestOpened += opened.Add;
            _world.RunUntil(() => session.ActiveBeekeeperCount == 1, 5f);
            _world.Run(1f);
            Assert.That(session.Nests.OpenCount, Is.EqualTo(2), "not while the wave lives");

            _world.KillAllBeekeepers();
            bool farm = _world.RunUntil(() => session.Phase == MatchPhase.Farm, 1f);

            Assert.That(farm, Is.True);
            Assert.That(session.Nests.OpenCount, Is.EqualTo(4), "opened on the tick the wave was cleared");
            Assert.That(opened, Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void CollectingANest_GivesFourHundredCapacityFourHundredWaspsAndALevel()
        {
            _world = new HiveDefenseWorld(firstFarmSeconds: 20f);
            HiveDefenseSession session = _world.Session;
            var collected = new List<int>();
            session.Nests.NestCollected += collected.Add;

            Assert.That(_world.CollectNest(0), Is.True);

            Assert.That(collected, Is.EqualTo(new[] { 0 }));
            Assert.That(session.Swarm.Capacity, Is.EqualTo(1200));
            Assert.That(session.Swarm.Count, Is.EqualTo(1200));
            Assert.That(session.Level, Is.EqualTo(2));
            Assert.That(session.SizeScale, Is.EqualTo(1.12f).Within(1e-5f));
            Assert.That(session.DamageScale, Is.EqualTo(1.12f).Within(1e-5f));
            Assert.That(session.Nests.OpenCount, Is.EqualTo(1), "the nest is gone");
        }

        [Test]
        public void CollectingANest_WithAHurtSwarm_AddsFourHundredWaspsNow()
        {
            _world = new HiveDefenseWorld(firstFarmSeconds: 20f);
            HiveDefenseSession session = _world.Session;
            session.Swarm.Lose(300);

            _world.CollectNest(0);

            Assert.That(session.Swarm.Count, Is.EqualTo(900));
            Assert.That(session.Swarm.Capacity, Is.EqualTo(1200));
        }

        [Test]
        public void CollectingANest_NearTheCeiling_StopsTheCapacityAtFourThousandAndTheWaspsAtTheCapacity()
        {
            _world = new HiveDefenseWorld(firstFarmSeconds: 20f);
            HiveDefenseSession session = _world.Session;
            session.Swarm.GrowCapacity(3000);
            session.Swarm.Gain(3000f);
            Assert.That(session.Swarm.Count, Is.EqualTo(3800));

            _world.CollectNest(0);

            Assert.That(session.Swarm.Capacity, Is.EqualTo(4000), "only 200 more fit under the ceiling");
            Assert.That(session.Swarm.Count, Is.EqualTo(4000), "and the 400 wasps stop at the capacity");
            Assert.That(session.Level, Is.EqualTo(2), "the level still rises");
        }

        [Test]
        public void CollectingANest_LeavingBeforeTwoSeconds_StartsTheStayAgain()
        {
            _world = new HiveDefenseWorld(firstFarmSeconds: 20f);
            HiveDefenseSession session = _world.Session;
            Vector3 nest = _world.NestPositions[0];

            _world.MoveCoreTo(nest);
            _world.Run(1.5f);
            Assert.That(session.Nests[0].Progress01, Is.GreaterThan(0.7f));
            _world.MoveCoreTo(nest + new Vector3(3f, 0f, 0f));
            Assert.That(session.Nests[0].Progress01, Is.EqualTo(0f));
            _world.MoveCoreTo(nest);
            _world.Run(1.5f);

            Assert.That(session.Nests[0].IsCollected, Is.False, "two stays of 1.5 s are not one of 2 s");
            Assert.That(session.Level, Is.EqualTo(1));
        }

        [Test]
        public void CollectingANest_AHitOnTheCore_StartsTheStayAgain()
        {
            Vector3 nest = HiveDefenseWorld.DefaultNests[0];
            _world = new HiveDefenseWorld(OneStick(), spawns: new[] { nest + new Vector3(1f, 0f, 0f) });
            HiveDefenseSession session = _world.Session;
            _world.RunUntil(() => session.ActiveBeekeeperCount == 1, 5f);
            int hits = 0;
            session.ActiveBeekeeper(0).CoreHit += brain => hits++;
            _world.MoveCoreTo(nest);
            float progressBeforeTheHit = 0f;

            bool hit = _world.RunUntil(
                () =>
                {
                    if (hits == 0)
                    {
                        progressBeforeTheHit = session.Nests[0].Progress01;
                    }

                    return hits > 0;
                },
                1.9f);

            Assert.That(hit, Is.True, "the stick struck within the 2 s");
            Assert.That(progressBeforeTheHit, Is.GreaterThan(0f));
            Assert.That(session.Nests[0].Progress01, Is.EqualTo(0f));
            Assert.That(session.Nests[0].IsCollected, Is.False);
        }

        [Test]
        public void CollectingANest_WhileTheSwarmIsCollapsed_MakesNoProgress()
        {
            _world = new HiveDefenseWorld(firstFarmSeconds: 20f);
            HiveDefenseSession session = _world.Session;
            _world.MoveCoreTo(_world.NestPositions[0]);
            session.Swarm.Lose(session.Swarm.Count);

            _world.Run(_world.Swarm.CollapseBlockSeconds - 0.2f);

            Assert.That(session.Nests[0].Progress01, Is.EqualTo(0f));
            Assert.That(session.Nests[0].IsCollected, Is.False);
            Assert.That(_world.RunUntil(() => session.Nests[0].IsCollected, 3f), Is.True, "after the respawn the stay counts again");
        }

        [Test]
        public void CollectingANest_ThatIsNotOpenYet_DoesNothing()
        {
            _world = new HiveDefenseWorld(firstFarmSeconds: 20f);
            HiveDefenseSession session = _world.Session;

            _world.MoveCoreTo(_world.NestPositions[2]);
            _world.Run(3f);

            Assert.That(session.Nests[2].IsCollected, Is.False);
            Assert.That(session.Level, Is.EqualTo(1));
            Assert.That(session.Swarm.Capacity, Is.EqualTo(800));
        }

        [Test]
        public void CollectingTwoNestsInReachAtOnce_GivesBoth()
        {
            var nests = (Vector3[])HiveDefenseWorld.DefaultNests.Clone();
            nests[0] = new Vector3(-1f, 0f, 12f);
            nests[1] = new Vector3(1f, 0f, 12f);
            _world = new HiveDefenseWorld(nests: nests, firstFarmSeconds: 20f);
            HiveDefenseSession session = _world.Session;

            _world.MoveCoreTo(new Vector3(0f, 0f, 12f));
            _world.Run(2.1f);

            Assert.That(session.Nests.CollectedCount, Is.EqualTo(2));
            Assert.That(session.Level, Is.EqualTo(3));
            Assert.That(session.Swarm.Capacity, Is.EqualTo(1600));
            Assert.That(session.Swarm.Count, Is.EqualTo(1600));
        }

        [Test]
        public void Level_FollowsTheNestsCollected_NotTheWaspsAlive()
        {
            _world = new HiveDefenseWorld(firstFarmSeconds: 20f);
            HiveDefenseSession session = _world.Session;
            _world.CollectNest(0);
            float damage = session.DamageScale;

            session.Swarm.Lose(session.Swarm.Count - session.Swarm.CollapseLine);

            Assert.That(session.Level, Is.EqualTo(2));
            Assert.That(session.DamageScale, Is.EqualTo(damage), "a hurt swarm does not hit softer");
            Assert.That(session.SizeScale, Is.EqualTo(1.12f).Within(1e-5f));
        }

        // The cast takes the level as it is: the Tornado's reach, height and wall reach the picture grown by 12%.
        [Test]
        public void Cast_AfterANest_SendsThePictureTheGrownSizes()
        {
            _world = new HiveDefenseWorld(withAllSongs: true, firstFarmSeconds: 20f);
            HiveDefenseSession session = _world.Session;
            _world.CollectNest(0);

            _world.Play(session.Caster[HiveDefenseWorld.TornadoIndex].Song.Keys);
            _world.Play(DomeHoles);

            SlotState strike = session.Forms.Slot(SongSlot.Strike);
            Assert.That(strike.Form, Is.EqualTo(SwarmForm.Tornado));
            Assert.That(strike.ParamA.x, Is.EqualTo(5f * 1.12f).Within(1e-4f));
            Assert.That(strike.ParamA.y, Is.EqualTo(3f * 1.12f).Within(1e-4f));
            Assert.That(strike.ParamB.x, Is.EqualTo(1.5f * 1.12f).Within(1e-4f));
            Assert.That(session.Forms.Slot(SongSlot.Guard).ParamA.x, Is.EqualTo(2.2f * 1.12f).Within(1e-4f), "the Dome too");
        }

        // The marker and the Sting share the Sting's reach, which grows with the level: 12 m, then 13.44 m.
        [TestCase(false)]
        [TestCase(true)]
        public void AimTarget_ReachGrowsWithTheLevel(bool afterANest)
        {
            _world = new HiveDefenseWorld(
                OneStick(), spawns: new[] { new Vector3(12.6f, 0f, 0f) }, nests: afterANest ? NestUnderTheCore() : null, firstFarmSeconds: 3f);
            StandStillAsTheySpawn();
            _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 1, 5f);
            _world.Run(HiveDefenseWorld.Step);

            Assert.That(_world.Session.Level, Is.EqualTo(afterANest ? 2 : 1));
            if (afterANest)
            {
                Assert.That(_world.Session.AimTarget, Is.SameAs(_world.Session.ActiveBeekeeper(0)));
            }
            else
            {
                Assert.That(_world.Session.AimTarget, Is.Null, "12.6 m is beyond 12 m");
            }
        }

        // The stick stands 4.2 m ahead of the giant: beyond a punch at 2.5 m with a 1.5 m reach, within one at 2.8 m with 1.68.
        [TestCase(false)]
        [TestCase(true)]
        public void Colossus_AfterANest_PunchesHarderAndFarther(bool afterANest)
        {
            _world = new HiveDefenseWorld(
                OneStick(), spawns: new[] { new Vector3(0f, 0f, 4.2f) }, nests: afterANest ? NestUnderTheCore() : null, firstFarmSeconds: 3f);
            HiveDefenseSession session = _world.Session;
            StandStillAsTheySpawn();
            _world.RunUntil(() => session.ActiveBeekeeperCount == 1, 5f);
            session.Forms.StartColossus(ColossusSeconds);
            _world.Run(HiveDefenseWorld.Step);
            BeekeeperBrain keeper = session.ActiveBeekeeper(0);

            session.Tick(_world.Frame(punch: true), HiveDefenseWorld.Step);
            _world.Run(_world.Colossus.PunchHitDelay + 0.1f);

            Assert.That(session.ColossusSizeScale, Is.EqualTo(afterANest ? 1.12f : 1f).Within(1e-5f), "the giant's size follows the level");
            float expected = afterANest ? _world.Stick.Health - (_world.Colossus.PunchDamage * 1.12f) : _world.Stick.Health;
            Assert.That(keeper.Health, Is.EqualTo(expected).Within(0.01f));
        }

        [Test]
        public void Colossus_KeepsTheLevelItStoodUpAt()
        {
            var nests = NestUnderTheCore();
            nests[1] = new Vector3(0f, 0f, 3.5f);
            _world = new HiveDefenseWorld(nests: nests, firstFarmSeconds: 20f);
            HiveDefenseSession session = _world.Session;
            _world.Run(2.1f);
            Assert.That(session.Level, Is.EqualTo(2));
            session.Forms.StartColossus(ColossusSeconds);
            _world.Run(HiveDefenseWorld.Step);

            _world.RunWith(1f, _world.Frame(move: new Vector2(0f, 1f)));
            _world.Run(2.1f);

            Assert.That(session.Nests[1].IsCollected, Is.True, "the core rides inside the giant to the nest");
            Assert.That(session.Level, Is.EqualTo(3));
            Assert.That(session.IsColossusActive, Is.True);
            Assert.That(session.ColossusSizeScale, Is.EqualTo(1.12f).Within(1e-5f), "the giant standing keeps its size");
        }

        // The world's random source always says 0.5, so the hum picks the middle of the six unfound songs: Dome, Colossus,
        // Tornado, Meteor, Gust and Hive Call, in the caster's order, have index 3, the Meteor.
        [Test]
        public void CollectingANest_HumsTheFirstTwoNotesOfAnUnfoundSongChosenByTheRandomSource()
        {
            _world = new HiveDefenseWorld(withAllSongs: true, firstFarmSeconds: 20f);
            SongDiscovery discovery = _world.Session.Discovery;
            var hummed = new List<int>();
            discovery.Hummed += hummed.Add;

            _world.CollectNest(0);

            Assert.That(hummed, Is.EqualTo(new[] { HiveDefenseWorld.MeteorIndex }));
            Assert.That(discovery.KnownNotes(HiveDefenseWorld.MeteorIndex), Is.EqualTo(2), "J K of J K L K");
            Assert.That(discovery.IsKnown(HiveDefenseWorld.MeteorIndex), Is.False, "never the whole song");
            Assert.That(discovery.KnownNotes(HiveDefenseWorld.TornadoIndex), Is.EqualTo(0), "one song only");
        }

        [Test]
        public void CollectingANest_WithEverySongFound_HumsNothing()
        {
            _world = new HiveDefenseWorld(withAllSongs: true, firstFarmSeconds: 20f);
            SongDiscovery discovery = _world.Session.Discovery;
            for (int song = 0; song < discovery.Count; song++)
            {
                discovery.NoteCast(song);
            }

            var hummed = new List<int>();
            discovery.Hummed += hummed.Add;

            Assert.That(_world.CollectNest(0), Is.True);

            Assert.That(hummed, Is.Empty);
            Assert.That(_world.Session.Level, Is.EqualTo(2), "the nest still counts");
        }

        [Test]
        public void Tick_StayingAtANest_AllocatesNothing()
        {
            _world = new HiveDefenseWorld(firstFarmSeconds: 20f);
            HiveDefenseSession session = _world.Session;
            _world.MoveCoreTo(_world.NestPositions[0]);
            _world.Run(0.1f);
            HiveDefenseFrame stay = _world.Frame();

            Assert.That(() => { session.Tick(stay, HiveDefenseWorld.Step); }, Is.Not.AllocatingGCMemory());
            Assert.That(session.Nests[0].Progress01, Is.GreaterThan(0f));
        }
    }
}
