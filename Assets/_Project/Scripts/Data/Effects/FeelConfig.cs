using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// The game feel of F-010 (plan section Y): how much each moment of play shakes the camera, the shake itself, the camera's
    /// short kicks and punches, the kill chain's rising sound and stamps, and the pop of the hit numbers. One master scale
    /// turns every camera shake, kick, punch and flash down together, and 0 turns them off.
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Feel Config", fileName = "FeelConfig")]
    public sealed class FeelConfig : ScriptableObject
    {
        [field: Header("Master")]
        [field: SerializeField, Range(0f, 1f), Tooltip("Scales every camera shake, kick and punch and every flash, 0 to 1. 0 turns them all off (a reduced-motion setting).")]
        public float MasterScale { get; private set; } = 1f;

        [field: Header("Shake (trauma)")]
        [field: SerializeField, Min(0.01f), Tooltip("How much trauma falls each second. Trauma is 0 to 1, so 1.2 rests a full shake in under a second.")]
        public float TraumaDecayPerSecond { get; private set; } = 1.2f;

        [field: SerializeField, Range(1f, 3f), Tooltip("The power the shake raises trauma to. 2 keeps small events barely visible and big ones big.")]
        public float TraumaExponent { get; private set; } = 2f;

        [field: SerializeField, Range(0f, 3f), Tooltip("The most a shake rolls the view, in degrees, at full trauma. Capped at 3 so it never hides the action.")]
        public float MaxRollDegrees { get; private set; } = 2.5f;

        [field: SerializeField, Range(0f, 3f), Tooltip("The most a shake tips the view up, down or sideways, in degrees, at full trauma.")]
        public float MaxTiltDegrees { get; private set; } = 1f;

        [field: SerializeField, Range(0f, 0.5f), Tooltip("The most a shake moves the camera, in metres, at full trauma. In 3D a turn reads better than a move, so this stays small.")]
        public float MaxShakeMetres { get; private set; } = 0.15f;

        [field: SerializeField, Min(1f), Tooltip("How fast the shake's noise moves, in steps per second. Lower is a rumble, higher a rattle.")]
        public float NoiseSpeed { get; private set; } = 18f;

        [field: Header("Trauma each moment adds")]
        [field: SerializeField, Range(0f, 1f), Tooltip("Trauma for a hit at the full-shake damage; smaller hits add a share. The Sting's light hits add least.")]
        public float HitTrauma { get; private set; } = 0.12f;

        [field: SerializeField, Min(1f), Tooltip("The damage of a hit that adds the full hit trauma.")]
        public float FullHitDamage { get; private set; } = 80f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Trauma for a kill.")]
        public float KillTrauma { get; private set; } = 0.06f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Trauma when the Tornado throws.")]
        public float TornadoTrauma { get; private set; } = 0.15f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Trauma when the Hydra or a Nest goes off.")]
        public float HydraTrauma { get; private set; } = 0.2f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Trauma when the Lance strikes.")]
        public float LanceTrauma { get; private set; } = 0.25f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Trauma when the Meteor lands. 0.6 since plan section Z, so the heaviest song shakes clearly: 0.36 of the full shake.")]
        public float MeteorTrauma { get; private set; } = 0.6f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Trauma when a chain reaches a milestone (x3, x6, x10).")]
        public float MultiKillTrauma { get; private set; } = 0.15f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Trauma when the giant's punch, kick or thrown fist lands.")]
        public float GiantBlowTrauma { get; private set; } = 0.3f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Trauma when the giant's slam, leap landing or third punch lands. 0.65 since plan section Z, between the Meteor and a lost hive.")]
        public float GiantHeavyTrauma { get; private set; } = 0.65f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Trauma the moment the swarm becomes the giant.")]
        public float GiantFormedTrauma { get; private set; } = 0.5f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Trauma when a beekeeper hits a hive. At most once per the hive-hit gap for each hive.")]
        public float HiveHitTrauma { get; private set; } = 0.2f;

        [field: SerializeField, Min(0f), Tooltip("The fewest seconds between two hive-hit shakes of one hive, so a hive under a crowd does not rattle the screen.")]
        public float HiveHitGapSeconds { get; private set; } = 0.4f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Trauma when a hive is lost.")]
        public float HiveLostTrauma { get; private set; } = 0.7f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Trauma when the last beekeeper of a wave dies.")]
        public float WaveClearedTrauma { get; private set; } = 0.35f;

        [field: Header("Camera punches")]
        [field: SerializeField, Range(0f, 0.2f), Tooltip("How much the field of view narrows the frame a song is cast, as a share. A quick zoom punch.")]
        public float CastFovPunch { get; private set; } = 0.04f;

        [field: SerializeField, Range(0f, 0.2f), Tooltip("How much the field of view narrows the moment the swarm becomes the giant, as a share.")]
        public float GiantFovPunch { get; private set; } = 0.08f;

        [field: SerializeField, Range(0f, 3f), Tooltip("Degrees the view rolls with a cast's punch, alternating sides each cast.")]
        public float CastRollDegrees { get; private set; } = 1f;

        [field: SerializeField, Range(0f, 1f), Tooltip("The share of the cast's zoom punch, roll and kick a light song gets (the Sting, the quick jab), so a run of Stings does not rock the view like a Meteor. Plan section Z.")]
        public float LightCastShare { get; private set; } = 0.5f;

        [field: SerializeField, Min(0.01f), Tooltip("Seconds a punch takes to reach its peak.")]
        public float PunchInSeconds { get; private set; } = 0.06f;

        [field: SerializeField, Min(0.01f), Tooltip("Seconds a punch takes to ease back.")]
        public float PunchOutSeconds { get; private set; } = 0.3f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Metres the camera is kicked along the hit's direction when the giant's punch, kick or fist lands.")]
        public float GiantKickMetres { get; private set; } = 0.4f;

        [field: SerializeField, Min(0.5f), Tooltip("How fast the kicked camera springs back, in cycles per second.")]
        public float KickSpringHertz { get; private set; } = 4f;

        [field: SerializeField, Range(0.1f, 1f), Tooltip("How damped the kick's spring is: 1 settles with no overshoot, lower overshoots a little.")]
        public float KickSpringDamping { get; private set; } = 0.5f;

        [field: Header("Kill chain")]
        [field: SerializeField, Min(0.05f), Tooltip("The most seconds between two kills of one chain.")]
        public float ChainWindowSeconds { get; private set; } = 0.5f;

        [field: SerializeField, Range(0, 24), Tooltip("The most semitones a kill's pop climbs in a chain. 12 is an octave.")]
        public int ChainTopSemitones { get; private set; } = 12;

        [field: SerializeField, Tooltip("The chain counts that show a stamp (x3, x6, x10) and shake a little.")]
        public int[] ChainMilestones { get; private set; } = { 3, 6, 10 };

        [field: SerializeField, Min(0.05f), Tooltip("Seconds a chain stamp shows, fading over its last half.")]
        public float StampSeconds { get; private set; } = 0.9f;

        [field: SerializeField, Min(0.01f), Tooltip("Seconds a chain stamp takes to land from its start scale.")]
        public float StampLandSeconds { get; private set; } = 0.12f;

        [field: SerializeField, Range(1f, 3f), Tooltip("The scale a chain stamp starts at before it lands at 1.")]
        public float StampStartScale { get; private set; } = 1.6f;

        [field: Header("Hit numbers")]
        [field: SerializeField, Min(0.01f), Tooltip("Seconds a hit number takes to pop up to its peak and settle.")]
        public float NumberPopSeconds { get; private set; } = 0.15f;

        [field: SerializeField, Range(1f, 2f), Tooltip("The scale a hit number pops to before it settles at 1. A kill's number pops further (KillPopExtra).")]
        public float NumberPopScale { get; private set; } = 1.2f;

        [field: SerializeField, Range(0.1f, 1f), Tooltip("The scale a hit number starts at before it pops up past 1.")]
        public float NumberPopStartScale { get; private set; } = 0.6f;

        [field: SerializeField, Range(1f, 3f), Tooltip("How much further a kill's number pops than a hit's: its pop above 1 is this many times a hit's. 1.33 is a third further.")]
        public float KillPopExtra { get; private set; } = 1.33f;
    }
}
