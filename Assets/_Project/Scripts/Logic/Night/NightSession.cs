using System;
using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>Runs the night, from the first rat to the final outcome.</summary>
    public sealed class NightSession
    {
        private readonly GameRules _rules;

        /// <param name="rules">The configs a night runs on.</param>
        /// <param name="night">The numbers for this night.</param>
        /// <param name="townRandom">Builds the town, so it should be seeded from the night.</param>
        /// <param name="simulationRandom">Drives the rats while the night runs.</param>
        public NightSession(GameRules rules, NightRules night, IRandomSource townRandom, IRandomSource simulationRandom)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));

            Quota = night.Quota;
            TimeLimit = night.TimeLimit;
            TimeLeft = night.TimeLimit;

            Town = new TownGenerator(rules.Town, townRandom).Generate();
            Bank = new RiverBank(rules.Town);

            var houses = new HouseCollider(Town.Houses);
            Rat[] rats = new RatSpawner(rules, houses, townRandom).Spawn(night, Town, rules.PiperStart);

            Swarm = new RatSwarm(rats, rules, Bank, houses, simulationRandom);
            Piper = new Piper(rules.Piper, rules.Town, Bank, houses, rules.PiperStart);
            Breath = new Breath(rules.Breath);
            Flute = new Flute(rules.Flute);
        }

        public event Action<NightOutcome> Ended;

        public TownLayout Town { get; }
        public RiverBank Bank { get; }
        public Piper Piper { get; }
        public Breath Breath { get; }
        public Flute Flute { get; }
        public RatSwarm Swarm { get; }
        public int Quota { get; }
        public float TimeLimit { get; }
        public float TimeLeft { get; private set; }
        public NightOutcome Outcome { get; private set; } = NightOutcome.InProgress;

        /// <summary>Advances the night by one step. Does nothing once it has ended.</summary>
        public void Tick(in PiperIntent intent, float deltaTime)
        {
            if (Outcome != NightOutcome.InProgress)
            {
                return;
            }

            TimeLeft -= deltaTime;
            Piper.Tick(intent.Move, deltaTime);
            PlayFlute(in intent, deltaTime);
            Swarm.Tick(new SwarmInfluence(Piper.Position, Piper.Facing, Flute.Mode, Flute.MusicRadius), deltaTime);
            CheckOutcome();
        }

        private void PlayFlute(in PiperIntent intent, float deltaTime)
        {
            RememberPressedHoles(intent.PressedHoles);

            int wantedHole = Flute.ChooseHole(intent.HeldHoles);
            bool isPlaying = Breath.Tick(wantedHole != Flute.NoHole, deltaTime);
            Flute.Sound(isPlaying ? wantedHole : Flute.NoHole);
        }

        private void RememberPressedHoles(int pressedHoles)
        {
            for (int hole = 0; hole < _rules.Flute.HoleCount; hole++)
            {
                if ((pressedHoles & (1 << hole)) != 0)
                {
                    Flute.Press(hole);
                }
            }
        }

        private void CheckOutcome()
        {
            Outcome = NightOutcomeRule.Resolve(Swarm.DrownedCount, Swarm.AliveCount, Quota, TimeLeft);

            if (Outcome != NightOutcome.InProgress)
            {
                Ended?.Invoke(Outcome);
            }
        }
    }
}
