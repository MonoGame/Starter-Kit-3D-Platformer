// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

/// <summary>
/// Represents a game entity.
/// It is the base class for all the game objects used in the game.
/// It provides common properties and methods for all entities.
/// This class can be extended to create specific types of entities like platforms, coins, etc.
/// It handles the model, position, rotation, scale, collision mesh, and rendering.
/// </summary>
public class Entity
{
    protected CollisionMesh _collisionMesh;
    protected ContentManager Content;

    /// <summary>
    /// Gets the model for this entity.
    /// </summary>
    public Model Model;

    /// <summary>
    /// Gets the world matrix for this entity.
    /// </summary>
    public Matrix WorldMatrix = Matrix.Identity;

    /// <summary>
    /// Gets the position for this entity.
    /// </summary>
    public Vector3 Position = Vector3.Zero;

    /// <summary>
    /// Gets the scale for this entity.
    /// </summary>
    public Vector3 Scale = Vector3.One;

    /// <summary>
    /// Gets the rotation for this entity.
    /// </summary>
    public Quaternion Rotation = Quaternion.Identity;

    /// <summary>
    /// Gets a value indicating whether this entity is blocking movement.
    /// </summary>
    public bool IsBlockingMovement = true; // Flag to block movement

    /// <summary>
    /// Gets the specular intensity for this entity.
    /// </summary>
    public float SpecularIntensity = 0.5f; // Intensity of specular highlights

    /// <summary>
    /// Gets the shininess for this entity.
    /// </summary>
    public float Shininess = 16f; // Power of the specular highlights

    /// <summary>
    /// Render this entity into the top down shadow instead of the world shadow.
    /// </summary>
    public bool CastPlacementShadow;

    /// <summary>
    /// Gets the bounding box for this entity.
    /// </summary>
    public BoundingBox BoundingBox => _collisionMesh.WorldBoundingBox; // World space bounding box

    /// <summary>
    /// Gets the collision mesh for this entity.
    /// </summary>
    public CollisionMesh CollisionMesh => _collisionMesh;

    /// <summary>
    /// Gets the transforms for each mesh in the model.
    /// </summary>
    public Matrix[] MeshTransforms { get; protected set; } = new Matrix[0];

    /// <summary>
    /// Gets a value indicating whether the entity is dead.
    /// </summary>
    public bool IsDead { get; protected set; }

    /// <summary>
    /// Gets a value indicating whether the entity is visible.
    /// </summary>
    public bool Visible { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Entity"/> class.
    /// </summary>
    /// <param name="model">The model associated with the entity.</param>
    /// <param name="contentManager">The content manager for loading assets.</param>
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

    /// <summary>
    /// Sets the properties of the entity from the JSON data.
    /// </summary>
    /// <param name="data">The JSON element containing the properties.</param>
    public virtual void SetProperties(SceneNodeContent data)
    {
        if (data.HasPosition)
        {
            Position = data.Position;
        }

        if (data.HasRotation)
            Rotation = SceneContentValueConverters.ToQuaternion(data.RotationDegrees);

        if (data.HasScale)
            Scale = data.Scale;

        if (data.HasCollidable)
            IsBlockingMovement = data.Collidable;

    }

    /// <summary>
    /// Loads the content for the entity.
    /// </summary>
    protected virtual void LoadContent()
    {
    }

    /// <summary>
    /// Updates the entity.
    /// </summary>
    /// <param name="gameTime">The game time.</param>
    public virtual void Update(GameTime gameTime)
    {
        // Update the world matrix based on position, rotation, and scale
        WorldMatrix = Matrix.CreateScale(Scale) * Matrix.CreateFromQuaternion(Rotation) * Matrix.CreateTranslation(Position);

        // Update the world bounding box
        if (_collisionMesh != null)
            _collisionMesh.UpdateWorldCollisionMesh();
    }

    /// <summary>
    /// Checks for a collision with another entity.
    /// </summary>
    /// <param name="other">The other entity to check for collision.</param>
    /// <returns>True if a collision is detected; otherwise, false.</returns>
    public virtual bool CheckCollision(Entity other)
    {
        if (_collisionMesh == null || other._collisionMesh == null || other.IsDead)
            return false;

        return _collisionMesh.Intersects(other._collisionMesh, out var contact);
    }

    /// <summary>
    /// Checks for a collision with another entity.
    /// </summary>
    /// <param name="other">The other entity to check for collision.</param>
    /// <param name="contact">The contact information if a collision is detected.</param>
    /// <returns>True if a collision is detected; otherwise, false.</returns>
    public bool CheckCollision(Entity other, out Contact contact)
    {
        contact.point = default(Vector3);
        contact.normal = default(Vector3);
        contact.depth = 0;

        if (_collisionMesh == null || other._collisionMesh == null)
            return false;

        return _collisionMesh.Intersects(other._collisionMesh, out contact);
    }

    /// <summary>
    /// Checks if the entity is dead.
    /// </summary>
    /// <returns>True if the entity is dead; otherwise, false.</returns>
    public virtual bool Dead()
    {
        // Check if the entity is dead (e.g., out of bounds)
        // Example threshold for "dead"
        var dead = Position.Y < -1000;

        if (dead && !IsDead)
            IsDead = true;

        return IsDead;
    }

    /// <summary>
    /// Draws the entity.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device.</param>
    /// <param name="spriteBatch">The sprite batch.</param>
    /// <param name="camera">The camera to use for drawing.</param>
    public virtual void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Camera camera)
    {
        if (Model == null)
            return;

        if (_collisionMesh != null)
            _collisionMesh.Draw(graphicsDevice, camera);
    }

    /// <summary>
    /// Draws the billboards for the entity.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device.</param>
    /// <param name="spriteBatch">The sprite batch to use for drawing the billboards.</param>
    /// <param name="camera">The camera to use for drawing.</param>
    public virtual void DrawBillboards(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Camera camera)
    {
    }
}
