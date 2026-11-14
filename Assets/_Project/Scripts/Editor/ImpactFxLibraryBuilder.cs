using System;
using System.Text;
using DigitalStory.Data;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DigitalStory.Editor
{
    /// <summary>
    /// Makes the local <see cref="ImpactFxLibrary"/> of plan section AB from the Epic Toon FX pack, for someone who owns the
    /// pack and has imported it. The pack and everything made from it stay in untracked folders (the repo's local exclude
    /// file lists them), because the Asset Store licence forbids publishing the pack and the repo is public. Without the pack
    /// it does nothing, so the game keeps its own effects.
    /// </summary>
    /// <remarks>
    /// Each moment takes one of the pack's prefabs, found by its path, and saves a cheap local copy: no lights, sounds or
    /// scripts, no world collision, no particle lights, a capped particle count, no looping, scaled with its transform. The
    /// recipe below holds the choice and its look values; it is the generator of a local asset, as the scene builder is of
    /// the configs. Works from the menu and from
    /// <c>-batchmode -executeMethod DigitalStory.Editor.ImpactFxLibraryBuilder.Build</c>.
    /// </remarks>
    public static class ImpactFxLibraryBuilder
    {
        private const string PackFolder = "Assets/Epic Toon FX";
        private const string PackPrefabFolder = PackFolder + "/Prefabs";
        private const string PackDemoFolder = PackFolder + "/Demo";
        private const string LocalFolder = "Assets/EpicToonFXLocal";
        private const string LocalPrefabFolder = LocalFolder + "/Prefabs";
        private const string LocalResourcesFolder = LocalFolder + "/Resources";
        private const string LibraryPath = LocalResourcesFolder + "/" + ImpactFxLibrary.ResourceName + ".asset";

        // A burst in the pack has tens of particles; its systems allow 1,000 each. The cap only guards against a surprise.
        private const int MostParticlesPerSystem = 200;

        // A hit below this damage shows no effect: the Sting's squeezes deal 0.3 to 0.6 many times a second.
        private const float HitLeastDamage = 1f;

        // Chosen from pictures of 32 of the pack's bursts on the game's ground (plan section AB). Every one layers over the
        // game's own effect, which carries the information (the flash on the struck beekeeper, the numbers, the reach rings):
        // a gold burst at a beekeeper's chest on a hit, orange toon stars on a kill, a gold level-up column at a nest's tree
        // as it bursts, a fire burst with a ring of smoke where the Meteor lands, and a shock ring of dust round the giant's
        // feet on its slam. Sizes are for the game's camera, 14 m away; the Meteor's and the slam's grow with their reach.
        private static readonly Recipe[] Recipes =
        {
            new Recipe(ImpactFxKind.Hit, "Combat/Brawling/RoundHit/RoundHitYellow.prefab", 1.5f, 12, 0.04f, 1f),
            new Recipe(ImpactFxKind.Death, "Combat/Explosions/StarExplosion/StarExplosionOrange.prefab", 1.8f, 8, 0f, 1f),
            new Recipe(ImpactFxKind.NestBurst, "Interactive/Level Up/Cylinder/LevelupCylinderYellow.prefab", 1.5f, 2, 0f, 0.05f),
            new Recipe(ImpactFxKind.MeteorLanding, "Combat/Explosions/GrenadeExplosion/GrenadeExplosionFire.prefab", 1.6f, 3, 0f, 0.05f),
            new Recipe(ImpactFxKind.GiantSlam, "Combat/Brawling/Toon/ToonBodySlam.prefab", 1.6f, 3, 0f, 0.05f),
        };

        /// <summary>Makes or refreshes the local library from the pack. Does nothing when the pack is not in the project.</summary>
        [MenuItem("DigitalStory/Impact FX/Make Local Library From Epic Toon FX")]
        public static void Build()
        {
            try
            {
                if (!AssetDatabase.IsValidFolder(PackFolder))
                {
                    Debug.Log($"[F010] {PackFolder} is not in this project, so no impact library was made; the game keeps its own effects.");
                    return;
                }

                CreateFolder(LocalFolder);
                CreateFolder(LocalPrefabFolder);
                CreateFolder(LocalResourcesFolder);
                ImpactFxLibrary library = LoadOrCreateLibrary();
                library.SetHitLeastDamage(HitLeastDamage);
                var report = new StringBuilder("[F010] Impact library made at ").Append(LibraryPath).Append(':');
                foreach (Recipe recipe in Recipes)
                {
                    library.SetSlot(recipe.Kind, MakeSlot(recipe, report));
                }

                EditorUtility.SetDirty(library);
                AssetDatabase.SaveAssets();
                Debug.Log(report.ToString());
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        /// <summary>
        /// Deletes the pack's Demo folder from this project, through the AssetDatabase. Its scripts need Unity UI, which this
        /// project does not have, so they stop the whole project compiling; the prefabs need nothing in it. The pack is
        /// untracked, so nothing committed changes. Does nothing when the folder is not there. In batch mode, run it with
        /// -ignorecompilererrors, since the Demo's errors are what it removes.
        /// </summary>
        [MenuItem("DigitalStory/Impact FX/Remove Epic Toon FX Demo")]
        public static void RemovePackDemo()
        {
            if (!AssetDatabase.IsValidFolder(PackDemoFolder))
            {
                Debug.Log($"[F010] {PackDemoFolder} is not in this project; nothing to remove.");
                return;
            }

            if (!AssetDatabase.DeleteAsset(PackDemoFolder))
            {
                Debug.LogError($"[F010] Could not delete {PackDemoFolder}.");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                return;
            }

            Debug.Log($"[F010] Deleted {PackDemoFolder}: its scripts need Unity UI, and the prefabs need none of it.");
        }

        private static ImpactFxLibrary LoadOrCreateLibrary()
        {
            var library = AssetDatabase.LoadAssetAtPath<ImpactFxLibrary>(LibraryPath);
            if (library != null)
            {
                return library;
            }

            library = ScriptableObject.CreateInstance<ImpactFxLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
            return library;
        }

        // A cheap local copy of the recipe's prefab and the slot that plays it; an empty slot when the pack lacks the prefab.
        private static ImpactFxSlot MakeSlot(Recipe recipe, StringBuilder report)
        {
            string source = PackPrefabFolder + "/" + recipe.PackPath;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(source);
            if (prefab == null)
            {
                Debug.LogWarning($"[F010] {source} is not in the pack, so {recipe.Kind} keeps only the game's own effect.");
                report.Append("\n  ").Append(recipe.Kind).Append(": none (missing ").Append(source).Append(')');
                return default;
            }

            GameObject copy = Object.Instantiate(prefab);
            try
            {
                int stripped = Strip(copy);
                ParticleSystem[] systems = copy.GetComponentsInChildren<ParticleSystem>(true);
                foreach (ParticleSystem system in systems)
                {
                    MakeCheap(system);
                }

                string path = $"{LocalPrefabFolder}/Impact{recipe.Kind}.prefab";
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(copy, path, out bool isSaved);
                if (!isSaved || saved == null)
                {
                    throw new InvalidOperationException($"Could not save {path}.");
                }

                report.Append("\n  ").Append(recipe.Kind).Append(": ").Append(recipe.PackPath).Append(", ").Append(systems.Length)
                    .Append(" systems, ").Append(stripped).Append(" components stripped, scale ").Append(recipe.Scale)
                    .Append(", at most ").Append(recipe.MostAtOnce).Append(" at once");
                return new ImpactFxSlot(saved, recipe.Scale, recipe.MostAtOnce, recipe.LeastGapSeconds, recipe.HeightMetres);
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
        }

        // Scripts first, since the pack's sound script requires its AudioSource. Returns how many components went.
        private static int Strip(GameObject copy)
        {
            int count = 0;
            foreach (Transform part in copy.GetComponentsInChildren<Transform>(true))
            {
                count += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(part.gameObject);
            }

            count += DestroyAll<MonoBehaviour>(copy);
            count += DestroyAll<Light>(copy);
            count += DestroyAll<AudioSource>(copy);
            return count;
        }

        private static int DestroyAll<T>(GameObject copy) where T : Component
        {
            T[] components = copy.GetComponentsInChildren<T>(true);
            foreach (T component in components)
            {
                Object.DestroyImmediate(component);
            }

            return components.Length;
        }

        private static void MakeCheap(ParticleSystem system)
        {
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.None;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = Mathf.Min(main.maxParticles, MostParticlesPerSystem);
            ParticleSystem.CollisionModule collision = system.collision;
            collision.enabled = false;
            ParticleSystem.LightsModule lights = system.lights;
            lights.enabled = false;
            var particleRenderer = system.GetComponent<ParticleSystemRenderer>();
            if (particleRenderer != null)
            {
                particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                particleRenderer.receiveShadows = false;
            }
        }

        private static void CreateFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        private readonly struct Recipe
        {
            public Recipe(ImpactFxKind kind, string packPath, float scale, int mostAtOnce, float leastGapSeconds, float heightMetres)
            {
                Kind = kind;
                PackPath = packPath;
                Scale = scale;
                MostAtOnce = mostAtOnce;
                LeastGapSeconds = leastGapSeconds;
                HeightMetres = heightMetres;
            }

            public ImpactFxKind Kind { get; }
            public string PackPath { get; }
            public float Scale { get; }
            public int MostAtOnce { get; }
            public float LeastGapSeconds { get; }
            public float HeightMetres { get; }
        }
    }
}
