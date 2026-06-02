// Using RBWhitaker.com guide - https://rbwhitaker.com/tutorials/xna/content-pipeline/extending/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

public sealed class SceneAssetContentReader : ContentTypeReader<SceneAssetContent>
{
    /// <summary>
    /// Standard levelx.json reader.
    /// </summary>
    /// <param name="input">The content reader to read from.</param>
    /// <param name="existingInstance">An existing instance of the content, if available.</param>
    /// <returns>The deserialized SceneAssetContent.</returns>
    protected override SceneAssetContent Read(ContentReader input, SceneAssetContent existingInstance)
    {
        var asset = new SceneAssetContent();
        int nodeCount = input.ReadInt32();
        for (int i = 0; i < nodeCount; i++)
        {
            asset.Nodes.Add(ReadNode(input));
        }
        return asset;
    }

    /// <summary>
    /// Detribalise the SceneNodeContent from the same format as the Processor serialises it, to reconstruct the SceneAssetContent in memory at runtime.
    /// </summary>
    /// <param name="input">Serialised content</param>
    /// <returns>Unpacked SceneNodeContent</returns>
    private static SceneNodeContent ReadNode(ContentReader input)
    {
        var node = new SceneNodeContent
        {
            Name = input.ReadString(),
            Type = (SceneNodeType)input.ReadInt32(),
            InstanceOf = input.ReadString(),
            HasPosition = input.ReadBoolean()
        };

        if (node.HasPosition) node.Position = input.ReadVector3();

        node.HasRotation = input.ReadBoolean();
        if (node.HasRotation) node.RotationDegrees = input.ReadVector3();

        node.HasScale = input.ReadBoolean();
        if (node.HasScale) node.Scale = input.ReadVector3();

        node.HasCollidable = input.ReadBoolean();
        if (node.HasCollidable) node.Collidable = input.ReadBoolean();

        node.ColorHex = input.ReadString();
        node.BackgroundHex = input.ReadString();

        node.HasIntensity = input.ReadBoolean();
        if (node.HasIntensity) node.Intensity = input.ReadSingle();

        node.HasRadius = input.ReadBoolean();
        if (node.HasRadius) node.Radius = input.ReadSingle();

        node.HasJumpForce = input.ReadBoolean();
        if (node.HasJumpForce) node.JumpForce = input.ReadSingle();

        node.HasMoveSpeed = input.ReadBoolean();
        if (node.HasMoveSpeed) node.MoveSpeed = input.ReadSingle();

        node.HasDirection = input.ReadBoolean();
        if (node.HasDirection) node.Direction = input.ReadVector3();

        node.HasUp = input.ReadBoolean();
        if (node.HasUp) node.Up = input.ReadVector3();

        node.HasFieldOfView = input.ReadBoolean();
        if (node.HasFieldOfView) node.FieldOfView = input.ReadSingle();

        int splineCount = input.ReadInt32();
        for (int i = 0; i < splineCount; i++)
        {
            node.Splines.Add(ReadSpline(input));
        }

        return node;
    }

    private static SceneSplineContent ReadSpline(ContentReader input)
    {
        var spline = new SceneSplineContent { Type = input.ReadString() };
        int pointCount = input.ReadInt32();
        for (int i = 0; i < pointCount; i++)
        {
            spline.Points.Add(input.ReadVector3());
        }
        return spline;
    }
}

public sealed class SceneListContentReader : ContentTypeReader<SceneListContent>
{
     /// <summary>
    /// Levels array list reader.
    /// </summary>
    /// <param name="input">The content reader to read from.</param>
    /// <param name="existingInstance">An existing instance of the content, if available.</param>
    /// <returns>The deserialized SceneListContent.</returns>
   protected override SceneListContent Read(ContentReader input, SceneListContent existingInstance)
    {
        var list = new SceneListContent();
        int count = input.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            list.SceneNames.Add(input.ReadString());
        }
        return list;
    }
}