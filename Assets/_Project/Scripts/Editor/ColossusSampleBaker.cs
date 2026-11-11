using System;
using System.Collections.Generic;
using DigitalStory.Data;
using UnityEditor;
using UnityEngine;

namespace DigitalStory.Editor
{
    /// <summary>
    /// Bakes where each wasp sits on the Colossus (spec F-010, section 9.5) from a rigged model into a ColossusSampleSet.
    /// </summary>
    /// <remarks>
    /// An imported mesh cannot be read at runtime, and the artist's import settings stay as they are, so the sampling
    /// happens here. Unity lets Editor code read any mesh's arrays outside Play Mode, whatever its Read/Write setting.
    /// </remarks>
    public static class ColossusSampleBaker
    {
        // The same seed every time, so rebuilding the scene gives the same samples and the asset does not churn.
        private const int Seed = 9505;
        private const int VerticesPerTriangle = 3;
        private const int NotKept = -1;

        // These must match the private serialized field names in ColossusSampleSet.
        private const string MeshNameField = "_meshName";
        private const string RendererBoneCountField = "_rendererBoneCount";
        private const string RendererBonesField = "_rendererBones";
        private const string BoneNamesField = "_boneNames";
        private const string BoneIndicesField = "_boneIndices";
        private const string BoneOffsetsField = "_boneOffsets";
        private const string Heights01Field = "_heights01";

        /// <summary>
        /// Picks <paramref name="sampleCount"/> points on the body, weighted by triangle area, ties each to the dominant
        /// bone of its triangle's first vertex, and replaces the samples in <paramref name="target"/>. Undoable.
        /// Call it outside Play Mode, with the model standing upright.
        /// </summary>
        /// <param name="renderer">The puppet's body: a SkinnedMeshRenderer with bones and skin weights.</param>
        /// <param name="sampleCount">How many samples to take. Pass SwarmConfig.MaxWasps, so every wasp has its own place.</param>
        /// <param name="target">The asset to fill.</param>
        public static void Bake(SkinnedMeshRenderer renderer, int sampleCount, ColossusSampleSet target)
        {
            if (renderer == null)
            {
                throw new ArgumentNullException(nameof(renderer));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (sampleCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleCount), "Take at least one sample.");
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Bake the Colossus samples outside Play Mode: Unity lets Editor code read an imported mesh only outside the game loop.");
            }

            Mesh mesh = renderer.sharedMesh;
            if (mesh == null)
            {
                throw new ArgumentException($"{renderer.name} has no mesh to sample.", nameof(renderer));
            }

            Transform[] bones = renderer.bones;
            Matrix4x4[] bindposes = mesh.bindposes;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            BoneWeight[] weights = mesh.boneWeights;
            CheckRig(renderer, mesh, bones, bindposes, vertices, triangles, weights);

            double[] cumulativeAreas = CumulativeAreas(vertices, triangles);
            Matrix4x4 meshToWorld = renderer.transform.localToWorldMatrix;
            HeightRange(vertices, meshToWorld, out float lowest, out float highest);

            var random = new System.Random(Seed);
            var points = new Vector3[sampleCount];
            var rigBoneOfSample = new int[sampleCount];
            var heights = new float[sampleCount];
            var uses = new int[bones.Length];

            for (int sample = 0; sample < sampleCount; sample++)
            {
                int corner = PickTriangle(cumulativeAreas, random) * VerticesPerTriangle;
                int first = triangles[corner];
                Vector3 point = PointOnTriangle(vertices[first], vertices[triangles[corner + 1]], vertices[triangles[corner + 2]], random);
                int bone = DominantBone(weights[first]);
                if (bone < 0 || bone >= bones.Length || bones[bone] == null)
                {
                    throw new InvalidOperationException(
                        $"{mesh.name}: vertex {first} is skinned to bone {bone}, which {renderer.name} does not have.");
                }

                points[sample] = point;
                rigBoneOfSample[sample] = bone;
                heights[sample] = Mathf.InverseLerp(lowest, highest, meshToWorld.MultiplyPoint3x4(point).y);
                uses[bone]++;
            }

            int[] table = ChooseBones(uses);
            int[] slotOfBone = SlotsOfBones(table, uses, bones);

            var boneIndices = new int[sampleCount];
            var offsets = new Vector3[sampleCount];
            for (int sample = 0; sample < sampleCount; sample++)
            {
                int slot = slotOfBone[rigBoneOfSample[sample]];
                boneIndices[sample] = slot;

                // The bind pose takes a point from mesh space into the bone's space, so the bone carries it as it moves.
                offsets[sample] = bindposes[table[slot]].MultiplyPoint3x4(points[sample]);
            }

            var names = new string[table.Length];
            for (int slot = 0; slot < table.Length; slot++)
            {
                names[slot] = bones[table[slot]].name;
            }

            Write(target, new BakedSamples(mesh.name, bones.Length, table, names, boneIndices, offsets, heights));
        }

        private static void CheckRig(
            SkinnedMeshRenderer renderer,
            Mesh mesh,
            Transform[] bones,
            Matrix4x4[] bindposes,
            Vector3[] vertices,
            int[] triangles,
            BoneWeight[] weights)
        {
            if (bones.Length == 0)
            {
                throw new ArgumentException(
                    $"{renderer.name} has no bones. Use a rigged model, with Optimize Game Objects off in its import settings.", nameof(renderer));
            }

            if (bindposes.Length != bones.Length)
            {
                throw new ArgumentException(
                    $"{mesh.name} has {bindposes.Length} bind poses for {bones.Length} bones, so its points cannot be put in bone space.", nameof(renderer));
            }

            if (weights.Length != vertices.Length)
            {
                throw new ArgumentException($"{mesh.name} has no skin weights, so its points cannot follow the bones.", nameof(renderer));
            }

            if (triangles.Length < VerticesPerTriangle)
            {
                throw new ArgumentException($"{mesh.name} has no triangles to sample.", nameof(renderer));
            }
        }

        // Running totals of triangle area, so a uniform pick along them lands on a triangle in proportion to its area.
        private static double[] CumulativeAreas(Vector3[] vertices, int[] triangles)
        {
            int triangleCount = triangles.Length / VerticesPerTriangle;
            var cumulative = new double[triangleCount];
            double total = 0.0;

            for (int triangle = 0; triangle < triangleCount; triangle++)
            {
                int corner = triangle * VerticesPerTriangle;
                Vector3 a = vertices[triangles[corner]];
                Vector3 b = vertices[triangles[corner + 1]];
                Vector3 c = vertices[triangles[corner + 2]];
                total += 0.5 * Vector3.Cross(b - a, c - a).magnitude;
                cumulative[triangle] = total;
            }

            if (total <= 0.0)
            {
                throw new ArgumentException("The mesh's triangles have no area to sample.");
            }

            return cumulative;
        }

        // Measured in the renderer's world space, not raw mesh space, because some exporters store the mesh lying down
        // and stand it up with the renderer's rotation. Feet are 0 and the top of the head is 1.
        private static void HeightRange(Vector3[] vertices, Matrix4x4 meshToWorld, out float lowest, out float highest)
        {
            lowest = float.MaxValue;
            highest = float.MinValue;
            for (int vertex = 0; vertex < vertices.Length; vertex++)
            {
                float height = meshToWorld.MultiplyPoint3x4(vertices[vertex]).y;
                lowest = Mathf.Min(lowest, height);
                highest = Mathf.Max(highest, height);
            }
        }

        private static int PickTriangle(double[] cumulativeAreas, System.Random random)
        {
            double pick = random.NextDouble() * cumulativeAreas[cumulativeAreas.Length - 1];
            int index = Array.BinarySearch(cumulativeAreas, pick);
            if (index < 0)
            {
                index = ~index;
            }

            return Mathf.Min(index, cumulativeAreas.Length - 1);
        }

        // Two uniform numbers folded back into the triangle when they land outside it, which keeps the spread even.
        private static Vector3 PointOnTriangle(Vector3 a, Vector3 b, Vector3 c, System.Random random)
        {
            float u = (float)random.NextDouble();
            float v = (float)random.NextDouble();
            if (u + v > 1f)
            {
                u = 1f - u;
                v = 1f - v;
            }

            return a + ((b - a) * u) + ((c - a) * v);
        }

        private static int DominantBone(BoneWeight weight)
        {
            int bone = weight.boneIndex0;
            float best = weight.weight0;

            if (weight.weight1 > best)
            {
                bone = weight.boneIndex1;
                best = weight.weight1;
            }

            if (weight.weight2 > best)
            {
                bone = weight.boneIndex2;
                best = weight.weight2;
            }

            if (weight.weight3 > best)
            {
                bone = weight.boneIndex3;
            }

            return bone;
        }

        // The bones the samples use, in rig order. When more than MaxBones are used (a rig with finger and end bones),
        // the busiest MaxBones are kept, and the rest are folded into an ancestor by SlotsOfBones.
        private static int[] ChooseBones(int[] uses)
        {
            var used = new List<int>(uses.Length);
            for (int bone = 0; bone < uses.Length; bone++)
            {
                if (uses[bone] > 0)
                {
                    used.Add(bone);
                }
            }

            if (used.Count > ColossusSampleSet.MaxBones)
            {
                used.Sort((first, second) =>
                {
                    int byUses = uses[second].CompareTo(uses[first]);
                    return byUses != 0 ? byUses : first.CompareTo(second);
                });
                used.RemoveRange(ColossusSampleSet.MaxBones, used.Count - ColossusSampleSet.MaxBones);
                used.Sort();
            }

            return used.ToArray();
        }

        // For every rig bone a sample uses: its slot in the table, or the slot of its nearest kept ancestor, so a
        // folded fingertip moves with the hand. A bone with no kept ancestor goes to the busiest kept bone.
        private static int[] SlotsOfBones(int[] table, int[] uses, Transform[] bones)
        {
            var slotOfBone = new int[bones.Length];
            for (int bone = 0; bone < bones.Length; bone++)
            {
                slotOfBone[bone] = NotKept;
            }

            int busiestSlot = 0;
            for (int slot = 0; slot < table.Length; slot++)
            {
                slotOfBone[table[slot]] = slot;
                if (uses[table[slot]] > uses[table[busiestSlot]])
                {
                    busiestSlot = slot;
                }
            }

            if (table.Length == CountUsed(uses))
            {
                return slotOfBone;
            }

            var keptSlotOf = new Dictionary<Transform, int>(table.Length);
            for (int slot = 0; slot < table.Length; slot++)
            {
                keptSlotOf[bones[table[slot]]] = slot;
            }

            for (int bone = 0; bone < bones.Length; bone++)
            {
                if (uses[bone] > 0 && slotOfBone[bone] == NotKept)
                {
                    slotOfBone[bone] = NearestKeptAncestor(bones[bone], keptSlotOf, busiestSlot);
                }
            }

            return slotOfBone;
        }

        private static int CountUsed(int[] uses)
        {
            int used = 0;
            for (int bone = 0; bone < uses.Length; bone++)
            {
                if (uses[bone] > 0)
                {
                    used++;
                }
            }

            return used;
        }

        private static int NearestKeptAncestor(Transform bone, Dictionary<Transform, int> keptSlotOf, int fallbackSlot)
        {
            for (Transform parent = bone.parent; parent != null; parent = parent.parent)
            {
                if (keptSlotOf.TryGetValue(parent, out int slot))
                {
                    return slot;
                }
            }

            return fallbackSlot;
        }

        // Through SerializedObject, so the change is undoable and saved like an Inspector edit, and the asset keeps no setter.
        private static void Write(ColossusSampleSet target, BakedSamples baked)
        {
            var serialized = new SerializedObject(target);
            FindField(serialized, MeshNameField).stringValue = baked.MeshName;
            FindField(serialized, RendererBoneCountField).intValue = baked.RendererBoneCount;

            SerializedProperty rendererBones = ResizedArray(serialized, RendererBonesField, baked.RendererBones.Length);
            SerializedProperty names = ResizedArray(serialized, BoneNamesField, baked.BoneNames.Length);
            for (int slot = 0; slot < baked.RendererBones.Length; slot++)
            {
                rendererBones.GetArrayElementAtIndex(slot).intValue = baked.RendererBones[slot];
                names.GetArrayElementAtIndex(slot).stringValue = baked.BoneNames[slot];
            }

            int sampleCount = baked.BoneIndices.Length;
            SerializedProperty boneIndices = ResizedArray(serialized, BoneIndicesField, sampleCount);
            SerializedProperty offsets = ResizedArray(serialized, BoneOffsetsField, sampleCount);
            SerializedProperty heights = ResizedArray(serialized, Heights01Field, sampleCount);
            for (int sample = 0; sample < sampleCount; sample++)
            {
                boneIndices.GetArrayElementAtIndex(sample).intValue = baked.BoneIndices[sample];
                offsets.GetArrayElementAtIndex(sample).vector3Value = baked.BoneOffsets[sample];
                heights.GetArrayElementAtIndex(sample).floatValue = baked.Heights01[sample];
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        private static SerializedProperty FindField(SerializedObject serialized, string field)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"ColossusSampleSet has no field '{field}'. Keep ColossusSampleBaker's field names in step with it.");
            }

            return property;
        }

        private static SerializedProperty ResizedArray(SerializedObject serialized, string field, int size)
        {
            SerializedProperty property = FindField(serialized, field);
            if (!property.isArray)
            {
                throw new InvalidOperationException($"ColossusSampleSet's field '{field}' is not an array.");
            }

            property.arraySize = size;
            return property;
        }

        private readonly struct BakedSamples
        {
            public BakedSamples(
                string meshName,
                int rendererBoneCount,
                int[] rendererBones,
                string[] boneNames,
                int[] boneIndices,
                Vector3[] boneOffsets,
                float[] heights01)
            {
                MeshName = meshName;
                RendererBoneCount = rendererBoneCount;
                RendererBones = rendererBones;
                BoneNames = boneNames;
                BoneIndices = boneIndices;
                BoneOffsets = boneOffsets;
                Heights01 = heights01;
            }

            public string MeshName { get; }
            public int RendererBoneCount { get; }
            public int[] RendererBones { get; }
            public string[] BoneNames { get; }
            public int[] BoneIndices { get; }
            public Vector3[] BoneOffsets { get; }
            public float[] Heights01 { get; }
        }
    }
}
