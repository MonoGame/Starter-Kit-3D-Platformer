// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using MonoGame.Framework.Content.Pipeline.Builder;

var contentCollectionArgs = new ContentBuilderParams()
{
    Mode = ContentBuilderMode.Builder,
    WorkingDirectory = $"{AppContext.BaseDirectory}../../", // path to where your content folder can be located
    SourceDirectory = "Assets", // Not actually needed as this is the default, but added for reference
    Platform = TargetPlatform.DesktopVK
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

        // Only effect files from the Effects folder
        content.Include<WildcardRule>("Effects/*.fx");

        // Only spritefonts from the Fonts folder (not ttf)
        content.Include<WildcardRule>("Font/*.spritefont");

        // include everything in the Models folder
        content.Include<WildcardRule>("Models/*.fbx", new FbxImporter(), new MeshAnimatedModelProcessor());
        content.Include<WildcardRule>("Models/*.glb", new FbxImporter(), new MeshAnimatedModelProcessor());

        // We use .ogg files for SoundEffects and not Song.
        content.Include<WildcardRule>("Sounds/*.ogg", new OggImporter(), new SoundEffectProcessor());
        content.Include<WildcardRule>("Sounds/*.wav");

        // Only import PNG files from the Textures Folder
        content.Include<WildcardRule>("Textures/*.png");
        content.Include("splash-screen.png");

        // Copy out the level json files.
        content.Include<WildcardRule>("Levels/*.json", new JsonImporter(), new JsonSceneProcessor());
        content.Include("Levels/levels.json", new JsonImporter(), new JsonSceneListProcessor());

        // The model is small so we need to scale it up a bunch.
        content.Include("Models/character.glb", new FbxImporter(),
            new MeshAnimatedModelProcessor()
            {
                Scale = 100.0f
            }
        );
        return content;
    }
}
