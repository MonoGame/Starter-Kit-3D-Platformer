// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework.Content;
using System.Text.Json;

public class Entity
{
    public Model Model;
    public Matrix WorldMatrix = Matrix.Identity;
    public Vector3 Position = Vector3.Zero;
    public Vector3 Scale = Vector3.One;
    public Quaternion Rotation = Quaternion.Identity;
    public bool IsBlockingMovement = true; // Flag to block movement
    protected CollisionMesh _collisionMesh;

    public float SpecularIntensity = 0.5f; // Intensity of specular highlights
    public float Shininess = 16f; // Power of the specular highlights

    /// <summary>
    /// Render this entity into the top down shadow instead of the world shadow.
    /// </summary>
    public bool CastPlacementShadow;

    public BoundingBox BoundingBox => _collisionMesh.WorldBoundingBox; // World space bounding box

    protected ContentManager Content;
    public CollisionMesh CollisionMesh => _collisionMesh;
    public Matrix[] MeshTransforms { get; protected set; } = new Matrix[0]; // Transforms for each mesh in the model

    public bool Visible { get; protected set; }

    public Entity(Model model, ContentManager contentManager)
    {
        Model = model;
        WorldMatrix = Matrix.Identity;
        Content = contentManager;
        Visible = true;

        if (model != null)
        {
            // If we have collision data make the collision mesh.
            var modelData = model.Tag as ModelData;
            if (modelData?.CollisionData?.Count > 0)
                _collisionMesh = new CollisionMesh(this, model, modelData.CollisionData, modelData.BoundingBox);

            MeshTransforms = new Matrix[model.Bones.Count];
            for (int i = 0; i < model.Bones.Count; i++)
                MeshTransforms[i] = Matrix.Identity;
        }

        LoadContent();
    }

    public virtual void SetProperties(JsonElement data)
    {
        if (data.TryGetProperty("position", out var position))
            Position = position.ReadVector3FromJson();

        if (data.TryGetProperty("rotation", out var rotation))
            Rotation = rotation.ReadRotationFromJson();

        if (data.TryGetProperty("scale", out var scale))
            Scale = scale.ReadVector3FromJson();
    }

    protected virtual void LoadContent()
    {
    }

    public virtual void Update(GameTime gameTime)
    {
        // Update the world matrix based on position, rotation, and scale
        WorldMatrix = Matrix.CreateScale(Scale) * Matrix.CreateFromQuaternion(Rotation) * Matrix.CreateTranslation(Position);

        // Update the world bounding box
        if (_collisionMesh != null)
            _collisionMesh.UpdateWorldCollisionMesh();
    }

    public virtual bool CheckCollision(Entity other)
    {
        if (_collisionMesh == null || other._collisionMesh == null)
            return false;

        return _collisionMesh.Intersects(other._collisionMesh, out var contactNormal, out var penetrationDepth);
    }

    public bool CheckCollision(Entity other, out Vector3 contactNormal, out float penetrationDepth)
    {
        contactNormal = default(Vector3);
        penetrationDepth = 0;

        if (_collisionMesh == null || other._collisionMesh == null)
            return false;

        return _collisionMesh.Intersects(other._collisionMesh, out contactNormal, out penetrationDepth);
    }

    public virtual bool Dead()
    {
        // Check if the entity is dead (e.g., out of bounds)
        return Position.Y < -1000; // Example threshold for "dead"
    }

    public virtual void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Camera camera)
    {
        if (Model == null)
            return;

        if (_collisionMesh != null)
            _collisionMesh.Draw(graphicsDevice, camera);
    }

    public virtual void DrawBillboards(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Camera camera)
    {
    }
}
