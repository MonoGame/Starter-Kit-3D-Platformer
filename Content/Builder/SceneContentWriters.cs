// Using RBWhitaker.com guide - https://rbwhitaker.com/tutorials/xna/content-pipeline/extending/

using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

[ContentTypeWriter]
public sealed class SceneAssetContentWriter : ContentTypeWriter<SceneAssetContent>
{
    public override string GetRuntimeReader(TargetPlatform targetPlatform)
        => $"SceneAssetContentReader, {GameConstants.AssemblyName}";

    protected override void Write(ContentWriter output, SceneAssetContent value)
    {
        output.Write(value.Nodes.Count);
        foreach (var node in value.Nodes)
        {
            WriteNode(output, node);
        }
    }

    /// <summary>
    /// Translate the same format in the Processor to serialise the SceneAssetContent
    /// </summary>
    /// <param name="output">Serialised content</param>
    /// <param name="node">Scene node to serialise</param>
    private static void WriteNode(ContentWriter output, SceneNodeContent node)
    {
        output.Write(node.Name);
        output.Write((int)node.Type);
        output.Write(node.InstanceOf);

        output.Write(node.HasPosition);
        if (node.HasPosition) output.Write(node.Position);

        output.Write(node.HasRotation);
        if (node.HasRotation) output.Write(node.RotationDegrees);

        output.Write(node.HasScale);
        if (node.HasScale) output.Write(node.Scale);

        output.Write(node.HasCollidable);
        if (node.HasCollidable) output.Write(node.Collidable);

        output.Write(node.ColorHex);
        output.Write(node.BackgroundHex);

        output.Write(node.HasIntensity);
        if (node.HasIntensity) output.Write(node.Intensity);

        output.Write(node.HasRadius);
        if (node.HasRadius) output.Write(node.Radius);

        output.Write(node.HasJumpForce);
        if (node.HasJumpForce) output.Write(node.JumpForce);

        output.Write(node.HasMoveSpeed);
        if (node.HasMoveSpeed) output.Write(node.MoveSpeed);

        output.Write(node.HasDirection);
        if (node.HasDirection) output.Write(node.Direction);

        output.Write(node.HasUp);
        if (node.HasUp) output.Write(node.Up);

        output.Write(node.HasFieldOfView);
        if (node.HasFieldOfView) output.Write(node.FieldOfView);

        output.Write(node.Splines.Count);
        foreach (var spline in node.Splines)
        {
            WriteSpline(output, spline);
        }
    }

    private static void WriteSpline(ContentWriter output, SceneSplineContent spline)
    {
        output.Write(spline.Type);
        output.Write(spline.Points.Count);
        foreach (var point in spline.Points)
        {
            output.Write(point);
        }
    }
}

[ContentTypeWriter]
public sealed class SceneListContentWriter : ContentTypeWriter<SceneListContent>
{
    public override string GetRuntimeReader(TargetPlatform targetPlatform)
        => $"SceneListContentReader, {GameConstants.AssemblyName}";

    protected override void Write(ContentWriter output, SceneListContent value)
    {
        output.Write(value.SceneNames.Count);
        foreach (var name in value.SceneNames)
        {
            output.Write(name);
        }
    }
}