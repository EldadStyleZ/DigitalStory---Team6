using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// Builds the two meshes the swarm draws thousands of times. The wasp is chunky and low-poly, like the artist's
    /// pictures: a six-sided head, thorax and fat abdomen along +Z, two eye facets and two flat wing quads, 58
    /// flat-shaded triangles in all. It is one unit long, head at z 0.5, and WaspInstanced.shader scales it to
    /// SwarmConfig.WaspLength and paints the stripes from local z. uv.y is 1 on wing vertices, so the shader can flap
    /// the wings, and uv.x is 1 on the eyes, so they can glow. The glow quad is what WaspGlow.shader turns to face the
    /// camera around each wasp.
    /// </summary>
    public static class WaspMeshFactory
    {
        /// <summary>The uv.y value that marks a wing vertex.</summary>
        public const float WingMark = 1f;

        /// <summary>The uv.x value that marks an eye vertex.</summary>
        public const float EyeMark = 1f;

        private const float NoMark = 0f;
        private const float AxisY = 0.01f;

        // Every segment is six-sided, with a corner at each side and a flat facet on top, so from the game camera the
        // body shows broad facets rather than a thin ridge. A six-sided ring seen head-on is the artist's hexagon.
        private const int RingSides = 6;
        private const float RingStep = Mathf.PI * 2f / RingSides;
        private const float TopFacetHeight = 0.8660254f; // sin 60°: the height of the top facet as a share of the half height

        // Each segment is one or two rings between two points on the axis, so the segments pinch apart at the neck and
        // the waist. The abdomen, the biggest, has two rings, so it reads round. Half widths are across, half heights up.
        private const float HeadTipZ = 0.5f;
        private const float HeadZ = 0.4f;
        private const float HeadHalfWidth = 0.14f;
        private const float HeadHalfHeight = 0.13f;
        private const float HeadBackZ = 0.27f;

        private const float ThoraxFrontZ = 0.33f;
        private const float ThoraxZ = 0.16f;
        private const float ThoraxHalfWidth = 0.21f;
        private const float ThoraxHalfHeight = 0.19f;
        private const float ThoraxBackZ = -0.04f;

        private const float AbdomenFrontZ = 0.06f;
        private const float AbdomenZ = -0.1f;
        private const float AbdomenHalfWidth = 0.25f;
        private const float AbdomenHalfHeight = 0.22f;
        private const float AbdomenBackZ = -0.3f;
        private const float AbdomenBackHalfWidth = 0.19f;
        private const float AbdomenBackHalfHeight = 0.16f;
        private const float StingZ = -0.5f;

        // Each eye is a smaller copy of an upper front facet of the head, lifted off it so the two never fight for depth.
        private const int RightEyeSide = 0;
        private const int LeftEyeSide = 2;
        private const float EyeShare = 0.6f;
        private const float EyeLift = 0.01f;

        // Each wing lies flat just above the top of the thorax, swept back. Its root stays close to the thorax's ring,
        // where the thorax is tallest, so the wing does not float clear of the body.
        private const float WingY = AxisY + (ThoraxHalfHeight * TopFacetHeight) + 0.01f;
        private const float WingRootX = 0.07f;
        private const float WingRootFrontZ = 0.22f;
        private const float WingRootBackZ = 0.1f;
        private const float WingTipX = 0.46f;
        private const float WingTipFrontZ = 0.12f;
        private const float WingTipBackX = 0.4f;
        private const float WingTipBackZ = -0.06f;

        // Head 12, thorax 12, abdomen 24, eyes 2 and wings 8. At 4,000 wasps that is 232,000 triangles, fewer than the
        // first swarm's 10,000 wasps of 24.
        private const int TriangleCount = 58;

        private const float GlowHalfSize = 0.5f;

        private static readonly Vector2 BodyUv = new Vector2(NoMark, NoMark);
        private static readonly Vector2 EyeUv = new Vector2(EyeMark, NoMark);
        private static readonly Vector2 WingUv = new Vector2(NoMark, WingMark);

        /// <summary>Makes a new wasp mesh. Call it once and keep the result; the caller destroys it.</summary>
        public static Mesh Create()
        {
            var vertices = new List<Vector3>(TriangleCount * 3);
            var normals = new List<Vector3>(TriangleCount * 3);
            var uvs = new List<Vector2>(TriangleCount * 3);
            var triangles = new List<int>(TriangleCount * 3);
            var parts = new Parts(vertices, normals, uvs, triangles);

            AddBody(parts);
            AddEye(parts, RightEyeSide);
            AddEye(parts, LeftEyeSide);
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

        /// <summary>
        /// Makes the quad WaspGlow.shader draws around each wasp: one unit across, centred on the origin in the XY plane.
        /// The shader turns it to face the camera, so its facing here does not matter. Call it once and keep the result;
        /// the caller destroys it.
        /// </summary>
        public static Mesh CreateGlowQuad()
        {
            var mesh = new Mesh { name = "Wasp Glow" };
            mesh.SetVertices(new[]
            {
                new Vector3(-GlowHalfSize, -GlowHalfSize, 0f),
                new Vector3(-GlowHalfSize, GlowHalfSize, 0f),
                new Vector3(GlowHalfSize, GlowHalfSize, 0f),
                new Vector3(GlowHalfSize, -GlowHalfSize, 0f),
            });
            mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddBody(Parts parts)
        {
            AddCone(parts, Axis(HeadTipZ), HeadZ, HeadHalfWidth, HeadHalfHeight);
            AddCone(parts, Axis(HeadBackZ), HeadZ, HeadHalfWidth, HeadHalfHeight);

            AddCone(parts, Axis(ThoraxFrontZ), ThoraxZ, ThoraxHalfWidth, ThoraxHalfHeight);
            AddCone(parts, Axis(ThoraxBackZ), ThoraxZ, ThoraxHalfWidth, ThoraxHalfHeight);

            AddCone(parts, Axis(AbdomenFrontZ), AbdomenZ, AbdomenHalfWidth, AbdomenHalfHeight);
            AddBand(parts, AbdomenZ, AbdomenHalfWidth, AbdomenHalfHeight, AbdomenBackZ, AbdomenBackHalfWidth, AbdomenBackHalfHeight);
            AddCone(parts, Axis(StingZ), AbdomenBackZ, AbdomenBackHalfWidth, AbdomenBackHalfHeight);
        }

        // Six triangles from a point on the axis to a ring.
        private static void AddCone(Parts parts, Vector3 tip, float ringZ, float halfWidth, float halfHeight)
        {
            for (int side = 0; side < RingSides; side++)
            {
                Vector3 corner = RingPoint(side, ringZ, halfWidth, halfHeight);
                Vector3 nextCorner = RingPoint((side + 1) % RingSides, ringZ, halfWidth, halfHeight);
                AddTriangle(parts, tip, corner, nextCorner, Outward(tip, corner, nextCorner), BodyUv);
            }
        }

        // Twelve triangles joining two rings.
        private static void AddBand(
            Parts parts, float frontZ, float frontHalfWidth, float frontHalfHeight, float backZ, float backHalfWidth, float backHalfHeight)
        {
            for (int side = 0; side < RingSides; side++)
            {
                int next = (side + 1) % RingSides;
                Vector3 front = RingPoint(side, frontZ, frontHalfWidth, frontHalfHeight);
                Vector3 frontNext = RingPoint(next, frontZ, frontHalfWidth, frontHalfHeight);
                Vector3 back = RingPoint(side, backZ, backHalfWidth, backHalfHeight);
                Vector3 backNext = RingPoint(next, backZ, backHalfWidth, backHalfHeight);

                AddTriangle(parts, front, back, backNext, Outward(front, back, backNext), BodyUv);
                AddTriangle(parts, front, backNext, frontNext, Outward(front, backNext, frontNext), BodyUv);
            }
        }

        // The head's front facet from `side` to the next corner, shrunk about its middle and lifted along its normal.
        private static void AddEye(Parts parts, int side)
        {
            Vector3 tip = Axis(HeadTipZ);
            Vector3 corner = RingPoint(side, HeadZ, HeadHalfWidth, HeadHalfHeight);
            Vector3 nextCorner = RingPoint((side + 1) % RingSides, HeadZ, HeadHalfWidth, HeadHalfHeight);
            Vector3 outward = Outward(tip, corner, nextCorner);
            Vector3 middle = (tip + corner + nextCorner) / 3f;

            Vector3 normal = Vector3.Cross(corner - tip, nextCorner - tip).normalized;
            if (Vector3.Dot(normal, outward) < 0f)
            {
                normal = -normal;
            }

            Vector3 lift = normal * EyeLift;
            AddTriangle(
                parts,
                Vector3.Lerp(middle, tip, EyeShare) + lift,
                Vector3.Lerp(middle, corner, EyeShare) + lift,
                Vector3.Lerp(middle, nextCorner, EyeShare) + lift,
                outward,
                EyeUv);
        }

        // Each wing is a quad drawn on both faces, so it shows from above and below with back faces culled.
        private static void AddWing(Parts parts, float side)
        {
            var rootFront = new Vector3(side * WingRootX, WingY, WingRootFrontZ);
            var tipFront = new Vector3(side * WingTipX, WingY, WingTipFrontZ);
            var tipBack = new Vector3(side * WingTipBackX, WingY, WingTipBackZ);
            var rootBack = new Vector3(side * WingRootX, WingY, WingRootBackZ);

            AddTriangle(parts, rootFront, tipFront, tipBack, Vector3.up, WingUv);
            AddTriangle(parts, rootFront, tipBack, rootBack, Vector3.up, WingUv);
            AddTriangle(parts, rootFront, tipFront, tipBack, Vector3.down, WingUv);
            AddTriangle(parts, rootFront, tipBack, rootBack, Vector3.down, WingUv);
        }

        private static Vector3 Axis(float z) => new Vector3(0f, AxisY, z);

        // Corner 0 is at the right side, and the corners go round through the top, so corners 1 and 2 bound the top facet.
        private static Vector3 RingPoint(int side, float z, float halfWidth, float halfHeight)
        {
            float angle = side * RingStep;
            return new Vector3(Mathf.Cos(angle) * halfWidth, AxisY + (Mathf.Sin(angle) * halfHeight), z);
        }

        // Away from the body's axis, through the triangle's middle. Every body facet faces this way.
        private static Vector3 Outward(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 center = (a + b + c) / 3f;
            return new Vector3(center.x, center.y - AxisY, 0f);
        }

        // Unity treats clockwise triangles as front faces, which makes Cross(b - a, c - a) the front normal.
        // The corners are swapped when that normal would point inward, so no triangle is built inside out.
        private static void AddTriangle(Parts parts, Vector3 a, Vector3 b, Vector3 c, Vector3 outward, Vector2 uv)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(normal, outward) < 0f)
            {
                (b, c) = (c, b);
                normal = -normal;
            }

            normal.Normalize();
            int first = parts.Vertices.Count;

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
