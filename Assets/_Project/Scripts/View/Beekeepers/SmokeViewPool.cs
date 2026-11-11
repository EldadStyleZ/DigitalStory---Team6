using System;
using System.Collections.Generic;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.Pool;

namespace DigitalStory.View
{
    /// <summary>Turns SmokeField's events into pooled grenade and cloud views, so a long fight makes no garbage.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Smoke View Pool")]
    public sealed class SmokeViewPool : MonoBehaviour
    {
        [SerializeField, Tooltip("How many grenade views to make up front.")]
        private int _grenadeCapacity = 4;

        [SerializeField, Tooltip("How many cloud views to make up front.")]
        private int _cloudCapacity = 4;

        private SmokeField _field;
        private SmokeGrenadeView _grenadePrefab;
        private SmokeCloudView _cloudPrefab;
        private ObjectPool<SmokeGrenadeView> _grenades;
        private ObjectPool<SmokeCloudView> _clouds;
        private Dictionary<SmokeCloud, SmokeCloudView> _cloudViews;
        private Action<SmokeGrenadeView> _onLanded;
        private Action<SmokeCloudView> _onFaded;
        private Action<Vector3, Vector3, float> _onGrenadeThrown;
        private Action<SmokeCloud> _onCloudFormed;
        private Action<SmokeCloud> _onCloudEnded;

        /// <summary>Starts listening to <paramref name="field"/>. Call once, from the composition root.</summary>
        public void Initialize(SmokeField field, SmokeGrenadeView grenadePrefab, SmokeCloudView cloudPrefab)
        {
            if (_field != null)
            {
                throw new InvalidOperationException("SmokeViewPool is already initialized.");
            }

            _field = field ?? throw new ArgumentNullException(nameof(field));
            _grenadePrefab = grenadePrefab != null ? grenadePrefab : throw new ArgumentNullException(nameof(grenadePrefab));
            _cloudPrefab = cloudPrefab != null ? cloudPrefab : throw new ArgumentNullException(nameof(cloudPrefab));

            _cloudViews = new Dictionary<SmokeCloud, SmokeCloudView>(SmokeField.Capacity);
            _onLanded = ReleaseGrenade;
            _onFaded = ReleaseCloud;
            _onGrenadeThrown = OnGrenadeThrown;
            _onCloudFormed = OnCloudFormed;
            _onCloudEnded = OnCloudEnded;

            _grenades = new ObjectPool<SmokeGrenadeView>(
                CreateGrenade,
                view => view.gameObject.SetActive(true),
                view => view.gameObject.SetActive(false),
                view => Destroy(view.gameObject),
                collectionCheck: false,
                defaultCapacity: _grenadeCapacity,
                maxSize: SmokeField.Capacity);
            _clouds = new ObjectPool<SmokeCloudView>(
                CreateCloud,
                view => view.gameObject.SetActive(true),
                view => view.gameObject.SetActive(false),
                view => Destroy(view.gameObject),
                collectionCheck: false,
                defaultCapacity: _cloudCapacity,
                maxSize: SmokeField.Capacity);

            _field.GrenadeThrown += _onGrenadeThrown;
            _field.CloudFormed += _onCloudFormed;
            _field.CloudEnded += _onCloudEnded;
        }

        private void OnDestroy()
        {
            if (_field != null)
            {
                _field.GrenadeThrown -= _onGrenadeThrown;
                _field.CloudFormed -= _onCloudFormed;
                _field.CloudEnded -= _onCloudEnded;
            }

            _grenades?.Dispose();
            _clouds?.Dispose();
        }

        private SmokeGrenadeView CreateGrenade()
        {
            SmokeGrenadeView view = Instantiate(_grenadePrefab, transform);
            view.Landed += _onLanded;
            return view;
        }

        private SmokeCloudView CreateCloud()
        {
            SmokeCloudView view = Instantiate(_cloudPrefab, transform);
            view.Faded += _onFaded;
            return view;
        }

        private void OnGrenadeThrown(Vector3 from, Vector3 to, float flightSeconds)
        {
            _grenades.Get().Launch(from, to, flightSeconds);
        }

        private void OnCloudFormed(SmokeCloud cloud)
        {
            // The field reuses cloud objects, so an old view may still be fading for this one. Let it finish alone.
            if (_cloudViews.TryGetValue(cloud, out SmokeCloudView old))
            {
                old.Hide();
            }

            SmokeCloudView view = _clouds.Get();
            _cloudViews[cloud] = view;
            view.Show(cloud);
        }

        private void OnCloudEnded(SmokeCloud cloud)
        {
            if (_cloudViews.TryGetValue(cloud, out SmokeCloudView view))
            {
                _cloudViews.Remove(cloud);
                view.Hide();
            }
        }

        private void ReleaseGrenade(SmokeGrenadeView view)
        {
            _grenades.Release(view);
        }

        private void ReleaseCloud(SmokeCloudView view)
        {
            _clouds.Release(view);
        }
    }
}
