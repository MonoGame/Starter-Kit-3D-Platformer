// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using Microsoft.Xna.Framework;

static public class MathHelpers
{
    /// <summary>
    /// Gets the value of the specified axis from a Vector3.
    /// The axis parameter should be 0 for X, 1 for Y, or 2 for Z.
    /// Returns the value of the specified axis.
    /// </summary>
    /// <param name="vector">The vector to get the axis value from.</param>
    /// <param name="axis">The axis to get (0 = X, 1 = Y, 2 = Z).</param>
    /// <returns>The value of the specified axis.</returns>
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

    /// <summary>
    /// Sets the value of the specified axis in a Vector3.
    /// The axis parameter should be 0 for X, 1 for Y, or 2 for Z.
    /// Sets the value of the specified axis to the given value.
    /// </summary>
    /// <param name="vector">The vector to set the axis value in.</param>
    /// <param name="axis">The axis to set (0 = X, 1 = Y, 2 = Z).</param>
    /// <param name="value">The value to set the axis to.</param>
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

    /// <summary>
    /// Cubic easing function for smooth animation start (ease out).
    /// Starts fast and slows down at the end.
    /// </summary>
    /// <param name="t">Progress value from 0.0 to 1.0</param>
    /// <returns>Eased value from 0.0 to 1.0</returns>
    public static float EaseOutCubic(float t)
    {
        return 1f - MathF.Pow(1f - t, 3f);
    }

    /// <summary>
    /// Cubic easing function for smooth animation end (ease in).
    /// Starts slow and speeds up at the end.
    /// </summary>
    /// <param name="t">Progress value from 0.0 to 1.0</param>
    /// <returns>Eased value from 0.0 to 1.0</returns>
    public static float EaseInCubic(float t)
    {
        return t * t * t;
    }

    /// <summary>
    /// Cubic easing function that eases both in and out.
    /// Starts slow, speeds up in the middle, then slows down at the end.
    /// </summary>
    /// <param name="t">Progress value from 0.0 to 1.0</param>
    /// <returns>Eased value from 0.0 to 1.0</returns>
    public static float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 3f) / 2f;
    }
}
