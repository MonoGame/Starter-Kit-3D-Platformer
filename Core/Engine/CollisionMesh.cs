// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

public class CollisionMesh
{
    // Collision box data structure
    private class OrientedBoundingBox
    {
        public BoundingBox LocalBox { get; set; }
        public Matrix Transform { get; set; }
        public Vector3[] WorldCorners { get; set; } = new Vector3[8];
        
        public OrientedBoundingBox(BoundingBox localBox)
        {
            LocalBox = localBox;
            Transform = Matrix.Identity;
            UpdateWorldCorners();
        }
        
        public void UpdateWorldCorners()
        {
            // Transform the local corners to world space
            Vector3[] corners = LocalBox.GetCorners();
            for (int i = 0; i < 8; i++)
            {
                WorldCorners[i] = Vector3.Transform(corners[i], Transform);
            }
        }
        
        // Check if this OBB intersects with another OBB using the Separating Axis Theorem
        public bool Intersects(OrientedBoundingBox other)
        {
            // We need to test 15 separating axes:
            // - 3 from this box's face normals
            // - 3 from other box's face normals
            // - 9 from cross products of all edges (3x3)
            
            // Face normals for this box (considering orientation)
            Vector3[] axesThis = new Vector3[3];
            axesThis[0] = Vector3.Normalize(WorldCorners[1] - WorldCorners[0]); // X axis
            axesThis[1] = Vector3.Normalize(WorldCorners[3] - WorldCorners[0]); // Y axis
            axesThis[2] = Vector3.Normalize(WorldCorners[4] - WorldCorners[0]); // Z axis
            
            // Face normals for other box
            Vector3[] axesOther = new Vector3[3];
            axesOther[0] = Vector3.Normalize(other.WorldCorners[1] - other.WorldCorners[0]); // X axis
            axesOther[1] = Vector3.Normalize(other.WorldCorners[3] - other.WorldCorners[0]); // Y axis
            axesOther[2] = Vector3.Normalize(other.WorldCorners[4] - other.WorldCorners[0]); // Z axis
            
            // Check face normals from this box
            foreach (var axis in axesThis)
            {
                if (!OverlapOnAxis(this, other, axis))
                    return false;
            }
            
            // Check face normals from other box
            foreach (var axis in axesOther)
            {
                if (!OverlapOnAxis(this, other, axis))
                    return false;
            }
            
            // Check cross-product axes (edge combinations)
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Vector3 axis = Vector3.Cross(axesThis[i], axesOther[j]);
                    
                    // Skip near-zero axes (parallel edges)
                    if (axis.LengthSquared() < 0.0001f)
                        continue;
                        
                    axis = Vector3.Normalize(axis);
                    
                    if (!OverlapOnAxis(this, other, axis))
                        return false;
                }
            }
            
            // No separating axis found, boxes must be intersecting
            return true;
        }
        
        // Check if two OBBs overlap when projected onto a specific axis
        private bool OverlapOnAxis(OrientedBoundingBox a, OrientedBoundingBox b, Vector3 axis)
        {
            // Project all corners onto the axis
            float minA = float.MaxValue, maxA = float.MinValue;
            float minB = float.MaxValue, maxB = float.MinValue;
            
            // Project box A
            foreach (var corner in a.WorldCorners)
            {
                float projection = Vector3.Dot(corner, axis);
                minA = Math.Min(minA, projection);
                maxA = Math.Max(maxA, projection);
            }
            
            // Project box B
            foreach (var corner in b.WorldCorners)
            {
                float projection = Vector3.Dot(corner, axis);
                minB = Math.Min(minB, projection);
                maxB = Math.Max(maxB, projection);
            }
            
            // Check for overlap
            return maxA >= minB && maxB >= minA;
        }
    }
    
    // Collection of collision boxes
    private List<OrientedBoundingBox> _collisionBoxes = new List<OrientedBoundingBox>();
    
    // The local space collision boxes
    private List<BoundingBox> _localCollisionBoxes = new List<BoundingBox>();
    
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
    
    public CollisionMesh(Entity parent)
    {
        _parent = parent;
    }
    
    public void GenerateFromModel(Model model, int boxCount = 8)
    {
        // Clear any existing collision boxes
        _localCollisionBoxes.Clear();
        
        // Get all vertices from the model
        List<Vector3> allVertices = new List<Vector3>();
        Matrix[] transforms = new Matrix[model.Bones.Count];
        model.CopyAbsoluteBoneTransformsTo(transforms);
        
        foreach (ModelMesh mesh in model.Meshes)
        {
            foreach (ModelMeshPart meshPart in mesh.MeshParts)
            {
                int vertexStride = meshPart.VertexBuffer.VertexDeclaration.VertexStride;
                int vertexBufferSize = meshPart.NumVertices * vertexStride;
                
                float[] vertexData = new float[vertexBufferSize / sizeof(float)];
                meshPart.VertexBuffer.GetData<float>(vertexData);
                
                // Extract vertex positions
                for (int i = 0; i < vertexBufferSize / sizeof(float); i += vertexStride / sizeof(float))
                {
                    Vector3 position = new Vector3(vertexData[i], vertexData[i + 1], vertexData[i + 2]);
                    position = Vector3.Transform(position, transforms[mesh.ParentBone.Index]);
                    allVertices.Add(position);
                }
            }
        }
        
        // Simple approach: divide the model into regions and create boxes
        _corseBoundingBox = BoundingBox.CreateFromPoints(allVertices);
        Vector3 dimensions = _corseBoundingBox.Max - _corseBoundingBox.Min;
        
        // Divide model into multiple boxes along its longest axis
        int axis = 0; // 0=X, 1=Y, 2=Z
        if (dimensions.Y > dimensions.X && dimensions.Y > dimensions.Z)
            axis = 1;
        else if (dimensions.Z > dimensions.X && dimensions.Z > dimensions.Y)
            axis = 2;
            
        // Create multiple boxes along the chosen axis
        for (int i = 0; i < boxCount; i++)
        {
            float start = (float)i / boxCount;
            float end = (float)(i + 1) / boxCount;
            
            Vector3 boxMin = _corseBoundingBox.Min;
            Vector3 boxMax = _corseBoundingBox.Max;
            
            // Adjust the min/max along the chosen axis
            switch (axis)
            {
                case 0: // X axis
                    boxMin.X = MathHelper.Lerp(_corseBoundingBox.Min.X, _corseBoundingBox.Max.X, start);
                    boxMax.X = MathHelper.Lerp(_corseBoundingBox.Min.X, _corseBoundingBox.Max.X, end);
                    break;
                case 1: // Y axis
                    boxMin.Y = MathHelper.Lerp(_corseBoundingBox.Min.Y, _corseBoundingBox.Max.Y, start);
                    boxMax.Y = MathHelper.Lerp(_corseBoundingBox.Min.Y, _corseBoundingBox.Max.Y, end);
                    break;
                case 2: // Z axis
                    boxMin.Z = MathHelper.Lerp(_corseBoundingBox.Min.Z, _corseBoundingBox.Max.Z, start);
                    boxMax.Z = MathHelper.Lerp(_corseBoundingBox.Min.Z, _corseBoundingBox.Max.Z, end);
                    break;
            }
            
            _localCollisionBoxes.Add(new BoundingBox(boxMin, boxMax));
        }
        
        // Initialize world-space boxes
        UpdateWorldCollisionMesh();
    }
    
    public void UpdateWorldCollisionMesh()
    {
        _collisionBoxes.Clear();
        
        foreach (BoundingBox localBox in _localCollisionBoxes)
        {
            OrientedBoundingBox worldBox = new OrientedBoundingBox(localBox);
            worldBox.Transform = _parent.WorldMatrix;
            worldBox.UpdateWorldCorners();
            
            _collisionBoxes.Add(worldBox);
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
    
    public bool Intersects(CollisionMesh other)
    {
        if (other == null)
            return false;
        // early out if the corse bounding boxes do not intersect
        if (_corseBoundingBox.Intersects(other._corseBoundingBox) == false)
            return false;
        foreach (OrientedBoundingBox box in _collisionBoxes)
        {
            foreach (OrientedBoundingBox otherBox in other._collisionBoxes)
            {
                if (box.Intersects(otherBox))
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    public Vector3 CalculateCollissionResolution(CollisionMesh other)
    {
        // Calculate the collision resolution vector
        Vector3 resolution = Vector3.Zero;
        
        // Early out if no intersection between coarse bounding boxes
        if (!_worldBoundingBox.Intersects(other._worldBoundingBox))
            return resolution;
            
        bool collisionFound = false;
         
        // Check each pair of collision boxes
        foreach (OrientedBoundingBox box in _collisionBoxes)
        {
            foreach (OrientedBoundingBox otherBox in other._collisionBoxes)
            {
                if (box.Intersects(otherBox))
                {
                    collisionFound = true;
                    
                    break;
                }
            }
            if (collisionFound)
                break;
        }
        
        return resolution;
    }
    
    public void Draw(GraphicsDevice graphicsDevice, Camera camera)
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
        
        foreach (OrientedBoundingBox box in _collisionBoxes)
        {
            DrawBox(graphicsDevice, box, Color.Blue);
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
            vertices[i] = new VertexPositionColor(corners[indices[i]], Color.Red);
        }

        // Draw the lines
        foreach (EffectPass pass in _debugEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, vertices.Length / 2);
        }

    }
    
    private void DrawBox(GraphicsDevice graphicsDevice, OrientedBoundingBox box, Color color)
    {
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

        // Create colored vertices from world space corners
        VertexPositionColor[] vertices = new VertexPositionColor[indices.Length];
        for (int i = 0; i < indices.Length; i++)
        {
            vertices[i] = new VertexPositionColor(box.WorldCorners[indices[i]], color);
        }

        // Draw the lines
        foreach (EffectPass pass in _debugEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, vertices.Length / 2);
        }
    }
}