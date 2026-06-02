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

/// <summary>
/// Converts the authored level index json into a compiled scene list asset.
/// </summary>
[ContentProcessor(DisplayName = "Json Scene List Processor")]
public sealed class JsonSceneListProcessor : ContentProcessor<JsonDocumentContent, SceneListContent>
{
    public override SceneListContent Process(JsonDocumentContent input, ContentProcessorContext context)
    {
        JsonSceneReader.EnsureValueKind(input.RootElement, JsonValueKind.Array, input.SourceFilename, "the root level list document");

        var sceneList = new SceneListContent();
        foreach (var element in input.RootElement.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.String)
                throw new InvalidContentException($"Scene list '{Path.GetFileName(input.SourceFilename)}' must contain only string entries.");

            sceneList.SceneNames.Add(element.GetString() ?? string.Empty);
        }

        return sceneList;
    }
}