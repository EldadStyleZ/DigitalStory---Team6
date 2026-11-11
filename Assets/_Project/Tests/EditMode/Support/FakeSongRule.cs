using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// The rule of a <see cref="FakeSlotSong"/>: it only records what the slot engine did to it, lets go when its song says,
    /// and changes the match by the modifiers its song sets.
    /// </summary>
    internal sealed class FakeSongRule : SongRule
    {
        private readonly FakeSlotSong _song;

        public FakeSongRule(FakeSlotSong song)
            : base(song)
        {
            _song = song;
        }

        public int StartCount { get; private set; }

        public int TickCount { get; private set; }

        public int HitCount { get; private set; }

        public int HiveHitCount { get; private set; }

        public int LastHiveHit { get; private set; } = -1;

        public ISwarmTarget LastHiveAttacker { get; private set; }

        public override Vector4 ParamA => new Vector4(1f, 2f, 3f, 4f);

        public override Vector4 ParamB => new Vector4(5f, 6f, 7f, 0f);

        protected override float WaspLossFraction => _song.WaspLossFraction;

        protected override float CoreSpeedMultiplier => _song.CoreSpeedMultiplier;

        protected override bool SmokeSlowsCore => _song.SmokeSlowsCore;

        protected override float HiveDamageFraction(int hive)
        {
            return hive == _song.ShieldedHive ? _song.HiveDamageFraction : 1f;
        }

        protected override void NotifyHiveHit(int hive, ISwarmTarget attacker, float seconds)
        {
            HiveHitCount++;
            LastHiveHit = hive;
            LastHiveAttacker = attacker;
        }

        protected override void Begin(in SwarmSongContext context)
        {
            StartCount++;
        }

        protected override void Tick(in SongTick tick)
        {
            TickCount++;

            if (tick.Seconds >= _song.ReleaseAfter)
            {
                Release(_song.ReleaseAfter);
            }
        }

        protected override void NotifyCoreHit(Vector3 direction, float seconds)
        {
            HitCount++;
        }
    }
}
