// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Provides helper methods for drawing simple shapes using SpriteBatch.
/// This is mostly used for debugging purposes.
/// </summary>
static public class SpriteHelper
{
    private static bool _init;
    private static Texture2D _white;

    private static void Setup(GraphicsDevice graphicsDevice)
    {
        if (_init)
            return;

        _white = new Texture2D(graphicsDevice, 1, 1);
        _white.SetData(new[] { Color.White });
        _init = true;
    }

    public static void DrawSquare(this SpriteBatch batch, Vector3 point, int size, Matrix projection, Matrix view, Color color)
    {
        var device = batch.GraphicsDevice;
        Setup(device);

        var screenPos = device.Viewport.Project(
            point,
            projection,
            view,
            Matrix.Identity
        );

        if (screenPos.Z < 0 || screenPos.Z > 1)
            return;

        var rect = new Rectangle(
            (int)(screenPos.X - size / 2),
            (int)(screenPos.Y - size / 2),
            size,
            size
        );

        batch.Draw(_white, rect, color);
    }
}
