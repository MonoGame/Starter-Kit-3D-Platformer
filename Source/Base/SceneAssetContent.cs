using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

/// <summary>
/// Root content type for a compiled scene asset.
/// </summary>
public class SceneAssetContent
{
    public List<SceneNodeContent> Nodes { get; set; } = [];
}

/// <summary>
/// Root content type for the compiled level index asset.
/// </summary>
public class SceneListContent
{
    public List<string> SceneNames { get; set; } = [];
}

/// <summary>
/// Shared scene node types produced by the content pipeline.
/// </summary>
public enum SceneNodeType
{
    Unknown = 0,
    Scene,
    Camera,
    Light,
    SpawnPoint,
    Goal,
    Mesh
}

/// <summary>
/// Shared scene node data produced by the content pipeline.
/// </summary>
public class SceneNodeContent
{
    public string Name { get; set; } = string.Empty;
    public SceneNodeType Type { get; set; } = SceneNodeType.Unknown;
    public string InstanceOf { get; set; } = string.Empty;

    public bool HasPosition { get; set; }
    public Vector3 Position { get; set; }

    public bool HasRotation { get; set; }
    public Vector3 RotationDegrees { get; set; }

    public bool HasScale { get; set; }
    public Vector3 Scale { get; set; } = Vector3.One;

    public bool HasCollidable { get; set; }
    public bool Collidable { get; set; }

    public string ColorHex { get; set; } = string.Empty;
    public string BackgroundHex { get; set; } = string.Empty;

    public bool HasIntensity { get; set; }
    public float Intensity { get; set; }

    public bool HasRadius { get; set; }
    public float Radius { get; set; }

    public bool HasJumpForce { get; set; }
    public float JumpForce { get; set; }

    public bool HasMoveSpeed { get; set; }
    public float MoveSpeed { get; set; }

    public bool HasDirection { get; set; }
    public Vector3 Direction { get; set; }

    public bool HasUp { get; set; }
    public Vector3 Up { get; set; }

    public bool HasFieldOfView { get; set; }
    public float FieldOfView { get; set; }

    public List<SceneSplineContent> Splines { get; set; } = new();
}

/// <summary>
/// Shared spline data for moving platform paths.
/// </summary>
public class SceneSplineContent
{
    public string Type { get; set; } = string.Empty;
    public List<Vector3> Points { get; set; } = new();
}

public static class SceneContentValueConverters
{
    public static Quaternion ToQuaternion(Vector3 rotationDegrees)
    {
        return Quaternion.CreateFromYawPitchRoll(
            MathHelper.ToRadians(rotationDegrees.Z),
            MathHelper.ToRadians(rotationDegrees.X),
            MathHelper.ToRadians(rotationDegrees.Y));
    }

    public static Color ToColor(string hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return fallback;

        if (hex.StartsWith("#", StringComparison.Ordinal) && hex.Length >= 7)
        {
            byte r = Convert.ToByte(hex.Substring(1, 2), 16);
            byte g = Convert.ToByte(hex.Substring(3, 2), 16);
            byte b = Convert.ToByte(hex.Substring(5, 2), 16);
            return new Color(r, g, b);
        }

        return fallback;
    }
}