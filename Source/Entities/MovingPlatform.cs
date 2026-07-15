// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// A platform that moves along a predefined path.
/// In Blender, you can define the path using the NURBS curve tool.
/// Then assign the path to the platform using Custom properties.
/// The path data is loaded from JSON.
/// </summary>
public class MovingPlatform : Platform
{
    private FollowPath _followPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="MovingPlatform"/> class.
    /// </summary>
    /// <param name="model">The model representing the platform.</param>
    /// <param name="contentManager">The content manager for loading assets.</param>
    public MovingPlatform(Model model, ContentManager contentManager) : base(model, contentManager)
    {
        _followPath = new FollowPath();
    }

    /// <summary>
    /// Sets the properties of the moving platform from JSON data.
    /// The JSON should contain path data that the platform will follow.
    /// This overrides the base SetProperties method to also load the path.
    /// </summary>
    /// <param name="data">The JSON element containing the path data and other properties</param>
    public override void SetProperties(SceneNodeContent data)
    {
        base.SetProperties(data);
        Position = _followPath.LoadFromContent(data);
    }

    /// <summary>
    /// Gets the current velocity of the moving platform.
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        Velocity = _followPath.GetVelocity();

        // Move the platform back and forth between min and max positions
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position += Velocity * deltaTime;

        _followPath.Update(gameTime, Position);
    }

    /// <summary>
    /// Draws the moving platform and its debug path if in dev mode if enabled.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device.</param>
    /// <param name="spriteBatch">The sprite batch.</param>
    /// <param name="camera">The camera.</param>
    public override void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Camera camera)
    {
        base.Draw(graphicsDevice, spriteBatch, camera);
#if DEVMODE
        _followPath.DrawDebugPath(graphicsDevice);
#endif
    }
}
