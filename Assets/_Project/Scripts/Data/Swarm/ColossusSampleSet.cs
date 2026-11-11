using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// Where each wasp sits on the Colossus: a point fixed in one of the puppet's bones (spec F-010, section 9.5).
    /// ColossusSampleBaker fills it in the Editor, because an imported mesh cannot be read at runtime.
    /// Sample i is wasp i's place. The arrays are parallel, one entry per sample.
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Colossus Sample Set", fileName = "ColossusSampleSet")]
    public sealed class ColossusSampleSet : ScriptableObject
    {
        /// <summary>The most bones the swarm can follow. Matches the bone buffer in SwarmGpuView and WaspSwarm.compute.</summary>
        public const int MaxBones = 64;

        [SerializeField, Tooltip("The mesh the samples were baked from, so a person can see what the asset holds.")]
        private string _meshName = string.Empty;

        [SerializeField, Tooltip("How many bones the puppet's SkinnedMeshRenderer had when it was baked. The puppet in the scene must have the same.")]
        private int _rendererBoneCount;

        [SerializeField, Tooltip("The bones the samples follow, as indices into SkinnedMeshRenderer.bones. At most 64.")]
        private int[] _rendererBones = new int[0];

        [SerializeField, Tooltip("The names of the bones above, in the same order, to check the puppet is the model that was baked.")]
        private string[] _boneNames = new string[0];

        [SerializeField, Tooltip("Per sample: which of the bones above it follows, counting from 0.")]
        private int[] _boneIndices = new int[0];

        [SerializeField, Tooltip("Per sample: its point in that bone's space, from the bone's bind pose.")]
        private Vector3[] _boneOffsets = new Vector3[0];

        [SerializeField, Tooltip("Per sample: its height on the body, 0 at the feet and 1 at the head. The Colossus builds from the feet up in this order.")]
        private float[] _heights01 = new float[0];

        /// <summary>The name of the mesh the samples were baked from.</summary>
        public string MeshName => _meshName;

        /// <summary>How many bones the baked SkinnedMeshRenderer had, all of them, used or not.</summary>
        public int RendererBoneCount => _rendererBoneCount;

        /// <summary>How many bones the samples follow. Each sample's bone index is below this.</summary>
        public int BoneCount => Mathf.Min(_rendererBones.Length, _boneNames.Length);

        /// <summary>How many samples there are.</summary>
        public int SampleCount => Mathf.Min(_boneIndices.Length, Mathf.Min(_boneOffsets.Length, _heights01.Length));

        /// <summary>True when the set has been baked.</summary>
        public bool IsBaked => SampleCount > 0 && BoneCount > 0;

        /// <summary>The followed bones, as indices into the puppet's SkinnedMeshRenderer.bones.</summary>
        public IReadOnlyList<int> RendererBones => _rendererBones;

        /// <summary>The followed bones' names, in the same order as <see cref="RendererBones"/>.</summary>
        public IReadOnlyList<string> BoneNames => _boneNames;

        /// <summary>Per sample: the bone it follows, an index into <see cref="RendererBones"/>.</summary>
        public IReadOnlyList<int> BoneIndices => _boneIndices;

        /// <summary>Per sample: its point in its bone's space.</summary>
        public IReadOnlyList<Vector3> BoneOffsets => _boneOffsets;

        /// <summary>Per sample: its height on the body, 0 at the feet and 1 at the head.</summary>
        public IReadOnlyList<float> Heights01 => _heights01;
    }
}
