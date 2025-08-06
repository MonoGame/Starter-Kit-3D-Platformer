/// <summary>
/// Entry point for the Content Builder project, 
/// which when executed will build content according to the "Content Collection Strategy" defined in the MyContentCollector class.
/// </summary>
/// <remarks>
/// Make sure to validate the directory paths in the "ContentBuilderParams" for your specific project.
/// For more details regarding the Content Builder, see the MonoGame documentation: <tbc.>
/// </remarks>
///

using PlatformerContentBuilder;
using Microsoft.Xna.Framework.Content.Pipeline;
using MonoGame.Framework.Content.Pipeline.Builder;

var contentCollectionArgs = new ContentBuilderParams()
{
    Mode = ContentBuilderMode.Builder,
    WorkingDirectory = $"{AppContext.BaseDirectory}../../../../", // path to where your content folder can be located
    SourceDirectory = "Content", // Not actually needed as this is the default, but added for reference
    OutputDirectory = $"{AppContext.BaseDirectory}../../../../Content/Output",
    Platform = TargetPlatform.DesktopGL
};
var contentCollector = new MyContentCollector();
contentCollector.Run(contentCollectionArgs); // alternativly just pass args to read from command line
