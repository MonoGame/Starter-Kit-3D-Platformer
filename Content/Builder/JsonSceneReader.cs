using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;

internal static class JsonSceneReader
{
    public static SceneNodeContent ReadNode(JsonElement element, string sourceFilename)
    {
        EnsureValueKind(element, JsonValueKind.Object, sourceFilename, "a scene node");

        var node = new SceneNodeContent
        {
            Name = TryReadString(element, sourceFilename, "name"),
            Type = ReadNodeType(element, sourceFilename),
            InstanceOf = TryReadString(element, sourceFilename, "instanceof"),
            ColorHex = TryReadString(element, sourceFilename, "color"),
            BackgroundHex = TryReadString(element, sourceFilename, "background")
        };

        if (TryReadVector3(element, sourceFilename, "position", out var position))
        {
            node.HasPosition = true;
            node.Position = position;
        }

        if (TryReadVector3(element, sourceFilename, "rotation", out var rotation))
        {
            node.HasRotation = true;
            node.RotationDegrees = rotation;
        }

        if (TryReadVector3(element, sourceFilename, "scale", out var scale))
        {
            node.HasScale = true;
            node.Scale = scale;
        }

        if (TryReadVector3(element, sourceFilename, "direction", out var direction))
        {
            node.HasDirection = true;
            node.Direction = direction;
        }

        if (TryReadVector3(element, sourceFilename, "up", out var up))
        {
            node.HasUp = true;
            node.Up = up;
        }

        if (TryReadBoolean(element, sourceFilename, "collidable", out var collidable))
        {
            node.HasCollidable = true;
            node.Collidable = collidable;
        }

        if (TryReadSingle(element, sourceFilename, "intensity", out var intensity))
        {
            node.HasIntensity = true;
            node.Intensity = intensity;
        }

        if (TryReadSingle(element, sourceFilename, "radius", out var radius))
        {
            node.HasRadius = true;
            node.Radius = radius;
        }

        if (TryReadSingle(element, sourceFilename, "jumpforce", out var jumpForce))
        {
            node.HasJumpForce = true;
            node.JumpForce = jumpForce;
        }

        if (TryReadSingle(element, sourceFilename, "movespeed", out var moveSpeed))
        {
            node.HasMoveSpeed = true;
            node.MoveSpeed = moveSpeed;
        }

        if (TryReadSingle(element, sourceFilename, "fov", out var fieldOfView))
        {
            node.HasFieldOfView = true;
            node.FieldOfView = fieldOfView;
        }

        if (element.TryGetProperty("splines", out var splinesElement))
            node.Splines = ReadSplines(splinesElement, sourceFilename);

        return node;
    }

    public static void EnsureValueKind(JsonElement element, JsonValueKind expected, string sourceFilename, string target)
    {
        if (element.ValueKind != expected)
        {
            throw new InvalidContentException(
                $"Scene json '{Path.GetFileName(sourceFilename)}' expected {target} to be {expected} but found {element.ValueKind}.");
        }
    }

    public static string TryReadString(JsonElement element, string sourceFilename, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return string.Empty;

        if (property.ValueKind != JsonValueKind.String)
            throw new InvalidContentException($"Scene json '{Path.GetFileName(sourceFilename)}' property '{propertyName}' must be a string.");

        return property.GetString() ?? string.Empty;
    }

    public static bool TryReadBoolean(JsonElement element, string sourceFilename, string propertyName, out bool value)
    {
        value = default;
        if (!element.TryGetProperty(propertyName, out var property))
            return false;

        if (property.ValueKind != JsonValueKind.True && property.ValueKind != JsonValueKind.False)
            throw new InvalidContentException($"Scene json '{Path.GetFileName(sourceFilename)}' property '{propertyName}' must be a boolean.");

        value = property.GetBoolean();
        return true;
    }

    public static bool TryReadSingle(JsonElement element, string sourceFilename, string propertyName, out float value)
    {
        value = default;
        if (!element.TryGetProperty(propertyName, out var property))
            return false;

        if (property.ValueKind != JsonValueKind.Number)
            throw new InvalidContentException($"Scene json '{Path.GetFileName(sourceFilename)}' property '{propertyName}' must be a number.");

        value = property.GetSingle();
        return true;
    }

    public static bool TryReadVector3(JsonElement element, string sourceFilename, string propertyName, out Vector3 value)
    {
        value = default;
        if (!element.TryGetProperty(propertyName, out var property))
            return false;

        value = ReadVector3(property, sourceFilename, propertyName);
        return true;
    }

    public static Vector3 ReadVector3(JsonElement element, string sourceFilename, string target)
    {
        EnsureValueKind(element, JsonValueKind.Array, sourceFilename, $"property '{target}'");

        if (element.GetArrayLength() != 3)
            throw new InvalidContentException($"Scene json '{Path.GetFileName(sourceFilename)}' property '{target}' must contain exactly three numeric values.");

        float[] components = new float[3];
        int index = 0;
        foreach (var component in element.EnumerateArray())
        {
            if (component.ValueKind != JsonValueKind.Number)
                throw new InvalidContentException($"Scene json '{Path.GetFileName(sourceFilename)}' property '{target}' must contain only numeric values.");

            components[index++] = component.GetSingle();
        }

        return new Vector3(components[0], components[1], components[2]);
    }

    private static SceneNodeType ReadNodeType(JsonElement element, string sourceFilename)
    {
        var rawType = TryReadString(element, sourceFilename, "type");
        if (string.IsNullOrWhiteSpace(rawType))
            return SceneNodeType.Unknown;

        return rawType.Trim().ToUpperInvariant() switch
        {
            "SCENE" => SceneNodeType.Scene,
            "CAMERA" => SceneNodeType.Camera,
            "LIGHT" => SceneNodeType.Light,
            "SPAWNPOINT" => SceneNodeType.SpawnPoint,
            "GOAL" => SceneNodeType.Goal,
            "MESH" => SceneNodeType.Mesh,
            _ => throw new InvalidContentException($"Scene json '{Path.GetFileName(sourceFilename)}' contains unsupported node type '{rawType}'.")
        };
    }

    private static List<SceneSplineContent> ReadSplines(JsonElement splinesElement, string sourceFilename)
    {
        EnsureValueKind(splinesElement, JsonValueKind.Array, sourceFilename, "the 'splines' property");

        var splines = new List<SceneSplineContent>();
        foreach (var splineElement in splinesElement.EnumerateArray())
        {
            EnsureValueKind(splineElement, JsonValueKind.Object, sourceFilename, "a spline entry");

            var spline = new SceneSplineContent
            {
                Type = TryReadString(splineElement, sourceFilename, "type")
            };

            if (!splineElement.TryGetProperty("points", out var pointsElement))
                throw new InvalidContentException($"Scene json '{Path.GetFileName(sourceFilename)}' spline is missing 'points'.");

            EnsureValueKind(pointsElement, JsonValueKind.Array, sourceFilename, "the spline points array");
            foreach (var pointElement in pointsElement.EnumerateArray())
            {
                EnsureValueKind(pointElement, JsonValueKind.Object, sourceFilename, "a spline point entry");

                if (!pointElement.TryGetProperty("point", out var vectorElement))
                    throw new InvalidContentException($"Scene json '{Path.GetFileName(sourceFilename)}' spline point is missing 'point'.");

                spline.Points.Add(ReadVector3(vectorElement, sourceFilename, "point"));
            }

            splines.Add(spline);
        }

        return splines;
    }
}