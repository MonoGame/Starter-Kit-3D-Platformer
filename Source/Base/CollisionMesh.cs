// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

/// <summary>
/// A collision mesh composed of one or more convex hulls.
/// Collision detection is performed using the Separating Axis Theorem (SAT).
/// The mesh can be generated from a model or primitive shapes like spheres and cylinders.
/// The mesh is transformed into world space using the parent entity's world matrix.
/// Debug rendering is supported to visualize the collision hulls and bounding box.
/// </summary>
public class CollisionMesh
{
    private List<ConvexHull> _hulls;
    private List<ConvexHull> _worldHulls;

    // Reference to the parent entity for transforms
    private Entity _parent;
    private BoundingBox _corseBoundingBox;
    private BoundingBox _worldBoundingBox;

    // Debug visualization
    private bool _showCollisionMesh = true;
    private static BasicEffect _debugEffect;

    public Entity Parent
    {
        get => _parent;
        set => _parent = value;
    }

    public bool ShowCollisionMesh
    {
        get => _showCollisionMesh;
        set => _showCollisionMesh = value;
    }

    public BoundingBox WorldBoundingBox => _worldBoundingBox;

    public CollisionMesh(Entity parent, Model model, List<ConvexHull> collisionData, BoundingBox boundingBox)
    {
        _parent = parent;

        GenerateFromModel(model, collisionData, boundingBox);
        UpdateWorldCollisionMesh();
    }

    public void GenerateFromSphere(Vector3 offset, float radius, int segments)
    {
        _hulls = new List<ConvexHull>();
        _hulls.Add(ConvexHull.CreateSphere(offset, radius, segments));

        _corseBoundingBox = new BoundingBox(
            offset + new Vector3(-radius),
            offset + new Vector3(radius));

        _worldHulls = new List<ConvexHull>();
        foreach (var hull in _hulls)
            _worldHulls.Add(hull.Clone());

        UpdateWorldCollisionMesh();
    }

    public void GenerateFromCylinder(Vector3 offset, float radius, float height, int segments)
    {
        _hulls = new List<ConvexHull>();
        _hulls.Add(ConvexHull.CreateCylinder(offset, radius, height, segments));

        _corseBoundingBox = new BoundingBox(
            offset + new Vector3(-radius, -height / 2f, -radius),
            offset + new Vector3(radius, height / 2f, radius));

        _worldHulls = new List<ConvexHull>();
        foreach (var hull in _hulls)
            _worldHulls.Add(hull.Clone());

        UpdateWorldCollisionMesh();
    }

    private void GenerateFromModel(Model model, List<ConvexHull> collisionData, BoundingBox boundingBox)
    {
        _hulls = collisionData;

        _corseBoundingBox = boundingBox;

        _worldHulls = new List<ConvexHull>();
        foreach (var hull in _hulls)
        {
            _worldHulls.Add(hull.Clone());
        }

        // Initialize world-space boxes
        UpdateWorldCollisionMesh();
    }

    public void UpdateWorldCollisionMesh()
    {
        for (int i = 0; i < _hulls.Count; i++)
        {
            var h = _hulls[i];
            var wh = _worldHulls[i];

            wh.Center = Vector3.Transform(h.Center, _parent.WorldMatrix);

            for (int j = 0; j < h.Vertices.Length; j++)
                wh.Vertices[j] = Vector3.Transform(h.Vertices[j], _parent.WorldMatrix);

            for (int j = 0; j < h.Faces.Length; j++)
                wh.Faces[j].Normal = Vector3.TransformNormal(h.Faces[j].Normal, _parent.WorldMatrix);
        }

        // Transform the local bounding box corners
        Vector3[] corners = _corseBoundingBox.GetCorners();
        Vector3 min = new Vector3(float.MaxValue);
        Vector3 max = new Vector3(float.MinValue);
        foreach (Vector3 corner in corners)
        {
            Vector3 transformed = Vector3.Transform(corner, _parent.WorldMatrix);
            min = Vector3.Min(min, transformed);
            max = Vector3.Max(max, transformed);
        }
        _worldBoundingBox = new BoundingBox(min, max);
    }

    public bool Intersects(CollisionMesh other, out Contact contact)
    {
        contact.point = default(Vector3);
        contact.normal = default(Vector3);
        contact.depth = 0;

        if (other == null)
            return false;

        // We can early out if the corse bounding boxes do not intersect.
        if (_corseBoundingBox.Intersects(other._corseBoundingBox) == false)
            return false;

        foreach (var hull in _worldHulls)
        {
            foreach (var hull2 in other._worldHulls)
            {
                if (ConvexHull.Intersects(hull, hull2, out contact))
                    return true;
            }
        }

        return false;
    }

    public void Draw(GraphicsDevice graphicsDevice, Camera camera, Color? color = null)
    {
        if (!_showCollisionMesh)
            return;

        if (_debugEffect == null)
        {
            _debugEffect = new BasicEffect(graphicsDevice);
            _debugEffect.VertexColorEnabled = true;
        }

        _debugEffect.View = camera.ViewMatrix;
        _debugEffect.Projection = camera.ProjectionMatrix;
        _debugEffect.World = Matrix.Identity;

        foreach (var hull in _worldHulls)
        {
            DrawHull(graphicsDevice, hull, Color.Blue);
        }

        // Get the corners of the bounding box
        Vector3[] corners = _worldBoundingBox.GetCorners();

        // Define the 12 edges of the bounding box cube
        // The corners array contains 8 points, ordered:
        // 0: Near bottom left, 1: Near bottom right
        // 2: Far bottom right, 3: Far bottom left
        // 4: Near top left, 5: Near top right
        // 6: Far top right, 7: Far top left
        int[] indices = {
            // Bottom face
            0, 1, 1, 2, 2, 3, 3, 0,
            // Top face
            4, 5, 5, 6, 6, 7, 7, 4,
            // Connecting edges
            0, 4, 1, 5, 2, 6, 3, 7
        };

        // Create colored vertices
        VertexPositionColor[] vertices = new VertexPositionColor[indices.Length];
        for (int i = 0; i < indices.Length; i++)
        {
            vertices[i] = new VertexPositionColor(corners[indices[i]], color ?? Color.Red);
        }

        // Draw the lines
        foreach (EffectPass pass in _debugEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, vertices.Length / 2);
        }

    }

    private void DrawHull(GraphicsDevice graphicsDevice, ConvexHull hull, Color color)
    {
        // TODO: We could cache the debug rendering hulls
        // to avoid per-frame rebuild work.

        // Create colored vertices from world space corners
        var lines = new List<VertexPositionColor>();
        for (var f = 0; f < hull.Faces.Length; f++)
        {
            var face = hull.Faces[f];

            for (var i = 0; i < face.Indices.Length; i++)
            {
                var v1 = face.Indices[i];
                var v2 = face.Indices[(i + 1) % face.Indices.Length];

                lines.Add(new VertexPositionColor(hull.Vertices[v1], color));
                lines.Add(new VertexPositionColor(hull.Vertices[v2], color));
            }
        }

        var verts = lines.ToArray();

        // Draw the lines
        foreach (var pass in _debugEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, verts, 0, verts.Length / 2);
        }
    }
}
