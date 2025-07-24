// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using System.Text.Json;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class MovingPlatform : Platform
{
    private float _moveSpeed;
    private Vector3 _direction;
    private Vector3 _destination;

    private Vector3[] _pathPoints;
    private int _currentPathIndex = 1;
    private int _moveDirection = 1; // 1 for forward, -1 for backward


    public MovingPlatform(Model model, ContentManager contentManager) : base(model, contentManager)
    {
    }

    public override void SetProperties(JsonElement data)
    {
        base.SetProperties(data);
        _moveSpeed = data.GetProperty("movespeed").GetSingle();

        if (data.TryGetProperty("splines", out var splineValue))
        {
            if (splineValue.ValueKind == JsonValueKind.Array)
            {
                foreach (var spline in splineValue.EnumerateArray())
                {
                    // Process each spline point
                    // This could be used to adjust the platform's path

                    // Handle spline data if necessary
                    // This could be used for more complex movement patterns
                    var type = spline.GetProperty("type").GetString();
                    _pathPoints = spline.GetProperty("points").ReadSplineFromJson();
                }
            }
            Position = _pathPoints[0];
            _direction = Vector3.Normalize(_pathPoints[_currentPathIndex] - _pathPoints[0]);
            _destination = _pathPoints[_currentPathIndex];
            _moveDirection = 1; // Start moving towards the first destination
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        Velocity = _direction * _moveSpeed;

        // Move the platform back and forth between min and max positions
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position += Velocity * deltaTime;

        // Check if we need to move to the next point
        if (Vector3.DistanceSquared(Position, _destination) < 1f)
        {
            _currentPathIndex += _moveDirection;
            if (_currentPathIndex > _pathPoints.Length - 1)
            {
                _currentPathIndex = _currentPathIndex - 2; // go back to the first point
                _moveDirection = -1;
                _destination = _pathPoints[_currentPathIndex];
            }
            else if (_currentPathIndex < 0)
            {
                _currentPathIndex = 1; // Loop back to the end
                _moveDirection = 1;
                _destination = _pathPoints[_currentPathIndex];
            }
            else
            {
                _destination = _pathPoints[_currentPathIndex];
            }
            _direction = Vector3.Normalize(_destination - Position);
        }
    }
}
