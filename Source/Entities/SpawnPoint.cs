// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class SpawnPoint : Entity
{
    public SpawnPoint(Model model, ContentManager content) : base(model, content)
    {
        IsBlockingMovement = false; // Spawn points should not block movement
    }

    public override void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Camera camera)
    {
       // Draw nothing for spawn points
    }
}