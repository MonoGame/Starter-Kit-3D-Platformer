using System.Text.Json;
using Microsoft.Xna.Framework.Content.Pipeline;

/// <summary>
/// Converts authored scene JSON documents into the shared compiled scene content types.
/// </summary>
[ContentProcessor(DisplayName = "Json Scene Processor")]
public sealed class JsonSceneProcessor : ContentProcessor<JsonDocumentContent, SceneAssetContent>
{
    public override SceneAssetContent Process(JsonDocumentContent input, ContentProcessorContext context)
    {
        JsonSceneReader.EnsureValueKind(input.RootElement, JsonValueKind.Array, input.SourceFilename, "the root scene document");

        var scene = new SceneAssetContent();
        foreach (var element in input.RootElement.EnumerateArray())
            scene.Nodes.Add(JsonSceneReader.ReadNode(element, input.SourceFilename));

        return scene;
    }
}