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
    private FollowPath _followPath;

    public MovingPlatform(Model model, ContentManager contentManager) : base(model, contentManager)
    {
        _followPath = new FollowPath();
    }

    public override void SetProperties(JsonElement data)
    {
        base.SetProperties(data);
        Position = _followPath.LoadFromJson(data);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        Velocity = _followPath.GetVelocity();

        // Move the platform back and forth between min and max positions
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position += Velocity * deltaTime;

        _followPath.Update(gameTime, Position);
    }
}
