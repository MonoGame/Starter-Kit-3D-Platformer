// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// An entity that bobs up and down using a sine wave.
/// The size and speed of the bobbing can be adjusted via
/// the BobAmplitude and BobSpeed properties.
/// </summary>
public class BobingEntity : Entity
{
    private Vector3 _initialPosition;
    private float _bobAmplitude = 50f;      // Height of the bob in game units
    private float _bobSpeed = 3f;          // Speed of the bobbing motion
    private float _timeAccumulator = 0f;     // To track time for the sine wave

    protected float TimeAccumilator
    {
        get => _timeAccumulator;
        set => _timeAccumulator = value;
    }

    protected float BobAmplitude
    {
        get => _bobAmplitude;
        set => _bobAmplitude = value;
    }
    protected float BobSpeed
    {
        get => _bobSpeed;
        set => _bobSpeed = value;
    }

    public BobingEntity(Model model, ContentManager contentManager) : base(model, contentManager)
    {
        // Randomize the starting phase so entities don't all bob in sync
        _timeAccumulator = new System.Random().Next(0, 628) / 100f; // Random value between 0 and 2π
    }

    public override void Update(GameTime gameTime)
    {
        // Store the initial position when created
        if (_initialPosition == Vector3.Zero)
            _initialPosition = Position;
        // Update the time accumulator
        _timeAccumulator += (float)gameTime.ElapsedGameTime.TotalSeconds * _bobSpeed;

        // Calculate new Y position using a sine wave
        Position = new Vector3(
            Position.X,
            _initialPosition.Y + _bobAmplitude * (float)Math.Sin(_timeAccumulator),
            Position.Z
        );

        base.Update(gameTime);
    }
}
