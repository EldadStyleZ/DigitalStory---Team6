using System;
using DigitalStory.Data;
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

        // Plan section Y: each number pops in (from its start scale, up past 1, settles at 1), and a kill chain's milestones
        // stamp x3 / x6 / x10. The pop's numbers come from FeelConfig.
        private const string StampClass = "chain-stamp";
        private float _popSeconds = 0.15f;
        private float _popScale = 1.2f;
        private float _popStartScale = 0.6f;
        private float _killPopExtra = 1.33f;
        private HitFeedbackView _chain;
        private Label _stamp;
        private string[] _stampTexts;
        private int[] _stampCounts;
        private float _stampAge = -1f;

        /// <summary>The stamp's text while it shows, or null. For tests.</summary>
        public string StampText => _stampAge >= 0f ? _stamp.text : null;

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

            HitNumberText.Of(1f); // Its strings are made here, once, not on the first hit.
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
            _stampAge = -1f;
            if (_stamp != null)
            {
                _stamp.style.display = DisplayStyle.None;
            }

            _session.SongLanded += OnSongLanded;
            _isReady = true;
        }

        /// <summary>
        /// Stamps the kill chain's milestones from now on (x3, x6, x10) and pops the numbers by the feel director's FeelConfig.
        /// Call after Initialize.
        /// </summary>
        /// <exception cref="ArgumentNullException">The feedback view is missing.</exception>
        public void ListenToKillChain(HitFeedbackView feedback)
        {
            if (feedback == null)
            {
                throw new ArgumentNullException(nameof(feedback));
            }

            if (_chain != null)
            {
                _chain.KillChained -= OnKillChained;
            }

            _chain = feedback;
            _popSeconds = feedback.Feel.NumberPopSeconds;
            _popScale = feedback.Feel.NumberPopScale;
            _popStartScale = feedback.Feel.NumberPopStartScale;
            _killPopExtra = feedback.Feel.KillPopExtra;
            int[] milestones = feedback.Feel.ChainMilestones;
            _stampCounts = milestones;
            _stampTexts = new string[milestones.Length];
            for (int index = 0; index < milestones.Length; index++)
            {
                _stampTexts[index] = "x" + milestones[index].ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            if (_stamp == null)
            {
                _stamp = new Label { name = "chain-stamp", pickingMode = PickingMode.Ignore };
                _stamp.AddToClassList(StampClass);
                _stamp.style.display = DisplayStyle.None;
                _layer.Add(_stamp);
            }

            _chain.KillChained += OnKillChained;
        }

        private void OnKillChained(KillChainStep step)
        {
            if (step.Milestone <= 0 || _stamp == null)
            {
                return;
            }

            for (int index = 0; index < _stampCounts.Length; index++)
            {
                if (_stampCounts[index] == step.Milestone)
                {
                    _stamp.text = _stampTexts[index];
                    _stampAge = 0f;
                    _stamp.style.display = DisplayStyle.Flex;
                    StepStamp(0f);
                    return;
                }
            }
        }

        // The stamp lands from its start scale to 1, holds, and fades over the second half of its time.
        private void StepStamp(float deltaTime)
        {
            if (_stampAge < 0f)
            {
                return;
            }

            FeelConfig feel = _chain.Feel;
            _stampAge += deltaTime;
            if (_stampAge >= feel.StampSeconds)
            {
                _stampAge = -1f;
                _stamp.style.display = DisplayStyle.None;
                return;
            }

            float land = Mathf.Clamp01(_stampAge / feel.StampLandSeconds);
            float scale = Mathf.Lerp(feel.StampStartScale, 1f, 1f - ((1f - land) * (1f - land)));
            _stamp.style.scale = new Scale(new Vector3(scale, scale, 1f));
            _stamp.style.opacity = Mathf.Clamp01(2f - (2f * _stampAge / feel.StampSeconds));
        }

        // 0.6 up to the peak over the first half of the pop, then down to 1 over the second.
        private float PopScale(float age, bool isKill)
        {
            if (age >= _popSeconds)
            {
                return 1f;
            }

            float peak = isKill ? 1f + ((_popScale - 1f) * _killPopExtra) : _popScale;
            float t = age / _popSeconds;
            return t < 0.5f ? Mathf.Lerp(_popStartScale, peak, t * 2f) : Mathf.Lerp(peak, 1f, (t - 0.5f) * 2f);
        }

        private void LateUpdate()
        {
            if (!_isReady)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            StepStamp(deltaTime);
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
                if (_ages[index] - deltaTime < _popSeconds)
                {
                    float pop = PopScale(_ages[index], _kinds[index] == 1);
                    label.style.scale = new Scale(new Vector3(pop, pop, 1f));
                }
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

            if (_chain != null)
            {
                _chain.KillChained -= OnKillChained;
                _chain = null;
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

            label.text = kind == 2 ? BlockedText : HitNumberText.Of(hit.Damage);
            _origins[slot] = hit.Position + (Vector3.up * _startHeight);
            _ages[slot] = 0f;
            _drift[slot] = (((slot * 7) % 5) - 2) * (_spreadPixels / 2f);
            label.style.opacity = 1f;
            float start = PopScale(0f, kind == 1);
            label.style.scale = new Scale(new Vector3(start, start, 1f));
            label.style.display = DisplayStyle.Flex;
        }

        private static void SetKind(Label label, int kind)
        {
            label.EnableInClassList(HitClass, kind == 0);
            label.EnableInClassList(KillClass, kind == 1);
            label.EnableInClassList(BlockedClass, kind == 2);
        }

    }
}
