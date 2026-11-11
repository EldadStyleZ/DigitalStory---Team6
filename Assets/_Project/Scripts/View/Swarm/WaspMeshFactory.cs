using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// Builds the one low-poly wasp the swarm draws thousands of times: a tapered body along +Z and two wing quads,
    /// 24 flat-shaded triangles in all. It is one unit long, head at z 0.5, and WaspInstanced.shader scales it
    /// to SwarmConfig.WaspLength. uv.y is 1 on wing vertices and 0 on the body, so the shader can flap the wings.
    /// </summary>
    public static class WaspMeshFactory
    {
        /// <summary>The uv.y value that marks a wing vertex.</summary>
        public const float WingMark = 1f;

        private const float BodyMark = 0f;
        private const float HeadZ = 0.5f;
        private const float TailZ = -0.5f;
        private const float AxisY = 0.01f;

        // The body is two diamond rings between the head and the sting.
        private const float ThoraxZ = 0.08f;
        private const float ThoraxHalfWidth = 0.14f;
        private const float ThoraxHalfHeight = 0.13f;
        private const float AbdomenZ = -0.18f;
        private const float AbdomenHalfWidth = 0.17f;
        private const float AbdomenHalfHeight = 0.15f;

        // Each wing lies flat at the top of the thorax, swept back.
        private const float WingY = AxisY + ThoraxHalfHeight + 0.01f;
        private const float WingRootX = 0.03f;
        private const float WingRootFrontZ = 0.16f;
        private const float WingRootBackZ = -0.02f;
        private const float WingTipX = 0.46f;
        private const float WingTipFrontZ = 0.0f;
        private const float WingTipBackX = 0.4f;
        private const float WingTipBackZ = -0.2f;

        private const int RingSides = 4;
        private const int TriangleCount = 24;

        /// <summary>Makes a new wasp mesh. Call it once and keep the result; the caller destroys it.</summary>
        public static Mesh Create()
        {
            var vertices = new List<Vector3>(TriangleCount * 3);
            var normals = new List<Vector3>(TriangleCount * 3);
            var uvs = new List<Vector2>(TriangleCount * 3);
            var triangles = new List<int>(TriangleCount * 3);
            var parts = new Parts(vertices, normals, uvs, triangles);

            AddBody(parts);
            AddWing(parts, 1f);
            AddWing(parts, -1f);

            var mesh = new Mesh { name = "Wasp" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddBody(Parts parts)
        {
            var head = new Vector3(0f, AxisY, HeadZ);
            var tail = new Vector3(0f, AxisY, TailZ);

            for (int side = 0; side < RingSides; side++)
            {
                int next = (side + 1) % RingSides;
                Vector3 thorax = RingPoint(side, ThoraxZ, ThoraxHalfWidth, ThoraxHalfHeight);
                Vector3 thoraxNext = RingPoint(next, ThoraxZ, ThoraxHalfWidth, ThoraxHalfHeight);
                Vector3 abdomen = RingPoint(side, AbdomenZ, AbdomenHalfWidth, AbdomenHalfHeight);
                Vector3 abdomenNext = RingPoint(next, AbdomenZ, AbdomenHalfWidth, AbdomenHalfHeight);

                AddBodyTriangle(parts, head, thorax, thoraxNext);
                AddBodyTriangle(parts, thorax, abdomen, abdomenNext);
                AddBodyTriangle(parts, thorax, abdomenNext, thoraxNext);
                AddBodyTriangle(parts, tail, abdomenNext, abdomen);
            }
        }

        // Each wing is a quad drawn on both faces, so it shows from above and below with back faces culled.
        private static void AddWing(Parts parts, float side)
        {
            var rootFront = new Vector3(side * WingRootX, WingY, WingRootFrontZ);
            var tipFront = new Vector3(side * WingTipX, WingY, WingTipFrontZ);
            var tipBack = new Vector3(side * WingTipBackX, WingY, WingTipBackZ);
            var rootBack = new Vector3(side * WingRootX, WingY, WingRootBackZ);

            AddTriangle(parts, rootFront, tipFront, tipBack, Vector3.up, WingMark);
            AddTriangle(parts, rootFront, tipBack, rootBack, Vector3.up, WingMark);
            AddTriangle(parts, rootFront, tipFront, tipBack, Vector3.down, WingMark);
            AddTriangle(parts, rootFront, tipBack, rootBack, Vector3.down, WingMark);
        }

        // Corners in order: top, right, bottom, left.
        private static Vector3 RingPoint(int side, float z, float halfWidth, float halfHeight)
        {
            switch (side)
            {
                case 0:
                    return new Vector3(0f, AxisY + halfHeight, z);
                case 1:
                    return new Vector3(halfWidth, AxisY, z);
                case 2:
                    return new Vector3(0f, AxisY - halfHeight, z);
                default:
                    return new Vector3(-halfWidth, AxisY, z);
            }
        }

        private static void AddBodyTriangle(Parts parts, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 center = (a + b + c) / 3f;
            var outward = new Vector3(center.x, center.y - AxisY, 0f);
            AddTriangle(parts, a, b, c, outward, BodyMark);
        }

        // Unity treats clockwise triangles as front faces, which makes Cross(b - a, c - a) the front normal.
        // The corners are swapped when that normal would point inward, so no triangle is built inside out.
        private static void AddTriangle(Parts parts, Vector3 a, Vector3 b, Vector3 c, Vector3 outward, float mark)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(normal, outward) < 0f)
            {
                (b, c) = (c, b);
                normal = -normal;
            }

            normal.Normalize();
            int first = parts.Vertices.Count;
            var uv = new Vector2(0f, mark);

            parts.Vertices.Add(a);
            parts.Vertices.Add(b);
            parts.Vertices.Add(c);

            for (int corner = 0; corner < 3; corner++)
            {
                parts.Normals.Add(normal);
                parts.Uvs.Add(uv);
                parts.Triangles.Add(first + corner);
            }
        }

        private readonly struct Parts
        {
            public Parts(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles)
            {
                Vertices = vertices;
                Normals = normals;
                Uvs = uvs;
                Triangles = triangles;
            }

            public List<Vector3> Vertices { get; }

            public List<Vector3> Normals { get; }

            public List<Vector2> Uvs { get; }

            public List<int> Triangles { get; }
        }
    }
}
