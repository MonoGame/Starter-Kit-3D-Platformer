// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class MovingPlatform : Platform
{
    private float _moveSpeed;
    private Vector3 _minMove;
    private Vector3 _maxMove;
    private Vector3 _direction;
    private Vector3 _destination;
    public MovingPlatform(Model model, ContentManager contentManager) : base(model, contentManager)
    {
    }

    public override void SetProperties(JsonElement data)
    {
        base.SetProperties(data);
        _moveSpeed = data.GetProperty("movespeed").GetSingle();
        _minMove = Position + data.GetProperty("minmove").ReadVector3FromJson();
        _maxMove = Position + data.GetProperty("maxmove").ReadVector3FromJson();

        _direction = Vector3.Normalize(_maxMove - _minMove);
        Position = _minMove; // Start at the minimum position
        _destination = _maxMove; // Set the initial destination to the maximum position
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // Move the platform back and forth between min and max positions
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position += _direction * _moveSpeed * deltaTime;
        // Check if we need to reverse direction
        if (Vector3.DistanceSquared(Position, _destination) < 1f)
        {
            _destination = Vector3.Distance(Position, _maxMove) < 1f ? _minMove : _maxMove;
            _direction = Vector3.Normalize(_destination - Position);
        }
    }
}