// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using System.Text.Json;
using Microsoft.Xna.Framework;


static public class JsonHelper
{
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

        return Color.CornflowerBlue;
    }

}
