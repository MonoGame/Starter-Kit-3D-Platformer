// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using MonoGame.Framework.Content.Pipeline.Builder;

var contentCollectionArgs = new ContentBuilderParams()
{
    Mode = ContentBuilderMode.Builder,
    WorkingDirectory = $"{AppContext.BaseDirectory}../../../", // path to where your content folder can be located
    SourceDirectory = "Assets", // Not actually needed as this is the default, but added for reference
    Platform = TargetPlatform.DesktopGL
};
var builder = new Builder();

if (args is not null && args.Length > 0)
{
    builder.Run(args);
}
else
{
    builder.Run(contentCollectionArgs);
}

return builder.FailedToBuild > 0 ? -1 : 0;

public class Builder : ContentBuilder
{
    public override IContentCollection GetContentCollection()
    {
        var content = new ContentCollection();

        // include everything in the folder
        content.Include<WildcardRule>("*");
        content.Include<WildcardRule>("*.fbx", new FbxImporter(), new MeshAnimatedModelProcessor());
        content.Include<WildcardRule>("*.glb", new FbxImporter(), new MeshAnimatedModelProcessor());

        // Copy out the level json files.
        content.IncludeCopy<WildcardRule>("*.json");

        // We use .ogg files for SoundEffects and not Song.
        content.Include<WildcardRule>("*.ogg", new OggImporter(), new SoundEffectProcessor());

        // The model is small so we need to scale it up a buch.
        content.Include("Models/character.glb", new FbxImporter(),
            new MeshAnimatedModelProcessor()
            {
                Scale = 100.0f
            }
        );

        return content;
    }
}
