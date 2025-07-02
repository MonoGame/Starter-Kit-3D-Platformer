// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

[ContentProcessor(DisplayName = "Mesh Animated Model Processor")]
class MeshAnimatedModelProcessor : ModelProcessor
{
    public override ModelContent Process(NodeContent input, ContentProcessorContext context)
    {
        MeshAnimatedModelHelper.FlattenAnimationKeyframes(input);
        var content = base.Process(input, context); 
        var clips = MeshAnimatedModelHelper.ProcessNodeAnimations(input, content.Bones);

        var animations = new AnimationData(clips);

        // The tag is used to pass extra data from the content pipeline to the engine.
        content.Tag = new ModelData()
        {
            AnimationData = animations
        };

        return content;
    }
}
