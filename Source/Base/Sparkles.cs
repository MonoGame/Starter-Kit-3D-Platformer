// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// A reusable sparkle system that can be used by any entity to create magical particle effects.
/// Renders sparkles as billboards that face the camera and supports various effects like
/// spawning, animation, and different sparkle types.
/// </summary>
public class Sparkles
{
    private List<Sparkle> _sparkles = new List<Sparkle>();
    private Texture2D _sparkleTexture;
    private Random _random = new Random();

    // Struct to represent a single sparkle particle
    private struct Sparkle
    {
        public Vector3 Position;
        public float Scale;
        public float Rotation;
        public Color Color;
        public float Lifetime;
        public float MaxLifetime;
        public SparkleType Type;
        public Vector3 Gravity;
    }

    /// <summary>
    /// Different types of sparkle effects
    /// </summary>
    public enum SparkleType
    {
        Coin,        // Golden sparkles for coin collection
        Celebration, // Colorful sparkles for celebrations
        Magic        // Mystical sparkles for special effects
    }

    /// <summary>
    /// Configuration for sparkle spawning
    /// </summary>
    public class SparkleConfig
    {
        public SparkleType Type { get; set; } = SparkleType.Coin;
        public float Lifetime { get; set; } = 0.5f;
        public float MaxLifetime { get; set; } = 1.5f;
        public float Scale { get; set; } = 0.05f;
        public float SpawnRadius { get; set; } = 30f;
        public float Rotation { get; set; } = 0f;
        public float VerticalSpread { get; set; } = 50f;
        public Vector3 Gravity { get; set; } = Vector3.Zero;
    }

    public Sparkles(ContentManager content)
    {
        LoadContent(content);
    }

    private void LoadContent(ContentManager content)
    {
        _sparkleTexture = content.Load<Texture2D>("Textures/particle");
    }

    /// <summary>
    /// Creates a single sparkle at the specified position with the given configuration
    /// </summary>
    public void CreateSparkle(Vector3 position, SparkleConfig config = null)
    {
        if (config == null)
            config = new SparkleConfig();

        float radius = config.SpawnRadius * _random.NextSingle();
        float angle = _random.NextSingle() * MathHelper.TwoPi;
        float height = (_random.NextSingle() - 0.5f) * config.VerticalSpread;

        Vector3 offset = new Vector3(
            (float)Math.Cos(angle) * radius,
            height,
            (float)Math.Sin(angle) * radius
        );

        Color color = GetColorForType(config.Type);
        
        Sparkle sparkle = new Sparkle
        {
            Position = position + offset,
            Scale = config.Scale,
            Rotation = _random.NextSingle() * MathHelper.TwoPi,
            Color = color,
            Lifetime = config.Lifetime,
            MaxLifetime = config.MaxLifetime,
            Type = config.Type,
            Gravity = config.Gravity
        };

        sparkle.MaxLifetime = sparkle.Lifetime; // Store original lifetime
        _sparkles.Add(sparkle);
    }

    /// <summary>
    /// Creates a burst of sparkles at the specified position
    /// </summary>
    public void CreateBurst(Vector3 position, int count = 10, SparkleConfig config = null)
    {
        for (int i = 0; i < count; i++)
        {
            CreateSparkle(position, config);
        }
    }

    /// <summary>
    /// Updates all sparkles, removing expired ones
    /// </summary>
    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

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

            // Update sparkle appearance (fade out based on lifetime)
            float lifePercent = sparkle.Lifetime / sparkle.MaxLifetime;
            byte alpha = (byte)(255 * lifePercent);
            sparkle.Color = new Color(sparkle.Color.R, sparkle.Color.G, sparkle.Color.B, alpha);

            sparkle.Position += sparkle.Gravity * deltaTime;

            _sparkles[i] = sparkle;
        }
    }

    /// <summary>
    /// Draws all sparkles as billboards
    /// </summary>
    public void DrawBillboards(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Camera camera)
    {
        if (_sparkleTexture == null || _sparkles.Count == 0)
            return;

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

                // Scale based on distance and sparkle size
                float finalScale = sparkle.Scale * (2.0f - screenPos.Z);

                // Draw the sparkle as a 2D sprite at the projected position
                spriteBatch.Draw(
                    _sparkleTexture,
                    new Vector2(screenPos.X, screenPos.Y),
                    null,
                    sparkle.Color,
                    sparkle.Rotation,
                    origin,
                    finalScale,
                    SpriteEffects.None,
                    screenPos.Z);
            }
        }
    }

    /// <summary>
    /// Gets the number of active sparkles
    /// </summary>
    public int Count => _sparkles.Count;

    /// <summary>
    /// Clears all sparkles
    /// </summary>
    public void Clear()
    {
        _sparkles.Clear();
    }

    /// <summary>
    /// Gets an appropriate color for the sparkle type
    /// </summary>
    private Color GetColorForType(SparkleType type)
    {
        return type switch
        {
            SparkleType.Coin => new Color(
                (byte)(220 + _random.Next(35)),    // Golden colors
                (byte)(220 + _random.Next(35)),
                (byte)(100 + _random.Next(100)),
                (byte)(150 + _random.Next(105))),
            
            SparkleType.Celebration => new Color(
                (byte)(100 + _random.Next(155)),   // Vibrant random colors
                (byte)(100 + _random.Next(155)),
                (byte)(100 + _random.Next(155)),
                (byte)(200 + _random.Next(55))),
            
            SparkleType.Magic => new Color(
                (byte)(150 + _random.Next(105)),   // Purple/blue magical colors
                (byte)(100 + _random.Next(100)),
                (byte)(200 + _random.Next(55)),
                (byte)(180 + _random.Next(75))),
            
            _ => Color.White
        };
    }
}
