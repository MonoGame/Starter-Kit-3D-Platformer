// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// A dust entity that emits dust particles.
/// The dust particles slowly fade out and disappear.
/// The AddDust method is called as the player moves around
/// creating a trail of dust particles.
/// </summary>
public class Dust : Entity
{
    struct DustParticle
    {
        public Vector3 Position;
        public float Age = -1f;

        public DustParticle() { }
    }
    DustParticle[] _dustParticles = new DustParticle[50];
    private const float DustScale = 1f; // Adjust the scale as needed
    private const float DustRotationSpeed = 0.1f; // Adjust the rotation speed as needed
    private const float DustLifetime = 1.0f; // Lifetime in seconds
    private float lastDustAddedTime = 0f;
    private const float DustInterval = 0.05f; // Time interval between dust particles
    public Dust(Model model, ContentManager contentManager) : base(model, contentManager)
    {
        // Initialize dust particles
        for (int i = 0; i < _dustParticles.Length; i++)
        {
            _dustParticles[i] = new DustParticle() { Age = -1f };
        }
    }

    public void AddDust(GameTime gameTime, Vector3 position)
    {
        // Add a new dust particle at the specified position
        lastDustAddedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (lastDustAddedTime < DustInterval)
        {
            return;
        }
        for (int i = _dustParticles.Length - 1; i >= 0; i--)
        {
            if (_dustParticles[i].Age < 0)
            {
                position.X += Random.Shared.NextSingle() * 20f - 10f;
                position.Y += Random.Shared.NextSingle() * 10f - 5f;
                position.Z += Random.Shared.NextSingle() * 20f - 10f;
                _dustParticles[i] = new DustParticle() { Position = position, Age = 0f };
                lastDustAddedTime = 0f;
                return;
            }
        }
    }

    public override void Update(GameTime gameTime)
    {
        // Update dust properties here if needed
        base.Update(gameTime);

        for (int i = _dustParticles.Length - 1; i >= 0; i--)
        {
            _dustParticles[i].Age += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_dustParticles[i].Age > DustLifetime)
            {
                // Remove the particle if it has exceeded its lifetime
                _dustParticles[i].Age = -1;
                _dustParticles[i].Position = Vector3.Zero; // Reset position
            }
        }
    }

    public override void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Camera camera)
    {
        for (int i = 0; i < _dustParticles.Length; i++)
        {
            var particle = _dustParticles[i];
            if (particle.Age < 0 || particle.Age > DustLifetime || particle.Position == Vector3.Zero)
            {
                continue; // Skip dead particles
            }
            var scale = DustScale * (1 - (particle.Age / DustLifetime));
            var worldMatrix = Matrix.CreateScale(scale) * Matrix.CreateTranslation(particle.Position);
            // Apply rotation based on age
            //var rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, DustRotationSpeed * particle.Age);
            //worldMatrix *= Matrix.CreateFromQuaternion(rotation);

            // Draw the dust particle
            Matrix[] transforms = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(transforms);
            foreach (var mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = transforms[mesh.ParentBone.Index] * worldMatrix;
                    effect.View = camera.ViewMatrix;
                    effect.Projection = camera.ProjectionMatrix;
                }
                mesh.Draw();
            }
        }
    }
}
