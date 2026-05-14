
using System.Text.Json;
using Microsoft.Xna.Framework.Content.Pipeline;

/// <summary>
/// Build-time wrapper around an authored JSON document.
/// </summary>
public sealed class JsonDocumentContent
{
    public JsonDocumentContent(string sourceFilename, JsonElement rootElement)
    {
        SourceFilename = sourceFilename;
        RootElement = rootElement;
    }

    public string SourceFilename { get; }

    public JsonElement RootElement { get; }
}

/// <summary>
/// Imports authored JSON files into a build-time JSON document wrapper.
/// </summary>
[ContentImporter(".json", DisplayName = "Json Importer", DefaultProcessor = "JsonSceneProcessor")]
public sealed class JsonImporter : ContentImporter<JsonDocumentContent>
{
    public override JsonDocumentContent Import(string filename, ContentImporterContext context)
    {
        using var stream = File.OpenRead(filename);
        using var document = JsonDocument.Parse(stream);
        return new JsonDocumentContent(filename, document.RootElement.Clone());
    }
}