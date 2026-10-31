using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Lays the night over the town, with lamps and the Piper cutting through it.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [DefaultExecutionOrder(100)] // Runs after TownCamera, so the darkness never lags behind the view.
    [AddComponentMenu("DigitalStory/Darkness View")]
    public sealed class DarknessView : MonoBehaviour
    {
        private const int DiscoLightCount = 5;
        private const float FullTurn = Mathf.PI * 2f;
        private const float DiscoSaturation = .8f;
        private const float DiscoBrightness = .96f;

        // Slightly larger than the view, so rounding can never show a lit edge.
        private const float Overscan = 1.02f;

        private static readonly int LightsId = Shader.PropertyToID("_Lights");
        private static readonly int GlowsId = Shader.PropertyToID("_Glows");
        private static readonly int LightCountId = Shader.PropertyToID("_LightCount");
        private static readonly int DarknessId = Shader.PropertyToID("_Darkness");
        private static readonly int DawnWashId = Shader.PropertyToID("_DawnWash");

        private NightSession _session;
        private Camera _camera;
        private NightLookConfig _look;
        private SpriteRenderer _renderer;
        private Material _material;

        /// <summary>The lights drawn this frame, for tests and debugging.</summary>
        public LightField Field { get; } = new LightField();

        /// <summary>The darkness drawn this frame, for tests and debugging.</summary>
        public Color ShownDarkness { get; private set; }

        /// <summary>Supplies what to draw. Call once, before this object starts.</summary>
        public void Initialize(NightSession session, Camera sceneCamera, NightLookConfig look)
        {
            _session = session;
            _camera = sceneCamera;
            _look = look;
        }

        /// <summary>Redraws the night.</summary>
        public void Paint()
        {
            Field.Begin(FitToTheCamera());
            AddPiperLight();
            AddDiscoLights();
            AddLamps();

            float dawn = _session.Dawn;
            Color wash = _look.DawnWash;
            wash.a *= dawn;
            ShownDarkness = Color.Lerp(_look.NightColor, _look.DawnColor, dawn);

            _material.SetVectorArray(LightsId, Field.Lights);
            _material.SetVectorArray(GlowsId, Field.Glows);
            _material.SetFloat(LightCountId, Field.Count);
            _material.SetVector(DarknessId, ShownDarkness);
            _material.SetVector(DawnWashId, wash);
        }

        private void Awake() => _renderer = GetComponent<SpriteRenderer>();

        private void Start()
        {
            // GameRoot has already reported the problem if nothing was supplied.
            if (_session != null)
            {
                _material = new Material(_renderer.sharedMaterial);
                _renderer.sharedMaterial = _material;
            }
        }

        private void LateUpdate()
        {
            if (_material != null)
            {
                Paint();
            }
        }

        private void OnDestroy()
        {
            // Materials made in code are not freed with the scene.
            if (_material != null)
            {
                Destroy(_material);
            }
        }

        private Rect FitToTheCamera()
        {
            float height = _camera.orthographicSize * 2f;
            float width = height * _camera.aspect;
            Vector3 centre = _camera.transform.position;

            transform.position = new Vector3(centre.x, centre.y, 0f);
            transform.localScale = new Vector3(width * Overscan, height * Overscan, 1f);
            return new Rect(centre.x - (width / 2f), centre.y - (height / 2f), width, height);
        }

        private void AddPiperLight()
        {
            Vector3 piper = TownSpace.ToScene(_session.Piper.Position);
            Field.Add(piper, TownSpace.ToSceneLength(PiperReach()), _look.PiperClearing);
        }

        private float PiperReach()
        {
            if (_session.Stage.IsDiscoing)
            {
                return _look.DiscoReach;
            }

            if (_session.Flute.Mode == FluteMode.None)
            {
                return _look.PiperReach;
            }

            return _look.PlayingReach + (Mathf.Sin(Time.time * _look.PlayingPulseRate) * _look.PlayingPulse);
        }

        private void AddDiscoLights()
        {
            if (!_session.Stage.IsDiscoing)
            {
                return;
            }

            Vector2 piper = TownSpace.ToScene(_session.Piper.Position);
            float circle = TownSpace.ToSceneLength(_look.DiscoCircle);
            float reach = TownSpace.ToSceneLength(_look.DiscoLightReach);

            for (int index = 0; index < DiscoLightCount; index++)
            {
                float share = index / (float)DiscoLightCount;
                float angle = (Time.time * _look.DiscoSpinRate) + (share * FullTurn);
                Vector2 position = piper + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * circle);
                Color glow = Color.HSVToRGB(Mathf.Repeat(share + (Time.time * _look.DiscoHueRate), 1f), DiscoSaturation, DiscoBrightness);
                glow.a = _look.DiscoLightStrength;
                Field.Add(position, reach, 0f, glow, reach);
            }
        }

        private void AddLamps()
        {
            float reach = TownSpace.ToSceneLength(_look.LampReach);
            float glowReach = TownSpace.ToSceneLength(_look.LampGlowReach);
            var lamps = _session.Town.Lamps;

            for (int index = 0; index < lamps.Count; index++)
            {
                Vector3 lamp = TownSpace.ToScene(lamps[index]);
                Field.Add(lamp, reach, _look.LampClearing, _look.LampGlow, glowReach);
            }
        }
    }
}
