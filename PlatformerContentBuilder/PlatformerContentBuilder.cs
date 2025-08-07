/// <summary>
/// Entry point for the Content Builder project, 
/// which when executed will build content according to the "Content Collection Strategy" defined in the MyContentCollector class.
/// </summary>
/// <remarks>
/// Make sure to validate the directory paths in the "ContentBuilderParams" for your specific project.
/// For more details regarding the Content Builder, see the MonoGame documentation: <tbc.>
/// </remarks>
///

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
var contentCollector = new PlatformerContentBuilder();
contentCollector.Run(contentCollectionArgs); // alternatively just pass args to read from command line


public class PlatformerContentBuilder : ContentBuilder
{
    public override IContentCollection GetContentCollection()
    {
        var contentCollection = new ContentCollection();

        // include everything in the folder
        contentCollection.Include<RegexRule>(".");
        contentCollection.Include<WildcardRule>("*.fbx", new FbxImporter(), new MeshAnimatedModelProcessor());
        contentCollection.Include<WildcardRule>("*.glb", new FbxImporter(), new MeshAnimatedModelProcessor());

        // override .txt files to be copied
        contentCollection.IncludeCopy<RegexRule>(".json");

        // exclude bin / obj paths
        contentCollection.Exclude<RegexRule>("bin/");
        contentCollection.Exclude<RegexRule>("obj/");
        contentCollection.Exclude<WildcardRule>("*.mgcb");
        contentCollection.Exclude<WildcardRule>("*.contentproj");
        contentCollection.Exclude<WildcardRule>("*.xnb");
        contentCollection.Exclude<WildcardRule>("*.mgcontent");

        return contentCollection;
    }
}
