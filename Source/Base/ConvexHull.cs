// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

/// <summary>
/// A convex hull used for collision detection.
/// Implements the Separating Axis Theorem (SAT) for intersection tests.
/// ContentSerializerRuntimeType attribute is needed to ensure the
/// XNB deserializer can properly instantiate this type at runtime. 
/// Because we use Shared code, we need to know the final assembly name at built time.
/// This is done using the GameConstants.AssemblyName constant.
/// </summary>
[ContentSerializerRuntimeType($"{nameof(ConvexHull)}, {GameConstants.AssemblyName}")]
public class ConvexHull
{
    /// <summary>
    /// A face of the convex hull.
    /// ContentSerializerRuntimeType attribute is needed to ensure the
    /// XNB deserializer can properly instantiate this type at runtime. 
    /// Because we use Shared code, we need to know the final assembly name at built time.
    /// This is done using the GameConstants.AssemblyName constant.
    /// </summary>
    [ContentSerializerRuntimeType($"{nameof(ConvexHull)}+{nameof(Face)}, {GameConstants.AssemblyName}")]
    public struct Face
    {
        public Vector3 Normal;
        public int[] Indices;
    }

    [ContentSerializer]
    public Vector3 Center;

    [ContentSerializer]
    public Vector3[] Vertices;

    [ContentSerializer]
    public Face[] Faces;

    [ContentSerializer]
    public BoundingBox AABB;

    public static ConvexHull CreateSphere(Vector3 offset, float radius, int segments)
    {
        var verts = new List<Vector3>();
        var faces = new List<Face>();

        for (int i = 0; i < segments; i++)
        {
            float theta = MathHelper.Pi * i / (segments - 1);
            for (int j = 0; j < segments; j++)
            {
                float phi = MathHelper.TwoPi * j / (segments - 1);
                float x = radius * (float)(Math.Sin(theta) * Math.Cos(phi));
                float y = radius * (float)Math.Cos(theta);
                float z = radius * (float)(Math.Sin(theta) * Math.Sin(phi));
                verts.Add(offset + new Vector3(x, y, z));
            }
        }

        // Create faces
        for (int i = 0; i < segments - 1; i++)
        {
            for (int j = 0; j < segments - 1; j++)
            {
                int a = i * segments + j;
                int b = a + segments;
                int c = a + 1;
                int d = b + 1;

                faces.Add(new Face
                {
                    Indices = new[] { a, b, c },
                    Normal = Vector3.Normalize(Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]))
                });

                faces.Add(new Face
                {
                    Indices = new[] { b, d, c },
                    Normal = Vector3.Normalize(Vector3.Cross(verts[d] - verts[b], verts[c] - verts[b]))
                });
            }
        }

        return new ConvexHull
        {
            Center = offset,
            Vertices = verts.ToArray(),
            Faces = faces.ToArray(),
            AABB = BoundingBox.CreateFromPoints(verts)
        };
    }

    public static ConvexHull CreateCylinder(Vector3 offset, float radius, float height, int segments)
    {
        var verts = new List<Vector3>();
        var faces = new List<Face>();

        var halfHeight = height / 2f;

        for (int i = 0; i < segments; i++)
        {
            float angle = MathHelper.TwoPi * i / segments;
            float x = (float)Math.Cos(angle) * radius;
            float z = (float)Math.Sin(angle) * radius;

            // Top ring and bottom ring of verts.
            verts.Add(offset + new Vector3(x, halfHeight, z));
            verts.Add(offset + new Vector3(x, -halfHeight, z));
        }

        // Top face.
        var topIndices = new List<int>();
        for (int i = 0; i < segments; i++)
            topIndices.Add(i * 2);
        faces.Add(new Face { Indices = topIndices.ToArray(), Normal = Vector3.Up });

        // Bottom face.
        var bottomIndices = new List<int>();
        for (int i = segments - 1; i >= 0; i--)
            bottomIndices.Add(i * 2 + 1);
        faces.Add(new Face { Indices = bottomIndices.ToArray(), Normal = Vector3.Down });

        // Side faces as triangles.
        for (int i = 0; i < segments; i++)
        {
            var topA = (i * 2) % (segments * 2);
            var botA = (topA + 1) % (segments * 2);
            var topB = (topA + 2) % (segments * 2);
            var botB = (topB + 1) % (segments * 2);

            var va = verts[topA];
            var vb = verts[topB];
            var vc = verts[botB];
            var normal1 = Vector3.Normalize(Vector3.Cross(vb - va, vc - va));

            faces.Add(new Face { Indices = new[] { topA, botA, botB }, Normal = normal1 });
            faces.Add(new Face { Indices = new[] { botB, topB, topA }, Normal = normal1 });
        }

        return new ConvexHull
        {
            Center = offset,
            Vertices = verts.ToArray(),
            Faces = faces.ToArray(),
            AABB = BoundingBox.CreateFromPoints(verts)
        };
    }

    /// <summary>
    /// Returns a clone of the hull sharing face indices.
    /// </summary>
    /// <returns></returns>
    public ConvexHull Clone()
    {
        var hull = new ConvexHull();

        hull.Faces = new Face[Faces.Length];

        for (var i = 0; i < hull.Faces.Length; i++)
        {
            var face = Faces[i];
            hull.Faces[i] = new Face
            {
                Indices = face.Indices,
                Normal = face.Normal
            };
        }

        hull.Center = Center;
        hull.Vertices = Vertices.ToArray();
        hull.AABB = AABB;

        return hull;
    }

    private static bool IsSeparatingAxis(Vector3 axisNormal, Vector3[] vertsA, Vector3[] vertsB, out float overlap)
    {
        const float Epsilon = 0.01f;

        ProjectOntoAxis(vertsA, axisNormal, out float minA, out float maxA);
        ProjectOntoAxis(vertsB, axisNormal, out float minB, out float maxB);

        if (maxA < (minB - Epsilon) || maxB < (minA - Epsilon))
        {
            overlap = 0;
            return true;
        }

        overlap = MathF.Min(maxA, maxB) - MathF.Max(minA, minB);
        return false;
    }

    private static void ProjectOntoAxis(Vector3[] vertices, Vector3 axis, out float min, out float max)
    {
        float dot = Vector3.Dot(vertices[0], axis);
        min = max = dot;

        for (int i = 1; i < vertices.Length; i++)
        {
            dot = Vector3.Dot(vertices[i], axis);
            if (dot < min) min = dot;
            if (dot > max) max = dot;
        }
    }

    static Vector3 GetSupportPoint(Vector3[] vertices, Vector3 direction)
    {
        float maxDot = float.NegativeInfinity;
        Vector3 best = vertices[0];

        foreach (var v in vertices)
        {
            float d = Vector3.Dot(v, direction);
            if (d > maxDot)
            {
                maxDot = d;
                best = v;
            }
        }
        return best;
    }

    static float MaxExtentAlong(Vector3 normal, Vector3[] verts, Vector3 center)
    {
        float max = float.NegativeInfinity;
        foreach (var v in verts)
        {
            float d = Vector3.Dot(v - center, normal);
            if (d > max)
                max = d;
        }
        return max;
    }

    public static bool Intersects(ConvexHull a, ConvexHull b, out Contact contact)
    {
        contact.point = Vector3.Zero;
        contact.normal = Vector3.Zero;
        contact.depth = float.MaxValue;

        if (!a.AABB.Intersects(b.AABB))
            return false;

        var sweep = b.Center - a.Center;

        foreach (var face in a.Faces)
        {
            // TODO: Why are these not already normalized?
            var normal = Vector3.Normalize(face.Normal);

            // Skip backfaces.
            if (Vector3.Dot(normal, sweep) > 0)
                continue;

            if (IsSeparatingAxis(normal, a.Vertices, b.Vertices, out float overlap))
                return false;

            if (overlap < contact.depth)
            {
                contact.depth = overlap;
                contact.normal = normal;
            }
        }

        foreach (var face in b.Faces)
        {
            // TODO: Why are these not already normalized?
            var normal = Vector3.Normalize(face.Normal);

            // Skip backfaces.
            if (Vector3.Dot(normal, sweep) > 0)
                continue;

            if (IsSeparatingAxis(normal, a.Vertices, b.Vertices, out float overlap))
                return false;

            if (overlap < contact.depth)
            {
                contact.depth = overlap;
                contact.normal = normal;
            }
        }

        // Get the approximate contact point.
        //
        // TOOD: This is not as good as i would like it.
        // But it works for now.
        //
        float extentA = MaxExtentAlong(-contact.normal, a.Vertices, a.Center);
        contact.point = a.Center + (-contact.normal * (extentA - 0.5f * contact.depth));

        return true;
    }
}
