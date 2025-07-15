// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class Cloud : BobingEntity
{
    public Cloud(Model model, ContentManager contentManager) : base(model, contentManager)
    {
        SpecularIntensity = 0.1f;
        Shininess = 0.5f;
        BobSpeed = 1f;
        IsBlockingMovement = false; // clouds should not block movement
    }
}
