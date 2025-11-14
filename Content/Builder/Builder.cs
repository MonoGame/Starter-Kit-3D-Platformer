// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System.Diagnostics;
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


// If you need to debug the content build process you can enable
// this, build the game, then attach the debugger when prompted.
//Debugger.Launch();


var builder = new Builder();
builder.Logger = new MsBuildLogger();
builder.Run(args);
var parameters = ContentBuilderParams.Parse(args);
//parameters.Rebuild = true;
builder.Run(parameters);
return builder.FailedToBuild > 0 ? -1 : 0; 


public class Builder : ContentBuilder
{
    public override IContentCollection GetContentCollection()
    {
        var content = new ContentCollection();

        // include everything in the folder
        content.Include<RegexRule>(".");
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
