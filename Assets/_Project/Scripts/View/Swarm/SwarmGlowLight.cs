using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// The light the glowing swarm casts on the ground, the trees and the beekeepers around it: one point light that
    /// follows the swarm in its colour, brighter and wider the stronger the swarm. One light stands in for thousands of
    /// glowing wasps, which could never each have their own. It eases toward each new place, colour and strength,
    /// so it never flickers.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    [AddComponentMenu("DigitalStory/Swarm Glow Light")]
    public sealed class SwarmGlowLight : MonoBehaviour
    {
        // Share of the way to the new value the light covers in a second, roughly. Its place follows faster than its
        // colour and strength, so it keeps up with the core while a change of song still fades in.
        private const float FollowPerSecond = 5f;
        private const float MoveFollowPerSecond = 12f;

        // A weak swarm still lights a small pool around itself, rather than shrinking to a point.
        private const float LeastRangeShare = 0.4f;

        private Light _light;
        private SwarmConfig _config;
        private bool _hasShown;
        private Vector3 _wantedPosition;
        private Color _wantedColor;
        private float _wantedStrength;
        private Vector3 _position;
        private Color _color;
        private float _strength;

        private void LateUpdate()
        {
            if (!_hasShown)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            float follow = 1f - Mathf.Exp(-FollowPerSecond * deltaTime);
            float moveFollow = 1f - Mathf.Exp(-MoveFollowPerSecond * deltaTime);
            _position = Vector3.Lerp(_position, _wantedPosition, moveFollow);
            _color = Color.Lerp(_color, _wantedColor, follow);
            _strength = Mathf.Lerp(_strength, _wantedStrength, follow);
            Apply();
        }

        /// <summary>
        /// Supplies the light's range and brightness at full strength (SwarmConfig.LightRange and LightIntensity), and
        /// makes this object's Light a point light without shadows, lit per pixel so it shows on the large ground.
        /// The light stays enabled at zero intensity until the first Show. Calling again (a restart) starts it over.
        /// </summary>
        public void Initialize(SwarmConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _config = config;
            if (_light == null)
            {
                _light = GetComponent<Light>();
            }

            _light.type = LightType.Point;
            _light.shadows = LightShadows.None;
            _light.renderMode = LightRenderMode.ForcePixel;
            _light.intensity = 0f;
            _light.enabled = true; // Never switched off: a spent swarm sits at zero, so no frame pays for the first use.
            _hasShown = false;
        }

        /// <summary>
        /// Where the swarm's light shines from, its colour (the swarm's current colour), and its strength from 0 to 1,
        /// such as the share of the most wasps the swarm holds. Call each frame. The light eases toward these, except on
        /// the first call after Initialize, which it takes at once.
        /// </summary>
        /// <exception cref="InvalidOperationException">Initialize has not been called.</exception>
        public void Show(Vector3 position, Color color, float strength01)
        {
            if (_config == null)
            {
                throw new InvalidOperationException($"{name}: call SwarmGlowLight.Initialize before Show.");
            }

            _wantedPosition = position;
            _wantedColor = color;
            _wantedStrength = Mathf.Clamp01(strength01);

            if (!_hasShown)
            {
                _position = _wantedPosition;
                _color = _wantedColor;
                _strength = _wantedStrength;
                _hasShown = true;
                Apply();
            }
        }

        private void Apply()
        {
            float intensity = _config.LightIntensity * _strength;
            transform.position = _position;
            _light.color = _color;
            _light.intensity = intensity;
            _light.range = _config.LightRange * Mathf.Lerp(LeastRangeShare, 1f, _strength);
        }
    }
}
