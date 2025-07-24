// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

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

    public static Vector3 ReadVector3FromJson(this JsonElement positionElement)
    {
        if (positionElement.ValueKind != JsonValueKind.Array)
            return Vector3.Zero;

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
}
