// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using System.Text.Json;
using Microsoft.Xna.Framework;

/// <summary>
/// Helper methods for reading common types from JSON elements.
/// </summary>
static public class JsonHelper
{
    /// <summary>
    /// Reads an array of Vector3 points from a JSON array element.
    /// Each point is expected to be an object with a "point" property that is an array of three numbers.
    /// If the element is not an array, returns null.
    /// </summary>
    /// <param name="splineElement">The JSON element to read from.</param>
    /// <returns>The read array of Vector3 points or null.</returns>
    public static Vector3[] ReadSplineFromJson(this JsonElement splineElement)
    {
        if (splineElement.ValueKind != JsonValueKind.Array)
            return null;

        var points = new Vector3[splineElement.GetArrayLength()];
        int index = 0;

        foreach (var point in splineElement.EnumerateArray())
        {
            points[index++] = point.GetProperty("point").ReadVector3FromJson();
        }

        return points;
    }

    /// <summary>
    /// Reads a Vector3 from a JSON array element.
    /// Expects the array to contain three numeric values representing X, Y, and Z.
    /// If the element is null or not an array, returns the specified default value.
    /// </summary>
    /// <param name="positionElement">The JSON element to read from.</param>
    /// <param name="defaultValue">The default value to return if the element is null or not an array.</param>
    /// <returns>The read Vector3 or the default value.</returns>
    public static Vector3 ReadVector3FromJson(this JsonElement positionElement, Vector3 defaultValue = default)
    {
        if (positionElement.ValueKind == JsonValueKind.Null)
            return defaultValue;

        var array = positionElement.EnumerateArray();
        float x = 0, y = 0, z = 0;
        int index = 0;

        foreach (var element in array)
        {
            if (element.ValueKind == JsonValueKind.Number)
            {
                switch (index)
                {
                    case 0: x = element.GetSingle(); break;
                    case 1: y = element.GetSingle(); break;
                    case 2: z = element.GetSingle(); break;
                }
            }
            index++;
        }

        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Reads a Quaternion from a JSON array element.
    /// Expects the array to contain three numeric values representing pitch, yaw, and roll in degrees.
    /// If the element is not an array, returns Quaternion.Identity.
    /// </summary>
    /// <param name="rotationElement">The JSON element to read from.</param>
    /// <returns>The read Quaternion or Quaternion.Identity.</returns>
    public static Quaternion ReadRotationFromJson(this JsonElement rotationElement)
    {
        if (rotationElement.ValueKind != JsonValueKind.Array)
            return Quaternion.Identity;

        var array = rotationElement.EnumerateArray();
        float x = 0, y = 0, z = 0;
        int index = 0;

        foreach (var element in array)
        {
            if (element.ValueKind == JsonValueKind.Number)
            {
                switch (index)
                {
                    case 0: x = element.GetSingle(); break;
                    case 1: y = element.GetSingle(); break;
                    case 2: z = element.GetSingle(); break;
                }
            }
            index++;
        }
        return Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(z), MathHelper.ToRadians(x), MathHelper.ToRadians(y));
    }

    /// <summary>
    /// Reads a Color from a JSON string element.
    /// Expects the string to be in hex format, e.g. "#RRGGBB".
    /// If the element is not a string or not in the correct format, returns Color.Purple.
    /// Because as well all know, purple does not exist in nature.
    /// </summary>
    /// <param name="colorElement">The JSON element to read from.</param>
    /// <returns>The read Color or Color.Purple.</returns>
    public static Color ToColorFromJson(this JsonElement colorElement)
    {
        if (colorElement.ValueKind != JsonValueKind.String)
            return Color.Purple;

        var hex = colorElement.GetString();
        if (hex.StartsWith("#") && hex.Length >= 7)
        {
            byte r = Convert.ToByte(hex.Substring(1, 2), 16);
            byte g = Convert.ToByte(hex.Substring(3, 2), 16);
            byte b = Convert.ToByte(hex.Substring(5, 2), 16);
            return new Color(r, g, b);
        }

        return Color.Purple;
    }

}
