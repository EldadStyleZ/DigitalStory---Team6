using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// The light of a grenade going off: a hot orange flash that lights the grass, the trees and the swarm round it, then
    /// flickers down to nothing, and the explosion effect with it is cleared away.
    /// </summary>
    /// <remarks>
    /// Plan section AD: each flash is a pixel light reaching 11 m, which draws every grass cell, beekeeper and prop in its
    /// reach again; at a hive under a wave of 39, several shone at once and cost about 3.4 ms a frame. At most
    /// <see cref="MostLightsAtOnce"/> shine together now; a grenade past that still bursts, without its light.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ExplosionFlash : MonoBehaviour
    {
        private const string EffectPath = "Effects/GrenadeExplosion";
        private const float FlashSeconds = 0.9f;
        private const float EffectSeconds = 6f;
        private const float PeakIntensity = 7f;
        private const float Range = 11f;
        private const int MostLightsAtOnce = 2;

        private static GameObject s_effect;
        private static bool s_looked;
        private static LightBudget s_lights = new LightBudget(MostLightsAtOnce);

        private Light _light;
        private bool _holdsLight;
        private float _elapsed;

        /// <summary>Sets off the explosion, with its light, at a point on the ground.</summary>
        public static void Burst(Vector3 point)
        {
            if (!s_looked)
            {
                s_effect = Resources.Load<GameObject>(EffectPath);
                s_looked = true;
            }

            var root = new GameObject("Grenade Explosion");
            root.transform.position = point;
            if (s_effect != null)
            {
                GameObject effect = Instantiate(s_effect, point, Quaternion.identity, root.transform);
                foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>())
                {
                    if (particles.transform.parent == effect.transform || particles.transform == effect.transform)
                    {
                        particles.Play(true);
                    }
                }
            }

            if (!s_lights.TryClaim())
            {
                Destroy(root, EffectSeconds);
                return;
            }

            var lightObject = new GameObject("Flash");
            lightObject.transform.SetParent(root.transform, false);
            lightObject.transform.localPosition = Vector3.up * 1.5f;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.55f, 0.22f);
            light.range = Range;
            light.intensity = PeakIntensity;
            light.renderMode = LightRenderMode.ForcePixel;
            light.shadows = LightShadows.None;

            var flash = root.AddComponent<ExplosionFlash>();
            flash._light = light;
            flash._holdsLight = true;
            Destroy(root, EffectSeconds);
        }

        private void Update()
        {
            if (_light == null)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / FlashSeconds);
            // A fast rise, then a falling flicker.
            float rise = Mathf.Clamp01(_elapsed / 0.06f);
            float fall = (1f - t) * (1f - t);
            float flicker = 0.85f + (0.15f * Mathf.Sin(_elapsed * 60f));
            _light.intensity = PeakIntensity * rise * fall * flicker;
            if (t >= 1f)
            {
                ReleaseLight();
            }
        }

        // A scene unloaded mid-flash takes its light with it.
        private void OnDestroy()
        {
            ReleaseLight();
        }

        // The budget outlives a scene (it is static); nothing of a play session may carry into the next.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_lights = new LightBudget(MostLightsAtOnce);
            s_effect = null;
            s_looked = false;
        }

        // The claim is kept apart from the light object, which a scene unload may have destroyed first, so the budget never
        // leaks into the next match.
        private void ReleaseLight()
        {
            if (!_holdsLight)
            {
                return;
            }

            _holdsLight = false;
            s_lights.Release();
            if (_light != null)
            {
                Destroy(_light.gameObject);
            }

            _light = null;
        }
    }
}
