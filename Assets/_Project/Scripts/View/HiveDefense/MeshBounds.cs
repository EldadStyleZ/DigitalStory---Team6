using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// The world-space box of every mesh under a transform, from each mesh's own bounds and its renderer's transform, as the
    /// scene builder measures props. It reads no renderer's cached bounds, so it is right straight after a move, and it
    /// works for meshes that are not readable. It allocates, so it is for setting a match up, not for each frame.
    /// </summary>
    internal static class MeshBounds
    {
        private const int Corners = 8;

        /// <summary>False when nothing under <paramref name="root"/> has a mesh.</summary>
        public static bool TryMeasure(Transform root, out Bounds bounds)
        {
            bounds = default;
            bool hasAny = false;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                Mesh mesh = MeshOf(renderers[index]);
                if (mesh == null)
                {
                    continue;
                }

                Matrix4x4 toWorld = renderers[index].transform.localToWorldMatrix;
                Bounds local = mesh.bounds;
                for (int corner = 0; corner < Corners; corner++)
                {
                    var sign = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                    Vector3 world = toWorld.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents, sign));
                    if (hasAny)
                    {
                        bounds.Encapsulate(world);
                    }
                    else
                    {
                        bounds = new Bounds(world, Vector3.zero);
                        hasAny = true;
                    }
                }
            }

            return hasAny;
        }

        private static Mesh MeshOf(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
            {
                return skinned.sharedMesh;
            }

            MeshFilter filter = renderer is MeshRenderer ? renderer.GetComponent<MeshFilter>() : null;
            return filter != null ? filter.sharedMesh : null;
        }
    }
}
