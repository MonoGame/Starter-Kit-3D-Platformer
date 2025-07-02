// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.
using System;
using System.Reflection.Metadata;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class Goal : Entity
{
    public bool Complete = false; 

    private float _goalDelay = 3f; // Duration before the goal is completed
    private float _goalTime = 0f; // Time since the goal was activated
    public bool GoalReached = false; // Flag to check if the goal was hit

    public float Radius {
        get => _goalBoundingSphere.Radius;
        set
        {
            _goalBoundingSphere.Radius = value;
            _goalBoundingSphere.Center = Position;
        }
    } // Radius of the goal area

    private BoundingSphere _goalBoundingSphere = new BoundingSphere(Vector3.Zero, 1f);

    public Goal(Model model, ContentManager content) : base(model, content)
    {
        IsBlockingMovement = false; // Spawn points should not block movement
        SpecularIntensity = 0.1f;
        Shininess = 0.5f;
    }

    public override bool CheckCollision(Entity other)
    {
        if (!(other is Player)) {
            return false;
        }
        var collision = _goalBoundingSphere.Intersects(other.BoundingBox);
        if (collision)
        {
            GoalReached = true;
            return false; // Ignore the collision
        }
        return collision;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (GoalReached)
        {
            _goalTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_goalTime >= _goalDelay)
            {
                Complete = true; // Mark the goal as complete
                GoalReached = false;
                _goalTime = 0.0f; // Reset the timer
            }
        }
        
    }

    public override void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Camera camera)
    {
       // Draw nothing.
    }
}
