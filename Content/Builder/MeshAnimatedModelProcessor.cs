// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

[ContentProcessor(DisplayName = "Mesh Animated Model Processor")]
public class MeshAnimatedModelProcessor : ModelProcessor
{
    // TODO: Expose max collision options, disable, max faces, tolerance, etc.

    public override ModelContent Process(NodeContent input, ContentProcessorContext context)
    {
        MeshAnimatedModelHelper.FlattenAnimationKeyframes(input);

        var content = base.Process(input, context);

        var clips = MeshAnimatedModelHelper.ProcessNodeAnimations(input, content.Bones);

        var animations = clips.Count > 0 ? new AnimationData(clips) : null;
        var collisions = ConvexHullHelper.GenerateConvexHulls(content, 64, 0.5f);

        var allVertices = new List<Vector3>();
        foreach (var hull in collisions)
        {
            foreach (var vertex in hull.Vertices)
            {
                allVertices.Add(vertex);
            }
        }
        // create a coarse bounding box from all vertices
        // this is used for culling and collision detection
        var boundingBox = BoundingBox.CreateFromPoints(allVertices);

        // The tag is used to pass extra data from the content pipeline to the engine.
        content.Tag = new ModelData()
        {
            AnimationData = animations,
            CollisionData = collisions,
            BoundingBox = boundingBox
        };

        return content;
    }
}
