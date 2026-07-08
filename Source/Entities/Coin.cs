// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// A coin entity that spins and bobs up and down.
/// When collected by the player, it increases the player's score and plays a sound.
/// It also emits sparkles periodically and when collected.
/// </summary>
public class Coin : BobingEntity
{
    private float _rotationSpeed = 3f;
    private float _rotationAngle = 0f;
    private bool _collected = false;
    private Vector3 _initialPosition;
    private SoundEffect _collectedSound;
    private Sparkles _sparkles;
    private float _sparkleTimer = 0f;
    private const float SPARKLE_SPAWN_RATE = 0.8f; // Spawn a new sparkle every 0.3 seconds


    public int Value = 1; // Default value of the coin

    public Coin(Model model, ContentManager contentManager) : base(model, contentManager)
    {
        IsBlockingMovement = false; // Coins do not block movement
        _rotationAngle = Random.Shared.NextSingle() * MathHelper.TwoPi; // Random initial rotation
        TimeAccumilator = 0f;
        BobAmplitude = 20f;
        BobSpeed = 4f; // Speed of bobbing
        CastPlacementShadow = true;
        //Shininess = 10f;
        //SpecularIntensity = 0.5f;
    }

    override protected void LoadContent()
    {
        // Load the sound effect for coin collection
        _collectedSound = Content.Load<SoundEffect>("Sounds/coin");
        _sparkles = new Sparkles(Content);
        base.LoadContent();
    }

    public override bool Dead()
    {
        return _collected && _sparkles.Count == 0; // Coin is considered "dead" if collected
    }

    public override bool CheckCollision(Entity other)
    {
        if (_collected)
            return false;

        // Check for collision with the player
        if (other is Player player)
        {
            if (base.CheckCollision(other))
            {
                Visible = false;
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
                _sparkles.CreateSparkle(Position);
            }
        }

        _sparkles.Update(gameTime);

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

        _sparkles.CreateSparkle(Position + offset, new Sparkles.SparkleConfig
        {
            Scale = 0.05f + (float)random.NextDouble() * 0.1f,
            Rotation = (float)random.NextDouble() * MathHelper.TwoPi,
            Type = Sparkles.SparkleType.Coin,
            Lifetime = 0.01f + (float)random.NextDouble(),  // Live for 0.5 to 1.5 seconds
            MaxLifetime = 0.01f + (float)random.NextDouble(),
            VerticalSpread = 50f,
        });
    }

    public override void DrawBillboards(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Camera camera)
    {
        _sparkles.DrawBillboards(graphicsDevice, spriteBatch, camera);
    }
}
