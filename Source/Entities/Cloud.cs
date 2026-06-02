// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// A cloud entity that bobs up and down and has specific material properties.
/// Clouds do not block movement.
/// </summary>
public class Cloud : BobingEntity
{
    public Cloud(Model model, ContentManager contentManager) : base(model, contentManager)
    {
        SpecularIntensity = 0.1f;
        Shininess = 0.5f;
        BobSpeed = 1f;
        IsBlockingMovement = false;
    }
}
