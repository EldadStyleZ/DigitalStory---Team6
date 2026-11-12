using System;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The Hydra: as it is cast, each head chooses one of the beekeepers nearest the core within its reach by the ThreeNearest
    /// aim rule, nearest first: 3 heads, 4 from level 4 and 5 from level 7. At <see cref="HydraSong.BiteSeconds"/> every head
    /// whose beekeeper still lives bites it once, wherever it has walked; a head whose beekeeper died first, and a spare head
    /// with no beekeeper, hurt nobody. Until the bite each head follows its beekeeper while it lives, then stays where it bit.
    /// Spare heads strike the ground in a fan around the aim. Its reach and damage grow with the swarm's level, as it stood
    /// at the cast; with no aim chooser, the first head bites the aimed beekeeper when it lives within reach.
    /// </summary>
    /// <remarks>
    /// The picture (plan section B's "CPU:" line has room for three heads only, so the numbers are packed):
    /// ParamA = (head 1, head 2, head 3, head 4); ParamB = (head 5, <see cref="HydraSong.BiteSeconds"/>, size scale, 0).
    /// The bite's seconds let the picture's heads land on the frame they bite, however the bite is tuned; the picture
    /// rears the heads back until shortly before it. The size scale is the level's at the cast
    /// (<see cref="SwarmSongContext.SizeScale"/>), which grows the necks, the rear and the splashes as the reach grows; the
    /// picture takes the ground's height from the core's, which the rule's heads share.
    /// Each head is one whole number, exact in a float: 4,194,304 (2^22) when the head bites a beekeeper and 0 for a spare
    /// head that strikes the ground, plus x × 2048 + z, where x = round((point.x + 64) × 16) and z = round((point.z + 64) × 16),
    /// each held to 0..2047: the head's point on the ground on a 1/16 m grid covering 64 m either side of the world origin.
    /// Decode: v = (uint)p; bites = v >= 4194304; x = ((v >> 11) &amp; 2047) / 16 - 64; z = (v &amp; 2047) / 16 - 64.
    /// A head is never 0 (one on the grid's very corner is moved 1/16 m), and heads past the count are 0, so the picture
    /// counts the heads by the ones that are not 0. Heads are in order: the bitten ones nearest first, then the spare ones.
    /// A spare head's point is at the core's height at the cast. Every head raises Landed at the bite with its point.
    /// </remarks>
    internal sealed class HydraRule : SongRule
    {
        // Not game tuning: the picture has room for five heads, the most the song allows (three, and two from the level).
        private const int MostHeads = 5;

        // Not game tuning: how one head is packed into a float for the picture, as the class remarks say.
        private const float GridOriginMetres = 64f;
        private const float GridStepsPerMetre = 16f;
        private const int GridLastStep = 2047;
        private const int GridAxisSteps = 2048;
        private const int BitesFlag = 1 << 22;

        private readonly HydraSong _song;
        private readonly ISwarmTarget[] _targets = new ISwarmTarget[MostHeads];
        private readonly Vector3[] _points = new Vector3[MostHeads];

        private FuryMeter _fury;
        private float _damage;
        private float _groundHeight;
        private float _sizeScale = 1f;
        private int _heads;
        private int _biting;
        private bool _hasBitten;

        public HydraRule(HydraSong song)
            : base(song)
        {
            _song = song;
        }

        /// <summary>Heads 1 to 4, each packed as the class remarks say.</summary>
        public override Vector4 ParamA => new Vector4(Packed(0), Packed(1), Packed(2), Packed(3));

        /// <summary>Head 5, packed as the class remarks say, the bite's seconds, and the level's size scale at the cast.</summary>
        public override Vector4 ParamB => new Vector4(Packed(4), _song.BiteSeconds, _sizeScale, 0f);

        protected override void Begin(in SwarmSongContext context)
        {
            _fury = context.Fury ?? throw new ArgumentException("The song context has no Fury meter.", nameof(context));
            float reach = _song.Reach * context.SizeScale;
            _damage = _song.Damage * context.DamageScale;
            _heads = Math.Min(_song.HeadCount(context.Level), MostHeads);
            _groundHeight = context.CorePosition.y;
            _sizeScale = context.SizeScale;
            _hasBitten = false;
            Array.Clear(_targets, 0, MostHeads);

            Vector3 aim = ChooseTargets(in context, reach);

            for (int head = 0; head < _biting; head++)
            {
                _points[head] = _targets[head].Position;
            }

            AimSpareHeads(context.CorePosition, aim, reach);
        }

        protected internal override void Tick(in SongTick tick)
        {
            if (!_hasBitten)
            {
                Follow();

                if (tick.Seconds >= _song.BiteSeconds)
                {
                    _hasBitten = true;
                    Bite();
                }
            }

            if (tick.Seconds >= _song.Seconds)
            {
                Release(_song.Seconds);
            }
        }

        // One beekeeper a head from the aim chooser; without one, the aimed beekeeper for the first head when it is in reach.
        // Returns the aim, around which the spare heads fan out.
        private Vector3 ChooseTargets(in SwarmSongContext context, float reach)
        {
            if (context.Aim != null)
            {
                int chosen = context.Aim.ChooseNearest(reach, _heads, _targets, out Vector3 point);
                _biting = Math.Max(0, Math.Min(chosen, _heads));
                return point;
            }

            ISwarmTarget aimed = context.AimTarget;
            bool isInReach = aimed != null
                && aimed.IsAlive
                && GroundMath.DistanceSquared(aimed.Position, context.CorePosition) <= reach * reach;
            _biting = isInReach ? 1 : 0;

            if (isInReach)
            {
                _targets[0] = aimed;
            }

            return context.AimPoint;
        }

        // The heads with no beekeeper strike the ground at the aim's distance, pulled in to the reach, fanned around the aim:
        // either side of it beside a bitten beekeeper, and on the aim itself first when none was chosen. Fan steps 0, 1, 2, 3
        // and 4 turn by 0, +1, -1, +2 and -2 fans.
        private void AimSpareHeads(Vector3 core, Vector3 aim, float reach)
        {
            Vector3 point = GroundMath.PullIn(aim, core, reach);
            float dx = point.x - core.x;
            float dz = point.z - core.z;
            float fan = _song.SpareFanDegrees * Mathf.Deg2Rad;
            int step = _biting == 0 ? 0 : 1;

            for (int head = _biting; head < _heads; head++)
            {
                int turns = (step + 1) / 2;
                float angle = (step % 2 == 1 ? turns : -turns) * fan;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                _points[head] = new Vector3(core.x + (dx * cos) - (dz * sin), _groundHeight, core.z + (dx * sin) + (dz * cos));
                step++;
            }
        }

        // A head follows its beekeeper while it lives, and stays where it last saw it once it dies.
        private void Follow()
        {
            for (int head = 0; head < _biting; head++)
            {
                if (_targets[head].IsAlive)
                {
                    _points[head] = _targets[head].Position;
                }
            }
        }

        private void Bite()
        {
            float totalDamage = 0f;

            for (int head = 0; head < _biting; head++)
            {
                ISwarmTarget target = _targets[head];

                if (!target.IsAlive)
                {
                    continue;
                }

                Hit(target, _damage);
                totalDamage += _damage;
            }

            if (totalDamage > 0f)
            {
                _fury.AddDamage(totalDamage);
            }

            for (int head = 0; head < _heads; head++)
            {
                Land(_points[head]);
            }
        }

        private float Packed(int head)
        {
            if (head >= _heads)
            {
                return 0f;
            }

            // At least 1: the picture reads 0 as no head at all.
            Vector3 point = _points[head];
            int bites = head < _biting ? BitesFlag : 0;
            return Math.Max(bites + (GridStep(point.x) * GridAxisSteps) + GridStep(point.z), 1);
        }

        private static int GridStep(float metres)
        {
            return Mathf.Clamp(Mathf.RoundToInt((metres + GridOriginMetres) * GridStepsPerMetre), 0, GridLastStep);
        }
    }
}
