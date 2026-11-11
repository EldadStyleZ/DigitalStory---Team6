using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Uses the default configs, except that the first wave comes after 1 s. The hives stand far from the Piper
    /// unless a test moves them, so a beekeeper that spawns beside the Piper goes for the Piper.
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
                _world.Piper, null, _world.Piper, session.Swarm, session.Forms, session.Fury, session.Hives, new List<ISwarmTarget>(), session.Smoke);
        }

        // A stick that spawns 1 m from the Piper, who stands at the origin.
        private HiveDefenseWorld StickBesidePiper(WaveEntry[] waves = null, bool withSongs = false)
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
            Assert.That(session.Hives.Count, Is.EqualTo(2));
            Assert.That(session.Hives.StandingCount, Is.EqualTo(2));
            Assert.That(session.Hives[0].Position, Is.EqualTo(new Vector3(-40f, 0f, 40f)));
            Assert.That(session.ActiveBeekeeperCount, Is.EqualTo(0));
            Assert.That(session.IsColossusActive, Is.False);
            Assert.That(session.PiperSpeedMultiplier, Is.EqualTo(1f));
            Assert.That(session.IsPiperRooted, Is.False);
            Assert.That(session.AimTarget, Is.Null);
        }

        [Test]
        public void Tick_BothHivesDestroyed_IsLost()
        {
            _world = new HiveDefenseWorld();
            var changes = new List<HiveDefenseState>();
            _world.Session.StateChanged += changes.Add;

            _world.Session.Hives[0].Damage(300f);
            _world.Run(HiveDefenseWorld.Step);
            Assert.That(_world.Session.State, Is.EqualTo(HiveDefenseState.Playing), "one hive still stands");

            _world.Session.Hives[1].Damage(300f);
            _world.Run(HiveDefenseWorld.Step);

            Assert.That(_world.Session.State, Is.EqualTo(HiveDefenseState.Lost));
            Assert.That(changes, Is.EqualTo(new[] { HiveDefenseState.Lost }));
        }

        [Test]
        public void Tick_AfterLost_DoesNothing()
        {
            _world = new HiveDefenseWorld();
            _world.Session.Hives[0].Damage(300f);
            _world.Session.Hives[1].Damage(300f);
            _world.Run(HiveDefenseWorld.Step);
            int wasps = _world.Session.Swarm.Count;
            float untilWave = _world.Session.Waves.SecondsToNextWave;

            _world.Run(5f);

            Assert.That(_world.Session.State, Is.EqualTo(HiveDefenseState.Lost));
            Assert.That(_world.Session.Waves.WaveNumber, Is.EqualTo(0));
            Assert.That(_world.Session.Waves.SecondsToNextWave, Is.EqualTo(untilWave));
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
            _world.Session.Hives[0].Damage(300f);
            _world.Session.Hives[1].Damage(300f);

            _world.Run(1f);

            Assert.That(_world.Session.State, Is.EqualTo(HiveDefenseState.Won), "a won match cannot be lost afterwards");
        }

        [Test]
        public void Tick_FirstWaveDelayNotOver_NoBeekeeperYet()
        {
            _world = new HiveDefenseWorld();

            _world.Run(0.5f);

            Assert.That(_world.Session.Waves.WaveNumber, Is.EqualTo(0));
            Assert.That(_world.Session.ActiveBeekeeperCount, Is.EqualTo(0));
            Assert.That(_world.Session.State, Is.EqualTo(HiveDefenseState.Playing));
        }

        [Test]
        public void Tick_HugeDeltaTime_IsClampedToTheMaximumStep()
        {
            _world = new HiveDefenseWorld();
            float before = _world.Session.Waves.SecondsToNextWave;

            _world.Session.Tick(_world.Frame(), 10f);

            Assert.That(_world.Session.Waves.SecondsToNextWave, Is.EqualTo(before - _world.Swarm.MaxDeltaTime).Within(0.0001f));
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
        public void StickHitOnPiper_RemovesTheWaspsItKnocksOff()
        {
            StickBesidePiper();
            _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 1, 10f);

            bool hit = _world.RunUntil(() => _world.Session.Swarm.Count < _world.Swarm.StartingWasps, 10f);

            Assert.That(hit, Is.True);
            Assert.That(_world.Session.Swarm.Count, Is.EqualTo(_world.Swarm.StartingWasps - _world.Stick.WaspsKnockedOff));
        }

        [Test]
        public void StickHitOnPiper_UnderTheDome_RemovesFewerWasps()
        {
            StickBesidePiper(withSongs: true);
            _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 1, 10f);
            _world.Play(DomeHoles);
            Assert.That(_world.Session.Forms.IsDomed, Is.True);

            bool hit = _world.RunUntil(() => _world.Session.Swarm.Count < _world.Swarm.StartingWasps, 10f);

            int lost = _world.Swarm.StartingWasps - _world.Session.Swarm.Count;
            var dome = (DomeSong)_world.Songs[HiveDefenseWorld.DomeIndex];
            Assert.That(hit, Is.True);
            Assert.That(lost, Is.EqualTo(Mathf.RoundToInt(_world.Stick.WaspsKnockedOff * dome.LossFraction)));
            Assert.That(lost, Is.LessThan(_world.Stick.WaspsKnockedOff));
        }

        [Test]
        public void PiperSpeedMultiplier_UnderTheDome_IsTheDomeAssetsMultiplier()
        {
            _world = new HiveDefenseWorld(withSongs: true);
            var dome = (DomeSong)_world.Songs[HiveDefenseWorld.DomeIndex];
            JsonUtility.FromJsonOverwrite("{\"<SpeedMultiplier>k__BackingField\":0.3}", dome);

            _world.Play(DomeHoles);

            Assert.That(_world.Session.PiperSpeedMultiplier, Is.EqualTo(0.3f).Within(1e-5f));
        }

        [Test]
        public void StickHitOnPiper_UnderTheDome_RipplesTheDomeFromTheHittersSide()
        {
            StickBesidePiper(withSongs: true);
            _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 1, 10f);
            _world.Play(DomeHoles);

            bool hit = _world.RunUntil(() => _world.Session.Swarm.Count < _world.Swarm.StartingWasps, 10f);

            Vector4 dome = _world.Session.Forms.Slot(SongSlot.Guard).ParamA;
            Assert.That(hit, Is.True);
            Assert.That(dome.w, Is.GreaterThan(0f), "the dome's seconds when it blocked the hit");
            Assert.That(dome.y, Is.EqualTo(1f).Within(0.05f), "the stick stands on the Piper's +x side");
            Assert.That(dome.z, Is.EqualTo(0f).Within(0.3f));
        }

        [Test]
        public void StickHitOnPiper_DuringHiveCallUnderTheDome_KeepsHiveCall()
        {
            StickBesidePiper(withSongs: true);
            _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 1, 10f);
            int hits = 0;
            _world.Session.ActiveBeekeeper(0).PiperHit += brain => hits++;
            _world.Play(DomeHoles);
            Assert.That(_world.Session.Forms.StartHiveCall(), Is.True);

            bool hit = _world.RunUntil(() => hits > 0, 10f);

            Assert.That(hit, Is.True);
            Assert.That(_world.Session.Forms.Current, Is.EqualTo(SwarmForm.HiveCall), "Dome then Hive Call is the safe refill");
        }

        // Plan section A: Hive Call ends when the Piper moves. The Piper is held in place while it lasts, so its position
        // never changes: asking to move must end it, or the held key would be refused without a sign.
        [Test]
        public void HiveCall_ThePlayerAsksToMove_EndsIt_ThoughThePiperIsHeldInPlace()
        {
            _world = new HiveDefenseWorld();
            Assert.That(_world.Session.Forms.StartHiveCall(), Is.True);
            _world.Run(HiveDefenseWorld.Step);
            Assert.That(_world.Session.Forms.Current, Is.EqualTo(SwarmForm.HiveCall), "standing still keeps it");

            _world.Session.Tick(_world.Frame(move: new Vector2(1f, 0f)), HiveDefenseWorld.Step);

            Assert.That(_world.Session.Forms.Current, Is.EqualTo(SwarmForm.River));
            Assert.That(_world.Session.IsPiperRooted, Is.False);
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

        [Test]
        public void StickHitOnPiper_CancelsHiveCallAndSetsTheLastHitPoint()
        {
            StickBesidePiper();
            _world.RunUntil(() => _world.Session.ActiveBeekeeperCount == 1, 10f);
            Assert.That(_world.Session.Forms.StartHiveCall(), Is.True);

            _world.RunUntil(() => _world.Session.Swarm.Count < _world.Swarm.StartingWasps, 10f);

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
                piper: new Vector3(0f, 0f, 80f));

            bool hit = _world.RunUntil(() => _world.Session.Hives[0].Health < 300f, 15f);

            Assert.That(hit, Is.True);
            Assert.That(_world.Session.Hives[0].Health, Is.EqualTo(300f - _world.Stick.HiveDamage));
            Assert.That(_world.Session.Hives[1].Health, Is.EqualTo(300f));
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
                piper: new Vector3(0f, 0f, 80f));
            var shell = ScriptableObject.CreateInstance<FakeSlotSong>();
            try
            {
                shell.ShieldedHive = 0;
                shell.HiveDamageFraction = 0.25f;
                SwarmSongContext context = SongContext();
                shell.Cast(in context);

                bool hit = _world.RunUntil(() => _world.Session.Hives[0].Health < 300f, 15f);

                Assert.That(hit, Is.True);
                Assert.That(_world.Session.Hives[0].Health, Is.EqualTo(300f - (_world.Stick.HiveDamage * 0.25f)).Within(1e-3f));
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
        public void PiperSpeedMultiplier_InSmoke_IsSlowerUnlessARunningSongKeepsTheSmokeOff()
        {
            _world = new HiveDefenseWorld();
            var wings = ScriptableObject.CreateInstance<FakeSlotSong>();
            try
            {
                _world.Session.Smoke.Throw(Origin, Origin);
                _world.Run(1.5f);
                Assert.That(_world.Session.Smoke.IsPiperInSmoke, Is.True);
                Assert.That(_world.Session.PiperSpeedMultiplier, Is.EqualTo(_world.PiperMove.SmokeSpeedMultiplier).Within(1e-5f));

                wings.SlotToUse = SongSlot.Guard;
                wings.PiperSpeedMultiplier = 1.6f;
                wings.SmokeSlowsPiper = false;
                SwarmSongContext context = SongContext();
                wings.Cast(in context);

                Assert.That(_world.Session.PiperSpeedMultiplier, Is.EqualTo(1.6f).Within(1e-5f));
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
            _world = new HiveDefenseWorld(piper: new Vector3(5f, 0f, 5f));
            _world.Session.Fury.AddDamage(_world.Fury.Max * _world.Fury.DamagePerFuryPoint);
            Assert.That(_world.Session.Fury.TryUse(), Is.True);
            Assert.That(_world.Session.Forms.StartColossus(ColossusSeconds), Is.True);

            _world.Session.Tick(_world.Frame(), HiveDefenseWorld.Step);
            Assert.That(_world.Session.IsColossusActive, Is.True);
            Assert.That(_world.Session.IsPiperRooted, Is.True);
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
            Assert.That(_world.Session.IsPiperRooted, Is.False);
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
            _world.Session.Caster.SongNotReady += notReady.Add;
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
            _world.Session.Caster.SongNotReady += notReady.Add;
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

        [TestCase(HiveDefenseWorld.MeteorIndex)]
        [TestCase(HiveDefenseWorld.StingIndex)]
        public void Cast_AimedSongWithNothingToHit_StrikesSixMetresAheadOfThePipersFacing(int song)
        {
            _world = new HiveDefenseWorld(piper: new Vector3(5f, 0f, 5f), withAllSongs: true);
            _world.Facing = Vector3.right;
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
        public void Cast_FacingWithHeightAndAnyLength_StillStrikesSixMetresAheadOnTheGround()
        {
            _world = new HiveDefenseWorld(withAllSongs: true);
            _world.Facing = new Vector3(0f, 3f, -0.5f);

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
                    _world.Swarm, _world.Squads, _world.PiperMove, _world.Fury, _world.Colossus, _world.Hive, _world.Smoke,
                    _world.WaveConfig, _world.Stick, _world.Smoker, _world.Aim, _world.SongConfig, new SwarmSongEffect[] { dome },
                    HiveDefenseWorld.HoleCount);

                Assert.That(
                    () => new HiveDefenseSession(rules, new[] { Vector3.zero }, new[] { Vector3.one }, Vector3.zero, new FixedRandomSource()),
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
            _world.Session.Caster.SongNotReady += notReady.Add;

            _world.Play(_world.Session.Caster[song].Song.Keys);

            Assert.That(_world.Session.ActiveBeekeeperCount, Is.EqualTo(0));
            Assert.That(cast, Is.EqualTo(new[] { song }));
            Assert.That(notReady, Is.Empty);
        }
    }
}
