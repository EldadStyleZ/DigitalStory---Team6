using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class BeekeeperBrainTests
    {
        private const float Step = 0.05f;
        private const float TinyStep = 0.001f;
        private const float Margin = 0.01f;
        private const float FarAway = 200f;

        // Smoker asset values from the spec: Smoker, 40, 2.2, 7, 14, 14, 0.8, 4.5, 0, 0.
        private const string SmokerJson =
            "{\"<Kind>k__BackingField\":1,\"<Health>k__BackingField\":40,\"<Speed>k__BackingField\":2.2,"
            + "\"<AttackRange>k__BackingField\":14,\"<WindUpSeconds>k__BackingField\":0.8,"
            + "\"<CooldownSeconds>k__BackingField\":4.5,\"<HiveDamage>k__BackingField\":0,"
            + "\"<WaspsKnockedOff>k__BackingField\":0}";

        private static readonly Vector3 CoreFar = new Vector3(FarAway, 0f, FarAway);

        private sealed class Recorder
        {
            public readonly List<int> HiveHits = new List<int>();
            public readonly List<Vector3> Grenades = new List<Vector3>();
            public int CoreHits;
            public int Deaths;
            public BeekeeperBrain LastSender;

            public Recorder(BeekeeperBrain brain)
            {
                brain.HiveHit += (sender, index) =>
                {
                    LastSender = sender;
                    HiveHits.Add(index);
                };
                brain.CoreHit += sender =>
                {
                    LastSender = sender;
                    CoreHits++;
                };
                brain.GrenadeThrown += (sender, landing) =>
                {
                    LastSender = sender;
                    Grenades.Add(landing);
                };
                brain.Died += sender =>
                {
                    LastSender = sender;
                    Deaths++;
                };
            }
        }

        private static BeekeeperConfig StickConfig()
        {
            return ScriptableObject.CreateInstance<BeekeeperConfig>();
        }

        private static BeekeeperConfig SmokerConfig()
        {
            var config = ScriptableObject.CreateInstance<BeekeeperConfig>();
            JsonUtility.FromJsonOverwrite(SmokerJson, config);
            return config;
        }

        private static HiveSet HivesAt(params Vector3[] positions)
        {
            var hiveConfig = ScriptableObject.CreateInstance<HiveConfig>();
            var hives = new List<Hive>();
            foreach (Vector3 position in positions)
            {
                hives.Add(new Hive(hiveConfig, position));
            }

            return new HiveSet(hives);
        }

        // A smoker with the stick's numbers, so its short reach shows the chase on foot. Smokers keep the notice and give-up
        // ranges that sticks gave up (plan section O).
        private static BeekeeperConfig HunterConfig()
        {
            var config = ScriptableObject.CreateInstance<BeekeeperConfig>();
            JsonUtility.FromJsonOverwrite("{\"<Kind>k__BackingField\":1}", config);
            return config;
        }

        private static BeekeeperSenses Senses(HiveSet hives, Vector3 core, bool canSeeCore = true)
        {
            return new BeekeeperSenses(core, hives, Vector3.zero, canSeeCore);
        }

        private static BeekeeperBrain SpawnAt(BeekeeperConfig config, Vector3 position)
        {
            var brain = new BeekeeperBrain(config);
            brain.Spawn(position);
            return brain;
        }

        private static void Advance(BeekeeperBrain brain, in BeekeeperSenses senses, float seconds, float step = Step)
        {
            float left = seconds;
            while (left > 0f)
            {
                float dt = Mathf.Min(step, left);
                brain.Tick(in senses, dt);
                left -= dt;
            }
        }

        private static bool AdvanceUntil(
            BeekeeperBrain brain, in BeekeeperSenses senses, System.Func<bool> done, float maxSeconds)
        {
            float elapsed = 0f;
            while (elapsed < maxSeconds)
            {
                if (done())
                {
                    return true;
                }

                brain.Tick(in senses, Step);
                elapsed += Step;
            }

            return done();
        }

        private static float GroundDistance(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }

        // ---------- Spawn ----------

        [Test]
        public void New_BeforeItSpawns_IsNotAliveAndCannotBeHurt()
        {
            var brain = new BeekeeperBrain(StickConfig());
            var recorder = new Recorder(brain);

            brain.TakeDamage(1000f);

            Assert.That(brain.IsAlive, Is.False, "a pooled beekeeper is not on the field until it spawns");
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.Dead));
            Assert.That(recorder.Deaths, Is.EqualTo(0));
        }

        [Test]
        public void Spawn_PlacesItAtFullHealthGoingToTheHive()
        {
            var brain = SpawnAt(StickConfig(), new Vector3(3f, 0f, 4f));

            Assert.That(brain.Position, Is.EqualTo(new Vector3(3f, 0f, 4f)));
            Assert.That(brain.Health, Is.EqualTo(StickConfig().Health));
            Assert.That(brain.IsAlive, Is.True);
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive));
            Assert.That(brain.WindUpProgress, Is.EqualTo(0f));
        }

        [Test]
        public void Spawn_AfterDeathStunAndDamage_ResetsEverything()
        {
            var brain = SpawnAt(StickConfig(), Vector3.zero);
            var recorder = new Recorder(brain);
            brain.TakeDamage(1000f);
            brain.Stun(5f);

            brain.Spawn(new Vector3(1f, 0f, 1f));
            var hives = HivesAt(new Vector3(0f, 0f, 100f));
            brain.Tick(Senses(hives, CoreFar), Step);

            Assert.That(brain.IsAlive, Is.True);
            Assert.That(brain.Health, Is.EqualTo(StickConfig().Health));
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive));
            Assert.That(brain.IsMoving, Is.True);
            Assert.That(recorder.Deaths, Is.EqualTo(1));
        }

        [Test]
        public void Spawn_AfterBeingHit_IsNoLongerProvoked()
        {
            var brain = SpawnAt(StickConfig(), Vector3.zero);
            brain.TakeDamage(1f);

            brain.Spawn(Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, 100f));
            brain.Tick(Senses(hives, new Vector3(10f, 0f, 0f)), Step);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive));
            Assert.That(brain.IsProvoked, Is.False);
        }

        // ---------- Going to the hive ----------

        [Test]
        public void Tick_FarFromEverything_WalksTowardTheNearestHiveAtSpeed()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, 30f), new Vector3(0f, 0f, -90f));

            Advance(brain, Senses(hives, CoreFar), 1f);

            Assert.That(brain.Position.z, Is.EqualTo(config.Speed).Within(0.05f));
            Assert.That(Mathf.Abs(brain.Position.x), Is.LessThan(0.01f));
            Assert.That(brain.IsMoving, Is.True);
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive));
        }

        // Plan section T: the waves split between the hives by rule, so a beekeeper keeps to its own hive wherever it is.
        [Test]
        public void Tick_WithAHomeHive_WalksToItEvenWhenAnotherHiveIsNearer()
        {
            var config = StickConfig();
            var brain = new BeekeeperBrain(config);
            brain.Spawn(Vector3.zero, 1);
            var hives = HivesAt(new Vector3(0f, 0f, 30f), new Vector3(0f, 0f, -90f));

            Advance(brain, Senses(hives, CoreFar), 1f);

            Assert.That(brain.HomeHive, Is.EqualTo(1));
            Assert.That(brain.Position.z, Is.EqualTo(-config.Speed).Within(0.05f), "it walked to the nearer hive, not its own");
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive));
        }

        [Test]
        public void Tick_OnceItsHomeHiveFalls_WalksToTheNearestStandingHive()
        {
            var config = StickConfig();
            var brain = new BeekeeperBrain(config);
            brain.Spawn(Vector3.zero, 1);
            var hives = HivesAt(new Vector3(0f, 0f, 30f), new Vector3(0f, 0f, -90f));
            hives[1].Damage(float.MaxValue);

            Advance(brain, Senses(hives, CoreFar), 1f);

            Assert.That(brain.Position.z, Is.EqualTo(config.Speed).Within(0.05f));
        }

        [Test]
        public void Spawn_WithoutAHomeHive_HasNone()
        {
            var brain = SpawnAt(StickConfig(), Vector3.zero);

            Assert.That(brain.HomeHive, Is.EqualTo(-1));
            Assert.That(() => brain.Spawn(Vector3.zero, -2), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void Tick_Walking_FacesTheWayItGoesWithAUnitVector()
        {
            var brain = SpawnAt(StickConfig(), Vector3.zero);
            var hives = HivesAt(new Vector3(30f, 0f, 0f));

            Advance(brain, Senses(hives, CoreFar), 0.5f);

            Assert.That(brain.Facing.magnitude, Is.EqualTo(1f).Within(0.001f));
            Assert.That(brain.Facing.x, Is.GreaterThan(0.99f));
            Assert.That(brain.Facing.y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void Tick_NearestHiveByGroundDistance_IsTheOneItWalksTo()
        {
            var brain = SpawnAt(StickConfig(), Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, 40f), new Vector3(-20f, 0f, 0f));

            Advance(brain, Senses(hives, CoreFar), 1f);

            Assert.That(brain.Position.x, Is.LessThan(-2f));
        }

        [Test]
        public void Tick_NearestHiveFalls_RetargetsTheOtherHive()
        {
            var brain = SpawnAt(StickConfig(), Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, 30f), new Vector3(0f, 0f, -50f));
            Advance(brain, Senses(hives, CoreFar), 1f);
            float zBefore = brain.Position.z;

            hives[0].Damage(10000f);
            Advance(brain, Senses(hives, CoreFar), 1f);

            Assert.That(brain.Position.z, Is.LessThan(zBefore - 1f));
        }

        [Test]
        public void Tick_NoHiveStanding_StaysPutAndThrowsNothing()
        {
            var brain = SpawnAt(SmokerConfig(), Vector3.zero);
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, 10f));
            hives[0].Damage(10000f);

            Advance(brain, Senses(hives, CoreFar), 3f);

            Assert.That(brain.Position, Is.EqualTo(Vector3.zero));
            Assert.That(brain.IsMoving, Is.False);
            Assert.That(recorder.Grenades, Is.Empty);
            Assert.That(recorder.HiveHits, Is.Empty);
        }

        [Test]
        public void Tick_SeparationPush_ShiftsItSideways()
        {
            var brain = SpawnAt(StickConfig(), Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));
            var pushed = new BeekeeperSenses(CoreFar, hives, new Vector3(1f, 0f, 0f));

            Advance(brain, pushed, 1f);

            Assert.That(brain.Position.x, Is.GreaterThan(0.1f));
        }

        // ---------- Attacking a hive (stick) ----------

        [Test]
        public void Tick_ReachesAttackRangeOfAHive_StopsAndStartsTheAttack()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, 10f));
            var senses = Senses(hives, CoreFar);

            bool started = AdvanceUntil(
                brain, in senses, () => brain.State == BeekeeperState.WindUp, 10f);

            Assert.That(started, Is.True);
            Assert.That(GroundDistance(brain.Position, hives[0].Position), Is.LessThanOrEqualTo(config.AttackRange + 0.2f));
            Assert.That(brain.IsMoving, Is.False);
        }

        [Test]
        public void Tick_StickWindUp_HitsTheHiveOnlyAfterTheWindUp()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, new Vector3(0f, 0f, 9f));
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, 10f));
            var senses = Senses(hives, CoreFar);
            AdvanceUntil(brain, in senses, () => brain.State == BeekeeperState.WindUp, 1f);

            Advance(brain, senses, config.WindUpSeconds - 0.15f);
            Assert.That(recorder.HiveHits, Is.Empty);

            Advance(brain, senses, 0.3f);
            Assert.That(recorder.HiveHits, Is.EqualTo(new[] { 0 }));
            Assert.That(recorder.LastSender, Is.SameAs(brain));
        }

        [Test]
        public void Tick_StickHitsTheSecondHive_ReportsItsIndex()
        {
            var brain = SpawnAt(StickConfig(), new Vector3(0f, 0f, -9f));
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, 50f), new Vector3(0f, 0f, -10f));

            Advance(brain, Senses(hives, CoreFar), 2f);

            Assert.That(recorder.HiveHits, Is.Not.Empty);
            Assert.That(recorder.HiveHits[0], Is.EqualTo(1));
        }

        [Test]
        public void Tick_WindUp_ReportsProgressFromZeroToOne()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, new Vector3(0f, 0f, 9f));
            var hives = HivesAt(new Vector3(0f, 0f, 10f));
            var senses = Senses(hives, CoreFar);
            AdvanceUntil(brain, in senses, () => brain.State == BeekeeperState.WindUp, 1f);
            float atStart = brain.WindUpProgress;

            Advance(brain, senses, config.WindUpSeconds * 0.5f);

            Assert.That(atStart, Is.LessThan(0.3f));
            Assert.That(brain.WindUpProgress, Is.EqualTo(0.5f).Within(0.15f));
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.WindUp));
        }

        [Test]
        public void IsTargetingHive_SwingAtAHive_IsTrueThroughWindUpAndRecover()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, new Vector3(0f, 0f, 9f));
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, 10f));
            var senses = Senses(hives, CoreFar);
            Assert.That(brain.IsTargetingHive, Is.False);

            AdvanceUntil(brain, in senses, () => brain.State == BeekeeperState.WindUp, 1f);
            Assert.That(brain.IsTargetingHive, Is.True);

            AdvanceUntil(brain, in senses, () => recorder.HiveHits.Count == 1, 3f);
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.Recover));
            Assert.That(brain.IsTargetingHive, Is.True);
        }

        [Test]
        public void IsTargetingHive_SwingAtTheCore_IsFalse()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, new Vector3(0f, 0f, 0f));
            var hives = HivesAt(new Vector3(0f, 0f, 100f));
            var senses = Senses(hives, new Vector3(0f, 0f, 1f));

            AdvanceUntil(brain, in senses, () => brain.State == BeekeeperState.WindUp, 1f);

            Assert.That(brain.IsTargetingHive, Is.False);
        }

        [Test]
        public void Tick_AfterTheHit_RecoversAndShowsNoWindUpProgress()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, new Vector3(0f, 0f, 9f));
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, 10f));
            var senses = Senses(hives, CoreFar);

            AdvanceUntil(brain, in senses, () => recorder.HiveHits.Count == 1, 3f);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.Recover));
            Assert.That(brain.WindUpProgress, Is.EqualTo(0f));
            Assert.That(brain.IsMoving, Is.False);
        }

        [Test]
        public void Tick_StickCooldown_BlocksTheNextHitUntilCooldownAndWindUpPass()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, new Vector3(0f, 0f, 9f));
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, 10f));
            var senses = Senses(hives, CoreFar);
            AdvanceUntil(brain, in senses, () => recorder.HiveHits.Count == 1, 3f);

            Advance(brain, senses, config.CooldownSeconds + config.WindUpSeconds - 0.25f);
            Assert.That(recorder.HiveHits.Count, Is.EqualTo(1));

            Advance(brain, senses, 0.5f);
            Assert.That(recorder.HiveHits.Count, Is.EqualTo(2));
        }

        [Test]
        public void Tick_StickAttackingHive_NeverThrowsGrenades()
        {
            var brain = SpawnAt(StickConfig(), new Vector3(0f, 0f, 9f));
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, 10f));

            Advance(brain, Senses(hives, CoreFar), 5f);

            Assert.That(recorder.Grenades, Is.Empty);
        }

        [Test]
        public void Tick_HiveFallsWhileAttacking_GoesToTheOtherHiveWithoutHittingTheDeadOne()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, new Vector3(0f, 0f, 9f));
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, -60f));
            var senses = Senses(hives, CoreFar);
            AdvanceUntil(brain, in senses, () => brain.State == BeekeeperState.WindUp, 1f);

            hives[0].Damage(10000f);
            Advance(brain, senses, 1f);

            Assert.That(recorder.HiveHits, Is.Empty);
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive));
            Assert.That(brain.IsMoving, Is.True);
            Assert.That(brain.Position.z, Is.LessThan(9f));
        }

        // ---------- Sticks go for the hives (plan section O) ----------

        // The owner: "if I camp the hive they try to hit me instead of the hive".
        [Test]
        public void Tick_StickAtItsHiveWithTheCoreBesideIt_HitsTheHiveAndNeverTheCore()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, new Vector3(0f, 0f, 9f));
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, 10f));
            var senses = Senses(hives, new Vector3(0.5f, 0f, 9.5f));

            Advance(brain, senses, (config.WindUpSeconds * 3f) + (config.CooldownSeconds * 2f) + 0.3f);

            Assert.That(recorder.HiveHits.Count, Is.EqualTo(3), "a hit at about 0.6 s, 2.6 s and 4.6 s");
            Assert.That(recorder.CoreHits, Is.EqualTo(0));
            Assert.That(brain.IsTargetingHive, Is.True);
        }

        [Test]
        public void Tick_StickWithTheCoreInsideItsNoticeRange_WalksOnToItsHive()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, 30f));

            Advance(brain, Senses(hives, new Vector3(1f, 0f, 0.5f)), 1f);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive));
            Assert.That(brain.Position.z, Is.EqualTo(config.Speed).Within(0.05f));
            Assert.That(brain.IsProvoked, Is.False);
        }

        [Test]
        public void TakeDamage_Stick_ChasesTheCoreForExactlyProvokedSecondsThenGoesBackToItsHive()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, -FarAway));
            var senses = Senses(hives, new Vector3(FarAway, 0f, 0f));

            brain.TakeDamage(1f);
            Assert.That(brain.IsProvoked, Is.True);
            Advance(brain, senses, config.ProvokedSeconds - 0.1f);
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.ChaseCore), "far beyond its give-up range, it still chases");
            Assert.That(brain.Position.x, Is.GreaterThan(9f));

            Advance(brain, senses, 0.2f);
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive));
            Assert.That(brain.IsProvoked, Is.False);
            Assert.That(brain.Facing.z, Is.LessThan(-0.99f), "it turned back toward its hive");
        }

        [Test]
        public void TakeDamage_AgainWhileProvoked_StartsTheTimeAgain()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, -FarAway));
            var senses = Senses(hives, new Vector3(FarAway, 0f, 0f));
            brain.TakeDamage(1f);
            Advance(brain, senses, config.ProvokedSeconds - 1f);

            brain.TakeDamage(1f);
            Advance(brain, senses, config.ProvokedSeconds - 0.1f);
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.ChaseCore), "the second hurt started the time again");

            Advance(brain, senses, 0.2f);
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive));
        }

        [Test]
        public void TakeDamage_ProvocationEndsDuringASwingAtTheCore_DropsTheSwingAndGoesBackToItsHive()
        {
            var config = StickConfig();
            JsonUtility.FromJsonOverwrite("{\"<ProvokedSeconds>k__BackingField\":0.3}", config);
            var brain = SpawnAt(config, Vector3.zero);
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));
            var senses = Senses(hives, new Vector3(1f, 0f, 0f));
            brain.TakeDamage(1f);
            brain.Tick(senses, Step);
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.WindUp));

            Advance(brain, senses, config.WindUpSeconds + 1f);

            Assert.That(recorder.CoreHits, Is.EqualTo(0));
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive));
        }

        [Test]
        public void Tick_ProvokedStickChasing_WalksTowardTheCoreAtSpeed()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, -FarAway));
            var core = new Vector3(6f, 0f, 0f);
            brain.TakeDamage(1f);

            Advance(brain, Senses(hives, core), 1f);

            Assert.That(brain.Position.x, Is.EqualTo(config.Speed).Within(0.1f));
            Assert.That(brain.IsMoving, Is.True);
            Assert.That(brain.Facing.x, Is.GreaterThan(0.95f));
        }

        [Test]
        public void Tick_ProvokedStickLosesItsHive_StillChasesTheCore()
        {
            var brain = SpawnAt(StickConfig(), Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, 30f));
            var senses = Senses(hives, new Vector3(5f, 0f, 0f));
            brain.TakeDamage(1f);
            Advance(brain, senses, 0.1f);

            hives[0].Damage(10000f);
            Advance(brain, senses, 0.1f);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.ChaseCore).Or.EqualTo(BeekeeperState.WindUp));
        }

        // ---------- Smokers hunt the swarm by their ranges (plan section O) ----------

        [Test]
        public void Tick_SmokerThatNoticedTheCore_ThrowsAtTheCore_OneThatDidNot_ThrowsAtItsHive()
        {
            var config = SmokerConfig();
            var hunting = SpawnAt(config, Vector3.zero);
            var walking = SpawnAt(config, Vector3.zero);
            var hunted = new Recorder(hunting);
            var walked = new Recorder(walking);
            var hives = HivesAt(new Vector3(0f, 0f, 20f));
            var core = new Vector3(config.NoticeRange - 1f, 0f, 0f);
            var faraway = new Vector3(-config.GiveUpRange - 10f, 0f, 0f);

            AdvanceUntil(hunting, Senses(hives, core), () => hunted.Grenades.Count > 0, 5f);
            AdvanceUntil(walking, Senses(hives, faraway), () => walked.Grenades.Count > 0, 10f);

            Assert.That(GroundDistance(hunted.Grenades[0], core), Is.LessThan(0.5f), "in its notice range: at the core");
            Assert.That(GroundDistance(walked.Grenades[0], hives[0].Position), Is.LessThan(0.5f), "otherwise at a hive");
        }

        [Test]
        public void Tick_HunterCoreJustInsideNoticeRange_StartsChasing()
        {
            var config = HunterConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));

            brain.Tick(Senses(hives, new Vector3(config.NoticeRange - Margin, 0f, 0f)), TinyStep);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.ChaseCore));
        }

        [Test]
        public void Tick_HunterCoreExactlyAtNoticeRange_StartsChasing()
        {
            var config = HunterConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));

            brain.Tick(Senses(hives, new Vector3(config.NoticeRange, 0f, 0f)), TinyStep);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.ChaseCore));
        }

        [Test]
        public void Tick_HunterCoreJustOutsideNoticeRange_KeepsGoingToTheHive()
        {
            var config = HunterConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));

            brain.Tick(Senses(hives, new Vector3(config.NoticeRange + Margin, 0f, 0f)), TinyStep);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive));
        }

        [Test]
        public void Tick_HunterIgnoresTheCoresHeightWhenNoticing()
        {
            var config = HunterConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));

            brain.Tick(Senses(hives, new Vector3(config.NoticeRange - 1f, 30f, 0f)), TinyStep);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.ChaseCore));
        }

        [Test]
        public void Tick_HunterChasingBetweenNoticeAndGiveUpRange_KeepsChasing()
        {
            var config = HunterConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));
            brain.Tick(Senses(hives, new Vector3(config.NoticeRange - 1f, 0f, 0f)), TinyStep);

            brain.Tick(Senses(hives, new Vector3(config.NoticeRange + 3f, 0f, 0f)), TinyStep);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.ChaseCore));
        }

        [Test]
        public void Tick_HunterCoreBeyondGiveUpRange_GoesBackToTheHive()
        {
            var config = HunterConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));
            brain.Tick(Senses(hives, new Vector3(config.NoticeRange - 1f, 0f, 0f)), TinyStep);

            brain.Tick(Senses(hives, new Vector3(config.GiveUpRange + Margin, 0f, 0f)), TinyStep);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive));
        }

        [Test]
        public void Tick_HunterCoreJustInsideGiveUpRange_KeepsChasing()
        {
            var config = HunterConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));
            brain.Tick(Senses(hives, new Vector3(config.NoticeRange - 1f, 0f, 0f)), TinyStep);

            brain.Tick(Senses(hives, new Vector3(config.GiveUpRange - Margin, 0f, 0f)), TinyStep);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.ChaseCore));
        }

        [Test]
        public void Tick_HunterGaveUpAndCoreStaysBetweenRanges_DoesNotChaseAgain()
        {
            var config = HunterConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));
            brain.Tick(Senses(hives, new Vector3(config.NoticeRange - 1f, 0f, 0f)), TinyStep);
            brain.Tick(Senses(hives, new Vector3(config.GiveUpRange + 1f, 0f, 0f)), TinyStep);

            brain.Tick(Senses(hives, new Vector3(config.NoticeRange + 3f, 0f, 0f)), TinyStep);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive));
        }

        // ---------- The collapsed swarm cannot be seen (plan section O) ----------

        [Test]
        public void Tick_CoreUnseen_ASmokerInRangeThrowsAtItsHiveInstead()
        {
            var config = SmokerConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, 10f));
            var core = new Vector3(3f, 0f, 0f);

            AdvanceUntil(brain, Senses(hives, core, false), () => recorder.Grenades.Count > 0, 5f);

            Assert.That(recorder.Grenades.Count, Is.EqualTo(1));
            Assert.That(GroundDistance(recorder.Grenades[0], hives[0].Position), Is.LessThan(0.5f));
        }

        [Test]
        public void Tick_CoreUnseen_AProvokedStickStopsChasingAndWalksToItsHive()
        {
            var brain = SpawnAt(StickConfig(), Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, 30f));
            brain.TakeDamage(1f);
            brain.Tick(Senses(hives, new Vector3(6f, 0f, 0f)), Step);
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.ChaseCore));

            brain.Tick(Senses(hives, new Vector3(6f, 0f, 0f), false), Step);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive));
            Assert.That(brain.IsProvoked, Is.False);
        }

        [Test]
        public void Tick_CoreUnseenDuringASwingAtIt_DropsTheSwingAndNoHitLands()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));
            var core = new Vector3(1f, 0f, 0f);
            brain.TakeDamage(1f);
            brain.Tick(Senses(hives, core), Step);
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.WindUp));

            Advance(brain, Senses(hives, core, false), config.WindUpSeconds + 0.5f);

            Assert.That(recorder.CoreHits, Is.EqualTo(0));
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive));
        }

        [Test]
        public void Tick_CoreUnseenDuringASmokersThrowAtIt_ThrowsNothingAtTheCore()
        {
            var config = SmokerConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));
            var core = new Vector3(5f, 0f, 0f);
            brain.Tick(Senses(hives, core), Step);
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.WindUp));

            Advance(brain, Senses(hives, core, false), config.WindUpSeconds + 0.5f);

            Assert.That(recorder.Grenades, Is.Empty);
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive));
        }

        [Test]
        public void Tick_CoreUnseenWhileRecovering_NeverTurnsBackOnTheCore()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));
            var core = new Vector3(1f, 0f, 0f);
            brain.TakeDamage(1f);
            AdvanceUntil(brain, Senses(hives, core), () => recorder.CoreHits == 1, 2f);
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.Recover));

            Advance(brain, Senses(hives, core, false), config.CooldownSeconds + config.WindUpSeconds + 0.5f);

            Assert.That(recorder.CoreHits, Is.EqualTo(1));
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive));
        }

        [Test]
        public void Tick_CoreUnseenWhileStunned_TheStickForgetsItsProvocation()
        {
            var brain = SpawnAt(StickConfig(), Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, 30f));
            var core = new Vector3(6f, 0f, 0f);
            brain.TakeDamage(1f);
            brain.Stun(0.5f);

            Advance(brain, Senses(hives, core, false), 0.3f);
            Advance(brain, Senses(hives, core), 0.5f);

            Assert.That(brain.IsProvoked, Is.False);
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive));
        }

        [Test]
        public void Tick_CoreSeenAgainAfterTheRespawn_ASmokerInRangeThrowsAtItAgain()
        {
            var config = SmokerConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));
            var core = new Vector3(4f, 0f, 0f);
            Advance(brain, Senses(hives, core, false), 1f);
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive));

            AdvanceUntil(brain, Senses(hives, core), () => recorder.Grenades.Count > 0, 2f);

            Assert.That(recorder.Grenades.Count, Is.EqualTo(1));
            Assert.That(GroundDistance(recorder.Grenades[0], core), Is.LessThan(0.5f));
        }

        [Test]
        public void TakeDamage_WhileTheCoreIsUnseen_TheStickStaysOnItsWayToTheHive()
        {
            var brain = SpawnAt(StickConfig(), Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, 30f));
            var core = new Vector3(6f, 0f, 0f);

            brain.TakeDamage(1f);
            brain.Tick(Senses(hives, core, false), Step);
            Advance(brain, Senses(hives, core), 0.5f);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive), "a hurt while the swarm is gone is forgotten by its return");
        }

        // ---------- Hitting the core (a provoked stick) ----------

        [Test]
        public void Tick_ProvokedStickWithTheCoreInReach_WindsUpThenHitsOnlyAfterTheWindUp()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));
            var senses = Senses(hives, new Vector3(config.AttackRange - 0.3f, 0f, 0f));
            brain.TakeDamage(1f);
            AdvanceUntil(brain, in senses, () => brain.State == BeekeeperState.WindUp, 1f);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.WindUp));
            Advance(brain, senses, config.WindUpSeconds - 0.15f);
            Assert.That(recorder.CoreHits, Is.EqualTo(0));

            Advance(brain, senses, 0.3f);
            Assert.That(recorder.CoreHits, Is.EqualTo(1));
            Assert.That(recorder.HiveHits, Is.Empty);
            Assert.That(recorder.LastSender, Is.SameAs(brain));
        }

        [Test]
        public void Tick_ProvokedStickChasingBeyondAttackRange_DoesNotWindUpYet()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));
            brain.TakeDamage(1f);

            brain.Tick(Senses(hives, new Vector3(config.AttackRange + 1f, 0f, 0f)), TinyStep);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.ChaseCore));
            Assert.That(brain.WindUpProgress, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_ProvokedStickCooldownOnTheCore_BlocksTheNextHitUntilCooldownAndWindUpPass()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));
            var senses = Senses(hives, new Vector3(1f, 0f, 0f));
            brain.TakeDamage(1f);
            AdvanceUntil(brain, in senses, () => recorder.CoreHits == 1, 3f);

            Advance(brain, senses, config.CooldownSeconds + config.WindUpSeconds - 0.25f);
            Assert.That(recorder.CoreHits, Is.EqualTo(1));

            Advance(brain, senses, 0.5f);
            Assert.That(recorder.CoreHits, Is.EqualTo(2));
        }

        // ---------- Damage, aggro and death ----------

        [Test]
        public void TakeDamage_Positive_LowersHealth()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, Vector3.zero);

            brain.TakeDamage(15f);

            Assert.That(brain.Health, Is.EqualTo(config.Health - 15f).Within(0.001f));
            Assert.That(brain.IsAlive, Is.True);
        }

        [Test]
        public void TakeDamage_ExactlyHealth_KillsItAndRaisesDiedOnce()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var recorder = new Recorder(brain);

            brain.TakeDamage(config.Health);

            Assert.That(brain.Health, Is.EqualTo(0f));
            Assert.That(brain.IsAlive, Is.False);
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.Dead));
            Assert.That(recorder.Deaths, Is.EqualTo(1));
            Assert.That(recorder.LastSender, Is.SameAs(brain));
        }

        [Test]
        public void TakeDamage_MoreThanHealth_ClampsHealthToZero()
        {
            var brain = SpawnAt(StickConfig(), Vector3.zero);

            brain.TakeDamage(10000f);

            Assert.That(brain.Health, Is.EqualTo(0f));
        }

        [Test]
        public void TakeDamage_JustBelowHealth_LeavesItAlive()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var recorder = new Recorder(brain);

            brain.TakeDamage(config.Health - 0.5f);

            Assert.That(brain.IsAlive, Is.True);
            Assert.That(recorder.Deaths, Is.EqualTo(0));
        }

        [Test]
        public void TakeDamage_WhenAlreadyDead_DoesNotRaiseDiedAgain()
        {
            var brain = SpawnAt(StickConfig(), Vector3.zero);
            var recorder = new Recorder(brain);
            brain.TakeDamage(10000f);

            brain.TakeDamage(10f);

            Assert.That(recorder.Deaths, Is.EqualTo(1));
            Assert.That(brain.Health, Is.EqualTo(0f));
        }

        [Test]
        public void TakeDamage_CoreBeyondNoticeRange_MakesItChaseTheCoreAnyway()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, -FarAway));
            var core = new Vector3(config.NoticeRange + 3f, 0f, 0f);

            brain.TakeDamage(1f);
            Advance(brain, Senses(hives, core), 1f);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.ChaseCore));
            Assert.That(brain.Position.x, Is.GreaterThan(2f));
        }

        [Test]
        public void TakeDamage_HunterThenCoreBeyondGiveUpRange_GivesUpTheChase()
        {
            var config = HunterConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));
            brain.TakeDamage(1f);
            brain.Tick(Senses(hives, new Vector3(config.NoticeRange + 3f, 0f, 0f)), TinyStep);

            brain.Tick(Senses(hives, new Vector3(config.GiveUpRange + Margin, 0f, 0f)), TinyStep);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive));
        }

        [Test]
        public void Tick_Dead_DoesNotMoveOrRaiseEvents()
        {
            var brain = SpawnAt(StickConfig(), new Vector3(0f, 0f, 9f));
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, 10f));
            brain.TakeDamage(10000f);

            Advance(brain, Senses(hives, new Vector3(1f, 0f, 9f)), 5f);

            Assert.That(brain.Position, Is.EqualTo(new Vector3(0f, 0f, 9f)));
            Assert.That(brain.IsMoving, Is.False);
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.Dead));
            Assert.That(recorder.HiveHits, Is.Empty);
            Assert.That(recorder.CoreHits, Is.EqualTo(0));
        }

        [Test]
        public void TakeDamage_DuringWindUp_DoesNotCancelTheHit()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, new Vector3(0f, 0f, 9f));
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, 10f));
            var senses = Senses(hives, new Vector3(100f, 0f, 9f));
            AdvanceUntil(brain, in senses, () => brain.State == BeekeeperState.WindUp, 1f);

            brain.TakeDamage(1f);
            Advance(brain, senses, config.WindUpSeconds + 0.2f);

            Assert.That(brain.IsAlive, Is.True);
            Assert.That(recorder.HiveHits.Count, Is.EqualTo(1));
        }

        [Test]
        public void TakeDamage_KillingDuringWindUp_MeansNoHitLands()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, new Vector3(0f, 0f, 9f));
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, 10f));
            var senses = Senses(hives, CoreFar);
            AdvanceUntil(brain, in senses, () => brain.State == BeekeeperState.WindUp, 1f);

            brain.TakeDamage(10000f);
            Advance(brain, senses, config.WindUpSeconds + 0.5f);

            Assert.That(recorder.HiveHits, Is.Empty);
            Assert.That(brain.WindUpProgress, Is.EqualTo(0f));
        }

        // ---------- Stun ----------

        [Test]
        public void Stun_WhileWalking_FreezesItForTheSecondsThenItWalksAgain()
        {
            var brain = SpawnAt(StickConfig(), Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, 100f));
            var senses = Senses(hives, CoreFar);

            brain.Stun(1f);
            brain.Tick(senses, Step);
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.Stunned));
            Assert.That(brain.IsMoving, Is.False);

            Advance(brain, senses, 0.8f);
            Assert.That(brain.Position, Is.EqualTo(Vector3.zero));
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.Stunned));

            Advance(brain, senses, 0.4f);
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.GoToHive));
            Assert.That(brain.Position.z, Is.GreaterThan(0f));
        }

        [Test]
        public void Stun_ShorterStunWhileStunned_DoesNotShortenTheLongerOne()
        {
            var brain = SpawnAt(StickConfig(), Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, 100f));
            var senses = Senses(hives, CoreFar);

            brain.Stun(1f);
            brain.Stun(0.2f);
            Advance(brain, senses, 0.8f);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.Stunned));
        }

        [Test]
        public void Stun_LongerStunWhileStunned_ExtendsIt()
        {
            var brain = SpawnAt(StickConfig(), Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, 100f));
            var senses = Senses(hives, CoreFar);

            brain.Stun(0.2f);
            brain.Stun(1f);
            Advance(brain, senses, 0.8f);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.Stunned));
        }

        [Test]
        public void Stun_WhileChasing_ResumesTheChaseAfterwards()
        {
            var brain = SpawnAt(StickConfig(), Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, -FarAway));
            var senses = Senses(hives, new Vector3(6f, 0f, 0f));
            brain.TakeDamage(1f);
            Advance(brain, senses, 0.1f);

            brain.Stun(0.5f);
            Advance(brain, senses, 0.8f);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.ChaseCore).Or.EqualTo(BeekeeperState.WindUp));
        }

        [Test]
        public void Stun_DuringWindUp_DelaysTheHiveHit()
        {
            var config = StickConfig();
            var brain = SpawnAt(config, new Vector3(0f, 0f, 9f));
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, 10f));
            var senses = Senses(hives, CoreFar);
            AdvanceUntil(brain, in senses, () => brain.State == BeekeeperState.WindUp, 1f);

            brain.Stun(1f);
            Advance(brain, senses, 0.9f);
            Assert.That(recorder.HiveHits, Is.Empty);
            Assert.That(brain.State, Is.EqualTo(BeekeeperState.Stunned));

            Advance(brain, senses, 1f + config.WindUpSeconds);
            Assert.That(recorder.HiveHits.Count, Is.EqualTo(1));
        }

        [Test]
        public void Stun_WhenDead_StaysDead()
        {
            var brain = SpawnAt(StickConfig(), Vector3.zero);
            brain.TakeDamage(10000f);

            brain.Stun(2f);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.Dead));
        }

        // ---------- Knockback ----------

        [Test]
        public void Knockback_PushesItThenDecaysToNothing()
        {
            var config = StickConfig();
            var pushed = SpawnAt(config, Vector3.zero);
            var steady = SpawnAt(config, Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));
            var senses = Senses(hives, CoreFar);

            pushed.Knockback(new Vector3(8f, 0f, 0f));
            Advance(pushed, senses, 0.1f);
            Advance(steady, senses, 0.1f);
            Assert.That(pushed.Position.x - steady.Position.x, Is.GreaterThan(0.1f));

            Advance(pushed, senses, 2f);
            Advance(steady, senses, 2f);
            float afterTwoSeconds = pushed.Position.x - steady.Position.x;
            Advance(pushed, senses, 1f);
            Advance(steady, senses, 1f);
            float afterThree = pushed.Position.x - steady.Position.x;

            Assert.That(afterThree - afterTwoSeconds, Is.LessThan(0.05f));
            Assert.That(afterThree, Is.GreaterThan(0.1f));
        }

        [Test]
        public void Knockback_WhenDead_DoesNotMoveIt()
        {
            var brain = SpawnAt(StickConfig(), Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));
            brain.TakeDamage(10000f);

            brain.Knockback(new Vector3(8f, 0f, 0f));
            Advance(brain, Senses(hives, CoreFar), 1f);

            Assert.That(brain.Position, Is.EqualTo(Vector3.zero));
        }

        // ---------- Smoker ----------

        [Test]
        public void Tick_SmokerWithCoreInRange_WindsUpWithoutWalkingCloser()
        {
            var config = SmokerConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));
            var senses = Senses(hives, new Vector3(6f, 0f, 0f));

            AdvanceUntil(brain, in senses, () => brain.State == BeekeeperState.WindUp, 1f);

            Assert.That(brain.State, Is.EqualTo(BeekeeperState.WindUp));
            Assert.That(GroundDistance(brain.Position, new Vector3(6f, 0f, 0f)), Is.GreaterThan(4f));
        }

        [Test]
        public void Tick_SmokerThrowsAfterItsWindUpAtTheCore()
        {
            var config = SmokerConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));
            var core = new Vector3(6f, 0f, 0f);
            var senses = Senses(hives, core);
            AdvanceUntil(brain, in senses, () => brain.State == BeekeeperState.WindUp, 1f);

            Advance(brain, senses, config.WindUpSeconds - 0.15f);
            Assert.That(recorder.Grenades, Is.Empty);

            Advance(brain, senses, 0.3f);
            Assert.That(recorder.Grenades.Count, Is.EqualTo(1));
            Assert.That(GroundDistance(recorder.Grenades[0], core), Is.LessThan(0.5f));
            Assert.That(recorder.CoreHits, Is.EqualTo(0));
            Assert.That(recorder.HiveHits, Is.Empty);
        }

        [Test]
        public void Tick_SmokerHitAtFourteenMetres_ThrowsWithoutApproaching()
        {
            var config = SmokerConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));
            var core = new Vector3(config.AttackRange - 0.5f, 0f, 0f);
            var senses = Senses(hives, core);
            brain.TakeDamage(1f);

            Advance(brain, senses, config.WindUpSeconds + 0.3f);

            Assert.That(recorder.Grenades.Count, Is.EqualTo(1));
            Assert.That(brain.Position.magnitude, Is.LessThan(1f));
        }

        [Test]
        public void Tick_SmokerGoingToAHive_StopsInRangeAndThrowsAtTheHive()
        {
            var config = SmokerConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, 30f));
            var senses = Senses(hives, CoreFar);

            bool threw = AdvanceUntil(brain, in senses, () => recorder.Grenades.Count > 0, 30f);

            Assert.That(threw, Is.True);
            Assert.That(GroundDistance(recorder.Grenades[0], hives[0].Position), Is.LessThan(0.5f));
            float distance = GroundDistance(brain.Position, hives[0].Position);
            Assert.That(distance, Is.LessThanOrEqualTo(config.AttackRange + 0.5f));
            Assert.That(distance, Is.GreaterThan(config.AttackRange - 4f));
            Assert.That(recorder.HiveHits, Is.Empty);
        }

        [Test]
        public void Tick_SmokerCooldown_ThrowsAgainOnlyAfterCooldownAndWindUp()
        {
            var config = SmokerConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var recorder = new Recorder(brain);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));
            var senses = Senses(hives, new Vector3(6f, 0f, 0f));
            AdvanceUntil(brain, in senses, () => recorder.Grenades.Count == 1, 3f);

            Advance(brain, senses, config.CooldownSeconds + config.WindUpSeconds - 0.3f);
            Assert.That(recorder.Grenades.Count, Is.EqualTo(1));

            Advance(brain, senses, 0.6f);
            Assert.That(recorder.Grenades.Count, Is.EqualTo(2));
        }

        [Test]
        public void Tick_SmokerWalksSlowerThanAStick()
        {
            var config = SmokerConfig();
            var brain = SpawnAt(config, Vector3.zero);
            var hives = HivesAt(new Vector3(0f, 0f, FarAway));

            Advance(brain, Senses(hives, CoreFar), 1f);

            Assert.That(brain.Position.z, Is.EqualTo(config.Speed).Within(0.05f));
            Assert.That(brain.Config, Is.SameAs(config));
        }

        [Test]
        public void Spawn_SmokerHealth_IsTheSmokerValue()
        {
            var brain = SpawnAt(SmokerConfig(), Vector3.zero);

            Assert.That(brain.Health, Is.EqualTo(40f));
        }
    }
}
