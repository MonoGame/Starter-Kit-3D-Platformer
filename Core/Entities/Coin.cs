// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class Coin : BobingEntity
{
    private float _rotationSpeed = 3f;
    private float _rotationAngle = 0f;
    private bool _collected = false;
    private Vector3 _initialPosition;
    private SoundEffectInstance _collectedSound; // Sound effect for coin collection
    private Texture2D _sparkleTexture; // Texture for the coin
    
    // Particle system for sparkles
    private List<Particle> _sparkles = new List<Particle>();
    private float _sparkleTimer = 0f;
    private const float SPARKLE_SPAWN_RATE = 0.8f; // Spawn a new sparkle every 0.3 seconds
    
    // Struct to represent a sparkle particle
    private struct Particle
    {
        public Vector3 Position;
        public float Scale;
        public float Rotation;
        public Color Color;
        public float Lifetime;
        public float MaxLifetime;
    }

    public int Value = 1; // Default value of the coin

    public Coin(Model model, ContentManager contentManager) : base(model, contentManager)
    {
        IsBlockingMovement = false; // Coins do not block movement
        _rotationAngle = Random.Shared.NextSingle() * MathHelper.TwoPi; // Random initial rotation
        foreach (var mesh in Model.Meshes)
        {
            foreach (var part in mesh.MeshParts)
            {
                var basicEffect = (BasicEffect)part.Effect;
                basicEffect.EnableDefaultLighting();
                basicEffect.DiffuseColor = new Vector3(1f, 1f, 0.5f); // Gold-like color
                basicEffect.AmbientLightColor = new Vector3(0.5f, 0.5f, 0.5f); // Ambient light
                basicEffect.SpecularColor = new Vector3(1f, 1f, 0.5f); // Gold-like color
                basicEffect.SpecularPower = 16f;
                basicEffect.EmissiveColor = new Vector3(1f, 1f, 0.5f); // Emissive color for glow
            }
        }
        TimeAccumilator = 0f;
        BobAmplitude = 20f;
        BobSpeed = 4f; // Speed of bobbing

        //Shininess = 10f;
        //SpecularIntensity = 0.5f;
    }

    override protected void LoadContent()
    {
        // Load the sound effect for coin collection
        _collectedSound = Content.Load<SoundEffect>("Sounds/coin").CreateInstance();
        _sparkleTexture = Content.Load<Texture2D>("Textures/particle"); // Load the coin texture
        base.LoadContent();
    }
    
    public override bool Dead()
    {
        return _collected; // Coin is considered "dead" if collected
    }

    public override bool CheckCollision(Entity other)
    {
        // Check for collision with the player
        if (other is Player player)
        {
            if (base.CheckCollision(other))
            {
                _collected = true; // Mark coin as collected
                player.Score += Value; // Increase player's score
                _collectedSound.Play(); // Play the collection sound
                
                // Create a burst of sparkles when collected
                for (int i = 0; i < 10; i++)
                {
                    CreateSparkle();
                }
                
                return true; // Coin collected
            }
        }
        return false; // No collision
    }

    public override void Update(GameTime gameTime)
    {
        if (_initialPosition == Vector3.Zero)
            _initialPosition = Position; // Store the initial position when created
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // Rotate the coin around its Y-axis
        _rotationAngle += _rotationSpeed * deltaTime;
        if (_rotationAngle > MathHelper.TwoPi)
            _rotationAngle -= MathHelper.TwoPi;

        // Update the world matrix with the new rotation
        Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, _rotationAngle);
        
        // Update sparkle timer and spawn new sparkles periodically if not collected
        if (!_collected)
        {
            _sparkleTimer += deltaTime;
            if (_sparkleTimer >= SPARKLE_SPAWN_RATE)
            {
                _sparkleTimer = 0f;
                CreateSparkle();
            }
        }
        
        // Update existing sparkles
        for (int i = _sparkles.Count - 1; i >= 0; i--)
        {
            var sparkle = _sparkles[i];
            sparkle.Lifetime -= deltaTime;
            
            // Remove expired sparkles
            if (sparkle.Lifetime <= 0)
            {
                _sparkles.RemoveAt(i);
                continue;
            }
            
            // Update sparkle (fade out based on lifetime)
            float lifePercent = sparkle.Lifetime / sparkle.MaxLifetime;
            sparkle.Color = new Color(sparkle.Color.R, sparkle.Color.G, sparkle.Color.B, (byte)(255 * lifePercent));
            sparkle.Rotation += deltaTime * 2f; // Rotate the sparkle
            
            _sparkles[i] = sparkle; // Update the list
        }

        base.Update(gameTime);
    }
    
    private void CreateSparkle()
    {
        // Create a new sparkle at a random position around the coin
        Random random = new Random();
        float radius = 0.8f + random.NextSingle() * 20f; // Radius around the coin
        float angle = (float)random.NextDouble() * MathHelper.TwoPi;
        float height = (float)random.NextDouble() * 50.0f - 5f;
        
        Vector3 offset = new Vector3(
            (float)Math.Cos(angle) * radius, 
            height, 
            (float)Math.Sin(angle) * radius
        );
        
        Particle sparkle = new Particle
        {
            Position = Position + offset,
            Scale = 0.05f + (float)random.NextDouble() * 0.1f, // Random size
            Rotation = (float)random.NextDouble() * MathHelper.TwoPi,
            Color = new Color(
                (byte)(220 + random.Next(35)),    // Mostly yellow/gold
                (byte)(220 + random.Next(35)),
                (byte)(100 + random.Next(100)),
                (byte)(100 + random.Next(100))),
            Lifetime = 0.01f + (float)random.NextDouble(),  // Live for 0.5 to 1.5 seconds
            MaxLifetime = 0.01f + (float)random.NextDouble()
        };
        
        _sparkles.Add(sparkle);
    }

    public override void DrawBillboards(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Camera camera)
    {
        if (_sparkleTexture != null && _sparkles.Count > 0)
        {
            foreach (var sparkle in _sparkles)
            {
                // Convert 3D position to screen position
                Vector3 screenPos = graphicsDevice.Viewport.Project(
                    sparkle.Position,
                    camera.ProjectionMatrix,
                    camera.ViewMatrix,
                    Matrix.Identity);
                
                // Only draw if in front of the camera
                if (screenPos.Z < 1)
                {
                    // Calculate origin (center of texture)
                    Vector2 origin = new Vector2(_sparkleTexture.Width / 2, _sparkleTexture.Height / 2);
                    
                    // Draw the sparkle as a 2D sprite at the projected position
                    spriteBatch.Draw(
                        _sparkleTexture,
                        new Vector2(screenPos.X, screenPos.Y),
                        null,
                        sparkle.Color,
                        sparkle.Rotation,
                        origin,
                        sparkle.Scale * (2.0f - screenPos.Z), // Scale based on distance
                        SpriteEffects.None,
                        screenPos.Z);
                }
            }
        }
    }
}
