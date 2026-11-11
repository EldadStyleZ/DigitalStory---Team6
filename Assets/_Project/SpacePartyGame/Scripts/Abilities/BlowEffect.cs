using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// What a blow looks like: a burst of watercolour dabs streaking out along the cone in the player's colour, and
    /// a wide painted arc sweeping out to the edge of the reach and fading. It plays when
    /// <see cref="PlayerAbilities.Blew"/> fires; every size and time is in <see cref="BlowSettings"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BlowEffect : MonoBehaviour
    {
        [SerializeField, Tooltip("The player whose blow this shows.")]
        private PlayerAbilities _abilities;

        [SerializeField, Tooltip("Whose colour the effect takes.")]
        private PlayerIdentity _identity;

        [SerializeField, Tooltip("The blow's numbers.")]
        private BlowSettings _settings;

        [SerializeField, Tooltip("The particle system that throws the dabs (world space, no emission of its own).")]
        private ParticleSystem _burst;

        [SerializeField, Tooltip("The painted arc; its sprite's pivot is the cone's tip and it points right.")]
        private SpriteRenderer _arc;

        [Header("Wave (replaces the burst and arc)")]
        [SerializeField, Tooltip("Plays the hand-drawn wave frames (Animations/BlowWave.controller); edit its timing in the Animation window.")]
        private Animator _wave;

        [SerializeField, Tooltip("The wave's picture, tinted the player's colour. Its frames are white and point right from their pivot.")]
        private SpriteRenderer _waveSprite;

        [SerializeField, Tooltip("The Animator state that plays the wave once.")]
        private string _waveState = "Play";

        [SerializeField, Tooltip("Where the drawing starts in the wave frames, from their pivot, in units at scale 1 (measured from the frames).")]
        private float _waveContentStart = 0.04f;

        [SerializeField, Tooltip("Where the drawing ends in the wave frames, from their pivot, in units at scale 1 (measured from the frames).")]
        private float _waveContentEnd = 7.72f;

        [Header("Sector (shown with the wave)")]
        [SerializeField, Tooltip("The see-through sector showing the blow's area; its white picture points right from its tip, drawn for 90° with a radius of 1 unit.")]
        private SpriteRenderer _sector;

        private float _sectorStartedAt = float.NegativeInfinity;
        private string _sectorShape;
        private Sprite _sectorPicture;

        private float _arcStartedAt = float.NegativeInfinity;
        private Color _arcColour;

        /// <summary>How many dabs the last blow threw, for checking.</summary>
        public int LastDabCount { get; private set; }

        /// <summary>Whether the arc is on screen now.</summary>
        public bool IsArcShowing => _arc != null && _arc.enabled;

        private void Awake()
        {
            if (_abilities == null || _identity == null || _settings == null || _wave == null || _waveSprite == null)
            {
                Debug.LogError($"{name}: BlowEffect is missing a reference.", this);
                return;
            }

            _arc.enabled = false;
        }

        private void OnEnable()
        {
            if (_abilities != null)
            {
                _abilities.Blew += Play;
            }
        }

        private void OnDisable()
        {
            if (_abilities != null)
            {
                _abilities.Blew -= Play;
            }

            if (_arc != null)
            {
                _arc.enabled = false;
            }
        }

        /// <summary>Plays the effect toward <paramref name="aim"/>.</summary>
        public void Play(Vector2 aim)
        {
            if (_settings == null || _wave == null || _waveSprite == null)
            {
                return;
            }

            Vector2 direction = aim.sqrMagnitude > 1e-6f ? aim.normalized : Vector2.right;
            // Starts in front of the player, not inside them.
            // Vector2 origin = (Vector2)_abilities.transform.position + _settings.EffectOffset;
            // Vector2 origin = (Vector2)_abilities.transform.position + _settings.EffectOffset + direction * _settings.EffectForward;
            // The wave now fits the blow's area exactly, from the body's centre (see PlayWave).
            Vector2 origin = (Vector2)_abilities.transform.position + _settings.EffectOffset;
            // The hand-drawn wave replaces the paint burst and arc (their objects are switched off).
            // ThrowDabs(origin, direction);
            // StartArc(origin, direction);
            PlayWave(origin, direction);
            ShowSector((Vector2)_abilities.transform.position + _settings.EffectOffset, direction);
        }

        /// <summary>Whether the sector is on screen now.</summary>
        public bool IsSectorShowing => _sector != null && _sector.enabled;

        // The sector covers the blow's real area, from the middle of the body out to the push reach.
        private void ShowSector(Vector2 origin, Vector2 direction)
        {
            if (_sector == null)
            {
                return;
            }

            Transform sector = _sector.transform;
            sector.position = new Vector3(origin.x, origin.y, sector.position.z);
            sector.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.right, direction));
            // The picture is drawn to the blow's exact area, so it is shown at its own size.
            // float reach = _settings.Reach;
            // float widen = Mathf.Tan(Mathf.Clamp(_settings.ConeAngle, 10f, 170f) * 0.5f * Mathf.Deg2Rad);
            // sector.localScale = new Vector3(reach, reach * widen, 1f);
            sector.localScale = Vector3.one;
            _sector.sprite = SectorPicture();
            _sectorStartedAt = Time.time;
            _sector.enabled = true;
            DrawSector();
        }

        // Draws the sector picture again only when the blow's shape or look has changed.
        private Sprite SectorPicture()
        {
            string shape = $"{_settings.ConeAngle}|{_settings.InnerRadius}|{_settings.Reach}|{_settings.SectorWashAlpha}|{_settings.SectorBlotchAlpha}|{_settings.SectorBlotchSize}|{_settings.SectorFeather}|{_settings.SectorWashVariation}|{_settings.SectorPixelsPerUnit}";

            if (_sectorPicture != null && shape == _sectorShape)
            {
                return _sectorPicture;
            }

            if (_sectorPicture != null)
            {
                Destroy(_sectorPicture.texture);
                Destroy(_sectorPicture);
            }

            var look = new BlowSectorPicture.Look
            {
                PixelsPerUnit = _settings.SectorPixelsPerUnit,
                WashAlpha = _settings.SectorWashAlpha,
                BlotchAlpha = _settings.SectorBlotchAlpha,
                BlotchSize = _settings.SectorBlotchSize,
                Feather = _settings.SectorFeather,
                WashVariation = _settings.SectorWashVariation,
            };

            _sectorPicture = BlowSectorPicture.Create(_settings.ConeAngle, _settings.InnerRadius, _settings.Reach, look);
            _sectorShape = shape;
            return _sectorPicture;
        }

        private void Start()
        {
            // Drawn once up front, so the first blow does not stall.
            if (_settings != null && _sector != null)
            {
                SectorPicture();
            }
        }

        private void OnDestroy()
        {
            if (_sectorPicture != null)
            {
                Destroy(_sectorPicture.texture);
                Destroy(_sectorPicture);
            }
        }

        private void DrawSector()
        {
            float t = Time.time - _sectorStartedAt;
            float fadeIn = _settings.SectorFadeInSeconds;
            float hold = _settings.SectorHoldSeconds;
            float fadeOut = _settings.SectorFadeOutSeconds;
            float strength = t < fadeIn ? (fadeIn > 0f ? t / fadeIn : 1f)
                : t < fadeIn + hold ? 1f
                : 1f - (t - fadeIn - hold) / fadeOut;

            // Hidden only once fully faded out (at the very start the fade-in is also at 0).
            if (t >= fadeIn + hold + fadeOut)
            {
                _sector.enabled = false;
                return;
            }

            Color colour = _identity.Colour;
            colour.a = _settings.SectorAlpha * Mathf.Clamp01(strength);
            _sector.color = colour;
        }

        /// <summary>Whether the wave is on screen now.</summary>
        public bool IsWaveShowing => _waveSprite != null && _waveSprite.enabled;

        // Turns the wave to the aim, stretches it over the whole cone (its length to the reach, its height to the
        // cone's width at the reach), tints it and plays it from the first frame.
        private void PlayWave(Vector2 origin, Vector2 direction)
        {
            Transform wave = _wave.transform;
            // wave.position = new Vector3(origin.x, origin.y, wave.position.z);
            wave.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.right, direction));

            Sprite frame = _waveSprite.sprite;
            Vector2 size = frame != null ? (Vector2)frame.bounds.size : Vector2.one;
            // float length = _settings.EffectReach;
            float length = _settings.Reach;
            float width = 2f * length * Mathf.Tan(Mathf.Clamp(_settings.ConeAngle, 10f, 170f) * 0.5f * Mathf.Deg2Rad);
            // wave.localScale = new Vector3(length / size.x, width / size.y, 1f);
            // Along the aim, the drawing runs exactly from the inner radius to the reach: the blow's area.
            // float keepShape = length / size.x;
            float keepShape = BlowCone.FitAlong(_waveContentStart, _waveContentEnd, _settings.InnerRadius, _settings.Reach, out float shift);
            Vector2 start = origin + direction * shift;
            wave.position = new Vector3(start.x, start.y, wave.position.z);
            float stretched = width / size.y;
            wave.localScale = new Vector3(keepShape, Mathf.Lerp(keepShape, stretched, _settings.WaveStretch), 1f);

            _waveSprite.color = _identity.Colour;
            _wave.Play(_waveState, 0, 0f);
        }

        private void ThrowDabs(Vector2 origin, Vector2 direction)
        {
            float half = _settings.ConeAngle * 0.5f;
            Color colour = _identity.Colour;
            int count = _settings.DabCount;

            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(-half, half);
                Vector2 heading = Quaternion.Euler(0f, 0f, angle) * direction;
                float lifetime = Random.Range(_settings.DabLifetime.x, _settings.DabLifetime.y);

                // Fast enough to reach the edge of the blow (some a little short, some a little past) in its lifetime.
                float travel = _settings.EffectReach * Random.Range(_settings.DabTravel.x, _settings.DabTravel.y);
                Color dabColour = Color.Lerp(colour, Color.white, Random.Range(0f, _settings.DabWhiteMix));
                dabColour.a = _settings.DabAlpha;

                var dab = new ParticleSystem.EmitParams
                {
                    position = origin + Random.insideUnitCircle * _settings.DabScatter,
                    velocity = heading * (travel / lifetime),
                    startSize = Random.Range(_settings.DabSize.x, _settings.DabSize.y),
                    startLifetime = lifetime,
                    startColor = dabColour,
                    rotation = Random.Range(0f, 360f),
                };

                _burst.Emit(dab, 1);
            }

            LastDabCount = count;
        }

        private void StartArc(Vector2 origin, Vector2 direction)
        {
            _arc.transform.position = new Vector3(origin.x, origin.y, _arc.transform.position.z);
            _arc.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.right, direction));
            _arcColour = _identity.Colour;
            _arcStartedAt = Time.time;
            _arc.enabled = true;
            DrawArc(0f);
        }

        private void Update()
        {
            if (_sector != null && _sector.enabled && _settings != null)
            {
                DrawSector();
            }

            if (_arc == null || !_arc.enabled)
            {
                return;
            }

            float t = (Time.time - _arcStartedAt) / Mathf.Max(0.01f, _settings.ArcSeconds);

            if (t >= 1f)
            {
                _arc.enabled = false;
                return;
            }

            DrawArc(t);
        }

        // The arc grows fast then slows (ease out) to the full reach, fading as it goes.
        private void DrawArc(float t)
        {
            float grow = 1f - (1f - t) * (1f - t);
            float radius = Mathf.Lerp(_settings.ArcStartFraction, 1f, grow) * _settings.EffectReach;
            // The arc picture is drawn for a 90° cone; other cone angles widen or narrow it to match.
            float widen = Mathf.Tan(Mathf.Clamp(_settings.ConeAngle, 10f, 170f) * 0.5f * Mathf.Deg2Rad);
            _arc.transform.localScale = new Vector3(radius, radius * widen, 1f);
            Color colour = _arcColour;
            colour.a = _settings.ArcAlpha * (1f - t * t);
            _arc.color = colour;
        }
    }
}
