using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework.Content;

public class Entity
{
    protected const float GRAVITY = -9.81f; // Gravity constant 
    protected const float GRAVITY_SCALE = 10f; // Gravity scale for the entity
    public Model Model;
    public Matrix WorldMatrix = Matrix.Identity;
    public Vector3 Position = Vector3.Zero;
    public Vector3 Scale = Vector3.One;
    public Quaternion Rotation = Quaternion.Identity;
    public bool IsBlockingMovement = true; // Flag to block movement
    private CollisionMesh _collisionMesh; // Model space bounding box

    public float SpecularIntensity = 0.5f; // Intensity of specular highlights
    public float Shininess = 16f; // Power of the specular highlights

    public BoundingBox BoundingBox => _collisionMesh.WorldBoundingBox; // World space bounding box

    protected ContentManager Content;
    public CollisionMesh CollisionMesh => _collisionMesh;
    public Matrix[] MeshTransforms { get; protected set; } = new Matrix[0]; // Transforms for each mesh in the model

    public Entity(Model model, ContentManager contentManager)
    {
        Model = model;
        WorldMatrix = Matrix.Identity;
        Content = contentManager;

        // Calculate the local bounding box once
        if (model != null)
        {
            _collisionMesh = new CollisionMesh(this);
            _collisionMesh.GenerateFromModel(model);
            _collisionMesh.UpdateWorldCollisionMesh();
            MeshTransforms = new Matrix[model.Bones.Count];
            for (int i = 0; i < model.Bones.Count; i++)
            {
                MeshTransforms[i] = Matrix.Identity; // Initialize with identity
            }
        }

        LoadContent();
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
        // Check for collision with another entity
        return _collisionMesh.Intersects(other._collisionMesh);
    }

    public virtual bool Dead ()
    {
        // Check if the entity is dead (e.g., out of bounds)
        return Position.Y < -1000; // Example threshold for "dead"
    }

    public virtual void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Camera camera)
    {
        if (Model == null)
            return;

        // Matrix[] transforms = new Matrix[Model.Bones.Count];
        // Model.CopyAbsoluteBoneTransformsTo(transforms);
        // foreach (var mesh in Model.Meshes)
        // {
        //     foreach (BasicEffect effect in mesh.Effects)
        //     {
        //         effect.World = transforms[mesh.ParentBone.Index] * WorldMatrix;
        //         effect.View = camera.ViewMatrix;
        //         effect.Projection = camera.ProjectionMatrix;
        //     }
        //     mesh.Draw();
        // }
        if (_collisionMesh != null)
            _collisionMesh.Draw(graphicsDevice, camera);
        
    }

    public virtual void DrawBillboards(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Camera camera)
    {
    }
}