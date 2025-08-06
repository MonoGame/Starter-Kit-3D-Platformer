using Microsoft.Xna.Framework.Content.Pipeline;
using MonoGame.Framework.Content.Pipeline.Builder;

namespace PlatformerContentBuilder;

public class MyContentCollector : ContentBuilder
{
    public override IContentCollection GetContentCollection()
    {
        var contentCollection = new ContentCollection();

        // include everything in the folder
        contentCollection.Include<RegexRule>(".");
        contentCollection.Include<WildcardRule>("*.fbx", new FbxImporter(), new MeshAnimatedModelProcessor());

        // override .txt files to be copied
        contentCollection.IncludeCopy<RegexRule>(".txt");
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
