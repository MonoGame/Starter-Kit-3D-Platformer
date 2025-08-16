// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using MonoGame.Framework.Content.Pipeline.Builder;


/// <summary>
/// Entry point for the Content Builder project, 
/// which when executed will build content according to the "Content Collection Strategy" defined in the MyContentCollector class.
/// </summary>
/// <remarks>
/// Make sure to validate the directory paths in the "ContentBuilderParams" for your specific project.
/// For more details regarding the Content Builder, see the MonoGame documentation: <tbc.>
/// </remarks>
///

var contentCollector = new Builder();
contentCollector.Run(args);
return contentCollector.FailedToBuild > 0 ? -1 : 0; 

public class Builder : ContentBuilder
{
    public override IContentCollection GetContentCollection()
    {
        var contentCollection = new ContentCollection();

        // include everything in the folder
        contentCollection.Include<RegexRule>(".");
        contentCollection.Include<WildcardRule>("*.fbx", new FbxImporter(), new MeshAnimatedModelProcessor());
        contentCollection.Include<WildcardRule>("*.glb", new FbxImporter(), new MeshAnimatedModelProcessor());

        // Copy out the level json files.
        contentCollection.IncludeCopy<WildcardRule>("*.json");

        // We use .ogg files for SoundEffects and not Song.
        contentCollection.Include<WildcardRule>("*.ogg", new OggImporter(), new SoundEffectProcessor());

        return contentCollection;
    }
}
