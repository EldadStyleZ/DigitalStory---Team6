using System;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// One smoke cloud: slow grey puffs that fill the cloud's radius and a faint disc on the ground showing how far it reaches.
    /// SmokeField decides when the cloud forms and ends; this only draws it.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Smoke Cloud View")]
    public sealed class SmokeCloudView : MonoBehaviour
    {
        private const int DiscSegments = 40;
        private const int TextureSize = 32;
        private const float DiscHeight = 0.03f;

        private static Texture2D s_puffTexture;
        private static int s_puffUsers;

        [SerializeField, Tooltip("Seconds a puff lives. Longer is smoother and thicker.")]
        private float _puffLifetime = 3f;

        [SerializeField, Tooltip("Puffs made per second for every square metre of ground the cloud covers.")]
        private float _puffsPerSquareMetre = 0.35f;

        [SerializeField, Tooltip("Size of one puff as a share of the cloud's radius.")]
        private float _puffSizeShare = 0.55f;

        [SerializeField, Tooltip("Colour of the puffs. The alpha is the thickest they get.")]
        private Color _smokeColor = new Color(0.55f, 0.55f, 0.55f, 0.4f);

        [SerializeField, Tooltip("Colour of the flat disc that shows the radius on the ground.")]
        private Color _discColor = new Color(0.5f, 0.5f, 0.5f, 0.14f);

        [SerializeField, Tooltip("Longest wait, in seconds, after the cloud ends before this view is handed back, even if puffs remain.")]
        private float _fadeSeconds = 3.5f;

        [SerializeField, Tooltip("Sorting order of the puffs, so they draw over the ground.")]
        private int _sortingOrder = 5;

        private ParticleSystem _particles;
        private ParticleSystemRenderer _particleRenderer;
        private Material _puffMaterial;
        private Material _discMaterial;
        private Mesh _discMesh;
        private GameObject _disc;
        private Gradient _fadeGradient;
        private float _radius;
        private float _hideElapsed;
        private bool _hiding;
        private bool _shown;
        private bool _stylized;

        /// <summary>Raised when the cloud has faded, so the pool can take this view back.</summary>
        public event Action<SmokeCloudView> Faded;

        /// <summary>Shows <paramref name="cloud"/> where it is and as wide as it is, and starts the smoke.</summary>
        public void Show(SmokeCloud cloud)
        {
            if (cloud == null)
            {
                throw new ArgumentNullException(nameof(cloud));
            }

            _radius = cloud.Radius;
            _hiding = false;
            _hideElapsed = 0f;
            _shown = true;
            transform.position = new Vector3(cloud.Position.x, 0f, cloud.Position.z);

            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Configure(_radius);

            SetDiscFade(1f);
            _disc.transform.localScale = new Vector3(_radius, 1f, _radius);
            _disc.SetActive(true);
            _particles.Play();
        }

        /// <summary>Stops making smoke. The view is handed back once the last puff is gone or the fade time has passed.</summary>
        public void Hide()
        {
            if (!_shown || _hiding)
            {
                return;
            }

            _hiding = true;
            _hideElapsed = 0f;
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void Awake()
        {
            _particles = GetComponent<ParticleSystem>();
            _particleRenderer = GetComponent<ParticleSystemRenderer>();
            if (_particles == null || _particleRenderer == null)
            {
                throw new InvalidOperationException("SmokeCloudView needs a ParticleSystem and its renderer.");
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }

            // Stylized smoke: toon-lit puffs that dissolve, and a soft ring on the ground. The plain look stays as a fallback.
            Shader puffShader = Shader.Find("DigitalStory/StylizedSmoke");
            Shader ringShader = Shader.Find("DigitalStory/SmokeGroundRing");

            if (shader == null)
            {
                throw new InvalidOperationException("Neither Sprites/Default nor Particles/Standard Unlit was found, so smoke cannot be drawn.");
            }

            if (s_puffTexture == null)
            {
                s_puffTexture = BuildPuffTexture();
            }

            s_puffUsers++;

            _stylized = puffShader != null;
            _puffMaterial = _stylized ? new Material(puffShader) : new Material(shader) { mainTexture = s_puffTexture };
            _particleRenderer.sharedMaterial = _puffMaterial;
            if (_stylized)
            {
                SmokeStyle.UseStreams(_particleRenderer);
            }
            _particleRenderer.sortingOrder = _sortingOrder;
            _particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _particleRenderer.receiveShadows = false;

            _discMaterial = ringShader != null ? new Material(ringShader) : new Material(shader) { color = _discColor };
            _discMesh = BuildDisc();
            _discMesh.name = "SmokeCloudDisc";
            _disc = new GameObject("RadiusDisc", typeof(MeshFilter), typeof(MeshRenderer));
            _disc.transform.SetParent(transform, false);
            _disc.transform.localPosition = new Vector3(0f, DiscHeight, 0f);
            _disc.GetComponent<MeshFilter>().sharedMesh = _discMesh;
            _disc.GetComponent<MeshRenderer>().sharedMaterial = _discMaterial;
            _disc.SetActive(false);

            _fadeGradient = new Gradient();
            _fadeGradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.25f),
                    new GradientAlphaKey(1f, 0.6f),
                    new GradientAlphaKey(0f, 1f),
                });

            ParticleSystem.MainModule main = _particles.main;
            main.playOnAwake = false;
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // While leaving Play mode the editor may already count as not playing, where only DestroyImmediate is allowed.
        private static void Release(UnityEngine.Object thing)
        {
            if (Application.isPlaying)
            {
                Destroy(thing);
            }
            else
            {
                DestroyImmediate(thing);
            }
        }

        private void OnDestroy()
        {
            Faded = null;
            if (_puffMaterial != null)
            {
                Release(_puffMaterial);
            }

            if (_discMaterial != null)
            {
                Release(_discMaterial);
            }

            if (_discMesh != null)
            {
                Release(_discMesh);
            }

            s_puffUsers--;
            if (s_puffUsers <= 0 && s_puffTexture != null)
            {
                Destroy(s_puffTexture);
                s_puffTexture = null;
                s_puffUsers = 0;
            }
        }

        private void Update()
        {
            if (!_hiding)
            {
                return;
            }

            _hideElapsed += Time.deltaTime;
            float fade = 1f - Mathf.Clamp01(_hideElapsed / _fadeSeconds);
            SetDiscFade(fade);

            if (_particles.particleCount > 0 && _hideElapsed < _fadeSeconds)
            {
                return;
            }

            _hiding = false;
            _shown = false;
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _disc.SetActive(false);
            Faded?.Invoke(this);
        }

        private void SetDiscFade(float fade)
        {
            if (_discMaterial.HasProperty("_Opacity"))
            {
                _discMaterial.SetFloat("_Opacity", fade);
                return;
            }

            Color disc = _discColor;
            disc.a *= fade;
            _discMaterial.color = disc;
        }

        private void Configure(float radius)
        {
            if (_stylized)
            {
                SmokeStyle.ConfigureCloud(_particles, radius, _puffLifetime, _puffsPerSquareMetre, _puffSizeShare);
                return;
            }

            ParticleSystem.MainModule main = _particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.duration = 5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = _puffLifetime;
            main.startSpeed = 0.15f;
            main.startSize = new ParticleSystem.MinMaxCurve(radius * _puffSizeShare * 0.7f, radius * _puffSizeShare);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = _smokeColor;
            main.gravityModifier = -0.02f;
            main.maxParticles = 300;

            ParticleSystem.EmissionModule emission = _particles.emission;
            emission.enabled = true;
            emission.rateOverTime = Mathf.PI * radius * radius * _puffsPerSquareMetre;

            ParticleSystem.ShapeModule shape = _particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = radius;
            shape.radiusThickness = 1f;
            shape.position = Vector3.zero;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            ParticleSystem.CollisionModule collision = _particles.collision;
            collision.enabled = false;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = _particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = _fadeGradient;

            ParticleSystem.RotationOverLifetimeModule rotation = _particles.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = 0.15f;
        }

        private static Texture2D BuildPuffTexture()
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                name = "SmokePuff",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var pixels = new Color32[TextureSize * TextureSize];
            float half = (TextureSize - 1) * 0.5f;
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float distance = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                    float alpha = Mathf.Clamp01(1f - distance);
                    alpha *= alpha;
                    pixels[y * TextureSize + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        // A flat unit-radius disc facing up. The transform scale sets the real radius.
        private static Mesh BuildDisc()
        {
            var vertices = new Vector3[DiscSegments + 1];
            var triangles = new int[DiscSegments * 3];
            for (int i = 0; i < DiscSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / DiscSegments;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

                int next = (i + 1) % DiscSegments;
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = next + 1;
                triangles[i * 3 + 2] = i + 1;
            }

            var mesh = new Mesh { vertices = vertices, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
