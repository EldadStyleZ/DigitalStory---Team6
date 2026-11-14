using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// Every number for the blow power, shared by both players: holding the Left note charges it, letting go when
    /// it is full blows everything in front of the player away.
    /// </summary>
    [CreateAssetMenu(fileName = "BlowSettings", menuName = "Space Party Game/Blow Settings")]
    public sealed class BlowSettings : ScriptableObject
    {
        [Header("Charge")]
        [SerializeField, Min(0.1f), Tooltip("How long the Left note must be held to fill the bar, in seconds.")]
        private float _chargeSeconds = 1.5f;

        [Header("Sound")]
        [SerializeField, Range(0f, 2f), Tooltip("How loud the charging note is when the bar is empty (1 = a normal note).")]
        private float _chargeStartVolume = 0.25f;

        [SerializeField, Range(0f, 2f), Tooltip("How loud the charging note is when the bar is full (1 = a normal note).")]
        private float _chargeEndVolume = 1.4f;

        [SerializeField, Tooltip("The chord's notes, in semitones above the charging note: 0, 4, 7 is a major chord.")]
        private float[] _chordSemitones = { 0f, 4f, 7f };

        [SerializeField, Min(0.1f), Tooltip("How long the chord plays, in seconds.")]
        private float _chordSeconds = 1.5f;

        [SerializeField, Range(0f, 2f), Tooltip("How loud each chord note is (1 = a normal note).")]
        private float _chordVolume = 0.9f;

        [Header("Push")]
        [SerializeField, Range(10f, 180f), Tooltip("How wide the blow's cone is, in degrees, centred on the way the player aims.")]
        private float _coneAngle = 90f;

        [SerializeField, Min(0.1f), Tooltip("How far the blow reaches from the middle of the player's body, in units.")]
        private float _reach = 4.5f;

        [SerializeField, Min(0f), Tooltip("The gap around the player the blow leaves out, from the middle of the body, in units. The blow's area (and the sector shown) runs from here to the reach.")]
        private float _innerRadius = 0.7f;

        [SerializeField, Min(0f), Tooltip("How forgiving the blow is, in units: a target is hit when its body's middle is this close to the area, so an enemy half inside still counts. 0 = only when its middle is inside.")]
        private float _hitLeeway = 0.35f;

        [Header("Aiming preview")]
        [SerializeField, Tooltip("Shows the blow's area, faintly and following the aim, while the Left note is held.")]
        private bool _showAreaWhileCharging = true;

        [SerializeField, Tooltip("For tuning: shows the blow's area all the time, following the aim. Change the push numbers above in Play Mode and watch it update.")]
        private bool _alwaysShowArea;

        [SerializeField, Range(0f, 1f), Tooltip("How solid the preview is (multiplies the sector's own look).")]
        private float _previewAlpha = 0.35f;

        [SerializeField, Min(0f), Tooltip("How fast vermin are blown away at first, in units per second (at the player; weaker at the edge).")]
        private float _verminPushSpeed = 20f;

        [SerializeField, Min(0f), Tooltip("How fast another player is blown away at first, in units per second (at the player; weaker at the edge).")]
        private float _playerPushSpeed = 18f;

        [SerializeField, Range(0f, 1f), Tooltip("How strong the push still is at the edge of the reach, next to right at the player.")]
        private float _edgeStrength = 0.45f;

        [SerializeField, Min(0.05f), Tooltip("How long the push lasts, fading to nothing, in seconds. Blown vermin pass through cage walls for this long.")]
        private float _pushSeconds = 0.6f;

        [SerializeField, Range(0f, 1f), Tooltip("How much of their own movement a blown player keeps while pushed: 0.3 = 30%.")]
        private float _pushedPlayerControl = 0.3f;

        [SerializeField, Tooltip("Which layers the blow pushes: Vermin and Player.")]
        private LayerMask _pushLayers;

        [Header("Effect: hand-drawn wave")]
        [SerializeField, Range(0f, 1f), Tooltip("How much the wave is stretched to fill the cone's width: 0 keeps the drawing's own shape, 1 stretches it over the whole cone.")]
        private float _waveStretch = 1f;

        [Header("Effect: sector (the blow's area)")]
        [SerializeField, Range(0f, 1f), Tooltip("How solid the sector is at its strongest (multiplies the wash and the blotches).")]
        private float _sectorAlpha = 1f;

        [SerializeField, Range(0f, 1f), Tooltip("How solid the sector's see-through wash is.")]
        private float _sectorWashAlpha = 0.3f;

        [SerializeField, Range(0f, 1f), Tooltip("How uneven the sector's wash is: 0 is one even colour.")]
        private float _sectorWashVariation;

        [SerializeField, Range(0f, 1f), Tooltip("How solid the sector's darker paint blotches are: 0 leaves them out.")]
        private float _sectorBlotchAlpha;

        [SerializeField, Min(0.05f), Tooltip("Roughly how big the sector's paint blotches are, in units.")]
        private float _sectorBlotchSize = 0.9f;

        [SerializeField, Min(0f), Tooltip("How soft the sector's edges are, in units.")]
        private float _sectorFeather = 0.06f;

        [SerializeField, Min(10f), Tooltip("How sharp the sector picture is, in pixels per unit (it is drawn when play starts and when its shape changes).")]
        private float _sectorPixelsPerUnit = 110f;

        [SerializeField, Min(0f), Tooltip("How long the sector takes to appear, in seconds.")]
        private float _sectorFadeInSeconds = 0.06f;

        [SerializeField, Min(0f), Tooltip("How long the sector stays fully shown, in seconds.")]
        private float _sectorHoldSeconds = 0.2f;

        [SerializeField, Min(0.01f), Tooltip("How long the sector takes to fade away, in seconds.")]
        private float _sectorFadeOutSeconds = 0.5f;

        [Header("Effect: paint burst")]
        [SerializeField, Tooltip("Where the effect starts, from the player's feet, in units (the middle of the body).")]
        private Vector2 _effectOffset = new Vector2(0f, 0.45f);

        [SerializeField, Min(0f), Tooltip("Old paint burst and arc only (switched off): how far in front of the player they start, along the aim, in units. The wave fits the blow's area instead.")]
        private float _effectForward = 0.55f;

        [SerializeField, Min(0.1f), Tooltip("Old paint burst and arc only (switched off): how far they reach, in units. The wave and sector use the push reach and inner radius.")]
        private float _effectReach = 4.5f;

        [SerializeField, Min(0), Tooltip("How many paint dabs one blow throws.")]
        private int _dabCount = 70;

        [SerializeField, Tooltip("The smallest and largest dab, in units.")]
        private Vector2 _dabSize = new Vector2(0.25f, 0.55f);

        [SerializeField, Tooltip("The shortest and longest a dab lasts, in seconds.")]
        private Vector2 _dabLifetime = new Vector2(0.35f, 0.6f);

        [SerializeField, Tooltip("How far dabs fly, as a share of the reach: least and most.")]
        private Vector2 _dabTravel = new Vector2(0.6f, 1.15f);

        [SerializeField, Min(0f), Tooltip("How widely the dabs start around the player, in units.")]
        private float _dabScatter = 0.2f;

        [SerializeField, Range(0f, 1f), Tooltip("How solid a new dab is.")]
        private float _dabAlpha = 0.9f;

        [SerializeField, Range(0f, 1f), Tooltip("The most a dab is paled toward white, for a watercolour mix of shades.")]
        private float _dabWhiteMix = 0.45f;

        [Header("Effect: painted arc")]
        [SerializeField, Min(0.05f), Tooltip("How long the arc takes to sweep out and fade, in seconds.")]
        private float _arcSeconds = 0.45f;

        [SerializeField, Range(0f, 1f), Tooltip("How big the arc starts, as a share of the reach.")]
        private float _arcStartFraction = 0.15f;

        [SerializeField, Range(0f, 1f), Tooltip("How solid the arc is at the start.")]
        private float _arcAlpha = 0.9f;

        [Header("Effect: camera shake")]
        [SerializeField, Min(0f), Tooltip("How far the camera shakes, in units, for the blower and anyone blown.")]
        private float _shakeAmplitude = 0.18f;

        [SerializeField, Min(0f), Tooltip("How long the shake lasts, in seconds.")]
        private float _shakeSeconds = 0.25f;

        [Header("Bar (under the player's feet)")]
        [SerializeField, Min(0.05f), Tooltip("The bar's width, in units.")]
        private float _barWidth = 0.675f;

        [SerializeField, Min(0.02f), Tooltip("The bar's height, in units.")]
        private float _barHeight = 0.065f;

        [SerializeField, Tooltip("Where the bar sits, from the player's feet, in units.")]
        private Vector2 _barOffset = new Vector2(0f, -0.22f);

        [SerializeField, Tooltip("The blow's colour, whoever blows: its charge bar, area, wave and paint dabs. The earworm uses the player's colour instead.")]
        private Color _blowColour = new Color(0.86f, 0.12f, 0.12f, 1f);

        [SerializeField, Tooltip("The empty part of the bar.")]
        private Color _barBackColour = new Color(0.231f, 0.129f, 0.114f, 0.75f);

        [SerializeField, Min(0f), Tooltip("How long the bar takes to fade in and out, in seconds.")]
        private float _barFadeSeconds = 0.12f;

        public float ChargeSeconds => _chargeSeconds;
        public float ChargeStartVolume => _chargeStartVolume;
        public float ChargeEndVolume => _chargeEndVolume;
        public float[] ChordSemitones => _chordSemitones;
        public float ChordSeconds => _chordSeconds;
        public float ChordVolume => _chordVolume;
        public float ConeAngle => _coneAngle;
        public float Reach => _reach;
        public float VerminPushSpeed => _verminPushSpeed;
        public float PlayerPushSpeed => _playerPushSpeed;
        public float EdgeStrength => _edgeStrength;
        public float PushSeconds => _pushSeconds;
        public float PushedPlayerControl => _pushedPlayerControl;
        public LayerMask PushLayers => _pushLayers;
        public Vector2 EffectOffset => _effectOffset;
        public float WaveStretch => _waveStretch;
        public float SectorAlpha => _sectorAlpha;
        public float SectorWashAlpha => _sectorWashAlpha;
        public float SectorBlotchAlpha => _sectorBlotchAlpha;
        public float SectorWashVariation => _sectorWashVariation;
        public float SectorBlotchSize => _sectorBlotchSize;
        public float SectorFeather => _sectorFeather;
        public float SectorPixelsPerUnit => _sectorPixelsPerUnit;
        public float InnerRadius => _innerRadius;
        public float HitLeeway => _hitLeeway;
        public bool ShowAreaWhileCharging => _showAreaWhileCharging;
        public bool AlwaysShowArea => _alwaysShowArea;
        public float PreviewAlpha => _previewAlpha;
        public float SectorFadeInSeconds => _sectorFadeInSeconds;
        public float SectorHoldSeconds => _sectorHoldSeconds;
        public float SectorFadeOutSeconds => _sectorFadeOutSeconds;
        public float EffectForward => _effectForward;
        public float EffectReach => _effectReach;
        public int DabCount => _dabCount;
        public Vector2 DabSize => _dabSize;
        public Vector2 DabLifetime => _dabLifetime;
        public Vector2 DabTravel => _dabTravel;
        public float DabScatter => _dabScatter;
        public float DabAlpha => _dabAlpha;
        public float DabWhiteMix => _dabWhiteMix;
        public float ArcSeconds => _arcSeconds;
        public float ArcStartFraction => _arcStartFraction;
        public float ArcAlpha => _arcAlpha;
        public float ShakeAmplitude => _shakeAmplitude;
        public float ShakeSeconds => _shakeSeconds;
        public float BarWidth => _barWidth;
        public float BarHeight => _barHeight;
        public Vector2 BarOffset => _barOffset;
        public Color BarBackColour => _barBackColour;
        public Color BlowColour => _blowColour;
        public float BarFadeSeconds => _barFadeSeconds;
    }
}
