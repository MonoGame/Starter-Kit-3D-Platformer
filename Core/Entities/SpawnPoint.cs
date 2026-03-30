// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// This class represents a spawn point in the game.
/// Spawn points are used to define where the player will start or respawn.
/// They do not block movement and are not drawn.
/// You can use Empties' in Blender to define spawn points just place
/// a `IsSpawnPoint` custom property on the Empty.
/// </summary>
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
