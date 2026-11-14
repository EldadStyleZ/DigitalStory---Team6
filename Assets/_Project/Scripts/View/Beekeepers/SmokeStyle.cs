using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// The stylized smoke's particle settings, shared by the cloud and the grenade's trail: big billowing puffs that burst
    /// out, roll upward and dissolve (their alpha is what StylizedSmoke.shader keeps of them).
    /// </summary>
    public static class SmokeStyle
    {
        private static readonly List<ParticleSystemVertexStream> Streams = new List<ParticleSystemVertexStream>
        {
            ParticleSystemVertexStream.Position,
            ParticleSystemVertexStream.Color,
            ParticleSystemVertexStream.UV,
            ParticleSystemVertexStream.StableRandomX,
        };

        private static Gradient s_dissolve;

        /// <summary>Sends each puff's own random number with it, so every puff has its own lumpy edge.</summary>
        public static void UseStreams(ParticleSystemRenderer renderer)
        {
            renderer.SetActiveVertexStreams(Streams);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            // A fixed order: sorting by distance swaps puffs every frame as the swarm moves through, and that flickers.
            renderer.sortMode = ParticleSystemSortMode.OldestInFront;
        }

        /// <summary>A puff forms from wisps, holds, then breaks up again.</summary>
        public static Gradient Dissolve()
        {
            if (s_dissolve == null)
            {
                s_dissolve = new Gradient();
                s_dissolve.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.85f, 0.87f, 0.95f), 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.85f, 0.6f), new GradientAlphaKey(0f, 1f) });
            }

            return s_dissolve;
        }

        public static void ConfigureCloud(ParticleSystem particles, float radius, float lifetime, float perSquareMetre, float sizeShare)
        {
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.duration = 5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.8f, lifetime * 1.25f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.7f);
            main.startSize = new ParticleSystem.MinMaxCurve(radius * sizeShare * 0.65f, radius * sizeShare * 1.15f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = Color.white;
            main.gravityModifier = -0.035f;
            main.maxParticles = 300;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = Mathf.PI * radius * radius * perSquareMetre * 0.8f;
            // The pop when the grenade lands: a ring of puffs all at once.
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(radius * 7f, 8f, 40f)) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = radius * 0.85f;
            shape.radiusThickness = 1f;
            shape.position = Vector3.zero;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            ParticleSystem.CollisionModule collision = particles.collision;
            collision.enabled = false;

            ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = Dissolve();

            // Puffs swell as they roll up, and slow down as they spread.
            ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(0.35f, 0.95f), new Keyframe(1f, 1.2f)));

            ParticleSystem.LimitVelocityOverLifetimeModule limit = particles.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 0.35f;
            limit.dampen = 0.08f;

            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.2f;
            noise.quality = ParticleSystemNoiseQuality.Medium;

            ParticleSystem.RotationOverLifetimeModule rotation = particles.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        }

        /// <summary>The grenade's trail: small puffs left behind it in the air, which swell and dissolve.</summary>
        public static ParticleSystem AddTrail(Transform ball, Material material)
        {
            var trail = new GameObject("Smoke Trail", typeof(ParticleSystem));
            trail.transform.SetParent(ball, false);
            var particles = trail.GetComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = -0.05f;
            main.maxParticles = 80;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 7f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;

            ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = Dissolve();

            ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1.6f)));

            var renderer = trail.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            UseStreams(renderer);
            return particles;
        }
    }
}
