using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Plays the notes, rings, ripples and puffs that follow what happens in the night.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Effects View")]
    public sealed class EffectsView : MonoBehaviour
    {
        // Under the darkness, like the town they happen in.
        private const int GroundOrder = 5;

        // Over the darkness, because notes are the flute's own light.
        private const int FloatingOrder = 13;

        private NightSession _session;
        private GameFlow _flow;
        private TownArtConfig _art;
        private EffectsConfig _effects;
        private SpriteRenderer _musicCircle;
        private float _noteWait;
        private float _ringWait;

        /// <summary>The effects on screen, for tests and debugging.</summary>
        public EffectPool Pool { get; private set; }

        /// <summary>True while the circle around the Piper shows, for tests and debugging.</summary>
        public bool IsMusicCircleShown => _musicCircle != null && _musicCircle.enabled;

        /// <summary>Supplies what to follow. Call once, before this object is enabled.</summary>
        public void Initialize(NightSession session, GameFlow flow, TownArtConfig art, EffectsConfig effects)
        {
            _session = session;
            _flow = flow;
            _art = art;
            _effects = effects;
        }

        private void Awake() => Pool = new EffectPool(transform);

        private void OnEnable()
        {
            if (_session == null)
            {
                return;
            }

            _session.SongCast += HandleSongCast;
            _session.Swarm.RatDrowned += HandleRatDrowned;
            _session.Swarm.RatEaten += HandleRatEaten;
            _session.Swarm.TranceEnded += HandleTranceEnded;
        }

        private void Start()
        {
            // GameRoot has already reported the problem if nothing was supplied.
            if (_session != null)
            {
                _musicCircle = NewMusicCircle();
            }
        }

        private void Update()
        {
            if (_session == null)
            {
                return;
            }

            ShowMusicCircle();

            if (_flow.IsPlaying)
            {
                float deltaTime = Time.deltaTime;
                PlayFluteEffects(deltaTime);
                PlayChoirNotes(deltaTime);
                Pool.Tick(deltaTime);
            }
        }

        private void OnDisable()
        {
            // Unity also calls this when the scene unloads before Initialize ran, so _session can be null.
            if (_session == null)
            {
                return;
            }

            _session.SongCast -= HandleSongCast;
            _session.Swarm.RatDrowned -= HandleRatDrowned;
            _session.Swarm.RatEaten -= HandleRatEaten;
            _session.Swarm.TranceEnded -= HandleTranceEnded;
        }

        private SpriteRenderer NewMusicCircle()
        {
            var circle = new GameObject("Music Circle");
            circle.transform.SetParent(transform, false);

            var circleRenderer = circle.AddComponent<SpriteRenderer>();
            circleRenderer.sprite = _art.RingSprite;
            circleRenderer.sortingOrder = GroundOrder;
            circleRenderer.enabled = false;
            return circleRenderer;
        }

        private void ShowMusicCircle()
        {
            int hole = _session.Flute.CurrentHole;
            _musicCircle.enabled = hole != Flute.NoHole && _flow.IsPlaying;

            if (!_musicCircle.enabled)
            {
                return;
            }

            Color color = _art.HoleColor(hole);
            color.a = _effects.MusicCircleOpacity;
            _musicCircle.color = color;
            _musicCircle.transform.position = TownSpace.ToScene(_session.Piper.Position);
            _musicCircle.transform.localScale = Vector3.one * TownSpace.ToSceneLength(_session.Flute.MusicRadius * 2f);
        }

        private void PlayFluteEffects(float deltaTime)
        {
            int hole = _session.Flute.CurrentHole;

            if (hole == Flute.NoHole)
            {
                return;
            }

            _noteWait -= deltaTime;
            _ringWait -= deltaTime;

            if (_noteWait <= 0f)
            {
                _noteWait = _effects.NoteInterval;
                SpawnFluteNote(_art.HoleColor(hole));
            }

            if (_ringWait <= 0f)
            {
                bool isScaring = _session.Flute.Mode == FluteMode.Scare;
                _ringWait = isScaring ? _effects.ScareRingInterval : _effects.RingInterval;
                SpawnRing(_art.HoleColor(hole), _session.Flute.MusicRadius, isScaring ? _effects.ScareRingSpeed : _effects.RingSpeed);
            }
        }

        private void SpawnFluteNote(Color color)
        {
            Vector2 start = _session.Piper.Position
                + new Vector2(Random.Range(-_effects.NoteStartSpread, _effects.NoteStartSpread), -_effects.NoteStartHeight);
            var velocity = new Vector2(Random.Range(-_effects.NoteDriftSpeed, _effects.NoteDriftSpeed), -_effects.NoteRiseSpeed);
            SpawnNote(start, velocity, color, _effects.NoteSeconds);
        }

        private void PlayChoirNotes(float deltaTime)
        {
            if (!_session.Stage.IsSinging || Random.value >= deltaTime * _effects.ChoirNotesPerSecond)
            {
                return;
            }

            RatSwarm swarm = _session.Swarm;
            int index = Random.Range(0, swarm.Count);

            if (swarm[index].IsAlive && swarm.IsCharmed(index))
            {
                var velocity = new Vector2(Random.Range(-_effects.NoteDriftSpeed, _effects.NoteDriftSpeed), -_effects.ChoirRiseSpeed);
                SpawnNote(swarm[index].Position, velocity, _effects.ChoirNoteColor, _effects.ChoirNoteSeconds);
            }
        }

        private void SpawnNote(Vector2 position, Vector2 velocity, Color color, float seconds)
        {
            Pool.Spawn(new EffectSpawn(_art.NoteSprite, FloatingOrder, position, velocity, color, _effects.NoteSize, 0f, 1f, seconds, 1f));
        }

        private void SpawnRing(Color color, float reach, float speed)
        {
            float start = _effects.RingStartRadius;
            float seconds = (reach - start) / speed;
            color.a = _effects.RingOpacity;
            Pool.Spawn(new EffectSpawn(
                _art.RingSprite,
                GroundOrder,
                _session.Piper.Position,
                Vector2.zero,
                color,
                start * 2f,
                speed * 2f,
                1f,
                seconds,
                seconds));
        }

        private void SpawnRipple(Vector2 position)
        {
            float seconds = _effects.RippleSeconds;
            Pool.Spawn(new EffectSpawn(
                _art.RingSprite,
                GroundOrder,
                position,
                Vector2.zero,
                _effects.RippleColor,
                _effects.RippleStartRadius * 2f,
                _effects.RippleGrowth * 2f,
                _effects.RippleFlatness,
                seconds,
                seconds));
        }

        private void SpawnPuffs(Vector2 position, in PuffStyle style, bool isRising)
        {
            for (int index = 0; index < style.Count; index++)
            {
                Vector2 velocity = isRising
                    ? Vector2.down * Random.Range(style.Speed / 2f, style.Speed)
                    : Random.insideUnitCircle * style.Speed;

                Pool.Spawn(new EffectSpawn(
                    _art.DotSprite,
                    GroundOrder,
                    position,
                    velocity,
                    style.Color,
                    _effects.PuffSize,
                    0f,
                    1f,
                    style.Seconds,
                    style.Seconds));
            }
        }

        private void HandleSongCast(int song)
        {
            SongFlourish flourish = _session.FlourishOf(song);
            SpawnRing(flourish.RingColor, flourish.RingReach, flourish.RingSpeed);
        }

        private void HandleRatDrowned(Vector2 position)
        {
            SpawnRipple(position);
            SpawnPuffs(position, _effects.SplashPuff, true);
        }

        private void HandleRatEaten(Vector2 position) => SpawnPuffs(position, _effects.EatenPuff, false);

        private void HandleTranceEnded(Vector2 position, TranceEnding ending)
        {
            if (ending == TranceEnding.Popped)
            {
                SpawnPuffs(position, _effects.PopPuff, false);
            }
        }
    }
}
