// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// A basic platform entity.
/// </summary>
public class Platform : Entity
{
    public Platform(Model model, ContentManager contentManager) : base(model, contentManager)
    {
        SpecularIntensity = 0.1f;
        Shininess = 0.5f;
    }

    public Vector3 Velocity { get; protected set; }
}
