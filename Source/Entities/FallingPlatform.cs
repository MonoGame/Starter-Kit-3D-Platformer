// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// A platform that falls after the player lands on it.
/// The platform shakes for a short duration before falling.
/// The fall delay and shake amplitude can be adjusted via the
/// _fallDelay and _shakeAmplitude fields.
/// </summary>
public class FallingPlatform : Platform
{
    private float _fallTime;
    private float _fallDelay = 1.5f; // Duration before the platform falls
    private bool _isFalling;

    private Vector3 _velocity;
    private Vector3 _originalPosition; // Store original position for shaking
    private float _shakeAmplitude = 5.0f; // Maximum shake distance
    private Random _random; // For random shake values

    // Sound effect for falling
    private SoundEffect _fallSound;

    public FallingPlatform(Model model, ContentManager contentManager) : base(model, contentManager)
    {
        _random = new Random();
    }

    protected override void LoadContent()
    {
        base.LoadContent();

        _fallSound = Content.Load<SoundEffect>("Sounds/fall");
    }

    public override bool CheckCollision(Entity other)
    {
        var collision = base.CheckCollision(other);
        if (other is Player player)
        {
            if (collision && !_isFalling)
            {
                // Start falling when the player lands on the platform
                _isFalling = true;
                _fallSound.Play();
                _originalPosition = Position; // Record original position when we start falling
            }
        }
        return collision;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (_isFalling)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _fallTime += deltaTime;

            if (_fallTime > _fallDelay)
            {
                // Apply gravity
                _velocity.Y += GameConstants.GRAVITY * deltaTime;

                // Apply velocity to position with time-based movement
                Position += _velocity;
            }
            else
            {
                // Apply shaking effect before falling
                // Create random jitter
                Vector3 shakeOffset = new Vector3(
                    (float)(_random.NextDouble() * 2 - 1) * _shakeAmplitude,
                    (float)(_random.NextDouble() * 2 - 1) * _shakeAmplitude * 0.5f, // Less vertical shake
                    (float)(_random.NextDouble() * 2 - 1) * _shakeAmplitude
                );

                // Apply a visual shake offset from original position
                WorldMatrix.Translation = _originalPosition + shakeOffset;
            }
        }
    }
}
