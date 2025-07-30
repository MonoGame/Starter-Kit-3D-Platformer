// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using Microsoft.Xna.Framework;

static public class MathHelpers
{
    public static float GetAxis(this Vector3 vector, int axis)
    {
        switch (axis)
        {
            default:
                return vector.X;
            case 1:
                return vector.Y;
            case 2:
                return vector.Z;
        }
    }

    public static void SetAxis(ref Vector3 vector, int axis, float value)
    {
        switch (axis)
        {
            default:
                vector.X = value;
                return;
            case 1:
                vector.Y = value;
                return;
            case 2:
                vector.Z = value;
                return;
        }
    }
}
