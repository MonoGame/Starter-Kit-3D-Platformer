// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using MIConvexHull;

internal static class ConvexHullHelper
{
    public class MIVertex : IVertex
    {
        public double[] Position { get; }

        public MIVertex(Vector3 position)
        {
            Position = new double[] { position.X, position.Y, position.Z };
        }
    }

    class Vector3Comparer : IEqualityComparer<Vector3>
    {
        bool IEqualityComparer<Vector3>.Equals(Vector3 x, Vector3 y)
        {
            return ((x - y).Length() < 0.001f);
        }

        int IEqualityComparer<Vector3>.GetHashCode(Vector3 obj)
        {
            var d = obj.LengthSquared;
            return d.GetHashCode();
        }
    }

    private static Vector3 ToVector3(double[] xyz) => new Vector3((float)xyz[0], (float)xyz[1], (float)xyz[2]);

    public static List<ConvexHull> GenerateConvexHulls(ModelContent content, int maxFaces, float tolerance)
    {
        var hulls = new List<ConvexHull>();

        foreach (var mesh in content.Meshes)
        {
            foreach (var geometry in mesh.SourceMesh.Geometry)
            {
                // Grab the mesh verts.
                var center = Vector3.Zero;
                var verts = new List<Vector3>();
                var positionChannel = geometry.Vertices.Positions;
                for (int i = 0; i < geometry.Vertices.VertexCount; i++)
                {
                    var v = positionChannel[i];
                    var vv = Vector3.Transform(v, geometry.Parent.Transform);
                    center += vv;
                    verts.Add(vv);
                }
                center /= (float)verts.Count;

                // Remove very similar verts and convert it to the MI vertex format.
                var miverts = verts.Distinct(new Vector3Comparer()).Select(p => new MIVertex(p)).ToList();

                // Generate the convex hull.... double the tolerance and
                // retry if we generate too many final faces.
                ConvexHullCreationResult<MIVertex, DefaultConvexFace<MIVertex>> result;
                while (true)
                {
                    result = MIConvexHull.ConvexHull.Create<MIVertex, DefaultConvexFace<MIVertex>>(miverts, tolerance);
                    if (result.Result.Faces.Count() <= maxFaces)
                        break;

                    tolerance *= 2;
                }

                // Get the final hull verts and faces.
                var hverts = new List<Vector3>();
                var hfaces = new List<ConvexHull.Face>();
                foreach (var f in result.Result.Faces)
                {
                    var ii = new List<int>();

                    foreach (var v in f.Vertices)
                    {
                        var vv = ToVector3(v.Position);
                        var index = hverts.IndexOf(vv);
                        if (index < 0)
                        {
                            index = hverts.Count;
                            hverts.Add(vv);
                        }
                        ii.Add(index);
                    }

                    hfaces.Add(new ConvexHull.Face
                    {
                        Normal = Vector3.Normalize(ToVector3(f.Normal)),
                        Indices = ii.ToArray()
                    });
                }

                // Build the final runtime hull.
                var hull = new ConvexHull();
                hull.Center = center;
                hull.Vertices = hverts.ToArray();
                hull.Faces = hfaces.ToArray();
                hull.AABB = BoundingBox.CreateFromPoints(hverts);
                hulls.Add(hull);
            }
        }

        return hulls;
    }
}
