using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Graphics;

[ContentProcessor(DisplayName = "Mesh Animated Model Processor")]
class MeshAnimatedModelProcessor : ModelProcessor
{
    public override ModelContent Process(NodeContent input, ContentProcessorContext context)
    {
        MeshAnimatedModelHelper.FlattenAnimationKeyframes(input);
        var content = base.Process(input, context); 
        var clips = MeshAnimatedModelHelper.ProcessNodeAnimations(input, content.Bones);
        var data = new AnimationData(clips);
        content.Tag = data;
        return content;
    }
}
