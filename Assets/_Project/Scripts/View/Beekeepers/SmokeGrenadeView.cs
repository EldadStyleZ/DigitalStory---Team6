using System;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// One smoke grenade in flight: a dark ball on an arc and a red ring on the ground where it will land.
    /// The rules live in SmokeField; this only draws the flight and says when it is over.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Smoke Grenade View")]
    public sealed class SmokeGrenadeView : MonoBehaviour
    {
        private const int RingSegments = 40;
        private const float RingHeight = 0.05f;

        [SerializeField, Tooltip("How high above the straight line the ball rises at the top of its arc, in metres.")]
        private float _peakHeight = 3f;

        [SerializeField, Tooltip("Diameter of the ball, in metres.")]
        private float _ballSize = 0.35f;

        [SerializeField, Tooltip("Outer radius of the landing ring, in metres. It shows roughly where the cloud will form.")]
        private float _ringRadius = 1.2f;

        [SerializeField, Range(0.05f, 0.9f), Tooltip("Inner radius of the ring as a share of the outer radius.")]
        private float _ringThickness = 0.8f;

        [SerializeField, Tooltip("Colour of the landing ring.")]
        private Color _ringColor = new Color(0.95f, 0.15f, 0.1f, 0.85f);

        [SerializeField, Tooltip("Colour of the ball.")]
        private Color _ballColor = new Color(0.12f, 0.12f, 0.12f, 1f);

        private Transform _ball;
        private Mesh _ringMesh;
        private Material _ringMaterial;
        private Material _ballMaterial;
        private Vector3 _from;
        private Vector3 _to;
        private float _flightSeconds;
        private float _elapsed;
        private bool _flying;

        /// <summary>Raised when the flight ends, so the pool can take this view back.</summary>
        public event Action<SmokeGrenadeView> Landed;

        /// <summary>Starts a flight from <paramref name="from"/> to <paramref name="to"/> that lasts <paramref name="flightSeconds"/>.</summary>
        public void Launch(Vector3 from, Vector3 to, float flightSeconds)
        {
            if (flightSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(flightSeconds), "Flight time must be above zero.");
            }

            _from = from;
            _to = to;
            _flightSeconds = flightSeconds;
            _elapsed = 0f;
            _flying = true;
            transform.position = new Vector3(to.x, RingHeight, to.z);
            _ball.position = from;
        }

        private void Awake()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                throw new InvalidOperationException("The Sprites/Default shader was not found, so the grenade cannot be drawn.");
            }

            _ringMaterial = new Material(shader) { color = _ringColor };
            _ballMaterial = new Material(shader) { color = _ballColor };

            _ringMesh = BuildRing(_ringRadius, _ringRadius * _ringThickness);
            _ringMesh.name = "SmokeGrenadeRing";
            var ring = new GameObject("Ring", typeof(MeshFilter), typeof(MeshRenderer));
            ring.transform.SetParent(transform, false);
            ring.GetComponent<MeshFilter>().sharedMesh = _ringMesh;
            ring.GetComponent<MeshRenderer>().sharedMaterial = _ringMaterial;

            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Ball";
            Destroy(ball.GetComponent<Collider>());
            ball.transform.SetParent(transform, false);
            ball.transform.localScale = Vector3.one * _ballSize;
            ball.GetComponent<MeshRenderer>().sharedMaterial = _ballMaterial;
            _ball = ball.transform;
        }

        private void OnDestroy()
        {
            Landed = null;
            if (_ringMesh != null)
            {
                Destroy(_ringMesh);
            }

            if (_ringMaterial != null)
            {
                Destroy(_ringMaterial);
            }

            if (_ballMaterial != null)
            {
                Destroy(_ballMaterial);
            }
        }

        private void Update()
        {
            if (!_flying)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _flightSeconds);
            Vector3 position = Vector3.Lerp(_from, _to, t);
            position.y += _peakHeight * 4f * t * (1f - t);
            _ball.position = position;

            if (t >= 1f)
            {
                _flying = false;
                Landed?.Invoke(this);
            }
        }

        private static Mesh BuildRing(float outer, float inner)
        {
            var vertices = new Vector3[RingSegments * 2];
            var triangles = new int[RingSegments * 6];
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / RingSegments;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i * 2] = direction * outer;
                vertices[i * 2 + 1] = direction * inner;

                int next = (i + 1) % RingSegments;
                int o0 = i * 2;
                int i0 = i * 2 + 1;
                int o1 = next * 2;
                int i1 = next * 2 + 1;
                int t = i * 6;

                // Wound clockwise seen from above, so the ring faces the sky.
                triangles[t] = o0;
                triangles[t + 1] = o1;
                triangles[t + 2] = i0;
                triangles[t + 3] = i0;
                triangles[t + 4] = o1;
                triangles[t + 5] = i1;
            }

            var mesh = new Mesh { vertices = vertices, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
