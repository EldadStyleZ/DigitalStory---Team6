using System;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// Small numbers that rise from every hit a song lands: white for a hit, larger and warm for a kill, blue "blocked" for a hit a shield
    /// takes. The labels are made once and reused; their texts come from a table of whole numbers made once, so a hit builds no string.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Damage Numbers View")]
    public sealed class DamageNumbersView : MonoBehaviour
    {
        private const string HitClass = "dmg--hit";
        private const string KillClass = "dmg--kill";
        private const string BlockedClass = "dmg--blocked";
        private const int CachedNumbers = 1000;
        private const string BlockedText = "blocked";

        [SerializeField, Min(1), Tooltip("How many numbers can show at once. The oldest is reused when all are busy.")]
        private int _poolSize = 24;

        [SerializeField, Min(0.1f), Tooltip("Seconds a number rises and fades.")]
        private float _lifeSeconds = 0.7f;

        [SerializeField, Min(0f), Tooltip("Pixels a number rises over its life.")]
        private float _risePixels = 46f;

        [SerializeField, Min(0f), Tooltip("Metres above the hit where a number starts.")]
        private float _startHeight = 1.6f;

        [SerializeField, Min(0f), Tooltip("Pixels a number may drift sideways, so numbers from one burst do not sit on each other.")]
        private float _spreadPixels = 18f;

        private static string[] _numberTable;

        private HiveDefenseSession _session;
        private Camera _camera;
        private VisualElement _layer;
        private Label[] _labels;
        private Vector3[] _origins;
        private float[] _ages;
        private float[] _drift;
        private int[] _kinds;
        private int _next;
        private bool _isReady;

        /// <summary>How many numbers are showing now. For tests.</summary>
        public int ActiveCount
        {
            get
            {
                int count = 0;
                for (int index = 0; _ages != null && index < _ages.Length; index++)
                {
                    if (_ages[index] >= 0f)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>Makes the pool of labels on a layer over the HUD and starts listening. Call once, after the HUD tree exists.</summary>
        /// <exception cref="ArgumentNullException">An argument is missing.</exception>
        public void Initialize(HiveDefenseSession session, Camera camera, VisualElement root)
        {
            Release();
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _camera = camera != null ? camera : throw new ArgumentNullException(nameof(camera));
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            BuildTable();
            if (_layer == null || _layer.parent != root)
            {
                _layer?.RemoveFromHierarchy();
                _layer = new VisualElement { name = "damage-layer", pickingMode = PickingMode.Ignore };
                _layer.AddToClassList("damage-layer");
                root.Add(_layer);
                _labels = new Label[_poolSize];
                _origins = new Vector3[_poolSize];
                _ages = new float[_poolSize];
                _drift = new float[_poolSize];
                _kinds = new int[_poolSize];
                for (int index = 0; index < _poolSize; index++)
                {
                    var label = new Label { pickingMode = PickingMode.Ignore };
                    label.AddToClassList("dmg");
                    label.style.display = DisplayStyle.None;
                    _layer.Add(label);
                    _labels[index] = label;
                    _ages[index] = -1f;
                    _kinds[index] = -1;
                }
            }

            _next = 0;
            _session.SongLanded += OnSongLanded;
            _isReady = true;
        }

        private void LateUpdate()
        {
            if (!_isReady)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            Rect area = _layer.layout;
            bool hasArea = area.width > 0f && area.height > 0f;
            for (int index = 0; index < _labels.Length; index++)
            {
                if (_ages[index] < 0f)
                {
                    continue;
                }

                _ages[index] += deltaTime;
                float t = _ages[index] / _lifeSeconds;
                Label label = _labels[index];
                if (t >= 1f)
                {
                    _ages[index] = -1f;
                    label.style.display = DisplayStyle.None;
                    continue;
                }

                // The layer covers the whole panel, as the camera's view covers the screen, so a share of the view is the same share
                // of the layer, whatever size the panel draws at. A point behind the camera shows nothing.
                Vector3 view = _camera.WorldToViewportPoint(_origins[index]);
                if (!hasArea || view.z <= 0f)
                {
                    label.style.opacity = 0f;
                    continue;
                }

                float rise = 1f - ((1f - t) * (1f - t));
                label.style.left = (view.x * area.width) + (_drift[index] * rise);
                label.style.top = ((1f - view.y) * area.height) - (_risePixels * rise);
                label.style.opacity = t < 0.6f ? 1f : 1f - ((t - 0.6f) / 0.4f);
            }
        }

        private void OnDestroy()
        {
            Release();
        }

        private void Release()
        {
            _isReady = false;
            if (_session != null)
            {
                _session.SongLanded -= OnSongLanded;
            }
        }

        private void OnSongLanded(SongHit hit)
        {
            int slot = _next;
            _next = (_next + 1) % _labels.Length;

            Label label = _labels[slot];
            int kind = hit.IsBlocked ? 2 : (hit.IsKill ? 1 : 0);
            if (_kinds[slot] != kind)
            {
                SetKind(label, kind);
                _kinds[slot] = kind;
            }

            label.text = kind == 2 ? BlockedText : TextOf(hit.Damage);
            _origins[slot] = hit.Position + (Vector3.up * _startHeight);
            _ages[slot] = 0f;
            _drift[slot] = (((slot * 7) % 5) - 2) * (_spreadPixels / 2f);
            label.style.opacity = 1f;
            label.style.display = DisplayStyle.Flex;
        }

        private static void SetKind(Label label, int kind)
        {
            label.EnableInClassList(HitClass, kind == 0);
            label.EnableInClassList(KillClass, kind == 1);
            label.EnableInClassList(BlockedClass, kind == 2);
        }

        private static string TextOf(float damage)
        {
            int whole = Mathf.Max(1, Mathf.RoundToInt(damage));
            return whole < CachedNumbers ? _numberTable[whole] : _numberTable[CachedNumbers - 1];
        }

        // The table is made once for the whole game: one string for each whole number a hit can show. The last one reads "999+".
        private static void BuildTable()
        {
            if (_numberTable != null)
            {
                return;
            }

            _numberTable = new string[CachedNumbers];
            for (int number = 0; number < CachedNumbers - 1; number++)
            {
                _numberTable[number] = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            _numberTable[CachedNumbers - 1] = (CachedNumbers - 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + "+";
        }
    }
}
